SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;
GO

PRINT '============================================================';
PRINT 'SPATIAL PROJECTION SYSTEM';
PRINT 'AtomEmbeddings distance-based spatial projection';
PRINT '============================================================';
GO

IF OBJECT_ID('dbo.SpatialAnchors', 'U') IS NULL
BEGIN
    PRINT 'Creating dbo.SpatialAnchors anchor table...';

    CREATE TABLE dbo.SpatialAnchors
    (
        AnchorId INT NOT NULL PRIMARY KEY,
        AnchorName NVARCHAR(128) NOT NULL,
        AnchorVector VECTOR(1998) NOT NULL,
        SelectionMethod NVARCHAR(64) NOT NULL,
        CreatedAt DATETIME2(3) NOT NULL CONSTRAINT DF_SpatialAnchors_CreatedAt DEFAULT SYSUTCDATETIME()
    );

    PRINT '  ✓ SpatialAnchors table created.';
END;
GO

CREATE OR ALTER PROCEDURE dbo.sp_InitializeSpatialAnchors
AS
BEGIN
    SET NOCOUNT ON;

    PRINT 'Initializing spatial anchor points...';

    DELETE FROM dbo.SpatialAnchors;

    DECLARE @available INT = (
        SELECT COUNT(*)
        FROM dbo.AtomEmbeddings
        WHERE EmbeddingVector IS NOT NULL
    );

    IF @available < 3
    BEGIN
        RAISERROR('Spatial anchor initialization requires at least three AtomEmbeddings rows with stored vectors.', 16, 1);
        RETURN;
    END;

    DECLARE @anchor1Id BIGINT, @anchor2Id BIGINT, @anchor3Id BIGINT;
    DECLARE @anchor1 VECTOR(1998), @anchor2 VECTOR(1998), @anchor3 VECTOR(1998);

    SELECT TOP (1)
        @anchor1Id = AtomEmbeddingId,
        @anchor1 = EmbeddingVector
    FROM dbo.AtomEmbeddings
    WHERE EmbeddingVector IS NOT NULL
    ORDER BY NEWID();

    INSERT INTO dbo.SpatialAnchors (AnchorId, AnchorName, AnchorVector, SelectionMethod)
    VALUES (1, 'Primary Anchor', @anchor1, 'random');

    SELECT TOP (1)
        @anchor2Id = AtomEmbeddingId,
        @anchor2 = EmbeddingVector
    FROM dbo.AtomEmbeddings
    WHERE EmbeddingVector IS NOT NULL
      AND AtomEmbeddingId <> @anchor1Id
    ORDER BY VECTOR_DISTANCE('euclidean', EmbeddingVector, @anchor1) DESC;

    IF @anchor2Id IS NULL
    BEGIN
        RAISERROR('Unable to select a second spatial anchor.', 16, 1);
        RETURN;
    END;

    INSERT INTO dbo.SpatialAnchors (AnchorId, AnchorName, AnchorVector, SelectionMethod)
    VALUES (2, 'Distant Anchor', @anchor2, 'maximal_distance');

    SELECT TOP (1)
        @anchor3Id = AtomEmbeddingId,
        @anchor3 = EmbeddingVector
    FROM dbo.AtomEmbeddings
    WHERE EmbeddingVector IS NOT NULL
      AND AtomEmbeddingId NOT IN (@anchor1Id, @anchor2Id)
    ORDER BY VECTOR_DISTANCE('euclidean', EmbeddingVector, @anchor1) +
             VECTOR_DISTANCE('euclidean', EmbeddingVector, @anchor2) DESC;

    IF @anchor3Id IS NULL
    BEGIN
        RAISERROR('Unable to select a third spatial anchor.', 16, 1);
        RETURN;
    END;

    INSERT INTO dbo.SpatialAnchors (AnchorId, AnchorName, AnchorVector, SelectionMethod)
    VALUES (3, 'Triangulation Anchor', @anchor3, 'maximal_combined_distance');

    PRINT '  ✓ Spatial anchors refreshed (3 anchors).';

    SELECT
        AnchorId,
        AnchorName,
        SelectionMethod,
        CreatedAt
    FROM dbo.SpatialAnchors
    ORDER BY AnchorId;
END;
GO

CREATE OR ALTER PROCEDURE dbo.sp_ComputeSpatialProjection
    @input_vector VECTOR(1998),
    @input_dimension INT,
    @output_x FLOAT OUTPUT,
    @output_y FLOAT OUTPUT,
    @output_z FLOAT OUTPUT
