#!/usr/bin/env pwsh
<#
.SYNOPSIS
    Singularizes database table names (plural → singular)
.DESCRIPTION
    Safe, idempotent script to rename tables following old-school T-SQL conventions:
    - Database: dbo.Atom (singular)
    - Code: DbSet<Atom> Atoms (entity singular, property plural)

    This script:
    1. Creates a backup of the Database project
    2. Renames table files (e.g., dbo.Atoms.sql → dbo.Atom.sql)
    3. Updates CREATE TABLE statements inside files
    4. Updates foreign key references throughout the project
    5. Generates a report of all changes
.PARAMETER WhatIf
    Show what would be changed without making changes
#>

param(
    [switch]$WhatIf
)

$ErrorActionPreference = "Stop"

# Table name mappings (plural → singular)
$tableMappings = @{
    "AgentTools" = "AgentTool"
    "AtomCompositions" = "AtomComposition"
    "AtomEmbeddingComponents" = "AtomEmbeddingComponent"
    "AtomEmbeddings" = "AtomEmbedding"
    "AtomEmbeddingSpatialMetadata" = "AtomEmbeddingSpatialMetadatum"
    "AtomRelations" = "AtomRelation"
    "Atoms" = "Atom"
    "AtomsHistory" = "AtomHistory"
    "AttentionInferenceResults" = "AttentionInferenceResult"
    "AutonomousComputeJobs" = "AutonomousComputeJob"
    "BackgroundJobs" = "BackgroundJob"
    "BillingInvoices" = "BillingInvoice"
    "BillingMultipliers" = "BillingMultiplier"
    "BillingOperationRates" = "BillingOperationRate"
    "BillingPricingTiers" = "BillingPricingTier"
    "BillingQuotaViolations" = "BillingQuotaViolation"
    "BillingRatePlans" = "BillingRatePlan"
    "BillingTenantQuotas" = "BillingTenantQuota"
    "CachedActivations" = "CachedActivation"
    "CdcCheckpoints" = "CdcCheckpoint"
    "CICDBuilds" = "CICDBuild"
    "CodeAtoms" = "CodeAtom"
    "DeduplicationPolicies" = "DeduplicationPolicy"
    "EventAtoms" = "EventAtom"
    "EventGenerationResults" = "EventGenerationResult"
    "EventHubCheckpoints" = "EventHubCheckpoint"
    "GenerationStreamSegments" = "GenerationStreamSegment"
    "InferenceRequests" = "InferenceRequest"
    "InferenceSteps" = "InferenceStep"
    "IngestionJobAtoms" = "IngestionJobAtom"
    "IngestionJobs" = "IngestionJob"
    "ModelLayers" = "ModelLayer"
    "Models" = "Model"
    "PendingActions" = "PendingAction"
    "ProvenanceAuditResults" = "ProvenanceAuditResult"
    "ProvenanceValidationResults" = "ProvenanceValidationResult"
    "ReasoningChains" = "ReasoningChain"
    "SelfConsistencyResults" = "SelfConsistencyResult"
    "SemanticFeatures" = "SemanticFeature"
    "SessionPaths" = "SessionPath"
    "SpatialLandmarks" = "SpatialLandmark"
    "StreamFusionResults" = "StreamFusionResult"
    "StreamOrchestrationResults" = "StreamOrchestrationResult"
    "TenantAtoms" = "TenantAtom"
    "TensorAtomCoefficients" = "TensorAtomCoefficient"
    "TensorAtoms" = "TensorAtom"
    "TestResults" = "TestResult"
    "TopicKeywords" = "TopicKeyword"
    "TransformerInferenceResults" = "TransformerInferenceResult"
    "WeightSnapshots" = "WeightSnapshot"
}

$databaseProjectPath = "src\Hartonomous.Database"
$tablesPath = Join-Path $databaseProjectPath "Tables"
$changesReport = @()

if ($WhatIf) {
    Write-Host "=== WHAT-IF MODE: No changes will be made ===" -ForegroundColor Yellow
    Write-Host ""
}

# Step 1: Create backup
if (-not $WhatIf) {
    $timestamp = Get-Date -Format "yyyyMMdd_HHmmss"
    $backupPath = "src\Hartonomous.Database.backup_$timestamp"

    Write-Host "Creating backup..." -ForegroundColor Cyan
    Copy-Item -Path $databaseProjectPath -Destination $backupPath -Recurse -Force
    Write-Host "  Backup created: $backupPath" -ForegroundColor Green
    Write-Host ""
}

