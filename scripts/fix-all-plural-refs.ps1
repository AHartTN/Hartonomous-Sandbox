#!/usr/bin/env pwsh
$ErrorActionPreference = "Stop"

Write-Host "Fixing ALL plural table references (bracketed and non-bracketed)..." -ForegroundColor Cyan

$files = Get-ChildItem -Path "src/Hartonomous.Database" -Include "*.sql" -Recurse
$count = 0

foreach ($file in $files) {
    $content = Get-Content $file.FullName -Raw
    $original = $content

    # Non-bracketed table names (dbo.TableName)
    $content = $content -creplace '\bdbo\.Models\b', 'dbo.Model'
    $content = $content -creplace '\bdbo\.Atoms\b', 'dbo.Atom'
    $content = $content -creplace '\bdbo\.AtomEmbeddings\b', 'dbo.AtomEmbedding'
    $content = $content -creplace '\bdbo\.AtomRelations\b', 'dbo.AtomRelation'
    $content = $content -creplace '\bdbo\.WeightSnapshots\b', 'dbo.WeightSnapshot'
    $content = $content -creplace '\bdbo\.InferenceRequests\b', 'dbo.InferenceRequest'

    # Bracketed table names ([dbo].[TableName])
    $content = $content -replace '\[dbo\]\.\[Models\]', '[dbo].[Model]'
    $content = $content -replace '\[dbo\]\.\[Atoms\]', '[dbo].[Atom]'
    $content = $content -replace '\[dbo\]\.\[AtomEmbeddings\]', '[dbo].[AtomEmbedding]'
    $content = $content -replace '\[dbo\]\.\[AtomRelations\]', '[dbo].[AtomRelation]'

    if ($content -ne $original) {
        Set-Content -Path $file.FullName -Value $content -NoNewline
        $count++
    }
}

Write-Host "Fixed $count files" -ForegroundColor Green
exit 0
