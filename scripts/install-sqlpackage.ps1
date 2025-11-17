#Requires -Version 7.0
param()

$ErrorActionPreference = 'Stop'

# Check if SqlPackage is already installed
if (Get-Command sqlpackage -ErrorAction SilentlyContinue) {
    $version = & sqlpackage /version 2>&1 | Select-Object -First 1
    Write-Host "SqlPackage already installed: $version"
    exit 0
}

Write-Host "Installing SqlPackage..."

# Determine OS and download appropriate version
if ($IsLinux) {
    $url = "https://aka.ms/sqlpackage-linux"
    $fileName = "sqlpackage-linux.zip"
} elseif ($IsMacOS) {
    $url = "https://aka.ms/sqlpackage-macos"
    $fileName = "sqlpackage-macos.zip"
} else {
    $url = "https://aka.ms/sqlpackage-windows"
    $fileName = "sqlpackage-windows.zip"
}

# Use system temp directory
$tempDir = [System.IO.Path]::GetTempPath()
$zipPath = Join-Path $tempDir $fileName
$installDir = Join-Path $tempDir "sqlpackage"

Write-Host "Downloading SqlPackage from $url"
Invoke-WebRequest -Uri $url -OutFile $zipPath -UseBasicParsing

Write-Host "Extracting to $installDir"
if (Test-Path $installDir) {
    Remove-Item $installDir -Recurse -Force
}
New-Item -ItemType Directory -Path $installDir -Force | Out-Null

Expand-Archive -Path $zipPath -DestinationPath $installDir -Force

# Make executable on Linux/Mac
if ($IsLinux -or $IsMacOS) {
    chmod +x "$installDir/sqlpackage"
}

# Add to PATH for this session
$env:PATH = "$installDir$([System.IO.Path]::PathSeparator)$env:PATH"
Write-Host "##vso[task.prependpath]$installDir"

# Verify installation
$version = & "$installDir/sqlpackage" /version 2>&1 | Select-Object -First 1
Write-Host "✓ SqlPackage installed successfully: $version"

# Cleanup
Remove-Item $zipPath -Force -ErrorAction SilentlyContinue
