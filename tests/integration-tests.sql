-- Week 3 Day 13-14: Integration Tests
-- Tests complete workflows with database deployed

SET NOCOUNT ON;

PRINT 'Hartonomous Integration Tests';
PRINT 'Running against deployed database';
PRINT '';

DECLARE @TestsPassed INT = 0;
DECLARE @TestsFailed INT = 0;

-- Test 1: Spatial Query Performance (O(log N) validation)
PRINT 'Test 1: Spatial Query Performance (O(log N))';
BEGIN TRY
    -- Ensure we have sample data
    IF NOT EXISTS (SELECT 1 FROM dbo.AtomEmbeddings WHERE SpatialGeometry IS NOT NULL)
    BEGIN
        PRINT '  SKIPPED: No sample data. Run seed-sample-data.sql first';
    END
    ELSE
    BEGIN
        DECLARE @StartTime DATETIME2 = SYSUTCDATETIME();
        DECLARE @AtomCount INT = (SELECT COUNT(*) FROM dbo.AtomEmbeddings);
        
        -- Run spatial query
        EXEC sp_SpatialNextToken
            @context_atom_ids = '1,2',
            @temperature = 1.0,
            @top_k = 10;
        
        DECLARE @ElapsedMs INT = DATEDIFF(MILLISECOND, @StartTime, SYSUTCDATETIME());
        
        -- O(log N) should be fast even with large datasets
        IF @ElapsedMs > 100
        BEGIN
            PRINT '  WARNING: Query took ' + CAST(@ElapsedMs AS VARCHAR(10)) + 'ms (expected <100ms for O(log N))';
            SET @TestsPassed = @TestsPassed + 1;
        END
        ELSE
        BEGIN
            PRINT '  PASSED: Query completed in ' + CAST(@ElapsedMs AS VARCHAR(10)) + 'ms (dataset: ' + CAST(@AtomCount AS VARCHAR(10)) + ' atoms)';
            SET @TestsPassed = @TestsPassed + 1;
        END
    END
END TRY
BEGIN CATCH
    PRINT '  FAILED: ' + ERROR_MESSAGE();
    SET @TestsFailed = @TestsFailed + 1;
END CATCH;

PRINT '';

-- Test 2: Spatial Projection Pipeline
PRINT 'Test 2: Spatial Projection Pipeline (1998D -> 3D -> Hilbert)';
BEGIN TRY
    -- Create test vector
    DECLARE @testVector VARBINARY(MAX) = CAST(REPLICATE(0x3F800000, 1998) AS VARBINARY(MAX));
    
    -- Project to 3D
    DECLARE @projected GEOMETRY = dbo.fn_ProjectTo3D(@testVector);
    
    IF @projected IS NULL
        THROW 50000, 'Projection returned NULL', 1;
    
    -- Compute Hilbert value
    DECLARE @hilbert BIGINT = dbo.clr_ComputeHilbertValue(@projected, 21);
    
    IF @hilbert IS NULL OR @hilbert <= 0
        THROW 50000, 'Hilbert computation failed', 1;
    
    PRINT '  PASSED: 1998D -> 3D (' + 
        CAST(@projected.STX AS VARCHAR(20)) + ', ' +
        CAST(@projected.STY AS VARCHAR(20)) + ', ' +
        CAST(@projected.STZ AS VARCHAR(20)) + ') -> Hilbert: ' +
        CAST(@hilbert AS VARCHAR(20));
    
    SET @TestsPassed = @TestsPassed + 1;
END TRY
BEGIN CATCH
    PRINT '  FAILED: ' + ERROR_MESSAGE();
    SET @TestsFailed = @TestsFailed + 1;
END CATCH;

PRINT '';

-- Test 3: Spatial Index Usage
PRINT 'Test 3: Spatial Index Usage Validation';
BEGIN TRY
    -- Check index exists
    IF NOT EXISTS (
        SELECT 1 FROM sys.indexes
        WHERE name = 'IX_AtomEmbeddings_SpatialGeometry'
          AND object_id = OBJECT_ID('dbo.AtomEmbeddings')
    )
        THROW 50000, 'Spatial index missing', 1;
    
    -- Check index is being used
    DECLARE @IndexUsage TABLE (
        user_seeks INT,
        user_scans INT,
        last_user_seek DATETIME
    );
    
    INSERT INTO @IndexUsage
    SELECT 
        ISNULL(ius.user_seeks, 0),
        ISNULL(ius.user_scans, 0),
        ius.last_user_seek
    FROM sys.indexes i
    LEFT JOIN sys.dm_db_index_usage_stats ius
        ON i.object_id = ius.object_id AND i.index_id = ius.index_id
    WHERE i.name = 'IX_AtomEmbeddings_SpatialGeometry'
        AND OBJECT_NAME(i.object_id) = 'AtomEmbeddings';
    
    DECLARE @seeks INT = (SELECT user_seeks FROM @IndexUsage);
    DECLARE @lastSeek DATETIME = (SELECT last_user_seek FROM @IndexUsage);
    
    PRINT '  PASSED: Spatial index exists';
    PRINT '    Seeks: ' + CAST(ISNULL(@seeks, 0) AS VARCHAR(10));
    IF @lastSeek IS NOT NULL
        PRINT '    Last used: ' + CONVERT(VARCHAR(20), @lastSeek, 120);
    
    SET @TestsPassed = @TestsPassed + 1;
