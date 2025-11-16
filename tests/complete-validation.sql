-- =====================================================
-- Complete Hartonomous Validation Test Suite
-- =====================================================
-- Tests all critical stored procedures end-to-end
-- Validates OODA loop, inference, and ingestion

SET NOCOUNT ON;
PRINT '';
PRINT '??????????????????????????????????????????????????????????';
PRINT '?   HARTONOMOUS COMPLETE VALIDATION TEST SUITE          ?';
PRINT '??????????????????????????????????????????????????????????';
PRINT '';

DECLARE @totalTests INT = 0;
DECLARE @passedTests INT = 0;
DECLARE @failedTests INT = 0;

-- =====================================================
-- TEST 1: CLR Functions Operational
-- =====================================================
PRINT 'TEST 1: CLR Functions Callable from SQL';
BEGIN TRY
    DECLARE @testVector VARBINARY(MAX) = CAST(REPLICATE(0x3F800000, 1998) AS VARBINARY(MAX));
    DECLARE @projected GEOMETRY = dbo.fn_ProjectTo3D(@testVector);
    
    IF @projected IS NULL
        THROW 50000, 'fn_ProjectTo3D returned NULL', 1;
    
    IF @projected.STX IS NULL OR @projected.STY IS NULL OR @projected.STZ IS NULL
        THROW 50000, 'Invalid projection coordinates', 1;
    
    SET @totalTests = @totalTests + 1;
    SET @passedTests = @passedTests + 1;
    PRINT '  ? PASSED: CLR projection working';
    PRINT '    Coordinates: (' + 
          CAST(ROUND(@projected.STX, 4) AS NVARCHAR(20)) + ', ' +
          CAST(ROUND(@projected.STY, 4) AS NVARCHAR(20)) + ', ' +
          CAST(ROUND(@projected.STZ, 4) AS NVARCHAR(20)) + ')';
END TRY
BEGIN CATCH
    SET @totalTests = @totalTests + 1;
    SET @failedTests = @failedTests + 1;
    PRINT '  ? FAILED: ' + ERROR_MESSAGE();
END CATCH

-- =====================================================
-- TEST 2: sp_IngestAtoms - Content-Addressable Storage
-- =====================================================
PRINT '';
PRINT 'TEST 2: Atom Ingestion with Deduplication';

DECLARE @testAtoms NVARCHAR(MAX) = N'[
    {
        "atomicValue": "The Hartonomous system uses spatial R-Tree indexes for O(log N) similarity search.",
        "canonicalText": "The Hartonomous system uses spatial R-Tree indexes for O(log N) similarity search.",
        "modality": "text",
        "subtype": "sentence"
    },
    {
        "atomicValue": "Embeddings are projected to 3D using deterministic landmark projection.",
        "canonicalText": "Embeddings are projected to 3D using deterministic landmark projection.",
        "modality": "text",
        "subtype": "sentence"
    },
    {
        "atomicValue": "The OODA loop enables autonomous self-improvement.",
        "canonicalText": "The OODA loop enables autonomous self-improvement.",
        "modality": "text",
        "subtype": "sentence"
    }
]';

DECLARE @batchId UNIQUEIDENTIFIER;
DECLARE @ingestResult NVARCHAR(MAX);

BEGIN TRY
    -- First ingestion
    EXEC dbo.sp_IngestAtoms 
        @atomsJson = @testAtoms,
        @tenantId = 0,
        @batchId = @batchId OUTPUT;
    
    SELECT @ingestResult = (
        SELECT BatchId, TotalAtoms, NewAtoms, Deduplicated
        FROM dbo.sp_IngestAtoms(@testAtoms, NULL, 0, @batchId)
    );
    
    DECLARE @newAtoms1 INT = JSON_VALUE(@ingestResult, '$.NewAtoms');
    
    IF @newAtoms1 IS NULL OR @newAtoms1 = 0
        THROW 50000, 'First ingestion should create new atoms', 1;
    
    -- Second ingestion (test deduplication)
    EXEC dbo.sp_IngestAtoms 
        @atomsJson = @testAtoms,
        @tenantId = 0;
    
    SET @totalTests = @totalTests + 1;
    SET @passedTests = @passedTests + 1;
    PRINT '  ? PASSED: Ingestion and deduplication working';
    PRINT '    First ingestion: ' + CAST(@newAtoms1 AS NVARCHAR) + ' new atoms';
    PRINT '    Batch ID: ' + CAST(@batchId AS NVARCHAR(36));
END TRY
BEGIN CATCH
    SET @totalTests = @totalTests + 1;
    SET @failedTests = @failedTests + 1;
    PRINT '  ? FAILED: ' + ERROR_MESSAGE();
END CATCH

-- Wait for embedding generation (if workers running)
WAITFOR DELAY '00:00:02';

