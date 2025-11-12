CREATE PROCEDURE dbo.sp_SpatialAttention
    @QueryAtomId BIGINT,
    @ContextSize INT = 5
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @query_spatial GEOMETRY;
    SELECT @query_spatial = SpatialGeometry
    FROM dbo.AtomEmbeddings
    WHERE AtomId = @QueryAtomId
      AND SpatialGeometry IS NOT NULL;

    IF @query_spatial IS NULL
    BEGIN
        RAISERROR('Query atom not found or missing spatial projection', 16, 1);
        RETURN;
    END;

    ;WITH CandidateNeighbors AS
    (
        SELECT TOP (@ContextSize + 1)
            nn.AtomEmbeddingId,
            nn.AtomId,
            nn.SpatialDistance
        FROM dbo.fn_SpatialKNN(@query_spatial, @ContextSize + 1, N'AtomEmbeddings') AS nn
    ),
    FilteredNeighbors AS
    (
        SELECT *
        FROM CandidateNeighbors
        WHERE AtomId <> @QueryAtomId
    )
    SELECT TOP (@ContextSize)
        ae.AtomId AS TokenId,
    CAST(a.CanonicalText AS NVARCHAR(100)) AS TokenText,
        fn.SpatialDistance,
        1.0 / (1.0 + fn.SpatialDistance) AS AttentionWeight,
        CASE
            WHEN ae.SpatialCoarse IS NULL THEN 'UNKNOWN'
            WHEN ae.SpatialCoarse.STDistance(@query_spatial) < 2.0 THEN 'COARSE_MATCH'
            WHEN fn.SpatialDistance < 0.5 THEN 'FINE_MATCH'
            ELSE 'MID_MATCH'
        END AS ResolutionLevel
    FROM FilteredNeighbors fn
    INNER JOIN dbo.AtomEmbeddings ae ON ae.AtomEmbeddingId = fn.AtomEmbeddingId
    INNER JOIN dbo.Atoms a ON a.AtomId = ae.AtomId
    ORDER BY fn.SpatialDistance ASC;
END;