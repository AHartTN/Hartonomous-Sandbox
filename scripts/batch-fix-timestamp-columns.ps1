# =============================================
# Batch Fix: Timestamp Column Standardization
# LastComputedUtc/UpdatedAt/CreatedUtc → CreatedAt
# Phase 6.4 from Recovery Execution Plan
# =============================================

$ErrorActionPreference = "Stop"

Write-Host "=== Batch Fix: Timestamp Column Standardization ===" -ForegroundColor Cyan
Write-Host ""

# Find all procedures referencing old timestamp columns
$procedures = Get-ChildItem -Path "src\Hartonomous.Database\Procedures" -Filter "*.sql" -Recurse |
    Where-Object { (Get-Content $_.FullName -Raw) -match "LastComputedUtc|UpdatedAt|CreatedUtc" }

Write-Host "Found $($procedures.Count) procedures referencing old timestamp columns:" -ForegroundColor Yellow
$procedures | ForEach-Object { Write-Host "  - $($_.Name)" }
Write-Host ""

$fixedCount = 0
$errorCount = 0

foreach ($proc in $procedures) {
    try {
        $content = Get-Content $proc.FullName -Raw

        # Replace old timestamp column names with CreatedAt
        $updated = $content -replace "\bLastComputedUtc\b", "CreatedAt"
        $updated = $updated -replace "\bUpdatedAt\b", "CreatedAt"
        $updated = $updated -replace "\bCreatedUtc\b", "CreatedAt"

        # Replace GETUTCDATE() with SYSUTCDATETIME() (v5 standard)
        $updated = $updated -replace "\bGETUTCDATE\(\)", "SYSUTCDATETIME()"
        $updated = $updated -replace "\bGETDATE\(\)", "SYSDATETIME()"

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
Write-Host "Note: Temporal tables use CreatedAt (GENERATED ALWAYS AS ROW START)" -ForegroundColor Yellow
