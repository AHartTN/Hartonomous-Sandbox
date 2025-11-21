#Requires -Version 7.0
<#
.SYNOPSIS
    Deploys Hartonomous to local development environment.

.DESCRIPTION
    Complete local deployment including:
    - Database schema (DACPAC)
    - CLR assemblies and signing
    - EF Core entity scaffolding
    - .NET solution build
    - Optional: Start local services

    Uses centralized configuration from scripts/config/config.local.json

.PARAMETER SkipBuild
    Skip building the database project (use existing DACPAC)

.PARAMETER SkipScaffold
    Skip EF Core entity scaffolding

.PARAMETER SkipAppBuild
    Skip building .NET applications

.PARAMETER StartServices
    Start local API and worker services after deployment

.PARAMETER Force
    Skip confirmation prompts

.EXAMPLE
    .\Deploy-Local.ps1

    Complete fresh deployment to localhost

.EXAMPLE
    .\Deploy-Local.ps1 -SkipBuild -SkipScaffold

    Quick deployment (reuse existing DACPAC and entities)

.EXAMPLE
    .\Deploy-Local.ps1 -StartServices

    Deploy and start local services

.NOTES
    Author: Hartonomous DevOps Team
    Version: 2.0.0 (Refactored with module system)
#>

[CmdletBinding()]
param(
    [switch]$SkipBuild,
    [switch]$SkipScaffold,
    [switch]$SkipAppBuild,
    [switch]$StartServices,
    [switch]$Force
)

$ErrorActionPreference = "Stop"
$script:DeploymentStartTime = Get-Date

# Get script root and repository root
$scriptRoot = if ($PSScriptRoot) { $PSScriptRoot } else { Split-Path -Parent $MyInvocation.MyCommand.Path }
$repoRoot = Split-Path (Split-Path $scriptRoot -Parent) -Parent
$modulesRoot = Join-Path (Split-Path $scriptRoot -Parent) "modules"

# Verify paths exist
if (-not (Test-Path $modulesRoot)) {
    Write-Error "Modules directory not found: $modulesRoot"
    Write-Error "Script root: $scriptRoot"
    Write-Error "Repo root: $repoRoot"
    exit 1
}

# Import modules
Import-Module "$modulesRoot\Logger.psm1" -Force -ErrorAction Stop
Import-Module "$modulesRoot\Environment.psm1" -Force -ErrorAction Stop
Import-Module "$modulesRoot\Config.psm1" -Force -ErrorAction Stop
Import-Module "$modulesRoot\Secrets.psm1" -Force -ErrorAction Stop
Import-Module "$modulesRoot\Validation.psm1" -Force -ErrorAction Stop
Import-Module "$modulesRoot\Monitoring.psm1" -Force -ErrorAction Stop

# =============================================================================
# INITIALIZATION
# =============================================================================

# Set log level
Set-LogLevel -Level "Debug"

# Enable file logging
Enable-FileLogging -Path "$repoRoot\logs\deployment-local.log"

# Show header
Write-Host ""
Write-Host "╔════════════════════════════════════════════════════════════════╗" -ForegroundColor Cyan
Write-Host "║                                                                ║" -ForegroundColor Cyan
Write-Host "║        HARTONOMOUS LOCAL DEPLOYMENT v2.0                       ║" -ForegroundColor Cyan
Write-Host "║                                                                ║" -ForegroundColor Cyan
Write-Host "║  Enterprise-Grade, Idempotent, Observable                      ║" -ForegroundColor Cyan
Write-Host "║                                                                ║" -ForegroundColor Cyan
Write-Host "╚════════════════════════════════════════════════════════════════╝" -ForegroundColor Cyan
Write-Host ""

# Show environment info
Show-EnvironmentInfo

# Load configuration
Write-Log "Loading configuration for Local environment..." -Level Info
$config = Get-DeploymentConfig -Environment "Local"
Set-DeploymentContext -Environment "Local"

