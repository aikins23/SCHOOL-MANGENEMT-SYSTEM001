[CmdletBinding()]
param(
    [string]$ServerName = "localhost",
    [string]$Database = "Neat_Academy",
    [string]$InstanceName = "MSSQLSERVER",
    [string]$InstanceId = "MSSQL17.MSSQLSERVER",
    [string]$CertificateThumbprint = "",
    [string]$ProtectedConnectionPath = (Join-Path $env:LOCALAPPDATA "Nyansapo ERP\database.connection"),
    [string]$EvidenceDirectory = (Join-Path $PSScriptRoot "..\artifacts\maintenance")
)

$ErrorActionPreference = "Stop"
Add-Type -AssemblyName System.Security
Add-Type -AssemblyName System.Data

$entropy = [Text.Encoding]::UTF8.GetBytes("KingdomPrep.LocalSettings.Secrets.v1")
$registryPath = "HKLM:\SOFTWARE\Microsoft\Microsoft SQL Server\$InstanceId\MSSQLServer\SuperSocketNetLib"
$certificateValidated = $false
$certificateAddedToRoot = $false
$loginRotated = $false
$oldBuilder = $null
$newBuilder = $null
$oldPassword = $null
$newPassword = $null
$loginName = $null
$oldCertificate = $null
$oldForceEncryption = $null
$originalProtectedValue = $null
$protectedConnectionUpdated = $false

function Assert-Administrator {
    $identity = [Security.Principal.WindowsIdentity]::GetCurrent()
    $principal = New-Object Security.Principal.WindowsPrincipal($identity)
    if (-not $principal.IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)) {
        throw "Run this maintenance operation from an elevated PowerShell session."
    }
}

function Read-ProtectedConnection([string]$Path) {
    if (-not (Test-Path -LiteralPath $Path)) {
        throw "The protected desktop database connection was not found at $Path."
    }

    $value = [IO.File]::ReadAllText($Path).Trim()
    if (-not $value.StartsWith("dpapi:v1:", [StringComparison]::Ordinal)) {
        throw "The desktop database connection file is not in the expected protected format."
    }

    $cipher = [Convert]::FromBase64String($value.Substring("dpapi:v1:".Length))
    $plain = [Security.Cryptography.ProtectedData]::Unprotect(
        $cipher,
        $entropy,
        [Security.Cryptography.DataProtectionScope]::CurrentUser)

    try {
        return [Text.Encoding]::UTF8.GetString($plain)
    }
    finally {
        [Array]::Clear($plain, 0, $plain.Length)
    }
}

function Convert-LegacyConnectionKeywords([string]$ConnectionString) {
    $normalized = $ConnectionString
    $aliases = [ordered]@{
        "DataSource" = "Data Source"
        "InitialCatalog" = "Initial Catalog"
        "IntegratedSecurity" = "Integrated Security"
        "UserID" = "User ID"
        "ConnectTimeout" = "Connect Timeout"
        "PersistSecurityInfo" = "Persist Security Info"
        "ApplicationName" = "Application Name"
        "MultipleActiveResultSets" = "Multiple Active Result Sets"
    }

    foreach ($alias in $aliases.GetEnumerator()) {
        $pattern = "(?i)(^|;)\s*" + [Regex]::Escape($alias.Key) + "\s*="
        $replacement = '${1}' + $alias.Value + '='
        $normalized = [Regex]::Replace($normalized, $pattern, $replacement)
    }
    return $normalized
}

function Write-ProtectedConnection([string]$Path, [string]$ConnectionString) {
    $plain = [Text.Encoding]::UTF8.GetBytes($ConnectionString)
    try {
        $cipher = [Security.Cryptography.ProtectedData]::Protect(
            $plain,
            $entropy,
            [Security.Cryptography.DataProtectionScope]::CurrentUser)
        $value = "dpapi:v1:" + [Convert]::ToBase64String($cipher)
        $directory = Split-Path -Parent $Path
        if ($directory -and -not (Test-Path -LiteralPath $directory)) {
            New-Item -ItemType Directory -Path $directory -Force | Out-Null
        }

        $temporaryPath = "$Path.new"
        [IO.File]::WriteAllText($temporaryPath, $value, (New-Object Text.UTF8Encoding($false)))
        Move-Item -LiteralPath $temporaryPath -Destination $Path -Force
    }
    finally {
        [Array]::Clear($plain, 0, $plain.Length)
    }
}

