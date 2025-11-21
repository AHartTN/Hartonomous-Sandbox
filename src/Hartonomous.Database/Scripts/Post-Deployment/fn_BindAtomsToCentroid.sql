-- =============================================
-- PHASE 7.3: fn_BindAtomsToCentroid
-- Table-valued function for sp_DiscoverAndBindConcepts
-- Finds atoms similar to concept centroid
-- =============================================

CREATE FUNCTION dbo.fn_BindAtomsToCentroid (
    @CentroidVector VARBINARY(MAX),
    @SimilarityThreshold FLOAT,
    @TenantId INT
)
RETURNS TABLE
AS
RETURN (
    SELECT 
        ae.AtomId,
        1.0 - VECTOR_DISTANCE('cosine', ae.EmbeddingVector, CAST(@CentroidVector AS VECTOR(1536))) AS Similarity
    FROM dbo.AtomEmbedding ae
    INNER JOIN dbo.Atom a ON ae.AtomId = a.AtomId
    WHERE 
        -- Multi-tenancy filter
        (a.TenantId = @TenantId OR EXISTS (SELECT 1 FROM dbo.TenantAtoms ta WHERE ta.AtomId = a.AtomId AND ta.TenantId = @TenantId))
        -- Similarity threshold
        AND (1.0 - VECTOR_DISTANCE('cosine', ae.EmbeddingVector, CAST(@CentroidVector AS VECTOR(1536)))) >= @SimilarityThreshold
        -- Valid vectors only
        AND ae.EmbeddingVector IS NOT NULL
);
GO
