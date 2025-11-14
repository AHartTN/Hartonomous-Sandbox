#!/usr/bin/env pwsh
<#
.SYNOPSIS
Comprehensive stored procedure migration for Hartonomous Core v5 schema

.DESCRIPTION
Handles complex transformations for the new atomic decomposition schema:
1. Atoms: Global content-addressable (no TenantId column)
2. TenantAtoms: Junction table for multi-tenant access control
3. AtomEmbeddings: SpatialKey GEOMETRY instead of EmbeddingVector VECTOR
4. TensorAtomCoefficients: Restructured for positional indexing

.NOTES
This requires manual review of output for semantic correctness.
#>

param(
    [Parameter(Mandatory=$true)]
    [string]$ProcedureName,
    
    [switch]$WhatIf = $false,
    [switch]$Verbose = $false
)

$ErrorActionPreference = "Stop"

$procPath = "src\Hartonomous.Database\Procedures\$ProcedureName"

if (-not (Test-Path $procPath)) {
    Write-Error "Procedure not found: $procPath"
    exit 1
}

Write-Host "Migrating: $ProcedureName" -ForegroundColor Cyan
Write-Host "=" * 80 -ForegroundColor Gray

$content = Get-Content $procPath -Raw
$original = $content
$warnings = @()
$errors = @()

# ============================================================================
# PHASE 1: Column Renames (Simple)
# ============================================================================

# 1. SpatialGeometry -> SpatialKey (already done by batch script)
if ($content -match '\bSpatialGeometry\b') {
    $content = $content -replace '\bSpatialGeometry\b', 'SpatialKey'
    Write-Host "✓ Renamed SpatialGeometry → SpatialKey" -ForegroundColor Green
}

# 2. SpatialBucket -> HilbertValue (single column, already done by batch)
if ($content -match '\bSpatialBucket\b' -and $content -notmatch 'SpatialBucket[XYZ]') {
    $content = $content -replace '\bSpatialBucket\b', 'HilbertValue'
    Write-Host "✓ Renamed SpatialBucket → HilbertValue" -ForegroundColor Green
}

# 3. LastComputedUtc -> CreatedAt (already done by batch)
if ($content -match '\bLastComputedUtc\b') {
    $content = $content -replace '\bLastComputedUtc\b', 'CreatedAt'
    Write-Host "✓ Renamed LastComputedUtc → CreatedAt" -ForegroundColor Green
}

# ============================================================================
# PHASE 2: Remove Deprecated Columns
# ============================================================================

# 4. Remove IsActive/IsDeleted filters (use temporal queries)
if ($content -match 'AND\s+a\.IsActive\s*=\s*1') {
    $content = $content -replace 'AND\s+a\.IsActive\s*=\s*1', ''
    Write-Host "✓ Removed a.IsActive = 1 filter" -ForegroundColor Green
}
if ($content -match 'AND\s+a\.IsDeleted\s*=\s*0') {
    $content = $content -replace 'AND\s+a\.IsDeleted\s*=\s*0', ''
    Write-Host "✓ Removed a.IsDeleted = 0 filter" -ForegroundColor Green
}

# ============================================================================
# PHASE 3: TenantId Handling (CRITICAL)
# ============================================================================

# Check if procedure uses TenantId
$usesTenantId = $content -match '@TenantId' -or $content -match 'TenantId'

