#!/usr/bin/env pwsh
<#
.SYNOPSIS
Master migration orchestrator for all stored procedures

.DESCRIPTION
Processes all procedures, categorizes them by complexity, and generates actionable reports.
#>

param(
    [switch]$Fix = $false
)

$ErrorActionPreference = "Stop"

Write-Host "=" * 100 -ForegroundColor Cyan
Write-Host "HARTONOMOUS CORE V5 - STORED PROCEDURE MIGRATION ANALYZER" -ForegroundColor Cyan
Write-Host "=" * 100 -ForegroundColor Cyan
Write-Host ""

$procedures = Get-ChildItem "src\Hartonomous.Database\Procedures\*.sql" | Sort-Object Name

$stats = @{
    Total = $procedures.Count
    Clean = 0
    WarningsOnly = 0
    NeedsFix = 0
    CriticalErrors = 0
}

$categorized = @{
    Clean = @()
    WarningsOnly = @()
    NeedsFix = @()
    CriticalErrors = @()
}

foreach ($proc in $procedures) {
    $content = Get-Content $proc.FullName -Raw
    
    $issues = @{
        Errors = @()
        Warnings = @()
    }
    
    # Check for deleted tables
    $deletedTables = @('TensorAtoms', 'AtomsLOB', 'AtomPayloadStore', 'TensorAtomPayloads')
    foreach ($table in $deletedTables) {
        if ($content -match "\b$table\b") {
            $issues.Errors += "References deleted table: $table"
        }
    }
    
    # Check for removed columns that need semantic changes
    if ($content -match '\bTensorAtomCoefficientId\b') {
        $issues.Errors += "TensorAtomCoefficientId (removed)"
    }
    if ($content -match 'VECTOR_DISTANCE') {
        $issues.Errors += "VECTOR_DISTANCE (needs GEOMETRY.STDistance)"
    }
    if ($content -match '\bEmbeddingVector\b') {
        $issues.Errors += "EmbeddingVector (needs SpatialKey GEOMETRY)"
    }
    if ($content -match 'a\.TenantId\b') {
        $issues.Errors += "a.TenantId (needs TenantAtoms JOIN)"
    }
    
    # Check for removed columns that can be replaced/removed
    if ($content -match '\bCanonicalText\b') {
        $issues.Warnings += "CanonicalText (removed)"
    }
    if ($content -match '\bSourceType\b') {
        $issues.Warnings += "SourceType (use Modality)"
    }
    if ($content -match '\bSourceUri\b') {
        $issues.Warnings += "SourceUri (removed)"
    }
    if ($content -match 'a\.ContentType\b') {
        $issues.Warnings += "Atoms.ContentType (removed)"
    }
    if ($content -match '\bPayloadLocator\b') {
        $issues.Warnings += "PayloadLocator (removed)"
    }
    if ($content -match 'a\.Metadata\b') {
        $issues.Warnings += "Atoms.Metadata (removed)"
    }
    if ($content -match '\bParentLayerId\b') {
        $issues.Warnings += "ParentLayerId (removed)"
    }
    if ($content -match '\bTensorRole\b') {
        $issues.Warnings += "TensorRole (removed)"
    }
    if ($content -match '\bCoefficient\b' -and $content -match 'TensorAtom') {
        $issues.Warnings += "Coefficient (now atom itself)"
    }
    if ($content -match '\bDimension\b' -and $content -notmatch '@.*[Dd]imension') {
        $issues.Warnings += "Dimension (removed)"
    }
    
    # Categorize
    if ($issues.Errors.Count -gt 0) {
        $stats.CriticalErrors++
        $categorized.CriticalErrors += @{Name = $proc.Name; Issues = $issues}
    } elseif ($issues.Warnings.Count -gt 0) {
        $stats.WarningsOnly++
        $categorized.WarningsOnly += @{Name = $proc.Name; Issues = $issues}
    } else {
        $stats.Clean++
        $categorized.Clean += $proc.Name
    }
}

# Display results
Write-Host "SUMMARY" -ForegroundColor Cyan
Write-Host "-" * 100 -ForegroundColor Gray
Write-Host "Total Procedures: $($stats.Total)" -ForegroundColor White
Write-Host "  ✓ Clean (no changes needed): $($stats.Clean)" -ForegroundColor Green
Write-Host "  ⚠ Warnings only: $($stats.WarningsOnly)" -ForegroundColor Yellow
Write-Host "  ✗ Critical errors (need rewrite): $($stats.CriticalErrors)" -ForegroundColor Red
Write-Host ""

if ($categorized.CriticalErrors.Count -gt 0) {
    Write-Host "CRITICAL ERRORS - REQUIRES MANUAL REWRITE ($($categorized.CriticalErrors.Count))" -ForegroundColor Red
    Write-Host "-" * 100 -ForegroundColor Gray
    foreach ($item in $categorized.CriticalErrors) {
        Write-Host "  $($item.Name)" -ForegroundColor Red
        foreach ($err in $item.Issues.Errors) {
            Write-Host "    ✗ $err" -ForegroundColor DarkRed
        }
        foreach ($warn in $item.Issues.Warnings) {
            Write-Host "    ⚠ $warn" -ForegroundColor Yellow
        }
    }
    Write-Host ""
}

if ($categorized.WarningsOnly.Count -gt 0 -and $categorized.WarningsOnly.Count -le 20) {
    Write-Host "WARNINGS ONLY - CAN BE FIXED ($($categorized.WarningsOnly.Count))" -ForegroundColor Yellow
    Write-Host "-" * 100 -ForegroundColor Gray
    foreach ($item in $categorized.WarningsOnly) {
        Write-Host "  $($item.Name)" -ForegroundColor Yellow
        foreach ($warn in $item.Issues.Warnings) {
            Write-Host "    ⚠ $warn" -ForegroundColor DarkYellow
        }
    }
    Write-Host ""
} elseif ($categorized.WarningsOnly.Count -gt 20) {
    Write-Host "WARNINGS ONLY - CAN BE FIXED ($($categorized.WarningsOnly.Count))" -ForegroundColor Yellow
    Write-Host "-" * 100 -ForegroundColor Gray
    Write-Host "  (Too many to list - see full analysis)" -ForegroundColor Gray
    Write-Host ""
}

Write-Host "NEXT STEPS" -ForegroundColor Cyan
Write-Host "-" * 100 -ForegroundColor Gray
Write-Host "1. Address critical errors first ($($stats.CriticalErrors) procedures)" -ForegroundColor White
Write-Host "2. Fix warnings ($($stats.WarningsOnly) procedures)" -ForegroundColor White
Write-Host "3. Test clean procedures ($($stats.Clean) procedures)" -ForegroundColor White
Write-Host ""

# Save detailed report
$report = @{
    Generated = Get-Date -Format "yyyy-MM-dd HH:mm:ss"
    Statistics = $stats
    Categorized = $categorized
}

$reportPath = "PROCEDURE_MIGRATION_ANALYSIS.json"
$report | ConvertTo-Json -Depth 10 | Out-File $reportPath -Encoding UTF8
Write-Host "✓ Detailed report saved to: $reportPath" -ForegroundColor Green
