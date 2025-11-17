#!/usr/bin/env pwsh
<#
.SYNOPSIS
    Fixes FK constraint names and extended properties to use singular table names
#>

$ErrorActionPreference = "Stop"

$databaseProjectPath = "src\Hartonomous.Database"

# Constraint name fixes (plural → singular in constraint names)
$constraintFixes = @{
    "FK_WeightSnapshots_Models" = "FK_WeightSnapshot_Model"
    "FK_TransformerInferenceResults_Models" = "FK_TransformerInferenceResult_Model"
    "FK_SemanticFeatures_AtomEmbeddings" = "FK_SemanticFeature_AtomEmbedding"
    "FK_EventAtoms_Atoms" = "FK_EventAtom_Atom"
    "FK_AttentionInferenceResults_Models" = "FK_AttentionInferenceResult_Model"
    "FK_AttentionGenerationLog_Models" = "FK_AttentionGenerationLog_Model"
    "FK.*Atoms.*" = "Atom"
    "FK.*Models.*" = "Model"
    "FK.*AtomEmbeddings.*" = "AtomEmbedding"
}

# Extended property table name fixes
$extPropFixes = @{
    "\[dbo\]\.\[BackgroundJobs\]" = "[dbo].[BackgroundJob]"
    "\[dbo\]\.\[AtomsHistory\]" = "[dbo].[AtomHistory]"
    "\[dbo\]\.\[AtomRelations\]" = "[dbo].[AtomRelation]"
}

Write-Host "Fixing constraint names and extended properties..." -ForegroundColor Cyan

# Get all SQL files
$sqlFiles = Get-ChildItem -Path $databaseProjectPath -Include "*.sql" -Recurse

foreach ($file in $sqlFiles) {
    $content = Get-Content $file.FullName -Raw
    $original = $content

    # Fix constraint names
    $content = $content -replace "FK_WeightSnapshots_Models", "FK_WeightSnapshot_Model"
    $content = $content -replace "FK_TransformerInferenceResults_Models", "FK_TransformerInferenceResult_Model"
    $content = $content -replace "FK_SemanticFeatures_AtomEmbeddings", "FK_SemanticFeature_AtomEmbedding"
    $content = $content -replace "FK_EventAtoms_Atoms", "FK_EventAtom_Atom"
    $content = $content -replace "FK_AttentionInferenceResults_Models", "FK_AttentionInferenceResult_Model"
    $content = $content -replace "FK_AttentionGenerationLog_Models", "FK_AttentionGenerationLog_Model"

    # Fix extended properties
    $content = $content -replace "\[dbo\]\.\[BackgroundJobs\]\.\[MS_Description\]", "[dbo].[BackgroundJob].[MS_Description]"
    $content = $content -replace "\[dbo\]\.\[AtomsHistory\]\.\[MS_Description\]", "[dbo].[AtomHistory].[MS_Description]"
    $content = $content -replace "N'dbo\.BackgroundJobs'", "N'dbo.BackgroundJob'"
    $content = $content -replace "N'dbo\.AtomsHistory'", "N'dbo.AtomHistory'"

    # Fix AtomRelations indexes
    $content = $content -replace "\[dbo\]\.\[AtomRelations\]\.", "[dbo].[AtomRelation]."

    if ($content -ne $original) {
        Set-Content -Path $file.FullName -Value $content -NoNewline
        Write-Host "  Updated: $($file.Name)" -ForegroundColor Gray
    }
}

Write-Host "Constraint names and extended properties fixed" -ForegroundColor Green

exit 0