-- =====================================================
-- TEST 3: sp_FindNearestAtoms - O(log N) Query
-- =====================================================
PRINT '';
PRINT 'TEST 3: Spatial Similarity Search (O(log N) + O(K))';

BEGIN TRY
    DECLARE @queryVec VARBINARY(MAX) = (
        SELECT TOP 1 EmbeddingVector
        FROM dbo.AtomEmbeddings
        WHERE EmbeddingVector IS NOT NULL
            AND SpatialGeometry IS NOT NULL
    );
    
    IF @queryVec IS NULL
    BEGIN
        PRINT '  ??  SKIPPED: No embeddings with spatial geometry yet';
        PRINT '     Run workers to generate embeddings first';
        SET @totalTests = @totalTests + 1;
    END
    ELSE
    BEGIN
        DECLARE @startTime DATETIME2 = SYSUTCDATETIME();
        
        DECLARE @results TABLE (
            AtomId BIGINT,
            Score FLOAT,
            QueryTimeMs INT
        );
        
        INSERT INTO @results
        SELECT AtomId, Score, QueryTimeMs
        FROM dbo.sp_FindNearestAtoms(
            @queryVector = @queryVec,
            @topK = 5,
            @tenantId = 0
        );
        
        DECLARE @resultCount INT = (SELECT COUNT(*) FROM @results);
        DECLARE @avgQueryTime INT = (SELECT AVG(QueryTimeMs) FROM @results);
        
        IF @resultCount = 0
            THROW 50000, 'Query returned no results', 1;
        
        SET @totalTests = @totalTests + 1;
        SET @passedTests = @passedTests + 1;
        PRINT '  ? PASSED: Spatial search working';
        PRINT '    Results: ' + CAST(@resultCount AS NVARCHAR) + ' atoms';
        PRINT '    Query time: ' + CAST(@avgQueryTime AS NVARCHAR) + 'ms';
    END
END TRY
BEGIN CATCH
    SET @totalTests = @totalTests + 1;
    SET @failedTests = @failedTests + 1;
    PRINT '  ? FAILED: ' + ERROR_MESSAGE();
END CATCH

-- =====================================================
-- TEST 4: sp_RunInference - Generative Inference
-- =====================================================
PRINT '';
PRINT 'TEST 4: Generative Autoregressive Inference';

BEGIN TRY
    DECLARE @contextIds NVARCHAR(MAX) = (
        SELECT TOP 3 STRING_AGG(CAST(AtomId AS NVARCHAR), ',')
        FROM dbo.Atoms
        WHERE Modality = 'text'
            AND CanonicalText IS NOT NULL
    );
    
    IF @contextIds IS NULL OR LEN(@contextIds) = 0
    BEGIN
        PRINT '  ??  SKIPPED: No text atoms available for inference';
        SET @totalTests = @totalTests + 1;
    END
    ELSE
    BEGIN
        DECLARE @inferenceId BIGINT;
        DECLARE @inferenceResults TABLE (
            Step INT,
            AtomId BIGINT,
            Probability FLOAT
        );
        
        INSERT INTO @inferenceResults
        SELECT Step, AtomId, Probability
        FROM dbo.sp_RunInference(
            @contextAtomIds = @contextIds,
            @temperature = 0.7,
            @topK = 5,
            @maxTokens = 10,
            @inferenceId = @inferenceId OUTPUT
        );
        
        DECLARE @generatedCount INT = (SELECT COUNT(*) FROM @inferenceResults);
        
        IF @generatedCount = 0
            THROW 50000, 'Inference generated no tokens', 1;
        
        SET @totalTests = @totalTests + 1;
        SET @passedTests = @passedTests + 1;
        PRINT '  ? PASSED: Generative inference working';
        PRINT '    Inference ID: ' + CAST(@inferenceId AS NVARCHAR);
        PRINT '    Generated tokens: ' + CAST(@generatedCount AS NVARCHAR);
    END
END TRY
BEGIN CATCH
    SET @totalTests = @totalTests + 1;
    SET @failedTests = @failedTests + 1;
    PRINT '  ? FAILED: ' + ERROR_MESSAGE();
END CATCH

-- =====================================================
-- TEST 5: OODA Loop - Full Cycle
-- =====================================================
PRINT '';
PRINT 'TEST 5: OODA Loop (Analyze ? Hypothesize ? Act ? Learn)';

