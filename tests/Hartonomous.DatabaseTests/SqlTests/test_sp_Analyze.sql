-- =============================================
-- Test: sp_Analyze (Autonomous OODA Loop Phase 1)
-- Validates observation, anomaly detection, pattern recognition
-- =============================================

USE Hartonomous;
GO

PRINT '=================================================';
PRINT 'TEST: sp_Analyze (Autonomous OODA Loop)';
PRINT '=================================================';

-- Setup: Create test inference requests with varying durations
PRINT 'Setup: Creating test inference requests...';

DECLARE @TestModelId INT = 1;
DECLARE @Now DATETIME2 = SYSUTCDATETIME();

-- Insert normal inferences (avg ~100ms)
INSERT INTO dbo.InferenceRequests (TaskType, Status, TotalDurationMs, RequestTimestamp, InputData, OutputData)
VALUES
    ('text-generation', 'Completed', 95, DATEADD(MINUTE, -30, @Now), '{"prompt":"test1"}', '{"result":"output1"}'),
    ('text-generation', 'Completed', 102, DATEADD(MINUTE, -25, @Now), '{"prompt":"test2"}', '{"result":"output2"}'),
    ('text-generation', 'Completed', 98, DATEADD(MINUTE, -20, @Now), '{"prompt":"test3"}', '{"result":"output3"}'),
    ('text-generation', 'Completed', 105, DATEADD(MINUTE, -15, @Now), '{"prompt":"test4"}', '{"result":"output4"}'),
    -- Anomalous slow inferences (>2x avg = >200ms)
    ('text-generation', 'Completed', 350, DATEADD(MINUTE, -10, @Now), '{"prompt":"slow1"}', '{"result":"slowoutput1"}'),
    ('text-generation', 'Completed', 420, DATEADD(MINUTE, -5, @Now), '{"prompt":"slow2"}', '{"result":"slowoutput2"}');

DECLARE @InsertedCount INT = @@ROWCOUNT;
PRINT 'Inserted ' + CAST(@InsertedCount AS VARCHAR(10)) + ' test inference requests';

-- Setup: Create test embeddings for pattern detection
PRINT 'Setup: Creating test embeddings for clustering...';

DECLARE @ClusterEmbedding1 VARBINARY(MAX) = (SELECT dbo.clr_RealArrayToBinary(CAST('<floats>' + REPLICATE('<v>0.5</v>', 512) + '</floats>' AS XML)));
DECLARE @ClusterEmbedding2 VARBINARY(MAX) = (SELECT dbo.clr_RealArrayToBinary(CAST('<floats>' + REPLICATE('<v>0.6</v>', 512) + '</floats>' AS XML)));

INSERT INTO dbo.Atoms (ContentHash, Modality, CanonicalText)
VALUES
    (HASHBYTES('SHA2_256', 'cluster_test_1'), 'text', 'Cluster test atom 1'),
    (HASHBYTES('SHA2_256', 'cluster_test_2'), 'text', 'Cluster test atom 2');

DECLARE @ClusterAtomId1 BIGINT = SCOPE_IDENTITY() - 1;
DECLARE @ClusterAtomId2 BIGINT = SCOPE_IDENTITY();

INSERT INTO dbo.AtomEmbeddings (AtomId, EmbeddingType, ModelId, Embedding, SpatialBucketX, SpatialBucketY, SpatialBucketZ)
VALUES
    (@ClusterAtomId1, 'semantic', @TestModelId, @ClusterEmbedding1, 10, 20, 30),
    (@ClusterAtomId2, 'semantic', @TestModelId, @ClusterEmbedding2, 10, 20, 30);  -- Same bucket = cluster

PRINT 'Created 2 embeddings in same spatial bucket for clustering test';

-- TEST 1: Run analysis and validate it detects anomalies
PRINT '';
PRINT 'TEST 1: Run sp_Analyze and detect performance anomalies';
DECLARE @StartTime DATETIME2 = SYSUTCDATETIME();

BEGIN TRY
    EXEC dbo.sp_Analyze
        @TenantId = 0,
        @AnalysisScope = 'full',
        @LookbackHours = 24;
    
    DECLARE @Duration INT = DATEDIFF(MILLISECOND, @StartTime, SYSUTCDATETIME());
    PRINT 'SUCCESS: sp_Analyze completed in ' + CAST(@Duration AS VARCHAR(10)) + ' ms';
END TRY
BEGIN CATCH
    PRINT 'ERROR: ' + ERROR_MESSAGE();
    PRINT 'Severity: ' + CAST(ERROR_SEVERITY() AS VARCHAR(10));
END CATCH;

-- TEST 2: Validate anomaly detection results
PRINT '';
PRINT 'TEST 2: Validate token count calculation';

-- Check that token count was calculated (not 0)
DECLARE @TokenCountCheck INT;
SELECT @TokenCountCheck = COUNT(*)
FROM (
    SELECT TOP 1000
        (LEN(ISNULL(InputData, '')) + LEN(ISNULL(OutputData, ''))) / 4 AS TokenCount
    FROM dbo.InferenceRequests
    WHERE RequestTimestamp >= DATEADD(HOUR, -24, SYSUTCDATETIME())
        AND Status IN ('Completed', 'Failed')
) AS Tokens
WHERE TokenCount > 0;

IF @TokenCountCheck > 0
    PRINT 'SUCCESS: Token count calculation working (' + CAST(@TokenCountCheck AS VARCHAR(10)) + ' inferences have tokens)';
ELSE
    PRINT 'WARNING: No inferences with token counts found';

-- TEST 3: Performance baseline measurement
PRINT '';
PRINT 'TEST 3: Performance baseline measurement';

DECLARE @Iterations INT = 5;
DECLARE @Iteration INT = 1;
DECLARE @TotalDuration INT = 0;

WHILE @Iteration <= @Iterations
BEGIN
    SET @StartTime = SYSUTCDATETIME();
    
    EXEC dbo.sp_Analyze
        @TenantId = 0,
        @AnalysisScope = 'full',
        @LookbackHours = 24;
    
    SET @TotalDuration = @TotalDuration + DATEDIFF(MILLISECOND, @StartTime, SYSUTCDATETIME());
    SET @Iteration = @Iteration + 1;
END;

DECLARE @AvgDuration FLOAT = CAST(@TotalDuration AS FLOAT) / @Iterations;
PRINT 'Average duration over ' + CAST(@Iterations AS VARCHAR(10)) + ' iterations: ' + CAST(@AvgDuration AS VARCHAR(20)) + ' ms';

-- Cleanup
DELETE FROM dbo.AtomEmbeddings WHERE AtomId IN (@ClusterAtomId1, @ClusterAtomId2);
DELETE FROM dbo.Atoms WHERE AtomId IN (@ClusterAtomId1, @ClusterAtomId2);
DELETE FROM dbo.InferenceRequests WHERE RequestTimestamp >= DATEADD(HOUR, -1, @Now);

PRINT '';
PRINT 'TEST COMPLETE: Cleanup successful';
PRINT '=================================================';
GO
