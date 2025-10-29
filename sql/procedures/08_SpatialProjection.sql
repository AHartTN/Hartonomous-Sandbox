-- SPATIAL PROJECTION SYSTEM
-- Pure T-SQL dimensionality reduction using distance-based coordinates
USE Hartonomous;
GO

SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;
GO

PRINT '============================================================';
PRINT 'SPATIAL PROJECTION SYSTEM';
PRINT 'Distance-Based Dimensionality Reduction in Pure T-SQL';
PRINT '============================================================';
GO

-- ==========================================
-- PART 1: Anchor Points Table
-- ==========================================

IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'SpatialAnchors')
BEGIN
    CREATE TABLE dbo.SpatialAnchors (
        anchor_id INT PRIMARY KEY,
        anchor_name NVARCHAR(100),
        anchor_vector VECTOR(768),
        selection_method NVARCHAR(50),
        created_at DATETIME2 DEFAULT SYSUTCDATETIME()
    );

    PRINT 'Created SpatialAnchors table';
END
GO

-- ==========================================
-- PART 2: Initialize Default Anchors
-- ==========================================

CREATE OR ALTER PROCEDURE sp_InitializeSpatialAnchors
AS
BEGIN
    SET NOCOUNT ON;

    PRINT 'Initializing spatial anchor points...';

    -- Clear existing anchors
    DELETE FROM dbo.SpatialAnchors;

    -- Select 3 diverse vectors as anchors using maximal distance strategy
    -- Anchor 1: First vector with high variance
    DECLARE @anchor1 VECTOR(768);
    SELECT TOP 1 @anchor1 = embedding_full
    FROM dbo.Embeddings_Production
    WHERE embedding_full IS NOT NULL
    ORDER BY NEWID(); -- Random for initial anchor

    INSERT INTO dbo.SpatialAnchors (anchor_id, anchor_name, anchor_vector, selection_method)
    VALUES (1, 'Primary Anchor', @anchor1, 'random');

    -- Anchor 2: Vector most distant from anchor1
    DECLARE @anchor2 VECTOR(768);
    SELECT TOP 1 @anchor2 = embedding_full
    FROM dbo.Embeddings_Production
    WHERE embedding_full IS NOT NULL
    ORDER BY VECTOR_DISTANCE('euclidean', embedding_full, @anchor1) DESC;

    INSERT INTO dbo.SpatialAnchors (anchor_id, anchor_name, anchor_vector, selection_method)
    VALUES (2, 'Distant Anchor', @anchor2, 'maximal_distance');

    -- Anchor 3: Vector most distant from both anchor1 and anchor2
    DECLARE @anchor3 VECTOR(768);
    SELECT TOP 1 @anchor3 = embedding_full
    FROM dbo.Embeddings_Production
    WHERE embedding_full IS NOT NULL
    ORDER BY
        VECTOR_DISTANCE('euclidean', embedding_full, @anchor1) +
        VECTOR_DISTANCE('euclidean', embedding_full, @anchor2) DESC;

    INSERT INTO dbo.SpatialAnchors (anchor_id, anchor_name, anchor_vector, selection_method)
    VALUES (3, 'Triangulation Anchor', @anchor3, 'maximal_combined_distance');

    PRINT '  ✓ Created 3 spatial anchors using maximal distance selection';

    SELECT
        anchor_id,
        anchor_name,
        selection_method,
        created_at
    FROM dbo.SpatialAnchors
    ORDER BY anchor_id;
END;
GO

-- ==========================================
-- PART 3: Distance-Based Projection
-- ==========================================

CREATE OR ALTER PROCEDURE sp_ComputeSpatialProjection
    @input_vector VECTOR(768)
AS
BEGIN
    SET NOCOUNT ON;

    -- Get anchor vectors
    DECLARE @anchor1 VECTOR(768), @anchor2 VECTOR(768), @anchor3 VECTOR(768);

    SELECT @anchor1 = anchor_vector FROM dbo.SpatialAnchors WHERE anchor_id = 1;
    SELECT @anchor2 = anchor_vector FROM dbo.SpatialAnchors WHERE anchor_id = 2;
    SELECT @anchor3 = anchor_vector FROM dbo.SpatialAnchors WHERE anchor_id = 3;

    -- If no anchors exist, initialize them
    IF @anchor1 IS NULL
    BEGIN
        EXEC sp_InitializeSpatialAnchors;
        SELECT @anchor1 = anchor_vector FROM dbo.SpatialAnchors WHERE anchor_id = 1;
        SELECT @anchor2 = anchor_vector FROM dbo.SpatialAnchors WHERE anchor_id = 2;
        SELECT @anchor3 = anchor_vector FROM dbo.SpatialAnchors WHERE anchor_id = 3;
    END;

    -- Compute distances to each anchor (these become coordinates)
    -- Using euclidean distance to preserve geometric properties
    DECLARE @x FLOAT, @y FLOAT, @z FLOAT;
    SET @x = VECTOR_DISTANCE('euclidean', @input_vector, @anchor1);
    SET @y = VECTOR_DISTANCE('euclidean', @input_vector, @anchor2);
    SET @z = VECTOR_DISTANCE('euclidean', @input_vector, @anchor3);

    -- Optional normalization to keep values in reasonable range
    DECLARE @max_dist FLOAT = 10.0; -- Expected max distance in normalized space
    SET @x = @x / @max_dist;
    SET @y = @y / @max_dist;
    SET @z = @z / @max_dist;

    -- Return as result set
    SELECT @x as x, @y as y, @z as z;
END;
GO

-- ==========================================
-- PART 4: Batch Projection for Existing Data
-- ==========================================

