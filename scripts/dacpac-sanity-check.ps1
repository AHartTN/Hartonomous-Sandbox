# DACPAC Build Sanity Check
# Validates that all critical database objects are accounted for

Write-Host "=== Hartonomous DACPAC Sanity Check ===" -ForegroundColor Cyan
Write-Host ""

$repoRoot = "d:\Repositories\Hartonomous"
$dacpacProject = Join-Path $repoRoot "src\Hartonomous.Database"
$originalSql = Join-Path $repoRoot "sql"

# Track issues
$issues = @()
$warnings = @()
$summary = @()

#region Tables Verification
Write-Host "1. Tables Verification" -ForegroundColor Yellow

$originalTables = Get-ChildItem (Join-Path $originalSql "tables") -Filter "*.sql" | Select-Object -ExpandProperty Name
$dacpacTables = Get-ChildItem (Join-Path $dacpacProject "Tables") -Filter "*.sql" | Select-Object -ExpandProperty Name

$summary += "Original tables: $($originalTables.Count)"
$summary += "DACPAC tables: $($dacpacTables.Count)"

# Find missing tables
$missingTables = Compare-Object $originalTables $dacpacTables | Where-Object { $_.SideIndicator -eq "<=" }
if ($missingTables) {
    $warnings += "Missing tables from original sql/tables/:"
    foreach ($table in $missingTables) {
        $warnings += "  - $($table.InputObject)"
    }
}

# Check excluded tables
$excludedTables = @(
    "dbo.BillingUsageLedger_InMemory.sql",
    "dbo.BillingUsageLedger_Migrate_to_Ledger.sql",
    "dbo.TestResults.sql",
    "provenance.GenerationStreams.sql"
)

foreach ($excluded in $excludedTables) {
    if (Test-Path (Join-Path $dacpacProject "Tables\$excluded")) {
        $summary += "✓ Excluded table exists: $excluded"
    } else {
        $issues += "✗ Excluded table missing: $excluded"
    }
}
#endregion

#region Procedures Verification
Write-Host "2. Procedures Verification" -ForegroundColor Yellow

$originalProcs = Get-ChildItem (Join-Path $originalSql "procedures") -Filter "*.sql" | Select-Object -ExpandProperty Name
$dacpacProcs = Get-ChildItem (Join-Path $dacpacProject "Procedures") -Filter "*.sql" | Select-Object -ExpandProperty Name

$summary += "Original procedures: $($originalProcs.Count)"
$summary += "DACPAC procedures: $($dacpacProcs.Count)"

# All procedures should be excluded from build
$projectContent = Get-Content (Join-Path $dacpacProject "Hartonomous.Database.sqlproj") -Raw
if ($projectContent -match 'Build Remove="Procedures\\\*\*"') {
    $summary += "✓ All procedures correctly excluded from DACPAC build"
} else {
    $issues += "✗ Procedures not properly excluded from DACPAC build"
}

# Verify key procedures exist
$keyProcedures = @(
    "dbo.sp_Act.sql",
    "dbo.sp_Analyze.sql",
    "dbo.sp_Learn.sql",
    "dbo.sp_Hypothesize.sql",
    "dbo.sp_AtomIngestion.sql",
    "Search.SemanticSearch.sql",
    "Inference.ServiceBrokerActivation.sql",
    "Common.ClrBindings.sql"
)

foreach ($proc in $keyProcedures) {
    if ($dacpacProcs -contains $proc) {
        $summary += "✓ Key procedure exists: $proc"
    } else {
        $issues += "✗ Key procedure missing: $proc"
    }
}
#endregion

#region CLR Components Verification
Write-Host "3. CLR Components Verification" -ForegroundColor Yellow

$clrDll = Join-Path $repoRoot "src\SqlClr\bin\Release\SqlClrFunctions.dll"
if (Test-Path $clrDll) {
    $summary += "✓ CLR assembly exists: SqlClrFunctions.dll"
    $fileInfo = Get-Item $clrDll
    $summary += "  Size: $([math]::Round($fileInfo.Length / 1KB, 2)) KB"
    $summary += "  Modified: $($fileInfo.LastWriteTime)"
} else {
    $issues += "✗ CLR assembly missing: SqlClrFunctions.dll"
}

# Verify CLR UDT exclusions
$udtFiles = @(
    "Types\provenance.AtomicStream.sql",
    "Types\provenance.ComponentStream.sql"
)

foreach ($udt in $udtFiles) {
    $fullPath = Join-Path $dacpacProject $udt
    if (Test-Path $fullPath) {
        $summary += "✓ CLR UDT exists (excluded from build): $udt"
    } else {
        $issues += "✗ CLR UDT file missing: $udt"
    }
}
#endregion

