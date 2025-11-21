-- =====================================================
-- fn_FindNearestAtoms
-- O(log N) + O(K) Three-Stage Similarity Query
-- =====================================================
-- Converted from stored procedure to inline table-valued function
-- for direct use in SELECT statements
--
-- CORE INNOVATION: Spatial R-Tree index IS the ANN algorithm
-- Stage 1: O(log N) spatial index seek
-- Stage 2: Hilbert clustering for cache locality
-- Stage 3: O(K) SIMD vector refinement

CREATE FUNCTION dbo.fn_FindNearestAtoms
(
    @queryVector VARBINARY(MAX),
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
    WITH QueryProjection AS (
        -- Project query vector to 3D
        SELECT 
            dbo.fn_ProjectTo3D(@queryVector) AS QueryGeometry,
            CASE 
                WHEN @useHilbertClustering = 1 
                THEN dbo.clr_ComputeHilbertValue(dbo.fn_ProjectTo3D(@queryVector), 21)
                ELSE NULL
            END AS QueryHilbert
    ),
    SpatialCandidates AS (
        SELECT TOP (@spatialPoolSize)
            ae.AtomId,
            ae.EmbeddingVector,
            ae.SpatialGeometry,
            ae.HilbertValue,
            ae.SpatialGeometry.STDistance(qp.QueryGeometry) AS SpatialDistance,
            a.Modality,
            a.Subtype,
            a.CanonicalText,
            a.ContentHash,
            qp.QueryHilbert
        FROM dbo.AtomEmbedding ae WITH (INDEX(IX_AtomEmbedding_SpatialGeometry))
        INNER JOIN dbo.Atom a ON ae.AtomId = a.AtomId
        CROSS JOIN QueryProjection qp
        WHERE ae.TenantId = @tenantId
            AND ae.SpatialGeometry IS NOT NULL
            AND ae.EmbeddingVector IS NOT NULL
            AND ae.SpatialGeometry.STIntersects(qp.QueryGeometry.STBuffer(10.0)) = 1
            AND (@modalityFilter IS NULL OR a.Modality = @modalityFilter)
        ORDER BY ae.SpatialGeometry.STDistance(qp.QueryGeometry)
    ),
    HilbertCandidates AS (
        SELECT
            sc.AtomId,
            sc.EmbeddingVector,
            sc.SpatialDistance,
            sc.HilbertValue,
            sc.Modality,
            sc.Subtype,
            sc.CanonicalText,
            sc.ContentHash,
            CASE 
                WHEN @useHilbertClustering = 1 AND sc.QueryHilbert IS NOT NULL
                THEN ABS(CAST(sc.HilbertValue AS BIGINT) - sc.QueryHilbert)
                ELSE 0
            END AS HilbertDistance
        FROM SpatialCandidates sc
        WHERE sc.HilbertValue IS NOT NULL OR @useHilbertClustering = 0
    ),
    RankedCandidates AS (
        SELECT
            hc.AtomId,
            hc.Modality,
            hc.Subtype,
            hc.CanonicalText,
            hc.ContentHash,
            hc.SpatialDistance,
            hc.HilbertValue,
            hc.HilbertDistance,
            dbo.clr_VectorCosineSimilarity(@queryVector, hc.EmbeddingVector) AS CosineSimilarity,
            (0.7 * dbo.clr_VectorCosineSimilarity(@queryVector, hc.EmbeddingVector)) +
            (0.3 * (1.0 / (1.0 + hc.SpatialDistance))) AS BlendedScore
        FROM HilbertCandidates hc
        WHERE hc.EmbeddingVector IS NOT NULL
    )
    SELECT TOP (@topK)
        rc.AtomId,
        rc.ContentHash,
        rc.CanonicalText,
        rc.Modality,
        rc.Subtype,
        rc.CosineSimilarity AS Score,
        rc.BlendedScore,
        rc.SpatialDistance,
        rc.HilbertValue
    FROM RankedCandidates rc
    ORDER BY rc.CosineSimilarity DESC
);
GO

GRANT SELECT ON dbo.fn_FindNearestAtoms TO PUBLIC;
GO