function Test-SqlConnection(
    [Data.SqlClient.SqlConnectionStringBuilder]$Builder,
    [int]$RetrySeconds = 0) {
    $deadline = [DateTime]::UtcNow.AddSeconds($RetrySeconds)
    do {
        [Data.SqlClient.SqlConnection]::ClearAllPools()
        $connection = New-Object Data.SqlClient.SqlConnection($Builder.ConnectionString)
        try {
            $connection.Open()
            $command = $connection.CreateCommand()
            $command.CommandText = "SELECT DB_NAME();"
            $null = $command.ExecuteScalar()
            return
        }
        catch {
            if ([DateTime]::UtcNow -ge $deadline) {
                throw
            }
            Start-Sleep -Seconds 2
        }
        finally {
            $connection.Dispose()
        }
    } while ($true)
}

function Wait-SqlService([string]$Name, [int]$TimeoutSeconds = 45) {
    $deadline = [DateTime]::UtcNow.AddSeconds($TimeoutSeconds)
    do {
        $service = Get-Service -Name $Name
        if ($service.Status -eq "Running") {
            return
        }
        Start-Sleep -Milliseconds 750
    } while ([DateTime]::UtcNow -lt $deadline)

    throw "SQL Server did not return to the Running state within $TimeoutSeconds seconds."
}

function Add-CertificatePrivateKeyRead([Security.Cryptography.X509Certificates.X509Certificate2]$Certificate, [string]$Account) {
    $rsa = [Security.Cryptography.X509Certificates.RSACertificateExtensions]::GetRSAPrivateKey($Certificate)
    if ($null -eq $rsa) {
        throw "The selected SQL Server certificate does not have an RSA private key."
    }

    try {
        $keyPath = $null
        $uniqueNames = New-Object Collections.Generic.List[string]
        if ($rsa -is [Security.Cryptography.RSACng]) {
            if (-not [string]::IsNullOrWhiteSpace($rsa.Key.UniqueName)) {
                $uniqueNames.Add($rsa.Key.UniqueName)
            }
        }
        elseif ($rsa -is [Security.Cryptography.RSACryptoServiceProvider]) {
            $container = $rsa.CspKeyContainerInfo.UniqueKeyContainerName
            if (-not [string]::IsNullOrWhiteSpace($container)) {
                $uniqueNames.Add($container)
            }
        }

        foreach ($uniqueName in $uniqueNames) {
            $candidatePaths = @(
                (Join-Path $env:ProgramData "Microsoft\Crypto\Keys\$uniqueName"),
                (Join-Path $env:ProgramData "Microsoft\Crypto\SystemKeys\$uniqueName"),
                (Join-Path $env:ProgramData "Microsoft\Crypto\RSA\MachineKeys\$uniqueName")
            )
            $keyPath = $candidatePaths | Where-Object { Test-Path -LiteralPath $_ } | Select-Object -First 1
            if ($keyPath) {
                break
            }
        }

        if (-not $keyPath -and $uniqueNames.Count -gt 0) {
            $cryptoRoot = Join-Path $env:ProgramData "Microsoft\Crypto"
            $keyPath = Get-ChildItem -LiteralPath $cryptoRoot -Recurse -Force -File -ErrorAction SilentlyContinue |
                Where-Object { $uniqueNames.Contains($_.Name) } |
                Select-Object -First 1 -ExpandProperty FullName
        }

        if (-not $keyPath -or -not (Test-Path -LiteralPath $keyPath)) {
            throw "Could not locate the certificate private-key file in the Windows machine key stores."
        }

        $acl = Get-Acl -LiteralPath $keyPath
        $rule = New-Object Security.AccessControl.FileSystemAccessRule($Account, "Read", "Allow")
        $acl.SetAccessRule($rule)
        Set-Acl -LiteralPath $keyPath -AclObject $acl
    }
    finally {
        $rsa.Dispose()
    }
}

