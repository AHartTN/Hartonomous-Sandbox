CREATE PROCEDURE dbo.sp_ExactVectorSearch
    @query_vector VECTOR(1998),
    @top_k INT = 10,
    @TenantId INT, -- V3: Added for security
    @distance_metric NVARCHAR(20) = 'cosine',
    @embedding_type NVARCHAR(128) = NULL,
    @ModelId INT = NULL
AS
BEGIN
    SET NOCOUNT ON;

    SELECT TOP (@top_k)
        ae.AtomEmbeddingId,
        ae.AtomId,
        a.Modality,
        a.Subtype,
        ae.EmbeddingType,
        ae.ModelId, 
        ae.SpatialKey.STDimension() AS Dimension,
        VECTOR_DISTANCE(@distance_metric, ae.EmbeddingVector, @query_vector) AS distance,
        1.0 - VECTOR_DISTANCE(@distance_metric, ae.EmbeddingVector, @query_vector) AS similarity,
        ae.CreatedAt
    FROM dbo.AtomEmbeddings AS ae
    INNER JOIN dbo.Atoms AS a ON a.AtomId = ae.AtomId
    WHERE 
        -- V3: TENANCY MODEL
        (a.TenantId = @TenantId OR EXISTS (SELECT 1 FROM dbo.TenantAtoms ta WHERE ta.AtomId = a.AtomId AND ta.TenantId = @TenantId))
        AND ae.EmbeddingVector IS NOT NULL
        AND (@embedding_type IS NULL OR ae.EmbeddingType = @embedding_type)
        AND (@ModelId IS NULL OR ae.ModelId = @ModelId)
    ORDER BY VECTOR_DISTANCE(@distance_metric, ae.EmbeddingVector, @query_vector);
END
