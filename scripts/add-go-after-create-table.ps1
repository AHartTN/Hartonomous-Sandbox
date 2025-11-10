#!/usr/bin/env pwsh
<#
.SYNOPSIS
Adds GO batch separators after CREATE TABLE statements in SQL files.

.DESCRIPTION
DACPAC requires GO after each CREATE statement. This script adds GO after CREATE TABLE
closing parenthesis if it's missing.
#>

param(
    [string]$Path = "d:\Repositories\Hartonomous\src\Hartonomous.Database\Tables"
)

function Add-GoAfterCreateTable {
    param([string]$Content)
    
    # Pattern: CREATE TABLE ... ); followed by CREATE INDEX (no GO between)
    $Content = $Content -replace '(?ms)(\);)\s*\n\s*(CREATE\s+(INDEX|UNIQUE))', "`$1`nGO`n`n`$2"
    
    # Pattern: CREATE TABLE ... ); at end of file (no trailing GO)
    if ($Content -match 'CREATE TABLE.*\);[\s]*$') {
        $Content = $Content.TrimEnd() + "`nGO`n"
    }
    
    return $Content
}

$sqlFiles = Get-ChildItem -Path $Path -Filter "*.sql"
Write-Host "Processing $($sqlFiles.Count) files...`n" -ForegroundColor Yellow

$modified = 0
foreach ($file in $sqlFiles) {
    $content = Get-Content $file.FullName -Raw -Encoding UTF8
    $updated = Add-GoAfterCreateTable $content
    
    if ($updated -ne $content) {
        Set-Content -Path $file.FullName -Value $updated -Encoding UTF8 -NoNewline
        Write-Host "✓ $($file.Name)" -ForegroundColor Green
        $modified++
    }
}

Write-Host "`nModified $modified files" -ForegroundColor Cyan