function New-StrongPassword([int]$Length = 36) {
    if ($Length -lt 24) {
        throw "Generated SQL passwords must be at least 24 characters."
    }

    $groups = @(
        "ABCDEFGHJKLMNPQRSTUVWXYZ",
        "abcdefghijkmnopqrstuvwxyz",
        "23456789",
        "!@#$%^&*_-+="
    )
    $all = ($groups -join "")
    $rng = [Security.Cryptography.RandomNumberGenerator]::Create()
    try {
        $characters = New-Object Collections.Generic.List[char]
        foreach ($group in $groups) {
            $buffer = New-Object byte[] 4
            $rng.GetBytes($buffer)
            $characters.Add($group[[BitConverter]::ToUInt32($buffer, 0) % $group.Length])
        }
        while ($characters.Count -lt $Length) {
            $buffer = New-Object byte[] 4
            $rng.GetBytes($buffer)
            $characters.Add($all[[BitConverter]::ToUInt32($buffer, 0) % $all.Length])
        }

        for ($index = $characters.Count - 1; $index -gt 0; $index--) {
            $buffer = New-Object byte[] 4
            $rng.GetBytes($buffer)
            $swapIndex = [BitConverter]::ToUInt32($buffer, 0) % ($index + 1)
            $temporary = $characters[$index]
            $characters[$index] = $characters[$swapIndex]
            $characters[$swapIndex] = $temporary
        }
        return -join $characters
    }
    finally {
        $rng.Dispose()
    }
}

function Set-LoginPassword([string]$Login, [string]$Password) {
    if ($Login -notmatch '^[A-Za-z0-9_.-]+$') {
        throw "The active SQL login name contains unsupported characters."
    }

    $adminBuilder = New-Object Data.SqlClient.SqlConnectionStringBuilder
    $adminBuilder.set_DataSource($ServerName)
    $adminBuilder.set_InitialCatalog("master")
    $adminBuilder.set_IntegratedSecurity($true)
    $adminBuilder.set_Encrypt($true)
    $adminBuilder.set_TrustServerCertificate($false)
    $adminBuilder.set_ConnectTimeout(10)
    $adminBuilder.set_Pooling($false)
    $adminBuilder.set_ApplicationName("Nyansapo SQL Security Maintenance")

    $connection = New-Object Data.SqlClient.SqlConnection($adminBuilder.ConnectionString)
    try {
        $connection.Open()
        $command = $connection.CreateCommand()
        $command.CommandTimeout = 15
        $command.CommandText = "ALTER LOGIN [$Login] WITH PASSWORD = N'$Password';"
        $null = $command.ExecuteNonQuery()
    }
    finally {
        $connection.Dispose()
    }
}

function Restore-CertificateConfiguration {
    if (-not (Test-Path -LiteralPath $registryPath)) {
        return
    }

    Set-ItemProperty -Path $registryPath -Name Certificate -Value ([string]$oldCertificate)
    Set-ItemProperty -Path $registryPath -Name ForceEncryption -Value ([int]$oldForceEncryption)

    if ($certificateAddedToRoot -and $script:selectedCertificate) {
        $store = New-Object Security.Cryptography.X509Certificates.X509Store("Root", "LocalMachine")
        try {
            $store.Open([Security.Cryptography.X509Certificates.OpenFlags]::ReadWrite)
            $matches = $store.Certificates.Find(
                [Security.Cryptography.X509Certificates.X509FindType]::FindByThumbprint,
                $script:selectedCertificate.Thumbprint,
                $false)
            foreach ($match in $matches) {
                $store.Remove($match)
            }
        }
        finally {
            $store.Close()
        }
    }

    Restart-Service -Name $InstanceName -Force
    Wait-SqlService -Name $InstanceName
}

Assert-Administrator

if (-not (Test-Path -LiteralPath $registryPath)) {
    throw "SQL Server network configuration was not found at $registryPath."
}

$service = Get-CimInstance Win32_Service -Filter "Name='$InstanceName'"
if ($null -eq $service) {
    throw "The SQL Server service $InstanceName was not found."
}

