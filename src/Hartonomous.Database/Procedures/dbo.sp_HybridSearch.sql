CREATE PROCEDURE dbo.sp_HybridSearch
    @query_vector VECTOR(1998),
    @query_dimension INT,
    @query_spatial_x FLOAT,
    @query_spatial_y FLOAT,
    @query_spatial_z FLOAT,
    @spatial_candidates INT = 100,
    @final_top_k INT = 10,
    @distance_metric NVARCHAR(20) = 'cosine',
    @embedding_type NVARCHAR(128) = NULL,
    @ModelId INT = NULL,
    @srid INT = 0
AS
BEGIN
    SET NOCOUNT ON;

    PRINT 'HYBRID SEARCH: Spatial filter + Vector rerank';

    DECLARE @wkt NVARCHAR(200) = CONCAT('POINT (', @query_spatial_x, ' ', @query_spatial_y, ' ', @query_spatial_z, ')');
    DECLARE @query_point GEOMETRY = geometry::STGeomFromText(@wkt, @srid);

    DECLARE @candidates TABLE (
        AtomEmbeddingId BIGINT PRIMARY KEY,
        SpatialDistance FLOAT
    );

    INSERT INTO @candidates (AtomEmbeddingId, SpatialDistance)
    SELECT TOP (@spatial_candidates)
        ae.AtomEmbeddingId,
        ae.SpatialGeometry.STDistance(@query_point) AS spatial_distance
    FROM dbo.AtomEmbeddings AS ae
    WHERE ae.SpatialGeometry IS NOT NULL
      AND ae.Dimension = @query_dimension
      AND (@embedding_type IS NULL OR ae.EmbeddingType = @embedding_type)
      AND (@ModelId IS NULL OR ae.ModelId = @ModelId)
    ORDER BY ae.SpatialGeometry.STDistance(@query_point);

    SELECT TOP (@final_top_k)
        ae.AtomEmbeddingId,
        ae.AtomId,
        a.Modality,
        a.Subtype,
        a.SourceUri,
        a.SourceType,
        ae.EmbeddingType,
        ae.ModelId,
        VECTOR_DISTANCE(@distance_metric, ae.EmbeddingVector, @query_vector) AS exact_distance,
        c.SpatialDistance AS spatial_distance
    FROM dbo.AtomEmbeddings AS ae
    INNER JOIN @candidates AS c ON c.AtomEmbeddingId = ae.AtomEmbeddingId
    INNER JOIN dbo.Atoms AS a ON a.AtomId = ae.AtomId
    WHERE ae.EmbeddingVector IS NOT NULL
      AND ae.Dimension = @query_dimension
    ORDER BY VECTOR_DISTANCE(@distance_metric, ae.EmbeddingVector, @query_vector);

    PRINT 'Hybrid search complete: Spatial O(log n) + Vector O(k)';
END;