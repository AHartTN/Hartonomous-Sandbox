#!/usr/bin/env pwsh
<#
.SYNOPSIS
    Deploys SQL stored procedures to Hartonomous database in correct dependency order.

.DESCRIPTION
    Executes all SQL procedure files from sql/procedures directory in dependency-safe order.
    Ensures CLR bindings, helpers, and core procedures are created before dependent objects.
    Returns JSON with deployment status for Azure DevOps pipeline integration.

.PARAMETER ServerName
    SQL Server instance name

.PARAMETER DatabaseName
    Target database name (default: Hartonomous)

.PARAMETER ProceduresPath
    Path to sql/procedures directory (default: ../../sql/procedures relative to script)

.PARAMETER SqlUser
    SQL authentication username (optional)

.PARAMETER SqlPassword
    SQL authentication password as SecureString

.EXAMPLE
    .\08-create-procedures.ps1 -ServerName "HART-DESKTOP" -DatabaseName "Hartonomous"
#>

[CmdletBinding()]
param(
    [Parameter(Mandatory)]
    [string]$ServerName,

    [Parameter(Mandatory=$false)]
    [string]$DatabaseName = "Hartonomous",

    [Parameter(Mandatory=$false)]
    [string]$ProceduresPath,

    [Parameter(Mandatory=$false)]
    [string]$SqlUser,

    [Parameter(Mandatory=$false)]
    [SecureString]$SqlPassword
)

$ErrorActionPreference = "Stop"
Set-StrictMode -Version Latest

$result = @{
    Step = "CreateStoredProcedures"
    Success = $false
    ProceduresPath = ""
    TotalFiles = 0
    ExecutedFiles = 0
    FailedFiles = @()
    ExecutionOrder = @()
    Errors = @()
    Warnings = @()
    Timestamp = (Get-Date -Format "o")
}

function Invoke-SqlCommand {
    param(
        [string]$Query,
        [string]$Database = "master",
        [switch]$IgnoreErrors
    )

    $sqlArgs = @("-S", $ServerName, "-d", $Database, "-C", "-b", "-Q", $Query, "-h", "-1")

    if ($SqlUser) {
        $plainPassword = [System.Runtime.InteropServices.Marshal]::PtrToStringAuto(
            [System.Runtime.InteropServices.Marshal]::SecureStringToBSTR($SqlPassword)
        )
        $sqlArgs += @("-U", $SqlUser, "-P", $plainPassword)
    } else {
        $sqlArgs += "-E"
    }

    $output = & sqlcmd @sqlArgs 2>&1
    if (-not $IgnoreErrors -and $LASTEXITCODE -ne 0) {
        throw "SQL command failed: $output"
    }
    return ($output | Out-String).Trim()
}

function Execute-SqlFile {
    param(
        [string]$FilePath,
        [string]$FileName
    )

    Write-Host "  Executing: $FileName" -NoNewline

    $sqlArgs = @("-S", $ServerName, "-d", $DatabaseName, "-C", "-b", "-i", $FilePath, "-h", "-1")

    if ($SqlUser) {
        $plainPassword = [System.Runtime.InteropServices.Marshal]::PtrToStringAuto(
            [System.Runtime.InteropServices.Marshal]::SecureStringToBSTR($SqlPassword)
        )
        $sqlArgs += @("-U", $SqlUser, "-P", $plainPassword)
    } else {
        $sqlArgs += "-E"
    }

    $output = & sqlcmd @sqlArgs 2>&1

    if ($LASTEXITCODE -eq 0) {
        Write-Host " ✓" -ForegroundColor Green
        return $true
    } else {
        Write-Host " ✗" -ForegroundColor Red
        Write-Host "    Error: $output" -ForegroundColor Red
        return $false
    }
}

