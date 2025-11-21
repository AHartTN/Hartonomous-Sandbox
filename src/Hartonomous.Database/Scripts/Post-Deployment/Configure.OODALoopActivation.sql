/*
================================================================================
Configure OODA Loop Queue Activation
================================================================================
Purpose:
  Enable Service Broker activation for the autonomous OODA loop queues.
  This ensures stored procedures are automatically triggered when messages arrive.

OODA Loop Flow:
  AnalyzeQueue → sp_Analyze (Observation & Analysis)
    ↓
  HypothesizeQueue → sp_Hypothesize (Orient & Hypothesize)
    ↓
  ActQueue → sp_Act (Decide & Act)
    ↓
  LearnQueue → sp_Learn (Learn & Measure)

Activation Settings:
  - STATUS = ON: Enable automatic activation
  - MAX_QUEUE_READERS = 1: Single-threaded processing for serialization
  - EXECUTE AS OWNER: Run with db_owner permissions

Idempotency:
  - Safe to run multiple times
  - Alters existing queue configuration

Prerequisites:
  - Service Broker enabled (checked in Pre-Deployment)
  - Stored procedures created (sp_Analyze, sp_Hypothesize, sp_Act, sp_Learn)
  - Queues created (ServiceBroker\Queues\dbo.*.sql)

Notes:
  - MAX_QUEUE_READERS=1 ensures sequential processing of OODA steps
  - Higher values (5-10) would allow parallel execution but risk race conditions
  - EXECUTE AS OWNER grants necessary permissions for CLR function calls
================================================================================
*/

PRINT '================================================================================';
PRINT 'POST-DEPLOYMENT: Configure OODA Loop Queue Activation';
PRINT '================================================================================';
PRINT '';
GO

-- ============================================================================
-- HypothesizeQueue Activation (Orient Phase)
-- ============================================================================
PRINT 'Configuring HypothesizeQueue activation...';

IF EXISTS (SELECT 1 FROM sys.service_queues WHERE name = 'HypothesizeQueue')
BEGIN
    ALTER QUEUE [dbo].[HypothesizeQueue]
    WITH ACTIVATION (
        STATUS = ON,
        PROCEDURE_NAME = dbo.sp_Hypothesize,
        MAX_QUEUE_READERS = 1,
        EXECUTE AS OWNER
    );
    PRINT '  ✓ HypothesizeQueue activation enabled (1 reader, sp_Hypothesize)';
END
ELSE
    PRINT '  ✗ HypothesizeQueue not found (check ServiceBroker\Queues deployment)';

PRINT '';
GO

-- ============================================================================
-- ActQueue Activation (Decide & Act Phase)
-- ============================================================================
PRINT 'Configuring ActQueue activation...';

IF EXISTS (SELECT 1 FROM sys.service_queues WHERE name = 'ActQueue')
BEGIN
    ALTER QUEUE [dbo].[ActQueue]
    WITH ACTIVATION (
        STATUS = ON,
        PROCEDURE_NAME = dbo.sp_Act,
        MAX_QUEUE_READERS = 1,
        EXECUTE AS OWNER
    );
    PRINT '  ✓ ActQueue activation enabled (1 reader, sp_Act)';
END
ELSE
    PRINT '  ✗ ActQueue not found (check ServiceBroker\Queues deployment)';

PRINT '';
GO

-- ============================================================================
-- LearnQueue Activation (Learn & Measure Phase)
-- ============================================================================
PRINT 'Configuring LearnQueue activation...';

IF EXISTS (SELECT 1 FROM sys.service_queues WHERE name = 'LearnQueue')
BEGIN
    ALTER QUEUE [dbo].[LearnQueue]
    WITH ACTIVATION (
        STATUS = ON,
        PROCEDURE_NAME = dbo.sp_Learn,
        MAX_QUEUE_READERS = 1,
        EXECUTE AS OWNER
    );
    PRINT '  ✓ LearnQueue activation enabled (1 reader, sp_Learn)';
END
ELSE
    PRINT '  ✗ LearnQueue not found (check ServiceBroker\Queues deployment)';

PRINT '';
GO

-- ============================================================================
-- AnalyzeQueue Activation (Observe Phase) - MANUAL TRIGGER ONLY
-- ============================================================================
PRINT 'Configuring AnalyzeQueue (manual trigger)...';
PRINT '  Note: AnalyzeQueue is triggered by SQL Agent job, not Service Broker';
PRINT '  Job: Hartonomous_Cognitive_Kernel (runs every 15 minutes)';
PRINT '  ○ No activation needed for AnalyzeQueue';

PRINT '';
GO

PRINT '================================================================================';
PRINT 'OODA LOOP ACTIVATION COMPLETE';
PRINT '================================================================================';
-- Verify activation configuration using DMVs
PRINT '';
PRINT '=== Activation Verification ===';
PRINT '';

-- Check queue configuration
SELECT 
    q.name AS QueueName,
    q.is_activation_enabled AS ActivationEnabled,
    q.max_readers AS MaxReaders,
    q.is_receive_enabled AS ReceiveEnabled,
    q.is_enqueue_enabled AS EnqueueEnabled
FROM sys.service_queues q
WHERE q.name IN ('HypothesizeQueue', 'ActQueue', 'LearnQueue')
ORDER BY q.name;

PRINT '';
PRINT 'Verification Commands:';
PRINT '  -- Check active queue monitors:';
PRINT '  SELECT * FROM sys.dm_broker_queue_monitors;';
PRINT '';
PRINT '  -- Check running activation tasks:';
PRINT '  SELECT * FROM sys.dm_broker_activated_tasks;';
PRINT '';
PRINT '  -- Manually trigger OODA loop:';
PRINT '  EXEC dbo.sp_Analyze;';
PRINT '';
PRINT '  -- Check improvement history:';
PRINT '  SELECT TOP 10 * FROM dbo.AutonomousImprovementHistory ORDER BY StartedAt DESC;';
PRINT '';
GO
