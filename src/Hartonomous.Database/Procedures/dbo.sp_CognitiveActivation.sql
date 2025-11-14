CREATE PROCEDURE dbo.sp_CognitiveActivation
    @query_embedding VECTOR(1998),
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

    PRINT 'COGNITIVE ACTIVATION: Atom embeddings firing based on cosine similarity';
    PRINT '  Threshold: ' + CAST(@activation_threshold AS NVARCHAR(10)) + ' | Max candidates: ' + CAST(@max_activated AS NVARCHAR(10));

    DECLARE @activated TABLE (
        AtomEmbeddingId BIGINT PRIMARY KEY,
        AtomId BIGINT NOT NULL,
        ActivationStrength FLOAT NOT NULL
    );

    INSERT INTO @activated (AtomEmbeddingId, AtomId, ActivationStrength)
    SELECT TOP (@max_activated)
        ae.AtomEmbeddingId,
        ae.AtomId,
        1.0 - VECTOR_DISTANCE('cosine', ae.SpatialKey, @query_embedding) AS ActivationStrength
    FROM dbo.AtomEmbeddings AS ae
    WHERE ae.SpatialKey IS NOT NULL
      AND VECTOR_DISTANCE('cosine', ae.SpatialKey, @query_embedding) <= @max_distance
    ORDER BY VECTOR_DISTANCE('cosine', ae.SpatialKey, @query_embedding) ASC;

    DECLARE @activated_count INT = @@ROWCOUNT;
    PRINT '  Activated nodes: ' + CAST(@activated_count AS NVARCHAR(10));

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
    DECLARE @input_json JSON = JSON_OBJECT('activationThreshold': @activation_threshold, 'maxActivated': @max_activated);
    DECLARE @output_json JSON = JSON_OBJECT('activatedCount': @activated_count);
    DECLARE @output_metadata JSON = JSON_OBJECT('status': 'completed', 'durationMs': @DurationMs);
    DECLARE @InferenceId BIGINT;

    INSERT INTO dbo.InferenceRequests (TaskType, InputData, ModelsUsed, EnsembleStrategy, OutputData, OutputMetadata, TotalDurationMs)
    VALUES (
        'cognitive_activation',
        CAST(@input_json AS NVARCHAR(MAX)),
        'atom_embeddings',
        'cognitive_activation',
        CAST(@output_json AS NVARCHAR(MAX)),
        CAST(@output_metadata AS NVARCHAR(MAX)),
        @DurationMs
    );

    SET @InferenceId = SCOPE_IDENTITY();
    PRINT '✓ Cognitive activation complete - Inference ID: ' + CAST(@InferenceId AS NVARCHAR(20));
END