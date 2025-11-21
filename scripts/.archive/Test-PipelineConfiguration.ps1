# =============================================
# Test-PipelineConfiguration.ps1
# Validates CI/CD pipeline configuration
# =============================================

param(
    [Parameter(Mandatory=$false)]
    [ValidateSet('GitHub', 'AzureDevOps', 'Both')]
    [string]$Platform = 'Both'
)

$ErrorActionPreference = 'Stop'

Write-Host "=============================================" -ForegroundColor Cyan
Write-Host "PIPELINE CONFIGURATION VALIDATION" -ForegroundColor Cyan
Write-Host "=============================================" -ForegroundColor Cyan
Write-Host ""

$issues = @()
$warnings = @()

# =============================================
# FUNCTION: Test File Exists
# =============================================
function Test-FileExists {
    param([string]$Path, [string]$Description)
    
    if (Test-Path $Path) {
        Write-Host "  ? $Description" -ForegroundColor Green
        return $true
    } else {
        Write-Host "  ? $Description (Missing: $Path)" -ForegroundColor Red
        $script:issues += "Missing file: $Path ($Description)"
        return $false
    }
}

# =============================================
# TEST 1: GITHUB ACTIONS PIPELINE
# =============================================
if ($Platform -in @('GitHub', 'Both')) {
    Write-Host "[1] GitHub Actions Pipeline" -ForegroundColor Yellow
    Write-Host ""
    
    $ghWorkflow = ".github\workflows\ci-cd.yml"
    if (Test-FileExists $ghWorkflow "GitHub Actions workflow") {
        # Parse YAML and check for key sections
        $content = Get-Content $ghWorkflow -Raw
        
        if ($content -match 'build-dacpac:') {
            Write-Host "  ? build-dacpac job found" -ForegroundColor Green
        } else {
            $issues += "GitHub Actions: Missing build-dacpac job"
        }
        
        if ($content -match 'deploy-database:') {
            Write-Host "  ? deploy-database job found" -ForegroundColor Green
        } else {
            $issues += "GitHub Actions: Missing deploy-database job"
        }
        
        if ($content -match 'scaffold-entities:') {
            Write-Host "  ? scaffold-entities job found" -ForegroundColor Green
        } else {
            $issues += "GitHub Actions: Missing scaffold-entities job"
        }
        
        if ($content -match 'build-and-test:') {
            Write-Host "  ? build-and-test job found" -ForegroundColor Green
        } else {
            $issues += "GitHub Actions: Missing build-and-test job"
        }
        
        # Check for hybrid testing configuration
        if ($content -match 'CI:\s*true' -or $content -match 'CI:\s*\$\{\{') {
            Write-Host "  ? Hybrid database testing configured (CI env variable)" -ForegroundColor Green
        } else {
            $warnings += "GitHub Actions: CI env variable not set - may not use hybrid testing"
        }
    }
    
    Write-Host ""
}

# =============================================
# TEST 2: AZURE DEVOPS PIPELINE
# =============================================
if ($Platform -in @('AzureDevOps', 'Both')) {
    Write-Host "[2] Azure DevOps Pipeline (Main)" -ForegroundColor Yellow
    Write-Host ""
    
    $adoMain = "azure-pipelines.yml"
    if (Test-FileExists $adoMain "Azure DevOps main pipeline") {
        $content = Get-Content $adoMain -Raw
        
        if ($content -match 'stage:\s*BuildDatabase') {
            Write-Host "  ? BuildDatabase stage found" -ForegroundColor Green
        } else {
            $issues += "Azure DevOps: Missing BuildDatabase stage"
        }
        
        if ($content -match 'stage:\s*DeployDatabase') {
            Write-Host "  ? DeployDatabase stage found" -ForegroundColor Green
        } else {
            $issues += "Azure DevOps: Missing DeployDatabase stage"
        }
        
        if ($content -match 'stage:\s*ScaffoldEntities') {
            Write-Host "  ? ScaffoldEntities stage found" -ForegroundColor Green
        } else {
            $issues += "Azure DevOps: Missing ScaffoldEntities stage"
        }
        
        if ($content -match 'stage:\s*BuildDotNet') {
            Write-Host "  ? BuildDotNet stage found" -ForegroundColor Green
        } else {
            $issues += "Azure DevOps: Missing BuildDotNet stage"
        }
        
        # Check for hybrid testing
        if ($content -match 'CI:') {
            Write-Host "  ? Hybrid database testing configured" -ForegroundColor Green
        } else {
            $warnings += "Azure DevOps: CI env variable not set - may not use hybrid testing"
        }
    }
    
    Write-Host ""
    
    # Check database-specific pipeline
    Write-Host "[3] Azure DevOps Pipeline (Database)" -ForegroundColor Yellow
    Write-Host ""
    
    $adoDb = ".azure-pipelines\database-pipeline.yml"
    if (Test-FileExists $adoDb "Azure DevOps database pipeline") {
        $content = Get-Content $adoDb -Raw
        
        if ($content -match 'stage:\s*Build') {
            Write-Host "  ? Build stage found" -ForegroundColor Green
        } else {
            $issues += "Azure DevOps DB: Missing Build stage"
        }
        
        if ($content -match 'stage:\s*DeployToHartDesktop') {
            Write-Host "  ? DeployToHartDesktop stage found" -ForegroundColor Green
        } else {
            $issues += "Azure DevOps DB: Missing DeployToHartDesktop stage"
        }
    }
    
    Write-Host ""
}

