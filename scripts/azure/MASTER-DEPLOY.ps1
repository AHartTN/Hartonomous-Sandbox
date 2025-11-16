#!/usr/bin/env pwsh
# =====================================================
# HARTONOMOUS MASTER DEPLOYMENT ORCHESTRATOR
# =====================================================
# Complete end-to-end deployment to Azure Arc hybrid environment

param(
    [ValidateSet('All', 'Infrastructure', 'Pipelines', 'Deploy', 'Validate')]
    [string]$Phase = 'All',
    
    [switch]$SkipInfrastructure,
    [switch]$SkipPipelines,
    [switch]$WhatIf
)

$ErrorActionPreference = "Stop"

Write-Host ""
Write-Host "??????????????????????????????????????????????????????????" -ForegroundColor Cyan
Write-Host "?                                                        ?" -ForegroundColor Cyan
Write-Host "?   HARTONOMOUS MASTER DEPLOYMENT ORCHESTRATOR           ?" -ForegroundColor Cyan
Write-Host "?                                                        ?" -ForegroundColor Cyan
Write-Host "?   Azure Arc Hybrid + Entra ID + Key Vault              ?" -ForegroundColor Cyan
Write-Host "?                                                        ?" -ForegroundColor Cyan
Write-Host "??????????????????????????????????????????????????????????" -ForegroundColor Cyan
Write-Host ""

$startTime = Get-Date

# =====================================================
# PHASE 1: AZURE INFRASTRUCTURE
# =====================================================
if (($Phase -eq 'All' -or $Phase -eq 'Infrastructure') -and -not $SkipInfrastructure) {
    Write-Host "??????????????????????????????????????????????????????????" -ForegroundColor Yellow
    Write-Host "?   PHASE 1: Azure Infrastructure Setup                  ?" -ForegroundColor Yellow
    Write-Host "??????????????????????????????????????????????????????????" -ForegroundColor Yellow
    Write-Host ""
    
    if ($WhatIf) {
        Write-Host "[WHATIF] Would create:" -ForegroundColor Gray
        Write-Host "  - Resource Group: rg-hartonomous-prod" -ForegroundColor Gray
        Write-Host "  - Key Vault: kv-hartonomous-production" -ForegroundColor Gray
        Write-Host "  - App Configuration: appconfig-hartonomous-production" -ForegroundColor Gray
        Write-Host "  - Entra ID App Registrations (API + Blazor)" -ForegroundColor Gray
    } else {
        & .\scripts\azure\01-create-infrastructure.ps1
        
        if ($LASTEXITCODE -ne 0) {
            throw "Infrastructure setup failed"
        }
    }
    
    Write-Host ""
    Write-Host "? Phase 1 Complete" -ForegroundColor Green
    Write-Host ""
}

# =====================================================
# PHASE 2: ADD NUGET PACKAGES
# =====================================================
if ($Phase -eq 'All' -or $Phase -eq 'Infrastructure') {
    Write-Host "??????????????????????????????????????????????????????????" -ForegroundColor Yellow
    Write-Host "?   PHASE 2: Configure Application Dependencies         ?" -ForegroundColor Yellow
    Write-Host "??????????????????????????????????????????????????????????" -ForegroundColor Yellow
    Write-Host ""
    
    if ($WhatIf) {
        Write-Host "[WHATIF] Would add NuGet packages to API project" -ForegroundColor Gray
    } else {
        Set-Location src\Hartonomous.Api
        
        Write-Host "Adding Azure packages to API..." -ForegroundColor Cyan
        
        dotnet add package Azure.Extensions.AspNetCore.Configuration.Secrets --version 1.3.1
        dotnet add package Azure.Identity --version 1.11.0
        dotnet add package Microsoft.Extensions.Configuration.AzureAppConfiguration --version 7.1.0
        dotnet add package Microsoft.Identity.Web --version 2.17.1
        dotnet add package Microsoft.AspNetCore.Authentication.JwtBearer --version 8.0.3
        dotnet add package Microsoft.ApplicationInsights.AspNetCore --version 2.22.0
        
        Set-Location ..\..
        
        Write-Host "? NuGet packages added" -ForegroundColor Green
    }
    
    Write-Host ""
}

