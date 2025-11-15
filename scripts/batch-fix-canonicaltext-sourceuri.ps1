# =============================================
# Batch Fix: CanonicalText/SourceUri Column Removal
# Remove references to deleted columns that violated 64-byte atomic limit
# v5 enforces strict atomic decomposition at schema level
# Phase 3 from Recovery Execution Plan
# =============================================

$ErrorActionPreference = "Stop"

Write-Host "=== Batch Fix: CanonicalText/SourceUri Removal ===`n" -ForegroundColor Cyan

# Find all procedures referencing these removed columns
$procedures = Get-ChildItem -Path "src\Hartonomous.Database\Procedures" -Filter "*.sql" -Recurse |
    Where-Object { (Get-Content $_.FullName -Raw) -match "\b(CanonicalText|SourceUri)\b" }

Write-Host "Found $($procedures.Count) procedures referencing CanonicalText/SourceUri:`n" -ForegroundColor Yellow
$procedures | ForEach-Object { Write-Host "  - $($_.Name)" }
Write-Host ""

$fixedCount = 0
$errorCount = 0

foreach ($proc in $procedures) {
    try {
        $content = Get-Content $proc.FullName -Raw
        $originalContent = $content

        # Remove CanonicalText/SourceUri from temp table definitions (entire line)
        $content = $content -replace "^\s*CanonicalText\s+[A-Z]+.*,?\s*[\r\n]+", "", "Multiline"
        $content = $content -replace "^\s*SourceUri\s+[A-Z]+.*,?\s*[\r\n]+", "", "Multiline"
        $content = $content -replace ",\s*[\r\n]+\s*CanonicalText\s+[A-Z]+.*(?=[\r\n])", ""
        $content = $content -replace ",\s*[\r\n]+\s*SourceUri\s+[A-Z]+.*(?=[\r\n])", ""

        # Remove from SELECT lists (with alias 'a.')
        $content = $content -replace ",\s*[\r\n]+\s*a\.CanonicalText\b", ""
        $content = $content -replace ",\s*[\r\n]+\s*a\.SourceUri\b", ""
        $content = $content -replace "\ba\.CanonicalText\s*,", ""
        $content = $content -replace "\ba\.SourceUri\s*,", ""

        # Clean up any double commas or trailing commas
        $content = $content -replace ",\s*,", ","
        $content = $content -replace ",\s*(?=FROM|WHERE|ORDER|GROUP)", ""

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
    Where-Object { (Get-Content $_.FullName -Raw) -match "\ba\.(CanonicalText|SourceUri)\b" }

if ($remaining.Count -eq 0) {
    Write-Host "[SUCCESS] No a.CanonicalText/SourceUri references remain" -ForegroundColor Green
} else {
    Write-Host "[WARNING] $($remaining.Count) files still contain these references:" -ForegroundColor Yellow
    $remaining | ForEach-Object { Write-Host "  - $($_.Name)" -ForegroundColor Yellow }
}

Write-Host "`nDone! Run 'git diff' to review changes." -ForegroundColor Cyan
Write-Host "Note: v5 removed these columns - they violated the 64-byte atomic limit." -ForegroundColor Yellow
Write-Host "      Use AtomicValue (VARBINARY(64)) or compose from AtomCompositions." -ForegroundColor Yellow
