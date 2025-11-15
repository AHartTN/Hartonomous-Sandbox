# =============================================
# Batch Fix: EmbeddingType Column Removal
# Remove references to deleted EmbeddingType column from AtomEmbeddings
# v5 uses ModelId to distinguish embedding types
# Phase 3 from Recovery Execution Plan
# =============================================

$ErrorActionPreference = "Stop"

Write-Host "=== Batch Fix: EmbeddingType Column Removal ===`n" -ForegroundColor Cyan

# Find all procedures referencing EmbeddingType
$procedures = Get-ChildItem -Path "src\Hartonomous.Database\Procedures" -Filter "*.sql" -Recurse |
    Where-Object { (Get-Content $_.FullName -Raw) -match "\bEmbeddingType\b" }

Write-Host "Found $($procedures.Count) procedures referencing EmbeddingType:`n" -ForegroundColor Yellow
$procedures | ForEach-Object { Write-Host "  - $($_.Name)" }
Write-Host ""

$fixedCount = 0
$errorCount = 0

foreach ($proc in $procedures) {
    try {
        $content = Get-Content $proc.FullName -Raw
        $originalContent = $content

        # Remove EmbeddingType from temp table definitions (entire line)
        $content = $content -replace "^\s*EmbeddingType\s+[A-Z]+.*,?\s*[\r\n]+", "", "Multiline"
        $content = $content -replace ",\s*[\r\n]+\s*EmbeddingType\s+[A-Z]+.*(?=[\r\n])", ""

        # Remove EmbeddingType from SELECT lists
        $content = $content -replace ",\s*[\r\n]+\s*ae\.EmbeddingType\b", ""
        $content = $content -replace "\bae\.EmbeddingType\s*,", ""

        # Remove EmbeddingType from WHERE clause conditions
        # Pattern: AND (@embedding_type IS NULL OR ae.EmbeddingType = @embedding_type)
        $content = $content -replace "\s*AND\s+\(\s*@embedding_type\s+IS\s+NULL\s+OR\s+ae\.EmbeddingType\s*=\s*@embedding_type\s*\)", ""

        # Clean up any double commas or trailing commas that may result
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
    Where-Object { (Get-Content $_.FullName -Raw) -match "\bae\.EmbeddingType\b" }

if ($remaining.Count -eq 0) {
    Write-Host "[SUCCESS] No ae.EmbeddingType references remain" -ForegroundColor Green
} else {
    Write-Host "[WARNING] $($remaining.Count) files still contain ae.EmbeddingType:" -ForegroundColor Yellow
    $remaining | ForEach-Object { Write-Host "  - $($_.Name)" -ForegroundColor Yellow }
}

Write-Host "`nDone! Run 'git diff' to review changes." -ForegroundColor Cyan
Write-Host "Note: v5 removed EmbeddingType column. Use ModelId to distinguish embedding models." -ForegroundColor Yellow
