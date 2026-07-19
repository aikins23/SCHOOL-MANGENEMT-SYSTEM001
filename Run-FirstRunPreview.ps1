param(
    [switch]$CloseExisting,
    [switch]$NoLaunch,
    [switch]$NoRestore,
    [switch]$IsolatedDotnet
)

$ErrorActionPreference = "Stop"

$root = Split-Path -Parent $MyInvocation.MyCommand.Path
$solution = Join-Path $root "Nyansapo ERP School Management Software.sln"

if ($IsolatedDotnet) {
    $env:DOTNET_CLI_HOME = Join-Path $root ".dotnet"
    $env:DOTNET_SKIP_FIRST_TIME_EXPERIENCE = "1"
    $env:NUGET_PACKAGES = Join-Path $root ".nuget\packages"
    $env:APPDATA = Join-Path $root ".appdata"
    $env:LOCALAPPDATA = Join-Path $root ".localappdata"
    $env:USERPROFILE = Join-Path $root ".userprofile"
    $env:HOME = $env:USERPROFILE

    New-Item -ItemType Directory -Force -Path $env:DOTNET_CLI_HOME, $env:NUGET_PACKAGES, $env:APPDATA, $env:LOCALAPPDATA, $env:USERPROFILE | Out-Null
}

if ($CloseExisting) {
    Get-Process -Name "kingdom_Preparatory_School_Management_System" -ErrorAction SilentlyContinue |
        ForEach-Object {
            if ($_.MainWindowHandle -ne 0) {
                [void]$_.CloseMainWindow()
            }
        }

    Start-Sleep -Seconds 2

    Get-Process -Name "kingdom_Preparatory_School_Management_System" -ErrorAction SilentlyContinue |
        Stop-Process -Force
}

$stamp = Get-Date -Format "yyyyMMdd_HHmmss"
$outDir = "bin\PreviewFirstRun_$stamp\"
$exe = Join-Path $root ($outDir + "kingdom_Preparatory_School_Management_System.exe")

Write-Host "Building first-run preview into $outDir"
if ($NoRestore) {
    dotnet build $solution --no-restore -p:OutDir=$outDir
} else {
    dotnet build $solution -p:OutDir=$outDir
}

if ($LASTEXITCODE -ne 0) {
    Write-Error "Build failed. Close open preview windows, or try running without -NoRestore if packages need to be restored."
    exit $LASTEXITCODE
}

if ($NoLaunch) {
    Write-Host "Build complete. Preview executable:"
    Write-Host $exe
    exit 0
}

Write-Host "Opening first-run setup preview..."
Start-Process -FilePath $exe -ArgumentList "--first-run-preview"