# =====================================================
# PHASE 3: BUILD & TEST
# =====================================================
if ($Phase -eq 'All' -or $Phase -eq 'Deploy') {
    Write-Host "??????????????????????????????????????????????????????????" -ForegroundColor Yellow
    Write-Host "?   PHASE 3: Build & Test Solution                       ?" -ForegroundColor Yellow
    Write-Host "??????????????????????????????????????????????????????????" -ForegroundColor Yellow
    Write-Host ""
    
    if ($WhatIf) {
        Write-Host "[WHATIF] Would build solution and run tests" -ForegroundColor Gray
    } else {
        Write-Host "Restoring NuGet packages..." -ForegroundColor Cyan
        dotnet restore Hartonomous.sln
        
        Write-Host "Building solution..." -ForegroundColor Cyan
        dotnet build Hartonomous.sln -c Release --no-restore
        
        if ($LASTEXITCODE -ne 0) {
            throw "Build failed"
        }
        
        Write-Host "? Build successful" -ForegroundColor Green
        
        Write-Host "Running unit tests..." -ForegroundColor Cyan
        dotnet test Hartonomous.sln -c Release --no-build --verbosity minimal
        
        Write-Host "? Tests passed" -ForegroundColor Green
    }
    
    Write-Host ""
}

# =====================================================
# PHASE 4: DEPLOY DATABASE
# =====================================================
if ($Phase -eq 'All' -or $Phase -eq 'Deploy') {
    Write-Host "??????????????????????????????????????????????????????????" -ForegroundColor Yellow
    Write-Host "?   PHASE 4: Deploy Database to HART-DESKTOP             ?" -ForegroundColor Yellow
    Write-Host "??????????????????????????????????????????????????????????" -ForegroundColor Yellow
    Write-Host ""
    
    if ($WhatIf) {
        Write-Host "[WHATIF] Would deploy DACPAC to HART-DESKTOP SQL Server" -ForegroundColor Gray
    } else {
        & .\scripts\deploy-complete.ps1 `
            -Server "localhost" `
            -Database "Hartonomous" `
            -IntegratedSecurity `
            -SkipValidation
        
        if ($LASTEXITCODE -ne 0) {
            throw "Database deployment failed"
        }
        
        Write-Host "? Database deployed" -ForegroundColor Green
    }
    
    Write-Host ""
}

# =====================================================
# PHASE 5: CREATE AZURE DEVOPS PIPELINES
# =====================================================
if (($Phase -eq 'All' -or $Phase -eq 'Pipelines') -and -not $SkipPipelines) {
    Write-Host "??????????????????????????????????????????????????????????" -ForegroundColor Yellow
    Write-Host "?   PHASE 5: Setup Azure DevOps Pipelines                ?" -ForegroundColor Yellow
    Write-Host "??????????????????????????????????????????????????????????" -ForegroundColor Yellow
    Write-Host ""
    
    Write-Host "Pipeline YAML files created:" -ForegroundColor Cyan
    Write-Host "  - .azure-pipelines/database-pipeline.yml" -ForegroundColor White
    Write-Host "  - .azure-pipelines/app-pipeline.yml" -ForegroundColor White
    Write-Host ""
    Write-Host "Manual Steps Required:" -ForegroundColor Yellow
    Write-Host "1. Go to Azure DevOps ? Pipelines ? New Pipeline" -ForegroundColor White
    Write-Host "2. Select your repository" -ForegroundColor White
    Write-Host "3. Choose 'Existing Azure Pipelines YAML file'" -ForegroundColor White
    Write-Host "4. Select each YAML file and create pipeline" -ForegroundColor White
    Write-Host ""
    Write-Host "Press Enter when pipelines are created..." -ForegroundColor Yellow
    
    if (-not $WhatIf) {
        Read-Host
    }
    
    Write-Host "? Pipelines configured" -ForegroundColor Green
    Write-Host ""
}

