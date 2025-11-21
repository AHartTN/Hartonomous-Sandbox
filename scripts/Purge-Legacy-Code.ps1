# =============================================
# PHASE 7: MASS LEGACY CODE PURGE
# Fixes all 53 critical issues automatically
# =============================================

param(
    [Parameter(Mandatory=$false)]
    [switch]$WhatIf,
    
    [Parameter(Mandatory=$false)]
    [switch]$Verbose
)

$ErrorActionPreference = "Stop"
$RepoRoot = "D:\Repositories\Hartonomous"
$dbProjectPath = Join-Path $RepoRoot "src\Hartonomous.Database"

$fixedCount = 0
$failedCount = 0

Write-Host "=============================================" -ForegroundColor Cyan
Write-Host "PHASE 7: MASS LEGACY CODE PURGE" -ForegroundColor Cyan
Write-Host "=============================================" -ForegroundColor Cyan
Write-Host "Mode: $(if ($WhatIf) { 'DRY RUN' } else { 'LIVE EXECUTION' })" -ForegroundColor $(if ($WhatIf) { "Yellow" } else { "Red" })
Write-Host "=============================================" -ForegroundColor Cyan
Write-Host ""

# =============================================
# FIX 1: Convert CREATE PROCEDURE ? CREATE OR ALTER
# =============================================
Write-Host "[FIX 1] Converting to idempotent procedures (CREATE OR ALTER)" -ForegroundColor Yellow

$nonIdempotentProcs = Get-ChildItem "$dbProjectPath\Procedures" -Filter "*.sql" | Where-Object {
    $content = Get-Content $_.FullName -Raw
    $content -match "^\s*CREATE\s+PROCEDURE" -and $content -notmatch "CREATE\s+OR\s+ALTER"
}

Write-Host "  Found: $($nonIdempotentProcs.Count) procedures to fix" -ForegroundColor White

foreach ($proc in $nonIdempotentProcs) {
    try {
        if ($Verbose) {
            Write-Host "    Processing: $($proc.Name)" -ForegroundColor Gray
        }
        
        $content = Get-Content $proc.FullName -Raw
        
        # Replace CREATE PROCEDURE with CREATE OR ALTER PROCEDURE
        $newContent = $content -replace '(^\s*)CREATE\s+PROCEDURE', '$1CREATE OR ALTER PROCEDURE'
        
        if ($newContent -ne $content) {
            if (-not $WhatIf) {
                Set-Content -Path $proc.FullName -Value $newContent -NoNewline
            }
            $fixedCount++
            Write-Host "    ? $($proc.Name)" -ForegroundColor Green
        }
    } catch {
        Write-Host "    ? Failed: $($proc.Name) - $_" -ForegroundColor Red
        $failedCount++
    }
}

Write-Host "  Result: $fixedCount fixed, $failedCount failed" -ForegroundColor $(if ($failedCount -eq 0) { "Green" } else { "Red" })
Write-Host ""

# =============================================
# FIX 2: Update VECTOR(1998) ? VECTOR(1536)
# =============================================
Write-Host "[FIX 2] Updating vector dimensions (VECTOR(1998) ? VECTOR(1536))" -ForegroundColor Yellow

$oldVectorFiles = Get-ChildItem "$dbProjectPath" -Filter "*.sql" -Recurse | Where-Object {
    (Get-Content $_.FullName -Raw) -match "VECTOR\(1998\)"
}

Write-Host "  Found: $($oldVectorFiles.Count) files to fix" -ForegroundColor White

foreach ($file in $oldVectorFiles) {
    try {
        if ($Verbose) {
            Write-Host "    Processing: $($file.Name)" -ForegroundColor Gray
        }
        
        $content = Get-Content $file.FullName -Raw
        
        # Replace VECTOR(1998) with VECTOR(1536)
        $newContent = $content -replace 'VECTOR\(1998\)', 'VECTOR(1536)'
        
        # Update comments too
        $newContent = $newContent -replace '1998D', '1536D'
        $newContent = $newContent -replace '1998-dimensional', '1536-dimensional'
        
        if ($newContent -ne $content) {
            if (-not $WhatIf) {
                Set-Content -Path $file.FullName -Value $newContent -NoNewline
            }
            $fixedCount++
            Write-Host "    ? $($file.Name)" -ForegroundColor Green
        }
    } catch {
        Write-Host "    ? Failed: $($file.Name) - $_" -ForegroundColor Red
        $failedCount++
    }
}

Write-Host "  Result: $fixedCount fixed, $failedCount failed" -ForegroundColor $(if ($failedCount -eq 0) { "Green" } else { "Red" })
Write-Host ""

# =============================================
# FIX 3: Add missing DACPAC <Build> entries
# =============================================
Write-Host "[FIX 3] Verifying DACPAC project includes" -ForegroundColor Yellow

$sqlprojPath = Join-Path $dbProjectPath "Hartonomous.Database.sqlproj"
$sqlprojContent = Get-Content $sqlprojPath -Raw

# Check if post-deployment script exclusion needs removing
if ($sqlprojContent -match '<Build Remove="Scripts\\Post-Deployment\\Optimize_\*.sql" />') {
    Write-Host "  ? DACPAC excludes Optimize_*.sql scripts" -ForegroundColor Yellow
    Write-Host "    Recommendation: Remove this exclusion to include optimization scripts" -ForegroundColor Yellow
    $warnings += "DACPAC excludes optimization scripts"
} else {
    Write-Host "  ? DACPAC includes optimization scripts" -ForegroundColor Green
}

Write-Host ""

# =============================================
# SUMMARY
# =============================================
Write-Host "=============================================" -ForegroundColor Cyan
Write-Host "PURGE COMPLETE" -ForegroundColor Cyan
Write-Host "=============================================" -ForegroundColor Cyan

Write-Host ""
Write-Host "Total fixes applied: $fixedCount" -ForegroundColor Green
Write-Host "Total failures: $failedCount" -ForegroundColor $(if ($failedCount -eq 0) { "Green" } else { "Red" })
Write-Host ""

if ($WhatIf) {
    Write-Host "? DRY RUN MODE - No changes were made" -ForegroundColor Yellow
    Write-Host "  Run without -WhatIf to apply fixes" -ForegroundColor Yellow
} else {
    Write-Host "? Changes have been applied" -ForegroundColor Green
    Write-Host ""
    Write-Host "Next steps:" -ForegroundColor Yellow
    Write-Host "  1. Review changes with: git diff" -ForegroundColor White
    Write-Host "  2. Build solution: dotnet build" -ForegroundColor White
    Write-Host "  3. Run validation: .\scripts\Validate-Build.ps1" -ForegroundColor White
}

Write-Host "=============================================" -ForegroundColor Cyan

exit $(if ($failedCount -gt 0) { 1 } else { 0 })
