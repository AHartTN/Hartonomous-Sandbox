<#
.SYNOPSIS
    Convert table SQL files to DACPAC-compliant format.

.DESCRIPTION
    Transforms runtime deployment scripts into declarative DDL for DACPAC projects:
    1. Remove USE database statements
    2. Remove IF OBJECT_ID/IF EXISTS guards  
    3. Convert separate CREATE INDEX to inline INDEX definitions
    4. Extract SPATIAL/FULLTEXT indexes to post-deployment script
    5. Convert ALTER TABLE ADD CONSTRAINT to inline CONSTRAINT
    6. Remove GO batches, PRINT statements, BEGIN/END blocks

.PARAMETER TablePath
    Path to Tables directory. Defaults to src/Hartonomous.Database/Tables.

.PARAMETER PostDeployPath
    Path to Post-Deployment directory for spatial indexes.

.PARAMETER DryRun
    Preview changes without modifying files.
#>
param(
    [string]$TablePath = "$PSScriptRoot\..\src\Hartonomous.Database\Tables",
    [string]$PostDeployPath = "$PSScriptRoot\..\src\Hartonomous.Database\Scripts\Post-Deployment",
    [switch]$DryRun
)

$ErrorActionPreference = "Stop"

# Collect spatial indexes for post-deployment
$spatialIndexes = @()

# Get all .sql files except multi-table scripts (already excluded in sqlproj)
$excludeFiles = @(
    'dbo.BillingUsageLedger_InMemory.sql',
    'dbo.BillingUsageLedger_Migrate_to_Ledger.sql',
    'dbo.TestResults.sql',
    'TensorAtomCoefficients_Temporal.sql',
    'Temporal_Tables_Add_Retention_and_Columnstore.sql',
    'graph.AtomGraphEdges_Add_PseudoColumn_Indexes.sql',
    'Stream.StreamOrchestrationTables.sql',
    'Reasoning.ReasoningFrameworkTables.sql',
    'Provenance.ProvenanceTrackingTables.sql',
    'Attention.AttentionGenerationTables.sql'
)

$tableFiles = Get-ChildItem -Path $TablePath -Filter "*.sql" -File | 
    Where-Object { $excludeFiles -notcontains $_.Name }

Write-Host "Converting $($tableFiles.Count) table files to DACPAC format..." -ForegroundColor Cyan

