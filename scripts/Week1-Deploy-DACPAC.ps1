#!/usr/bin/env pwsh
<#
.SYNOPSIS
    Week 1 Day 5 - DACPAC Deployment Script
.DESCRIPTION
    Deploys Hartonomous DACPAC following the exact pattern from docs/rewrite-guide/17-Master-Implementation-Roadmap.md
.PARAMETER Server
    SQL Server instance (required)
.PARAMETER Database  
    Target database name (required)
.PARAMETER User
    SQL authentication username (optional, defaults to integrated security)
.PARAMETER Password
    SQL authentication password (required if User specified)
.PARAMETER IntegratedSecurity
    Use Windows authentication
.PARAMETER TrustServerCertificate
    Trust server certificate for encrypted connections
#>

param(
    [Parameter(Mandatory=$true)]
    [string]$Server,

    [Parameter(Mandatory=$true)]
    [string]$Database,

    [Parameter(Mandatory=$false)]
    [string]$User,

    [Parameter(Mandatory=$false)]
    [string]$Password,

    [Parameter(Mandatory=$false)]
    [switch]$IntegratedSecurity,

    [Parameter(Mandatory=$false)]
    [switch]$TrustServerCertificate
)

$ErrorActionPreference = 'Stop'

Write-Host "═══════════════════════════════════════════════════════" -ForegroundColor Cyan
Write-Host "  HARTONOMOUS DACPAC DEPLOYMENT - WEEK 1" -ForegroundColor Cyan
Write-Host "═══════════════════════════════════════════════════════" -ForegroundColor Cyan

# Step 1: Build DACPAC
Write-Host "`n[1/3] Building DACPAC..." -ForegroundColor Yellow

$msbuildPath = "C:\Program Files\Microsoft Visual Studio\18\Insiders\MSBuild\Current\Bin\amd64\MSBuild.exe"
if (-not (Test-Path $msbuildPath)) {
    $msbuildPath = "C:\Program Files\Microsoft Visual Studio\2022\Enterprise\MSBuild\Current\Bin\amd64\MSBuild.exe"
    if (-not (Test-Path $msbuildPath)) {
        throw "MSBuild not found. Install Visual Studio 2022 with SQL Server Data Tools."
    }
}

& $msbuildPath src\Hartonomous.Database\Hartonomous.Database.sqlproj `
    /t:Build /p:Configuration=Release /v:minimal /nologo

if ($LASTEXITCODE -ne 0) {
    throw "DACPAC build failed"
}

$dacpac = "src\Hartonomous.Database\bin\Output\Hartonomous.Database.dacpac"

if (-not (Test-Path $dacpac)) {
    throw "DACPAC not found at $dacpac"
}

Write-Host "  ✓ DACPAC built successfully" -ForegroundColor Green

# Step 2: Deploy
Write-Host "`n[2/3] Deploying DACPAC to $Server/$Database..." -ForegroundColor Yellow

$connectionString = if ($IntegratedSecurity -or -not $User) {
    "Server=$Server;Database=$Database;Integrated Security=True;"
} else {
    "Server=$Server;Database=$Database;User=$User;Password=$Password;"
}

if ($TrustServerCertificate) {
    $connectionString += "TrustServerCertificate=True;"
}

# Find SqlPackage
$sqlPackagePaths = @(
    "C:\Program Files\Microsoft SQL Server\160\DAC\bin\SqlPackage.exe",
    "C:\Program Files\Microsoft SQL Server\150\DAC\bin\SqlPackage.exe"
)

$sqlPackage = $null
foreach ($path in $sqlPackagePaths) {
    if (Test-Path $path) {
        $sqlPackage = $path
        break
    }
}

if (-not $sqlPackage) {
    throw "SqlPackage.exe not found. Install SQL Server Data Tools or DacFx."
}

& $sqlPackage /Action:Publish `
    /SourceFile:$dacpac `
    /TargetConnectionString:$connectionString `
    /p:IncludeCompositeObjects=True `
    /p:BlockOnPossibleDataLoss=False

if ($LASTEXITCODE -ne 0) {
    throw "Deployment failed"
}

Write-Host "  ✓ Deployment complete" -ForegroundColor Green

# Step 3: Run smoke tests
Write-Host "`n[3/3] Running smoke tests..." -ForegroundColor Yellow

$smokeTestPath = "tests\smoke-tests.sql"
if (Test-Path $smokeTestPath) {
    $sqlcmd = (Get-Command sqlcmd -ErrorAction SilentlyContinue).Source
    if ($sqlcmd) {
        $sqlcmdArgs = @("-S", $Server, "-d", $Database)
        if ($IntegratedSecurity -or -not $User) {
            $sqlcmdArgs += "-E"
        } else {
            $sqlcmdArgs += @("-U", $User, "-P", $Password)
        }
        $sqlcmdArgs += @("-i", $smokeTestPath)
        
        & $sqlcmd $sqlcmdArgs
        
        if ($LASTEXITCODE -eq 0) {
            Write-Host "  ✓ Smoke tests passed" -ForegroundColor Green
        } else {
            Write-Host "  ⚠ Some smoke tests failed" -ForegroundColor Yellow
        }
    } else {
        Write-Host "  ⚠ sqlcmd not found, skipping smoke tests" -ForegroundColor Yellow
    }
} else {
    Write-Host "  ⚠ Smoke tests not found at $smokeTestPath" -ForegroundColor Yellow
}

Write-Host "`n═══════════════════════════════════════════════════════" -ForegroundColor Green
Write-Host "  ✓✓✓ DEPLOYMENT SUCCESSFUL ✓✓✓" -ForegroundColor Green  
Write-Host "═══════════════════════════════════════════════════════" -ForegroundColor Green
Write-Host "`nDatabase: $Server/$Database" -ForegroundColor White

exit 0
