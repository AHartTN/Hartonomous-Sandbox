-- ==========================================
-- SEMANTIC DEDUPLICATION PROCEDURES
-- ==========================================

CREATE OR ALTER PROCEDURE dbo.sp_CheckSimilarityAboveThreshold
    @query_vector VECTOR(1998),
    @threshold FLOAT,
    @embedding_type NVARCHAR(128) = NULL,
    @model_id INT = NULL
AS
BEGIN
    SET NOCOUNT ON;

    IF @query_vector IS NULL
    BEGIN
        RAISERROR('Query vector cannot be NULL.', 16, 1);
        RETURN;
    END;

    IF @threshold IS NULL OR @threshold < -1.0 OR @threshold > 1.0
    BEGIN
        RAISERROR('Threshold must be between -1.0 and 1.0.', 16, 1);
        RETURN;
    END;

    DECLARE @max_distance FLOAT = 2.0 * (1.0 - @threshold);

    IF @max_distance <= 0
    BEGIN
        RAISERROR('Threshold is too high for cosine similarity search.', 16, 1);
        RETURN;
    END;

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
      AND (@model_id IS NULL OR ae.ModelId = @model_id)
    ORDER BY VECTOR_DISTANCE('cosine', ae.EmbeddingVector, @query_vector);
END;
GO

PRINT 'Created sp_CheckSimilarityAboveThreshold for semantic deduplication';
GO