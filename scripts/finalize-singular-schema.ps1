#!/usr/bin/env pwsh
<#
.SYNOPSIS
    FINAL schema singularization - fixes ALL naming inconsistencies
.DESCRIPTION
    Production-grade, idempotent script to ensure complete singular naming
#>
param([switch]$WhatIf)

$ErrorActionPreference = "Stop"

Write-Host "=== FINAL SCHEMA SINGULARIZATION ===" -ForegroundColor Cyan

$fixes = @(
    # Constraint names
    @{Pattern='PK_Atoms\b'; Replacement='PK_Atom'},
    @{Pattern='PK_Models\b'; Replacement='PK_Model'},
    @{Pattern='PK_AtomEmbeddings\b'; Replacement='PK_AtomEmbedding'},
    @{Pattern='PK_AtomCompositions\b'; Replacement='PK_AtomComposition'},
    @{Pattern='PK_AtomRelations\b'; Replacement='PK_AtomRelation'},
    @{Pattern='PK_AtomEmbeddingComponents\b'; Replacement='PK_AtomEmbeddingComponent'},
    @{Pattern='PK_ModelLayers\b'; Replacement='PK_ModelLayer'},
    @{Pattern='PK_TensorAtoms\b'; Replacement='PK_TensorAtom'},
    @{Pattern='PK_InferenceRequests\b'; Replacement='PK_InferenceRequest'},
    @{Pattern='PK_BackgroundJobs\b'; Replacement='PK_BackgroundJob'},
    @{Pattern='PK_WeightSnapshots\b'; Replacement='PK_WeightSnapshot'},

    # Unique constraints
    @{Pattern='UX_Atoms_'; Replacement='UX_Atom_'},
    @{Pattern='UQ_Atoms_'; Replacement='UQ_Atom_'},
    @{Pattern='UX_Models_'; Replacement='UX_Model_'},

    # Index names
    @{Pattern='IX_Atoms_'; Replacement='IX_Atom_'},
    @{Pattern='IX_Models_'; Replacement='IX_Model_'},
    @{Pattern='IX_AtomEmbeddings_'; Replacement='IX_AtomEmbedding_'},
    @{Pattern='IX_ModelLayers_'; Replacement='IX_ModelLayer_'},
    @{Pattern='IX_BackgroundJobs_'; Replacement='IX_BackgroundJob_'}
)

$files = Get-ChildItem -Path "src/Hartonomous.Database" -Include "*.sql" -Recurse
$totalFixed = 0

foreach ($file in $files) {
    $content = Get-Content $file.FullName -Raw
    $original = $content
    $changed = $false

    foreach ($fix in $fixes) {
        if ($content -match $fix.Pattern) {
            $content = $content -replace $fix.Pattern, $fix.Replacement
            $changed = $true
        }
    }

    if ($changed) {
        if (-not $WhatIf) {
            Set-Content -Path $file.FullName -Value $content -NoNewline
        }
        $totalFixed++
    }
}

Write-Host "Fixed $totalFixed files" -ForegroundColor Green
if ($WhatIf) {
    Write-Host "DRY RUN - no changes made" -ForegroundColor Yellow
}
exit 0
