#!/usr/bin/env pwsh
<#
.SYNOPSIS
    Fixes remaining plural references in index names, comments, and extended properties
#>

$ErrorActionPreference = "Stop"

$databaseProjectPath = "src\Hartonomous.Database"

Write-Host "Fixing remaining plural references..." -ForegroundColor Cyan

# Fix all files
$sqlFiles = Get-ChildItem -Path $databaseProjectPath -Include "*.sql" -Recurse

foreach ($file in $sqlFiles) {
    $content = Get-Content $file.FullName -Raw
    $original = $content

    # Fix index names
    $content = $content -replace "IX_AtomsHistory_", "IX_AtomHistory_"
    $content = $content -replace "IX_BackgroundJobs_", "IX_BackgroundJob_"
    $content = $content -replace "IX_AtomRelations_", "IX_AtomRelation_"
    $content = $content -replace "IX_AtomEmbeddings_", "IX_AtomEmbedding_"
    $content = $content -replace "IX_Models_", "IX_Model_"
    $content = $content -replace "IX_ModelLayers_", "IX_ModelLayer_"

    # Fix statistics names
    $content = $content -replace "ST_Coordinates.*ON dbo\.AtomRelations", "ST_Coordinates ON dbo.AtomRelation"

    # Fix header comments
    $content = $content -replace "-- AtomsHistory:", "-- AtomHistory:"
    $content = $content -replace "BackgroundJobs:", "BackgroundJob:"

    # Fix extended property references
    $content = $content -replace "@level1name = N'AtomsHistory'", "@level1name = N'AtomHistory'"

    # Fix table references in index files
    $content = $content -replace "ON dbo\.AtomRelations \(", "ON dbo.AtomRelation ("

    if ($content -ne $original) {
        Set-Content -Path $file.FullName -Value $content -NoNewline
        Write-Host "  Updated: $($file.Name)" -ForegroundColor Gray
    }
}

# Rename index files from plural to singular
$indexPath = Join-Path $databaseProjectPath "Indexes"
if (Test-Path $indexPath) {
    $indexFiles = Get-ChildItem -Path $indexPath -Filter "dbo.AtomRelations.*.sql"

    foreach ($file in $indexFiles) {
        $newName = $file.Name -replace "dbo\.AtomRelations\.", "dbo.AtomRelation."
        $newPath = Join-Path $indexPath $newName

        Move-Item -Path $file.FullName -Destination $newPath -Force
        Write-Host "  Renamed index file: $($file.Name) → $newName" -ForegroundColor Gray
    }
}

# Update project file to reference renamed index files
$projFile = Join-Path $databaseProjectPath "Hartonomous.Database.sqlproj"
if (Test-Path $projFile) {
    $content = Get-Content $projFile -Raw
    $content = $content -replace "Indexes\\dbo\.AtomRelations\.", "Indexes\dbo.AtomRelation."
    Set-Content -Path $projFile -Value $content -NoNewline
    Write-Host "  Updated .sqlproj file" -ForegroundColor Gray
}

Write-Host "All remaining references fixed" -ForegroundColor Green

exit 0
