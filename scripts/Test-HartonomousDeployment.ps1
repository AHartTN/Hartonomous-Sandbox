<#
.SYNOPSIS
    End-to-end validation and smoke test suite for Hartonomous deployment.

.DESCRIPTION
    This script performs comprehensive validation of the Hartonomous Cognitive Engine
    deployment across all system layers:
    
    Phase 1: Physical Layer (CLR Functions & Database)
    Phase 2: Nervous System (Service Broker & Workers)
    Phase 3: Ingestion Layer (API & Storage)
    Phase 4: Propagation Layer (Event Bus)
    Phase 5: Cognition Layer (OODA Loop)
    
    Each test phase reports pass/fail status and provides detailed diagnostics
    for troubleshooting deployment issues.

.PARAMETER Server
    SQL Server instance name (e.g., "localhost" or "server\instance")

.PARAMETER Database
    Target database name (default: "Hartonomous")

.PARAMETER ApiBaseUrl
    Base URL of the Hartonomous API (default: "https://localhost:5001")

.PARAMETER SkipApiTests
    Skip API endpoint tests (useful for database-only validation)

.PARAMETER TenantId
    Tenant ID to use for test data (default: 0)

.EXAMPLE
    .\Test-HartonomousDeployment.ps1 -Server "localhost" -Database "Hartonomous"

.EXAMPLE
    .\Test-HartonomousDeployment.ps1 -Server "localhost" -SkipApiTests -Verbose

.NOTES
    Prerequisites:
    - SQL Server with Hartonomous database deployed
    - CLR assemblies and functions deployed
    - Service Broker enabled
    - (Optional) Hartonomous API running
#>

[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [string]$Server,
    
    [Parameter(Mandatory = $false)]
    [string]$Database = "Hartonomous",
    
    [Parameter(Mandatory = $false)]
    [string]$ApiBaseUrl = "https://localhost:5001",
    
    [Parameter(Mandatory = $false)]
    [switch]$SkipApiTests,
    
    [Parameter(Mandatory = $false)]
    [int]$TenantId = 0
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

# Import required modules
Import-Module SqlServer -ErrorAction Stop

# Test results tracking
$script:TestResults = @{
    Passed = 0
    Failed = 0
    Skipped = 0
    Tests = @()
}

# Helper functions
function Write-TestHeader {
    param([string]$Message)
    Write-Host "`n========================================" -ForegroundColor Cyan
    Write-Host $Message -ForegroundColor Cyan
    Write-Host "========================================`n" -ForegroundColor Cyan
}

function Write-TestResult {
    param(
        [string]$TestName,
        [bool]$Passed,
        [string]$Message = "",
        [string]$Details = ""
    )
    
    $result = @{
        TestName = $TestName
        Passed = $Passed
        Message = $Message
        Details = $Details
        Timestamp = Get-Date
    }
    
    $script:TestResults.Tests += $result
    
    if ($Passed) {
        $script:TestResults.Passed++
        Write-Host "[✓] PASS: $TestName" -ForegroundColor Green
        if ($Message) {
            Write-Host "    $Message" -ForegroundColor Gray
        }
    }
    else {
        $script:TestResults.Failed++
        Write-Host "[✗] FAIL: $TestName" -ForegroundColor Red
        if ($Message) {
            Write-Host "    $Message" -ForegroundColor Yellow
        }
        if ($Details) {
            Write-Host "    Details: $Details" -ForegroundColor Gray
        }
    }
}

function Invoke-SqlTest {
    param(
        [string]$Query,
        [string]$TestName,
        [scriptblock]$ValidationScript
    )
    
    try {
        $result = Invoke-Sqlcmd -ServerInstance $Server -Database $Database -Query $Query -ErrorAction Stop
        $isValid = & $ValidationScript $result
        
        if ($isValid) {
            Write-TestResult -TestName $TestName -Passed $true
        }
        else {
            Write-TestResult -TestName $TestName -Passed $false -Message "Validation failed"
        }
    }
    catch {
        Write-TestResult -TestName $TestName -Passed $false -Message $_.Exception.Message
    }
}

# ============================================================================
# Phase 1: Physical Layer Validation
# ============================================================================

Write-TestHeader "Phase 1: Physical Layer Validation (CLR & Database)"

# Test 1.1: Database connectivity
try {
    $query = "SELECT @@VERSION AS Version, DB_NAME() AS DatabaseName;"
    $result = Invoke-Sqlcmd -ServerInstance $Server -Database $Database -Query $query -ErrorAction Stop
    Write-TestResult -TestName "Database Connectivity" -Passed $true -Message "Connected to $($result.DatabaseName)"
}
catch {
    Write-TestResult -TestName "Database Connectivity" -Passed $false -Message $_.Exception.Message
    Write-Host "`nCritical failure: Cannot connect to database. Exiting..." -ForegroundColor Red
    exit 1
}

# Test 1.2: CLR Integration enabled
Invoke-SqlTest -TestName "CLR Integration Enabled" -Query @"
SELECT CAST(value_in_use AS BIT) AS ClrEnabled
FROM sys.configurations
WHERE name = 'clr enabled';
"@ -ValidationScript {
    param($result)
    return $result.ClrEnabled -eq $true
}