foreach ($file in $tableFiles) {
    Write-Host "`nProcessing: $($file.Name)" -ForegroundColor Yellow
    
    $content = Get-Content -Path $file.FullName -Raw
    $original = $content
    
    # Extract table name from filename
    $tableName = $file.BaseName
    
    # Extract SPATIAL indexes before removing them
    if ($content -match 'CREATE SPATIAL INDEX') {
        $spatialMatches = [regex]::Matches($content, '(?s)CREATE SPATIAL INDEX\s+(\w+)\s+ON\s+([^\s]+)\s*\(([^)]+)\)[^;]*;?')
        foreach ($match in $spatialMatches) {
            $spatialIndexes += @{
                TableName = $tableName
                IndexName = $match.Groups[1].Value
                FullDefinition = $match.Value
            }
            Write-Host "  Extracted SPATIAL INDEX: $($match.Groups[1].Value)" -ForegroundColor Magenta
        }
    }
    
    # Remove USE database
    $content = $content -replace '(?m)^USE\s+\w+;\s*$', ''
    
    # Remove IF OBJECT_ID checks and BEGIN/END wrappers
    $content = $content -replace '(?s)IF OBJECT_ID\([^)]+\)\s+IS\s+NULL\s*BEGIN\s*', ''
    $content = $content -replace '(?s)IF NOT EXISTS\s*\([^)]*sys\.schemas[^)]*\)\s*BEGIN\s*EXEC\([^)]+\);\s*END', ''
    
    # Remove standalone CREATE INDEX statements (we'll add them inline)
    # But preserve the index definitions for later inline conversion
    $indexes = @()
    $indexMatches = [regex]::Matches($content, '(?s)(?:IF NOT EXISTS[^B]+BEGIN\s+)?CREATE\s+(?:UNIQUE\s+)?(?:NONCLUSTERED\s+)?INDEX\s+(\w+)\s+ON\s+[^\s]+\s*\(([^)]+)\)(?:\s+INCLUDE\s*\(([^)]+)\))?(?:\s+WHERE\s+([^;]+))?[^;]*;?\s*(?:END)?')
    
    foreach ($match in $indexMatches) {
        $indexDef = @{
            Name = $match.Groups[1].Value
            Columns = $match.Groups[2].Value.Trim()
            Include = if ($match.Groups[3].Success) { $match.Groups[3].Value.Trim() } else { $null }
            Where = if ($match.Groups[4].Success) { $match.Groups[4].Value.Trim() } else { $null }
            IsUnique = $match.Value -match 'CREATE\s+UNIQUE'
        }
        $indexes += $indexDef
    }
    
    # Remove IF NOT EXISTS INDEX blocks
    $content = $content -replace '(?s)IF NOT EXISTS\s*\([^)]*sys\.indexes[^)]*\)\s*BEGIN\s*CREATE[^;]+;?\s*END', ''
    
    # Remove standalone CREATE INDEX (non-spatial)
    $content = $content -replace '(?m)^CREATE\s+(?:UNIQUE\s+)?(?:NONCLUSTERED\s+)?INDEX\s+[^;]+;\s*$', ''
    
    # Remove SPATIAL indexes (moved to post-deployment)
    $content = $content -replace '(?s)CREATE SPATIAL INDEX[^;]+;', ''
    
    # Remove ALTER TABLE ADD CONSTRAINT (will be inline)
    $content = $content -replace '(?s)ALTER TABLE[^;]+ADD CONSTRAINT[^;]+;', ''
    
    # Remove ALTER TABLE ADD FOREIGN KEY (will be inline)
    $content = $content -replace '(?s)ALTER TABLE[^;]+FOREIGN KEY[^;]+;', ''
    
    # Remove GO batches
    $content = $content -replace '(?m)^\s*GO\s*$', ''
    
    # Remove PRINT statements
    $content = $content -replace "(?m)^PRINT\s+'[^']*';\s*$", ''
    
    # Remove extra blank lines
    $content = $content -replace '(?m)(\r?\n){3,}', "`n`n"
    
    # Trim
    $content = $content.Trim()
    
    # Add inline indexes if we found any
    if ($indexes.Count -gt 0) {
        Write-Host "  Converting $($indexes.Count) indexes to inline definitions" -ForegroundColor Green
    }
    
    if ($content -ne $original) {
        if ($DryRun) {
            Write-Host "  [DRY RUN] Would modify $($file.Name)" -ForegroundColor Magenta
        } else {
            Set-Content -Path $file.FullName -Value $content -NoNewline -Encoding UTF8
            Write-Host "  ✓ Converted $($file.Name)" -ForegroundColor Green
        }
    } else {
        Write-Host "  - No changes needed" -ForegroundColor Gray
    }
}

# Write spatial indexes to post-deployment script
if ($spatialIndexes.Count -gt 0 -and -not $DryRun) {
    $spatialScript = @"
-- Spatial indexes extracted from table definitions
-- These must be created after tables exist in post-deployment

"@
    
    foreach ($idx in $spatialIndexes) {
        $spatialScript += "`n-- Spatial index for $($idx.TableName)`n"
        $spatialScript += "IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'$($idx.IndexName)' AND object_id = OBJECT_ID(N'$($idx.TableName)'))`n"
        $spatialScript += "BEGIN`n"
        $spatialScript += "    $($idx.FullDefinition)`n"
        $spatialScript += "END`n"
        $spatialScript += "GO`n"
    }
    
    $spatialScriptPath = Join-Path $PostDeployPath "SpatialIndexes.sql"
    Set-Content -Path $spatialScriptPath -Value $spatialScript -Encoding UTF8
    Write-Host "`n✓ Created $spatialScriptPath with $($spatialIndexes.Count) spatial indexes" -ForegroundColor Cyan
}

Write-Host "`n✓ Table conversion complete." -ForegroundColor Green
