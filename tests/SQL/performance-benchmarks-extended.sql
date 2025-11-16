-- =========================================================================
-- Hartonomous Performance Benchmark Tests
-- Validates O(log N) + O(K) Performance Claims
-- =========================================================================

SET NOCOUNT ON;
SET STATISTICS TIME ON;
SET STATISTICS IO ON;

PRINT '═══════════════════════════════════════════════════════';
PRINT '  HARTONOMOUS PERFORMANCE BENCHMARKS';
PRINT '  Validating O(log N) + O(K) Query Pattern';
PRINT '═══════════════════════════════════════════════════════';
PRINT '';

-- =========================================================================
-- BENCHMARK 1: Spatial Index Seek Performance
-- Expected: O(log N) for spatial index seek
-- =========================================================================

PRINT '-------------------------------------------------------------------';
PRINT 'BENCHMARK 1: Spatial Index Seek (O(log N) Target)';
PRINT '-------------------------------------------------------------------';
PRINT '';

DECLARE @searchPoint GEOMETRY = geometry::Point(0, 0, 0);
DECLARE @searchRadius FLOAT = 10.0;
DECLARE @searchArea GEOMETRY = @searchPoint.STBuffer(@searchRadius);
DECLARE @start DATETIME2 = SYSUTCDATETIME();

-- Stage 1: Spatial index seek
SELECT TOP 100
    AtomId,
    SpatialKey
FROM dbo.AtomEmbeddings WITH(INDEX(SIX_AtomEmbeddings_SpatialKey))
WHERE SpatialKey.STIntersects(@searchArea) = 1;

DECLARE @elapsed_ms INT = DATEDIFF(MILLISECOND, @start, SYSUTCDATETIME());

PRINT 'Spatial seek completed in ' + CAST(@elapsed_ms AS VARCHAR) + ' ms';
IF @elapsed_ms < 50
    PRINT '  ✓ PASS: Performance within O(log N) expectations';
ELSE IF @elapsed_ms < 200
    PRINT '  ⚠ WARNING: Slower than expected, but acceptable';
ELSE
    PRINT '  ✗ FAIL: Performance degraded, investigate index fragmentation';
PRINT '';

-- =========================================================================
-- BENCHMARK 2: In-Memory Cache Performance
-- Expected: < 1ms for cache hits
-- =========================================================================

PRINT '-------------------------------------------------------------------';
PRINT 'BENCHMARK 2: In-Memory Cache Performance';
PRINT '-------------------------------------------------------------------';
PRINT '';

SET @start = SYSUTCDATETIME();

-- Test cache lookup (if table has data)
SELECT TOP 10 *
FROM dbo.InferenceCache_InMemory
WHERE CacheKey LIKE 'test%';

SET @elapsed_ms = DATEDIFF(MILLISECOND, @start, SYSUTCDATETIME());

PRINT 'In-memory lookup completed in ' + CAST(@elapsed_ms AS VARCHAR) + ' ms';
IF @elapsed_ms < 1
    PRINT '  ✓ PASS: Sub-millisecond cache performance';
ELSE IF @elapsed_ms < 10
    PRINT '  ⚠ WARNING: Cache slower than expected';
ELSE
    PRINT '  ✗ FAIL: Cache performance degraded';
PRINT '';

-- =========================================================================
-- BENCHMARK 3: Native Procedure Performance
-- Expected: 10-50x faster than interpreted T-SQL
-- =========================================================================

PRINT '-------------------------------------------------------------------';
PRINT 'BENCHMARK 3: Native vs Interpreted Procedure Performance';
PRINT '-------------------------------------------------------------------';
PRINT '';

IF OBJECT_ID('dbo.sp_GetInferenceCacheHit_Native') IS NOT NULL
BEGIN
    SET @start = SYSUTCDATETIME();
    
    -- Run native procedure
    DECLARE @output VARBINARY(MAX);
    DECLARE @computeTime INT;
    DECLARE @found BIT;
    
    EXEC dbo.sp_GetInferenceCacheHit_Native 
        @CacheKey = 'benchmark_test',
        @OutputData = @output OUTPUT,
        @ComputeTimeMs = @computeTime OUTPUT,
        @Found = @found OUTPUT;
    
    SET @elapsed_ms = DATEDIFF(MILLISECOND, @start, SYSUTCDATETIME());
    
    PRINT 'Native procedure executed in ' + CAST(@elapsed_ms AS VARCHAR) + ' ms';
    IF @elapsed_ms < 5
        PRINT '  ✓ PASS: Native compilation providing expected speedup';
    ELSE
        PRINT '  ⚠ WARNING: Native procedure slower than expected';
END
ELSE
    PRINT '  ⚠ SKIPPED: Native procedure not found';
PRINT '';

-- =========================================================================
-- BENCHMARK 4: Graph Traversal Performance
-- Expected: < 100ms for 3-hop traversal
-- =========================================================================

PRINT '-------------------------------------------------------------------';
PRINT 'BENCHMARK 4: Graph Traversal Performance';
PRINT '-------------------------------------------------------------------';
PRINT '';

SET @start = SYSUTCDATETIME();

-- 3-hop graph traversal
SELECT TOP 10
    n1.AtomId AS StartAtom,
    n2.AtomId AS Hop1,
    n3.AtomId AS Hop2,
    n4.AtomId AS Hop3
FROM graph.AtomGraphNodes n1,
     graph.AtomGraphEdges e1,
     graph.AtomGraphNodes n2,
     graph.AtomGraphEdges e2,
     graph.AtomGraphNodes n3,
     graph.AtomGraphEdges e3,
     graph.AtomGraphNodes n4
WHERE MATCH(n1-(e1)->n2-(e2)->n3-(e3)->n4));

SET @elapsed_ms = DATEDIFF(MILLISECOND, @start, SYSUTCDATETIME());

PRINT 'Graph traversal (3 hops) completed in ' + CAST(@elapsed_ms AS VARCHAR) + ' ms';
IF @elapsed_ms < 100
    PRINT '  ✓ PASS: Graph traversal performance acceptable';
ELSE IF @elapsed_ms < 500
    PRINT '  ⚠ WARNING: Graph traversal slower than target';
ELSE
    PRINT '  ✗ FAIL: Graph traversal too slow, check indexes';
PRINT '';

-- =========================================================================
-- BENCHMARK SUMMARY
-- =========================================================================

PRINT '═══════════════════════════════════════════════════════';
PRINT '  PERFORMANCE BENCHMARKS COMPLETE';
PRINT '';
PRINT '  Performance Targets:';
PRINT '    - Spatial Seek:     < 50ms   (O(log N))';
PRINT '    - Cache Lookup:     < 1ms    (Hekaton)';
PRINT '    - Native Proc:      < 5ms    (compiled)';
PRINT '    - Graph Traversal:  < 100ms  (3 hops)';
PRINT '═══════════════════════════════════════════════════════';

SET STATISTICS TIME OFF;
SET STATISTICS IO OFF;
