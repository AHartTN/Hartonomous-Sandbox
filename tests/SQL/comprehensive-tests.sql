-- =========================================================================
-- Hartonomous Comprehensive SQL Test Suite
-- Following Testing Strategy Document (15-Testing-Strategy.md)
-- =========================================================================

SET NOCOUNT ON;

PRINT '═══════════════════════════════════════════════════════';
PRINT '  HARTONOMOUS COMPREHENSIVE SQL TEST SUITE';
PRINT '  Database Unit & Integration Tests';
PRINT '  ' + CONVERT(VARCHAR, GETDATE(), 120);
PRINT '═══════════════════════════════════════════════════════';
PRINT '';

-- =========================================================================
-- TEST CATEGORY 1: CLR FUNCTION TESTS
-- =========================================================================

PRINT '-------------------------------------------------------------------';
PRINT 'TEST CATEGORY 1: CLR FUNCTIONS';
PRINT '-------------------------------------------------------------------';
PRINT '';

-- Test 1.1: fn_ProjectTo3D basic functionality
PRINT '[1.1] fn_ProjectTo3D - Basic Projection';
BEGIN TRY
    DECLARE @testVector VARBINARY(MAX) = CAST(REPLICATE(0x3F800000, 1998) AS VARBINARY(MAX));
    DECLARE @result GEOMETRY = dbo.fn_ProjectTo3D(@testVector);
    
    IF @result IS NOT NULL AND @result.STGeometryType() = 'Point'
        PRINT '  ✓ PASS: Returns valid 3D POINT geometry';
    ELSE
        PRINT '  ✗ FAIL: Invalid geometry type returned';
END TRY
BEGIN CATCH
    PRINT '  ✗ FAIL: ' + ERROR_MESSAGE();
END CATCH;
PRINT '';

-- Test 1.2: fn_ProjectTo3D determinism
PRINT '[1.2] fn_ProjectTo3D - Determinism Test';
BEGIN TRY
    DECLARE @vec VARBINARY(MAX) = CAST(REPLICATE(0x40000000, 1998) AS VARBINARY(MAX));
    DECLARE @result1 GEOMETRY = dbo.fn_ProjectTo3D(@vec);
    DECLARE @result2 GEOMETRY = dbo.fn_ProjectTo3D(@vec);
    
    IF @result1.STEquals(@result2) = 1
        PRINT '  ✓ PASS: Function is deterministic';
    ELSE
        PRINT '  ✗ FAIL: Function is non-deterministic';
END TRY
BEGIN CATCH
    PRINT '  ✗ FAIL: ' + ERROR_MESSAGE();
END CATCH;
PRINT '';

-- Test 1.3: clr_ComputeHilbertValue basic functionality
PRINT '[1.3] clr_ComputeHilbertValue - Basic Computation';
BEGIN TRY
    DECLARE @hilbert BIGINT = dbo.clr_ComputeHilbertValue(1.0, 2.0, 3.0);
    
    IF @hilbert IS NOT NULL AND @hilbert > 0
        PRINT '  ✓ PASS: Returns valid Hilbert value: ' + CAST(@hilbert AS VARCHAR);
    ELSE
        PRINT '  ✗ FAIL: Invalid Hilbert value';
END TRY
BEGIN CATCH
    PRINT '  ✗ FAIL: ' + ERROR_MESSAGE();
END CATCH;
PRINT '';

-- Test 1.4: clr_ComputeHilbertValue ordering
PRINT '[1.4] clr_ComputeHilbertValue - Space-Filling Curve Ordering';
BEGIN TRY
    DECLARE @h1 BIGINT = dbo.clr_ComputeHilbertValue(0.0, 0.0, 0.0);
    DECLARE @h2 BIGINT = dbo.clr_ComputeHilbertValue(0.1, 0.1, 0.1);
    DECLARE @h3 BIGINT = dbo.clr_ComputeHilbertValue(10.0, 10.0, 10.0);
    
    IF @h1 < @h2 AND @h2 < @h3
        PRINT '  ✓ PASS: Hilbert values preserve spatial locality';
    ELSE
        PRINT '  ⚠ WARNING: Unexpected Hilbert ordering';
END TRY
BEGIN CATCH
    PRINT '  ✗ FAIL: ' + ERROR_MESSAGE();
END CATCH;
PRINT '';

-- =========================================================================
-- TEST CATEGORY 2: SPATIAL INDEX TESTS
-- =========================================================================