CREATE OR ALTER PROCEDURE sp_RecomputeAllSpatialProjections
AS
BEGIN
    SET NOCOUNT ON;
    SET QUOTED_IDENTIFIER ON;
    SET ANSI_NULLS ON;

    PRINT 'Recomputing spatial projections for all embeddings...';

    -- Ensure anchors are initialized
    EXEC sp_InitializeSpatialAnchors;

    -- Update all embeddings with new projections
    DECLARE @EmbeddingId BIGINT;
    DECLARE @full_vector VECTOR(768);
    DECLARE @x FLOAT, @y FLOAT, @z FLOAT;

    DECLARE cursor_embeddings CURSOR FOR
        SELECT EmbeddingId, embedding_full
        FROM dbo.Embeddings_Production
        WHERE embedding_full IS NOT NULL;

    OPEN cursor_embeddings;
    FETCH NEXT FROM cursor_embeddings INTO @EmbeddingId, @full_vector;

    DECLARE @count INT = 0;

    WHILE @@FETCH_STATUS = 0
    BEGIN
        -- Compute projection using output parameters
        EXEC sp_ComputeSpatialProjection 
            @input_vector = @full_vector,
            @output_x = @x OUTPUT,
            @output_y = @y OUTPUT,
            @output_z = @z OUTPUT;

        -- Update record
        UPDATE dbo.Embeddings_Production
        SET
            SpatialProjX = @x,
            SpatialProjY = @y,
            SpatialProjZ = @z,
            spatial_geometry = geometry::STGeomFromText(
                'POINT(' + CAST(@x AS NVARCHAR(50)) + ' ' +
                          CAST(@y AS NVARCHAR(50)) + ')', 0),
            spatial_coarse = geometry::STGeomFromText(
                'POINT(' + CAST(FLOOR(@x) AS NVARCHAR(50)) + ' ' +
                          CAST(FLOOR(@y) AS NVARCHAR(50)) + ')', 0)
        WHERE EmbeddingId = @EmbeddingId;

        SET @count = @count + 1;

        IF @count % 100 = 0
        BEGIN
            PRINT '  Processed ' + CAST(@count AS VARCHAR(10)) + ' embeddings...';
        END;

        FETCH NEXT FROM cursor_embeddings INTO @EmbeddingId, @full_vector;
    END;

    CLOSE cursor_embeddings;
    DEALLOCATE cursor_embeddings;

    PRINT '  ✓ Recomputed ' + CAST(@count AS VARCHAR(10)) + ' spatial projections';
END;
GO

-- ==========================================
-- PART 5: Projection Quality Analysis
-- ==========================================

CREATE OR ALTER PROCEDURE sp_AnalyzeProjectionQuality
AS
BEGIN
    SET NOCOUNT ON;

    PRINT 'Analyzing spatial projection quality...';
    PRINT '';

    -- 1. Coordinate ranges
    PRINT '1. Coordinate Ranges:';
    SELECT
        'X' as coordinate,
        MIN(SpatialProjX) as min_value,
        MAX(SpatialProjX) as max_value,
        AVG(SpatialProjX) as avg_value,
        STDEV(SpatialProjX) as stdev
    FROM dbo.Embeddings_Production
    WHERE SpatialProjX IS NOT NULL

    UNION ALL

    SELECT
        'Y' as coordinate,
        MIN(SpatialProjY) as min_value,
        MAX(SpatialProjY) as max_value,
        AVG(SpatialProjY) as avg_value,
        STDEV(SpatialProjY) as stdev
    FROM dbo.Embeddings_Production
    WHERE SpatialProjY IS NOT NULL

    UNION ALL

    SELECT
        'Z' as coordinate,
        MIN(SpatialProjZ) as min_value,
        MAX(SpatialProjZ) as max_value,
        AVG(SpatialProjZ) as avg_value,
        STDEV(SpatialProjZ) as stdev
    FROM dbo.Embeddings_Production
    WHERE SpatialProjZ IS NOT NULL;

    PRINT '';
    PRINT '2. Distance Preservation:';

    -- Compare high-dim distances vs spatial distances
    WITH distance_comparison AS (
        SELECT TOP 10
            a.EmbeddingId as id_a,
            b.EmbeddingId as id_b,
            VECTOR_DISTANCE('euclidean', a.embedding_full, b.embedding_full) as highdim_dist,
            a.spatial_geometry.STDistance(b.spatial_geometry) as spatial_dist
        FROM dbo.Embeddings_Production a
        CROSS JOIN dbo.Embeddings_Production b
        WHERE a.EmbeddingId < b.EmbeddingId
            AND a.embedding_full IS NOT NULL
            AND b.embedding_full IS NOT NULL
    )
    SELECT
        AVG(highdim_dist) as avg_highdim_distance,
        AVG(spatial_dist) as avg_spatial_distance,
        COUNT(*) as comparison_count
    FROM distance_comparison;

    PRINT '  ✓ Analysis complete';
END;
GO

PRINT '';
PRINT '============================================================';
PRINT 'SPATIAL PROJECTION SYSTEM CREATED';
PRINT '============================================================';
PRINT 'Procedures available:';
PRINT '  1. sp_InitializeSpatialAnchors - Create anchor points';
PRINT '  2. sp_ComputeSpatialProjection - Project vector to 3D';
PRINT '  3. sp_RecomputeAllSpatialProjections - Batch recompute';
PRINT '  4. sp_AnalyzeProjectionQuality - Quality metrics';
PRINT '';
PRINT 'Usage:';
PRINT '  EXEC sp_InitializeSpatialAnchors;';
PRINT '  EXEC sp_RecomputeAllSpatialProjections;';
PRINT '  EXEC sp_AnalyzeProjectionQuality;';
PRINT '============================================================';
GO
