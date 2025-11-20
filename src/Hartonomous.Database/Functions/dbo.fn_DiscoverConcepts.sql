-- =============================================
-- Function: fn_DiscoverConcepts
-- Description: Discovers semantic concepts using DBSCAN clustering
-- Returns table of concept centroids with member counts
-- =============================================
CREATE FUNCTION dbo.fn_DiscoverConcepts(
    @min_cluster_size INT,
    @similarity_threshold FLOAT,
    @tenant_id INT
)
RETURNS TABLE
AS
RETURN
(
    WITH EmbeddingClusters AS (
        SELECT 
            ae1.AtomEmbeddingId,
            ae1.AtomId,
            ae1.SpatialKey,
            COUNT(ae2.AtomEmbeddingId) AS NeighborCount
        FROM dbo.AtomEmbedding ae1
        CROSS APPLY (
            SELECT ae2.AtomEmbeddingId
            FROM dbo.AtomEmbedding ae2
            WHERE ae2.AtomEmbeddingId != ae1.AtomEmbeddingId
              AND ae2.TenantId = ae1.TenantId
              AND ae1.SpatialKey.STDistance(ae2.SpatialKey) < @similarity_threshold
        ) ae2
        WHERE ae1.TenantId = @tenant_id
        GROUP BY ae1.AtomEmbeddingId, ae1.AtomId, ae1.SpatialKey
        HAVING COUNT(ae2.AtomEmbeddingId) >= @min_cluster_size
    ),
    ConceptCentroids AS (
        SELECT 
            ROW_NUMBER() OVER (ORDER BY NeighborCount DESC) AS ConceptId,
            SpatialKey AS ConceptCentroid,
            NeighborCount AS MemberCount,
            AtomId AS RepresentativeAtomId
        FROM EmbeddingClusters
    )
    SELECT 
        ConceptId,
        ConceptCentroid,
        MemberCount,
        RepresentativeAtomId,
        @tenant_id AS TenantId
    FROM ConceptCentroids
);
GO
