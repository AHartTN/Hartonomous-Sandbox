-- ???????????????????????????????????????????????????????????????????
-- HARTONOMOUS DATABASE TEST SUITE - RUNNER
-- Executes all tests and generates XML results for Azure DevOps
-- ???????????????????????????????????????????????????????????????????

SET NOCOUNT ON;
SET XACT_ABORT ON;

PRINT '???????????????????????????????????????????????????????';
PRINT 'HARTONOMOUS DATABASE TEST SUITE';
PRINT 'Execution Started: ' + CONVERT(VARCHAR(30), GETUTCDATE(), 121);
PRINT '???????????????????????????????????????????????????????';
PRINT '';

-- Create test results table
IF OBJECT_ID('tempdb..#TestResults') IS NOT NULL DROP TABLE #TestResults;
CREATE TABLE #TestResults (
    TestSuite VARCHAR(100),
    TestName VARCHAR(200),
    Status VARCHAR(10),
    Duration INT,
    ErrorMessage VARCHAR(MAX),
    ExecutedAt DATETIME2 DEFAULT GETUTCDATE()
);

DECLARE @TotalTests INT = 0;
DECLARE @PassedTests INT = 0;
DECLARE @FailedTests INT = 0;
DECLARE @StartTime DATETIME2;
DECLARE @EndTime DATETIME2;
DECLARE @TestSuite VARCHAR(100);
DECLARE @TestName VARCHAR(200);

-- ???????????????????????????????????????????????????????????????????
-- TEST SUITE 1: SMOKE TESTS
-- ???????????????????????????????????????????????????????????????????
SET @TestSuite = 'SmokeTests';
PRINT 'Running: ' + @TestSuite;
PRINT REPLICATE('-', 60);

-- Test 1.1: CLR Functions Exist
SET @TestName = 'CLR_Functions_Exist';
SET @StartTime = SYSUTCDATETIME();
BEGIN TRY
    IF OBJECT_ID('dbo.fn_ProjectTo3D') IS NULL
        THROW 50000, 'fn_ProjectTo3D not found', 1;
    IF OBJECT_ID('dbo.clr_ComputeHilbertValue') IS NULL
        THROW 50000, 'clr_ComputeHilbertValue not found', 1;
    
    SET @EndTime = SYSUTCDATETIME();
    INSERT INTO #TestResults (TestSuite, TestName, Status, Duration)
    VALUES (@TestSuite, @TestName, 'PASSED', DATEDIFF(MILLISECOND, @StartTime, @EndTime));
    
    PRINT '  ? ' + @TestName;
    SET @PassedTests = @PassedTests + 1;
END TRY
BEGIN CATCH
    SET @EndTime = SYSUTCDATETIME();
    INSERT INTO #TestResults (TestSuite, TestName, Status, Duration, ErrorMessage)
    VALUES (@TestSuite, @TestName, 'FAILED', DATEDIFF(MILLISECOND, @StartTime, @EndTime), ERROR_MESSAGE());
    
    PRINT '  ? ' + @TestName + ': ' + ERROR_MESSAGE();
    SET @FailedTests = @FailedTests + 1;
END CATCH;
SET @TotalTests = @TotalTests + 1;

-- Test 1.2: Spatial Projection Works
SET @TestName = 'Spatial_Projection_Works';
SET @StartTime = SYSUTCDATETIME();
BEGIN TRY
    DECLARE @testVec VARBINARY(MAX) = CAST(REPLICATE(0x3F800000, 1998) AS VARBINARY(MAX));
    DECLARE @projected GEOMETRY = dbo.fn_ProjectTo3D(@testVec);
    
    IF @projected IS NULL OR @projected.STX IS NULL
        THROW 50000, 'Projection failed', 1;
    
    SET @EndTime = SYSUTCDATETIME();
    INSERT INTO #TestResults (TestSuite, TestName, Status, Duration)
    VALUES (@TestSuite, @TestName, 'PASSED', DATEDIFF(MILLISECOND, @StartTime, @EndTime));
    
    PRINT '  ? ' + @TestName;
    SET @PassedTests = @PassedTests + 1;
END TRY
BEGIN CATCH
    SET @EndTime = SYSUTCDATETIME();
    INSERT INTO #TestResults (TestSuite, TestName, Status, Duration, ErrorMessage)
    VALUES (@TestSuite, @TestName, 'FAILED', DATEDIFF(MILLISECOND, @StartTime, @EndTime), ERROR_MESSAGE());
    
    PRINT '  ? ' + @TestName + ': ' + ERROR_MESSAGE();
    SET @FailedTests = @FailedTests + 1;
END CATCH;
SET @TotalTests = @TotalTests + 1;

