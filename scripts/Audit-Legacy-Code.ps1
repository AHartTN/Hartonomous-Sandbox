# =============================================
# PHASE 7: LEGACY CODE PURGE & CONSISTENCY AUDIT
# Comprehensive Repository Analysis
# =============================================

param(
    [Parameter(Mandatory=$false)]
    [switch]$ShowDetails
)

$ErrorActionPreference = "Continue"
$RepoRoot = "D:\Repositories\Hartonomous"

Write-Host "=============================================" -ForegroundColor Cyan
Write-Host "HARTONOMOUS LEGACY CODE AUDIT" -ForegroundColor Cyan
Write-Host "Phase 7: Comprehensive Consistency Check" -ForegroundColor Cyan
Write-Host "=============================================" -ForegroundColor Cyan
Write-Host ""

$issues = @()
$warnings = @()

# =============================================
# AUDIT 1: SQL SCRIPTS IN DACPAC
# =============================================
Write-Host "[AUDIT 1] SQL Scripts in DACPAC Project" -ForegroundColor Yellow

$dbProjectPath = Join-Path $RepoRoot "src\Hartonomous.Database"

# Count actual SQL files
$actualTables = (Get-ChildItem "$dbProjectPath\Tables" -Filter "*.sql" -ErrorAction SilentlyContinue).Count
$actualProcs = (Get-ChildItem "$dbProjectPath\Procedures" -Filter "*.sql" -ErrorAction SilentlyContinue).Count
$actualFuncs = (Get-ChildItem "$dbProjectPath\Functions" -Filter "*.sql" -ErrorAction SilentlyContinue).Count
$actualViews = (Get-ChildItem "$dbProjectPath\Views" -Filter "*.sql" -ErrorAction SilentlyContinue).Count
$actualSchemas = (Get-ChildItem "$dbProjectPath\Schemas" -Filter "*.sql" -ErrorAction SilentlyContinue).Count

Write-Host "  Tables:         $actualTables files" -ForegroundColor White
Write-Host "  Procedures:     $actualProcs files" -ForegroundColor White
Write-Host "  Functions:      $actualFuncs files" -ForegroundColor White
Write-Host "  Views:          $actualViews files" -ForegroundColor White
Write-Host "  Schemas:        $actualSchemas files" -ForegroundColor White

# Check for Phase 1-6 critical files
$criticalFiles = @(
    "Tables\provenance.SemanticPathCache.sql",
    "Procedures\dbo.sp_GenerateOptimalPath.sql",
    "Procedures\dbo.sp_AtomizeImage_Governed.sql",
    "Procedures\dbo.sp_AtomizeText_Governed.sql",
    "Procedures\dbo.sp_MultiModelEnsemble.sql",
    "Schemas\provenance.sql"
)

Write-Host ""
Write-Host "  Critical files (Phases 1-6):" -ForegroundColor White
foreach ($file in $criticalFiles) {
    $fullPath = Join-Path $dbProjectPath $file
    if (Test-Path $fullPath) {
        Write-Host "    ? $file" -ForegroundColor Green
    } else {
        Write-Host "    ? MISSING: $file" -ForegroundColor Red
        $issues += "Missing critical file: $file"
    }
}

# Check Post-Deployment scripts
Write-Host ""
Write-Host "  Post-Deployment Scripts:" -ForegroundColor White
$postDeployScripts = Get-ChildItem "$dbProjectPath\Scripts\Post-Deployment" -Filter "*.sql" -ErrorAction SilentlyContinue

if ($postDeployScripts) {
    foreach ($script in $postDeployScripts) {
        Write-Host "    - $($script.Name)" -ForegroundColor Gray
    }
    
    # Check if Optimize_ColumnstoreCompression.sql exists
    if ($postDeployScripts.Name -contains "Optimize_ColumnstoreCompression.sql") {
        Write-Host "    ? Optimize_ColumnstoreCompression.sql found" -ForegroundColor Green
    } else {
        Write-Host "    ? Optimize_ColumnstoreCompression.sql missing" -ForegroundColor Red
        $issues += "Missing Optimize_ColumnstoreCompression.sql"
    }
} else {
    Write-Host "    ? No post-deployment scripts found" -ForegroundColor Yellow
    $warnings += "No post-deployment scripts"
}

Write-Host ""

# =============================================
# AUDIT 2: DUPLICATE/LEGACY PATTERNS
# =============================================
Write-Host "[AUDIT 2] Duplicate & Legacy Code Patterns" -ForegroundColor Yellow

