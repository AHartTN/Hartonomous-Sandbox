-- Week 3 Day 15: Performance Benchmarks
-- Validates O(log N) + O(K) complexity claims

SET NOCOUNT ON;
SET STATISTICS IO OFF;
SET STATISTICS TIME OFF;

PRINT 'Hartonomous Performance Benchmarks';
PRINT 'Testing O(log N) + O(K) query performance';
PRINT '';

-- Benchmark configuration
DECLARE @TopK INT = 10;
DECLARE @Iterations INT = 10;

-- Results table
DECLARE @BenchmarkResults TABLE (
    DatasetSize INT,
    Iteration INT,
    QueryTimeMs INT
);

-- Get current atom count
DECLARE @CurrentAtomCount INT = (
    SELECT COUNT(*) 
    FROM dbo.AtomEmbeddings 
    WHERE SpatialGeometry IS NOT NULL
);

PRINT 'Current dataset size: ' + CAST(@CurrentAtomCount AS VARCHAR(10)) + ' atoms';
PRINT 'Running ' + CAST(@Iterations AS VARCHAR(10)) + ' iterations per test...';
PRINT '';

-- Benchmark: Spatial Query Performance
PRINT 'Benchmark 1: Spatial Next Token Query (sp_SpatialNextToken)';
PRINT '-----------------------------------------------------------';

DECLARE @i INT = 1;
WHILE @i <= @Iterations
BEGIN
    DECLARE @StartTime DATETIME2 = SYSUTCDATETIME();
    
    -- Run spatial query
    EXEC sp_SpatialNextToken
        @context_atom_ids = '1,2',
        @temperature = 1.0,
        @top_k = @TopK;
    
    DECLARE @ElapsedMs INT = DATEDIFF(MILLISECOND, @StartTime, SYSUTCDATETIME());
    
    INSERT INTO @BenchmarkResults (DatasetSize, Iteration, QueryTimeMs)
    VALUES (@CurrentAtomCount, @i, @ElapsedMs);
    
    SET @i = @i + 1;
END

-- Calculate statistics
DECLARE @AvgMs FLOAT = (SELECT AVG(CAST(QueryTimeMs AS FLOAT)) FROM @BenchmarkResults);
DECLARE @MinMs INT = (SELECT MIN(QueryTimeMs) FROM @BenchmarkResults);
DECLARE @MaxMs INT = (SELECT MAX(QueryTimeMs) FROM @BenchmarkResults);
DECLARE @P95Ms INT = (
    SELECT TOP 1 QueryTimeMs 
    FROM @BenchmarkResults 
    ORDER BY QueryTimeMs DESC 
    OFFSET (@Iterations * 5 / 100) ROWS
);

PRINT 'Results for ' + CAST(@CurrentAtomCount AS VARCHAR(10)) + ' atoms:';
PRINT '  Average: ' + CAST(CAST(@AvgMs AS DECIMAL(10,2)) AS VARCHAR(20)) + ' ms';
PRINT '  Min: ' + CAST(@MinMs AS VARCHAR(10)) + ' ms';
PRINT '  Max: ' + CAST(@MaxMs AS VARCHAR(10)) + ' ms';
PRINT '  P95: ' + CAST(@P95Ms AS VARCHAR(10)) + ' ms';
PRINT '';

-- Benchmark 2: Spatial Projection
PRINT 'Benchmark 2: Spatial Projection (1998D -> 3D)';
PRINT '----------------------------------------------';

DELETE FROM @BenchmarkResults;

DECLARE @testVector VARBINARY(MAX) = CAST(REPLICATE(0x3F800000, 1998) AS VARBINARY(MAX));

SET @i = 1;
WHILE @i <= @Iterations
BEGIN
    SET @StartTime = SYSUTCDATETIME();
    
    DECLARE @projected GEOMETRY = dbo.fn_ProjectTo3D(@testVector);
    
    SET @ElapsedMs = DATEDIFF(MICROSECOND, @StartTime, SYSUTCDATETIME()) / 1000;
    
    INSERT INTO @BenchmarkResults (DatasetSize, Iteration, QueryTimeMs)
    VALUES (1, @i, @ElapsedMs);
    
    SET @i = @i + 1;