$originalProtectedValue = [IO.File]::ReadAllText($ProtectedConnectionPath)
$protectedConnection = Read-ProtectedConnection -Path $ProtectedConnectionPath
$protectedConnection = Convert-LegacyConnectionKeywords -ConnectionString $protectedConnection
$oldBuilder = New-Object Data.SqlClient.SqlConnectionStringBuilder($protectedConnection)
$protectedConnection = $null

if ($oldBuilder.IntegratedSecurity) {
    throw "The protected desktop connection uses Windows authentication; there is no SQL login password to rotate."
}

$loginName = $oldBuilder.UserID
if ([string]::IsNullOrWhiteSpace($loginName) -or $loginName.Equals("sa", [StringComparison]::OrdinalIgnoreCase)) {
    throw "Refusing to rotate a missing login or the sa account."
}

$oldPassword = $oldBuilder.Password
$oldBuilder.set_DataSource($ServerName)
$oldBuilder.set_InitialCatalog($Database)
$oldBuilder.set_Encrypt($true)
$oldBuilder.set_TrustServerCertificate($true)
$oldBuilder.set_Pooling($false)
$oldBuilder.set_ConnectTimeout(10)
$oldBuilder.set_PersistSecurityInfo($false)
Test-SqlConnection -Builder $oldBuilder

if ([string]::IsNullOrWhiteSpace($CertificateThumbprint)) {
    $candidates = @(Get-ChildItem Cert:\LocalMachine\My | Where-Object {
        $_.HasPrivateKey -and
        $_.NotAfter -gt [DateTime]::Now.AddDays(30) -and
        ($_.DnsNameList.Unicode -contains $ServerName) -and
        ($_.EnhancedKeyUsageList.ObjectId -contains "1.3.6.1.5.5.7.3.1")
    })
    if ($candidates.Count -ne 1) {
        throw "Expected one valid server-authentication certificate for $ServerName, but found $($candidates.Count). Specify -CertificateThumbprint explicitly."
    }
    $script:selectedCertificate = $candidates | Select-Object -First 1
}
else {
    $normalizedThumbprint = $CertificateThumbprint.Replace(" ", "")
    $script:selectedCertificate = Get-Item "Cert:\LocalMachine\My\$normalizedThumbprint"
}

$dnsNames = @($script:selectedCertificate.DnsNameList.Unicode)
if ($dnsNames -notcontains $ServerName) {
    throw "The selected certificate does not contain $ServerName in its subject alternative names."
}
if ($script:selectedCertificate.NotAfter -le [DateTime]::Now.AddDays(30)) {
    throw "The selected certificate expires too soon for deployment."
}

$oldNetworkConfig = Get-ItemProperty -Path $registryPath
$oldCertificate = [string]$oldNetworkConfig.Certificate
$oldForceEncryption = [int]$oldNetworkConfig.ForceEncryption

$timestamp = [DateTime]::UtcNow.ToString("yyyyMMdd_HHmmss")
$evidencePath = Join-Path $EvidenceDirectory $timestamp
New-Item -ItemType Directory -Path $evidencePath -Force | Out-Null
[pscustomobject]@{
    CapturedUtc = [DateTime]::UtcNow.ToString("o")
    Instance = $InstanceName
    InstanceId = $InstanceId
    PreviousCertificate = $oldCertificate
    PreviousForceEncryption = $oldForceEncryption
} | ConvertTo-Json | Set-Content -LiteralPath (Join-Path $evidencePath "sql-network-before.json") -Encoding UTF8

