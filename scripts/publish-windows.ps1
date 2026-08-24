# Publishes a self-contained, single-file Release build of the AGC Launcher client
# and, if Inno Setup is installed, compiles it into a Windows installer.
#
# Usage:  powershell -File scripts\publish-windows.ps1

$ErrorActionPreference = "Stop"
$root = Split-Path -Parent $PSScriptRoot
$publishDir = Join-Path $root "publish\win-x64"

Write-Host "Publishing AGC.Launcher (Release, win-x64, self-contained, single-file)..." -ForegroundColor Cyan
dotnet publish (Join-Path $root "src\AGC.Launcher\AGC.Launcher.csproj") `
    -c Release -r win-x64 --self-contained true `
    -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true `
    -o $publishDir

if ($LASTEXITCODE -ne 0) {
    throw "dotnet publish failed."
}

Write-Host "Published to $publishDir" -ForegroundColor Green

$iscc = Get-Command "ISCC.exe" -ErrorAction SilentlyContinue
if (-not $iscc) {
    $defaultIscc = "C:\Program Files (x86)\Inno Setup 6\ISCC.exe"
    if (Test-Path $defaultIscc) {
        $iscc = $defaultIscc
    }
}

if ($iscc) {
    Write-Host "Compiling installer with Inno Setup..." -ForegroundColor Cyan
    & $iscc (Join-Path $root "installer\AGCLauncher.iss")
    if ($LASTEXITCODE -ne 0) {
        throw "ISCC.exe failed."
    }
    Write-Host "Installer written to installer\Output\AGC-Launcher-Setup.exe" -ForegroundColor Green
} else {
    Write-Warning "Inno Setup (ISCC.exe) not found. Install it from https://jrsoftware.org/isinfo.php, then re-run this script to also build the Setup.exe. The published app is ready at $publishDir in the meantime."
}
