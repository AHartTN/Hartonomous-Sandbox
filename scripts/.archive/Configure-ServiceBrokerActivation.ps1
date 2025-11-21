<#
.SYNOPSIS
    Configure Service Broker queue activation for OODA loop (idempotent).

.DESCRIPTION
    Enables automatic activation of stored procedures when Service Broker messages arrive.
    This is required for the autonomous OODA loop to function properly.
    
    OODA Loop Flow:
      AnalyzeQueue → (Manual/Agent trigger) → sp_Analyze
      HypothesizeQueue → (Auto) → sp_Hypothesize  
      ActQueue → (Auto) → sp_Act
      LearnQueue → (Auto) → sp_Learn

.PARAMETER Server
    SQL Server instance name

.PARAMETER Database
    Database name (default: Hartonomous)

.EXAMPLE
    .\Configure-ServiceBrokerActivation.ps1 -Server "localhost" -Database "Hartonomous"

.NOTES
    Idempotent: Safe to run multiple times
    Prerequisites: Service Broker enabled, OODA procedures deployed
#>

[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [string]$Server,
    
    [Parameter(Mandatory = $false)]
    [string]$Database = "Hartonomous"
)

$ErrorActionPreference = 'Stop'

Write-Host "================================================================================`n" -ForegroundColor Cyan
Write-Host "Configuring Service Broker Queue Activation" -ForegroundColor Cyan
Write-Host "Server: $Server" -ForegroundColor White
Write-Host "Database: $Database" -ForegroundColor White
Write-Host "`n================================================================================" -ForegroundColor Cyan

$configScript = @"
USE [$Database];
GO

PRINT '';
PRINT '=== Configuring OODA Loop Queue Activation ===';
PRINT '';

-- ============================================================================
-- HypothesizeQueue Activation (Orient Phase)
-- ============================================================================
PRINT 'Configuring HypothesizeQueue...';

IF EXISTS (SELECT 1 FROM sys.service_queues WHERE name = 'HypothesizeQueue')
BEGIN
    ALTER QUEUE [dbo].[HypothesizeQueue]
    WITH ACTIVATION (
        STATUS = ON,
        PROCEDURE_NAME = dbo.sp_Hypothesize,
        MAX_QUEUE_READERS = 1,
        EXECUTE AS OWNER
    );
    PRINT '  ✓ HypothesizeQueue activation enabled (sp_Hypothesize)';
END
ELSE
    PRINT '  ✗ HypothesizeQueue not found';

-- ============================================================================
-- ActQueue Activation (Decide & Act Phase)
-- ============================================================================
PRINT 'Configuring ActQueue...';

IF EXISTS (SELECT 1 FROM sys.service_queues WHERE name = 'ActQueue')
BEGIN
    ALTER QUEUE [dbo].[ActQueue]
    WITH ACTIVATION (
        STATUS = ON,
        PROCEDURE_NAME = dbo.sp_Act,
        MAX_QUEUE_READERS = 1,
        EXECUTE AS OWNER
    );
    PRINT '  ✓ ActQueue activation enabled (sp_Act)';
END
ELSE
    PRINT '  ✗ ActQueue not found';

-- ============================================================================
-- LearnQueue Activation (Learn & Measure Phase)
-- ============================================================================
PRINT 'Configuring LearnQueue...';

IF EXISTS (SELECT 1 FROM sys.service_queues WHERE name = 'LearnQueue')
BEGIN
    ALTER QUEUE [dbo].[LearnQueue]
    WITH ACTIVATION (
        STATUS = ON,
        PROCEDURE_NAME = dbo.sp_Learn,
        MAX_QUEUE_READERS = 1,
        EXECUTE AS OWNER
    );
    PRINT '  ✓ LearnQueue activation enabled (sp_Learn)';
END
ELSE
    PRINT '  ✗ LearnQueue not found';

-- ============================================================================
-- Verification
-- ============================================================================
PRINT '';
PRINT '=== Verification ===';
PRINT '';

SELECT 
    q.name AS QueueName,
    q.is_activation_enabled AS ActivationEnabled,
    q.max_readers AS MaxReaders,
    q.is_receive_enabled AS ReceiveEnabled,
    q.is_enqueue_enabled AS EnqueueEnabled
FROM sys.service_queues q
WHERE q.name IN ('AnalyzeQueue', 'HypothesizeQueue', 'ActQueue', 'LearnQueue')
ORDER BY q.name;

-- Check queue monitors (should show RECEIVES_OCCURRING when active)
PRINT '';
PRINT 'Queue Monitors:';
SELECT database_id, queue_id, state, tasks_waiting, last_activated_time
FROM sys.dm_broker_queue_monitors
WHERE database_id = DB_ID();

PRINT '';
PRINT '✓ Service Broker activation configuration complete';
PRINT '';
GO
"@

try {
    Write-Host "`nExecuting configuration..." -ForegroundColor Yellow
    
    $tempFile = [System.IO.Path]::GetTempFileName()
    $configScript | Out-File -FilePath $tempFile -Encoding UTF8
    
    sqlcmd -S $Server -d $Database -C -i $tempFile -W -h -1
    
    if ($LASTEXITCODE -ne 0) {
        throw "Configuration failed with exit code $LASTEXITCODE"
    }
    
    Remove-Item $tempFile -Force -ErrorAction SilentlyContinue
    
    Write-Host "`n✓ Service Broker activation configured successfully" -ForegroundColor Green
    Write-Host "`nNext Steps:" -ForegroundColor White
    Write-Host "  1. Manually trigger OODA loop: EXEC dbo.sp_Analyze;" -ForegroundColor Gray
    Write-Host "  2. Check history: SELECT * FROM dbo.AutonomousImprovementHistory;" -ForegroundColor Gray
    Write-Host "  3. Start SQL Agent job: EXEC msdb.dbo.sp_start_job @job_name = 'Hartonomous_Cognitive_Kernel';" -ForegroundColor Gray
    
    exit 0
}
catch {
    Write-Host "`n✗ Configuration failed: $_" -ForegroundColor Red
    exit 1
}