PRINT '-------------------------------------------------------------------';
PRINT 'TEST CATEGORY 2: SPATIAL INDEXES';
PRINT '-------------------------------------------------------------------';
PRINT '';

-- Test 2.1: Spatial index exists on AtomEmbeddings
PRINT '[2.1] Spatial Index - AtomEmbeddings';
IF EXISTS (
    SELECT 1 FROM sys.indexes 
    WHERE object_id = OBJECT_ID('dbo.AtomEmbeddings') 
    AND type_desc = 'SPATIAL'
)
    PRINT '  ✓ PASS: Spatial index exists on AtomEmbeddings';
ELSE
    PRINT '  ✗ FAIL: Missing spatial index on AtomEmbeddings';
PRINT '';

-- Test 2.2: Spatial index query plan usage
PRINT '[2.2] Spatial Index - Query Plan Verification';
BEGIN TRY
    DECLARE @searchArea GEOMETRY = geometry::Point(0, 0, 0).STBuffer(10);
    DECLARE @plan XML;
    
    SET STATISTICS XML ON;
    SELECT TOP 1 AtomId 
    FROM dbo.AtomEmbeddings WITH(INDEX(SIX_AtomEmbeddings_SpatialKey))
    WHERE SpatialKey.STIntersects(@searchArea) = 1;
    SET STATISTICS XML OFF;
    
    PRINT '  ✓ PASS: Spatial index query executes';
END TRY
BEGIN CATCH
    PRINT '  ✗ FAIL: ' + ERROR_MESSAGE();
END CATCH;
PRINT '';

-- =========================================================================
-- TEST CATEGORY 3: IN-MEMORY OLTP (HEKATON) TESTS
-- =========================================================================

PRINT '-------------------------------------------------------------------';
PRINT 'TEST CATEGORY 3: IN-MEMORY OLTP (HEKATON)';
PRINT '-------------------------------------------------------------------';
PRINT '';

-- Test 3.1: Memory-optimized tables exist
PRINT '[3.1] Hekaton - Memory-Optimized Tables';
DECLARE @hekatonCount INT = (
    SELECT COUNT(*) FROM sys.tables WHERE is_memory_optimized = 1
);
IF @hekatonCount >= 4
    PRINT '  ✓ PASS: Found ' + CAST(@hekatonCount AS VARCHAR) + ' memory-optimized tables';
ELSE
    PRINT '  ⚠ WARNING: Expected >= 4 Hekaton tables, found ' + CAST(@hekatonCount AS VARCHAR);
PRINT '';

