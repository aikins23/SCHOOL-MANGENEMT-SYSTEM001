param(
    [Security.SecureString]$AppPassword,

    [Security.SecureString]$TestPassword,

    [string]$InstanceRegistryPath = "HKLM:\SOFTWARE\Microsoft\Microsoft SQL Server\MSSQL17.MSSQLSERVER\MSSQLServer",
    [string]$SqlCmdPath = ""
)

$ErrorActionPreference = "Stop"
$ScriptRoot = Split-Path -Parent $MyInvocation.MyCommand.Path

function Assert-Administrator {
    $identity = [Security.Principal.WindowsIdentity]::GetCurrent()
    $principal = New-Object Security.Principal.WindowsPrincipal($identity)
    if (-not $principal.IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)) {
        throw "Run this script from an elevated PowerShell window: right-click PowerShell and choose 'Run as administrator'."
    }
}

function Find-SqlCmd {
    if ($SqlCmdPath -and (Test-Path -LiteralPath $SqlCmdPath)) {
        return (Resolve-Path -LiteralPath $SqlCmdPath).Path
    }

    $candidates = @(
        "C:\Program Files\Microsoft SQL Server\Client SDK\ODBC\180\Tools\Binn\SQLCMD.EXE",
        "C:\Program Files\Microsoft SQL Server\Client SDK\ODBC\170\Tools\Binn\SQLCMD.EXE",
        "C:\Program Files\Microsoft SQL Server\170\Tools\Binn\SQLCMD.EXE",
        "sqlcmd.exe"
    )

    foreach ($candidate in $candidates) {
        $command = Get-Command $candidate -ErrorAction SilentlyContinue
        if ($command) {
            return $command.Source
        }
    }

    throw "sqlcmd was not found. Install SQL Server command-line tools and rerun this script."
}

function ConvertTo-PlainText([Security.SecureString]$SecureValue) {
    $pointer = [IntPtr]::Zero
    try {
        $pointer = [Runtime.InteropServices.Marshal]::SecureStringToBSTR($SecureValue)
        return [Runtime.InteropServices.Marshal]::PtrToStringBSTR($pointer)
    }
    finally {
        if ($pointer -ne [IntPtr]::Zero) {
            [Runtime.InteropServices.Marshal]::ZeroFreeBSTR($pointer)
        }
    }
}

Assert-Administrator

if ($null -eq $AppPassword) {
    $AppPassword = Read-Host "New password for the nyansapo_app SQL login" -AsSecureString
}
if ($null -eq $TestPassword) {
    $TestPassword = Read-Host "New password for the nyansapo_test SQL login" -AsSecureString
}
if ($AppPassword.Length -lt 12 -or $TestPassword.Length -lt 12) {
    throw "Each SQL login password must contain at least 12 characters."
}

$SqlCmdPath = Find-SqlCmd
$loginScript = Join-Path $ScriptRoot "scripts\CreateNyansapoSqlLogins.sql"

if (-not (Test-Path -LiteralPath $loginScript)) {
    throw "Could not find login script: $loginScript"
}

$tcpPath = Join-Path $InstanceRegistryPath "SuperSocketNetLib\Tcp"
$npPath = Join-Path $InstanceRegistryPath "SuperSocketNetLib\Np"

Write-Host "Enabling SQL Server mixed authentication..."
Set-ItemProperty -Path $InstanceRegistryPath -Name LoginMode -Value 2

Write-Host "Enabling TCP/IP and Named Pipes..."
Set-ItemProperty -Path $tcpPath -Name Enabled -Value 1
Set-ItemProperty -Path $npPath -Name Enabled -Value 1

$ipAllPath = Join-Path $tcpPath "IPAll"
if (Test-Path -LiteralPath $ipAllPath) {
    Write-Host "Setting SQL Server TCP/IP to listen on port 1433..."
    Set-ItemProperty -Path $ipAllPath -Name TcpDynamicPorts -Value ""
    Set-ItemProperty -Path $ipAllPath -Name TcpPort -Value "1433"
}

Write-Host "Restarting SQL Server service..."
Restart-Service -Name MSSQLSERVER -Force
Start-Sleep -Seconds 8

$service = Get-Service MSSQLSERVER
if ($service.Status -ne "Running") {
    throw "SQL Server did not return to Running state. Current state: $($service.Status)"
}

Write-Host "Creating Nyansapo SQL logins..."
$originalAppPassword = $env:APP_PASSWORD
$originalTestPassword = $env:TEST_PASSWORD
try {
    # sqlcmd reads scripting variables from its child-process environment. This
    # keeps passwords out of the command line, console output, and transcripts.
    $env:APP_PASSWORD = ConvertTo-PlainText $AppPassword
    $env:TEST_PASSWORD = ConvertTo-PlainText $TestPassword
    & $SqlCmdPath -S . -E -N m -C -b -i $loginScript
    if ($LASTEXITCODE -ne 0) {
        throw "sqlcmd failed while creating the Nyansapo SQL logins. Exit code: $LASTEXITCODE"
    }
}
finally {
    if ($null -eq $originalAppPassword) {
        Remove-Item Env:APP_PASSWORD -ErrorAction SilentlyContinue
    }
    else {
        $env:APP_PASSWORD = $originalAppPassword
    }

    if ($null -eq $originalTestPassword) {
        Remove-Item Env:TEST_PASSWORD -ErrorAction SilentlyContinue
    }
    else {
        $env:TEST_PASSWORD = $originalTestPassword
    }
}

Write-Host ""
Write-Host "SQL Server and the Nyansapo SQL logins are configured."
Write-Host "No password or full connection string was written to the console or a log file."
Write-Host "Run Scripts\Set-NyansapoDesktopConnection.ps1 to verify and protect the desktop connection for the current Windows user."
