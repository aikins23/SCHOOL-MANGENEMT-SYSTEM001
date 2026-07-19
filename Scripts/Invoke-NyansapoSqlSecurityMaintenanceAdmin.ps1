[CmdletBinding()]
param(
    [string]$ServerName = "localhost",
    [string]$Database = "Neat_Academy",
    [string]$InstanceName = "MSSQLSERVER",
    [string]$InstanceId = "MSSQL17.MSSQLSERVER"
)

$ErrorActionPreference = "Stop"
$maintenanceRoot = Join-Path $PSScriptRoot "..\artifacts\maintenance"
$diagnosticPath = Join-Path $maintenanceRoot "latest-admin-error.json"
New-Item -ItemType Directory -Path $maintenanceRoot -Force | Out-Null

try {
    & (Join-Path $PSScriptRoot "Rotate-NyansapoProductionSqlSecurity.ps1") `
        -ServerName $ServerName `
        -Database $Database `
        -InstanceName $InstanceName `
        -InstanceId $InstanceId `
        -EvidenceDirectory $maintenanceRoot

    if (Test-Path -LiteralPath $diagnosticPath) {
        Remove-Item -LiteralPath $diagnosticPath -Force
    }
    exit 0
}
catch {
    [pscustomobject]@{
        FailedUtc = [DateTime]::UtcNow.ToString("o")
        ExceptionType = $_.Exception.GetType().FullName
        Message = $_.Exception.Message
        ScriptName = $_.InvocationInfo.ScriptName
        ScriptLineNumber = $_.InvocationInfo.ScriptLineNumber
        ScriptStackTrace = $_.ScriptStackTrace
    } | ConvertTo-Json | Set-Content -LiteralPath $diagnosticPath -Encoding UTF8
    exit 1
}