# Check for old CREATE PROCEDURE (should all be CREATE OR ALTER)
Write-Host ""
Write-Host "  Checking for non-idempotent procedures..." -ForegroundColor White
$nonIdempotentProcs = Get-ChildItem "$dbProjectPath\Procedures" -Filter "*.sql" -ErrorAction SilentlyContinue | Where-Object {
    $content = Get-Content $_.FullName -Raw
    $content -match "^\s*CREATE\s+PROCEDURE" -and $content -notmatch "CREATE\s+OR\s+ALTER"
}

if ($nonIdempotentProcs) {
    Write-Host "    ? Found $($nonIdempotentProcs.Count) non-idempotent procedures:" -ForegroundColor Red
    foreach ($proc in $nonIdempotentProcs) {
        Write-Host "      - $($proc.Name)" -ForegroundColor Red
        $issues += "Non-idempotent procedure: $($proc.Name)"
    }
} else {
    Write-Host "    ? All procedures are idempotent (CREATE OR ALTER)" -ForegroundColor Green
}

# Check for VECTOR(1998) - should be VECTOR(1536)
Write-Host ""
Write-Host "  Checking for old vector dimensions (VECTOR(1998))..." -ForegroundColor White
$oldVectorDims = Get-ChildItem "$dbProjectPath\Procedures" -Filter "*.sql" -Recurse -ErrorAction SilentlyContinue | Where-Object {
    (Get-Content $_.FullName -Raw) -match "VECTOR\(1998\)"
}

if ($oldVectorDims) {
    Write-Host "    ? Found $($oldVectorDims.Count) files with old VECTOR(1998):" -ForegroundColor Red
    foreach ($file in $oldVectorDims) {
        Write-Host "      - $($file.Name)" -ForegroundColor Red
        $issues += "Old vector dimension in: $($file.Name)"
    }
} else {
    Write-Host "    ? No VECTOR(1998) found - all using correct dimensions" -ForegroundColor Green
}

# Check for deprecated sp_ prefixes in non-system tables
Write-Host ""
Write-Host "  Checking for deprecated patterns..." -ForegroundColor White
$deprecatedPatterns = @(
    @{ Pattern = "geometry::Point\("; Description = "Old geometry construction (should use STGeomFromText with M-value)" },
    @{ Pattern = "CREATE\s+CLUSTERED\s+INDEX.*AtomComposition"; Description = "Old index on AtomComposition (should be Columnstore)" }
)

$deprecatedCount = 0
foreach ($pattern in $deprecatedPatterns) {
    $matches = Get-ChildItem "$dbProjectPath" -Filter "*.sql" -Recurse -ErrorAction SilentlyContinue | Where-Object {
        (Get-Content $_.FullName -Raw) -match $pattern.Pattern
    }
    
    if ($matches) {
        $deprecatedCount += $matches.Count
        Write-Host "    ? Found $($matches.Count) files with: $($pattern.Description)" -ForegroundColor Yellow
        if ($ShowDetails) {
            foreach ($match in $matches) {
                Write-Host "      - $($match.Name)" -ForegroundColor Gray
            }
        }
        $warnings += $pattern.Description
    }
}

if ($deprecatedCount -eq 0) {
    Write-Host "    ? No deprecated patterns found" -ForegroundColor Green
}

Write-Host ""

# =============================================
# AUDIT 3: INFRASTRUCTURE CODE CONSISTENCY
# =============================================
Write-Host "[AUDIT 3] Infrastructure Code Consistency" -ForegroundColor Yellow

# Check for SignalR stubs
Write-Host ""
Write-Host "  Checking SignalR implementation..." -ForegroundColor White
$signalrStubs = Test-Path "$RepoRoot\src\Hartonomous.Infrastructure\Services\SignalR\SignalRStubs.cs"
$signalrHub = Test-Path "$RepoRoot\src\Hartonomous.Infrastructure\Services\SignalR\IngestionHub.cs"

if ($signalrStubs) {
    Write-Host "    ? SignalRStubs.cs still exists (should be deleted)" -ForegroundColor Red
    $issues += "Legacy SignalRStubs.cs file exists"
} else {
    Write-Host "    ? SignalRStubs.cs properly removed" -ForegroundColor Green
}

if ($signalrHub) {
    Write-Host "    ? Real IngestionHub.cs exists" -ForegroundColor Green
} else {
    Write-Host "    ? IngestionHub.cs missing" -ForegroundColor Red
    $issues += "Missing IngestionHub.cs"
}

