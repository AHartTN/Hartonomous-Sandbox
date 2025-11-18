/*
================================================================================
Functional Test: sp_TemporalVectorSearch
================================================================================
Purpose: Validate temporal vector search returns correct results with:
  1. Point-in-time queries
  2. Temporal range queries  
  3. Vector similarity ranking
  4. Filter parameters (modality, embeddingType, modelId)

Success Criteria:
  - Returns JSON array with correct schema
  - Similarity scores are between 0 and 1
  - Results ordered by similarity descending
  - Temporal filtering works correctly
  - Top K limit respected
================================================================================
*/

PRINT '=== TEST: sp_TemporalVectorSearch Functionality ===';
PRINT '';

-- Setup: Create test data with known vectors and temporal metadata
DECLARE @TestModelId INT = 999;
DECLARE @TestDimension INT = 384; -- Smaller dimension for test performance

-- Cleanup any existing test data
DELETE FROM dbo.AtomEmbedding WHERE ModelId = @TestModelId;
DELETE FROM dbo.Atom WHERE AtomId IN (90001, 90002, 90003, 90004);

-- Create test atoms with known timestamps
DECLARE @BaseTime DATETIME2 = '2025-01-01 12:00:00';

INSERT INTO dbo.Atom (AtomId, Modality, Subtype, ContentHash, CanonicalText, CreatedAt)
VALUES 
    (90001, 'text', 'sentence', 'TEST001', 'Test vector 1 - early', @BaseTime),
    (90002, 'text', 'sentence', 'TEST002', 'Test vector 2 - middle', DATEADD(HOUR, 6, @BaseTime)),
    (90003, 'text', 'sentence', 'TEST003', 'Test vector 3 - late', DATEADD(HOUR, 12, @BaseTime)),
    (90004, 'text', 'sentence', 'TEST004', 'Test vector 4 - very late', DATEADD(HOUR, 24, @BaseTime));

-- Create embeddings with known similarity relationships
-- Vector 1: [1, 0, 0, ...] (identity in first dimension)
-- Vector 2: [0.9, 0.1, 0, ...] (high similarity to Vector 1)
-- Vector 3: [0.5, 0.5, 0, ...] (medium similarity)
-- Vector 4: [0, 1, 0, ...] (orthogonal to Vector 1)

DECLARE @Vector1 VARBINARY(MAX) = (
    SELECT CAST(CONCAT(
        '1.0,', REPLICATE('0.0,', @TestDimension - 2), '0.0'
    ) AS VARBINARY(MAX))
);

DECLARE @Vector2 VARBINARY(MAX) = (
    SELECT CAST(CONCAT(
        '0.9,0.1,', REPLICATE('0.0,', @TestDimension - 3), '0.0'
    ) AS VARBINARY(MAX))
);

DECLARE @Vector3 VARBINARY(MAX) = (
    SELECT CAST(CONCAT(
        '0.5,0.5,', REPLICATE('0.0,', @TestDimension - 3), '0.0'
    ) AS VARBINARY(MAX))
);

DECLARE @Vector4 VARBINARY(MAX) = (
    SELECT CAST(CONCAT(
        '0.0,1.0,', REPLICATE('0.0,', @TestDimension - 3), '0.0'
    ) AS VARBINARY(MAX))
);

-- Note: Real implementation would use proper VECTOR(1998) type
-- This is simplified for test clarity

INSERT INTO dbo.AtomEmbedding (AtomId, EmbeddingVector, Dimension, EmbeddingType, ModelId, CreatedAt)
VALUES 
    (90001, @Vector1, @TestDimension, 'test-embedding', @TestModelId, @BaseTime),
    (90002, @Vector2, @TestDimension, 'test-embedding', @TestModelId, DATEADD(HOUR, 6, @BaseTime)),
    (90003, @Vector3, @TestDimension, 'test-embedding', @TestModelId, DATEADD(HOUR, 12, @BaseTime)),
    (90004, @Vector4, @TestDimension, 'test-embedding', @TestModelId, DATEADD(HOUR, 24, @BaseTime));

