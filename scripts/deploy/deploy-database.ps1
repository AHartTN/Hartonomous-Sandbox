#!/usr/bin/env pwsh
<#
.SYNOPSIS
    Orchestrates complete Hartonomous database deployment to Azure Arc SQL Server.

.DESCRIPTION
    Master deployment orchestrator for Azure DevOps pipeline deployment to Arc-enabled SQL Server.
    Executes modular deployment scripts in correct order:
    1. Prerequisites validation (01-prerequisites.ps1)
    2. Database creation (02-database-create.ps1)
    3. FILESTREAM setup (03-filestream.ps1)
    4. CLR assembly deployment (04-clr-assembly.ps1)
    5. EF Core migrations (05-ef-migrations.ps1)
    6. Service Broker configuration (06-service-broker.ps1)
    7. Deployment verification (07-verification.ps1)
    
    Aggregates JSON outputs from each step and publishes as pipeline artifact.

.PARAMETER ServerName
    SQL Server instance name (default: reads from environment or uses localhost)

.PARAMETER DatabaseName
    Target database name (default: Hartonomous)

.PARAMETER ScriptsDirectory
    Path to directory containing deployment scripts (default: current directory)

.PARAMETER AssemblyPath
    Path to SqlClrFunctions.dll compiled assembly

.PARAMETER ProjectPath
    Path to Hartonomous.Data.csproj for EF migrations

.PARAMETER SqlUser
    SQL authentication username (optional - uses Windows/integrated auth if not provided)

.PARAMETER SqlPassword
    SQL authentication password (optional - will be prompted if SqlUser provided without password)

.PARAMETER SkipVerification
    Skip final verification step (not recommended for production)

.EXAMPLE
    .\deploy-database.ps1 -ServerName "hart-server" -AssemblyPath "./SqlClrFunctions.dll" -ProjectPath "./Hartonomous.Data.csproj"
#>

[CmdletBinding()]
param(
    [Parameter(Mandatory=$false)]
    [string]$ServerName = $(if ($env:SQL_SERVER_NAME) { $env:SQL_SERVER_NAME } else { "localhost" }),
    
    [Parameter(Mandatory=$false)]
    [string]$DatabaseName = "Hartonomous",
    
    [Parameter(Mandatory=$false)]
    [string]$ScriptsDirectory = $PSScriptRoot,
    
    [Parameter(Mandatory)]
    [string]$AssemblyPath,
    
    [Parameter(Mandatory)]
    [string]$ProjectPath,
    
    [Parameter(Mandatory=$false)]
    [string]$SqlUser,
    
    [Parameter(Mandatory=$false)]
    [SecureString]$SqlPassword,
    
    [Parameter(Mandatory=$false)]
    [switch]$SkipVerification
)

$ErrorActionPreference = "Stop"
Set-StrictMode -Version Latest

# Aggregate results from all deployment steps
$deploymentResult = @{
    Orchestrator = "deploy-database.ps1"
    ServerName = $ServerName
    DatabaseName = $DatabaseName
    StartTime = (Get-Date -Format "o")
    EndTime = ""
    Success = $false
    Steps = @()
    Summary = @{
        TotalSteps = 7
        CompletedSteps = 0
        FailedStep = ""
        Errors = @()
        Warnings = @()
    }
}

