CREATE PROCEDURE [Deduplication].[SimilarityCheck]
    @QuerySpatialKey GEOMETRY,
    @Threshold FLOAT,
    @ModelId INT = NULL
AS
BEGIN
    SET NOCOUNT ON;

    IF @QuerySpatialKey IS NULL
    BEGIN
        THROW 50030, 'Query spatial key cannot be NULL.', 1;
    END

    IF @Threshold IS NULL OR @Threshold < 0.0 OR @Threshold > 1.0
    BEGIN
        THROW 50031, 'Threshold must be between 0.0 and 1.0 (Euclidean distance normalized).', 1;
    END

    -- Query spatial embeddings using GEOMETRY distance
    SELECT TOP (1)
        ae.AtomEmbeddingId,
        ae.AtomId,
        a.ContentHash,
        a.Modality,
        a.Subtype,
        ae.ModelId,
        ae.SpatialKey.STDistance(@QuerySpatialKey) AS EuclideanDistance,
        1.0 / (1.0 + ae.SpatialKey.STDistance(@QuerySpatialKey)) AS SimilarityScore,
        ae.SpatialKey,
        ae.HilbertValue,
        a.ReferenceCount,
        a.CreatedAt,
        a.ModifiedAt
    FROM dbo.AtomEmbedding AS ae WITH (INDEX(SIX_AtomEmbedding_SpatialKey))
    INNER JOIN dbo.Atom AS a ON a.AtomId = ae.AtomId
    WHERE ae.SpatialKey IS NOT NULL
      AND ae.SpatialKey.STDistance(@QuerySpatialKey) <= @Threshold
      AND (@ModelId IS NULL OR ae.ModelId = @ModelId)
    ORDER BY ae.SpatialKey.STDistance(@QuerySpatialKey) ASC;
END;
GO

