#!/usr/bin/env pwsh
$ErrorActionPreference = "Stop"

Write-Host "Fixing FK REFERENCES to singular table names..." -ForegroundColor Cyan

$files = Get-ChildItem -Path "src/Hartonomous.Database" -Include "*.sql" -Recurse

foreach ($file in $files) {
    $content = Get-Content $file.FullName -Raw
    $original = $content

    # Fix REFERENCES to plural tables
    $content = $content -replace 'REFERENCES \[dbo\]\.\[Models\]', 'REFERENCES [dbo].[Model]'
    $content = $content -replace 'REFERENCES \[dbo\]\.\[Atoms\]', 'REFERENCES [dbo].[Atom]'
    $content = $content -replace 'REFERENCES \[dbo\]\.\[AtomEmbeddings\]', 'REFERENCES [dbo].[AtomEmbedding]'
    $content = $content -replace 'REFERENCES \[dbo\]\.\[AtomRelations\]', 'REFERENCES [dbo].[AtomRelation]'

    if ($content -ne $original) {
        Set-Content -Path $file.FullName -Value $content -NoNewline
        Write-Host "  Fixed: $($file.Name)" -ForegroundColor Gray
    }
}

Write-Host "FK references fixed" -ForegroundColor Green
exit 0
