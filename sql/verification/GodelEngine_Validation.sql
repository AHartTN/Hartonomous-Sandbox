-- Gödel Engine Validation Script
-- Tests the autonomous compute loop using prime number search as the validation task
-- This script validates that the OODA loop can process abstract computational problems

USE Hartonomous;
GO

PRINT '========================================';
PRINT 'GÖDEL ENGINE VALIDATION TEST SUITE';
PRINT '========================================';
PRINT '';

-- =====================================================
-- TEST 1: Verify Infrastructure
-- =====================================================
PRINT 'TEST 1: Infrastructure Verification';
PRINT '-------------------------------------';

-- Check AutonomousComputeJobs table exists
IF OBJECT_ID('dbo.AutonomousComputeJobs', 'U') IS NULL
BEGIN
    RAISERROR('FAILED: AutonomousComputeJobs table not found. Run deploy-database-unified.ps1 first.', 16, 1);
END
ELSE
BEGIN
    PRINT '✓ AutonomousComputeJobs table exists';
END

-- Check CLR function exists
IF OBJECT_ID('dbo.clr_FindPrimes', 'FS') IS NULL
BEGIN
    RAISERROR('FAILED: clr_FindPrimes CLR function not found. Deploy CLR assemblies first.', 16, 1);
END
ELSE
BEGIN
    PRINT '✓ clr_FindPrimes CLR function exists';
END

-- Check OODA loop procedures exist
DECLARE @MissingProcs TABLE (ProcName NVARCHAR(256));

INSERT INTO @MissingProcs
SELECT 'dbo.sp_Analyze' WHERE OBJECT_ID('dbo.sp_Analyze', 'P') IS NULL
UNION ALL
SELECT 'dbo.sp_Hypothesize' WHERE OBJECT_ID('dbo.sp_Hypothesize', 'P') IS NULL
UNION ALL
SELECT 'dbo.sp_Act' WHERE OBJECT_ID('dbo.sp_Act', 'P') IS NULL
UNION ALL
SELECT 'dbo.sp_Learn' WHERE OBJECT_ID('dbo.sp_Learn', 'P') IS NULL
UNION ALL
SELECT 'dbo.sp_StartPrimeSearch' WHERE OBJECT_ID('dbo.sp_StartPrimeSearch', 'P') IS NULL;

IF EXISTS (SELECT 1 FROM @MissingProcs)
BEGIN
    DECLARE @Missing NVARCHAR(MAX);
    SELECT @Missing = STRING_AGG(ProcName, ', ') FROM @MissingProcs;
    RAISERROR('FAILED: Missing procedures: %s', 16, 1, @Missing);
END
ELSE
BEGIN
    PRINT '✓ All OODA loop procedures exist';
END

-- Check Service Broker services
IF NOT EXISTS (SELECT 1 FROM sys.services WHERE name = 'AnalyzeService')
   OR NOT EXISTS (SELECT 1 FROM sys.services WHERE name = 'HypothesizeService')
   OR NOT EXISTS (SELECT 1 FROM sys.services WHERE name = 'ActService')
   OR NOT EXISTS (SELECT 1 FROM sys.services WHERE name = 'LearnService')
   OR NOT EXISTS (SELECT 1 FROM sys.services WHERE name = '//Hartonomous/Service/Initiator')
BEGIN
    RAISERROR('FAILED: Service Broker services not configured. Run setup-service-broker.sql.', 16, 1);
END
ELSE
BEGIN
    PRINT '✓ Service Broker services configured';
END

PRINT '';
PRINT 'Infrastructure verification complete.';
PRINT '';

-- =====================================================
-- TEST 2: CLR Function Smoke Test
-- =====================================================
PRINT 'TEST 2: CLR Function Smoke Test';
PRINT '--------------------------------';

DECLARE @SmallRangeResult NVARCHAR(MAX);
SET @SmallRangeResult = dbo.clr_FindPrimes(2, 20);

PRINT 'Primes from 2 to 20: ' + @SmallRangeResult;

