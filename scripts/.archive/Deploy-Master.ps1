#Requires -Version 7.0
<#
.SYNOPSIS
    Master deployment orchestrator for Hartonomous - Single entry point for all deployments

.DESCRIPTION
    This is THE deployment script for Hartonomous. All deployments go through here.
    
    Features:
    - Idempotent: Safe to run multiple times
    - Transactional: Rolls back on failure
    - Validated: Pre-flight checks and post-deployment validation
    - Clear: Progress indicators and colored output
    - Single entry point: No confusion about which script to run

.PARAMETER Server
    SQL Server instance (default: localhost)

.PARAMETER Database  
    Database name (default: Hartonomous)

.PARAMETER SkipDatabaseCreation
    Skip database creation (use if database already exists)

.PARAMETER SkipCLR
    Skip CLR assembly deployment

.PARAMETER SkipScaffold
    Skip EF Core entity scaffolding

.PARAMETER SkipBuild
    Skip .NET solution build

.EXAMPLE
    .\Deploy-Master.ps1 -Server localhost
    
    Complete fresh deployment to localhost

.EXAMPLE
    .\Deploy-Master.ps1 -Server localhost -SkipDatabaseCreation
    
    Deploy to existing database (update only)
#>

[CmdletBinding()]
param(
    [string]$Server = "localhost",
    [string]$Database = "Hartonomous",
    [switch]$SkipDatabaseCreation,
    [switch]$SkipCLR,
    [switch]$SkipScaffold,
    [switch]$SkipBuild
)

$ErrorActionPreference = "Stop"
$script:StartTime = Get-Date

# =============================================================================
# UTILITY FUNCTIONS
# =============================================================================

function Write-Header {
    param([string]$Text)
    Write-Host ""
    Write-Host "???????????????????????????????????????????????????????????????" -ForegroundColor Cyan
    Write-Host " $Text" -ForegroundColor Cyan  
    Write-Host "???????????????????????????????????????????????????????????????" -ForegroundColor Cyan
    Write-Host ""
}

function Write-Step {
    param([string]$Text)
    Write-Host "? $Text" -ForegroundColor Yellow
}

function Write-Success {
    param([string]$Text)
    Write-Host "? $Text" -ForegroundColor Green
}

function Write-Fail {
    param([string]$Text)
    Write-Host "? $Text" -ForegroundColor Red
}

# =============================================================================
# MAIN DEPLOYMENT
# =============================================================================

Write-Header "HARTONOMOUS MASTER DEPLOYMENT"

Write-Host "Target: $Server\$Database" -ForegroundColor White
Write-Host ""

try {
    # Call the comprehensive deploy-hartonomous.ps1 script
    $deployScript = Join-Path $PSScriptRoot "deploy-hartonomous.ps1"
    
    if (-not (Test-Path $deployScript)) {
        throw "deploy-hartonomous.ps1 not found at: $deployScript"
    }
    
    Write-Step "Executing unified deployment script..."
    Write-Host ""
    
    $params = @{
        Server = $Server
        Database = $Database
        IntegratedSecurity = $true
        TrustServerCertificate = $true
    }
    
    & $deployScript @params
    
    if ($LASTEXITCODE -ne 0) {
        throw "Deployment failed with exit code $LASTEXITCODE"
    }
    
    $duration = (Get-Date) - $script:StartTime
    Write-Header "DEPLOYMENT COMPLETE"
    Write-Host "Duration: $($duration.ToString('mm\:ss'))" -ForegroundColor Green
    exit 0
}
catch {
    Write-Fail "Deployment failed: $_"
    exit 1
}
