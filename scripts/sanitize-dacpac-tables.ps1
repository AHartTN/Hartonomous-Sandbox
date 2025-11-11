<#
.SYNOPSIS
    Sanitize table scripts for DACPAC compilation by removing runtime constructs.

.DESCRIPTION
    DACPAC projects require pure declarative DDL. This script removes:
    - GO batch separators
    - IF EXISTS/IF NOT EXISTS guards
    - DROP TABLE/INDEX statements
    - PRINT messages
    - Schema creation blocks (handled by Pre-Deployment script)

.PARAMETER TablePath
    Path to the Tables directory. Defaults to src/Hartonomous.Database/Tables.

.PARAMETER DryRun
    Preview changes without modifying files.
#>
param(
    [string]$TablePath = "$PSScriptRoot\..\src\Hartonomous.Database\Tables",
    [switch]$DryRun
)

$ErrorActionPreference = "Stop"

# Get all .sql files in Tables directory
$tableFiles = Get-ChildItem -Path $TablePath -Filter "*.sql" -File

Write-Host "Found $($tableFiles.Count) table scripts in $TablePath" -ForegroundColor Cyan

foreach ($file in $tableFiles) {
    Write-Host "`n Processing: $($file.Name)" -ForegroundColor Yellow
    
    $content = Get-Content -Path $file.FullName -Raw
    $original = $content
    
    # Remove GO batch separators (standalone lines)
    $content = $content -replace '(?m)^\s*GO\s*$', ''
    
    # Remove IF EXISTS schema creation blocks
    # Pattern: IF NOT EXISTS (SELECT 1 FROM sys.schemas WHERE name = ...) ... CREATE SCHEMA ... GO
    $content = $content -replace '(?s)IF NOT EXISTS\s*\(\s*SELECT\s+1\s+FROM\s+sys\.schemas.*?CREATE SCHEMA.*?(\r?\n)', ''
    
    # Remove IF NOT EXISTS index guards (multi-line with BEGIN/END)
    # Pattern: IF NOT EXISTS (...) BEGIN CREATE INDEX ... END
    $content = $content -replace '(?s)IF NOT EXISTS\s*\([^)]*sys\.indexes[^)]*\)\s*BEGIN\s*(CREATE\s+(?:UNIQUE\s+)?INDEX[^;]+;?)\s*END', '$1'
    
    # Remove IF NOT EXISTS index guards (single-line without BEGIN/END)
    $content = $content -replace '(?m)^IF NOT EXISTS\s*\([^)]*sys\.indexes[^)]*\)\s*$', ''
    
    # Remove DROP TABLE IF EXISTS statements
    $content = $content -replace '(?m)^\s*DROP\s+TABLE\s+IF\s+EXISTS\s+.*?;?\s*$', ''
    
    # Remove PRINT statements
    $content = $content -replace "(?m)^\s*PRINT\s+'[^']*';\s*$", ''
    
    # Remove excessive blank lines (more than 2 consecutive)
    $content = $content -replace '(?m)(\r?\n){3,}', "`n`n"
    
    # Trim trailing whitespace
    $content = $content.Trim()
    
    if ($content -ne $original) {
        if ($DryRun) {
            Write-Host "  [DRY RUN] Would modify $($file.Name)" -ForegroundColor Magenta
        } else {
            Set-Content -Path $file.FullName -Value $content -NoNewline -Encoding UTF8
            Write-Host "  ✓ Sanitized $($file.Name)" -ForegroundColor Green
        }
    } else {
        Write-Host "  - No changes needed for $($file.Name)" -ForegroundColor Gray
    }
}

Write-Host "`n✓ Table sanitization complete." -ForegroundColor Green
if ($DryRun) {
    Write-Host "Re-run without -DryRun to apply changes." -ForegroundColor Yellow
}
