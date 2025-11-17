#!/usr/bin/env pwsh
<#
.SYNOPSIS
    Builds Hartonomous DACPAC
.DESCRIPTION
    Compiles the SQL Server Database Project to create a DACPAC artifact.
    Idempotent - checks if rebuild is needed based on source file timestamps.
#>

$ErrorActionPreference = "Stop"

$ProjectPath = "src\Hartonomous.Database\Hartonomous.Database.sqlproj"
$OutputDir = "src\Hartonomous.Database\bin\Output"
$DacpacPath = Join-Path $OutputDir "Hartonomous.Database.dacpac"

# Check if DACPAC exists and is up to date
$needsBuild = $false

if (-not (Test-Path $DacpacPath)) {
    Write-Host "DACPAC does not exist, build required" -ForegroundColor Yellow
    $needsBuild = $true
} else {
    $dacpac = Get-Item $DacpacPath
    $projectFile = Get-Item $ProjectPath
    
    # Check if project file is newer than DACPAC
    if ($projectFile.LastWriteTime -gt $dacpac.LastWriteTime) {
        Write-Host "Project file modified since last build, rebuild required" -ForegroundColor Yellow
        $needsBuild = $true
    }
    
    # Check if any SQL files are newer than DACPAC
    $sqlFiles = Get-ChildItem -Path "src\Hartonomous.Database" -Include "*.sql" -Recurse
    $newerFiles = $sqlFiles | Where-Object { $_.LastWriteTime -gt $dacpac.LastWriteTime }
    
    if ($newerFiles.Count -gt 0) {
        Write-Host "$($newerFiles.Count) SQL files modified since last build, rebuild required" -ForegroundColor Yellow
        $needsBuild = $true
    }
}

if (-not $needsBuild) {
    $dacpac = Get-Item $DacpacPath
    $sizeKB = [math]::Round($dacpac.Length / 1KB, 2)
    Write-Host "DACPAC is up to date ($sizeKB KB)" -ForegroundColor Green
    Write-Host "  Path: $DacpacPath" -ForegroundColor Gray
    exit 0
}

# Find MSBuild
$msbuildPath = $null
$msbuildPaths = @(
    "${env:ProgramFiles}\Microsoft Visual Studio\18\Insiders\MSBuild\Current\Bin\MSBuild.exe",
    "${env:ProgramFiles}\Microsoft Visual Studio\2022\Enterprise\MSBuild\Current\Bin\MSBuild.exe",
    "${env:ProgramFiles}\Microsoft Visual Studio\2022\Professional\MSBuild\Current\Bin\MSBuild.exe",
    "${env:ProgramFiles}\Microsoft Visual Studio\2022\Community\MSBuild\Current\Bin\MSBuild.exe"
)

foreach ($path in $msbuildPaths) {
    if (Test-Path $path) {
        $msbuildPath = $path
        break
    }
}

if (-not $msbuildPath) {
    throw "MSBuild not found. Install Visual Studio 2022 with SQL Server Data Tools (SSDT)."
}

# Build DACPAC
Write-Host "Building DACPAC..." -ForegroundColor Cyan
Write-Host "  Project: $ProjectPath" -ForegroundColor Gray
Write-Host "  MSBuild: $msbuildPath" -ForegroundColor Gray

& $msbuildPath $ProjectPath `
    /t:Build `
    /p:Configuration=Release `
    /p:OutDir=$OutputDir `
    /v:minimal `
    /nologo

if ($LASTEXITCODE -ne 0) {
    throw "DACPAC build failed with exit code $LASTEXITCODE"
}

# Verify DACPAC exists (check both locations)
if (Test-Path $AltDacpacPath) {
    $DacpacPath = $AltDacpacPath
} elseif (-not (Test-Path $DacpacPath)) {
    throw "DACPAC not found at expected paths"
}

$dacpac = Get-Item $DacpacPath
$sizeKB = [math]::Round($dacpac.Length / 1KB, 2)

Write-Host "  Build complete: $sizeKB KB" -ForegroundColor Green
Write-Host "  Path: $DacpacPath" -ForegroundColor Green

exit 0
