-- =====================================================
-- Comprehensive Validation Script
-- Tests all fixes implemented in the remediation
-- =====================================================
-- Run this script after deploying the database changes
-- Expected: All tests should pass without errors
-- =====================================================

SET NOCOUNT ON;
GO

PRINT '========================================';
PRINT 'Hartonomous System Validation';
PRINT '========================================';
PRINT '';

-- =====================================================
-- TEST 1: Verify CLR Wrapper Functions Exist
-- =====================================================
PRINT 'TEST 1: Verifying CLR Wrapper Functions...';

DECLARE @missingFunctions TABLE (FunctionName NVARCHAR(255));

-- Critical vector operations
INSERT INTO @missingFunctions (FunctionName)
SELECT 'clr_VectorCosineSimilarity' WHERE OBJECT_ID('dbo.clr_VectorCosineSimilarity', 'FN') IS NULL
UNION ALL
SELECT 'clr_VectorEuclideanDistance' WHERE OBJECT_ID('dbo.clr_VectorEuclideanDistance', 'FN') IS NULL
UNION ALL
SELECT 'clr_VectorAverage' WHERE OBJECT_ID('dbo.clr_VectorAverage', 'FN') IS NULL
UNION ALL
SELECT 'clr_VectorDotProduct' WHERE OBJECT_ID('dbo.clr_VectorDotProduct', 'FN') IS NULL
UNION ALL
SELECT 'clr_VectorAdd' WHERE OBJECT_ID('dbo.clr_VectorAdd', 'FN') IS NULL
UNION ALL
SELECT 'clr_VectorNormalize' WHERE OBJECT_ID('dbo.clr_VectorNormalize', 'FN') IS NULL
UNION ALL
-- Spatial operations
SELECT 'fn_ProjectTo3D' WHERE OBJECT_ID('dbo.fn_ProjectTo3D', 'FN') IS NULL
UNION ALL
SELECT 'clr_ProjectToPoint' WHERE OBJECT_ID('dbo.clr_ProjectToPoint', 'FN') IS NULL
UNION ALL
SELECT 'clr_ComputeHilbertValue' WHERE OBJECT_ID('dbo.clr_ComputeHilbertValue', 'FN') IS NULL
UNION ALL
-- Code analysis
SELECT 'clr_GenerateCodeAstVector' WHERE OBJECT_ID('dbo.clr_GenerateCodeAstVector', 'FN') IS NULL;

IF EXISTS (SELECT 1 FROM @missingFunctions)
BEGIN
    PRINT '  ? FAILED: Missing CLR wrapper functions:';
    SELECT '    - ' + FunctionName FROM @missingFunctions;
END
ELSE
BEGIN
    PRINT '  ? PASSED: All critical CLR wrapper functions exist';
END
PRINT '';

-- =====================================================
-- TEST 2: Verify fn_FindNearestAtoms (TVF) Exists
-- =====================================================
PRINT 'TEST 2: Verifying fn_FindNearestAtoms (Table-Valued Function)...';

IF OBJECT_ID('dbo.fn_FindNearestAtoms', 'IF') IS NOT NULL
BEGIN
    PRINT '  ? PASSED: fn_FindNearestAtoms inline TVF exists';
END
ELSE IF OBJECT_ID('dbo.fn_FindNearestAtoms', 'TF') IS NOT NULL
BEGIN
    PRINT '  ?? WARNING: fn_FindNearestAtoms is multi-statement TVF (should be inline)';
END
ELSE
BEGIN
    PRINT '  ? FAILED: fn_FindNearestAtoms does not exist';
END
PRINT '';

-- =====================================================
-- TEST 3: Verify InferenceRequest Schema
-- =====================================================
PRINT 'TEST 3: Verifying InferenceRequest table schema...';

DECLARE @missingColumns TABLE (ColumnName NVARCHAR(255));

