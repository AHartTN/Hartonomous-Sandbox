-- =============================================
-- PHASE 5: VERIFICATION SCRIPT
-- Technical Implementation Roadmap - All Phases
-- =============================================

SET NOCOUNT ON;
PRINT '=======================================================';
PRINT 'HARTONOMOUS TECHNICAL IMPLEMENTATION VERIFICATION';
PRINT 'Cognitive Kernel v1.0 - All Phases';
PRINT '=======================================================';
PRINT '';

-- =============================================
-- TEST 1: Verify M-Value Storage (Phase 2)
-- =============================================
PRINT '--- TEST 1: Self-Indexing Geometry (M-Value) ---';
PRINT 'Checking if SpatialKey.M contains Hilbert values...';

DECLARE @MValueCheck INT;
SELECT @MValueCheck = COUNT(*)
FROM dbo.AtomComposition
WHERE SpatialKey.M IS NOT NULL 
  AND SpatialKey.M > 0
  AND SpatialKey.M = dbo.fn_ComputeHilbertValue(SpatialKey, 16);

IF @MValueCheck > 0
BEGIN
    PRINT '? PASS: M-values are populated and match Hilbert calculations';
    PRINT '  Sample M-values:';
    
    SELECT TOP 5 
        ComponentAtomId,
        SpatialKey.STX AS X,
        SpatialKey.STY AS Y,
        SpatialKey.M AS HilbertValue,
        dbo.fn_ComputeHilbertValue(SpatialKey, 16) AS ComputedHilbert
    FROM dbo.AtomComposition
    WHERE SpatialKey.M > 0;
END
ELSE
BEGIN
    PRINT '? FAIL: M-values are NULL or do not match Hilbert calculations';
    PRINT '  Action Required: Run sp_AtomizeImage_Governed or sp_AtomizeText_Governed with new geometry construction';
END
PRINT '';

-- =============================================
-- TEST 2: Verify Columnstore Compression (Phase 2)
-- =============================================
PRINT '--- TEST 2: Columnstore Compression on AtomComposition ---';

DECLARE @HasColumnstore BIT;
SELECT @HasColumnstore = CASE 
    WHEN EXISTS (
        SELECT 1 
        FROM sys.indexes 
        WHERE object_id = OBJECT_ID('dbo.AtomComposition') 
          AND type = 5  -- Clustered Columnstore
    ) THEN 1 
    ELSE 0 
END;

IF @HasColumnstore = 1
BEGIN
    PRINT '? PASS: AtomComposition has Clustered Columnstore Index';
    
    -- Show compression statistics
    SELECT 
        'AtomComposition' AS TableName,
        SUM(reserved_page_count) * 8.0 / 1024 AS ReservedMB,
        SUM(used_page_count) * 8.0 / 1024 AS UsedMB,
        SUM(row_count) AS RowCount,
        CASE 
            WHEN SUM(row_count) > 0 
            THEN (SUM(used_page_count) * 8192.0) / SUM(row_count)
            ELSE 0 
        END AS BytesPerRow
    FROM sys.dm_db_partition_stats
    WHERE object_id = OBJECT_ID('dbo.AtomComposition');
    
    PRINT '  Expected: < 100 bytes/row for high compression ratio';
END
ELSE
BEGIN
    PRINT '? FAIL: AtomComposition does NOT have Columnstore Index';
    PRINT '  Action Required: Run Optimize_ColumnstoreCompression.sql';
END
PRINT '';

-- =============================================
-- TEST 3: Verify Semantic Path Cache (Phase 1)
-- =============================================
PRINT '--- TEST 3: Semantic Path Cache Table ---';

IF EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_SCHEMA = 'provenance' AND TABLE_NAME = 'SemanticPathCache')
BEGIN
    PRINT '? PASS: provenance.SemanticPathCache table exists';
    
    DECLARE @CachedPaths INT;
    SELECT @CachedPaths = COUNT(*) FROM provenance.SemanticPathCache;
    
    PRINT '  Cached paths: ' + CAST(@CachedPaths AS VARCHAR(10));
    
    IF @CachedPaths > 0
    BEGIN
        SELECT TOP 3
            StartAtomId,
            TargetConceptId,
            TotalCost,
            HitCount,
            CreatedAt,
            ExpiresAt
        FROM provenance.SemanticPathCache
        ORDER BY HitCount DESC;
    END
END
ELSE
BEGIN
    PRINT '? FAIL: provenance.SemanticPathCache table does NOT exist';
    PRINT '  Action Required: Deploy provenance.SemanticPathCache.sql';
END
PRINT '';

-- =============================================
-- TEST 4: Verify Vector Dimensions (Phase 3)
-- =============================================
PRINT '--- TEST 4: Vector Dimension Consistency ---';

-- Check sp_MultiModelEnsemble signature
DECLARE @ProcDef NVARCHAR(MAX);
SELECT @ProcDef = OBJECT_DEFINITION(OBJECT_ID('dbo.sp_MultiModelEnsemble'));

IF @ProcDef LIKE '%VECTOR(1536)%'
BEGIN
    PRINT '? PASS: sp_MultiModelEnsemble uses VECTOR(1536) (OpenAI standard)';
END
ELSE IF @ProcDef LIKE '%VECTOR(1998)%'
BEGIN
    PRINT '? FAIL: sp_MultiModelEnsemble still uses VECTOR(1998)';
    PRINT '  Action Required: Update to VECTOR(1536) or VECTOR(768)';
