<#
.SYNOPSIS
    Master deployment orchestrator for Hartonomous Cognitive Engine.

.DESCRIPTION
    Executes the complete deployment sequence in the correct order:
    1. Initialize CLR signing infrastructure (certificates)
    2. Deploy CLR certificate to SQL Server
    3. Build assemblies with strong-name signing
    4. Deploy DACPAC (schema and objects)
    5. Deploy CLR assemblies
    6. Provision SQL Server Agent Job for OODA loop

.PARAMETER Server
    SQL Server instance name (e.g., "localhost" or "server\instance")

.PARAMETER Database
    Target database name (default: "Hartonomous")

.PARAMETER SkipCertificate
    Skip certificate initialization (use if already exists)

.PARAMETER SkipBuild
    Skip build step (use if assemblies already built)

.PARAMETER Configuration
    Build configuration (default: "Release")

.EXAMPLE
    .\Deploy-All.ps1 -Server "localhost" -Database "Hartonomous"

.EXAMPLE
    .\Deploy-All.ps1 -Server "localhost" -SkipCertificate -SkipBuild

.NOTES
    Prerequisites:
    - PowerShell 7+
    - .NET 8.0 SDK
    - SQL Server 2022+
    - Windows SDK (for signtool.exe)
#>

[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [string]$Server,
    
    [Parameter(Mandatory = $false)]
    [string]$Database = "Hartonomous",
    
    [Parameter(Mandatory = $false)]
    [switch]$SkipCertificate,
    
    [Parameter(Mandatory = $false)]
    [switch]$SkipBuild,
    
    [Parameter(Mandatory = $false)]
    [string]$Configuration = "Release"
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$scriptRoot = Split-Path -Parent $PSCommandPath
$repoRoot = Split-Path -Parent $scriptRoot

function Write-Phase {
    param([string]$Message)
    Write-Host "`n========================================" -ForegroundColor Cyan
    Write-Host $Message -ForegroundColor Cyan
    Write-Host "========================================`n" -ForegroundColor Cyan
}

function Invoke-Step {
    param(
        [string]$Description,
        [scriptblock]$Action
    )
    
    Write-Host "▶ $Description..." -ForegroundColor Yellow
    try {
        & $Action
        Write-Host "✓ $Description completed" -ForegroundColor Green
        return $true
    }
    catch {
        Write-Host "✗ $Description failed: $_" -ForegroundColor Red
        throw
    }
}

try {
    Write-Host "╔════════════════════════════════════════════════╗" -ForegroundColor Magenta
    Write-Host "║   HARTONOMOUS COGNITIVE ENGINE DEPLOYMENT      ║" -ForegroundColor Magenta
    Write-Host "╚════════════════════════════════════════════════╝" -ForegroundColor Magenta
    
    Write-Host "`nTarget: $Server.$Database" -ForegroundColor White
    Write-Host "Repository Root: $repoRoot" -ForegroundColor Gray
    
    # =============================================================================
    # PHASE 1: SIGNING INFRASTRUCTURE
    # =============================================================================
    
    Write-Phase "PHASE 1: Signing Infrastructure"
    
    if (-not $SkipCertificate) {
        Invoke-Step "Initialize CLR signing certificate" {
            $certScript = Join-Path $scriptRoot "Initialize-CLRSigning.ps1"
            if (Test-Path $certScript) {
                & $certScript
            }
            else {
                Write-Host "Warning: Initialize-CLRSigning.ps1 not found, attempting to continue..." -ForegroundColor Yellow
            }
        }
        
        Invoke-Step "Deploy certificate to SQL Server" {
            $deployScript = Join-Path $scriptRoot "Deploy-CLRCertificate.ps1"
            if (Test-Path $deployScript) {
                & $deployScript -Server $Server
            }
            else {
                Write-Host "Warning: Deploy-CLRCertificate.ps1 not found, attempting to continue..." -ForegroundColor Yellow
            }
        }
    }
    else {
        Write-Host "⊘ Skipping certificate initialization (already exists)" -ForegroundColor Yellow
    }
    
    # =============================================================================
    # PHASE 2: BUILD
    # =============================================================================
    
    Write-Phase "PHASE 2: Build Assemblies"
    
    if (-not $SkipBuild) {
        Invoke-Step "Build with signing" {
            $buildScript = Join-Path $scriptRoot "Build-WithSigning.ps1"
            if (Test-Path $buildScript) {
                & $buildScript -Configuration $Configuration
            }
            else {
                # Fallback to dotnet build
                Push-Location $repoRoot
                try {
                    dotnet build Hartonomous.sln -c $Configuration
                }
                finally {
                    Pop-Location
                }
            }
        }
    }
    else {
        Write-Host "⊘ Skipping build (using existing assemblies)" -ForegroundColor Yellow
    }
    
    # =============================================================================
    # PHASE 3: SCHEMA DEPLOYMENT
    # =============================================================================
    
    Write-Phase "PHASE 3: Database Schema"
    
    Invoke-Step "Deploy DACPAC" {
        $dacpacScript = Join-Path $scriptRoot "deploy-dacpac.ps1"
        if (Test-Path $dacpacScript) {
            & $dacpacScript -Server $Server -Database $Database
        }
        else {
            Write-Host "Warning: deploy-dacpac.ps1 not found, skipping DACPAC deployment" -ForegroundColor Yellow
        }
    }
    
    # =============================================================================
    # PHASE 4: CLR ASSEMBLIES
    # =============================================================================
    
    Write-Phase "PHASE 4: CLR Assemblies"
    
    Invoke-Step "Deploy CLR assemblies" {
        $clrScript = Join-Path $scriptRoot "deploy-clr-assemblies.ps1"
        if (Test-Path $clrScript) {
            & $clrScript -Server $Server -Database $Database
        }
        else {
            throw "deploy-clr-assemblies.ps1 not found"
        }
    }
    
    # =============================================================================
    # PHASE 5: AUTONOMOUS OPERATIONS
    # =============================================================================
    
    Write-Phase "PHASE 5: Autonomous Operations"
    
    Invoke-Step "Provision SQL Server Agent Job" {
        $jobScript = Join-Path $scriptRoot "sql\Create-AutonomousAgentJob.sql"
        if (Test-Path $jobScript) {
            sqlcmd -S $Server -d msdb -i $jobScript -b
            if ($LASTEXITCODE -ne 0) {
                throw "SQL Server Agent Job provisioning failed with exit code $LASTEXITCODE"
            }
        }
        else {
            Write-Host "Warning: Create-AutonomousAgentJob.sql not found, skipping job creation" -ForegroundColor Yellow
        }
    }
    
    # =============================================================================
    # PHASE 6: VALIDATION
    # =============================================================================
    
    Write-Phase "PHASE 6: Deployment Validation"
    
    Invoke-Step "Run smoke tests" {
        $testScript = Join-Path $scriptRoot "Test-HartonomousDeployment-Simple.ps1"
        if (Test-Path $testScript) {
            & $testScript -Server $Server -Database $Database
        }
        else {
            Write-Host "Warning: Test script not found, skipping validation" -ForegroundColor Yellow
        }
    }
    
    # =============================================================================
    # COMPLETION
    # =============================================================================
    
    Write-Host "`n╔════════════════════════════════════════════════╗" -ForegroundColor Green
    Write-Host "║         DEPLOYMENT COMPLETED SUCCESSFULLY      ║" -ForegroundColor Green
    Write-Host "╚════════════════════════════════════════════════╝" -ForegroundColor Green
    
    Write-Host "`nNext Steps:" -ForegroundColor White
    Write-Host "1. Verify SQL Server Agent service is running" -ForegroundColor Gray
    Write-Host "2. Check OODA loop job execution history" -ForegroundColor Gray
    Write-Host "3. Monitor Service Broker queues for message flow" -ForegroundColor Gray
    Write-Host "4. Review application logs for any errors" -ForegroundColor Gray
    
    exit 0
}
catch {
    Write-Host "`n╔════════════════════════════════════════════════╗" -ForegroundColor Red
    Write-Host "║           DEPLOYMENT FAILED                    ║" -ForegroundColor Red
    Write-Host "╚════════════════════════════════════════════════╝" -ForegroundColor Red
    
    Write-Host "`nError: $_" -ForegroundColor Red
    Write-Host "`nStack Trace:" -ForegroundColor Gray
    Write-Host $_.ScriptStackTrace -ForegroundColor Gray
    
    exit 1
}
