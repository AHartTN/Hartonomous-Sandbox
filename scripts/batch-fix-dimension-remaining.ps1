# =============================================
# Batch Fix: Remaining Dimension Column References
# Replace ae.Dimension with ae.SpatialKey.STDimension() in WHERE clauses
# Phase 3 continuation from Phase 2 Dimension fixes
# =============================================

$ErrorActionPreference = "Stop"

Write-Host "=== Batch Fix: Remaining Dimension Column References ===`n" -ForegroundColor Cyan

# Find all procedures still referencing Dimension column
$procedures = Get-ChildItem -Path "src\Hartonomous.Database\Procedures" -Filter "*.sql" -Recurse |
    Where-Object { (Get-Content $_.FullName -Raw) -match "\bae\.Dimension\b" }

Write-Host "Found $($procedures.Count) procedures with remaining Dimension references:`n" -ForegroundColor Yellow
$procedures | ForEach-Object { Write-Host "  - $($_.Name)" }
Write-Host ""

$fixedCount = 0
$errorCount = 0

foreach ($proc in $procedures) {
    try {
        $content = Get-Content $proc.FullName -Raw
        $originalContent = $content

        # Replace Dimension in WHERE clauses with computed GEOMETRY method
        $content = $content -replace "\bae\.Dimension\s*=", "ae.SpatialKey.STDimension() ="
        $content = $content -replace "\bWHERE\s+Dimension\s*=", "WHERE SpatialKey.STDimension() ="

        # Replace in SELECT lists if aliased
        $content = $content -replace "\bae\.Dimension\b(?!\w)", "ae.SpatialKey.STDimension() AS Dimension"

        # Only update if changes were made
        if ($content -ne $originalContent) {
            Set-Content -Path $proc.FullName -Value $content -NoNewline
            Write-Host "[OK] Fixed: $($proc.Name)" -ForegroundColor Green
            $fixedCount++
        } else {
            Write-Host "[SKIP] No changes needed: $($proc.Name)" -ForegroundColor Gray
        }
    }
    catch {
        Write-Host "[ERROR] Failed to fix $($proc.Name): $_" -ForegroundColor Red
        $errorCount++
    }
}

Write-Host "`n=== Summary ===" -ForegroundColor Cyan
Write-Host "Fixed: $fixedCount procedures" -ForegroundColor Green
Write-Host "Errors: $errorCount procedures" -ForegroundColor $(if ($errorCount -gt 0) { "Red" } else { "Green" })

# Verification
Write-Host "`nVerifying fix..." -ForegroundColor Cyan
$remaining = Get-ChildItem -Path "src\Hartonomous.Database\Procedures" -Filter "*.sql" -Recurse |
    Where-Object { (Get-Content $_.FullName -Raw) -match "\bae\.Dimension\b" }

if ($remaining.Count -eq 0) {
    Write-Host "[SUCCESS] No ae.Dimension references remain" -ForegroundColor Green
} else {
    Write-Host "[WARNING] $($remaining.Count) files still contain ae.Dimension:" -ForegroundColor Yellow
    $remaining | ForEach-Object { Write-Host "  - $($_.Name)" -ForegroundColor Yellow }
}

Write-Host "`nDone! Run 'git diff' to review changes." -ForegroundColor Cyan
Write-Host "Note: v5 removed Dimension column. Use SpatialKey.STDimension() instead." -ForegroundColor Yellow
