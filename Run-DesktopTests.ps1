[CmdletBinding()]
param(
    [switch]$Full,
    [string]$MasterConnectionString,
    [string]$TestServer = "localhost",
    [string]$TestUsername = "nyansapo_test",
    [Security.SecureString]$TestPassword,
    [switch]$TrustServerCertificate
)

$ErrorActionPreference = "Stop"
$originalMasterConnection = $env:KINGDOM_TEST_SQL_MASTER_CONNECTION_STRING
$assignedMasterConnection = $false

if ($MasterConnectionString) {
    Write-Warning "Passing a full connection string on the command line can expose its password in shell history. Prefer -TestPassword or the secure prompt."
    $env:KINGDOM_TEST_SQL_MASTER_CONNECTION_STRING = $MasterConnectionString
    $assignedMasterConnection = $true
}
elseif ($Full -and -not $env:KINGDOM_TEST_SQL_MASTER_CONNECTION_STRING) {
    if ($null -eq $TestPassword) {
        $TestPassword = Read-Host "SQL password for $TestUsername" -AsSecureString
    }

    $passwordPointer = [IntPtr]::Zero
    try {
        $passwordPointer = [Runtime.InteropServices.Marshal]::SecureStringToBSTR($TestPassword)
        $plainPassword = [Runtime.InteropServices.Marshal]::PtrToStringBSTR($passwordPointer)
        $builder = New-Object System.Data.SqlClient.SqlConnectionStringBuilder
        $builder.set_DataSource($TestServer)
        $builder.set_InitialCatalog("master")
        $builder.set_UserID($TestUsername)
        $builder.set_Password($plainPassword)
        $builder.set_Encrypt($true)
        $builder.set_TrustServerCertificate([bool]$TrustServerCertificate)
        $builder.set_PersistSecurityInfo($false)
        $builder.set_ConnectTimeout(8)
        $builder.set_ApplicationName("Nyansapo Desktop Integration Tests")
        $env:KINGDOM_TEST_SQL_MASTER_CONNECTION_STRING = $builder.get_ConnectionString()
        $assignedMasterConnection = $true
    }
    finally {
        $plainPassword = $null
        if ($passwordPointer -ne [IntPtr]::Zero) {
            [Runtime.InteropServices.Marshal]::ZeroFreeBSTR($passwordPointer)
        }
    }
}

$argsList = @()
if (-not $Full) {
    $argsList += "--unit-only"
} elseif (-not $env:KINGDOM_TEST_SQL_MASTER_CONNECTION_STRING) {
    Write-Warning "Running full desktop tests without KINGDOM_TEST_SQL_MASTER_CONNECTION_STRING. SQL integration tests may be skipped."
}

try {
    dotnet run --project "Tests\Kingdom.Tests\Kingdom.Tests.csproj" --configuration Release --no-restore -- @argsList
}
finally {
    if ($assignedMasterConnection) {
        if ($null -eq $originalMasterConnection) {
            Remove-Item Env:KINGDOM_TEST_SQL_MASTER_CONNECTION_STRING -ErrorAction SilentlyContinue
        }
        else {
            $env:KINGDOM_TEST_SQL_MASTER_CONNECTION_STRING = $originalMasterConnection
        }
    }
}
