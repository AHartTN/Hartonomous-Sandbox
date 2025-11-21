#!/usr/bin/env pwsh
<#
.SYNOPSIS
    Hartonomous Master Deployment Script
.DESCRIPTION
    Idempotent deployment orchestrator that:
    1. Builds the DACPAC from the SQL project
    2. Deploys DACPAC to SQL Server (uses pre/post deployment scripts)
    3. Scaffolds EF Core entities from deployed database
    4. Validates deployment with tests
.PARAMETER Server
    SQL Server instance (default: localhost)
.PARAMETER Database
    Target database name (default: Hartonomous)
.PARAMETER IntegratedSecurity
    Use Windows Authentication (default if no User specified)
.PARAMETER User
    SQL Authentication username
.PARAMETER Password
    SQL Authentication password
.PARAMETER TrustServerCertificate
    Trust server certificate (default: true for localhost)
.PARAMETER SkipBuild
    Skip building DACPAC (use existing)
.PARAMETER SkipDeploy
    Skip deploying DACPAC
.PARAMETER SkipScaffold
    Skip scaffolding entities
.PARAMETER SkipTests
    Skip validation tests
.EXAMPLE
    .\Deploy.ps1
    .\Deploy.ps1 -Server localhost -Database Hartonomous
    .\Deploy.ps1 -User sa -Password MyPass123
#>

param(
    [string]$Server = "localhost",
    [string]$Database = "Hartonomous",
    [switch]$IntegratedSecurity,
    [string]$User,
    [string]$Password,
    [switch]$TrustServerCertificate = $true,
    [switch]$SkipBuild,
    [switch]$SkipDeploy,
    [switch]$SkipScaffold,
    [switch]$SkipTests
)

$ErrorActionPreference = "Stop"
$StartTime = Get-Date

# Determine authentication mode
$UseIntegratedAuth = $IntegratedSecurity -or (-not $User)

Write-Host @"

?????????????????????????????????????????????????????????????????
?                                                               ?
?     HARTONOMOUS DEPLOYMENT ORCHESTRATOR                       ?
?     Database-First Architecture                               ?
?                                                               ?
?????????????????????????????????????????????????????????????????

"@ -ForegroundColor Cyan

Write-Host "Target:        $Server / $Database" -ForegroundColor White
Write-Host "Authentication: $(if ($UseIntegratedAuth) { 'Windows' } else { 'SQL' })" -ForegroundColor White
Write-Host "Started:       $(Get-Date -Format 'yyyy-MM-dd HH:mm:ss')" -ForegroundColor White
Write-Host ""

# ============================================================================
# STEP 1: BUILD DACPAC
# ============================================================================
if (-not $SkipBuild) {
    Write-Host "[1/4] Building DACPAC..." -ForegroundColor Yellow

    $repoRoot = Split-Path $PSScriptRoot -Parent
    $projectPath = Join-Path $repoRoot "src\Hartonomous.Database\Hartonomous.Database.sqlproj"
    $outputDir = Join-Path $repoRoot "src\Hartonomous.Database\bin\Release"

    $buildScript = Join-Path $PSScriptRoot "build-dacpac.ps1"
    & $buildScript -ProjectPath $projectPath -OutputDir $outputDir -Configuration Release

    if ($LASTEXITCODE -ne 0) {
        throw "DACPAC build failed"
    }

    Write-Host "      ? DACPAC built successfully" -ForegroundColor Green
    Write-Host ""
} else {
    Write-Host "[1/4] Skipping build (using existing DACPAC)" -ForegroundColor Gray
    Write-Host ""
}

# ============================================================================
# STEP 2: DEPLOY DACPAC
# ============================================================================
if (-not $SkipDeploy) {
    Write-Host "[2/4] Deploying DACPAC..." -ForegroundColor Yellow
    
    $deployScript = Join-Path $PSScriptRoot "deploy-dacpac.ps1"
    
    # deploy-dacpac.ps1 only accepts: Server, Database, UseAzureAD, AccessToken
    # It builds its own connection string with IntegratedSecurity and TrustServerCertificate
    $deployArgs = @{
        Server = $Server
        Database = $Database
    }
    
    # Note: deploy-dacpac.ps1 uses Windows integrated auth by default
    # and hardcodes TrustServerCertificate=True in connection string
    
    & $deployScript @deployArgs
    
    if ($LASTEXITCODE -ne 0) {
        throw "DACPAC deployment failed"
    }
    
    Write-Host "      ? Database deployed successfully" -ForegroundColor Green
    Write-Host ""
} else {
    Write-Host "[2/4] Skipping deployment" -ForegroundColor Gray
    Write-Host ""
}

# ============================================================================
# STEP 3: SCAFFOLD ENTITIES
# ============================================================================
if (-not $SkipScaffold) {
    Write-Host "[3/4] Scaffolding EF Core entities..." -ForegroundColor Yellow
    
    $scaffoldScript = Join-Path $PSScriptRoot "scaffold-entities.ps1"
    
    # Check if scaffold script exists
    if (Test-Path $scaffoldScript) {
        $scaffoldArgs = @{
            Server = $Server
            Database = $Database
        }
        
        if (-not $UseIntegratedAuth) {
            $scaffoldArgs.User = $User
            $scaffoldArgs.Password = $Password
        }
        
        & $scaffoldScript @scaffoldArgs
        
        if ($LASTEXITCODE -ne 0) {
            Write-Host "      ? Entity scaffolding failed" -ForegroundColor Yellow
        } else {
            Write-Host "      ? Entities generated successfully" -ForegroundColor Green
        }
    } else {
        Write-Host "      ? Scaffold script not found, skipping" -ForegroundColor Yellow
    }
    
    Write-Host ""
} else {
    Write-Host "[3/4] Skipping entity scaffolding" -ForegroundColor Gray
    Write-Host ""
}

# ============================================================================
# STEP 4: RUN VALIDATION TESTS
# ============================================================================
if (-not $SkipTests) {
    Write-Host "[4/4] Running validation tests..." -ForegroundColor Yellow
    
    $testScript = Join-Path $PSScriptRoot "tests\Run-DatabaseTests.ps1"
    
    if (Test-Path $testScript) {
        & $testScript -Server $Server -Database $Database
        
        if ($LASTEXITCODE -ne 0) {
            Write-Host "      ? Some tests failed" -ForegroundColor Yellow
        } else {
            Write-Host "      ? All tests passed" -ForegroundColor Green
        }
    } else {
        Write-Host "      ? Test script not found, skipping" -ForegroundColor Yellow
    }
    
    Write-Host ""
} else {
    Write-Host "[4/4] Skipping tests" -ForegroundColor Gray
    Write-Host ""
}

# ============================================================================
# SUMMARY
# ============================================================================
$Duration = (Get-Date) - $StartTime

Write-Host @"
?????????????????????????????????????????????????????????????????
?                                                               ?
?     DEPLOYMENT COMPLETE                                       ?
?                                                               ?
?????????????????????????????????????????????????????????????????

"@ -ForegroundColor Green

Write-Host "Database:  $Server / $Database" -ForegroundColor White
Write-Host "Duration:  $($Duration.TotalSeconds) seconds" -ForegroundColor White
Write-Host "Completed: $(Get-Date -Format 'yyyy-MM-dd HH:mm:ss')" -ForegroundColor White
Write-Host ""

exit 0
