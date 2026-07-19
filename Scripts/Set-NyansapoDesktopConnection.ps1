[CmdletBinding(DefaultParameterSetName = "SqlLogin")]
param(
    [Parameter(Mandatory = $true)]
    [string]$Server,

    [Parameter(Mandatory = $true)]
    [string]$Database,

    [Parameter(Mandatory = $true, ParameterSetName = "SqlLogin")]
    [string]$Username,

    [Parameter(ParameterSetName = "SqlLogin")]
    [Security.SecureString]$Password,

    [Parameter(Mandatory = $true, ParameterSetName = "Windows")]
    [switch]$IntegratedSecurity,

    [switch]$TrustServerCertificate,

    [string]$OutputPath = (Join-Path $env:LOCALAPPDATA "Nyansapo ERP\database.connection"),

    [switch]$Remove
)

$ErrorActionPreference = "Stop"
Add-Type -AssemblyName System.Security

if ($Remove) {
    if (Test-Path -LiteralPath $OutputPath) {
        Remove-Item -LiteralPath $OutputPath -Force
    }
    Write-Host "Protected desktop database connection removed."
    return
}

if (-not $IntegratedSecurity -and $null -eq $Password) {
    $Password = Read-Host "SQL password for $Username" -AsSecureString
}

$plainPassword = $null
$passwordPointer = [IntPtr]::Zero

try {
    $builder = New-Object System.Data.SqlClient.SqlConnectionStringBuilder
    $builder.set_DataSource($Server)
    $builder.set_InitialCatalog($Database)
    $builder.set_Encrypt($true)
    $builder.set_TrustServerCertificate([bool]$TrustServerCertificate)
    $builder.set_ConnectTimeout(8)
    $builder.set_PersistSecurityInfo($false)
    $builder.set_ApplicationName("Nyansapo School ERP Desktop")

    if ($IntegratedSecurity) {
        $builder.set_IntegratedSecurity($true)
    }
    else {
        $passwordPointer = [Runtime.InteropServices.Marshal]::SecureStringToBSTR($Password)
        $plainPassword = [Runtime.InteropServices.Marshal]::PtrToStringBSTR($passwordPointer)
        $builder.set_UserID($Username)
        $builder.set_Password($plainPassword)
    }

    $connection = New-Object System.Data.SqlClient.SqlConnection($builder.get_ConnectionString())
    try {
        $connection.Open()
    }
    finally {
        $connection.Dispose()
    }

    $entropy = [Text.Encoding]::UTF8.GetBytes("KingdomPrep.LocalSettings.Secrets.v1")
    $plainBytes = [Text.Encoding]::UTF8.GetBytes($builder.get_ConnectionString())
    $protectedBytes = [System.Security.Cryptography.ProtectedData]::Protect(
        $plainBytes,
        $entropy,
        [System.Security.Cryptography.DataProtectionScope]::CurrentUser)
    $protectedValue = "dpapi:v1:" + [Convert]::ToBase64String($protectedBytes)

    $directory = Split-Path -Parent $OutputPath
    if ($directory -and -not (Test-Path -LiteralPath $directory)) {
        New-Item -ItemType Directory -Path $directory -Force | Out-Null
    }

    [IO.File]::WriteAllText($OutputPath, $protectedValue, (New-Object Text.UTF8Encoding($false)))

    Write-Host "Desktop database connection verified and protected for the current Windows user."
    Write-Host "Server: $Server"
    Write-Host "Database: $Database"
    Write-Host "Authentication: $(if ($IntegratedSecurity) { 'Windows' } else { 'SQL login' })"
    Write-Host "Encrypt: True"
    Write-Host "Trust server certificate: $([bool]$TrustServerCertificate)"
    Write-Host "Protected file: $OutputPath"
}
finally {
    $plainPassword = $null
    if ($passwordPointer -ne [IntPtr]::Zero) {
        [Runtime.InteropServices.Marshal]::ZeroFreeBSTR($passwordPointer)
    }
}
