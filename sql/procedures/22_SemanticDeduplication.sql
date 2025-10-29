-- ==========================================
-- SEMANTIC DEDUPLICATION PROCEDURES
-- ==========================================

CREATE OR ALTER PROCEDURE sp_CheckSimilarityAboveThreshold
    @query_vector VECTOR(768),
    @threshold FLOAT
AS
BEGIN
    SET NOCOUNT ON;

    -- VECTOR_DISTANCE('cosine') returns cosine distance (0=identical, 2=opposite)
    -- Cosine similarity = 1 - (distance/2)
    -- We want similarity > threshold, so: 1 - (distance/2) > threshold
    -- Which means: distance < 2 * (1 - threshold)

    SELECT TOP 1
        EmbeddingId,
        SourceText,
        SourceType,
        embedding_full,
        EmbeddingModel,
        SpatialProjX,
        SpatialProjY,
        SpatialProjZ,
        spatial_geometry,
        spatial_coarse,
        Dimension,
        ContentHash,
        AccessCount,
        LastAccessed,
        CreatedAt
    FROM dbo.Embeddings_Production
    WHERE embedding_full IS NOT NULL
        AND VECTOR_DISTANCE('cosine', embedding_full, @query_vector) < (2.0 * (1.0 - @threshold))
    ORDER BY VECTOR_DISTANCE('cosine', embedding_full, @query_vector);
END;
GO

PRINT 'Created sp_CheckSimilarityAboveThreshold for semantic deduplication';
GO