# Step 2: Rename table files
Write-Host "Renaming table files..." -ForegroundColor Cyan
foreach ($mapping in $tableMappings.GetEnumerator()) {
    $oldName = $mapping.Key
    $newName = $mapping.Value

    $oldFile = Join-Path $tablesPath "dbo.$oldName.sql"
    $newFile = Join-Path $tablesPath "dbo.$newName.sql"

    if (Test-Path $oldFile) {
        $action = "Rename: dbo.$oldName.sql → dbo.$newName.sql"
        $changesReport += $action
        Write-Host "  $action" -ForegroundColor Gray

        if (-not $WhatIf) {
            Move-Item -Path $oldFile -Destination $newFile -Force
        }
    }
}
Write-Host ""

# Step 3: Update table file contents
Write-Host "Updating CREATE TABLE statements..." -ForegroundColor Cyan
foreach ($mapping in $tableMappings.GetEnumerator()) {
    $oldName = $mapping.Key
    $newName = $mapping.Value

    $newFile = Join-Path $tablesPath "dbo.$newName.sql"

    if (Test-Path $newFile) {
        if (-not $WhatIf) {
            $content = Get-Content $newFile -Raw

            # Update CREATE TABLE statement
            $content = $content -replace "\[dbo\]\.\[$oldName\]", "[dbo].[$newName]"
            $content = $content -replace "CREATE TABLE \[dbo\]\.\[$oldName\]", "CREATE TABLE [dbo].[$newName]"

            Set-Content -Path $newFile -Value $content -NoNewline
        }

        Write-Host "  Updated: dbo.$newName.sql" -ForegroundColor Gray
    }
}
Write-Host ""

# Step 4: Update FK references throughout project
Write-Host "Updating foreign key references..." -ForegroundColor Cyan
$sqlFiles = Get-ChildItem -Path $databaseProjectPath -Include "*.sql" -Recurse

foreach ($mapping in $tableMappings.GetEnumerator()) {
    $oldName = $mapping.Key
    $newName = $mapping.Value

    foreach ($file in $sqlFiles) {
        if ($WhatIf) {
            # Just check if file contains the old name
            $content = Get-Content $file.FullName -Raw
            if ($content -match "\[dbo\]\.\[$oldName\]") {
                Write-Host "  Would update: $($file.Name)" -ForegroundColor Gray
            }
        } else {
            $content = Get-Content $file.FullName -Raw
            $updated = $content -replace "\[dbo\]\.\[$oldName\]", "[dbo].[$newName]"

            if ($content -ne $updated) {
                Set-Content -Path $file.FullName -Value $updated -NoNewline
                Write-Host "  Updated references in: $($file.Name)" -ForegroundColor Gray
            }
        }
    }
}
Write-Host ""

# Step 5: Update .sqlproj file
Write-Host "Updating .sqlproj project file..." -ForegroundColor Cyan
$projFile = Join-Path $databaseProjectPath "Hartonomous.Database.sqlproj"

if (Test-Path $projFile) {
    if (-not $WhatIf) {
        $content = Get-Content $projFile -Raw

        foreach ($mapping in $tableMappings.GetEnumerator()) {
            $oldName = $mapping.Key
            $newName = $mapping.Value

            $content = $content -replace "Tables\\dbo\.$oldName\.sql", "Tables\dbo.$newName.sql"
        }

        Set-Content -Path $projFile -Value $content -NoNewline
    }
    Write-Host "  Updated: Hartonomous.Database.sqlproj" -ForegroundColor Green
}
Write-Host ""

# Summary
Write-Host "=== SUMMARY ===" -ForegroundColor Cyan
Write-Host "Tables singularized: $($tableMappings.Count)" -ForegroundColor White
Write-Host ""

if ($WhatIf) {
    Write-Host "This was a dry-run. Re-run without -WhatIf to apply changes." -ForegroundColor Yellow
} else {
    Write-Host "Changes applied successfully!" -ForegroundColor Green
    Write-Host ""
    Write-Host "Next steps:" -ForegroundColor Yellow
    Write-Host "  1. Review changes: git diff src/Hartonomous.Database" -ForegroundColor Gray
    Write-Host "  2. Build DACPAC: .\scripts\build-dacpac.ps1" -ForegroundColor Gray
    Write-Host "  3. Deploy: .\scripts\deploy-dacpac.ps1 -Server localhost -Database Hartonomous" -ForegroundColor Gray
    Write-Host "  4. Re-scaffold: .\scripts\scaffold-entities.ps1 -Server localhost -Database Hartonomous" -ForegroundColor Gray
}

exit 0