function Invoke-DeploymentStep {
    param(
        [string]$StepName,
        [string]$ScriptPath,
        [hashtable]$Parameters
    )
    
    Write-Host "`n" -NoNewline
    Write-Host ("=" * 80) -ForegroundColor Cyan
    Write-Host "STEP: $StepName" -ForegroundColor Cyan
    Write-Host ("=" * 80) -ForegroundColor Cyan
    
    $stepResult = @{
        Name = $StepName
        ScriptPath = $ScriptPath
        StartTime = (Get-Date -Format "o")
        EndTime = ""
        Success = $false
        Output = ""
        JsonOutput = $null
        ExitCode = -1
    }
    
    try {
        # Execute script with parameters
        $output = & $ScriptPath @Parameters 2>&1
        $stepResult.ExitCode = $LASTEXITCODE
        $stepResult.Output = ($output | Out-String)
        
        # Try to parse JSON output from artifact staging directory
        if ($env:BUILD_ARTIFACTSTAGINGDIRECTORY) {
            $jsonFileName = [System.IO.Path]::GetFileNameWithoutExtension($ScriptPath) -replace '^\d+-', ''
            $jsonPath = Join-Path $env:BUILD_ARTIFACTSTAGINGDIRECTORY "$jsonFileName.json"
            
            if (Test-Path $jsonPath) {
                $jsonContent = Get-Content $jsonPath -Raw -Encoding UTF8
                $stepResult.JsonOutput = $jsonContent | ConvertFrom-Json
            }
        }
        
        if ($stepResult.ExitCode -eq 0) {
            $stepResult.Success = $true
            $deploymentResult.Summary.CompletedSteps++
            Write-Host "`n✓ Step completed successfully" -ForegroundColor Green
        }
        else {
            throw "Script exited with code $($stepResult.ExitCode)"
        }
    }
    catch {
        $stepResult.Success = $false
        $deploymentResult.Summary.FailedStep = $StepName
        $deploymentResult.Summary.Errors += "$StepName failed: $($_.Exception.Message)"
        Write-Host "`n✗ Step failed: $($_.Exception.Message)" -ForegroundColor Red
        throw
    }
    finally {
        $stepResult.EndTime = (Get-Date -Format "o")
        $deploymentResult.Steps += $stepResult
    }
    
    return $stepResult
}