if ($usesTenantId) {
    Write-Host "⚠ Procedure uses TenantId - checking pattern..." -ForegroundColor Yellow
    
    # Pattern 1: Direct column access (a.TenantId) - NEEDS JUNCTION TABLE
    if ($content -match 'a\.TenantId') {
        $warnings += "Found 'a.TenantId' - Atoms table no longer has TenantId column. Must JOIN TenantAtoms."
        Write-Host "  ⚠ WARNING: Atoms.TenantId column removed - need TenantAtoms JOIN" -ForegroundColor Magenta
    }
    
    # Pattern 2: Already using TenantAtoms - GOOD
    if ($content -match 'FROM.*TenantAtoms' -or $content -match 'JOIN.*TenantAtoms') {
        Write-Host "  ✓ Already uses TenantAtoms junction table" -ForegroundColor Green
    }
    
    # Pattern 3: @TenantId parameter without junction - NEEDS MANUAL FIX
    if (($content -match '@TenantId\s+INT') -and ($content -notmatch 'TenantAtoms')) {
        $warnings += "Has @TenantId parameter but doesn't JOIN TenantAtoms - needs manual review"
        Write-Host "  ⚠ WARNING: @TenantId param exists but no TenantAtoms JOIN" -ForegroundColor Magenta
    }
}

# ============================================================================
# PHASE 4: Column Removals (NULL or remove references)
# ============================================================================

# 5. CanonicalText - removed, was NVARCHAR(256)
if ($content -match '\bCanonicalText\b') {
    $warnings += "References CanonicalText (removed) - replace with CONVERT(NVARCHAR, AtomicValue) or remove"
    Write-Host "  ⚠ WARNING: CanonicalText column removed" -ForegroundColor Magenta
}

# 6. SourceType/SourceUri - removed, use Modality/Subtype
if ($content -match '\bSourceType\b') {
    $warnings += "References SourceType (removed) - replace with Modality"
    Write-Host "  ⚠ WARNING: SourceType column removed - use Modality" -ForegroundColor Magenta
}
if ($content -match '\bSourceUri\b') {
    $warnings += "References SourceUri (removed) - consider removing or use Metadata"
    Write-Host "  ⚠ WARNING: SourceUri column removed" -ForegroundColor Magenta
}

# 7. ContentType - removed from Atoms, derive as Modality + '/' + Subtype
if ($content -match 'a\.ContentType\b') {
    $warnings += "References a.ContentType (removed) - derive as Modality + '/' + Subtype"
    Write-Host "  ⚠ WARNING: Atoms.ContentType removed" -ForegroundColor Magenta
}

# 8. PayloadLocator - removed (no blob storage)
if ($content -match '\bPayloadLocator\b') {
    $warnings += "References PayloadLocator (removed) - atomic values only"
    Write-Host "  ⚠ WARNING: PayloadLocator removed" -ForegroundColor Magenta
}

# 9. Metadata JSON - removed from Atoms
if ($content -match 'a\.Metadata\b') {
    $warnings += "References a.Metadata (removed from Atoms)"
    Write-Host "  ⚠ WARNING: Atoms.Metadata column removed" -ForegroundColor Magenta
}

# ============================================================================
# PHASE 5: Vector → Geometry Transformations
# ============================================================================

# 10. EmbeddingVector VECTOR(1998) -> SpatialKey GEOMETRY
if ($content -match '\bEmbeddingVector\b') {
    $warnings += "References EmbeddingVector (removed) - replace with SpatialKey GEOMETRY"
    Write-Host "  ⚠ WARNING: EmbeddingVector → SpatialKey requires semantic changes" -ForegroundColor Magenta
}

# 11. VECTOR_DISTANCE -> STDistance
if ($content -match 'VECTOR_DISTANCE') {
    $warnings += "Uses VECTOR_DISTANCE - replace with SpatialKey.STDistance() for GEOMETRY"
    Write-Host "  ⚠ WARNING: VECTOR_DISTANCE → STDistance() transformation needed" -ForegroundColor Magenta
}

# 12. Dimension column - removed
if ($content -match '\bDimension\b' -and $content -notmatch '@.*[Dd]imension') {
    $warnings += "References Dimension column (removed) - check SpatialKey.STDimension() if needed"
    Write-Host "  ⚠ WARNING: Dimension column removed" -ForegroundColor Magenta
}

# ============================================================================
# PHASE 6: TensorAtomCoefficients Restructuring
# ============================================================================

