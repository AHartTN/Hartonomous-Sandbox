-- =====================================================
-- fn_FindNearestAtoms
-- O(log N) + O(K) Three-Stage Similarity Query
-- =====================================================
-- Simplified to use native VECTOR type and VECTOR_DISTANCE
-- Uses SQL Server 2025 VECTOR(1998) type

CREATE FUNCTION dbo.fn_FindNearestAtoms
(
    @queryVector VECTOR(1998),
    @topK INT,
    @spatialPoolSize INT,
    @tenantId INT,
    @modalityFilter NVARCHAR(50),
    @useHilbertClustering BIT
)
RETURNS TABLE
AS
RETURN
(
    WITH SpatialCandidates AS (
        SELECT TOP (@spatialPoolSize)
            ae.AtomId,
            ae.EmbeddingVector,
            ae.SpatialKey,
            ae.HilbertValue,
            a.Modality,
            a.Subtype,
            a.CanonicalText,
            a.ContentHash
        FROM dbo.AtomEmbedding ae WITH (INDEX(SIX_AtomEmbedding_SpatialKey))
        INNER JOIN dbo.Atom a ON ae.AtomId = a.AtomId
        WHERE ae.TenantId = @tenantId
            AND ae.EmbeddingVector IS NOT NULL
            AND (@modalityFilter IS NULL OR a.Modality = @modalityFilter)
        ORDER BY VECTOR_DISTANCE('cosine', @queryVector, ae.EmbeddingVector)
    ),
    RankedCandidates AS (
        SELECT
            sc.AtomId,
            sc.Modality,
            sc.Subtype,
            sc.CanonicalText,
            sc.ContentHash,
            sc.HilbertValue,
            VECTOR_DISTANCE('cosine', @queryVector, sc.EmbeddingVector) AS CosineSimilarity
        FROM SpatialCandidates sc
    )
    SELECT TOP (@topK)
        rc.AtomId,
        rc.ContentHash,
        rc.CanonicalText,
        rc.Modality,
        rc.Subtype,
        rc.CosineSimilarity AS Score,
        rc.HilbertValue
    FROM RankedCandidates rc
    ORDER BY rc.CosineSimilarity ASC -- cosine distance: lower is better
);
GO

GRANT SELECT ON dbo.fn_FindNearestAtoms TO PUBLIC;
GO