-- Validate result
DECLARE @ExpectedPrimes NVARCHAR(MAX) = '[2,3,5,7,11,13,17,19]';
IF @SmallRangeResult != @ExpectedPrimes
BEGIN
    PRINT '⚠ WARNING: CLR function result differs from expected. Check implementation.';
    PRINT '  Expected: ' + @ExpectedPrimes;
    PRINT '  Got:      ' + @SmallRangeResult;
END
ELSE
BEGIN
    PRINT '✓ CLR function produces correct results';
END

PRINT '';

-- =====================================================
-- TEST 3: Job Creation and State Management
-- =====================================================
PRINT 'TEST 3: Job Creation Test';
PRINT '-------------------------';

-- Clean up any previous test jobs
DELETE FROM dbo.AutonomousComputeJobs WHERE JobType = 'PrimeSearch';

-- Start a small autonomous job
PRINT 'Starting prime search job for range [2, 1000]...';
EXEC dbo.sp_StartPrimeSearch @RangeStart = 2, @RangeEnd = 1000;

-- Wait a moment for the job to initialize
WAITFOR DELAY '00:00:01';

-- Check job was created
DECLARE @JobCount INT;
SELECT @JobCount = COUNT(*) FROM dbo.AutonomousComputeJobs WHERE JobType = 'PrimeSearch' AND Status = 'Running';

IF @JobCount = 0
BEGIN
    RAISERROR('FAILED: Job was not created in AutonomousComputeJobs table.', 16, 1);
END
ELSE
BEGIN
    PRINT '✓ Job created successfully';
    
    -- Display job details
    SELECT TOP 1
        JobId,
        JobType,
        Status,
        JobParameters,
        CurrentState,
        CreatedAt
    FROM dbo.AutonomousComputeJobs
    WHERE JobType = 'PrimeSearch'
    ORDER BY CreatedAt DESC;
END

PRINT '';

-- =====================================================
-- TEST 4: Service Broker Message Flow
-- =====================================================
PRINT 'TEST 4: Service Broker Message Flow';
PRINT '------------------------------------';

-- Check for messages in queues
SELECT 
    'AnalyzeQueue' AS QueueName,
    COUNT(*) AS MessageCount
FROM AnalyzeQueue
UNION ALL
SELECT 'HypothesizeQueue', COUNT(*) FROM HypothesizeQueue
UNION ALL
SELECT 'ActQueue', COUNT(*) FROM ActQueue
UNION ALL
SELECT 'LearnQueue', COUNT(*) FROM LearnQueue;

-- Check for transmission errors
IF EXISTS (SELECT 1 FROM sys.transmission_queue WHERE transmission_status != '')
BEGIN
    PRINT '⚠ WARNING: Service Broker transmission errors detected:';
    SELECT 
        from_service_name,
        to_service_name,
        transmission_status,
        message_type_name
    FROM sys.transmission_queue
    WHERE transmission_status != '';
END
ELSE
BEGIN
    PRINT '✓ No Service Broker transmission errors';
END

PRINT '';

-- =====================================================
-- TEST 5: Wait for Job Completion (Small Job)
-- =====================================================
PRINT 'TEST 5: Autonomous Execution Test';
PRINT '----------------------------------';
PRINT 'Waiting for job to complete autonomously (timeout: 60 seconds)...';

DECLARE @WaitCount INT = 0;
DECLARE @JobStatus NVARCHAR(50);
DECLARE @TestJobId UNIQUEIDENTIFIER;

SELECT TOP 1 @TestJobId = JobId 
FROM dbo.AutonomousComputeJobs 
WHERE JobType = 'PrimeSearch' 
ORDER BY CreatedAt DESC;

WHILE @WaitCount < 60
BEGIN
    SELECT @JobStatus = Status 
    FROM dbo.AutonomousComputeJobs 
    WHERE JobId = @TestJobId;
    
    IF @JobStatus = 'Completed'
    BEGIN
        PRINT '✓ Job completed successfully after ' + CAST(@WaitCount AS NVARCHAR(10)) + ' seconds';
        
        -- Display results
        SELECT 
            JobId,
            Status,
            JSON_VALUE(CurrentState, '$.lastChecked') AS LastChecked,
            LEN(Results) AS ResultsSize,
            CompletedAt
        FROM dbo.AutonomousComputeJobs
        WHERE JobId = @TestJobId;
        
        BREAK;
    END
    
    WAITFOR DELAY '00:00:01';
    SET @WaitCount = @WaitCount + 1;
