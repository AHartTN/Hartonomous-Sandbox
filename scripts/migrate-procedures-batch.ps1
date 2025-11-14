#!/usr/bin/env pwsh
<#
.SYNOPSIS
Batch migration of stored procedures to new schema (Hartonomous Core v5)

.DESCRIPTION
Applies regex-based transformations to update stored procedures for the new atomic decomposition schema.
Handles simple column renames and removal of deprecated concepts.
#>

param(
    [switch]$WhatIf = $false
)

$ErrorActionPreference = "Stop"

$proceduresPath = "src\Hartonomous.Database\Procedures"
$changedFiles = @()

# Get all procedures that need updates
$procedures = Get-ChildItem "$proceduresPath\*.sql" | Where-Object {
    $content = Get-Content $_.FullName -Raw
    ($content -match '\bSpatialGeometry\b') -or 
    ($content -match '\bSpatialBucket\b') -or 
    ($content -match '\bLastComputedUtc\b') -or
    ($content -match 'AND\s+a\.IsActive\s*=\s*1') -or
    ($content -match 'AND\s+a\.IsDeleted\s*=\s*0') -or
    ($content -match 'WHERE\s+IsActive\s*=\s*1') -or
    ($content -match 'WHERE\s+IsDeleted\s*=\s*0')
}

Write-Host "Found $($procedures.Count) procedures needing batch updates" -ForegroundColor Cyan

foreach ($proc in $procedures) {
    Write-Host "Processing $($proc.Name)..." -ForegroundColor Yellow
    
    $content = Get-Content $proc.FullName -Raw
    $originalContent = $content
    $changes = @()
    
    # 1. SpatialGeometry -> SpatialKey (simple column rename)
    if ($content -match '\bSpatialGeometry\b') {
        $content = $content -replace '\bSpatialGeometry\b', 'SpatialKey'
        $changes += "SpatialGeometry → SpatialKey"
    }
    
    # 2. SpatialBucketX/Y/Z -> HilbertValue
    if ($content -match '\bSpatialBucketX\b|\bSpatialBucketY\b|\bSpatialBucketZ\b') {
        # These need semantic transformation - skip for now
        Write-Host "  ⚠ Contains SpatialBucket coordinates - needs manual review" -ForegroundColor Magenta
    }
    
    # 3. SpatialBucket (single column) -> HilbertValue
    if ($content -match '\bSpatialBucket\b' -and $content -notmatch 'SpatialBucket[XYZ]') {
        $content = $content -replace '\bSpatialBucket\b', 'HilbertValue'
        $changes += "SpatialBucket → HilbertValue"
    }
    
    # 4. LastComputedUtc -> CreatedAt
    if ($content -match '\bLastComputedUtc\b') {
        $content = $content -replace '\bLastComputedUtc\b', 'CreatedAt'
        $changes += "LastComputedUtc → CreatedAt"
    }
    
    # 5. Remove IsActive = 1 filters (use temporal queries instead)
    if ($content -match 'AND\s+a\.IsActive\s*=\s*1') {
        $content = $content -replace 'AND\s+a\.IsActive\s*=\s*1', ''
        $changes += "Removed IsActive filter"
    }
    if ($content -match 'AND\s+IsActive\s*=\s*1') {
        $content = $content -replace 'AND\s+IsActive\s*=\s*1', ''
        $changes += "Removed IsActive filter"
    }
    
    # 6. Remove IsDeleted = 0 filters
    if ($content -match 'AND\s+a\.IsDeleted\s*=\s*0') {
        $content = $content -replace 'AND\s+a\.IsDeleted\s*=\s*0', ''
        $changes += "Removed IsDeleted filter"
    }
    if ($content -match 'AND\s+IsDeleted\s*=\s*0') {
        $content = $content -replace 'AND\s+IsDeleted\s*=\s*0', ''
        $changes += "Removed IsDeleted filter"
    }
    
    # Check if content changed
    if ($content -ne $originalContent) {
        if ($WhatIf) {
            Write-Host "  Would apply: $($changes -join ', ')" -ForegroundColor Green
        } else {
            Set-Content -Path $proc.FullName -Value $content -NoNewline
            Write-Host "  ✓ Applied: $($changes -join ', ')" -ForegroundColor Green
            $changedFiles += $proc.Name
        }
    } else {
        Write-Host "  No simple batch changes applicable" -ForegroundColor Gray
    }
}

Write-Host "`n========================================" -ForegroundColor Cyan
Write-Host "Batch Migration Complete" -ForegroundColor Cyan
Write-Host "Files changed: $($changedFiles.Count)" -ForegroundColor Cyan
if ($WhatIf) {
    Write-Host "(DRY RUN - no files actually modified)" -ForegroundColor Yellow
}
Write-Host "========================================`n" -ForegroundColor Cyan

if ($changedFiles.Count -gt 0) {
    Write-Host "Changed files:" -ForegroundColor White
    $changedFiles | ForEach-Object { Write-Host "  - $_" -ForegroundColor Gray }
}
