CREATE OR ALTER PROCEDURE dbo.sp_SpatialNextToken
    @context_atom_ids NVARCHAR(MAX),
    @temperature FLOAT = 1.0,
    @top_k INT = 3
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @context_centroid GEOMETRY;
    DECLARE @atom_count INT;

    SELECT
        @context_centroid = ContextCentroid,
        @atom_count = AtomCount
    FROM dbo.fn_GetContextCentroid(@context_atom_ids);

    IF @context_centroid IS NULL OR @atom_count IS NULL OR @atom_count = 0
    BEGIN
        RAISERROR('No valid context atoms found', 16, 1);
        RETURN;
    END;

    DECLARE @resolved_top_k INT = CASE WHEN @top_k IS NULL OR @top_k <= 0 THEN 3 ELSE @top_k END;
    DECLARE @candidate_pool INT = @resolved_top_k * 4;

    DECLARE @candidates TABLE
    (
        AtomId BIGINT,
        AtomText NVARCHAR(100),
        SpatialDistance FLOAT,
        Logit FLOAT,
        ProbabilityScore FLOAT
    );

    INSERT INTO @candidates (AtomId, AtomText, SpatialDistance, Logit, ProbabilityScore)
    SELECT TOP (@resolved_top_k)
        ae.AtomId,
                CONVERT(NVARCHAR(256), a.AtomicValue) AS token, -- Derived from AtomicValue
        nn.SpatialDistance,
        -1.0 * nn.SpatialDistance AS Logit,
        0.0
    FROM dbo.fn_SpatialKNN(@context_centroid, @candidate_pool, N'AtomEmbeddings') AS nn
    INNER JOIN dbo.AtomEmbedding ae ON ae.AtomEmbeddingId = nn.AtomEmbeddingId
    INNER JOIN dbo.Atom a ON a.AtomId = ae.AtomId
    WHERE ae.AtomId NOT IN (SELECT CAST(value AS BIGINT) FROM STRING_SPLIT(@context_atom_ids, ','))
    ORDER BY nn.SpatialDistance ASC;

    IF NOT EXISTS (SELECT 1 FROM @candidates)
    BEGIN
        RETURN;
    END;

    DECLARE @maxLogit FLOAT = (SELECT MAX(Logit) FROM @candidates);
    DECLARE @temperatureSafe FLOAT = CASE WHEN @temperature IS NULL OR @temperature <= 0 THEN 1.0 ELSE @temperature END;

    UPDATE c
    SET ProbabilityScore = dbo.fn_SoftmaxTemperature(Logit, @maxLogit, @temperatureSafe)
    FROM @candidates AS c;

    DECLARE @totalWeight FLOAT = (SELECT SUM(ProbabilityScore) FROM @candidates);

    SELECT
        AtomId AS TokenId,
        AtomText AS TokenText,
        SpatialDistance,
        CASE
            WHEN @totalWeight IS NULL OR @totalWeight = 0 THEN 0
            ELSE ProbabilityScore / @totalWeight
        END AS ProbabilityScore
    FROM @candidates
    ORDER BY ProbabilityScore DESC, SpatialDistance ASC;
END;