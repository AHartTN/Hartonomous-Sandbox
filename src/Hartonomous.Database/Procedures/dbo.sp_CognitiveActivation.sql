CREATE PROCEDURE dbo.sp_CognitiveActivation
    @query_embedding VARBINARY(MAX),
    @activation_threshold FLOAT = 0.8,
    @max_activated INT = 50
AS
BEGIN
    SET NOCOUNT ON;

    IF @query_embedding IS NULL
    BEGIN
        ;THROW 50010, 'Query embedding cannot be NULL.', 1;
    END;

    IF @activation_threshold IS NULL OR @activation_threshold <= -1.0 OR @activation_threshold > 1.0
    BEGIN
        ;THROW 50011, 'Activation threshold must be within (-1, 1].', 1;
    END;

    DECLARE @start_time DATETIME2 = SYSUTCDATETIME();
    DECLARE @max_distance FLOAT = 1.0 - @activation_threshold;

    IF @max_distance <= 0
    BEGIN
        ;THROW 50012, 'Activation threshold too high for cosine similarity search.', 1;
    END;

    DECLARE @activated TABLE (
        AtomEmbeddingId BIGINT PRIMARY KEY,
        AtomId BIGINT NOT NULL,
        ActivationStrength FLOAT NOT NULL
    );

    INSERT INTO @activated (AtomEmbeddingId, AtomId, ActivationStrength)
    SELECT TOP (@max_activated)
        ae.AtomEmbeddingId,
        ae.AtomId,
        1.0 - VECTOR_DISTANCE('cosine', ae.EmbeddingVector, @query_embedding) AS ActivationStrength
    FROM dbo.AtomEmbeddings AS ae
    WHERE ae.EmbeddingVector IS NOT NULL
      AND VECTOR_DISTANCE('cosine', ae.EmbeddingVector, @query_embedding) <= @max_distance
    ORDER BY VECTOR_DISTANCE('cosine', ae.EmbeddingVector, @query_embedding) ASC;

    DECLARE @activated_count INT = @@ROWCOUNT;

    SELECT
        ae.AtomEmbeddingId,
        ae.AtomId,
        a.Modality,
        a.Subtype,
        a.SourceType,
        a.SourceUri,
        a.CanonicalText,
        act.ActivationStrength,
        CASE
            WHEN act.ActivationStrength >= 0.95 THEN 'VERY_HIGH'
            WHEN act.ActivationStrength >= 0.90 THEN 'HIGH'
            WHEN act.ActivationStrength >= 0.85 THEN 'MEDIUM'
            ELSE 'LOW'
        END AS ActivationLevel
    FROM @activated AS act
    INNER JOIN dbo.AtomEmbeddings AS ae ON ae.AtomEmbeddingId = act.AtomEmbeddingId
    INNER JOIN dbo.Atoms AS a ON a.AtomId = act.AtomId
    ORDER BY act.ActivationStrength DESC;

    DECLARE @DurationMs INT = DATEDIFF(MILLISECOND, @start_time, SYSUTCDATETIME());
    DECLARE @input_json NVARCHAR(MAX) = (SELECT @activation_threshold as activationThreshold, @max_activated as maxActivated FOR JSON PATH, WITHOUT_ARRAY_WRAPPER);
    DECLARE @output_json NVARCHAR(MAX) = (SELECT @activated_count as activatedCount FOR JSON PATH, WITHOUT_ARRAY_WRAPPER);
    DECLARE @output_metadata NVARCHAR(MAX) = (SELECT 'completed' as status, @DurationMs as durationMs FOR JSON PATH, WITHOUT_ARRAY_WRAPPER);
    DECLARE @InferenceId BIGINT;

    INSERT INTO dbo.InferenceRequests (TaskType, InputData, ModelsUsed, EnsembleStrategy, OutputData, OutputMetadata, TotalDurationMs)
    VALUES (
        'cognitive_activation',
        @input_json,
        'atom_embeddings',
        'cognitive_activation',
        @output_json,
        @output_metadata,
        @DurationMs
    );

    SET @InferenceId = SCOPE_IDENTITY();
END;
GO