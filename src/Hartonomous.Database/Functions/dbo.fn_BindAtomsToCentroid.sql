-- =============================================
-- Function: fn_BindAtomsToCentroid
-- Description: Finds atoms similar to a concept centroid
-- Returns atoms within similarity threshold of centroid
-- =============================================
CREATE FUNCTION dbo.fn_BindAtomsToCentroid(
    @concept_centroid GEOMETRY,
    @similarity_threshold FLOAT,
    @tenant_id INT
)
RETURNS TABLE
AS
RETURN
(
    SELECT 
        ae.AtomId,
        ae.AtomEmbeddingId,
        ae.SpatialKey,
        ae.SpatialKey.STDistance(@concept_centroid) AS DistanceFromCentroid,
        a.CanonicalText,
        a.Modality
    FROM dbo.AtomEmbedding ae
    INNER JOIN dbo.Atom a ON ae.AtomId = a.AtomId
    WHERE ae.TenantId = @tenant_id
      AND ae.SpatialKey.STDistance(@concept_centroid) <= @similarity_threshold
);
GO