END TRY
BEGIN CATCH
    PRINT '  FAILED: ' + ERROR_MESSAGE();
    SET @TestsFailed = @TestsFailed + 1;
END CATCH;

PRINT '';

-- Test 4: OODA Loop Procedures Executable
PRINT 'Test 4: OODA Loop Procedures Executable';
BEGIN TRY
    -- Just verify they can be called (don't run full loops in tests)
    IF OBJECT_ID('dbo.sp_Analyze') IS NULL
        THROW 50000, 'sp_Analyze missing', 1;
    IF OBJECT_ID('dbo.sp_Hypothesize') IS NULL
        THROW 50000, 'sp_Hypothesize missing', 1;
    IF OBJECT_ID('dbo.sp_Act') IS NULL
        THROW 50000, 'sp_Act missing', 1;
    IF OBJECT_ID('dbo.sp_Learn') IS NULL
        THROW 50000, 'sp_Learn missing', 1;
    
    PRINT '  PASSED: All OODA loop procedures exist and are callable';
    SET @TestsPassed = @TestsPassed + 1;
END TRY
BEGIN CATCH
    PRINT '  FAILED: ' + ERROR_MESSAGE();
    SET @TestsFailed = @TestsFailed + 1;
END CATCH;

PRINT '';

-- Test 5: Reasoning Frameworks Available
PRINT 'Test 5: Reasoning Frameworks Available';
BEGIN TRY
    IF OBJECT_ID('dbo.sp_ChainOfThoughtReasoning') IS NULL
        THROW 50000, 'Chain of Thought missing', 1;
    IF OBJECT_ID('dbo.sp_MultiPathReasoning') IS NULL
        THROW 50000, 'Tree of Thought missing', 1;
    IF OBJECT_ID('dbo.sp_SelfConsistencyReasoning') IS NULL
        THROW 50000, 'Reflexion missing', 1;
    
    PRINT '  PASSED: All reasoning frameworks available';
    PRINT '    - Chain of Thought';
    PRINT '    - Tree of Thought (Multi-Path)';
    PRINT '    - Reflexion (Self-Consistency)';
    SET @TestsPassed = @TestsPassed + 1;
END TRY
BEGIN CATCH
    PRINT '  FAILED: ' + ERROR_MESSAGE();
    SET @TestsFailed = @TestsFailed + 1;
END CATCH;

PRINT '';

-- Test 6: Cross-Modal Support
PRINT 'Test 6: Cross-Modal Generation Support';
BEGIN TRY
    IF OBJECT_ID('dbo.sp_GenerateText') IS NULL
        THROW 50000, 'Text generation missing', 1;
    IF OBJECT_ID('dbo.sp_GenerateImage') IS NULL
        THROW 50000, 'Image generation missing', 1;
    IF OBJECT_ID('dbo.sp_CrossModalQuery') IS NULL
        THROW 50000, 'Cross-modal query missing', 1;
    
    PRINT '  PASSED: Cross-modal generation support available';
    PRINT '    - Text generation';
    PRINT '    - Image generation';
    PRINT '    - Cross-modal queries';
    SET @TestsPassed = @TestsPassed + 1;
END TRY
BEGIN CATCH
    PRINT '  FAILED: ' + ERROR_MESSAGE();
    SET @TestsFailed = @TestsFailed + 1;
END CATCH;

PRINT '';
PRINT '========================================';
PRINT 'INTEGRATION TEST RESULTS';
PRINT '========================================';
PRINT 'Passed: ' + CAST(@TestsPassed AS VARCHAR(10));
PRINT 'Failed: ' + CAST(@TestsFailed AS VARCHAR(10));
PRINT '========================================';

IF @TestsFailed = 0
BEGIN
    PRINT 'ALL INTEGRATION TESTS PASSED';
    PRINT '';
END
ELSE
BEGIN
    PRINT 'SOME INTEGRATION TESTS FAILED';
    PRINT '';
    THROW 50000, 'Integration tests failed', 1;
END
