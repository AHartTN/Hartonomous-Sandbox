CREATE PROCEDURE dbo.sp_ExactVectorSearch
    @query_vector VECTOR(1998),
    @top_k INT = 10,
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
        a.SourceUri,
        a.SourceType,
        a.CanonicalText,
        ae.EmbeddingType,
        ae.ModelId, ae.SpatialKey.STDimension() AS Dimension,
        VECTOR_DISTANCE(@distance_metric, ae.SpatialKey, @query_vector) AS distance,
        1.0 - VECTOR_DISTANCE(@distance_metric, ae.SpatialKey, @query_vector) AS similarity,
        ae.CreatedAt
    FROM dbo.AtomEmbeddings AS ae
    INNER JOIN dbo.Atoms AS a ON a.AtomId = ae.AtomId
    WHERE ae.SpatialKey IS NOT NULL
      AND (@embedding_type IS NULL OR ae.EmbeddingType = @embedding_type)
      AND (@ModelId IS NULL OR ae.ModelId = @ModelId)
    ORDER BY VECTOR_DISTANCE(@distance_metric, ae.SpatialKey, @query_vector);
END