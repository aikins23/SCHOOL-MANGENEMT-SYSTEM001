[CmdletBinding()]
param(
    [string]$ProtectedConnectionPath = (Join-Path $env:LOCALAPPDATA "Nyansapo ERP\database.connection"),
    [string]$EvidenceDirectory
)

$ErrorActionPreference = "Stop"
Add-Type -AssemblyName System.Security
Add-Type -AssemblyName System.Data

if ([string]::IsNullOrWhiteSpace($EvidenceDirectory)) {
    $EvidenceDirectory = Join-Path $PSScriptRoot "..\artifacts\maintenance"
}

if (-not (Test-Path -LiteralPath $ProtectedConnectionPath)) {
    throw "The current-user DPAPI database connection file was not found."
}

$value = [IO.File]::ReadAllText($ProtectedConnectionPath).Trim()
if (-not $value.StartsWith("dpapi:v1:", [StringComparison]::Ordinal)) {
    throw "The database connection file is not in the expected protected format."
}

$entropy = [Text.Encoding]::UTF8.GetBytes("KingdomPrep.LocalSettings.Secrets.v1")
$cipher = [Convert]::FromBase64String($value.Substring("dpapi:v1:".Length))
$plain = [Security.Cryptography.ProtectedData]::Unprotect(
    $cipher,
    $entropy,
    [Security.Cryptography.DataProtectionScope]::CurrentUser)

$connectionString = $null
$builder = $null
try {
    $connectionString = [Text.Encoding]::UTF8.GetString($plain)
    $builder = New-Object Data.SqlClient.SqlConnectionStringBuilder($connectionString)
    $connectionString = $null

    if (-not $builder.Encrypt) {
        throw "The protected connection does not enforce SQL encryption."
    }
    if ($builder.TrustServerCertificate) {
        throw "The protected connection bypasses SQL certificate validation."
    }

    $builder.set_Pooling($false)
    [Data.SqlClient.SqlConnection]::ClearAllPools()
    $connection = New-Object Data.SqlClient.SqlConnection($builder.ConnectionString)
    try {
        $connection.Open()

        $sessionCommand = $connection.CreateCommand()
        $sessionCommand.CommandText = @"
SELECT DB_NAME() AS DatabaseName,
       ORIGINAL_LOGIN() AS LoginName,
       CONVERT(nvarchar(40), CONNECTIONPROPERTY('net_transport')) AS NetTransport;
"@
        $reader = $sessionCommand.ExecuteReader()
        try {
            if (-not $reader.Read()) {
                throw "SQL Server did not return session encryption details."
            }
            $databaseName = [string]$reader["DatabaseName"]
            $loginName = [string]$reader["LoginName"]
            $netTransport = [string]$reader["NetTransport"]
        }
        finally {
            $reader.Dispose()
        }

        $studentCommand = $connection.CreateCommand()
        $studentCommand.CommandText = "SELECT COUNT_BIG(*) FROM dbo.Students;"
        $studentCount = [long]$studentCommand.ExecuteScalar()
    }
    finally {
        $connection.Dispose()
    }

    $latest = Get-ChildItem -LiteralPath $EvidenceDirectory -Directory -ErrorAction SilentlyContinue |
        Sort-Object LastWriteTime -Descending |
        Select-Object -First 1
    $outputDirectory = if ($latest) { $latest.FullName } else { $EvidenceDirectory }
    New-Item -ItemType Directory -Path $outputDirectory -Force | Out-Null

    [pscustomobject]@{
        VerifiedUtc = [DateTime]::UtcNow.ToString("o")
        Database = $databaseName
        Login = $loginName
        NetTransport = $netTransport
        ConnectionEncrypt = [bool]$builder.Encrypt
        TrustServerCertificate = [bool]$builder.TrustServerCertificate
        CertificateValidatedConnection = $true
        StudentCount = $studentCount
    } | ConvertTo-Json | Set-Content -LiteralPath (Join-Path $outputDirectory "independent-verification.json") -Encoding UTF8

    Write-Host "Production SQL security verification passed."
    Write-Host "Database: $databaseName"
    Write-Host "Encrypted client connection: $([bool]$builder.Encrypt)"
    Write-Host "Certificate-validated connection: True"
    Write-Host "TrustServerCertificate: $([bool]$builder.TrustServerCertificate)"
    Write-Host "Student count: $studentCount"
}
finally {
    [Array]::Clear($plain, 0, $plain.Length)
    $connectionString = $null
    if ($builder) {
        $builder.set_Password("")
    }
}