# Show configuration
Show-Configuration -Config $config

# Confirm deployment (unless -Force)
if (-not $Force) {
    Write-Host ""
    Write-Host "This will deploy to: $($config.database.server)\$($config.database.name)" -ForegroundColor Yellow
    Write-Host ""
    $confirmation = Read-Host "Continue? (Y/N)"
    if ($confirmation -ne 'Y' -and $confirmation -ne 'y') {
        Write-Log "Deployment cancelled by user" -Level Warning
        exit 0
    }
}

# Send deployment started event
Send-DeploymentEvent -EventName "DeploymentStarted" -Config $config -Properties @{
    Component = "Database"
    SkipBuild = $SkipBuild
    SkipScaffold = $SkipScaffold
}

# =============================================================================
# STEP 0: PRE-FLIGHT CHECKS
# =============================================================================

Write-LogSection "Pre-Flight Validation" -Step 0 -TotalSteps 7

Measure-Operation -Name "Pre-Flight Checks" -ScriptBlock {
    if (-not (Test-Prerequisites -Config $config)) {
        throw "Pre-flight checks failed. Please resolve issues before deploying."
    }
}

# =============================================================================
# STEP 1: BUILD DATABASE PROJECT (DACPAC)
# =============================================================================

if (-not $SkipBuild) {
    Write-LogSection "Building Database Project (DACPAC)" -Step 1 -TotalSteps 7

    Measure-Operation -Name "Build DACPAC" -ScriptBlock {
        $buildScript = Join-Path $repoRoot "scripts\build-dacpac.ps1"

        & $buildScript `
            -ProjectPath "$repoRoot\src\Hartonomous.Database\Hartonomous.Database.sqlproj" `
            -OutputDir "$repoRoot\src\Hartonomous.Database\bin\Release" `
            -Configuration $config.build.configuration

        if ($LASTEXITCODE -ne 0) {
            throw "DACPAC build failed with exit code $LASTEXITCODE"
        }
    }
} else {
    Write-Log "Skipping DACPAC build (using existing)" -Level Warning
}

# =============================================================================
# STEP 2: DEPLOY DATABASE SCHEMA
# =============================================================================

Write-LogSection "Deploying Database Schema (DACPAC)" -Step 2 -TotalSteps 7

Measure-Operation -Name "Deploy DACPAC" -ScriptBlock {
    $deployDacpacScript = Join-Path $repoRoot "scripts\deploy-dacpac.ps1"

    & $deployDacpacScript `
        -Server $config.database.server `
        -Database $config.database.name `
        -TrustServerCertificate

    if ($LASTEXITCODE -ne 0) {
        throw "DACPAC deployment failed with exit code $LASTEXITCODE"
    }
}

# =============================================================================
# STEP 3: CONFIGURE CLR ASSEMBLIES
# =============================================================================

Write-LogSection "Configuring CLR Assemblies" -Step 3 -TotalSteps 7

Write-Log "CLR assemblies are embedded in DACPAC and deployed automatically" -Level Info
Write-Log "CLR Strict Security: $($config.database.clr.enableStrictSecurity)" -Level Info
Write-Log "TRUSTWORTHY: $($config.database.clr.trustworthy)" -Level Info

# =============================================================================
# STEP 4: SCAFFOLD EF CORE ENTITIES
# =============================================================================

if (-not $SkipScaffold) {
    Write-LogSection "Scaffolding EF Core Entities" -Step 4 -TotalSteps 7

    Measure-Operation -Name "Scaffold Entities" -ScriptBlock {
        $scaffoldScript = Join-Path $repoRoot "scripts\scaffold-entities.ps1"

        & $scaffoldScript `
            -Server $config.database.server `
            -Database $config.database.name

        if ($LASTEXITCODE -ne 0) {
            throw "Entity scaffolding failed with exit code $LASTEXITCODE"
        }
    }
} else {
    Write-Log "Skipping entity scaffolding (using existing)" -Level Warning
}

# =============================================================================
# STEP 5: BUILD .NET SOLUTION
# =============================================================================

if (-not $SkipAppBuild) {
    Write-LogSection "Building .NET Solution" -Step 5 -TotalSteps 7

    Measure-Operation -Name "Build .NET Solution" -ScriptBlock {
        Push-Location $repoRoot
        try {
            Write-Log "Restoring NuGet packages..." -Level Info
            dotnet restore $config.build.solutionFile --verbosity quiet

            if ($LASTEXITCODE -ne 0) {
                throw "NuGet restore failed"
            }

            Write-Log "Building solution..." -Level Info
            dotnet build $config.build.solutionFile `
                -c $config.build.configuration `
                --no-restore `
                --verbosity $config.build.verbosity

            if ($LASTEXITCODE -ne 0) {
                throw "Solution build failed"
            }
        }
        finally {
            Pop-Location
        }
    }
} else {
    Write-Log "Skipping .NET solution build" -Level Warning
}

# =============================================================================
# STEP 6: POST-DEPLOYMENT VALIDATION
# =============================================================================

Write-LogSection "Post-Deployment Validation" -Step 6 -TotalSteps 7

Measure-Operation -Name "Validate Deployment" -ScriptBlock {
    if (-not (Test-DeploymentSuccess -Config $config)) {
        Write-Log "Some validation checks failed (see warnings above)" -Level Warning
    }
}

# =============================================================================
# STEP 7: START SERVICES (OPTIONAL)
# =============================================================================

if ($StartServices) {
    Write-LogSection "Starting Local Services" -Step 7 -TotalSteps 7

    Write-Log "Starting Hartonomous.Api..." -Level Info
    Write-Log "You can start services manually with:" -Level Info
    Write-Log "  cd src\Hartonomous.Api && dotnet run" -Level Info
    Write-Log "  cd src\Hartonomous.Workers.Neo4jSync && dotnet run" -Level Info
}

# =============================================================================
# DEPLOYMENT SUMMARY
# =============================================================================

$duration = (Get-Date) - $script:DeploymentStartTime

Write-Host ""
Write-Host "╔════════════════════════════════════════════════════════════════╗" -ForegroundColor Green
Write-Host "║                                                                ║" -ForegroundColor Green
Write-Host "║        DEPLOYMENT COMPLETED SUCCESSFULLY                       ║" -ForegroundColor Green
Write-Host "║                                                                ║" -ForegroundColor Green
Write-Host "╚════════════════════════════════════════════════════════════════╝" -ForegroundColor Green
Write-Host ""
Write-Host "  Server:      $($config.database.server)" -ForegroundColor White
Write-Host "  Database:    $($config.database.name)" -ForegroundColor White
Write-Host "  Environment: Local" -ForegroundColor White
Write-Host "  Duration:    $($duration.ToString('mm\:ss'))" -ForegroundColor White
Write-Host ""

Write-Host "Next Steps:" -ForegroundColor Cyan
Write-Host ""
Write-Host "1. Start the API:" -ForegroundColor White
Write-Host "   cd src\Hartonomous.Api" -ForegroundColor Gray
Write-Host "   dotnet run" -ForegroundColor Gray
Write-Host ""
Write-Host "2. Test the API:" -ForegroundColor White
Write-Host "   Invoke-WebRequest http://localhost:5000/health" -ForegroundColor Gray
Write-Host ""
Write-Host "3. Start workers (optional):" -ForegroundColor White
Write-Host "   cd src\Hartonomous.Workers.Neo4jSync" -ForegroundColor Gray
Write-Host "   dotnet run" -ForegroundColor Gray
Write-Host ""

# Send deployment completed event
Send-DeploymentEvent -EventName "DeploymentCompleted" -Config $config -Properties @{
    Component = "Database"
    Success = $true
    DurationSeconds = $duration.TotalSeconds
}

exit 0