# 13. TensorAtomCoefficientId - removed (no identity column)
if ($content -match '\bTensorAtomCoefficientId\b') {
    $errors += "References TensorAtomCoefficientId (removed) - table restructured"
    Write-Host "  ✗ ERROR: TensorAtomCoefficientId removed - needs restructure" -ForegroundColor Red
}

# 14. ParentLayerId - removed, use ModelId + LayerIdx
if ($content -match '\bParentLayerId\b') {
    $warnings += "References ParentLayerId (removed) - use ModelId + LayerIdx"
    Write-Host "  ⚠ WARNING: ParentLayerId removed" -ForegroundColor Magenta
}

# 15. TensorRole - removed
if ($content -match '\bTensorRole\b') {
    $warnings += "References TensorRole (removed)"
    Write-Host "  ⚠ WARNING: TensorRole column removed" -ForegroundColor Magenta
}

# 16. Coefficient REAL - now the atom itself (TensorAtomId FK to Atoms)
if ($content -match '\bCoefficient\b' -and $content -match 'TensorAtom') {
    $warnings += "References Coefficient column - value is now the atom itself (TensorAtomId)"
    Write-Host "  ⚠ WARNING: Coefficient is now the atom (TensorAtomId)" -ForegroundColor Magenta
}

# ============================================================================
# PHASE 7: Deleted Tables
# ============================================================================

$deletedTables = @('TensorAtoms', 'AtomsLOB', 'AtomPayloadStore', 'TensorAtomPayloads',
                   'AtomicPixels', 'AtomicTextTokens', 'AtomicAudioSamples', 'AtomicWeights',
                   'ImagePatches', 'Images', 'AudioData', 'AudioFrames', 'VideoFrames', 'Videos',
                   'TextDocuments', 'LayerTensorSegments', 'Weights', 'Weights_History')

foreach ($table in $deletedTables) {
    if ($content -match "\b$table\b") {
        $errors += "References deleted table: $table"
        Write-Host "  ✗ ERROR: References deleted table '$table'" -ForegroundColor Red
    }
}

# ============================================================================
# PHASE 8: Output Results
# ============================================================================

Write-Host "`n" + ("=" * 80) -ForegroundColor Gray
Write-Host "Migration Analysis Complete" -ForegroundColor Cyan
Write-Host ("=" * 80) -ForegroundColor Gray

if ($errors.Count -gt 0) {
    Write-Host "`nERRORS ($($errors.Count)):" -ForegroundColor Red
    $errors | ForEach-Object { Write-Host "  - $_" -ForegroundColor Red }
    Write-Host "`n⚠ PROCEDURE REQUIRES MANUAL REWRITE" -ForegroundColor Red
}

if ($warnings.Count -gt 0) {
    Write-Host "`nWARNINGS ($($warnings.Count)):" -ForegroundColor Yellow
    $warnings | ForEach-Object { Write-Host "  - $_" -ForegroundColor Yellow }
}

if ($content -ne $original) {
    if ($WhatIf) {
        Write-Host "`n[DRY RUN] Would save changes" -ForegroundColor Cyan
    } else {
        Set-Content -Path $procPath -Value $content -NoNewline
        Write-Host "`n✓ Saved automatic changes" -ForegroundColor Green
    }
} else {
    Write-Host "`nNo automatic changes applied" -ForegroundColor Gray
}

if ($errors.Count -eq 0 -and $warnings.Count -eq 0) {
    Write-Host "`n✓ PROCEDURE MIGRATION COMPLETE - NO ISSUES" -ForegroundColor Green
} elseif ($errors.Count -eq 0) {
    Write-Host "`n⚠ PROCEDURE NEEDS MANUAL REVIEW (warnings only)" -ForegroundColor Yellow
} else {
    Write-Host "`n✗ PROCEDURE REQUIRES SIGNIFICANT REWORK" -ForegroundColor Red
}

exit ($errors.Count -gt 0 ? 1 : 0)