INSERT INTO @missingColumns (ColumnName)
SELECT 'TenantId' WHERE NOT EXISTS (
    SELECT 1 FROM sys.columns 
    WHERE object_id = OBJECT_ID('dbo.InferenceRequest') 
    AND name = 'TenantId'
)
UNION ALL
SELECT 'Temperature' WHERE NOT EXISTS (
    SELECT 1 FROM sys.columns 
    WHERE object_id = OBJECT_ID('dbo.InferenceRequest') 
    AND name = 'Temperature'
)
UNION ALL
SELECT 'TopK' WHERE NOT EXISTS (
    SELECT 1 FROM sys.columns 
    WHERE object_id = OBJECT_ID('dbo.InferenceRequest') 
    AND name = 'TopK'
)
UNION ALL
SELECT 'TopP' WHERE NOT EXISTS (
    SELECT 1 FROM sys.columns 
    WHERE object_id = OBJECT_ID('dbo.InferenceRequest') 
    AND name = 'TopP'
)
UNION ALL
SELECT 'MaxTokens' WHERE NOT EXISTS (
    SELECT 1 FROM sys.columns 
    WHERE object_id = OBJECT_ID('dbo.InferenceRequest') 
    AND name = 'MaxTokens'
);

IF EXISTS (SELECT 1 FROM @missingColumns)
BEGIN
    PRINT '  ? FAILED: Missing columns in InferenceRequest:';
    SELECT '    - ' + ColumnName FROM @missingColumns;
    PRINT '';
    PRINT '  ACTION REQUIRED: Run migration script:';
    PRINT '  src\Hartonomous.Database\Migrations\001_Add_InferenceRequest_Columns.sql';
END
ELSE
BEGIN
    PRINT '  ? PASSED: All required columns exist in InferenceRequest';
END
PRINT '';

-- =====================================================
-- TEST 4: Verify ModelMetadata Table Exists
-- =====================================================
PRINT 'TEST 4: Verifying ModelMetadata table...';

IF OBJECT_ID('dbo.ModelMetadata', 'U') IS NOT NULL
BEGIN
    PRINT '  ? PASSED: ModelMetadata table exists';
    
    -- Check critical columns
    IF EXISTS (
        SELECT 1 FROM sys.columns 
        WHERE object_id = OBJECT_ID('dbo.ModelMetadata') 
        AND name IN ('SupportedTasks', 'SupportedModalities')
        GROUP BY object_id
        HAVING COUNT(*) = 2
    )
    BEGIN
        PRINT '  ? PASSED: ModelMetadata has SupportedTasks and SupportedModalities columns';
    END
    ELSE
    BEGIN
        PRINT '  ? FAILED: ModelMetadata missing critical columns';
    END
END
ELSE
BEGIN
    PRINT '  ? FAILED: ModelMetadata table does not exist';
END
PRINT '';

-- =====================================================
-- TEST 5: Functional Test - CLR Vector Operations
-- =====================================================
PRINT 'TEST 5: Testing CLR vector operations...';

BEGIN TRY
    -- Create test vectors (4 floats = 16 bytes each)
    DECLARE @vector1 VARBINARY(MAX) = 0x3F8000003F0000003E8000003E000000; -- [1.0, 0.5, 0.25, 0.125]
    DECLARE @vector2 VARBINARY(MAX) = 0x3F8000003F8000003F8000003F800000; -- [1.0, 1.0, 1.0, 1.0]
    
    -- Test dot product
    DECLARE @dotProduct FLOAT = dbo.clr_VectorDotProduct(@vector1, @vector2);
    
    IF @dotProduct IS NOT NULL
    BEGIN
        PRINT '  ? PASSED: clr_VectorDotProduct returned: ' + CAST(@dotProduct AS NVARCHAR(50));
    END
    ELSE
    BEGIN
        PRINT '  ? FAILED: clr_VectorDotProduct returned NULL';
    END
    
    -- Test cosine similarity
    DECLARE @cosineSim FLOAT = dbo.clr_VectorCosineSimilarity(@vector1, @vector2);
    
    IF @cosineSim IS NOT NULL AND @cosineSim BETWEEN 0.0 AND 1.0
    BEGIN
        PRINT '  ? PASSED: clr_VectorCosineSimilarity returned: ' + CAST(@cosineSim AS NVARCHAR(50));
    END
    ELSE
    BEGIN
        PRINT '  ? FAILED: clr_VectorCosineSimilarity returned invalid value';
    END
END TRY
BEGIN CATCH
    PRINT '  ? FAILED: Error executing CLR vector operations';
    PRINT '    Error: ' + ERROR_MESSAGE();
END CATCH
PRINT '';