try {
    Write-Host "=== SQL Stored Procedures Deployment ===" -ForegroundColor Cyan
    Write-Host "Server: $ServerName" -ForegroundColor Gray
    Write-Host "Database: $DatabaseName" -ForegroundColor Gray

    # Resolve procedures path
    if (-not $ProceduresPath) {
        $scriptRoot = Split-Path -Parent $PSScriptRoot
        $ProceduresPath = Join-Path (Split-Path -Parent $scriptRoot) "sql\procedures"
    }

    $ProceduresPath = Resolve-Path $ProceduresPath -ErrorAction Stop
    $result.ProceduresPath = $ProceduresPath
    Write-Host "Procedures: $ProceduresPath" -ForegroundColor Gray

    # Verify procedures directory exists
    if (-not (Test-Path $ProceduresPath)) {
        throw "Procedures directory not found: $ProceduresPath"
    }

    # Get all SQL files from procedures directory and sort by dependencies
    $allFiles = Get-ChildItem -Path $ProceduresPath -Filter "*.sql" | Select-Object -ExpandProperty Name | Sort-Object

    Write-Host "  Found $($allFiles.Count) SQL files" -ForegroundColor Gray

    # Execute autonomous CLR functions deployment first (if exists)
    $autonomousClrScript = Join-Path (Split-Path -Parent $ProceduresPath) "deploy-autonomous-clr-functions.sql"
    if (Test-Path $autonomousClrScript) {
        Write-Host "`nDeploying Autonomous CLR Functions..." -ForegroundColor Cyan
        $success = Execute-SqlFile -FilePath $autonomousClrScript -FileName "deploy-autonomous-clr-functions.sql"
        if ($success) {
            Write-Host "  Autonomous CLR functions bound successfully" -ForegroundColor Green
        } else {
            Write-Host "  Warning: Autonomous CLR functions deployment failed" -ForegroundColor Yellow
            $result.Warnings += "Autonomous CLR functions deployment failed"
        }
    } else {
        Write-Host "`nWarning: deploy-autonomous-clr-functions.sql not found at $autonomousClrScript" -ForegroundColor Yellow
        $result.Warnings += "deploy-autonomous-clr-functions.sql not found"
    }

    # Define execution order (dependency-safe)
    # Files not explicitly listed will be executed alphabetically at the end
    $priorityOrder = @(
        # Phase 1: CLR Bindings (must be first - other procedures depend on CLR functions)
        "Common.ClrBindings.sql",
        "Functions.AggregateVectorOperations_Core.sql",

        # Phase 2: Helpers and Utilities (no dependencies)
        "Common.Helpers.sql",
        "Common.CreateSpatialIndexes.sql",
        "Functions.BinaryToRealConversion.sql",
        "Functions.VectorOperations.sql",

        # Phase 3: Core Domain Procedures (foundational, minimal dependencies)
        "dbo.AtomIngestion.sql",
        "dbo.BillingFunctions.sql",
        "dbo.FullTextSearch.sql",
        "dbo.ModelManagement.sql",
        "dbo.ProvenanceFunctions.sql",
        "dbo.VectorSearch.sql",

        # Phase 4: Provenance (depends on core)
        "provenance.AtomicStreamFactory.sql",
        "provenance.AtomicStreamSegments.sql",
        "Provenance.ProvenanceTracking.sql",

        # Phase 5: Spatial Operations (depends on core)
        "Spatial.ProjectionSystem.sql",
        "Spatial.LargeLineStringFunctions.sql",

        # Phase 6: Inference and Search (depends on spatial)
        "Inference.VectorSearchSuite.sql",
        "Inference.AdvancedAnalytics.sql",
        "Inference.JobManagement.sql",
        "Inference.MultiModelEnsemble.sql",
        "Inference.SpatialGenerationSuite.sql",
        "Search.SemanticSearch.sql",

        # Phase 7: Generation (depends on inference)
        "Generation.TextFromVector.sql",
        "Generation.ImageFromPrompt.sql",
        "Generation.AudioFromPrompt.sql",
        "Generation.VideoFromPrompt.sql",
        "Attention.AttentionGeneration.sql",

        # Phase 8: Embedding and Semantics (depends on inference)
        "Embedding.TextToVector.sql",
        "Semantics.FeatureExtraction.sql",
        "Deduplication.SimilarityCheck.sql",

        # Phase 9: Graph Operations (depends on core)
        "Graph.AtomSurface.sql",

        # Phase 10: Billing (depends on core)
        "Billing.InsertUsageRecord_Native.sql",

        # Phase 11: Stream Processing (depends on inference)
        "Stream.StreamOrchestration.sql",
        "Messaging.EventHubCheckpoint.sql",

        # Phase 12: Reasoning Frameworks (depends on inference)
        "Reasoning.ReasoningFrameworks.sql",

        # Phase 13: Operations (depends on most systems)
        "Operations.IndexMaintenance.sql",
        "Feedback.ModelWeightUpdates.sql",

        # Phase 14: OODA Loop - Autonomous System (depends on everything)
        "dbo.sp_Analyze.sql",
        "dbo.sp_Hypothesize.sql",
        "dbo.sp_Act.sql",
        "dbo.sp_Learn.sql",
        "Autonomy.SelfImprovement.sql",
        "Autonomy.FileSystemBindings.sql",

        # Phase 15: Individual stored procedures (depends on domain procedures)
        "dbo.sp_AtomIngestion.sql",
        "dbo.sp_DiscoverAndBindConcepts.sql",
        "dbo.sp_RetrieveAtomPayload.sql",
        "dbo.sp_StoreAtomPayload.sql",
        "dbo.sp_UpdateAtomEmbeddingSpatialMetadata.sql",
        "dbo.fn_BindConcepts.sql",
        "dbo.fn_DiscoverConcepts.sql",

        # Phase 16: Advanced CLR Aggregates (optional - experimental features)
        # NOTE: Functions.AggregateVectorOperations.sql contains 30+ unimplemented aggregates
        # Commented out to prevent deployment failures - uncomment after implementation
        # "Functions.AggregateVectorOperations.sql"
    )

    # Build final execution order: priority files first, then remaining files alphabetically
    $executionOrder = @()
    foreach ($file in $priorityOrder) {
        if ($allFiles -contains $file) {
            $executionOrder += $file
        }
    }

    # Add any files not in priority list
    foreach ($file in $allFiles) {
        if ($priorityOrder -notcontains $file) {
            Write-Host "  Warning: '$file' not in priority order, adding at end" -ForegroundColor Yellow
            $result.Warnings += "File not in priority order: $file"
            $executionOrder += $file
        }
    }

    $result.ExecutionOrder = $executionOrder
    $result.TotalFiles = $executionOrder.Count

    Write-Host "`nExecuting $($executionOrder.Count) procedure files in dependency order..." -ForegroundColor Cyan

    $executedCount = 0
    $failedCount = 0

    foreach ($fileName in $executionOrder) {
        $filePath = Join-Path $ProceduresPath $fileName

        if (-not (Test-Path $filePath)) {
            Write-Host "  Warning: File not found - $fileName" -ForegroundColor Yellow
            $result.Warnings += "File not found: $fileName"
            continue
        }

        $success = Execute-SqlFile -FilePath $filePath -FileName $fileName

        if ($success) {
            $executedCount++
        } else {
            $failedCount++
            $result.FailedFiles += $fileName
        }
    }

    $result.ExecutedFiles = $executedCount

    # Verify deployment
    Write-Host "`nVerifying deployment..." -NoNewline
    $procedureCountQuery = @"
SELECT COUNT(*) AS ProcedureCount
FROM sys.procedures
WHERE schema_id = SCHEMA_ID('dbo');
"@
    $procedureCount = [int](Invoke-SqlCommand -Query $procedureCountQuery -Database $DatabaseName)

    Write-Host " $procedureCount procedures created" -ForegroundColor $(if ($procedureCount -ge 40) { "Green" } else { "Yellow" })

    if ($procedureCount -lt 40) {
        $result.Warnings += "Procedure count ($procedureCount) is lower than expected (40+)"
    }

    # Check for CLR aggregates
    $aggregateCountQuery = @"
SELECT COUNT(*) AS AggregateCount
FROM sys.objects
WHERE type = 'AF';
"@
    $aggregateCount = [int](Invoke-SqlCommand -Query $aggregateCountQuery -Database $DatabaseName)
    Write-Host "  CLR Aggregates: $aggregateCount" -ForegroundColor $(if ($aggregateCount -gt 0) { "Green" } else { "Yellow" })

    if ($aggregateCount -eq 0) {
        $result.Warnings += "No CLR aggregates created - check Functions.AggregateVectorOperations.sql syntax"
    }

    # Summary
    Write-Host "`n" -NoNewline
    if ($failedCount -eq 0) {
        $result.Success = $true
        Write-Host "✓ All procedures deployed successfully" -ForegroundColor Green
        Write-Host "  Executed: $executedCount/$($executionOrder.Count)" -ForegroundColor Green
    } else {
        Write-Host "⚠ Deployment completed with errors" -ForegroundColor Yellow
        Write-Host "  Executed: $executedCount/$($executionOrder.Count)" -ForegroundColor Yellow
        Write-Host "  Failed: $failedCount" -ForegroundColor Red
        foreach ($failedFile in $result.FailedFiles) {
            Write-Host "    - $failedFile" -ForegroundColor Red
        }
    }
}
catch {
    $result.Success = $false
    $result.Errors += $_.Exception.Message
    Write-Host "`n✗ Deployment failed: $($_.Exception.Message)" -ForegroundColor Red
}

# Output JSON
$jsonOutput = $result | ConvertTo-Json -Depth 5 -Compress
Write-Host "`n=== JSON Output ===" -ForegroundColor Cyan
Write-Host $jsonOutput

if ($env:BUILD_ARTIFACTSTAGINGDIRECTORY) {
    $outputPath = Join-Path $env:BUILD_ARTIFACTSTAGINGDIRECTORY "create-procedures.json"
    $jsonOutput | Out-File -FilePath $outputPath -Encoding utf8 -NoNewline
    Write-Host "`nJSON written to: $outputPath" -ForegroundColor Gray
}

if (-not $result.Success) {
    exit 1
}

exit 0
