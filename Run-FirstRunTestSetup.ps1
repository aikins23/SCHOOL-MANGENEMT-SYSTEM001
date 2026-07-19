param(
    [switch]$Reset,
    [switch]$KeepExistingTestDatabase,
    [switch]$NoLaunch,
    [switch]$NoRestore
)

$ErrorActionPreference = "Stop"

$root = Split-Path -Parent $MyInvocation.MyCommand.Path
$solution = Join-Path $root "Nyansapo ERP School Management Software.sln"

if ($Reset -and $KeepExistingTestDatabase) {
    Write-Error "Use either -Reset or -KeepExistingTestDatabase, not both."
    exit 1
}

Get-Process -Name "kingdom_Preparatory_School_Management_System" -ErrorAction SilentlyContinue |
    ForEach-Object {
        if ($_.MainWindowHandle -ne 0) {
            [void]$_.CloseMainWindow()
        }
    }

Start-Sleep -Seconds 2

$stamp = Get-Date -Format "yyyyMMdd_HHmmss"
$outDir = "bin\FirstRunTest_$stamp\"
$exe = Join-Path $root ($outDir + "kingdom_Preparatory_School_Management_System.exe")

Write-Host "Building first-run test setup into $outDir"
if ($NoRestore) {
    dotnet build $solution --no-restore -p:OutDir=$outDir
} else {
    dotnet build $solution -p:OutDir=$outDir
}

if ($LASTEXITCODE -ne 0) {
    Write-Error "Build failed."
    exit $LASTEXITCODE
}

$args = "--first-run-test-db"
if ($Reset) {
    $args += " --reset-first-run-test-db"
}

if ($NoLaunch) {
    Write-Host "Build complete. Test executable:"
    Write-Host $exe
    Write-Host "Arguments:"
    Write-Host $args
    exit 0
}

Write-Host "Opening real first-run setup against Nyansapo_FirstRun_Test..."
if ($Reset) {
    Write-Host "Reset requested: Nyansapo_FirstRun_Test will be recreated before opening."
} else {
    Write-Host "Existing test data will be kept. Use -Reset only when you want a fresh first-run test."
}
Start-Process -FilePath $exe -ArgumentList $args