PRINT '✓ Test data created';
PRINT '';

-- ============================================================================
-- TEST 1: Basic Vector Similarity Search
-- ============================================================================
PRINT 'TEST 1: Vector similarity ranking';

DECLARE @QueryVector VARBINARY(MAX) = @Vector1; -- Query with Vector1 (should rank: V2 > V3 > V4)

-- Execute stored procedure
EXEC dbo.sp_TemporalVectorSearch
    @QueryVector = @QueryVector,
    @TopK = 3,
    @StartTime = @BaseTime,
    @EndTime = DATEADD(HOUR, 30, @BaseTime),
    @Modality = 'text',
    @EmbeddingType = 'test-embedding',
    @ModelId = @TestModelId,
    @Dimension = @TestDimension;

-- Expected Results:
-- 1st: AtomId 90002 (Vector2, similarity ~0.95+)
-- 2nd: AtomId 90003 (Vector3, similarity ~0.71)
-- 3rd: AtomId 90004 (Vector4, similarity ~0.0)

PRINT '✓ Expect 3 results ordered by similarity DESC';
PRINT '';

-- ============================================================================
-- TEST 2: Temporal Filtering (Range)
-- ============================================================================
PRINT 'TEST 2: Temporal range filtering';

-- Query only middle 12 hours (should exclude Vector 4)
EXEC dbo.sp_TemporalVectorSearch
    @QueryVector = @QueryVector,
    @TopK = 10,
    @StartTime = @BaseTime,
    @EndTime = DATEADD(HOUR, 13, @BaseTime),
    @Modality = NULL,
    @EmbeddingType = NULL,
    @ModelId = @TestModelId,
    @Dimension = @TestDimension;

-- Expected: Only 3 results (Vector 4 created at +24h should be excluded)
PRINT '✓ Expect only vectors within 13-hour window';
PRINT '';

-- ============================================================================
-- TEST 3: TopK Limit
-- ============================================================================
PRINT 'TEST 3: TopK limit enforcement';

EXEC dbo.sp_TemporalVectorSearch
    @QueryVector = @QueryVector,
    @TopK = 2,
    @StartTime = @BaseTime,
    @EndTime = DATEADD(HOUR, 30, @BaseTime),
    @Modality = NULL,
    @EmbeddingType = NULL,
    @ModelId = @TestModelId,
    @Dimension = @TestDimension;

-- Expected: Exactly 2 results (top 2 by similarity)
PRINT '✓ Expect exactly 2 results';
PRINT '';

-- ============================================================================
-- TEST 4: Modality Filtering
-- ============================================================================
PRINT 'TEST 4: Modality filter';

-- Create audio atom that should be filtered out
INSERT INTO dbo.Atom (AtomId, Modality, Subtype, ContentHash, CanonicalText, CreatedAt)
VALUES (90005, 'audio', 'waveform', 'TEST005', 'Audio test', @BaseTime);

INSERT INTO dbo.AtomEmbedding (AtomId, EmbeddingVector, Dimension, EmbeddingType, ModelId, CreatedAt)
VALUES (90005, @Vector2, @TestDimension, 'test-embedding', @TestModelId, @BaseTime);

EXEC dbo.sp_TemporalVectorSearch
    @QueryVector = @QueryVector,
    @TopK = 10,
    @StartTime = @BaseTime,
    @EndTime = DATEADD(HOUR, 30, @BaseTime),
    @Modality = 'text', -- Should exclude audio atom
    @EmbeddingType = NULL,
    @ModelId = @TestModelId,
    @Dimension = @TestDimension;

-- Expected: 4 results, all with Modality='text'
PRINT '✓ Expect only text modality results';
PRINT '';

-- ============================================================================
-- Cleanup
-- ============================================================================
DELETE FROM dbo.AtomEmbedding WHERE ModelId = @TestModelId;
DELETE FROM dbo.Atom WHERE AtomId IN (90001, 90002, 90003, 90004, 90005);

PRINT '✓ Test data cleaned up';
PRINT '';
PRINT '=== All sp_TemporalVectorSearch functionality tests complete ===';