# Main deployment execution
try {
    Write-Host @"
╔════════════════════════════════════════════════════════════════════════════╗
║                                                                            ║
║           HARTONOMOUS DATABASE DEPLOYMENT TO AZURE ARC SQL SERVER          ║
║                                                                            ║
╚════════════════════════════════════════════════════════════════════════════╝
"@ -ForegroundColor Cyan

    Write-Host "`nDeployment Configuration:" -ForegroundColor Yellow
    Write-Host "  Server: $ServerName" -ForegroundColor Gray
    Write-Host "  Database: $DatabaseName" -ForegroundColor Gray
    Write-Host "  Scripts: $ScriptsDirectory" -ForegroundColor Gray
    Write-Host "  Assembly: $AssemblyPath" -ForegroundColor Gray
    Write-Host "  EF Project: $ProjectPath" -ForegroundColor Gray
    Write-Host "  Authentication: $(if ($SqlUser) { "SQL ($SqlUser)" } else { "Windows/Integrated" })" -ForegroundColor Gray
    
    # Prepare common parameters
    $commonParams = @{
        ServerName = $ServerName
        DatabaseName = $DatabaseName
    }
    
    if ($SqlUser) {
        $commonParams.SqlUser = $SqlUser
        
        if (-not $SqlPassword) {
            $SqlPassword = Read-Host -Prompt "Enter SQL Server password for user '$SqlUser'" -AsSecureString
        }
        $commonParams.SqlPassword = $SqlPassword
    }
    
    # Step 1: Prerequisites
    $prereqParams = $commonParams.Clone()
    Invoke-DeploymentStep `
        -StepName "Prerequisites Validation" `
        -ScriptPath (Join-Path $ScriptsDirectory "01-prerequisites.ps1") `
        -Parameters $prereqParams
    
    # Step 2: Database Creation
    $dbCreateParams = $commonParams.Clone()
    Invoke-DeploymentStep `
        -StepName "Database Creation" `
        -ScriptPath (Join-Path $ScriptsDirectory "02-database-create.ps1") `
        -Parameters $dbCreateParams
    
    # Step 3: FILESTREAM Setup
    $filestreamParams = $commonParams.Clone()
    Invoke-DeploymentStep `
        -StepName "FILESTREAM Configuration" `
        -ScriptPath (Join-Path $ScriptsDirectory "03-filestream.ps1") `
        -Parameters $filestreamParams
    
    # Step 4: CLR Assembly
    $clrParams = $commonParams.Clone()
    $clrParams.AssemblyPath = $AssemblyPath
    Invoke-DeploymentStep `
        -StepName "CLR Assembly Deployment" `
        -ScriptPath (Join-Path $ScriptsDirectory "04-clr-assembly.ps1") `
        -Parameters $clrParams
    
    # Step 5: EF Migrations
    $migrationsParams = $commonParams.Clone()
    $migrationsParams.ProjectPath = $ProjectPath
    Invoke-DeploymentStep `
        -StepName "EF Core Migrations" `
        -ScriptPath (Join-Path $ScriptsDirectory "05-ef-migrations.ps1") `
        -Parameters $migrationsParams
    
    # Step 6: Service Broker
    $brokerParams = $commonParams.Clone()
    # Check if setup-service-broker.sql exists
    $setupScriptPath = Join-Path $ScriptsDirectory "..\setup-service-broker.sql"
    if (Test-Path $setupScriptPath) {
        $brokerParams.SetupScriptPath = $setupScriptPath
    }
    Invoke-DeploymentStep `
        -StepName "Service Broker Setup" `
        -ScriptPath (Join-Path $ScriptsDirectory "06-service-broker.ps1") `
        -Parameters $brokerParams
    
    # Step 7: Verification (unless skipped)
    if (-not $SkipVerification) {
        $verifyParams = $commonParams.Clone()
        Invoke-DeploymentStep `
            -StepName "Deployment Verification" `
            -ScriptPath (Join-Path $ScriptsDirectory "07-verification.ps1") `
            -Parameters $verifyParams
    }
    else {
        Write-Host "`n! Skipping verification step (not recommended)" -ForegroundColor Yellow
        $deploymentResult.Summary.Warnings += "Verification step skipped"
    }
    
    # Mark deployment as successful
    $deploymentResult.Success = $true
    
    Write-Host "`n" -NoNewline
    Write-Host ("=" * 80) -ForegroundColor Green
    Write-Host "✓ DEPLOYMENT COMPLETED SUCCESSFULLY" -ForegroundColor Green
    Write-Host ("=" * 80) -ForegroundColor Green
    Write-Host "`nSteps Completed: $($deploymentResult.Summary.CompletedSteps) / $($deploymentResult.Summary.TotalSteps)" -ForegroundColor Green
}
catch {
    $deploymentResult.Success = $false
    Write-Host "`n" -NoNewline
    Write-Host ("=" * 80) -ForegroundColor Red
    Write-Host "✗ DEPLOYMENT FAILED" -ForegroundColor Red
    Write-Host ("=" * 80) -ForegroundColor Red
    Write-Host "`nFailed Step: $($deploymentResult.Summary.FailedStep)" -ForegroundColor Red
    Write-Host "Error: $($_.Exception.Message)" -ForegroundColor Red
    Write-Host "`nSteps Completed: $($deploymentResult.Summary.CompletedSteps) / $($deploymentResult.Summary.TotalSteps)" -ForegroundColor Yellow
}
finally {
    $deploymentResult.EndTime = (Get-Date -Format "o")
    
    # Calculate duration
    $duration = ([DateTime]$deploymentResult.EndTime) - ([DateTime]$deploymentResult.StartTime)
    Write-Host "`nDeployment Duration: $($duration.ToString('mm\:ss'))" -ForegroundColor Gray
    
    # Output aggregate JSON
    $jsonOutput = $deploymentResult | ConvertTo-Json -Depth 10 -Compress
    Write-Host "`n=== Deployment Summary JSON ===" -ForegroundColor Cyan
    Write-Host $jsonOutput
    
    # Write to artifact staging directory if in Azure DevOps
    if ($env:BUILD_ARTIFACTSTAGINGDIRECTORY) {
        $summaryPath = Join-Path $env:BUILD_ARTIFACTSTAGINGDIRECTORY "deployment-summary.json"
        $jsonOutput | Out-File -FilePath $summaryPath -Encoding utf8 -NoNewline
        Write-Host "`nDeployment summary written to: $summaryPath" -ForegroundColor Gray
    }
    
    # Exit with appropriate code
    if (-not $deploymentResult.Success) {
        exit 1
    }
}

exit 0
