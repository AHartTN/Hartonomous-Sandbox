# =============================================
# Batch Fix: ContentType/SourceType → Modality Column Mapping
# Maps v4 content type columns to v5 Modality/Subtype design
# Phase 3 from Recovery Execution Plan
# =============================================

$ErrorActionPreference = "Stop"

Write-Host "=== Batch Fix: ContentType/SourceType → Modality ===`n" -ForegroundColor Cyan

# Find all procedures referencing old content type columns
$procedures = Get-ChildItem -Path "src\Hartonomous.Database\Procedures" -Filter "*.sql" -Recurse |
    Where-Object { (Get-Content $_.FullName -Raw) -match "\b(ContentType|SourceType)\b" }

Write-Host "Found $($procedures.Count) procedures referencing ContentType/SourceType:`n" -ForegroundColor Yellow
$procedures | ForEach-Object { Write-Host "  - $($_.Name)" }
Write-Host ""

$fixedCount = 0
$errorCount = 0

foreach ($proc in $procedures) {
    try {
        $content = Get-Content $proc.FullName -Raw
        $originalContent = $content

        # Replace column references in WHERE clauses (with alias 'a.')
        $content = $content -replace "\ba\.SourceType\s*=", "a.Modality ="
        $content = $content -replace "\ba\.ContentType\s*=", "a.Modality ="

        # Replace column references in SELECT lists (with alias 'a.')
        $content = $content -replace "\ba\.SourceType\b(?![\w])", "a.Modality"
        $content = $content -replace "\ba\.ContentType\b(?![\w])", "a.Modality"

        # Remove SourceType/ContentType from temp table definitions (they're redundant)
        # Pattern: Line with just SourceType or ContentType declaration
        $content = $content -replace "^\s*SourceType\s+[A-Z]+.*,?\s*[\r\n]+", "", "Multiline"
        $content = $content -replace "^\s*ContentType\s+[A-Z]+.*,?\s*[\r\n]+", "", "Multiline"
        $content = $content -replace ",\s*[\r\n]+\s*SourceType\s+[A-Z]+.*(?=[\r\n])", ""
        $content = $content -replace ",\s*[\r\n]+\s*ContentType\s+[A-Z]+.*(?=[\r\n])", ""

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

# Verification: Check that minimal ContentType/SourceType references remain
Write-Host "`nVerifying fix..." -ForegroundColor Cyan
$remaining = Get-ChildItem -Path "src\Hartonomous.Database\Procedures" -Filter "*.sql" -Recurse |
    Where-Object { (Get-Content $_.FullName -Raw) -match "\b(a\.ContentType|a\.SourceType|ae\.ContentType|ae\.SourceType)\b" }

if ($remaining.Count -eq 0) {
    Write-Host "[SUCCESS] No problematic ContentType/SourceType references remain" -ForegroundColor Green
} else {
    Write-Host "[INFO] $($remaining.Count) files still contain ContentType/SourceType:" -ForegroundColor Yellow
    $remaining | ForEach-Object { Write-Host "  - $($_.Name)" -ForegroundColor Yellow }
    Write-Host "  (May need manual review for complex cases)" -ForegroundColor Gray
}

Write-Host "`nDone! Run 'git diff' to review changes." -ForegroundColor Cyan
Write-Host "Note: v5 uses Modality (required) and Subtype (optional) for content classification" -ForegroundColor Yellow
