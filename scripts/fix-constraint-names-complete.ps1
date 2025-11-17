#!/usr/bin/env pwsh
$ErrorActionPreference = "Stop"

Write-Host "Fixing ALL constraint names to singular..." -ForegroundColor Cyan

$files = Get-ChildItem -Path "src/Hartonomous.Database" -Include "*.sql" -Recurse
$count = 0

foreach ($file in $files) {
    $content = Get-Content $file.FullName -Raw
    $original = $content

    # Fix PK constraint names
    $content = $content -creplace '\bPK_Atoms\b', 'PK_Atom'
    $content = $content -creplace '\bPK_Models\b', 'PK_Model'
    $content = $content -creplace '\bPK_AtomEmbeddings\b', 'PK_AtomEmbedding'
    $content = $content -creplace '\bPK_AtomCompositions\b', 'PK_AtomComposition'
    $content = $content -creplace '\bPK_AtomRelations\b', 'PK_AtomRelation'
    $content = $content -creplace '\bPK_WeightSnapshots\b', 'PK_WeightSnapshot'
    $content = $content -creplace '\bPK_([A-Z][a-zA-Z]+)s\b', 'PK_$1'

    # Fix FK constraint names (already partially done, but ensure complete)
    $content = $content -creplace '\bFK_([A-Z][a-zA-Z]+)s_([A-Z][a-zA-Z]+)s\b', 'FK_$1_$2'
    $content = $content -creplace '\bFK_([A-Z][a-zA-Z]+)s_([A-Z][a-zA-Z]+)\b', 'FK_$1_$2'

    # Fix index names
    $content = $content -creplace '\bIX_([A-Z][a-zA-Z]+)s_', 'IX_$1_'
    $content = $content -creplace '\bUQ_([A-Z][a-zA-Z]+)s_', 'UQ_$1_'
    $content = $content -creplace '\bCK_([A-Z][a-zA-Z]+)s_', 'CK_$1_'

    if ($content -ne $original) {
        Set-Content -Path $file.FullName -Value $content -NoNewline
        $count++
        Write-Host "  Fixed: $($file.Name)" -ForegroundColor Gray
    }
}

Write-Host "Fixed constraint names in $count files" -ForegroundColor Green
exit 0