#region Post-Deployment Scripts Verification
Write-Host "4. Post-Deployment Scripts Verification" -ForegroundColor Yellow

$postDeployScripts = @(
    "TensorAtomCoefficients_Temporal.sql",
    "Temporal_Tables_Add_Retention_and_Columnstore.sql",
    "graph.AtomGraphEdges_Add_PseudoColumn_Indexes.sql"
)

foreach ($script in $postDeployScripts) {
    $fullPath = Join-Path $dacpacProject "Scripts\Post-Deployment\$script"
    if (Test-Path $fullPath) {
        $summary += "✓ Post-deployment script exists: $script"
    } else {
        $issues += "✗ Post-deployment script missing: $script"
    }
}
#endregion

#region DACPAC Build Status
Write-Host "5. DACPAC Build Status" -ForegroundColor Yellow

$dacpacFile = Join-Path $dacpacProject "bin\Release\Hartonomous.Database.dacpac"
if (Test-Path $dacpacFile) {
    $fileInfo = Get-Item $dacpacFile
    $summary += "✓ DACPAC exists: Hartonomous.Database.dacpac"
    $summary += "  Size: $([math]::Round($fileInfo.Length / 1KB, 2)) KB"
    $summary += "  Modified: $($fileInfo.LastWriteTime)"
    
    # Check if recently built
    $age = (Get-Date) - $fileInfo.LastWriteTime
    if ($age.TotalMinutes -lt 30) {
        $summary += "  Status: Recently built ($(([int]$age.TotalMinutes)) minutes ago)"
    } else {
        $warnings += "DACPAC is older than 30 minutes - consider rebuilding"
    }
} else {
    $issues += "✗ DACPAC file missing - run: dotnet build -c Release"
}
#endregion

#region Deployment Script Verification
Write-Host "6. Deployment Script Verification" -ForegroundColor Yellow

$deployScript = Join-Path $repoRoot "scripts\deploy-database-unified.ps1"
if (Test-Path $deployScript) {
    $summary += "✓ Unified deployment script exists"
    
    # Check if it references the Database project procedures
    $content = Get-Content $deployScript -Raw
    if ($content -match "sql\\procedures") {
        $warnings += "Deployment script references sql/procedures/ (should reference src/Hartonomous.Database/Procedures/)"
    }
} else {
    $issues += "✗ Deployment script missing: scripts/deploy-database-unified.ps1"
}

$clrDeployScript = Join-Path $repoRoot "scripts\deploy-clr-secure.ps1"
if (Test-Path $clrDeployScript) {
    $summary += "✓ CLR deployment script exists"
} else {
    $issues += "✗ CLR deployment script missing: scripts/deploy-clr-secure.ps1"
}
#endregion

#region Schema Files Verification
Write-Host "7. Schema Files Verification" -ForegroundColor Yellow

$schemas = @(
    "Schemas\provenance.sql",
    "Schemas\graph.sql"
)

foreach ($schema in $schemas) {
    $fullPath = Join-Path $dacpacProject $schema
    if (Test-Path $fullPath) {
        $summary += "✓ Schema exists: $schema"
    } else {
        $issues += "✗ Schema missing: $schema"
    }
}
#endregion

#region Results Summary
Write-Host ""
Write-Host "=== SUMMARY ===" -ForegroundColor Cyan
foreach ($line in $summary) {
    if ($line -match "^✓") {
        Write-Host $line -ForegroundColor Green
    } else {
        Write-Host $line -ForegroundColor White
    }
}

if ($warnings.Count -gt 0) {
    Write-Host ""
    Write-Host "=== WARNINGS ===" -ForegroundColor Yellow
    foreach ($warning in $warnings) {
        Write-Host $warning -ForegroundColor Yellow
    }
}

if ($issues.Count -gt 0) {
    Write-Host ""
    Write-Host "=== ISSUES ===" -ForegroundColor Red
    foreach ($issue in $issues) {
        Write-Host $issue -ForegroundColor Red
    }
    Write-Host ""
    Write-Host "SANITY CHECK FAILED - $($issues.Count) issues found" -ForegroundColor Red
    exit 1
} else {
    Write-Host ""
    Write-Host "SANITY CHECK PASSED - All critical components accounted for" -ForegroundColor Green
    if ($warnings.Count -gt 0) {
        Write-Host "Note: $($warnings.Count) warnings found (see above)" -ForegroundColor Yellow
    }
    exit 0
}
#endregion
