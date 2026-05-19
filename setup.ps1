# =============================================================
# setup.ps1 - LlamaLib Dependency Installer
# Run this once after cloning, or when LlamaLib version changes
# Usage: Right-click -> "Run with PowerShell"
#        OR from terminal: .\setup.ps1
# =============================================================

$ErrorActionPreference = "Stop"

# --- Config ---
$version     = "v2.0.5"
$zipName     = "LlamaLib-v2.0.5-win-x64.zip"  # verify this matches the release page
$downloadUrl = "https://github.com/undreamai/LlamaLib/releases/download/$version/$zipName"
$scriptDir   = Split-Path -Parent $MyInvocation.MyCommand.Path
$destFolder  = Join-Path $scriptDir "Doodle drifters 2\Assets\StreamingAssets\LlamaLib-$version\win-x64\native"
$tempZip     = Join-Path $env:TEMP "LlamaLib-$version.zip"
$tempExtract = Join-Path $env:TEMP "LlamaLib-extract"

# --- Check if already installed ---
if (Test-Path $destFolder) {
    Write-Host "LlamaLib $version already installed at:" -ForegroundColor Green
    Write-Host $destFolder
    Write-Host ""
    Write-Host "Delete that folder and re-run if you need a fresh install."
    Read-Host "Press Enter to exit"
    exit 0
}

# --- Download ---
Write-Host "========================================" -ForegroundColor Cyan
Write-Host " LlamaLib Installer" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""
Write-Host "Downloading LlamaLib $version (~1.7 GB)..." -ForegroundColor Yellow
Write-Host "From: $downloadUrl"
Write-Host ""

$webClient = New-Object System.Net.WebClient

# Show download progress
$webClient.DownloadProgressChanged += {
    param($s, $e)
    $mb = [math]::Round($e.BytesReceived / 1MB, 1)
    $totalMb = [math]::Round($e.TotalBytesToReceive / 1MB, 1)
    Write-Progress -Activity "Downloading LlamaLib" `
        -Status "$mb MB / $totalMb MB" `
        -PercentComplete $e.ProgressPercentage
}

$webClient.DownloadFileTaskAsync($downloadUrl, $tempZip).GetAwaiter().GetResult()
Write-Progress -Activity "Downloading LlamaLib" -Completed
Write-Host "Download complete." -ForegroundColor Green

# --- Extract ---
Write-Host ""
Write-Host "Extracting..." -ForegroundColor Yellow

if (Test-Path $tempExtract) { Remove-Item $tempExtract -Recurse -Force }
Expand-Archive -Path $tempZip -DestinationPath $tempExtract -Force

Write-Host "Extraction complete." -ForegroundColor Green

# --- Copy to destination ---
Write-Host ""
Write-Host "Installing to:" -ForegroundColor Yellow
Write-Host $destFolder
Write-Host ""

New-Item -ItemType Directory -Force -Path $destFolder | Out-Null
Copy-Item "$tempExtract\*" -Destination $destFolder -Recurse -Force

# --- Cleanup ---
Remove-Item $tempZip -Force
Remove-Item $tempExtract -Recurse -Force

Write-Host ""
Write-Host "========================================" -ForegroundColor Green
Write-Host " LlamaLib $version installed successfully!" -ForegroundColor Green
Write-Host "========================================" -ForegroundColor Green
Write-Host ""
Read-Host "Press Enter to exit"