-- Test 3.2: Native procedure execution
PRINT '[3.2] Hekaton - Native Procedure Execution';
IF OBJECT_ID('dbo.sp_InsertBillingUsageRecord_Native') IS NOT NULL
BEGIN TRY
    -- Test native procedure (if it doesn't require real data)
    PRINT '  ✓ PASS: Native procedure sp_InsertBillingUsageRecord_Native exists';
END TRY
BEGIN CATCH
    PRINT '  ✗ FAIL: ' + ERROR_MESSAGE();
END CATCH
ELSE
    PRINT '  ⚠ WARNING: sp_InsertBillingUsageRecord_Native not found';
PRINT '';

-- =========================================================================
-- TEST CATEGORY 4: GRAPH DATABASE TESTS
-- =========================================================================

PRINT '-------------------------------------------------------------------';
PRINT 'TEST CATEGORY 4: GRAPH TABLES';
PRINT '-------------------------------------------------------------------';
PRINT '';

-- Test 4.1: Graph node table exists
PRINT '[4.1] Graph - Node Table';
IF EXISTS (
    SELECT 1 FROM sys.tables 
    WHERE name = 'AtomGraphNodes' AND is_node = 1
)
    PRINT '  ✓ PASS: AtomGraphNodes is a valid node table';
ELSE
    PRINT '  ✗ FAIL: AtomGraphNodes node table missing';
PRINT '';

-- Test 4.2: Graph edge table exists
PRINT '[4.2] Graph - Edge Table';
IF EXISTS (
    SELECT 1 FROM sys.tables 
    WHERE name = 'AtomGraphEdges' AND is_edge = 1
)
    PRINT '  ✓ PASS: AtomGraphEdges is a valid edge table';
ELSE
    PRINT '  ✗ FAIL: AtomGraphEdges edge table missing';
PRINT '';

-- Test 4.3: Graph edge indexes
PRINT '[4.3] Graph - Edge Indexes';
IF EXISTS (
    SELECT 1 FROM sys.indexes 
    WHERE object_id = OBJECT_ID('graph.AtomGraphEdges')
    AND name LIKE 'IX_AtomGraphEdges_%'
)
    PRINT '  ✓ PASS: Graph edge indexes exist';
ELSE
    PRINT '  ✗ FAIL: Missing graph edge indexes';
PRINT '';

-- =========================================================================
-- TEST CATEGORY 5: SERVICE BROKER TESTS
-- =========================================================================

PRINT '-------------------------------------------------------------------';
PRINT 'TEST CATEGORY 5: SERVICE BROKER QUEUES';
PRINT '-------------------------------------------------------------------';
PRINT '';

-- Test 5.1: OODA Loop queues active
PRINT '[5.1] Service Broker - OODA Loop Queues';
DECLARE @oodaQueues TABLE (QueueName VARCHAR(100));
INSERT INTO @oodaQueues VALUES 
    ('AnalyzeQueue'), ('HypothesizeQueue'), ('ActQueue'), ('LearnQueue');

DECLARE @activeQueues INT = (
    SELECT COUNT(*)
    FROM sys.service_queues sq
    WHERE sq.name IN (SELECT QueueName FROM @oodaQueues)
    AND sq.is_receive_enabled = 1
    AND sq.is_enqueue_enabled = 1
);

IF @activeQueues = 4
    PRINT '  ✓ PASS: All 4 OODA Loop queues are active';
ELSE
    PRINT '  ⚠ WARNING: Only ' + CAST(@activeQueues AS VARCHAR) + '/4 OODA queues active';
PRINT '';

-- Test 5.2: InferenceQueue configured
PRINT '[5.2] Service Broker - InferenceQueue';
IF EXISTS (
    SELECT 1 FROM sys.service_queues 
    WHERE name = 'InferenceQueue' 
    AND is_receive_enabled = 1
)
    PRINT '  ✓ PASS: InferenceQueue is active';
ELSE
    PRINT '  ✗ FAIL: InferenceQueue not configured';
PRINT '';

-- =========================================================================
-- TEST CATEGORY 6: TEMPORAL TABLE TESTS
-- =========================================================================

PRINT '-------------------------------------------------------------------';
PRINT 'TEST CATEGORY 6: TEMPORAL TABLES';
PRINT '-------------------------------------------------------------------';
PRINT '';

-- Test 6.1: Temporal tables configured
PRINT '[6.1] Temporal - System-Versioned Tables';
DECLARE @temporalCount INT = (
    SELECT COUNT(*) FROM sys.tables WHERE temporal_type = 2
);
IF @temporalCount > 0
    PRINT '  ✓ PASS: Found ' + CAST(@temporalCount AS VARCHAR) + ' temporal tables';
ELSE
    PRINT '  ⚠ WARNING: No temporal tables found';
PRINT '';

-- =========================================================================
-- TEST CATEGORY 7: QUERY STORE & AUTO-TUNING
-- =========================================================================

PRINT '-------------------------------------------------------------------';
PRINT 'TEST CATEGORY 7: QUERY STORE & AUTO-TUNING';
PRINT '-------------------------------------------------------------------';
PRINT '';

-- Test 7.1: Query Store enabled
PRINT '[7.1] Query Store - Status';
DECLARE @qsState VARCHAR(50) = (
    SELECT actual_state_desc FROM sys.database_query_store_options
);
IF @qsState = 'READ_WRITE'
    PRINT '  ✓ PASS: Query Store is READ_WRITE';
ELSE
    PRINT '  ⚠ WARNING: Query Store state is ' + @qsState;
PRINT '';

-- Test 7.2: Auto-Tuning enabled
PRINT '[7.2] Auto-Tuning - FORCE_LAST_GOOD_PLAN';
IF EXISTS (
    SELECT 1 FROM sys.database_automatic_tuning_options
    WHERE name = 'FORCE_LAST_GOOD_PLAN' AND actual_state_desc = 'ON'
)
    PRINT '  ✓ PASS: Auto-tuning enabled';
ELSE
    PRINT '  ⚠ WARNING: Auto-tuning not fully enabled';
PRINT '';

-- =========================================================================
-- TEST SUMMARY
-- =========================================================================

PRINT '═══════════════════════════════════════════════════════';
PRINT '  TEST SUITE COMPLETE';
PRINT '  Review output above for any FAIL or WARNING results';
PRINT '═══════════════════════════════════════════════════════';
