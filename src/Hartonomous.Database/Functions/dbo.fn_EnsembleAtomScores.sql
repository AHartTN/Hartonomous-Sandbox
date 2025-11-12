CREATE FUNCTION dbo.fn_EnsembleAtomScores
(
    @embedding VECTOR(1998),
    @models_json NVARCHAR(MAX),
    @top_per_model INT = 10,
    @required_modality NVARCHAR(64) = NULL
)
RETURNS TABLE
AS
RETURN
(
    WITH ParsedModels AS (
        SELECT
            TRY_CAST(JSON_VALUE(value, '$.ModelId') AS INT) AS ModelId,
            TRY_CAST(JSON_VALUE(value, '$.Weight') AS FLOAT) AS Weight
        FROM OPENJSON(@models_json)
        WHERE JSON_VALUE(value, '$.ModelId') IS NOT NULL
    ),
    Normalized AS (
        SELECT
            ModelId,
            CASE
                WHEN SUM(Weight) OVER () IS NULL OR SUM(Weight) OVER () = 0
                    THEN 1.0 / NULLIF(COUNT(*) OVER (), 0)
                ELSE Weight / SUM(Weight) OVER ()
            END AS Weight
        FROM ParsedModels
    ),
    RankedCandidates AS (
        SELECT
            n.ModelId,
            n.Weight,
            ae.AtomEmbeddingId,
            ae.AtomId,
            a.Modality,
            a.Subtype,
            a.SourceType,
            a.SourceUri,
            a.CanonicalText,
            VECTOR_DISTANCE('cosine', ae.EmbeddingVector, @embedding) AS Distance,
            ROW_NUMBER() OVER (
                PARTITION BY n.ModelId
                ORDER BY VECTOR_DISTANCE('cosine', ae.EmbeddingVector, @embedding), ae.AtomEmbeddingId
            ) AS RankWithinModel
        FROM Normalized n
        INNER JOIN dbo.AtomEmbeddings ae ON ae.ModelId = n.ModelId
        INNER JOIN dbo.Atoms a ON a.AtomId = ae.AtomId
        WHERE ae.EmbeddingVector IS NOT NULL
          AND (@required_modality IS NULL OR a.Modality = @required_modality)
    )
    SELECT
        ModelId,
        AtomEmbeddingId,
        AtomId,
        Modality,
        Subtype,
        SourceType,
        SourceUri,
        CanonicalText,
        Distance,
        Weight * (1.0 - Distance) AS WeightedScore
    FROM RankedCandidates
    WHERE RankWithinModel <= CASE WHEN @top_per_model IS NULL OR @top_per_model <= 0 THEN 10 ELSE @top_per_model END
);
GO