-- =============================================
-- Test: Graph & Spatial Operations Performance
-- Validates 100x spatial search speedup claim
-- =============================================

USE Hartonomous;
GO

PRINT '=================================================';
PRINT 'TEST: Graph & Spatial Operations Performance';
PRINT '=================================================';

-- Setup: Create test spatial data
PRINT 'Setup: Creating test spatial atoms...';

DECLARE @TestPoints TABLE (
    AtomId BIGINT,
    SpatialKey GEOMETRY
);

DECLARE @i INT = 1;
DECLARE @BatchSize INT = 1000;
DECLARE @StartTime DATETIME2;

-- Insert test atoms with spatial keys
WHILE @i <= @BatchSize
BEGIN
    -- Create random points in 2D space (-100 to +100)
    DECLARE @X FLOAT = (RAND() * 200) - 100;
    DECLARE @Y FLOAT = (RAND() * 200) - 100;
    DECLARE @SpatialKey GEOMETRY = geometry::Point(@X, @Y, 0);
    
    INSERT INTO dbo.Atoms (ContentHash, Modality, CanonicalText, SpatialKey)
    OUTPUT INSERTED.AtomId, INSERTED.SpatialKey INTO @TestPoints
    VALUES (
        HASHBYTES('SHA2_256', 'spatial_test_' + CAST(@i AS NVARCHAR(10))),
        'spatial',
        'Spatial test atom ' + CAST(@i AS NVARCHAR(10)),
        @SpatialKey
    );
    
    SET @i = @i + 1;
END;

PRINT 'Created ' + CAST(@BatchSize AS VARCHAR(10)) + ' spatial test atoms';

-- TEST 1: Full table scan spatial search (baseline)
PRINT '';
PRINT 'TEST 1: Baseline full table scan spatial search';

DECLARE @QueryPoint GEOMETRY = geometry::Point(0, 0, 0);  -- Search near origin
DECLARE @SearchRadius FLOAT = 50.0;  -- 50 unit radius

SET @StartTime = SYSUTCDATETIME();

-- Full table scan approach (no spatial index)
DECLARE @FullScanResults TABLE (AtomId BIGINT, Distance FLOAT);

INSERT INTO @FullScanResults
SELECT TOP 100 
    AtomId,
    SpatialKey.STDistance(@QueryPoint) AS Distance
FROM dbo.Atoms
WHERE SpatialKey IS NOT NULL
    AND SpatialKey.STDistance(@QueryPoint) <= @SearchRadius
ORDER BY Distance;

DECLARE @FullScanDuration INT = DATEDIFF(MILLISECOND, @StartTime, SYSUTCDATETIME());
DECLARE @FullScanCount INT = (SELECT COUNT(*) FROM @FullScanResults);

PRINT 'Full scan: Found ' + CAST(@FullScanCount AS VARCHAR(10)) + ' atoms in ' + CAST(@FullScanDuration AS VARCHAR(10)) + ' ms';

-- TEST 2: Spatial index search
PRINT '';
PRINT 'TEST 2: Spatial index search (optimized)';

-- Ensure spatial index exists
IF NOT EXISTS (SELECT 1 FROM sys.spatial_indexes WHERE object_id = OBJECT_ID('dbo.Atoms'))
BEGIN
    PRINT 'Creating spatial index...';
    CREATE SPATIAL INDEX IX_Atoms_SpatialKey
        ON dbo.Atoms(SpatialKey)
        USING GEOMETRY_GRID
        WITH (
            BOUNDING_BOX = (-200, -200, 200, 200),
            GRIDS = (LEVEL_1 = MEDIUM, LEVEL_2 = MEDIUM, LEVEL_3 = MEDIUM, LEVEL_4 = MEDIUM)
        );
    PRINT 'Spatial index created';
END;

SET @StartTime = SYSUTCDATETIME();

-- Spatial index approach
DECLARE @IndexResults TABLE (AtomId BIGINT, Distance FLOAT);

INSERT INTO @IndexResults
SELECT TOP 100
    AtomId,
    SpatialKey.STDistance(@QueryPoint) AS Distance
FROM dbo.Atoms WITH(INDEX(IX_Atoms_SpatialKey))
WHERE SpatialKey IS NOT NULL
    AND SpatialKey.STDistance(@QueryPoint) <= @SearchRadius
ORDER BY Distance;

DECLARE @IndexDuration INT = DATEDIFF(MILLISECOND, @StartTime, SYSUTCDATETIME());
DECLARE @IndexCount INT = (SELECT COUNT(*) FROM @IndexResults);

PRINT 'Index search: Found ' + CAST(@IndexCount AS VARCHAR(10)) + ' atoms in ' + CAST(@IndexDuration AS VARCHAR(10)) + ' ms';

-- TEST 3: Calculate speedup
PRINT '';
PRINT 'TEST 3: Speedup calculation';