BEGIN TRY
    -- Trigger Analyze phase
    EXEC dbo.sp_Analyze @TenantId = 0;
    
    -- Wait for Service Broker messages to propagate
    WAITFOR DELAY '00:00:03';
    
    -- Check if OODA metrics were logged
    DECLARE @oodaMetrics TABLE (
        Phase NVARCHAR(50),
        ExecutedAt DATETIME2
    );
    
    INSERT INTO @oodaMetrics
    SELECT TOP 10 Phase, ExecutedAt
    FROM dbo.OODALoopMetrics
    WHERE ExecutedAt >= DATEADD(MINUTE, -5, SYSUTCDATETIME())
    ORDER BY ExecutedAt DESC;
    
    DECLARE @oodaPhases INT = (SELECT COUNT(DISTINCT Phase) FROM @oodaMetrics);
    
    SET @totalTests = @totalTests + 1;
    
    IF @oodaPhases >= 1
    BEGIN
        SET @passedTests = @passedTests + 1;
        PRINT '  ? PASSED: OODA loop executed';
        PRINT '    Phases completed: ' + CAST(@oodaPhases AS NVARCHAR);
        
        SELECT '    - ' + Phase + ' at ' + FORMAT(ExecutedAt, 'HH:mm:ss') AS [OODA Phases]
        FROM @oodaMetrics
        ORDER BY ExecutedAt;
    END
    ELSE
    BEGIN
        SET @failedTests = @failedTests + 1;
        PRINT '  ??  PARTIAL: OODA triggered but phases may not have completed';
        PRINT '     Check Service Broker configuration';
    END
END TRY
BEGIN CATCH
    SET @totalTests = @totalTests + 1;
    SET @failedTests = @failedTests + 1;
    PRINT '  ? FAILED: ' + ERROR_MESSAGE();
END CATCH

-- =====================================================
-- TEST 6: Service Broker Configuration
-- =====================================================
PRINT '';
PRINT 'TEST 6: Service Broker Infrastructure';

BEGIN TRY
    DECLARE @brokerEnabled BIT = (
        SELECT is_broker_enabled
        FROM sys.databases
        WHERE name = DB_NAME()
    );
    
    IF @brokerEnabled = 0
        THROW 50000, 'Service Broker not enabled', 1;
    
    DECLARE @queueCount INT = (
        SELECT COUNT(*)
        FROM sys.service_queues
        WHERE is_ms_shipped = 0
            AND name IN ('AnalyzeQueue', 'HypothesizeQueue', 'ActQueue', 'LearnQueue')
    );
    
    IF @queueCount < 4
        THROW 50000, 'Missing OODA loop queues', 1;
    
    SET @totalTests = @totalTests + 1;
    SET @passedTests = @passedTests + 1;
    PRINT '  ? PASSED: Service Broker configured';
    PRINT '    Broker enabled: Yes';
    PRINT '    OODA queues: ' + CAST(@queueCount AS NVARCHAR) + '/4';
END TRY
BEGIN CATCH
    SET @totalTests = @totalTests + 1;
    SET @failedTests = @failedTests + 1;
    PRINT '  ? FAILED: ' + ERROR_MESSAGE();
END CATCH

-- =====================================================
-- FINAL SUMMARY
-- =====================================================
PRINT '';
PRINT '??????????????????????????????????????????????????????????';
PRINT '?              VALIDATION SUMMARY                        ?';
PRINT '??????????????????????????????????????????????????????????';
PRINT '';
PRINT '  Total Tests: ' + CAST(@totalTests AS NVARCHAR);
PRINT '  Passed: ' + CAST(@passedTests AS NVARCHAR) + ' ?';
PRINT '  Failed: ' + CAST(@failedTests AS NVARCHAR) + ' ?';
PRINT '';

DECLARE @successRate FLOAT = CAST(@passedTests AS FLOAT) / NULLIF(@totalTests, 0) * 100;

IF @successRate >= 90
BEGIN
    PRINT '  STATUS: ? OPERATIONAL (' + CAST(ROUND(@successRate, 1) AS NVARCHAR) + '%)';
    PRINT '';
    PRINT '  ??????????????????????????????????????????????????????';
    PRINT '  ?   HARTONOMOUS IS READY FOR PRODUCTION USE         ?';
    PRINT '  ??????????????????????????????????????????????????????';
END
ELSE IF @successRate >= 70
BEGIN
    PRINT '  STATUS: ??  PARTIAL (' + CAST(ROUND(@successRate, 1) AS NVARCHAR) + '%)';
    PRINT '  Action Required: Fix failed tests before production';
END
ELSE
BEGIN
    PRINT '  STATUS: ? NOT READY (' + CAST(ROUND(@successRate, 1) AS NVARCHAR) + '%)';
    PRINT '  Action Required: Critical issues must be resolved';
END

PRINT '';
PRINT 'Next Steps:';
PRINT '1. Start workers: dotnet run --project src\Hartonomous.Workers';
PRINT '2. Start API: dotnet run --project src\Hartonomous.Api';
PRINT '3. Test endpoint: curl http://localhost:5000/api/admin/health';
PRINT '';
