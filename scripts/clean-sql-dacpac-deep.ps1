#!/usr/bin/env pwsh
<#
.SYNOPSIS
Deep clean SQL files for DACPAC compilation by removing dynamic SQL constructs.

.DESCRIPTION
Removes IF blocks, BEGIN/END wrappers, DECLARE statements, and seed data that prevent DACPAC build.
Preserves pure CREATE statements, indexes, and constraints.

.PARAMETER Path
Path to SQL file or directory to clean. Defaults to current DACPAC project.
#>

param(
    [string]$Path = "d:\Repositories\Hartonomous\src\Hartonomous.Database"
)

function Remove-DynamicSqlWrapper {
    param([string]$Content)
    
    # Remove IF OBJECT_ID(...) wrappers with nested BEGIN/END
    $Content = $Content -replace '(?ms)IF\s+OBJECT_ID\s*\([^)]+\)\s+IS\s+(NOT\s+)?NULL\s*BEGIN\s*(.*?)\s*END\s*GO', '$2GO'
    
    # Remove IF EXISTS(...) wrappers
    $Content = $Content -replace '(?ms)IF\s+(NOT\s+)?EXISTS\s*\([^)]*SELECT[^)]*\)\s*BEGIN\s*(.*?)\s*END\s*GO', '$2GO'
    
    # Remove IF COL_LENGTH(...) wrappers (temporal column checks)
    $Content = $Content -replace '(?ms)IF\s+COL_LENGTH\s*\([^)]+\)\s+IS\s+(NOT\s+)?NULL\s*BEGIN\s*(.*?)\s*END\s*GO', ''
    
    # Remove seed data blocks (DECLARE + INSERT)
    $Content = $Content -replace '(?ms)DECLARE\s+@\w+\s+TABLE\s*\([^)]+\);.*?GO', ''
    
    # Remove standalone DECLARE statements
    $Content = $Content -replace '(?m)^\s*DECLARE\s+@.*?;?\s*$', ''
    
    # Remove INSERT statements (seed data)
    $Content = $Content -replace '(?ms)INSERT\s+INTO\s+[^;]+;', ''
    
    # Remove ALTER TABLE statements (temporal versioning, system_time)
    $Content = $Content -replace '(?ms)ALTER\s+TABLE\s+[^\n]+\s+SET\s*\(SYSTEM_VERSIONING[^)]*\)\s*;?\s*GO?', ''
    $Content = $Content -replace '(?ms)ALTER\s+TABLE\s+[^\n]+\s+ADD\s+ValidFrom[^;]+;\s*GO?', ''
    
    # Clean up multiple consecutive GOs
    $Content = $Content -replace '(?m)^\s*GO\s*\n\s*GO\s*$', 'GO'
    
    # Clean up trailing GO at end of file
    $Content = $Content -replace '(?m)^\s*GO\s*$\s*$', ''
    
    return $Content
}

function Clean-SqlFile {
    param(
        [string]$FilePath
    )
    
    Write-Host "Cleaning: $FilePath" -ForegroundColor Cyan
    
    $content = Get-Content $FilePath -Raw -Encoding UTF8
    $originalLength = $content.Length
    
    $cleaned = Remove-DynamicSqlWrapper $content
    
    if ($cleaned.Length -ne $originalLength) {
        Set-Content -Path $FilePath -Value $cleaned -Encoding UTF8 -NoNewline
        Write-Host "  ✓ Modified (saved $($originalLength - $cleaned.Length) bytes)" -ForegroundColor Green
    } else {
        Write-Host "  - No changes needed" -ForegroundColor Gray
    }
}

# Main execution
if (Test-Path $Path -PathType Container) {
    $sqlFiles = Get-ChildItem -Path $Path -Filter "*.sql" -Recurse
    Write-Host "Found $($sqlFiles.Count) SQL files to process`n" -ForegroundColor Yellow
    
    foreach ($file in $sqlFiles) {
        # Skip pre/post deployment scripts
        if ($file.Name -like "*Deployment*") {
            Write-Host "Skipping: $($file.FullName)" -ForegroundColor DarkGray
            continue
        }
        
        Clean-SqlFile -FilePath $file.FullName
    }
} elseif (Test-Path $Path -PathType Leaf) {
    Clean-SqlFile -FilePath $Path
} else {
    Write-Error "Path not found: $Path"
    exit 1
}

Write-Host "`nCleaning complete!" -ForegroundColor Green