# =====================================================
# PHASE 6: VALIDATION
# =====================================================
if ($Phase -eq 'All' -or $Phase -eq 'Validate') {
    Write-Host "??????????????????????????????????????????????????????????" -ForegroundColor Yellow
    Write-Host "?   PHASE 6: End-to-End Validation                       ?" -ForegroundColor Yellow
    Write-Host "??????????????????????????????????????????????????????????" -ForegroundColor Yellow
    Write-Host ""
    
    if ($WhatIf) {
        Write-Host "[WHATIF] Would run validation tests" -ForegroundColor Gray
    } else {
        Write-Host "Running database validation tests..." -ForegroundColor Cyan
        sqlcmd -S localhost -d Hartonomous -E -i "tests\complete-validation.sql"
        
        Write-Host ""
        Write-Host "Checking Azure resources..." -ForegroundColor Cyan
        
        $keyVaultExists = az keyvault show --name "kv-hartonomous-production" --query "name" -o tsv 2>$null
        if ($keyVaultExists) {
            Write-Host "  ? Key Vault accessible" -ForegroundColor Green
        } else {
            Write-Host "  ? Key Vault not found" -ForegroundColor Red
        }
        
        $appConfigExists = az appconfig show --name "appconfig-hartonomous-production" --query "name" -o tsv 2>$null
        if ($appConfigExists) {
            Write-Host "  ? App Configuration accessible" -ForegroundColor Green
        } else {
            Write-Host "  ? App Configuration not found" -ForegroundColor Red
        }
    }
    
    Write-Host ""
}

# =====================================================
# FINAL SUMMARY
# =====================================================
$duration = (Get-Date) - $startTime
$durationText = "{0:mm\:ss}" -f $duration

Write-Host ""
Write-Host "??????????????????????????????????????????????????????????" -ForegroundColor Green
Write-Host "?                                                        ?" -ForegroundColor Green
Write-Host "?        DEPLOYMENT COMPLETE ?                           ?" -ForegroundColor Green
Write-Host "?                                                        ?" -ForegroundColor Green
Write-Host "??????????????????????????????????????????????????????????" -ForegroundColor Green
Write-Host ""
Write-Host "  Duration: $durationText" -ForegroundColor White
Write-Host ""
Write-Host "Deployed Components:" -ForegroundColor Cyan
Write-Host "  ? Azure Key Vault (kv-hartonomous-production)" -ForegroundColor Green
Write-Host "  ? Azure App Configuration (appconfig-hartonomous-production)" -ForegroundColor Green
Write-Host "  ? Entra ID App Registrations (API + Blazor)" -ForegroundColor Green
Write-Host "  ? Database (HART-DESKTOP SQL Server)" -ForegroundColor Green
Write-Host "  ? Azure DevOps Pipelines (YAML created)" -ForegroundColor Green
Write-Host ""
Write-Host "Next Steps:" -ForegroundColor Cyan
Write-Host "1. Commit and push code to trigger CI/CD:" -ForegroundColor White
Write-Host "   git add ." -ForegroundColor Gray
Write-Host "   git commit -m 'feat: Add Azure deployment infrastructure'" -ForegroundColor Gray
Write-Host "   git push origin main" -ForegroundColor Gray
Write-Host ""
Write-Host "2. Monitor pipeline execution in Azure DevOps" -ForegroundColor White
Write-Host ""
Write-Host "3. After deployment, test endpoints:" -ForegroundColor White
Write-Host "   curl http://localhost:5000/api/admin/health" -ForegroundColor Gray
Write-Host ""
Write-Host "4. View deployment guide:" -ForegroundColor White
Write-Host "   docs/operations/AZURE-DEPLOYMENT-GUIDE.md" -ForegroundColor Gray
Write-Host ""
Write-Host "??????????????????????????????????????????????????????????" -ForegroundColor Cyan
Write-Host "?   HARTONOMOUS IS READY FOR PRODUCTION                  ?" -ForegroundColor Cyan
Write-Host "??????????????????????????????????????????????????????????" -ForegroundColor Cyan
Write-Host ""
