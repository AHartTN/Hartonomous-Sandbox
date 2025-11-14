# =============================================
# Batch Fix: EmbeddingVector → SpatialKey Column Rename
# Phase 2 from Recovery Execution Plan
# =============================================

$ErrorActionPreference = "Stop"

Write-Host "=== Batch Fix: EmbeddingVector → SpatialKey Rename ===" -ForegroundColor Cyan
Write-Host ""

# Find all procedures referencing EmbeddingVector
$procedures = Get-ChildItem -Path "src\Hartonomous.Database\Procedures" -Filter "*.sql" -Recurse |
    Where-Object { (Get-Content $_.FullName -Raw) -match "EmbeddingVector" }

Write-Host "Found $($procedures.Count) procedures referencing EmbeddingVector:" -ForegroundColor Yellow
$procedures | ForEach-Object { Write-Host "  - $($_.Name)" }
Write-Host ""

$fixedCount = 0
$errorCount = 0

foreach ($proc in $procedures) {
    try {
        $content = Get-Content $proc.FullName -Raw

        # Perform column rename
        $updated = $content -replace "\bEmbeddingVector\b", "SpatialKey"

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

# Verification: Check that no EmbeddingVector references remain
Write-Host ""
Write-Host "Verifying fix..." -ForegroundColor Cyan
$remaining = Get-ChildItem -Path "src\Hartonomous.Database\Procedures" -Filter "*.sql" -Recurse |
    Where-Object { (Get-Content $_.FullName -Raw) -match "EmbeddingVector" }

if ($remaining.Count -eq 0) {
    Write-Host "[SUCCESS] No EmbeddingVector references remain" -ForegroundColor Green
} else {
    Write-Host "[WARNING] $($remaining.Count) files still contain EmbeddingVector:" -ForegroundColor Yellow
    $remaining | ForEach-Object { Write-Host "  - $($_.Name)" -ForegroundColor Yellow }
}

Write-Host ""
Write-Host "Done! Run 'git diff' to review changes." -ForegroundColor Cyan
