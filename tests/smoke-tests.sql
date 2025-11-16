-- ═══════════════════════════════════════════════════════════════════
-- HARTONOMOUS SMOKE TESTS - WEEK 1
-- Validates core functionality after DACPAC deployment
-- ═══════════════════════════════════════════════════════════════════

SET NOCOUNT ON;

PRINT '';
PRINT '═══════════════════════════════════════════════════════';
PRINT '  HARTONOMOUS SMOKE TESTS - WEEK 1';
PRINT '═══════════════════════════════════════════════════════';
PRINT '';

DECLARE @TestsPassed INT = 0;
DECLARE @TestsFailed INT = 0;

-- ═══════════════════════════════════════════════════════════════════
-- TEST 1: CLR Functions Exist
-- ═══════════════════════════════════════════════════════════════════
PRINT 'Test 1: CLR Functions Exist...';
BEGIN TRY
    IF OBJECT_ID('dbo.fn_ProjectTo3D') IS NULL
        THROW 50000, 'fn_ProjectTo3D not found', 1;
    IF OBJECT_ID('dbo.clr_ComputeHilbertValue') IS NULL
        THROW 50000, 'clr_ComputeHilbertValue not found', 1;
    
    PRINT '  ✓ PASSED: CLR functions exist';
    SET @TestsPassed = @TestsPassed + 1;
END TRY
BEGIN CATCH
    PRINT '  ✗ FAILED: ' + ERROR_MESSAGE();
    SET @TestsFailed = @TestsFailed + 1;
END CATCH;

-- ═══════════════════════════════════════════════════════════════════
-- TEST 2: Spatial Projection Works
-- ═══════════════════════════════════════════════════════════════════
PRINT '';
PRINT 'Test 2: Spatial Projection Works...';
BEGIN TRY
    DECLARE @testVec VARBINARY(MAX) = CAST(REPLICATE(0x3F800000, 1998) AS VARBINARY(MAX));
    DECLARE @projected GEOMETRY = dbo.fn_ProjectTo3D(@testVec);
    
    IF @projected IS NULL
        THROW 50000, 'Projection returned NULL', 1;
    IF @projected.STX IS NULL OR @projected.STY IS NULL
        THROW 50000, 'Invalid projection coordinates', 1;
        
    PRINT '  ✓ PASSED: Projection working';
    SET @TestsPassed = @TestsPassed + 1;
END TRY
BEGIN CATCH
    PRINT '  ✗ FAILED: ' + ERROR_MESSAGE();
    SET @TestsFailed = @TestsFailed + 1;
END CATCH;

-- ═══════════════════════════════════════════════════════════════════
-- TEST 3: Hilbert Curve Computation
-- ═══════════════════════════════════════════════════════════════════
PRINT '';
PRINT 'Test 3: Hilbert Curve Computation...';
BEGIN TRY
    DECLARE @point GEOMETRY = geometry::STGeomFromText('POINT(0.5 0.5 0.5)', 0);
    DECLARE @hilbert BIGINT = dbo.clr_ComputeHilbertValue(@point, 21);
    
    IF @hilbert IS NULL OR @hilbert <= 0
        THROW 50000, 'Invalid Hilbert value', 1;
        
    PRINT '  ✓ PASSED: Hilbert curve working (value: ' + CAST(@hilbert AS VARCHAR(20)) + ')';
    SET @TestsPassed = @TestsPassed + 1;
END TRY
BEGIN CATCH
    PRINT '  ✗ FAILED: ' + ERROR_MESSAGE();
    SET @TestsFailed = @TestsFailed + 1;
END CATCH;

-- ═══════════════════════════════════════════════════════════════════
-- TEST 4: Spatial Indexes Exist
-- ═══════════════════════════════════════════════════════════════════
PRINT '';
PRINT 'Test 4: Spatial Indexes Exist...';
BEGIN TRY
    IF NOT EXISTS (
        SELECT 1 FROM sys.indexes
        WHERE name = 'SIX_AtomEmbeddings_SpatialKey'
          AND object_id = OBJECT_ID('dbo.AtomEmbeddings')
    )
        THROW 50000, 'Spatial index SIX_AtomEmbeddings_SpatialKey missing', 1;
        
    PRINT '  ✓ PASSED: Spatial indexes exist';
    SET @TestsPassed = @TestsPassed + 1;
END TRY
BEGIN CATCH
    PRINT '  ✗ FAILED: ' + ERROR_MESSAGE();
    SET @TestsFailed = @TestsFailed + 1;
END CATCH;

-- ═══════════════════════════════════════════════════════════════════
-- TEST 5: Service Broker Enabled
-- ═══════════════════════════════════════════════════════════════════
PRINT '';
PRINT 'Test 5: Service Broker Enabled...';
BEGIN TRY
    IF NOT EXISTS (
        SELECT 1 FROM sys.databases
        WHERE name = DB_NAME() AND is_broker_enabled = 1
    )
        THROW 50000, 'Service Broker not enabled', 1;
        
    PRINT '  ✓ PASSED: Service Broker enabled';
    SET @TestsPassed = @TestsPassed + 1;
END TRY
BEGIN CATCH
    PRINT '  ✗ FAILED: ' + ERROR_MESSAGE();
    SET @TestsFailed = @TestsFailed + 1;
END CATCH;

-- ═══════════════════════════════════════════════════════════════════
-- TEST 6: OODA Loop Procedures Exist
-- ═══════════════════════════════════════════════════════════════════
PRINT '';
PRINT 'Test 6: OODA Loop Procedures Exist...';
BEGIN TRY
    IF OBJECT_ID('dbo.sp_Analyze') IS NULL
        THROW 50000, 'sp_Analyze not found', 1;
    IF OBJECT_ID('dbo.sp_Hypothesize') IS NULL
        THROW 50000, 'sp_Hypothesize not found', 1;
    IF OBJECT_ID('dbo.sp_Act') IS NULL
        THROW 50000, 'sp_Act not found', 1;
    IF OBJECT_ID('dbo.sp_Learn') IS NULL
        THROW 50000, 'sp_Learn not found', 1;
        
    PRINT '  ✓ PASSED: OODA procedures exist';
    SET @TestsPassed = @TestsPassed + 1;
END TRY
BEGIN CATCH
    PRINT '  ✗ FAILED: ' + ERROR_MESSAGE();
    SET @TestsFailed = @TestsFailed + 1;
END CATCH;

-- ═══════════════════════════════════════════════════════════════════
-- SUMMARY
-- ═══════════════════════════════════════════════════════════════════
PRINT '';
PRINT '═══════════════════════════════════════════════════════';
PRINT '  TEST RESULTS';
PRINT '═══════════════════════════════════════════════════════';
PRINT '  Passed: ' + CAST(@TestsPassed AS VARCHAR(10));
PRINT '  Failed: ' + CAST(@TestsFailed AS VARCHAR(10));
PRINT '═══════════════════════════════════════════════════════';

IF @TestsFailed = 0
BEGIN
    PRINT '';
    PRINT '  ✓✓✓ ALL SMOKE TESTS PASSED ✓✓✓';
    PRINT '';
END
ELSE
BEGIN
    PRINT '';
    PRINT '  ✗✗✗ SOME TESTS FAILED ✗✗✗';
    PRINT '';
    -- Return error code
    THROW 50000, 'Smoke tests failed', 1;
END
