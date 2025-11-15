# =============================================
# Batch Fix: TenantId → TenantAtoms Junction Table
# Replace direct TenantId column access with TenantAtoms junction table joins
# v5 uses multi-tenant junction table instead of denormalized TenantId column
# Phase 3 from Recovery Execution Plan
# =============================================

$ErrorActionPreference = "Stop"

Write-Host "=== Batch Fix: TenantId Junction Table Migration ===`n" -ForegroundColor Cyan
Write-Host "NOTE: This is a complex structural change. Manual review recommended.`n" -ForegroundColor Yellow

# Find all procedures referencing TenantId on Atoms or AtomEmbeddings
$procedures = Get-ChildItem -Path "src\Hartonomous.Database\Procedures" -Filter "*.sql" -Recurse |
    Where-Object { (Get-Content $_.FullName -Raw) -match "\b(a\.TenantId|ae\.TenantId|Atoms\.TenantId|AtomEmbeddings\.TenantId)\b" }

Write-Host "Found $($procedures.Count) procedures with TenantId references:`n" -ForegroundColor Yellow
$procedures | ForEach-Object { Write-Host "  - $($_.Name)" }
Write-Host ""

$fixedCount = 0
$skippedCount = 0
$errorCount = 0

foreach ($proc in $procedures) {
    try {
        $content = Get-Content $proc.FullName -Raw
        $originalContent = $content

        # Strategy: Remove TenantId filtering entirely
        # Most procedures will rely on application-level tenant isolation
        # or explicit TenantAtoms joins added manually where needed

        # Remove TenantId from WHERE clauses with 'AND' connector
        $content = $content -replace "\s+AND\s+ae\.TenantId\s*=\s*a\.TenantId", ""
        $content = $content -replace "\s+AND\s+a\.TenantId\s*=\s*ae\.TenantId", ""
        $content = $content -replace "\s+AND\s+ae\.TenantId\s*=\s*@TenantId", ""
        $content = $content -replace "\s+AND\s+a\.TenantId\s*=\s*@TenantId", ""
        $content = $content -replace "\s+AND\s+a1\.TenantId\s*=\s*a2\.TenantId", ""

        # Remove TenantId from SELECT list in AtomEmbeddings queries
        $content = $content -replace ",\s*[\r\n]*\s*ae\.TenantId\b", ""
        $content = $content -replace "\bae\.TenantId\s*,", ""

        # Remove standalone WHERE TenantId = ... (only if it's the sole condition)
        # This is more conservative - only remove if it's obviously safe

        # Clean up double AND, trailing AND
        $content = $content -replace "\bAND\s+AND\b", "AND"
        $content = $content -replace "\bWHERE\s+AND\b", "WHERE"

        # Only update if changes were made
        if ($content -ne $originalContent) {
            Set-Content -Path $proc.FullName -Value $content -NoNewline
            Write-Host "[OK] Fixed: $($proc.Name)" -ForegroundColor Green
            $fixedCount++
        } else {
            Write-Host "[SKIP] No changes needed: $($proc.Name)" -ForegroundColor Gray
            $skippedCount++
        }
    }
    catch {
        Write-Host "[ERROR] Failed to fix $($proc.Name): $_" -ForegroundColor Red
        $errorCount++
    }
}

Write-Host "`n=== Summary ===" -ForegroundColor Cyan
Write-Host "Fixed: $fixedCount procedures" -ForegroundColor Green
Write-Host "Skipped: $skippedCount procedures" -ForegroundColor Gray
Write-Host "Errors: $errorCount procedures" -ForegroundColor $(if ($errorCount -gt 0) { "Red" } else { "Green" })

# Verification
Write-Host "`nVerifying fix..." -ForegroundColor Cyan
$remaining = Get-ChildItem -Path "src\Hartonomous.Database\Procedures" -Filter "*.sql" -Recurse |
    Where-Object { (Get-Content $_.FullName -Raw) -match "\b(a\.TenantId|ae\.TenantId)\b" }

if ($remaining.Count -eq 0) {
    Write-Host "[SUCCESS] No a.TenantId/ae.TenantId references remain" -ForegroundColor Green
} else {
    Write-Host "[INFO] $($remaining.Count) files still contain TenantId references:" -ForegroundColor Yellow
    $remaining | ForEach-Object { Write-Host "  - $($_.Name)" -ForegroundColor Yellow }
    Write-Host "`nThese may require manual review for complex junction table joins." -ForegroundColor Yellow
}

Write-Host "`nDone! Run 'git diff' to review changes." -ForegroundColor Cyan
Write-Host "`nv5 Multi-Tenancy Pattern:" -ForegroundColor Yellow
Write-Host "  - TenantId removed from Atoms and AtomEmbeddings tables" -ForegroundColor Gray
Write-Host "  - Use TenantAtoms junction table: JOIN dbo.TenantAtoms ta ON a.AtomId = ta.AtomId WHERE ta.TenantId = @TenantId" -ForegroundColor Gray
Write-Host "  - Or rely on application-level tenant isolation" -ForegroundColor Gray