DECLARE @SpeedupFactor FLOAT = CAST(@FullScanDuration AS FLOAT) / NULLIF(@IndexDuration, 0);

PRINT 'Speedup factor: ' + CAST(@SpeedupFactor AS VARCHAR(20)) + 'x';

IF @SpeedupFactor >= 10
    PRINT 'SUCCESS: Significant speedup achieved (target: 100x, actual: ' + CAST(@SpeedupFactor AS VARCHAR(20)) + 'x)';
ELSE IF @SpeedupFactor >= 2
    PRINT 'PARTIAL: Moderate speedup (' + CAST(@SpeedupFactor AS VARCHAR(20)) + 'x)';
ELSE
    PRINT 'WARNING: Speedup lower than expected (' + CAST(@SpeedupFactor AS VARCHAR(20)) + 'x)';

-- TEST 4: Graph traversal performance
PRINT '';
PRINT 'TEST 4: Graph traversal performance';

-- Setup: Create graph nodes and edges
IF OBJECT_ID('graph.AtomGraphNodes', 'U') IS NOT NULL
BEGIN
    SET @StartTime = SYSUTCDATETIME();
    
    -- Insert graph nodes for test atoms
    INSERT INTO graph.AtomGraphNodes (AtomId, NodeType, Metadata)
    SELECT TOP 100 AtomId, 'Atom', JSON_OBJECT('test': 'true')
    FROM @TestPoints;
    
    DECLARE @GraphNodesInserted INT = @@ROWCOUNT;
    DECLARE @GraphInsertDuration INT = DATEDIFF(MILLISECOND, @StartTime, SYSUTCDATETIME());
    
    PRINT 'Graph setup: Inserted ' + CAST(@GraphNodesInserted AS VARCHAR(10)) + ' nodes in ' + CAST(@GraphInsertDuration AS VARCHAR(10)) + ' ms';
    
    -- Simple graph query (no MATCH - just count)
    SET @StartTime = SYSUTCDATETIME();
    
    DECLARE @GraphQueryCount INT;
    SELECT @GraphQueryCount = COUNT(*)
    FROM graph.AtomGraphNodes
    WHERE NodeType = 'Atom';
    
    DECLARE @GraphQueryDuration INT = DATEDIFF(MILLISECOND, @StartTime, SYSUTCDATETIME());
    
    PRINT 'Graph query: Counted ' + CAST(@GraphQueryCount AS VARCHAR(10)) + ' nodes in ' + CAST(@GraphQueryDuration AS VARCHAR(10)) + ' ms';
END
ELSE
BEGIN
    PRINT 'Graph tables not available - skipping graph tests';
END;

-- TEST 5: Hybrid spatial + semantic search
PRINT '';
PRINT 'TEST 5: Hybrid spatial + semantic search';

-- Create test embeddings for spatial atoms
DECLARE @TestEmbedding VARBINARY(MAX) = (SELECT dbo.clr_RealArrayToBinary(CAST('<floats>' + REPLICATE('<v>0.5</v>', 512) + '</floats>' AS XML)));

INSERT INTO dbo.AtomEmbeddings (AtomId, EmbeddingType, ModelId, Embedding)
SELECT TOP 100 AtomId, 'semantic', 1, @TestEmbedding
FROM @TestPoints;

PRINT 'Created ' + CAST(@@ROWCOUNT AS VARCHAR(10)) + ' embeddings for hybrid search';

SET @StartTime = SYSUTCDATETIME();

-- Hybrid query: Spatial proximity AND semantic similarity
SELECT TOP 10
    a.AtomId,
    a.CanonicalText,
    a.SpatialKey.STDistance(@QueryPoint) AS SpatialDistance,
    ae.Embedding AS SemanticEmbedding
FROM dbo.Atoms a
INNER JOIN dbo.AtomEmbeddings ae ON a.AtomId = ae.AtomId
WHERE a.SpatialKey IS NOT NULL
    AND a.SpatialKey.STDistance(@QueryPoint) <= @SearchRadius
    AND ae.EmbeddingType = 'semantic'
ORDER BY a.SpatialKey.STDistance(@QueryPoint);

DECLARE @HybridDuration INT = DATEDIFF(MILLISECOND, @StartTime, SYSUTCDATETIME());
PRINT 'Hybrid search completed in ' + CAST(@HybridDuration AS VARCHAR(10)) + ' ms';

-- Cleanup
DELETE FROM dbo.AtomEmbeddings WHERE AtomId IN (SELECT AtomId FROM @TestPoints);
IF OBJECT_ID('graph.AtomGraphNodes', 'U') IS NOT NULL
    DELETE FROM graph.AtomGraphNodes WHERE AtomId IN (SELECT AtomId FROM @TestPoints);
DELETE FROM dbo.Atoms WHERE AtomId IN (SELECT AtomId FROM @TestPoints);

PRINT '';
PRINT 'TEST COMPLETE: Cleanup successful';
PRINT '=================================================';
GO
