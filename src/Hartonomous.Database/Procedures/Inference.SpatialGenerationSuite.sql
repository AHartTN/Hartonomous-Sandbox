-- Spatial attention and generation routines for Hartonomous atoms.

CREATE OR ALTER PROCEDURE dbo.sp_SpatialAttention
    @QueryAtomId BIGINT,
    @ContextSize INT = 5
AS
BEGIN
    SET NOCOUNT ON;

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

CREATE OR ALTER PROCEDURE dbo.sp_SpatialNextToken
    @context_atom_ids NVARCHAR(MAX),
    @temperature FLOAT = 1.0,
    @top_k INT = 3
AS
BEGIN
    SET NOCOUNT ON;


    SELECT
        @context_centroid = ContextCentroid,
        @atom_count = AtomCount
    FROM dbo.fn_GetContextCentroid(@context_atom_ids);

    IF @context_centroid IS NULL OR @atom_count IS NULL OR @atom_count = 0
    BEGIN
        RAISERROR('No valid context atoms found', 16, 1);
        RETURN;
    END;



    (
        AtomId BIGINT,
        AtomText NVARCHAR(100),
        SpatialDistance FLOAT,
        Logit FLOAT,
        ProbabilityScore FLOAT
    );

    

    IF NOT EXISTS (SELECT 1 FROM @candidates)
    BEGIN
        RETURN;
    END;


    UPDATE c
    SET ProbabilityScore = dbo.fn_SoftmaxTemperature(Logit, @maxLogit, @temperatureSafe)
    FROM @candidates AS c;

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

CREATE OR ALTER PROCEDURE dbo.sp_GenerateTextSpatial
    @prompt NVARCHAR(MAX),
    @max_tokens INT = 10,
    @temperature FLOAT = 1.0
AS
BEGIN
    SET NOCOUNT ON;






    

    WHILE @iteration < @max_tokens
    BEGIN
        SELECT @context_ids = STRING_AGG(CAST(AtomId AS NVARCHAR(20)), ',')
        FROM @context;

        IF @context_ids IS NULL
        BEGIN
            BREAK;
        END;

        

        SELECT TOP 1
            @NextAtomId = TokenId,
            @NextAtomText = TokenText
        FROM @next
        ORDER BY ProbabilityScore DESC;

        IF @NextAtomId IS NULL OR EXISTS (SELECT 1 FROM @context WHERE AtomId = @NextAtomId)
        BEGIN
            BREAK;
        END;

        
        SET @generated_text = @generated_text + N' ' + @NextAtomText;
        SET @iteration = @iteration + 1;
    END;

    SELECT
        @prompt AS OriginalPrompt,
        @generated_text AS GeneratedText,
        @iteration AS TokensGenerated,
        'SPATIAL_GEOMETRY_R_TREE' AS Method;
END;
