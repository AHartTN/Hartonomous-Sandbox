#Requires -Version 7.0
<#
.SYNOPSIS
    Deploys Hartonomous from Azure Pipelines CI/CD.

.DESCRIPTION
    Complete deployment orchestration for Azure DevOps Pipelines including:
    - Automatic environment detection
    - Azure authentication
    - Database deployment (HART-DESKTOP)
    - Application deployment (HART-SERVER)
    - Health checks and validation
    - Integration with Azure Monitor

.PARAMETER Environment
    Override environment detection (Local, Development, Staging, Production)

.PARAMETER SkipDatabase
    Skip database deployment

.PARAMETER SkipApplication
    Skip application deployment

.PARAMETER DryRun
    Simulate deployment without making changes

.EXAMPLE
    .\Deploy-AzurePipelines.ps1

    Deploy to environment detected from pipeline context

.EXAMPLE
    .\Deploy-AzurePipelines.ps1 -Environment Production

    Explicitly deploy to production

.NOTES
    Author: Hartonomous DevOps Team
    Version: 2.0.0
    Requires: Azure Pipelines agent with Azure CLI configured
#>

[CmdletBinding()]
param(
    [string]$Environment,
    [switch]$SkipDatabase,
    [switch]$SkipApplication,
    [switch]$DryRun
)

$ErrorActionPreference = "Stop"
$script:DeploymentStartTime = Get-Date

# Get script paths
$scriptRoot = if ($PSScriptRoot) { $PSScriptRoot } else { Split-Path -Parent $MyInvocation.MyCommand.Path }
$modulesRoot = Join-Path (Split-Path $scriptRoot -Parent) "modules"

# Import modules first (we need Environment module for Get-WorkspaceRoot)
Import-Module "$modulesRoot\Logger.psm1" -Force -ErrorAction Stop
Import-Module "$modulesRoot\Environment.psm1" -Force -ErrorAction Stop
Import-Module "$modulesRoot\Config.psm1" -Force -ErrorAction Stop
Import-Module "$modulesRoot\Secrets.psm1" -Force -ErrorAction Stop
Import-Module "$modulesRoot\Validation.psm1" -Force -ErrorAction Stop
Import-Module "$modulesRoot\Monitoring.psm1" -Force -ErrorAction Stop

# Now we can get repo root
$repoRoot = Get-WorkspaceRoot

# =============================================================================
# INITIALIZATION
# =============================================================================

# Verify running in Azure Pipelines
if (-not (Test-IsAzurePipelines)) {
    Write-Error "This script must be run from Azure Pipelines. Use Deploy-Local.ps1 for local deployments."
    exit 1
}

# Set log level
Set-LogLevel -Level "Info"

# Show header
Write-Host ""
Write-Host "╔════════════════════════════════════════════════════════════════╗" -ForegroundColor Cyan
Write-Host "║                                                                ║" -ForegroundColor Cyan
Write-Host "║        HARTONOMOUS AZURE PIPELINES DEPLOYMENT v2.0             ║" -ForegroundColor Cyan
Write-Host "║                                                                ║" -ForegroundColor Cyan
Write-Host "╚════════════════════════════════════════════════════════════════╝" -ForegroundColor Cyan
Write-Host ""

# Show environment info
Show-EnvironmentInfo

# Auto-detect or use explicit environment
if (-not $Environment) {
    $Environment = Get-DeploymentEnvironment
}

Write-Log "Deploying to environment: $Environment" -Level Info

# Load configuration
Write-Log "Loading configuration..." -Level Info
$config = Get-DeploymentConfig -Environment $Environment
Set-DeploymentContext -Environment $Environment -DeploymentId $env:BUILD_BUILDID

# Show configuration
Show-Configuration -Config $config

# Send deployment started event
Send-DeploymentEvent -EventName "DeploymentStarted" -Config $config -Properties @{
    BuildId = $env:BUILD_BUILDID
    BuildNumber = $env:BUILD_BUILDNUMBER
    RequestedFor = $env:BUILD_REQUESTEDFOR
}

