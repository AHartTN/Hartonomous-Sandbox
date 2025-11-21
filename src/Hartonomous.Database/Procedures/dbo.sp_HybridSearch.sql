CREATE PROCEDURE dbo.sp_HybridSearch
    @query_vector VECTOR(1536),
    @query_dimension INT,
    @query_spatial_x FLOAT,
    @query_spatial_y FLOAT,
    @query_spatial_z FLOAT,
    @spatial_candidates INT = 100,
    @final_top_k INT = 10,
    @distance_metric NVARCHAR(20) = 'cosine',
    @embedding_type NVARCHAR(128) = NULL,
    @ModelId INT = NULL,
    @srid INT = 0,
    @TenantId INT -- V3: Added for security
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

    -- V3: Added tenant filtering to the candidate selection phase
    INSERT INTO @candidates (AtomEmbeddingId, SpatialDistance)
    SELECT TOP (@spatial_candidates)
        ae.AtomEmbeddingId,
        ae.SpatialKey.STDistance(@query_point) AS spatial_distance
    FROM dbo.AtomEmbedding AS ae
    INNER JOIN dbo.Atom AS a ON a.AtomId = ae.AtomId
    WHERE 
        -- V3: TENANCY MODEL
        (a.TenantId = @TenantId OR EXISTS (SELECT 1 FROM dbo.TenantAtoms ta WHERE ta.AtomId = a.AtomId AND ta.TenantId = @TenantId))
        AND ae.SpatialKey IS NOT NULL
        AND ae.Dimension = @query_dimension
        AND (@embedding_type IS NULL OR ae.EmbeddingType = @embedding_type)
        AND (@ModelId IS NULL OR ae.ModelId = @ModelId)
    ORDER BY ae.SpatialKey.STDistance(@query_point);

    -- V3: Added tenant filtering to the final reranking and removed deleted columns
    SELECT TOP (@final_top_k)
        ae.AtomEmbeddingId,
        ae.AtomId,
        a.Modality,
        a.Subtype,
        ae.EmbeddingType,
        ae.ModelId,
        VECTOR_DISTANCE(@distance_metric, ae.EmbeddingVector, @query_vector) AS exact_distance,
        c.SpatialDistance AS spatial_distance
    FROM dbo.AtomEmbedding AS ae
    INNER JOIN @candidates AS c ON c.AtomEmbeddingId = ae.AtomEmbeddingId
    INNER JOIN dbo.Atom AS a ON a.AtomId = ae.AtomId
    WHERE 
        -- V3: TENANCY MODEL (redundant check, but safe)
        (a.TenantId = @TenantId OR EXISTS (SELECT 1 FROM dbo.TenantAtoms ta WHERE ta.AtomId = a.AtomId AND ta.TenantId = @TenantId))
        AND ae.EmbeddingVector IS NOT NULL
        AND ae.Dimension = @query_dimension
    ORDER BY VECTOR_DISTANCE(@distance_metric, ae.EmbeddingVector, @query_vector);

    PRINT 'Hybrid search complete: Spatial O(log n) + Vector O(k)';
END;
