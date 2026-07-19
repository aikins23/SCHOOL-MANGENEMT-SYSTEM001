param(
    [string]$SourceMdf = "$env:USERPROFILE\Downloads\database\database\Neat_Academy.mdf",
    [string]$SourceLdf = "$env:USERPROFILE\Downloads\database\database\Neat_Academy_log.ldf",
    [string]$DestinationDirectory = "C:\ProgramData\NyansapoRecovery",
    [string]$SqlServerServiceAccount = "NT SERVICE\MSSQLSERVER",
    [string]$LogPath = ""
)

$ErrorActionPreference = "Stop"

function Assert-Administrator {
    $identity = [Security.Principal.WindowsIdentity]::GetCurrent()
    $principal = New-Object Security.Principal.WindowsPrincipal($identity)
    if (-not $principal.IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)) {
        throw "Run this script from an elevated PowerShell window: right-click PowerShell and choose 'Run as administrator'."
    }
}

$scriptRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
if (-not $LogPath) {
    $LogPath = Join-Path (Split-Path -Parent $scriptRoot) "logs\PrepareLatestDatabaseFiles.log"
}

$logDirectory = Split-Path -Parent $LogPath
if ($logDirectory -and -not (Test-Path -LiteralPath $logDirectory)) {
    New-Item -ItemType Directory -Path $logDirectory -Force | Out-Null
}

Start-Transcript -Path $LogPath -Force | Out-Null

try {
    Assert-Administrator

    if (-not (Test-Path -LiteralPath $SourceMdf)) {
        throw "Source MDF was not found: $SourceMdf"
    }

    if (-not (Test-Path -LiteralPath $SourceLdf)) {
        throw "Source LDF was not found: $SourceLdf"
    }

    New-Item -ItemType Directory -Path $DestinationDirectory -Force | Out-Null

    $destinationMdf = Join-Path $DestinationDirectory "Neat_Academy.mdf"
    $destinationLdf = Join-Path $DestinationDirectory "Neat_Academy_log.ldf"

    Write-Host "Copying MDF to $destinationMdf"
    Copy-Item -LiteralPath $SourceMdf -Destination $destinationMdf -Force

    Write-Host "Copying LDF to $destinationLdf"
    Copy-Item -LiteralPath $SourceLdf -Destination $destinationLdf -Force

    Write-Host "Granting SQL Server service account access to $DestinationDirectory"
    & icacls.exe $DestinationDirectory /grant "${SqlServerServiceAccount}:(OI)(CI)M" /T
    if ($LASTEXITCODE -ne 0) {
        throw "icacls failed with exit code $LASTEXITCODE"
    }

    Get-ChildItem -LiteralPath $DestinationDirectory -File |
        Select-Object FullName, Length, LastWriteTime |
        Format-Table -AutoSize

    Write-Host ""
    Write-Host "Prepared SQL Server-readable database files:"
    Write-Host $destinationMdf
    Write-Host $destinationLdf
}
finally {
    Stop-Transcript | Out-Null
}
