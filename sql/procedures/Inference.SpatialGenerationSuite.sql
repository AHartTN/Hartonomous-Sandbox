-- Spatial attention and generation routines for Hartonomous atoms.
GO

CREATE OR ALTER PROCEDURE dbo.sp_SpatialAttention
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
        CAST(a.AtomData AS NVARCHAR(100)) AS TokenText,
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
GO

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
        CAST(a.AtomData AS NVARCHAR(100)) AS AtomText,
        nn.SpatialDistance,
        -1.0 * nn.SpatialDistance AS Logit,
        0.0
    FROM dbo.fn_SpatialKNN(@context_centroid, @candidate_pool, N'AtomEmbeddings') AS nn
    INNER JOIN dbo.AtomEmbeddings ae ON ae.AtomEmbeddingId = nn.AtomEmbeddingId
    INNER JOIN dbo.Atoms a ON a.AtomId = ae.AtomId
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
GO

CREATE OR ALTER PROCEDURE dbo.sp_GenerateTextSpatial
    @prompt NVARCHAR(MAX),
    @max_tokens INT = 10,
    @temperature FLOAT = 1.0
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @context TABLE (AtomId BIGINT PRIMARY KEY, AtomText NVARCHAR(100));
    DECLARE @generated_text NVARCHAR(MAX) = @prompt;
    DECLARE @iteration INT = 0;
    DECLARE @context_ids NVARCHAR(MAX);
    DECLARE @NextAtomId BIGINT;
    DECLARE @NextAtomText NVARCHAR(100);

    INSERT INTO @context (AtomId, AtomText)
    SELECT a.AtomId, CAST(a.AtomData AS NVARCHAR(100))
    FROM dbo.Atoms a
    WHERE CAST(a.AtomData AS NVARCHAR(100)) IN (
        SELECT LTRIM(RTRIM(value)) FROM STRING_SPLIT(@prompt, ' ')
    );

    WHILE @iteration < @max_tokens
    BEGIN
        SELECT @context_ids = STRING_AGG(CAST(AtomId AS NVARCHAR(20)), ',')
        FROM @context;

        IF @context_ids IS NULL
        BEGIN
            BREAK;
        END;

        DECLARE @next TABLE (TokenId BIGINT, TokenText NVARCHAR(100), SpatialDistance FLOAT, ProbabilityScore FLOAT);

        INSERT INTO @next
        EXEC dbo.sp_SpatialNextToken
            @context_atom_ids = @context_ids,
            @temperature = @temperature,
            @top_k = 1;

        SELECT TOP 1
            @NextAtomId = TokenId,
            @NextAtomText = TokenText
        FROM @next
        ORDER BY ProbabilityScore DESC;

        IF @NextAtomId IS NULL OR EXISTS (SELECT 1 FROM @context WHERE AtomId = @NextAtomId)
        BEGIN
            BREAK;
        END;

        INSERT INTO @context (AtomId, AtomText) VALUES (@NextAtomId, @NextAtomText);
        SET @generated_text = @generated_text + N' ' + @NextAtomText;
        SET @iteration = @iteration + 1;
    END;

    SELECT
        @prompt AS OriginalPrompt,
        @generated_text AS GeneratedText,
        @iteration AS TokensGenerated,
        'SPATIAL_GEOMETRY_R_TREE' AS Method;
END;
GO