-- Test 1.3: Service Broker Enabled
SET @TestName = 'Service_Broker_Enabled';
SET @StartTime = SYSUTCDATETIME();
BEGIN TRY
    IF NOT EXISTS (SELECT 1 FROM sys.databases WHERE name = DB_NAME() AND is_broker_enabled = 1)
        THROW 50000, 'Service Broker not enabled', 1;
    
    SET @EndTime = SYSUTCDATETIME();
    INSERT INTO #TestResults (TestSuite, TestName, Status, Duration)
    VALUES (@TestSuite, @TestName, 'PASSED', DATEDIFF(MILLISECOND, @StartTime, @EndTime));
    
    PRINT '  ? ' + @TestName;
    SET @PassedTests = @PassedTests + 1;
END TRY
BEGIN CATCH
    SET @EndTime = SYSUTCDATETIME();
    INSERT INTO #TestResults (TestSuite, TestName, Status, Duration, ErrorMessage)
    VALUES (@TestSuite, @TestName, 'FAILED', DATEDIFF(MILLISECOND, @StartTime, @EndTime), ERROR_MESSAGE());
    
    PRINT '  ? ' + @TestName + ': ' + ERROR_MESSAGE();
    SET @FailedTests = @FailedTests + 1;
END CATCH;
SET @TotalTests = @TotalTests + 1;

-- Test 1.4: Spatial Indexes Exist
SET @TestName = 'Spatial_Indexes_Exist';
SET @StartTime = SYSUTCDATETIME();
BEGIN TRY
    IF NOT EXISTS (
        SELECT 1 FROM sys.indexes
        WHERE name = 'SIX_AtomEmbeddings_SpatialKey'
          AND object_id = OBJECT_ID('dbo.AtomEmbeddings')
    )
        THROW 50000, 'Spatial index SIX_AtomEmbeddings_SpatialKey missing', 1;
    
    SET @EndTime = SYSUTCDATETIME();
    INSERT INTO #TestResults (TestSuite, TestName, Status, Duration)
    VALUES (@TestSuite, @TestName, 'PASSED', DATEDIFF(MILLISECOND, @StartTime, @EndTime));
    
    PRINT '  ? ' + @TestName;
    SET @PassedTests = @PassedTests + 1;
END TRY
BEGIN CATCH
    SET @EndTime = SYSUTCDATETIME();
    INSERT INTO #TestResults (TestSuite, TestName, Status, Duration, ErrorMessage)
    VALUES (@TestSuite, @TestName, 'FAILED', DATEDIFF(MILLISECOND, @StartTime, @EndTime), ERROR_MESSAGE());
    
    PRINT '  ? ' + @TestName + ': ' + ERROR_MESSAGE();
    SET @FailedTests = @FailedTests + 1;
END CATCH;
SET @TotalTests = @TotalTests + 1;

PRINT '';

-- ???????????????????????????????????????????????????????????????????
-- TEST SUITE 2: SCHEMA VALIDATION
-- ???????????????????????????????????????????????????????????????????
SET @TestSuite = 'SchemaValidation';
PRINT 'Running: ' + @TestSuite;
PRINT REPLICATE('-', 60);

-- Test 2.1: Core Tables Exist
SET @TestName = 'Core_Tables_Exist';
SET @StartTime = SYSUTCDATETIME();
BEGIN TRY
    IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name IN ('Atoms', 'AtomEmbeddings', 'Models', 'InferenceRequests'))
        THROW 50000, 'Core tables missing', 1;
    
    SET @EndTime = SYSUTCDATETIME();
    INSERT INTO #TestResults (TestSuite, TestName, Status, Duration)
    VALUES (@TestSuite, @TestName, 'PASSED', DATEDIFF(MILLISECOND, @StartTime, @EndTime));
    
    PRINT '  ? ' + @TestName;
    SET @PassedTests = @PassedTests + 1;
END TRY
BEGIN CATCH
    SET @EndTime = SYSUTCDATETIME();
    INSERT INTO #TestResults (TestSuite, TestName, Status, Duration, ErrorMessage)
    VALUES (@TestSuite, @TestName, 'FAILED', DATEDIFF(MILLISECOND, @StartTime, @EndTime), ERROR_MESSAGE());
    
    PRINT '  ? ' + @TestName + ': ' + ERROR_MESSAGE();
    SET @FailedTests = @FailedTests + 1;
END CATCH;
SET @TotalTests = @TotalTests + 1;

-- Test 2.2: Foreign Keys Exist
SET @TestName = 'Foreign_Keys_Exist';
SET @StartTime = SYSUTCDATETIME();
BEGIN TRY
    IF NOT EXISTS (
        SELECT 1 FROM sys.foreign_keys 
        WHERE name IN ('FK_AtomEmbeddings_Atom', 'FK_AtomEmbeddings_Model')
    )
        THROW 50000, 'Required foreign keys missing', 1;
    
    SET @EndTime = SYSUTCDATETIME();
    INSERT INTO #TestResults (TestSuite, TestName, Status, Duration)
    VALUES (@TestSuite, @TestName, 'PASSED', DATEDIFF(MILLISECOND, @StartTime, @EndTime));
    
    PRINT '  ? ' + @TestName;
    SET @PassedTests = @PassedTests + 1;
