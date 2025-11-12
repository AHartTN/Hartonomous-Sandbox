CREATE PROCEDURE dbo.sp_MultiResolutionSearch
    @query_x FLOAT,
    @query_y FLOAT,
    @query_z FLOAT,
    @coarse_candidates INT = 1000,
    @fine_candidates INT = 100,
    @final_top_k INT = 10
AS
BEGIN
    SET NOCOUNT ON;

    PRINT 'MULTI-RESOLUTION SEARCH: Coarse → Fine → Exact';
    PRINT '  Strategy: 3-stage funnel for billion-scale performance';

    DECLARE @query_wkt NVARCHAR(200) = CONCAT('POINT (', @query_x, ' ', @query_y, ' ', @query_z, ')');
    DECLARE @query_pt GEOMETRY = geometry::STGeomFromText(@query_wkt, 0);

    DECLARE @coarse_results TABLE (AtomEmbeddingId BIGINT PRIMARY KEY);

    INSERT INTO @coarse_results (AtomEmbeddingId)
    SELECT TOP (@coarse_candidates) ae.AtomEmbeddingId
    FROM dbo.AtomEmbeddings AS ae
    WHERE ae.SpatialCoarse IS NOT NULL
    ORDER BY ae.SpatialCoarse.STDistance(@query_pt);

    DECLARE @coarse_count INT = @@ROWCOUNT;
    PRINT '  Stage 1: ' + CAST(@coarse_count AS NVARCHAR(10)) + ' coarse candidates';

    DECLARE @fine_results TABLE (AtomEmbeddingId BIGINT PRIMARY KEY);

    INSERT INTO @fine_results (AtomEmbeddingId)
    SELECT TOP (@fine_candidates) ae.AtomEmbeddingId
    FROM dbo.AtomEmbeddings AS ae
    INNER JOIN @coarse_results AS cr ON cr.AtomEmbeddingId = ae.AtomEmbeddingId
    WHERE ae.SpatialGeometry IS NOT NULL
    ORDER BY ae.SpatialGeometry.STDistance(@query_pt);

    DECLARE @fine_count INT = @@ROWCOUNT;
    PRINT '  Stage 2: ' + CAST(@fine_count AS NVARCHAR(10)) + ' fine candidates';

    SELECT TOP (@final_top_k)
        ae.AtomEmbeddingId,
        ae.AtomId,
        a.Modality,
        a.Subtype,
        a.SourceType,
        a.SourceUri,
        a.CanonicalText,
        ae.EmbeddingType,
        ae.ModelId,
        ae.SpatialGeometry.STDistance(@query_pt) AS SpatialDistance,
        ae.SpatialCoarse.STDistance(@query_pt) AS CoarseDistance
    FROM dbo.AtomEmbeddings AS ae
    INNER JOIN @fine_results AS fr ON fr.AtomEmbeddingId = ae.AtomEmbeddingId
    INNER JOIN dbo.Atoms AS a ON a.AtomId = ae.AtomId
    ORDER BY SpatialDistance ASC;

    PRINT '  Stage 3: Top ' + CAST(@final_top_k AS NVARCHAR(10)) + ' results';
    PRINT '✓ Multi-resolution search complete';
END