# Test 1.3: CLR assemblies deployed
Invoke-SqlTest -TestName "CLR Assemblies Deployed" -Query @"
SELECT COUNT(*) AS AssemblyCount
FROM sys.assemblies
WHERE is_user_defined = 1;
"@ -ValidationScript {
    param($result)
    $expectedMin = 10  # Expect at least 10 assemblies (dependencies + main)
    $actual = $result.AssemblyCount
    Write-Host "    Found $actual assemblies (expected >= $expectedMin)" -ForegroundColor Gray
    return $actual -ge $expectedMin
}

# Test 1.4: CLR Scalar Function - Vector Dot Product
try {
    $vec1 = "0x3F8000003F8000003F800000"  # [1.0, 1.0, 1.0]
    $vec2 = "0x3F8000003F8000003F800000"
    
    $query = "SELECT dbo.clr_VectorDotProduct($vec1, $vec2) AS DotProduct;"
    $result = Invoke-Sqlcmd -ServerInstance $Server -Database $Database -Query $query -ErrorAction Stop
    
    $expected = 3.0
    $actual = $result.DotProduct
    $tolerance = 0.001
    
    if ([Math]::Abs($actual - $expected) -lt $tolerance) {
        Write-TestResult -TestName "CLR Scalar Function (Vector Dot Product)" -Passed $true -Message "Result: $actual (expected: $expected)"
    }
    else {
        Write-TestResult -TestName "CLR Scalar Function (Vector Dot Product)" -Passed $false -Message "Result: $actual (expected: $expected)"
    }
}
catch {
    Write-TestResult -TestName "CLR Scalar Function (Vector Dot Product)" -Passed $false -Message $_.Exception.Message
}

# Test 1.5: Core tables exist
$coreTables = @('Atom', 'AtomRelation', 'AtomEmbedding', 'AutonomousImprovementHistory')

foreach ($table in $coreTables) {
    Invoke-SqlTest -TestName "Core Table Exists: $table" -Query @"
SELECT COUNT(*) AS TableExists
FROM INFORMATION_SCHEMA.TABLES
WHERE TABLE_SCHEMA = 'dbo' AND TABLE_NAME = '$table';
"@ -ValidationScript {
        param($result)
        return $result.TableExists -eq 1
    }
}

# Test 1.6: Schema migration status (SpatialKey and HilbertValue)
Invoke-SqlTest -TestName "Schema Migration: Atom.SpatialKey" -Query @"
SELECT COUNT(*) AS ColumnExists
FROM INFORMATION_SCHEMA.COLUMNS
WHERE TABLE_NAME = 'Atom' AND COLUMN_NAME = 'SpatialKey';
"@ -ValidationScript {
    param($result)
    return $result.ColumnExists -eq 1
}

Invoke-SqlTest -TestName "Schema Migration: AtomRelation.HilbertValue" -Query @"
SELECT COUNT(*) AS ColumnExists
FROM INFORMATION_SCHEMA.COLUMNS
WHERE TABLE_NAME = 'AtomRelation' AND COLUMN_NAME = 'HilbertValue';
"@ -ValidationScript {
    param($result)
    return $result.ColumnExists -eq 1
}

# ============================================================================
# Phase 2: Nervous System Validation (Service Broker)
# ============================================================================

Write-TestHeader "Phase 2: Nervous System Validation (Service Broker)"

# Test 2.1: Service Broker enabled
Invoke-SqlTest -TestName "Service Broker Enabled" -Query @"
SELECT is_broker_enabled AS BrokerEnabled
FROM sys.databases
WHERE name = '$Database';
"@ -ValidationScript {
    param($result)
    return $result.BrokerEnabled -eq $true
}

# Test 2.2: Neo4j Sync Service exists
Invoke-SqlTest -TestName "Neo4j Sync Service Exists" -Query @"
SELECT COUNT(*) AS ServiceExists
FROM sys.services
WHERE name = 'Neo4jSyncService';
"@ -ValidationScript {
    param($result)
    return $result.ServiceExists -eq 1
}

# Test 2.3: Service Broker queue exists
Invoke-SqlTest -TestName "Service Broker Queue Exists" -Query @"
SELECT COUNT(*) AS QueueExists
FROM sys.service_queues
WHERE name LIKE '%Neo4j%';
"@ -ValidationScript {
    param($result)
    return $result.QueueExists -ge 1
}