# Check CLR optimizations
Write-Host ""
Write-Host "  Checking CLR optimizations..." -ForegroundColor White
$clrTensorProvider = Get-Content "$RepoRoot\src\Hartonomous.Database\CLR\TensorOperations\ClrTensorProvider.cs" -Raw
$transformerInference = Get-Content "$RepoRoot\src\Hartonomous.Database\CLR\TensorOperations\TransformerInference.cs" -Raw

if ($clrTensorProvider -match "ConcurrentDictionary.*_weightCache") {
    Write-Host "    ? ClrTensorProvider has static cache" -ForegroundColor Green
} else {
    Write-Host "    ? ClrTensorProvider missing static cache" -ForegroundColor Red
    $issues += "ClrTensorProvider missing Phase 3 cache optimization"
}

if ($transformerInference -match "UseNativeMKL") {
    Write-Host "    ? TransformerInference has MKL initialization" -ForegroundColor Green
} else {
    Write-Host "    ? TransformerInference missing MKL initialization" -ForegroundColor Red
    $issues += "TransformerInference missing Phase 3 MKL optimization"
}

Write-Host ""

# =============================================
# AUDIT 4: TEST COVERAGE
# =============================================
Write-Host "[AUDIT 4] Test Coverage Analysis" -ForegroundColor Yellow

$testProjects = @(
    "tests\Hartonomous.UnitTests",
    "tests\Hartonomous.IntegrationTests",
    "tests\Hartonomous.EndToEndTests",
    "tests\Hartonomous.DatabaseTests"
)

Write-Host ""
foreach ($testProject in $testProjects) {
    $projectPath = Join-Path $RepoRoot $testProject
    if (Test-Path $projectPath) {
        $testFiles = (Get-ChildItem $projectPath -Filter "*Tests.cs" -Recurse).Count
        Write-Host "  $testProject : $testFiles test files" -ForegroundColor White
    } else {
        Write-Host "  $testProject : NOT FOUND" -ForegroundColor Red
        $issues += "Missing test project: $testProject"
    }
}

Write-Host ""

# =============================================
# AUDIT 5: DEPLOYMENT SCRIPTS
# =============================================
Write-Host "[AUDIT 5] Deployment Automation" -ForegroundColor Yellow

$deploymentScripts = @(
    "scripts\Deploy-Idempotent.ps1",
    "scripts\Validate-Build.ps1",
    "tests\Phase_5_Verification.sql"
)

Write-Host ""
foreach ($script in $deploymentScripts) {
    $scriptPath = Join-Path $RepoRoot $script
    if (Test-Path $scriptPath) {
        Write-Host "  ? $script" -ForegroundColor Green
    } else {
        Write-Host "  ? $script missing" -ForegroundColor Red
        $issues += "Missing deployment script: $script"
    }
}

Write-Host ""

# =============================================
# SUMMARY
# =============================================
Write-Host "=============================================" -ForegroundColor Cyan
Write-Host "AUDIT SUMMARY" -ForegroundColor Cyan
Write-Host "=============================================" -ForegroundColor Cyan

Write-Host ""
Write-Host "Repository Statistics:" -ForegroundColor White
Write-Host "  SQL Tables:     $actualTables" -ForegroundColor White
Write-Host "  SQL Procedures: $actualProcs" -ForegroundColor White
Write-Host "  SQL Functions:  $actualFuncs" -ForegroundColor White
Write-Host "  SQL Views:      $actualViews" -ForegroundColor White
Write-Host ""

if ($issues.Count -eq 0 -and $warnings.Count -eq 0) {
    Write-Host "? NO ISSUES FOUND - Codebase is consistent!" -ForegroundColor Green
    exit 0
}

if ($issues.Count -gt 0) {
    Write-Host "? CRITICAL ISSUES: $($issues.Count)" -ForegroundColor Red
    foreach ($issue in $issues) {
        Write-Host "  - $issue" -ForegroundColor Red
    }
    Write-Host ""
}

if ($warnings.Count -gt 0) {
    Write-Host "? WARNINGS: $($warnings.Count)" -ForegroundColor Yellow
    foreach ($warning in $warnings) {
        Write-Host "  - $warning" -ForegroundColor Yellow
    }
    Write-Host ""
}

Write-Host "Run with -ShowDetails for verbose output" -ForegroundColor Gray
Write-Host "=============================================" -ForegroundColor Cyan

exit $(if ($issues.Count -gt 0) { 1 } else { 0 })