# =============================================================================
# STEP 1: AUTHENTICATION
# =============================================================================

Write-LogSection "Azure Authentication" -Step 1 -TotalSteps 5

Measure-Operation -Name "Azure Login" -ScriptBlock {
    Write-Log "Authenticating to Azure..." -Level Info

    # Azure Pipelines typically uses service connection
    $account = az account show 2>$null | ConvertFrom-Json
    if ($account) {
        Write-Log "Authenticated as: $($account.user.name)" -Level Success
    } else {
        Write-Log "Azure authentication required" -Level Warning
    }
}

# =============================================================================
# STEP 2: PRE-FLIGHT VALIDATION
# =============================================================================

Write-LogSection "Pre-Flight Validation" -Step 2 -TotalSteps 5

Measure-Operation -Name "Pre-Flight Checks" -ScriptBlock {
    if (-not (Test-Prerequisites -Config $config)) {
        throw "Pre-flight checks failed"
    }
}

# =============================================================================
# STEP 3: DATABASE DEPLOYMENT
# =============================================================================

if (-not $SkipDatabase) {
    Write-LogSection "Deploying Database to HART-DESKTOP" -Step 3 -TotalSteps 5

    Measure-Operation -Name "Database Deployment" -ScriptBlock {
        $deployDbScript = Join-Path $repoRoot "scripts\Deploy-Database.ps1"

        & $deployDbScript `
            -Server $config.database.server `
            -Database $config.database.name `
            -AccessToken (az account get-access-token --resource https://database.windows.net/ --query accessToken -o tsv) `
            -DacpacPath "$env:PIPELINE_WORKSPACE\database\Hartonomous.Database.dacpac" `
            -DependenciesPath "$env:PIPELINE_WORKSPACE\dependencies" `
            -ScriptsPath "$repoRoot\scripts\sql"

        if ($LASTEXITCODE -ne 0) {
            throw "Database deployment failed"
        }
    }
} else {
    Write-Log "Skipping database deployment" -Level Warning
}

# =============================================================================
# STEP 4: APPLICATION DEPLOYMENT (IF ENABLED)
# =============================================================================

if ($config.application.deployEnabled -and -not $SkipApplication) {
    Write-LogSection "Deploying Application to HART-SERVER" -Step 4 -TotalSteps 5

    Measure-Operation -Name "Application Deployment" -ScriptBlock {
        $deployAppScript = Join-Path $repoRoot "scripts\deploy\deploy-to-hart-server.ps1"

        & $deployAppScript `
            -Server $config.application.deployTarget `
            -DeployRoot $config.application.deployPath `
            -SkipBuild

        if ($LASTEXITCODE -ne 0) {
            throw "Application deployment failed"
        }
    }
} else {
    Write-Log "Skipping application deployment" -Level Warning
}

# =============================================================================
# STEP 5: POST-DEPLOYMENT VALIDATION
# =============================================================================

Write-LogSection "Post-Deployment Validation" -Step 5 -TotalSteps 5

Measure-Operation -Name "Validate Deployment" -ScriptBlock {
    if (-not (Test-DeploymentSuccess -Config $config)) {
        throw "Post-deployment validation failed"
    }
}

# Show pipeline status
Show-AzurePipelineStatus

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
Write-Host "  Environment:      $Environment" -ForegroundColor White
Write-Host "  Database Server:  $($config.database.server)" -ForegroundColor White
Write-Host "  App Server:       $($config.application.deployTarget)" -ForegroundColor White
Write-Host "  Duration:         $($duration.ToString('mm\:ss'))" -ForegroundColor White
Write-Host "  Build ID:         $env:BUILD_BUILDID" -ForegroundColor White
Write-Host ""

# Send deployment completed event
Send-DeploymentEvent -EventName "DeploymentCompleted" -Config $config -Properties @{
    Success = $true
    DurationSeconds = $duration.TotalSeconds
    BuildId = $env:BUILD_BUILDID
}

exit 0