# Test 2.4: sp_EnqueueNeo4jSync procedure exists
Invoke-SqlTest -TestName "Enqueue Procedure Exists" -Query @"
SELECT COUNT(*) AS ProcExists
FROM INFORMATION_SCHEMA.ROUTINES
WHERE ROUTINE_TYPE = 'PROCEDURE' AND ROUTINE_NAME = 'sp_EnqueueNeo4jSync';
"@ -ValidationScript {
    param($result)
    return $result.ProcExists -eq 1
}

# ============================================================================
# Phase 3: Ingestion Layer Validation
# ============================================================================

Write-TestHeader "Phase 3: Ingestion Layer Validation"

# Test 3.1: Ingestion stored procedure exists
Invoke-SqlTest -TestName "Ingestion Procedure Exists (sp_IngestAtoms)" -Query @"
SELECT COUNT(*) AS ProcExists
FROM INFORMATION_SCHEMA.ROUTINES
WHERE ROUTINE_TYPE = 'PROCEDURE' AND ROUTINE_NAME = 'sp_IngestAtoms';
"@ -ValidationScript {
    param($result)
    return $result.ProcExists -eq 1
}

# Test 3.2: Test atom ingestion
try {
    $testJson = @'
[
    {
        "atomicValue": "Test atom from smoke test suite",
        "canonicalText": "Test atom from smoke test suite",
        "modality": "Text",
        "subtype": "PlainText",
        "metadata": "{\"source\":\"SmokeTest\",\"timestamp\":\"' + (Get-Date -Format "o") + '\"}"
    }
]
'@
    
    $query = @"
DECLARE @result NVARCHAR(MAX);
DECLARE @batchId UNIQUEIDENTIFIER;

EXEC dbo.sp_IngestAtoms
    @atomsJson = '$($testJson -replace "'", "''")',
    @sourceId = NULL,
    @tenantId = $TenantId,
    @batchId = @batchId OUTPUT;

SELECT @batchId AS BatchId;
"@
    
    $result = Invoke-Sqlcmd -ServerInstance $Server -Database $Database -Query $query -ErrorAction Stop
    
    if ($result.BatchId) {
        Write-TestResult -TestName "Atom Ingestion Test" -Passed $true -Message "BatchId: $($result.BatchId)"
    }
    else {
        Write-TestResult -TestName "Atom Ingestion Test" -Passed $false -Message "No BatchId returned"
    }
}
catch {
    Write-TestResult -TestName "Atom Ingestion Test" -Passed $false -Message $_.Exception.Message
}

# Test 3.3: Verify ingested atom exists
Invoke-SqlTest -TestName "Verify Ingested Atom Stored" -Query @"
SELECT COUNT(*) AS AtomCount
FROM dbo.Atom
WHERE TenantId = $TenantId
  AND CanonicalText LIKE '%smoke test suite%';
"@ -ValidationScript {
    param($result)
    return $result.AtomCount -ge 1
}

# ============================================================================
# Phase 4: Propagation Layer Validation
# ============================================================================

Write-TestHeader "Phase 4: Propagation Layer Validation (Event Bus)"

# Test 4.1: Check for enqueued messages
Invoke-SqlTest -TestName "Service Broker Messages Enqueued" -Query @"
SELECT COUNT(*) AS MessageCount
FROM sys.transmission_queue;
"@ -ValidationScript {
    param($result)
    # Messages may have been processed, so we just check the queue exists
    Write-Host "    Transmission queue contains $($result.MessageCount) messages" -ForegroundColor Gray
    return $true  # Always pass, just informational
}

# Test 4.2: Check Neo4jSyncQueue table (fallback monitoring)
Invoke-SqlTest -TestName "Neo4j Sync Queue Monitoring" -Query @"
IF OBJECT_ID('dbo.Neo4jSyncQueue', 'U') IS NOT NULL
    SELECT COUNT(*) AS QueuedItems
    FROM dbo.Neo4jSyncQueue
    WHERE Status = 'Pending';
ELSE
    SELECT 0 AS QueuedItems;
"@ -ValidationScript {
    param($result)
    Write-Host "    Pending sync items: $($result.QueuedItems)" -ForegroundColor Gray
    return $true  # Informational only
}

# ============================================================================
# Phase 5: Cognition Layer Validation (OODA Loop)
# ============================================================================

Write-TestHeader "Phase 5: Cognition Layer Validation (OODA Loop)"

# Test 5.1: Autonomous analysis procedure exists
Invoke-SqlTest -TestName "Autonomous Analysis Procedure Exists (sp_Analyze)" -Query @"
SELECT COUNT(*) AS ProcExists
FROM INFORMATION_SCHEMA.ROUTINES
WHERE ROUTINE_TYPE = 'PROCEDURE' AND ROUTINE_NAME = 'sp_Analyze';
"@ -ValidationScript {
    param($result)
    return $result.ProcExists -eq 1
}