END
ELSE
BEGIN
    PRINT '? WARNING: Could not verify vector dimensions in sp_MultiModelEnsemble';
END
PRINT '';

-- =============================================
-- TEST 5: Verify CLR Functions (Phase 3)
-- =============================================
PRINT '--- TEST 5: CLR Function Availability ---';

DECLARE @CLRFunctions TABLE (FunctionName NVARCHAR(128), TypeDesc NVARCHAR(60));
INSERT INTO @CLRFunctions
SELECT name, type_desc
FROM sys.objects
WHERE name IN ('fn_ComputeEmbedding', 'fn_ProjectTo3D', 'clr_ComputeHilbertValue')
  AND type_desc LIKE '%FUNCTION%';

IF (SELECT COUNT(*) FROM @CLRFunctions) = 3
BEGIN
    PRINT '? PASS: All critical CLR functions deployed';
    SELECT * FROM @CLRFunctions;
END
ELSE
BEGIN
    PRINT '? FAIL: Missing CLR functions';
    SELECT * FROM @CLRFunctions;
    PRINT '  Action Required: Deploy CLR assembly with all required functions';
END
PRINT '';

-- =============================================
-- TEST 6: Verify sp_RunInference Logic (Phase 5)
-- =============================================
PRINT '--- TEST 6: sp_RunInference Implementation ---';

IF EXISTS (SELECT 1 FROM sys.objects WHERE name = 'sp_RunInference')
BEGIN
    DECLARE @InferenceDef NVARCHAR(MAX);
    SELECT @InferenceDef = OBJECT_DEFINITION(OBJECT_ID('dbo.sp_RunInference'));
    
    IF @InferenceDef LIKE '%fn_GenerateWithAttention%' OR @InferenceDef LIKE '%clr_RunInference%'
    BEGIN
        PRINT '? PASS: sp_RunInference calls CLR transformer inference';
    END
    ELSE IF @InferenceDef LIKE '%fn_FindNearestAtoms%'
    BEGIN
        PRINT '? WARNING: sp_RunInference only uses search, not inference';
        PRINT '  Recommendation: Integrate fn_GenerateWithAttention for reasoning tasks';
    END
    ELSE
    BEGIN
        PRINT '? WARNING: Could not verify sp_RunInference implementation';
    END
END
ELSE
BEGIN
    PRINT '? FAIL: sp_RunInference does not exist';
END
PRINT '';

-- =============================================
-- TEST 7: Compression Ratio Analysis (Phase 2)
-- =============================================
PRINT '--- TEST 7: Compression Ratio After Ingestion ---';

-- Only run if data exists
IF EXISTS (SELECT 1 FROM dbo.AtomComposition)
BEGIN
    DECLARE @CompressedSizeMB DECIMAL(10,2);
    DECLARE @RowCount BIGINT;
    DECLARE @CompressionRatio DECIMAL(5,2);
    
    SELECT 
        @CompressedSizeMB = SUM(used_page_count) * 8.0 / 1024,
        @RowCount = SUM(row_count)
    FROM sys.dm_db_partition_stats
    WHERE object_id = OBJECT_ID('dbo.AtomComposition');
    
    -- Estimate raw data size (assuming 64 bytes per atom minimum)
    DECLARE @EstimatedRawSizeMB DECIMAL(10,2) = (@RowCount * 64.0) / 1024 / 1024;
    SET @CompressionRatio = (@EstimatedRawSizeMB / NULLIF(@CompressedSizeMB, 0)) * 100;
    
    PRINT '  Rows: ' + CAST(@RowCount AS VARCHAR(20));
    PRINT '  Compressed Size: ' + CAST(@CompressedSizeMB AS VARCHAR(20)) + ' MB';
    PRINT '  Estimated Raw Size: ' + CAST(@EstimatedRawSizeMB AS VARCHAR(20)) + ' MB';
    PRINT '  Compression Ratio: ' + CAST(@CompressionRatio AS VARCHAR(10)) + '%';
    
    IF @CompressionRatio > 95
    BEGIN
        PRINT '? PASS: Compression ratio exceeds 95% target';
    END
    ELSE IF @CompressionRatio > 80
    BEGIN
        PRINT '? WARNING: Compression ratio ' + CAST(@CompressionRatio AS VARCHAR(10)) + '% (target: >95%)';
        PRINT '  Recommendation: Ensure Hilbert sorting in sp_AtomizeImage_Governed';
    END
    ELSE
    BEGIN
        PRINT '? FAIL: Compression ratio only ' + CAST(@CompressionRatio AS VARCHAR(10)) + '%';
        PRINT '  Action Required: Verify columnstore archive compression and Hilbert pre-sorting';
    END
END
ELSE
BEGIN
    PRINT '? No data in AtomComposition - cannot measure compression';
END
PRINT '';

-- =============================================
-- SUMMARY
-- =============================================
PRINT '=======================================================';
PRINT 'VERIFICATION COMPLETE';
PRINT '=======================================================';
PRINT '';
PRINT 'Next Steps:';
PRINT '1. Address any FAIL results above';
PRINT '2. Ingest test data (1000+ images recommended)';
PRINT '3. Run compression analysis again';
PRINT '4. Test end-to-end inference with sp_RunInference';
PRINT '';
PRINT '=======================================================';
GO