AS
BEGIN
    SET NOCOUNT ON;

    IF (@input_dimension <= 0 OR @input_dimension > 1998)
    BEGIN
        RAISERROR('Input dimension %d is outside the supported range (1-1998).', 16, 1, @input_dimension);
        RETURN;
    END;

    DECLARE @anchor1 VECTOR(1998), @anchor2 VECTOR(1998), @anchor3 VECTOR(1998);

    SELECT @anchor1 = AnchorVector FROM dbo.SpatialAnchors WHERE AnchorId = 1;
    SELECT @anchor2 = AnchorVector FROM dbo.SpatialAnchors WHERE AnchorId = 2;
    SELECT @anchor3 = AnchorVector FROM dbo.SpatialAnchors WHERE AnchorId = 3;

    IF @anchor1 IS NULL OR @anchor2 IS NULL OR @anchor3 IS NULL
    BEGIN
        EXEC dbo.sp_InitializeSpatialAnchors;
        SELECT @anchor1 = AnchorVector FROM dbo.SpatialAnchors WHERE AnchorId = 1;
        SELECT @anchor2 = AnchorVector FROM dbo.SpatialAnchors WHERE AnchorId = 2;
        SELECT @anchor3 = AnchorVector FROM dbo.SpatialAnchors WHERE AnchorId = 3;

        IF @anchor1 IS NULL OR @anchor2 IS NULL OR @anchor3 IS NULL
        BEGIN
            RAISERROR('Spatial anchors are not available.', 16, 1);
            RETURN;
        END;
    END;

    DECLARE @distance1 FLOAT = VECTOR_DISTANCE('euclidean', @input_vector, @anchor1);
    DECLARE @distance2 FLOAT = VECTOR_DISTANCE('euclidean', @input_vector, @anchor2);
    DECLARE @distance3 FLOAT = VECTOR_DISTANCE('euclidean', @input_vector, @anchor3);

    DECLARE @a12 FLOAT = VECTOR_DISTANCE('euclidean', @anchor1, @anchor2);
    DECLARE @a13 FLOAT = VECTOR_DISTANCE('euclidean', @anchor1, @anchor3);
    DECLARE @a23 FLOAT = VECTOR_DISTANCE('euclidean', @anchor2, @anchor3);

    DECLARE @scale FLOAT = (
        SELECT MAX(v)
        FROM (VALUES (@a12), (@a13), (@a23), (1.0)) AS distances(v)
    );

    IF @scale IS NULL OR @scale = 0
    BEGIN
        SET @scale = 1.0;
    END;

    SET @output_x = @distance1 / @scale;
    SET @output_y = @distance2 / @scale;
    SET @output_z = @distance3 / @scale;

    SELECT @output_x AS x, @output_y AS y, @output_z AS z;
END;
GO

CREATE OR ALTER PROCEDURE dbo.sp_RecomputeAllSpatialProjections
AS
BEGIN
    SET NOCOUNT ON;

    PRINT 'Recomputing spatial projections for AtomEmbeddings...';

    EXEC dbo.sp_InitializeSpatialAnchors;

    DECLARE @embeddingId BIGINT;
    DECLARE @vector VECTOR(1998);
    DECLARE @dimension INT;
    DECLARE @x FLOAT, @y FLOAT, @z FLOAT;

    DECLARE embedding_cursor CURSOR LOCAL FAST_FORWARD FOR
        SELECT AtomEmbeddingId, EmbeddingVector, Dimension
        FROM dbo.AtomEmbeddings
        WHERE EmbeddingVector IS NOT NULL;

    OPEN embedding_cursor;
    FETCH NEXT FROM embedding_cursor INTO @embeddingId, @vector, @dimension;

    DECLARE @processed INT = 0;

    WHILE @@FETCH_STATUS = 0
    BEGIN
        IF (@dimension <= 0 OR @dimension > 1998)
        BEGIN
            SET @dimension = 1998;
        END;

        EXEC dbo.sp_ComputeSpatialProjection
            @input_vector = @vector,
            @input_dimension = @dimension,
            @output_x = @x OUTPUT,
            @output_y = @y OUTPUT,
            @output_z = @z OUTPUT;

        DECLARE @geometryWkt NVARCHAR(200) =
            'POINT (' + CONVERT(NVARCHAR(50), @x) + ' ' +
                          CONVERT(NVARCHAR(50), @y) + ' ' +
                          CONVERT(NVARCHAR(50), @z) + ')';

        DECLARE @coarseWkt NVARCHAR(200) =
            'POINT (' + CONVERT(NVARCHAR(50), FLOOR(@x)) + ' ' +
                          CONVERT(NVARCHAR(50), FLOOR(@y)) + ' ' +
                          CONVERT(NVARCHAR(50), FLOOR(@z)) + ')';

        UPDATE dbo.AtomEmbeddings
        SET
            SpatialGeometry = geometry::STGeomFromText(@geometryWkt, 0),
            SpatialCoarse = geometry::STGeomFromText(@coarseWkt, 0)
        WHERE AtomEmbeddingId = @embeddingId;

        SET @processed += 1;

        IF @processed % 100 = 0
        BEGIN
            PRINT '  Processed ' + CAST(@processed AS NVARCHAR(10)) + ' embeddings...';
        END;

        FETCH NEXT FROM embedding_cursor INTO @embeddingId, @vector, @dimension;
    END;

    CLOSE embedding_cursor;
    DEALLOCATE embedding_cursor;

    PRINT '  ✓ Spatial projections refreshed for ' + CAST(@processed AS NVARCHAR(10)) + ' embeddings.';
