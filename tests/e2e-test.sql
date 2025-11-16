-- Week 2 Day 8-9: End-to-End Test
-- Tests the complete O(log N) + O(K) spatial query pipeline

SET NOCOUNT ON;

PRINT '';
PRINT 'Running end-to-end spatial query test...';
PRINT '';

-- Test 1: Verify sample data exists
PRINT 'Test 1: Verifying sample data exists...';
DECLARE @sampleCount INT = (
    SELECT COUNT(*) 
    FROM dbo.Atoms a
    INNER JOIN dbo.AtomEmbeddings ae ON a.AtomId = ae.AtomId
    WHERE a.ContentHash IN (
        HASHBYTES('SHA2_256', 'test1'),
        HASHBYTES('SHA2_256', 'test2'),
        HASHBYTES('SHA2_256', 'test3')
    )
);

IF @sampleCount < 3
BEGIN
    PRINT '  FAILED: Sample data not found. Run seed-sample-data.sql first.';
    THROW 50000, 'Sample data missing', 1;
END

PRINT '  PASSED: Found ' + CAST(@sampleCount AS VARCHAR(10)) + ' sample atoms';
PRINT '';

-- Test 2: Test spatial projection
PRINT 'Test 2: Testing spatial projection...';
DECLARE @testGeometry GEOMETRY = (
    SELECT TOP 1 SpatialGeometry
    FROM dbo.AtomEmbeddings
    WHERE SpatialGeometry IS NOT NULL
);

IF @testGeometry IS NULL
BEGIN
    PRINT '  FAILED: No spatial geometry found';
    THROW 50000, 'Spatial projection failed', 1;
END

PRINT '  PASSED: Spatial geometry exists (X=' + 
    CAST(@testGeometry.STX AS VARCHAR(20)) + ', Y=' + 
    CAST(@testGeometry.STY AS VARCHAR(20)) + ', Z=' + 
    CAST(@testGeometry.STZ AS VARCHAR(20)) + ')';
PRINT '';

-- Test 3: Execute spatial next token query (O(log N) + O(K) pattern)
PRINT 'Test 3: Testing sp_SpatialNextToken (O(log N) + O(K))...';

BEGIN TRY
    DECLARE @atom1 BIGINT = (SELECT TOP 1 AtomId FROM dbo.Atoms WHERE CONVERT(VARCHAR(MAX), AtomicValue) = 'The quick brown fox');
    DECLARE @atom2 BIGINT = (SELECT TOP 1 AtomId FROM dbo.Atoms WHERE CONVERT(VARCHAR(MAX), AtomicValue) = 'jumps over the lazy dog');
    
    EXEC sp_SpatialNextToken
        @context_atom_ids = @atom1,
        @temperature = 1.0,
        @top_k = 3;
    
    PRINT '  PASSED: Spatial query executed successfully';
END TRY
BEGIN CATCH
    PRINT '  FAILED: ' + ERROR_MESSAGE();
    THROW;
END CATCH

PRINT '';
PRINT 'All end-to-end tests passed';
PRINT '';