-- =====================================================
-- TEST 6: Functional Test - fn_FindNearestAtoms
-- =====================================================
PRINT 'TEST 6: Testing fn_FindNearestAtoms...';

IF OBJECT_ID('dbo.fn_FindNearestAtoms', 'IF') IS NOT NULL
BEGIN
    BEGIN TRY
        -- Create test query vector (1998 dimensions of 1.0)
        -- 1998 * 4 bytes per float = 7992 bytes
        DECLARE @queryVector VARBINARY(MAX) = CAST(REPLICATE(CAST(0x3F800000 AS BINARY(4)), 100) AS VARBINARY(MAX));
        
        -- Note: This will fail if no atoms exist in the database
        -- We're just testing that the function can be called without syntax errors
        DECLARE @resultCount INT = 0;
        
        SELECT @resultCount = COUNT(*)
        FROM dbo.fn_FindNearestAtoms(
            @queryVector,  -- @queryVector
            5,             -- @topK
            1000,          -- @spatialPoolSize
            0,             -- @tenantId
            NULL,          -- @modalityFilter
            1              -- @useHilbertClustering
        );
        
        PRINT '  ? PASSED: fn_FindNearestAtoms executed successfully';
        PRINT '    Results returned: ' + CAST(@resultCount AS NVARCHAR(50));
        
        IF @resultCount = 0
        BEGIN
            PRINT '    ?? NOTE: No atoms found (database may be empty)';
        END
    END TRY
    BEGIN CATCH
        PRINT '  ? FAILED: Error executing fn_FindNearestAtoms';
        PRINT '    Error: ' + ERROR_MESSAGE();
    END CATCH
END
ELSE
BEGIN
    PRINT '  ?? SKIPPED: fn_FindNearestAtoms does not exist';
END
PRINT '';

-- =====================================================
-- TEST 7: Verify sp_RunInference Syntax
-- =====================================================
PRINT 'TEST 7: Verifying sp_RunInference can be parsed...';

IF OBJECT_ID('dbo.sp_RunInference', 'P') IS NOT NULL
BEGIN
    PRINT '  ? PASSED: sp_RunInference stored procedure exists';
    
    -- Check if it references fn_FindNearestAtoms (not sp_FindNearestAtoms)
    DECLARE @procDefinition NVARCHAR(MAX);
    
    SELECT @procDefinition = OBJECT_DEFINITION(OBJECT_ID('dbo.sp_RunInference'));
    
    IF @procDefinition LIKE '%fn_FindNearestAtoms%'
    BEGIN
        PRINT '  ? PASSED: sp_RunInference correctly calls fn_FindNearestAtoms (TVF)';
    END
    ELSE IF @procDefinition LIKE '%sp_FindNearestAtoms%'
    BEGIN
        PRINT '  ? FAILED: sp_RunInference still calls sp_FindNearestAtoms (should be fn_)';
    END
    ELSE
    BEGIN
        PRINT '  ?? WARNING: Could not verify function call in sp_RunInference';
    END
END
ELSE
BEGIN
    PRINT '  ? FAILED: sp_RunInference does not exist';
END
PRINT '';

-- =====================================================
-- TEST 8: Verify Spatial Indexes
-- =====================================================
PRINT 'TEST 8: Verifying spatial indexes...';

IF EXISTS (
    SELECT 1 
    FROM sys.indexes 
    WHERE object_id = OBJECT_ID('dbo.AtomEmbedding') 
    AND name = 'IX_AtomEmbedding_SpatialGeometry'
)
BEGIN
    PRINT '  ? PASSED: IX_AtomEmbedding_SpatialGeometry exists';
END
ELSE
BEGIN
    PRINT '  ?? WARNING: IX_AtomEmbedding_SpatialGeometry does not exist';
    PRINT '    This index is critical for spatial search performance';
END
PRINT '';

-- =====================================================
-- FINAL SUMMARY
-- =====================================================
PRINT '========================================';
PRINT 'Validation Complete';
PRINT '========================================';
PRINT '';
PRINT 'Next Steps:';
PRINT '1. Review any failed tests above';
PRINT '2. If InferenceRequest columns are missing, run migration script';
PRINT '3. Test end-to-end inference pipeline with real data';
PRINT '4. Deploy to production when all tests pass';
PRINT '';
PRINT '========================================';
GO
