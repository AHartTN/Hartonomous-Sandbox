CREATE OR ALTER PROCEDURE dbo.sp_CheckSimilarityAboveThreshold
    @query_vector VECTOR(1998),
    @threshold FLOAT,
    @embedding_type NVARCHAR(128) = NULL,
    @ModelId INT = NULL
AS
BEGIN
    SET NOCOUNT ON;

    IF @query_vector IS NULL
    BEGIN
        THROW 50030, 'Query vector cannot be NULL.', 1;
    END

    IF @threshold IS NULL OR @threshold < -1.0 OR @threshold > 1.0
    BEGIN
        THROW 50031, 'Threshold must be between -1.0 and 1.0.', 1;
    END

    IF @max_distance <= 0
    BEGIN
        THROW 50032, 'Threshold is too high for cosine similarity search.', 1;
    END

    SELECT TOP (1)
        ae.AtomEmbeddingId,
        ae.AtomId,
        a.ContentHash,
        a.Modality,
        a.Subtype,
        a.SourceType,
        a.SourceUri,
        a.CanonicalText,
        ae.EmbeddingType,
        ae.ModelId,
        ae.Dimension,
        VECTOR_DISTANCE('cosine', ae.EmbeddingVector, @query_vector) AS CosineDistance,
        1.0 - VECTOR_DISTANCE('cosine', ae.EmbeddingVector, @query_vector) AS SimilarityScore,
        ae.EmbeddingVector,
        ae.SpatialGeometry,
        ae.SpatialCoarse,
        ae.Metadata,
        a.ReferenceCount,
        a.CreatedAt,
        a.UpdatedAt
    FROM dbo.AtomEmbeddings AS ae
    INNER JOIN dbo.Atoms AS a ON a.AtomId = ae.AtomId
    WHERE ae.EmbeddingVector IS NOT NULL
      AND VECTOR_DISTANCE('cosine', ae.EmbeddingVector, @query_vector) < @max_distance
      AND (@embedding_type IS NULL OR ae.EmbeddingType = @embedding_type)
      AND (@ModelId IS NULL OR ae.ModelId = @ModelId)
    ORDER BY VECTOR_DISTANCE('cosine', ae.EmbeddingVector, @query_vector);
END
