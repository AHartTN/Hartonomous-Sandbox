-- =============================================
-- Test: sp_SearchSemanticVector Performance & Correctness
-- Validates vector search with real embeddings
-- =============================================

USE Hartonomous;
GO

PRINT '=================================================';
PRINT 'TEST: sp_SearchSemanticVector';
PRINT '=================================================';

-- Setup: Create test atoms and embeddings
DECLARE @TestAtomId1 BIGINT, @TestAtomId2 BIGINT, @TestAtomId3 BIGINT;
DECLARE @TestEmbedding1 VARBINARY(MAX), @TestEmbedding2 VARBINARY(MAX), @TestEmbedding3 VARBINARY(MAX);

-- Create test embedding vectors (512-dim normalized)
-- Embedding 1: [1,0,0,...] (first dimension high)
-- Embedding 2: [0,1,0,...] (second dimension high)
-- Embedding 3: [0.707,0.707,0,...] (45-degree between 1 and 2)
SET @TestEmbedding1 = (SELECT dbo.clr_RealArrayToBinary(CAST('<floats><v>1.0</v>' + REPLICATE('<v>0.0</v>', 511) + '</floats>' AS XML)));
SET @TestEmbedding2 = (SELECT dbo.clr_RealArrayToBinary(CAST('<floats><v>0.0</v><v>1.0</v>' + REPLICATE('<v>0.0</v>', 510) + '</floats>' AS XML)));
SET @TestEmbedding3 = (SELECT dbo.clr_RealArrayToBinary(CAST('<floats><v>0.707</v><v>0.707</v>' + REPLICATE('<v>0.0</v>', 510) + '</floats>' AS XML)));

-- Insert test atoms
INSERT INTO dbo.Atoms (ContentHash, Modality, CanonicalText)
VALUES 
    (HASHBYTES('SHA2_256', 'test_atom_1'), 'text', 'First test atom for semantic search'),
    (HASHBYTES('SHA2_256', 'test_atom_2'), 'text', 'Second test atom orthogonal to first'),
    (HASHBYTES('SHA2_256', 'test_atom_3'), 'text', 'Third test atom between first and second');

SET @TestAtomId1 = SCOPE_IDENTITY() - 2;
SET @TestAtomId2 = SCOPE_IDENTITY() - 1;
SET @TestAtomId3 = SCOPE_IDENTITY();

-- Insert embeddings
INSERT INTO dbo.AtomEmbeddings (AtomId, EmbeddingType, ModelId, Embedding)
VALUES
    (@TestAtomId1, 'semantic', 1, @TestEmbedding1),
    (@TestAtomId2, 'semantic', 1, @TestEmbedding2),
    (@TestAtomId3, 'semantic', 1, @TestEmbedding3);

PRINT 'Setup: Created 3 test atoms with embeddings';

-- TEST 1: Search for nearest to Embedding1 (should return Atom1, then Atom3, then Atom2)
PRINT '';
PRINT 'TEST 1: Nearest neighbor search';
DECLARE @StartTime DATETIME2 = SYSUTCDATETIME();

EXEC dbo.sp_SearchSemanticVector
    @QueryEmbedding = @TestEmbedding1,
    @TopK = 3,
    @ModelId = 1,
    @Debug = 1;

DECLARE @Duration1 INT = DATEDIFF(MILLISECOND, @StartTime, SYSUTCDATETIME());
PRINT 'Duration: ' + CAST(@Duration1 AS VARCHAR(10)) + ' ms';

-- TEST 2: Search with distance threshold (cosine similarity > 0.5)
PRINT '';
PRINT 'TEST 2: Distance threshold filtering';
SET @StartTime = SYSUTCDATETIME();

EXEC dbo.sp_SearchSemanticVector
    @QueryEmbedding = @TestEmbedding1,
    @TopK = 10,
    @ModelId = 1,
    @DistanceThreshold = 0.5,  -- Only atoms with similarity > 0.5
    @Debug = 1;

DECLARE @Duration2 INT = DATEDIFF(MILLISECOND, @StartTime, SYSUTCDATETIME());
PRINT 'Duration: ' + CAST(@Duration2 AS VARCHAR(10)) + ' ms';

-- TEST 3: Measure performance with larger dataset (if exists)
PRINT '';
PRINT 'TEST 3: Performance test on full dataset';
SET @StartTime = SYSUTCDATETIME();

EXEC dbo.sp_SearchSemanticVector
    @QueryEmbedding = @TestEmbedding1,
    @TopK = 100,
    @ModelId = 1,
    @Debug = 0;  -- No debug output for perf test

DECLARE @Duration3 INT = DATEDIFF(MILLISECOND, @StartTime, SYSUTCDATETIME());
DECLARE @TotalEmbeddings INT = (SELECT COUNT(*) FROM dbo.AtomEmbeddings);
PRINT 'Duration: ' + CAST(@Duration3 AS VARCHAR(10)) + ' ms for ' + CAST(@TotalEmbeddings AS VARCHAR(10)) + ' embeddings';
PRINT 'Throughput: ' + CAST((@TotalEmbeddings * 1000.0 / NULLIF(@Duration3, 0)) AS VARCHAR(20)) + ' embeddings/sec';

-- Cleanup
DELETE FROM dbo.AtomEmbeddings WHERE AtomId IN (@TestAtomId1, @TestAtomId2, @TestAtomId3);
DELETE FROM dbo.Atoms WHERE AtomId IN (@TestAtomId1, @TestAtomId2, @TestAtomId3);

PRINT '';
PRINT 'TEST COMPLETE: Cleanup successful';
PRINT '=================================================';
GO