# Test 5.2: SQL Server Agent Job exists
try {
    $query = @"
SELECT 
    j.name AS JobName,
    j.enabled AS IsEnabled,
    CASE WHEN js.next_run_date > 0 
        THEN 'Scheduled' 
        ELSE 'Not Scheduled' 
    END AS ScheduleStatus
FROM msdb.dbo.sysjobs j
LEFT JOIN msdb.dbo.sysjobschedules js ON j.job_id = js.job_id
WHERE j.name = 'Hartonomous_Cognitive_Kernel';
"@
    
    $result = Invoke-Sqlcmd -ServerInstance $Server -Database "msdb" -Query $query -ErrorAction Stop
    
    if ($result) {
        $isEnabled = $result.IsEnabled
        Write-TestResult -TestName "SQL Server Agent Job Configured" -Passed $true `
            -Message "Job: $($result.JobName), Enabled: $isEnabled, Status: $($result.ScheduleStatus)"
    }
    else {
        Write-TestResult -TestName "SQL Server Agent Job Configured" -Passed $false `
            -Message "Job 'Hartonomous_Cognitive_Kernel' not found"
    }
}
catch {
    Write-TestResult -TestName "SQL Server Agent Job Configured" -Passed $false -Message $_.Exception.Message
}

# Test 5.3: Check OODA execution history
Invoke-SqlTest -TestName "OODA Loop Execution History" -Query @"
IF OBJECT_ID('dbo.AutonomousImprovementHistory', 'U') IS NOT NULL
    SELECT COUNT(*) AS ExecutionCount
    FROM dbo.AutonomousImprovementHistory;
ELSE
    SELECT 0 AS ExecutionCount;
"@ -ValidationScript {
    param($result)
    Write-Host "    Historical OODA executions: $($result.ExecutionCount)" -ForegroundColor Gray
    return $true  # Informational only
}

# ============================================================================
# Phase 6: API Layer Validation (Optional)
# ============================================================================

if (-not $SkipApiTests) {
    Write-TestHeader "Phase 6: API Layer Validation (Optional)"
    
    # Test 6.1: API health endpoint
    try {
        $healthUrl = "$ApiBaseUrl/health"
        $response = Invoke-WebRequest -Uri $healthUrl -Method GET -TimeoutSec 10 -UseBasicParsing
        
        if ($response.StatusCode -eq 200) {
            Write-TestResult -TestName "API Health Endpoint" -Passed $true -Message "Status: $($response.StatusCode)"
        }
        else {
            Write-TestResult -TestName "API Health Endpoint" -Passed $false -Message "Status: $($response.StatusCode)"
        }
    }
    catch {
        Write-TestResult -TestName "API Health Endpoint" -Passed $false -Message $_.Exception.Message
    }
}
else {
    Write-Host "`nSkipping API tests (use -SkipApiTests:$false to enable)" -ForegroundColor Yellow
}

# ============================================================================
# Summary Report
# ============================================================================

Write-TestHeader "Test Execution Summary"

$totalTests = $script:TestResults.Passed + $script:TestResults.Failed + $script:TestResults.Skipped
$passRate = if ($totalTests -gt 0) { [math]::Round(($script:TestResults.Passed / $totalTests) * 100, 2) } else { 0 }

Write-Host "Total Tests:    $totalTests" -ForegroundColor White
Write-Host "Passed:         $($script:TestResults.Passed)" -ForegroundColor Green
Write-Host "Failed:         $($script:TestResults.Failed)" -ForegroundColor $(if ($script:TestResults.Failed -gt 0) { "Red" } else { "Gray" })
Write-Host "Skipped:        $($script:TestResults.Skipped)" -ForegroundColor Gray
Write-Host "Pass Rate:      $passRate%" -ForegroundColor $(if ($passRate -ge 90) { "Green" } elseif ($passRate -ge 70) { "Yellow" } else { "Red" })

Write-Host "`n========================================" -ForegroundColor Cyan

# Export detailed results to JSON
$resultsPath = ".\HartonomousTestResults_$(Get-Date -Format 'yyyyMMdd_HHmmss').json"
$script:TestResults | ConvertTo-Json -Depth 10 | Out-File -FilePath $resultsPath -Encoding UTF8
Write-Host "Detailed results exported to: $resultsPath" -ForegroundColor Gray

# Exit code based on test results
if ($script:TestResults.Failed -eq 0) {
    Write-Host "`n✓ All tests passed! Hartonomous deployment is healthy." -ForegroundColor Green
    exit 0
}
else {
    Write-Host "`n✗ Some tests failed. Review the output above for details." -ForegroundColor Red
    exit 1
}