END

SET @AvgMs = (SELECT AVG(CAST(QueryTimeMs AS FLOAT)) FROM @BenchmarkResults);
SET @MinMs = (SELECT MIN(QueryTimeMs) FROM @BenchmarkResults);
SET @MaxMs = (SELECT MAX(QueryTimeMs) FROM @BenchmarkResults);

PRINT 'Results:';
PRINT '  Average: ' + CAST(CAST(@AvgMs AS DECIMAL(10,2)) AS VARCHAR(20)) + ' ms';
PRINT '  Min: ' + CAST(@MinMs AS VARCHAR(10)) + ' ms';
PRINT '  Max: ' + CAST(@MaxMs AS VARCHAR(10)) + ' ms';
PRINT '';

-- Benchmark 3: Hilbert Curve Computation
PRINT 'Benchmark 3: Hilbert Curve Computation';
PRINT '---------------------------------------';

DELETE FROM @BenchmarkResults;

DECLARE @testPoint GEOMETRY = geometry::Point(0.5, 0.5, 0.5, 0);

SET @i = 1;
WHILE @i <= @Iterations
BEGIN
    SET @StartTime = SYSUTCDATETIME();
    
    DECLARE @hilbert BIGINT = dbo.clr_ComputeHilbertValue(@testPoint, 21);
    
    SET @ElapsedMs = DATEDIFF(MICROSECOND, @StartTime, SYSUTCDATETIME()) / 1000;
    
    INSERT INTO @BenchmarkResults (DatasetSize, Iteration, QueryTimeMs)
    VALUES (1, @i, @ElapsedMs);
    
    SET @i = @i + 1;
END

SET @AvgMs = (SELECT AVG(CAST(QueryTimeMs AS FLOAT)) FROM @BenchmarkResults);
SET @MinMs = (SELECT MIN(QueryTimeMs) FROM @BenchmarkResults);
SET @MaxMs = (SELECT MAX(QueryTimeMs) FROM @BenchmarkResults);

PRINT 'Results:';
PRINT '  Average: ' + CAST(CAST(@AvgMs AS DECIMAL(10,2)) AS VARCHAR(20)) + ' ms';
PRINT '  Min: ' + CAST(@MinMs AS VARCHAR(10)) + ' ms';
PRINT '  Max: ' + CAST(@MaxMs AS VARCHAR(10)) + ' ms';
PRINT '';

-- Index usage statistics
PRINT 'Spatial Index Usage Statistics';
PRINT '-------------------------------';

SELECT 
    i.name AS IndexName,
    ISNULL(ius.user_seeks, 0) AS UserSeeks,
    ISNULL(ius.user_scans, 0) AS UserScans,
    ISNULL(ius.user_lookups, 0) AS UserLookups,
    ius.last_user_seek AS LastSeek
FROM sys.indexes i
LEFT JOIN sys.dm_db_index_usage_stats ius
    ON i.object_id = ius.object_id AND i.index_id = ius.index_id
WHERE i.type_desc = 'SPATIAL'
    AND OBJECT_NAME(i.object_id) = 'AtomEmbeddings';

PRINT '';
PRINT '===================================';
PRINT 'PERFORMANCE BENCHMARK COMPLETE';
PRINT '===================================';
PRINT '';
PRINT 'Expected O(log N) behavior:';
PRINT '  Query time should scale logarithmically with dataset size';
PRINT '  For reference:';
PRINT '    1K atoms: ~5-10ms expected';
PRINT '    10K atoms: ~10-20ms expected';
PRINT '    100K atoms: ~15-30ms expected';
PRINT '    1M atoms: ~20-40ms expected';
PRINT '';
PRINT 'If query times exceed these ranges, investigate:';
PRINT '  1. Spatial index configuration (BOUNDING_BOX, GRIDS)';
PRINT '  2. Index hints in queries (WITH INDEX(...))';
PRINT '  3. Statistics currency (UPDATE STATISTICS)';
PRINT '  4. Hardware resources (CPU, memory)';
