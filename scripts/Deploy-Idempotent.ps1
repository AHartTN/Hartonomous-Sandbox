# =============================================
# MASTER DEPLOYMENT SCRIPT - IDEMPOTENT
# Hartonomous Cognitive Kernel v1.0
# =============================================
# This script can be run multiple times safely
# All operations are idempotent (CREATE OR ALTER, IF NOT EXISTS, etc.)
# =============================================

param(
    [Parameter(Mandatory=$false)]
    [string]$ServerInstance = "localhost",
    
    [Parameter(Mandatory=$false)]
    [string]$DatabaseName = "Hartonomous",
    
    [Parameter(Mandatory=$false)]
    [ValidateSet("Development", "Staging", "Production")]
    [string]$Environment = "Development",
    
    [Parameter(Mandatory=$false)]
    [switch]$SkipTests,
    
    [Parameter(Mandatory=$false)]
    [switch]$Verbose
)

$ErrorActionPreference = "Stop"
$VerbosePreference = if ($Verbose) { "Continue" } else { "SilentlyContinue" }

$ScriptRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
$RepoRoot = Split-Path -Parent $ScriptRoot

Write-Host "=============================================" -ForegroundColor Cyan
Write-Host "HARTONOMOUS DEPLOYMENT - IDEMPOTENT" -ForegroundColor Cyan
Write-Host "=============================================" -ForegroundColor Cyan
Write-Host "Server:      $ServerInstance" -ForegroundColor White
Write-Host "Database:    $DatabaseName" -ForegroundColor White
Write-Host "Environment: $Environment" -ForegroundColor White
Write-Host "=============================================" -ForegroundColor Cyan
Write-Host ""

# =============================================
# PHASE 0: PRE-DEPLOYMENT VALIDATION
# =============================================
Write-Host "[PHASE 0] Pre-Deployment Validation" -ForegroundColor Yellow

# Check SQL Server connectivity
Write-Verbose "Testing SQL Server connection..."
try {
    $testQuery = "SELECT @@VERSION"
    $null = Invoke-Sqlcmd -ServerInstance $ServerInstance -Query $testQuery -ErrorAction Stop
    Write-Host "  ? SQL Server connection successful" -ForegroundColor Green
} catch {
    Write-Host "  ? Cannot connect to SQL Server: $ServerInstance" -ForegroundColor Red
    Write-Host "    Error: $_" -ForegroundColor Red
    exit 1
}

# Check if database exists
Write-Verbose "Checking if database exists..."
$dbCheck = Invoke-Sqlcmd -ServerInstance $ServerInstance -Query "SELECT COUNT(*) AS Exists FROM sys.databases WHERE name = '$DatabaseName'" -ErrorAction Stop
if ($dbCheck.Exists -eq 0) {
    Write-Host "  ! Database '$DatabaseName' does not exist - creating..." -ForegroundColor Yellow
    Invoke-Sqlcmd -ServerInstance $ServerInstance -Query "CREATE DATABASE [$DatabaseName]" -ErrorAction Stop
    Write-Host "  ? Database created" -ForegroundColor Green
} else {
    Write-Host "  ? Database exists" -ForegroundColor Green
}

# Check dotnet CLI
Write-Verbose "Checking dotnet CLI..."
try {
    $dotnetVersion = dotnet --version
    Write-Host "  ? .NET SDK $dotnetVersion" -ForegroundColor Green
} catch {
    Write-Host "  ? .NET SDK not found" -ForegroundColor Red
    exit 1
}

Write-Host ""

# =============================================
# PHASE 1: BUILD SOLUTION
# =============================================
Write-Host "[PHASE 1] Building Solution" -ForegroundColor Yellow

Push-Location $RepoRoot
try {
    Write-Verbose "Running dotnet restore..."
    dotnet restore Hartonomous.sln --verbosity quiet
    
    Write-Verbose "Running dotnet build..."
    $buildOutput = dotnet build Hartonomous.sln --configuration Release --no-restore 2>&1
    
    if ($LASTEXITCODE -ne 0) {
        Write-Host "  ? Build failed" -ForegroundColor Red
        Write-Host $buildOutput -ForegroundColor Red
        exit 1
    }
    
    # Check for warnings
    $warnings = $buildOutput | Select-String "warning"
    if ($warnings) {
        Write-Host "  ? Build succeeded with warnings:" -ForegroundColor Yellow
        $warnings | ForEach-Object { Write-Host "    $_" -ForegroundColor Yellow }
    } else {
        Write-Host "  ? Build successful (0 warnings)" -ForegroundColor Green
    }
} finally {
    Pop-Location
}

Write-Host ""

# =============================================
# PHASE 2: DATABASE SCHEMA DEPLOYMENT
# =============================================
Write-Host "[PHASE 2] Database Schema Deployment" -ForegroundColor Yellow

$schemaScripts = @(
    "src\Hartonomous.Database\Schemas\provenance.sql",
    "src\Hartonomous.Database\Tables\provenance.SemanticPathCache.sql"
)

foreach ($script in $schemaScripts) {
    $scriptPath = Join-Path $RepoRoot $script
    if (Test-Path $scriptPath) {
        Write-Verbose "Executing: $script"
        try {
            Invoke-Sqlcmd -ServerInstance $ServerInstance -Database $DatabaseName -InputFile $scriptPath -ErrorAction Stop
            Write-Host "  ? $script" -ForegroundColor Green
        } catch {
            Write-Host "  ? Failed: $script" -ForegroundColor Red
            Write-Host "    Error: $_" -ForegroundColor Red
            exit 1
        }
    } else {
        Write-Host "  ? Not found: $script" -ForegroundColor Yellow
    }
}