try {
    Add-CertificatePrivateKeyRead -Certificate $script:selectedCertificate -Account $service.StartName

    $rootStore = New-Object Security.Cryptography.X509Certificates.X509Store("Root", "LocalMachine")
    try {
        $rootStore.Open([Security.Cryptography.X509Certificates.OpenFlags]::ReadWrite)
        $trusted = $rootStore.Certificates.Find(
            [Security.Cryptography.X509Certificates.X509FindType]::FindByThumbprint,
            $script:selectedCertificate.Thumbprint,
            $false)
        if ($trusted.Count -eq 0) {
            $rootStore.Add($script:selectedCertificate)
            $certificateAddedToRoot = $true
        }
    }
    finally {
        $rootStore.Close()
    }

    Set-ItemProperty -Path $registryPath -Name Certificate -Value $script:selectedCertificate.Thumbprint.ToLowerInvariant()
    Set-ItemProperty -Path $registryPath -Name ForceEncryption -Value 1
    Restart-Service -Name $InstanceName -Force
    Wait-SqlService -Name $InstanceName

    $validatedOldBuilder = New-Object Data.SqlClient.SqlConnectionStringBuilder($oldBuilder.ConnectionString)
    $validatedOldBuilder.set_TrustServerCertificate($false)
    Test-SqlConnection -Builder $validatedOldBuilder -RetrySeconds 60
    $certificateValidated = $true

    $newPassword = New-StrongPassword
    Set-LoginPassword -Login $loginName -Password $newPassword
    $loginRotated = $true

    $retiredCredentialRejected = $false
    try {
        Test-SqlConnection -Builder $validatedOldBuilder
    }
    catch {
        $retiredCredentialRejected = $true
    }
    if (-not $retiredCredentialRejected) {
        throw "The retired SQL credential was unexpectedly accepted after rotation."
    }

    $newBuilder = New-Object Data.SqlClient.SqlConnectionStringBuilder($validatedOldBuilder.ConnectionString)
    $newBuilder.set_Password($newPassword)
    $newBuilder.set_Pooling($false)
    $newBuilder.set_PersistSecurityInfo($false)
    Test-SqlConnection -Builder $newBuilder

    $persistedBuilder = New-Object Data.SqlClient.SqlConnectionStringBuilder($newBuilder.ConnectionString)
    $persistedBuilder.set_Pooling($true)
    Write-ProtectedConnection -Path $ProtectedConnectionPath -ConnectionString $persistedBuilder.ConnectionString
    $protectedConnectionUpdated = $true

    $roundTrip = New-Object Data.SqlClient.SqlConnectionStringBuilder((Read-ProtectedConnection -Path $ProtectedConnectionPath))
    $roundTrip.set_Pooling($false)
    Test-SqlConnection -Builder $roundTrip -RetrySeconds 20

    [pscustomobject]@{
        CompletedUtc = [DateTime]::UtcNow.ToString("o")
        Server = $ServerName
        Database = $Database
        Instance = $InstanceName
        Login = $loginName
        CertificateSubject = $script:selectedCertificate.Subject
        CertificateThumbprint = $script:selectedCertificate.Thumbprint
        CertificateExpires = $script:selectedCertificate.NotAfter.ToString("o")
        ForceEncryption = $true
        TrustServerCertificate = $false
        RetiredCredentialRejected = $true
        ProtectedConnectionUpdated = $true
    } | ConvertTo-Json | Set-Content -LiteralPath (Join-Path $evidencePath "rotation-result.json") -Encoding UTF8

    Write-Host "SQL transport certificate validated and forced encryption enabled."
    Write-Host "Application login rotated; the retired credential was rejected."
    Write-Host "The current-user DPAPI connection now enforces TrustServerCertificate=False."
    Write-Host "Non-secret maintenance evidence: $evidencePath"
}
catch {
    $operationError = $_
    if ($loginRotated -and $oldPassword) {
        try {
            Set-LoginPassword -Login $loginName -Password $oldPassword
            if ($protectedConnectionUpdated -and $originalProtectedValue) {
                [IO.File]::WriteAllText(
                    $ProtectedConnectionPath,
                    $originalProtectedValue,
                    (New-Object Text.UTF8Encoding($false)))
            }
        }
        catch {
            throw "Maintenance failed and automatic SQL login rollback also failed. Immediate database administrator action is required. Original error: $($operationError.Exception.Message)"
        }
    }

    if (-not $certificateValidated) {
        try {
            Restore-CertificateConfiguration
        }
        catch {
            throw "Certificate validation failed and automatic SQL network rollback also failed. Immediate database administrator action is required. Original error: $($operationError.Exception.Message)"
        }
    }

    throw $operationError
}
finally {
    if ($oldBuilder) {
        $oldBuilder.set_Password("")
    }
    if ($newBuilder) {
        $newBuilder.set_Password("")
    }
    $oldPassword = $null
    $newPassword = $null
}
