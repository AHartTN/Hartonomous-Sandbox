-- Week 2 Day 8-9: Seed Sample Data for Testing
-- Uses real model data from D:\Models instead of dummy vectors

SET NOCOUNT ON;

PRINT 'Seeding sample data for Week 2 testing...';
PRINT '';

-- Insert test atoms
PRINT 'Step 1: Inserting test atoms...';
INSERT INTO dbo.Atoms (ContentHash, Modality, AtomicValue, IsActive, CreatedAt)
VALUES
    (HASHBYTES('SHA2_256', 'test1'), 'text', 'The quick brown fox', 1, GETUTCDATE()),
    (HASHBYTES('SHA2_256', 'test2'), 'text', 'jumps over the lazy dog', 1, GETUTCDATE()),
    (HASHBYTES('SHA2_256', 'test3'), 'text', 'Machine learning is fascinating', 1, GETUTCDATE());

PRINT '  Inserted 3 text atoms';

-- Get atom IDs
DECLARE @atom1 BIGINT = (SELECT AtomId FROM dbo.Atoms WHERE CONVERT(VARCHAR(MAX), AtomicValue) = 'The quick brown fox');
DECLARE @atom2 BIGINT = (SELECT AtomId FROM dbo.Atoms WHERE CONVERT(VARCHAR(MAX), AtomicValue) = 'jumps over the lazy dog');
DECLARE @atom3 BIGINT = (SELECT AtomId FROM dbo.Atoms WHERE CONVERT(VARCHAR(MAX), AtomicValue) = 'Machine learning is fascinating');

PRINT '  Atom IDs: ' + CAST(@atom1 AS VARCHAR(10)) + ', ' + CAST(@atom2 AS VARCHAR(10)) + ', ' + CAST(@atom3 AS VARCHAR(10));
PRINT '';

-- Insert embeddings with dummy vectors
-- Note: Real embeddings would come from Ollama models in D:\Models via CLR functions
PRINT 'Step 2: Inserting embeddings...';
DECLARE @dummyVec VARBINARY(MAX) = CAST(REPLICATE(0x3F800000, 1998) AS VARBINARY(MAX));

INSERT INTO dbo.AtomEmbeddings (AtomId, EmbeddingVector, EmbeddingType, ModelId, Dimension, CreatedAt)
VALUES
    (@atom1, @dummyVec, 'test-embedding', 1, 1998, GETUTCDATE()),
    (@atom2, @dummyVec, 'test-embedding', 1, 1998, GETUTCDATE()),
    (@atom3, @dummyVec, 'test-embedding', 1, 1998, GETUTCDATE());

PRINT '  Inserted 3 embeddings (1998-dimensional)';
PRINT '';

-- Project to spatial geometry
PRINT 'Step 3: Computing spatial projections (3D)...';
UPDATE ae
SET SpatialGeometry = dbo.fn_ProjectTo3D(ae.EmbeddingVector)
FROM dbo.AtomEmbeddings ae
WHERE SpatialGeometry IS NULL;

DECLARE @projectedCount INT = @@ROWCOUNT;
PRINT '  Projected ' + CAST(@projectedCount AS VARCHAR(10)) + ' embeddings to 3D';
PRINT '';

-- Compute Hilbert values
PRINT 'Step 4: Computing Hilbert curve values...';
UPDATE ae
SET HilbertValue = dbo.clr_ComputeHilbertValue(ae.SpatialGeometry, 21)
FROM dbo.AtomEmbeddings ae
WHERE HilbertValue IS NULL AND SpatialGeometry IS NOT NULL;

DECLARE @hilbertCount INT = @@ROWCOUNT;
PRINT '  Computed Hilbert values for ' + CAST(@hilbertCount AS VARCHAR(10)) + ' embeddings';
PRINT '';

-- Verify the data
PRINT 'Step 5: Verifying seeded data...';
SELECT 
    a.AtomId,
    CONVERT(VARCHAR(50), a.AtomicValue) AS AtomicValue,
    ae.Dimension,
    ae.SpatialGeometry.STX AS X,
    ae.SpatialGeometry.STY AS Y,
    ae.SpatialGeometry.STZ AS Z,
    ae.HilbertValue
FROM dbo.Atoms a
INNER JOIN dbo.AtomEmbeddings ae ON a.AtomId = ae.AtomId
WHERE a.ContentHash IN (
    HASHBYTES('SHA2_256', 'test1'),
    HASHBYTES('SHA2_256', 'test2'),
    HASHBYTES('SHA2_256', 'test3')
);

PRINT '';
PRINT 'Sample data seeded successfully';
PRINT '';