Write-Host ""

# =============================================
# PHASE 3: STORED PROCEDURES DEPLOYMENT
# =============================================
Write-Host "[PHASE 3] Stored Procedures Deployment" -ForegroundColor Yellow

$procedureScripts = @(
    "src\Hartonomous.Database\Procedures\dbo.sp_GenerateOptimalPath.sql",
    "src\Hartonomous.Database\Procedures\dbo.sp_AtomizeImage_Governed.sql",
    "src\Hartonomous.Database\Procedures\dbo.sp_AtomizeText_Governed.sql",
    "src\Hartonomous.Database\Procedures\dbo.sp_MultiModelEnsemble.sql"
)

foreach ($script in $procedureScripts) {
    $scriptPath = Join-Path $RepoRoot $script
    if (Test-Path $scriptPath) {
        Write-Verbose "Executing: $script"
        try {
            Invoke-Sqlcmd -ServerInstance $ServerInstance -Database $DatabaseName -InputFile $scriptPath -ErrorAction Stop -QueryTimeout 120
            Write-Host "  ? $(Split-Path $script -Leaf)" -ForegroundColor Green
        } catch {
            Write-Host "  ? Failed: $script" -ForegroundColor Red
            Write-Host "    Error: $_" -ForegroundColor Red
            exit 1
        }
    }
}

Write-Host ""

# =============================================
# PHASE 4: POST-DEPLOYMENT OPTIMIZATION
# =============================================
Write-Host "[PHASE 4] Post-Deployment Optimization" -ForegroundColor Yellow

$optimizationScripts = @(
    "src\Hartonomous.Database\Scripts\Post-Deployment\Optimize_ColumnstoreCompression.sql"
)

foreach ($script in $optimizationScripts) {
    $scriptPath = Join-Path $RepoRoot $script
    if (Test-Path $scriptPath) {
        Write-Verbose "Executing: $script"
        try {
            Invoke-Sqlcmd -ServerInstance $ServerInstance -Database $DatabaseName -InputFile $scriptPath -ErrorAction Stop -QueryTimeout 300
            Write-Host "  ? $(Split-Path $script -Leaf)" -ForegroundColor Green
        } catch {
            Write-Host "  ? Failed: $script" -ForegroundColor Red
            Write-Host "    Error: $_" -ForegroundColor Red
            exit 1
        }
    }
}

Write-Host ""

# =============================================
# PHASE 5: VERIFICATION
# =============================================
Write-Host "[PHASE 5] Deployment Verification" -ForegroundColor Yellow

$verificationScript = Join-Path $RepoRoot "tests\Phase_5_Verification.sql"
if (Test-Path $verificationScript) {
    Write-Verbose "Running verification tests..."
    try {
        $verificationOutput = Invoke-Sqlcmd -ServerInstance $ServerInstance -Database $DatabaseName -InputFile $verificationScript -ErrorAction Stop -Verbose:$false
        
        # Count passes and fails
        $output = $verificationOutput | Out-String
        $passCount = ([regex]::Matches($output, "? PASS")).Count
        $failCount = ([regex]::Matches($output, "? FAIL")).Count
        
        Write-Host "  Results: $passCount passed, $failCount failed" -ForegroundColor $(if ($failCount -eq 0) { "Green" } else { "Yellow" })
        
        if ($failCount -gt 0) {
            Write-Host "  ? Some verification tests failed - review output above" -ForegroundColor Yellow
        } else {
            Write-Host "  ? All verification tests passed" -ForegroundColor Green
        }
    } catch {
        Write-Host "  ? Verification failed" -ForegroundColor Red
        Write-Host "    Error: $_" -ForegroundColor Red
    }
} else {
    Write-Host "  ? Verification script not found" -ForegroundColor Yellow
}

Write-Host ""

# =============================================
# PHASE 6: RUN TESTS (Optional)
# =============================================
if (-not $SkipTests) {
    Write-Host "[PHASE 6] Running Tests" -ForegroundColor Yellow
    
    Push-Location $RepoRoot
    try {
        Write-Verbose "Running dotnet test..."
        $testOutput = dotnet test Hartonomous.sln --configuration Release --no-build --verbosity minimal 2>&1
        
        if ($LASTEXITCODE -eq 0) {
            $testResults = $testOutput | Select-String "Passed!"
            Write-Host "  ? $testResults" -ForegroundColor Green
        } else {
            Write-Host "  ? Tests failed" -ForegroundColor Red
            $testOutput | Select-String "Failed!" | ForEach-Object { Write-Host "    $_" -ForegroundColor Red }
        }
    } finally {
        Pop-Location
    }
} else {
    Write-Host "[PHASE 6] Tests Skipped" -ForegroundColor Gray
}

Write-Host ""

# =============================================
# DEPLOYMENT SUMMARY
# =============================================
Write-Host "=============================================" -ForegroundColor Cyan
Write-Host "DEPLOYMENT COMPLETE" -ForegroundColor Cyan
Write-Host "=============================================" -ForegroundColor Cyan
Write-Host "Database:    $DatabaseName" -ForegroundColor White
Write-Host "Environment: $Environment" -ForegroundColor White
Write-Host "Timestamp:   $(Get-Date -Format 'yyyy-MM-dd HH:mm:ss')" -ForegroundColor White
Write-Host "=============================================" -ForegroundColor Cyan
Write-Host ""

Write-Host "Next steps:" -ForegroundColor Yellow
Write-Host "  1. Review verification results above" -ForegroundColor White
Write-Host "  2. Ingest test data for compression analysis" -ForegroundColor White
Write-Host "  3. Run application smoke tests" -ForegroundColor White
Write-Host ""

exit 0
