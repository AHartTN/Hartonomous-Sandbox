# =============================================
# Batch Fix: Dimension Column → SpatialKey.STDimension()
# Phase 6.3 from Recovery Execution Plan
# =============================================

$ErrorActionPreference = "Stop"

Write-Host "=== Batch Fix: Dimension Column Replacement ===" -ForegroundColor Cyan
Write-Host ""

# Find all procedures referencing Dimension column
$procedures = Get-ChildItem -Path "src\Hartonomous.Database\Procedures" -Filter "*.sql" -Recurse |
    Where-Object { (Get-Content $_.FullName -Raw) -match "\bDimension\b" }

Write-Host "Found $($procedures.Count) procedures referencing Dimension:" -ForegroundColor Yellow
$procedures | ForEach-Object { Write-Host "  - $($_.Name)" }
Write-Host ""

$fixedCount = 0
$errorCount = 0

foreach ($proc in $procedures) {
    try {
        $content = Get-Content $proc.FullName -Raw

        # Replace SELECT Dimension with computed column
        $updated = $content -replace "SELECT\s+Dimension\s+FROM\s+AtomEmbeddings", "SELECT SpatialKey.STDimension() AS Dimension FROM AtomEmbeddings"
        $updated = $updated -replace "SELECT\s+ae\.Dimension\b", "SELECT ae.SpatialKey.STDimension() AS Dimension"

        # Replace WHERE clauses referencing Dimension
        $updated = $updated -replace "WHERE\s+Dimension\s*=", "WHERE SpatialKey.STDimension() ="
        $updated = $updated -replace "WHERE\s+ae\.Dimension\s*=", "WHERE ae.SpatialKey.STDimension() ="

        # Replace standalone Dimension column references in SELECT lists
        $updated = $updated -replace ",\s*Dimension\s*,", ", SpatialKey.STDimension() AS Dimension,"
        $updated = $updated -replace ",\s*ae\.Dimension\b", ", ae.SpatialKey.STDimension() AS Dimension"

        # Only update if changes were made
        if ($content -ne $updated) {
            Set-Content -Path $proc.FullName -Value $updated -NoNewline
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

Write-Host ""
Write-Host "=== Summary ===" -ForegroundColor Cyan
Write-Host "Fixed: $fixedCount procedures" -ForegroundColor Green
Write-Host "Errors: $errorCount procedures" -ForegroundColor $(if ($errorCount -gt 0) { "Red" } else { "Green" })

Write-Host ""
Write-Host "Done! Run 'git diff' to review changes." -ForegroundColor Cyan
Write-Host "Note: Some procedures may need manual review for complex Dimension usage." -ForegroundColor Yellow
