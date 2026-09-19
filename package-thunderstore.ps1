<#
.SYNOPSIS
    Packages ExpandedPlayerInventory into a Thunderstore-ready zip package.
.DESCRIPTION
    Builds the Release configuration, validates required files (manifest.json, README.md, icon.png, dll),
    and creates a zip archive in the 'dist' folder ready for upload to Thunderstore.
#>

param(
    [switch]$NoBuild = $false
)

$ErrorActionPreference = "Stop"

$repoRoot = $PSScriptRoot
Set-Location $repoRoot

Write-Host "========================================" -ForegroundColor Cyan
Write-Host " Packaging for Thunderstore" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan

# 1. Build project in Release mode unless -NoBuild is specified
if (-not $NoBuild) {
    Write-Host "`n[1/4] Building project (Release mode)..." -ForegroundColor Yellow
    dotnet build -c Release
    if ($LASTEXITCODE -ne 0) {
        Write-Error "Build failed! Please resolve build errors before packaging."
    }
} else {
    Write-Host "`n[1/4] Skipping build (-NoBuild specified)..." -ForegroundColor Gray
}

# 2. Validate manifest.json
Write-Host "`n[2/4] Validating files..." -ForegroundColor Yellow

$manifestPath = Join-Path $repoRoot "manifest.json"
if (-not (Test-Path $manifestPath)) {
    Write-Error "manifest.json not found in repository root!"
}

$manifest = Get-Content $manifestPath -Raw | ConvertFrom-Json
$version = $manifest.version_number
$modName = $manifest.name

if ([string]::IsNullOrWhiteSpace($version)) {
    Write-Error "manifest.json must specify 'version_number'!"
}

Write-Host "  -> Mod: $modName (v$version)" -ForegroundColor Green

# 3. Validate required files
$dllPath = Join-Path $repoRoot "bin\Release\ExpandedPlayerInventory.dll"
$readmePath = Join-Path $repoRoot "README.md"
$iconPath = Join-Path $repoRoot "icon.png"

if (-not (Test-Path $dllPath)) {
    Write-Error "Compiled DLL not found at: $dllPath"
}
if (-not (Test-Path $readmePath)) {
    Write-Error "README.md not found!"
}
if (-not (Test-Path $iconPath)) {
    Write-Error "icon.png not found! Thunderstore requires a 256x256 icon.png."
}

# Check icon file size
$iconSize = (Get-Item $iconPath).Length
if ($iconSize -gt 262144) {
    Write-Warning "icon.png exceeds 256KB ($([math]::Round($iconSize/1KB, 1)) KB). Thunderstore may reject it."
}

# 4. Prepare staging directory
Write-Host "`n[3/4] Preparing package staging directory..." -ForegroundColor Yellow

$distDir = Join-Path $repoRoot "dist"
$stagingDir = Join-Path $distDir "staging"

if (Test-Path $stagingDir) {
    Remove-Item -Path $stagingDir -Recurse -Force
}

$null = New-Item -ItemType Directory -Path $stagingDir -Force

# Helper to write UTF-8 without BOM
$utf8NoBom = New-Object System.Text.UTF8Encoding($false)

# Copy files (ensure text files are saved as UTF-8 without BOM)
Copy-Item -Path $dllPath -Destination (Join-Path $stagingDir "ExpandedPlayerInventory.dll") -Force
[System.IO.File]::WriteAllText((Join-Path $stagingDir "manifest.json"), [System.IO.File]::ReadAllText($manifestPath), $utf8NoBom)
[System.IO.File]::WriteAllText((Join-Path $stagingDir "README.md"), [System.IO.File]::ReadAllText($readmePath), $utf8NoBom)
Copy-Item -Path $iconPath -Destination (Join-Path $stagingDir "icon.png") -Force

$changelogPath = Join-Path $repoRoot "CHANGELOG.md"
if (Test-Path $changelogPath) {
    [System.IO.File]::WriteAllText((Join-Path $stagingDir "CHANGELOG.md"), [System.IO.File]::ReadAllText($changelogPath), $utf8NoBom)
}

$licensePath = Join-Path $repoRoot "LICENSE"
if (Test-Path $licensePath) {
    [System.IO.File]::WriteAllText((Join-Path $stagingDir "LICENSE"), [System.IO.File]::ReadAllText($licensePath), $utf8NoBom)
}

# 5. Create zip archive
Write-Host "`n[4/4] Creating zip archive..." -ForegroundColor Yellow

$zipName = "$($modName)_v$($version).zip"
$zipPath = Join-Path $distDir $zipName

if (Test-Path $zipPath) {
    Remove-Item -Path $zipPath -Force
}

Compress-Archive -Path "$stagingDir\*" -DestinationPath $zipPath -Force

# Cleanup staging
Remove-Item -Path $stagingDir -Recurse -Force

Write-Host "`n========================================" -ForegroundColor Green
Write-Host " Package created successfully!" -ForegroundColor Green
Write-Host "========================================" -ForegroundColor Green
Write-Host "File: $zipPath" -ForegroundColor Cyan
Write-Host "Size: $([math]::Round((Get-Item $zipPath).Length / 1KB, 2)) KB" -ForegroundColor Gray
Write-Host "`nYou can now upload this zip file directly to Thunderstore!" -ForegroundColor White