END TRY
BEGIN CATCH
    SET @EndTime = SYSUTCDATETIME();
    INSERT INTO #TestResults (TestSuite, TestName, Status, Duration, ErrorMessage)
    VALUES (@TestSuite, @TestName, 'FAILED', DATEDIFF(MILLISECOND, @StartTime, @EndTime), ERROR_MESSAGE());
    
    PRINT '  ? ' + @TestName + ': ' + ERROR_MESSAGE();
    SET @FailedTests = @FailedTests + 1;
END CATCH;
SET @TotalTests = @TotalTests + 1;

PRINT '';

-- ???????????????????????????????????????????????????????????????????
-- GENERATE NUnit XML OUTPUT
-- ???????????????????????????????????????????????????????????????????
DECLARE @XmlOutput XML;
DECLARE @TestDuration DECIMAL(10,3) = (SELECT SUM(Duration) / 1000.0 FROM #TestResults);

SELECT @XmlOutput = (
    SELECT 
        DB_NAME() AS [@name],
        @TotalTests AS [@total],
        @PassedTests AS [@passed],
        @FailedTests AS [@failed],
        0 AS [@inconclusive],
        0 AS [@skipped],
        'Success' AS [@result],
        CONVERT(VARCHAR(30), MIN(ExecutedAt), 127) AS [@start-time],
        CONVERT(VARCHAR(30), MAX(ExecutedAt), 127) AS [@end-time],
        @TestDuration AS [@duration],
        (
            SELECT 
                TestSuite AS [@name],
                'Success' AS [@result],
                SUM(Duration) / 1000.0 AS [@duration],
                COUNT(*) AS [@total],
                SUM(CASE WHEN Status = 'PASSED' THEN 1 ELSE 0 END) AS [@passed],
                SUM(CASE WHEN Status = 'FAILED' THEN 1 ELSE 0 END) AS [@failed],
                (
                    SELECT 
                        t2.TestName AS [@name],
                        t2.Status AS [@result],
                        t2.Duration / 1000.0 AS [@duration],
                        CONVERT(VARCHAR(30), t2.ExecutedAt, 127) AS [@start-time],
                        CONVERT(VARCHAR(30), t2.ExecutedAt, 127) AS [@end-time],
                        (
                            SELECT t2.ErrorMessage AS [message]
                            WHERE t2.Status = 'FAILED'
                            FOR XML PATH('failure'), TYPE
                        )
                    FROM #TestResults t2
                    WHERE t2.TestSuite = t1.TestSuite
                    FOR XML PATH('test-case'), TYPE
                )
            FROM #TestResults t1
            GROUP BY TestSuite
            FOR XML PATH('test-suite'), TYPE
        )
    FROM #TestResults
    FOR XML PATH('test-run'), TYPE
);

-- Output XML to file (requires xp_cmdshell or external process)
PRINT '';
PRINT '???????????????????????????????????????????????????????';
PRINT 'TEST RESULTS SUMMARY';
PRINT '???????????????????????????????????????????????????????';
PRINT 'Total Tests:   ' + CAST(@TotalTests AS VARCHAR(10));
PRINT 'Passed:        ' + CAST(@PassedTests AS VARCHAR(10));
PRINT 'Failed:        ' + CAST(@FailedTests AS VARCHAR(10));
PRINT 'Success Rate:  ' + CAST(CAST(@PassedTests AS FLOAT) / @TotalTests * 100 AS VARCHAR(10)) + '%';
PRINT 'Duration:      ' + CAST(@TestDuration AS VARCHAR(10)) + 's';
PRINT '???????????????????????????????????????????????????????';
PRINT '';

-- Output results table for inspection
SELECT 
    TestSuite,
    TestName,
    Status,
    Duration,
    ErrorMessage
FROM #TestResults
ORDER BY TestSuite, TestName;

-- Save XML to table for external export
IF OBJECT_ID('dbo.TestRunResults') IS NOT NULL DROP TABLE dbo.TestRunResults;
SELECT 
    GETUTCDATE() AS ExecutedAt,
    @TotalTests AS TotalTests,
    @PassedTests AS PassedTests,
    @FailedTests AS FailedTests,
    @TestDuration AS Duration,
    CAST(@XmlOutput AS NVARCHAR(MAX)) AS ResultsXml
INTO dbo.TestRunResults;

-- Return exit code
IF @FailedTests > 0
BEGIN
    PRINT '? TESTS FAILED';
    THROW 50000, 'Test suite failed', 1;
END
ELSE
BEGIN
    PRINT '? ALL TESTS PASSED';
END
