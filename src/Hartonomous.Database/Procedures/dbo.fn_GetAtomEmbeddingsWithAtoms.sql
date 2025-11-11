CREATE FUNCTION dbo.fn_GetAtomEmbeddingsWithAtoms(@dimension INT = NULL)
RETURNS TABLE
AS
RETURN
(
    SELECT
        ae.AtomEmbeddingId,
        ae.AtomId,
        ae.EmbeddingVector,
        ae.SpatialGeometry,
        ae.SpatialCoarse,
        ae.Dimension,
        ae.EmbeddingType,
        ae.ModelId,
        a.ContentHash,
        a.Modality,
        a.Subtype,
        a.SourceType,
        a.SourceUri,
        a.CanonicalText,
        a.PayloadLocator,
        a.Metadata,
        a.ReferenceCount,
        a.SpatialKey,
        a.CanonicalText AS AtomText
    FROM dbo.AtomEmbeddings ae
    INNER JOIN dbo.Atoms a ON ae.AtomId = a.AtomId
    WHERE @dimension IS NULL OR ae.Dimension = @dimension
);
GO