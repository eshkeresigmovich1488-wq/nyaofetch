# install.ps1 - one-command installer for nyaofetch
#
# Usage (once the repo is on GitHub, run from any PowerShell):
#   irm https://raw.githubusercontent.com/neofetch-tech/nyaofetch/main/install.ps1 | iex
#
# What it does:
#   1. Clones the repo into %LOCALAPPDATA%\nyaofetch\src
#   2. Publishes a self-contained nyaofetch.exe with `dotnet publish`
#      (no separate .NET runtime install needed to run it)
#   3. Copies nyaofetch.exe into %LOCALAPPDATA%\nyaofetch\bin
#   4. Adds that bin folder to your user PATH (if not already there)
#
# After this, open a NEW terminal and just type: nyaofetch

$ErrorActionPreference = "Stop"

$RepoUrl    = "https://github.com/neofetch-tech/nyaofetch.git"
$InstallDir = "$env:LOCALAPPDATA\nyaofetch"
$SrcDir     = "$InstallDir\src"
$BinDir     = "$InstallDir\bin"

Write-Host "==> Installing nyaofetch into $InstallDir" -ForegroundColor Cyan

New-Item -ItemType Directory -Force -Path $BinDir | Out-Null

if (Test-Path $SrcDir) {
    Write-Host "==> Existing install found, pulling latest..." -ForegroundColor Cyan
    Push-Location $SrcDir
    git pull
    Pop-Location
} else {
    Write-Host "==> Cloning repo..." -ForegroundColor Cyan
    git clone $RepoUrl $SrcDir
    if ($LASTEXITCODE -ne 0) {
        Write-Error "git clone failed (exit code $LASTEXITCODE) - check the URL/your internet connection above."
        exit 1
    }
}

if (-not (Get-Command dotnet -ErrorAction SilentlyContinue)) {
    Write-Error "The .NET SDK is not installed. Get it from https://dotnet.microsoft.com/download and re-run this script."
    exit 1
}

Write-Host "==> Publishing (this can take a minute)..." -ForegroundColor Cyan
Push-Location $SrcDir
dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -o "$SrcDir\publish"
Pop-Location

$ExePath = Join-Path "$SrcDir\publish" "nyaofetch.exe"
if (-not (Test-Path $ExePath)) {
    Write-Error "Build finished but nyaofetch.exe was not found at $ExePath - check the build output above."
    exit 1
}

Copy-Item $ExePath "$BinDir\nyaofetch.exe" -Force
Copy-Item "$SrcDir\assets" "$BinDir\assets" -Recurse -Force -ErrorAction SilentlyContinue

# Add BinDir to the user PATH if it's not already there.
$currentPath = [Environment]::GetEnvironmentVariable("Path", "User")
if ($currentPath -notlike "*$BinDir*") {
    [Environment]::SetEnvironmentVariable("Path", "$currentPath;$BinDir", "User")
    Write-Host "==> Added $BinDir to your PATH." -ForegroundColor Green
    Write-Host "==> Open a NEW terminal window, then just run: nyaofetch" -ForegroundColor Green
} else {
    Write-Host "==> Already on PATH. Just run: nyaofetch" -ForegroundColor Green
}