END

IF @JobStatus != 'Completed'
BEGIN
    PRINT '⚠ WARNING: Job did not complete within timeout. Status: ' + ISNULL(@JobStatus, 'NULL');
    PRINT '  This may indicate the OODA loop is not running autonomously.';
    PRINT '  Manually execute sp_Analyze, sp_Hypothesize, sp_Act, sp_Learn to process the job.';
END

PRINT '';

-- =====================================================
-- TEST 6: Result Validation
-- =====================================================
PRINT 'TEST 6: Result Validation';
PRINT '-------------------------';

DECLARE @JobResults NVARCHAR(MAX);
SELECT @JobResults = Results 
FROM dbo.AutonomousComputeJobs 
WHERE JobId = @TestJobId AND Status = 'Completed';

IF @JobResults IS NOT NULL
BEGIN
    DECLARE @PrimeCount INT = (SELECT COUNT(*) FROM OPENJSON(@JobResults));
    PRINT 'Primes found: ' + CAST(@PrimeCount AS NVARCHAR(10));
    
    -- Expected: 168 primes between 2 and 1000
    IF @PrimeCount = 168
    BEGIN
        PRINT '✓ Correct number of primes found (168)';
    END
    ELSE
    BEGIN
        PRINT '⚠ WARNING: Expected 168 primes, found ' + CAST(@PrimeCount AS NVARCHAR(10));
    END
    
    -- Display first 10 primes
    PRINT 'First 10 primes found:';
    SELECT TOP 10 value AS Prime 
    FROM OPENJSON(@JobResults)
    ORDER BY CAST(value AS BIGINT);
END
ELSE
BEGIN
    PRINT '⚠ WARNING: No results available. Job may not have completed successfully.';
END

PRINT '';

-- =====================================================
-- SUMMARY
-- =====================================================
PRINT '========================================';
PRINT 'VALIDATION SUMMARY';
PRINT '========================================';

DECLARE @TotalTests INT = 6;
DECLARE @PassedTests INT = 0;

IF OBJECT_ID('dbo.AutonomousComputeJobs', 'U') IS NOT NULL SET @PassedTests = @PassedTests + 1;
IF OBJECT_ID('dbo.clr_FindPrimes', 'FS') IS NOT NULL SET @PassedTests = @PassedTests + 1;
IF @SmallRangeResult = @ExpectedPrimes SET @PassedTests = @PassedTests + 1;
IF @JobCount > 0 SET @PassedTests = @PassedTests + 1;
IF NOT EXISTS (SELECT 1 FROM sys.transmission_queue WHERE transmission_status != '') SET @PassedTests = @PassedTests + 1;
IF @JobStatus = 'Completed' SET @PassedTests = @PassedTests + 1;

PRINT 'Tests Passed: ' + CAST(@PassedTests AS NVARCHAR(10)) + '/' + CAST(@TotalTests AS NVARCHAR(10));

IF @PassedTests = @TotalTests
BEGIN
    PRINT '';
    PRINT '✓✓✓ ALL TESTS PASSED ✓✓✓';
    PRINT 'The Gödel Engine is operational and can process autonomous compute tasks.';
END
ELSE
BEGIN
    PRINT '';
    PRINT '⚠⚠⚠ SOME TESTS FAILED ⚠⚠⚠';
    PRINT 'Review the output above for details. Common issues:';
    PRINT '  1. CLR assemblies not deployed (run deploy-clr-final.ps1)';
    PRINT '  2. Service Broker not configured (run setup-service-broker.sql)';
    PRINT '  3. Procedures need queue activation (see docs/GODEL_ENGINE_IMPLEMENTATION.md)';
END

PRINT '';
PRINT 'For detailed implementation guide, see docs/GODEL_ENGINE_IMPLEMENTATION.md';
PRINT '';
GO
