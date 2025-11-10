#!/usr/bin/env pwsh
<#
.SYNOPSIS
    Splits multi-procedure/function SQL files into individual files for DACPAC compatibility.

.DESCRIPTION
    DACPAC requires one database object per file. This script:
    1. Reads each SQL file in Procedures directory
    2. Identifies CREATE OR ALTER PROCEDURE/FUNCTION statements
    3. Converts them to pure CREATE statements
    4. Splits into individual files named after the object
    5. Backs up originals to Procedures_Original

.NOTES
    This is a destructive operation. Originals are backed up first.
#>

param(
    [string]$SourceDir = "src\Hartonomous.Database\Procedures",
    [string]$BackupDir = "src\Hartonomous.Database\Procedures_Original",
    [switch]$DryRun
)

$ErrorActionPreference = "Stop"

# Ensure we're in repo root
if (-not (Test-Path "Hartonomous.sln")) {
    throw "Must run from repository root"
}

Write-Host "=== DACPAC Procedure Splitter ===" -ForegroundColor Cyan
Write-Host ""

# Create backup directory
if (-not (Test-Path $BackupDir)) {
    New-Item -ItemType Directory -Path $BackupDir -Force | Out-Null
    Write-Host "Created backup directory: $BackupDir" -ForegroundColor Green
}

# Get all SQL files
$files = Get-ChildItem $SourceDir -Filter "*.sql" -File

$totalFiles = $files.Count
$totalObjectsFound = 0
$totalFilesSplit = 0

Write-Host "Processing $totalFiles files..." -ForegroundColor Yellow
Write-Host ""

foreach ($file in $files) {
    $content = Get-Content $file.FullName -Raw
    
    # Find all CREATE OR ALTER PROCEDURE/FUNCTION statements
    $pattern = '(?s)CREATE\s+OR\s+ALTER\s+(PROCEDURE|FUNCTION)\s+(\[?[\w\.]+\]?)[\s\(].*?(?=CREATE\s+OR\s+ALTER\s+(PROCEDURE|FUNCTION)|$)'
    $matches = [regex]::Matches($content, $pattern)
    
    if ($matches.Count -le 1) {
        # Single object file or no matches - convert CREATE OR ALTER to CREATE but don't split
        if ($content -match 'CREATE\s+OR\s+ALTER\s+(PROCEDURE|FUNCTION)') {
            Write-Host "  $($file.Name): Single object, converting to CREATE" -ForegroundColor Gray
            
            if (-not $DryRun) {
                # Backup original
                Copy-Item $file.FullName -Destination (Join-Path $BackupDir $file.Name) -Force
                
                # Convert CREATE OR ALTER to CREATE
                $newContent = $content -replace 'CREATE\s+OR\s+ALTER\s+(PROCEDURE|FUNCTION)', 'CREATE $1'
                
                # Ensure proper GO separator at end
                if ($newContent -notmatch 'GO\s*$') {
                    $newContent += "`nGO`n"
                }
                
                Set-Content -Path $file.FullName -Value $newContent -NoNewline
            }
            $totalObjectsFound++
        }
        continue
    }
    
    # Multi-object file - needs splitting
    Write-Host "  $($file.Name): $($matches.Count) objects - splitting..." -ForegroundColor Yellow
    
    if (-not $DryRun) {
        # Backup original
        Copy-Item $file.FullName -Destination (Join-Path $BackupDir $file.Name) -Force
        
        # Delete original (we'll create new split files)
        Remove-Item $file.FullName -Force
    }
    
    $objectIndex = 0
    foreach ($match in $matches) {
        $objectIndex++
        $objectType = $match.Groups[1].Value
        $objectName = $match.Groups[2].Value -replace '\[|\]', ''
        $objectContent = $match.Value
        
        # Convert CREATE OR ALTER to CREATE
        $objectContent = $objectContent -replace 'CREATE\s+OR\s+ALTER\s+(PROCEDURE|FUNCTION)', 'CREATE $1'
        
        # Ensure proper GO separator
        if ($objectContent -notmatch 'GO\s*$') {
            $objectContent += "`nGO`n"
        }
        
        # Determine new filename
        $newFileName = "$objectName.sql"
        $newFilePath = Join-Path $SourceDir $newFileName
        
        Write-Host "    -> Creating $newFileName" -ForegroundColor Green
        
        if (-not $DryRun) {
            # Add header comment
            $header = "-- Auto-split from $($file.Name)`n-- Object: $objectType $objectName`n`n"
            $finalContent = $header + $objectContent
            
            Set-Content -Path $newFilePath -Value $finalContent -NoNewline
        }
        
        $totalObjectsFound++
    }
    
    $totalFilesSplit++
}

Write-Host ""
Write-Host "=== Summary ===" -ForegroundColor Cyan
Write-Host "Files processed: $totalFiles"
Write-Host "Files split: $totalFilesSplit"
Write-Host "Total objects: $totalObjectsFound"
Write-Host "Backup location: $BackupDir"

if ($DryRun) {
    Write-Host ""
    Write-Host "DRY RUN - No changes made. Remove -DryRun to execute." -ForegroundColor Yellow
} else {
    Write-Host ""
    Write-Host "Split complete! Original files backed up." -ForegroundColor Green
    Write-Host ""
    Write-Host "Next steps:" -ForegroundColor Cyan
    Write-Host "1. Remove procedure exclusion from .sqlproj: <Build Remove='Procedures\**' />"
    Write-Host "2. Rebuild DACPAC: dotnet build src\Hartonomous.Database\Hartonomous.Database.sqlproj -c Release"
    Write-Host "3. Verify: Check bin\Release\Hartonomous.Database.dacpac"
}