END;
GO

CREATE OR ALTER PROCEDURE dbo.sp_AnalyzeProjectionQuality
AS
BEGIN
    SET NOCOUNT ON;

    PRINT 'Analyzing spatial projection quality...';
    PRINT '';

    PRINT '1. Coordinate Ranges:';
    SELECT
        'X' AS coordinate,
        MIN(SpatialGeometry.STX) AS min_value,
        MAX(SpatialGeometry.STX) AS max_value,
        AVG(SpatialGeometry.STX) AS avg_value,
        STDEV(SpatialGeometry.STX) AS stdev
    FROM dbo.AtomEmbeddings
    WHERE SpatialGeometry IS NOT NULL

    UNION ALL

    SELECT
        'Y' AS coordinate,
        MIN(SpatialGeometry.STY) AS min_value,
        MAX(SpatialGeometry.STY) AS max_value,
        AVG(SpatialGeometry.STY) AS avg_value,
        STDEV(SpatialGeometry.STY) AS stdev
    FROM dbo.AtomEmbeddings
    WHERE SpatialGeometry IS NOT NULL

    UNION ALL

    SELECT
        'Z' AS coordinate,
        MIN(SpatialGeometry.Z) AS min_value,
        MAX(SpatialGeometry.Z) AS max_value,
        AVG(CAST(SpatialGeometry.Z AS FLOAT)) AS avg_value,
        STDEV(CAST(SpatialGeometry.Z AS FLOAT)) AS stdev
    FROM dbo.AtomEmbeddings
    WHERE SpatialGeometry IS NOT NULL;

    PRINT '';
    PRINT '2. Distance Preservation:';

    WITH distance_comparison AS (
        SELECT TOP (50)
            a.AtomEmbeddingId AS id_a,
            b.AtomEmbeddingId AS id_b,
            VECTOR_DISTANCE('cosine', a.EmbeddingVector, b.EmbeddingVector) AS vector_dist,
            a.SpatialGeometry.STDistance(b.SpatialGeometry) AS spatial_dist
        FROM dbo.AtomEmbeddings a
        JOIN dbo.AtomEmbeddings b ON a.AtomEmbeddingId < b.AtomEmbeddingId
        WHERE a.EmbeddingVector IS NOT NULL
          AND b.EmbeddingVector IS NOT NULL
          AND a.SpatialGeometry IS NOT NULL
          AND b.SpatialGeometry IS NOT NULL
        ORDER BY NEWID()
    )
    SELECT
        AVG(vector_dist) AS avg_vector_distance,
        AVG(spatial_dist) AS avg_spatial_distance,
        COUNT(*) AS comparison_count
    FROM distance_comparison;

    PRINT '  ✓ Analysis complete';
END;
GO

PRINT '';
PRINT '============================================================';
PRINT 'SPATIAL PROJECTION SYSTEM UPDATED';
PRINT '============================================================';
PRINT 'Procedures available:';
PRINT '  1. dbo.sp_InitializeSpatialAnchors';
PRINT '  2. dbo.sp_ComputeSpatialProjection';
PRINT '  3. dbo.sp_RecomputeAllSpatialProjections';
PRINT '  4. dbo.sp_AnalyzeProjectionQuality';
PRINT '';
PRINT 'Usage:';
PRINT '  EXEC dbo.sp_InitializeSpatialAnchors;';
PRINT '  EXEC dbo.sp_RecomputeAllSpatialProjections;';
PRINT '  EXEC dbo.sp_AnalyzeProjectionQuality;';
PRINT '============================================================';
GO