# =============================================
# TEST 3: DEPLOYMENT SCRIPTS
# =============================================
Write-Host "[4] Deployment Scripts" -ForegroundColor Yellow
Write-Host ""

$requiredScripts = @(
    @{Path="scripts\Initialize-CLRSigning.ps1"; Description="CLR signing initialization"},
    @{Path="scripts\Sign-CLRAssemblies.ps1"; Description="CLR assembly signing"},
    @{Path="scripts\Deploy-CLRCertificate.ps1"; Description="CLR certificate deployment"},
    @{Path="scripts\deploy-clr-assemblies.ps1"; Description="External CLR deployment"},
    @{Path="scripts\Deploy-Database.ps1"; Description="Unified database deployment"},
    @{Path="scripts\scaffold-entities.ps1"; Description="EF Core scaffolding"},
    @{Path="scripts\build-dacpac.ps1"; Description="DACPAC build"},
    @{Path="scripts\verify-dacpac.ps1"; Description="DACPAC verification"},
    @{Path="scripts\Run-CoreTests.ps1"; Description="Core test execution"}
)

foreach ($script in $requiredScripts) {
    Test-FileExists $script.Path $script.Description | Out-Null
}

Write-Host ""

# =============================================
# TEST 4: TEST INFRASTRUCTURE
# =============================================
Write-Host "[5] Test Infrastructure" -ForegroundColor Yellow
Write-Host ""

$testFiles = @(
    @{Path="tests\README.md"; Description="Test documentation"},
    @{Path="tests\TESTING_ROADMAP.md"; Description="Testing roadmap"},
    @{Path="tests\Hartonomous.DatabaseTests\Infrastructure\DatabaseTestBase.cs"; Description="Hybrid database test base"},
    @{Path="tests\Hartonomous.DatabaseTests\Tests\Infrastructure\DatabaseConnectionTests.cs"; Description="Database connection tests"},
    @{Path="scripts\Run-CoreTests.ps1"; Description="Core test script"}
)

foreach ($file in $testFiles) {
    Test-FileExists $file.Path $file.Description | Out-Null
}

Write-Host ""

# =============================================
# TEST 5: DOCUMENTATION
# =============================================
Write-Host "[6] Documentation" -ForegroundColor Yellow
Write-Host ""

$docs = @(
    @{Path="docs\CI_CD_PIPELINE_GUIDE.md"; Description="CI/CD pipeline guide"},
    @{Path="docs\ENTERPRISE_DEPLOYMENT.md"; Description="Enterprise deployment guide"},
    @{Path="docs\TESTING_STRATEGY_PROPOSAL.md"; Description="Testing strategy"},
    @{Path="docs\PHASE_7_COMPLETE.md"; Description="Phase 7 completion report"}
)

foreach ($doc in $docs) {
    Test-FileExists $doc.Path $doc.Description | Out-Null
}

Write-Host ""

# =============================================
# TEST 6: HYBRID TEST BASE CONFIGURATION
# =============================================
Write-Host "[7] Hybrid Test Base" -ForegroundColor Yellow
Write-Host ""

$testBase = "tests\Hartonomous.DatabaseTests\Infrastructure\DatabaseTestBase.cs"
if (Test-Path $testBase) {
    $content = Get-Content $testBase -Raw
    
    if ($content -match 'TestEnvironment\.LocalDevelopment') {
        Write-Host "  ? LocalDB support detected" -ForegroundColor Green
    } else {
        $warnings += "DatabaseTestBase: LocalDB support may be missing"
    }
    
    if ($content -match 'TestEnvironment\.CiCd') {
        Write-Host "  ? CI/CD (Docker) support detected" -ForegroundColor Green
    } else {
        $warnings += "DatabaseTestBase: Docker support may be missing"
    }
    
    if ($content -match 'TestEnvironment\.AzureSql') {
        Write-Host "  ? Azure SQL support detected" -ForegroundColor Green
    } else {
        $warnings += "DatabaseTestBase: Azure SQL support may be missing"
    }
    
    if ($content -match 'MsSqlContainer') {
        Write-Host "  ? Testcontainers integration found" -ForegroundColor Green
    } else {
        $issues += "DatabaseTestBase: Missing Testcontainers integration"
    }
} else {
    $issues += "DatabaseTestBase.cs not found"
}

Write-Host ""

# =============================================
# SUMMARY
# =============================================
Write-Host "=============================================" -ForegroundColor Cyan
Write-Host "VALIDATION SUMMARY" -ForegroundColor Cyan
Write-Host "=============================================" -ForegroundColor Cyan
Write-Host ""

if ($issues.Count -eq 0 -and $warnings.Count -eq 0) {
    Write-Host "? ALL CHECKS PASSED" -ForegroundColor Green
    Write-Host ""
    Write-Host "Your CI/CD pipelines are correctly configured!" -ForegroundColor Green
    exit 0
} else {
    if ($issues.Count -gt 0) {
        Write-Host "? ISSUES FOUND: $($issues.Count)" -ForegroundColor Red
        Write-Host ""
        foreach ($issue in $issues) {
            Write-Host "  • $issue" -ForegroundColor Red
        }
        Write-Host ""
    }
    
    if ($warnings.Count -gt 0) {
        Write-Host "??  WARNINGS: $($warnings.Count)" -ForegroundColor Yellow
        Write-Host ""
        foreach ($warning in $warnings) {
            Write-Host "  • $warning" -ForegroundColor Yellow
        }
        Write-Host ""
    }
    
    Write-Host "Please fix the issues above before running pipelines." -ForegroundColor Yellow
    exit 1
}
