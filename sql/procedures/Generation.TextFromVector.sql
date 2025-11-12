CREATE OR ALTER PROCEDURE dbo.sp_GenerateText
    @prompt NVARCHAR(MAX),
    @max_tokens INT = 64,
    @temperature FLOAT = 0.8,
    @ModelIds NVARCHAR(MAX) = NULL,
    @top_k INT = 6,
    @GeneratedText NVARCHAR(MAX) = NULL OUTPUT
AS
BEGIN
    SET NOCOUNT ON;

    SET @GeneratedText = NULL;

    IF @prompt IS NULL OR LTRIM(RTRIM(@prompt)) = ''
        THROW 50090, 'Prompt is required for text generation.', 1;

    IF @max_tokens <= 0
        SET @max_tokens = 1;

    IF @temperature IS NULL OR @temperature <= 0
        SET @temperature = 0.01;

    DECLARE @promptEmbedding VECTOR(1998);
    DECLARE @embeddingDim INT;
    EXEC dbo.sp_TextToEmbedding @text = @prompt, @ModelName = NULL, @embedding = @promptEmbedding OUTPUT, @dimension = @embeddingDim OUTPUT;

    IF @promptEmbedding IS NULL
        THROW 50091, 'Failed to derive prompt embedding for generation.', 1;

    DECLARE @models TABLE (ModelId INT PRIMARY KEY, Weight FLOAT NOT NULL, ModelName NVARCHAR(200));

    INSERT INTO @models (ModelId, Weight, ModelName)
    SELECT ModelId, Weight, ModelName
    FROM dbo.fn_SelectModelsForTask('text_generation', @ModelIds, NULL, 'text', 'language_model');

    IF NOT EXISTS (SELECT 1 FROM @models)
        THROW 50092, 'No eligible models found for text generation.', 1;

    DECLARE @modelsJson NVARCHAR(MAX) = (
        SELECT ModelId, Weight, ModelName
        FROM @models
        FOR JSON PATH
    );

    DECLARE @requestPayload NVARCHAR(MAX) = JSON_OBJECT(
        'prompt': @prompt,
        'max_tokens': @max_tokens,
        'temperature': @temperature,
        'top_k': @top_k
    );

    DECLARE @inferenceId BIGINT;
    INSERT INTO dbo.InferenceRequests (TaskType, InputData, ModelsUsed, EnsembleStrategy, OutputMetadata)
    VALUES (
        'text_generation',
        TRY_CAST(@requestPayload AS JSON),
        TRY_CAST(@modelsJson AS JSON),
        'weighted_vector_consensus',
        JSON_OBJECT('status': 'running')
    );
    SET @inferenceId = SCOPE_IDENTITY();

    DECLARE @sequence TABLE (
        StepNumber INT,
        AtomId BIGINT,
        Token NVARCHAR(400),
        Score FLOAT,
        Distance FLOAT,
        ModelCount INT,
        DurationMs INT
    );

    DECLARE @startTime DATETIME2 = SYSUTCDATETIME();

    -- Convert VECTOR to NVARCHAR(MAX) (JSON format) for CLR function
    -- SQL Server 2025 VECTOR type can only convert to VARCHAR/NVARCHAR/JSON, not VARBINARY
    DECLARE @promptEmbeddingJson NVARCHAR(MAX) = CAST(@promptEmbedding AS NVARCHAR(MAX));

    INSERT INTO @sequence (StepNumber, AtomId, Token, Score, Distance, ModelCount, DurationMs)
    SELECT
        ROW_NUMBER() OVER (ORDER BY (SELECT NULL)) AS StepNumber,
        t.AtomId,
        t.Token,
        t.Score,
        t.Distance,
        t.ModelCount,
        t.DurationMs
    FROM dbo.clr_GenerateTextSequence(
        CAST(@promptEmbeddingJson AS VARBINARY(MAX)),
        @modelsJson,
        @max_tokens,
        @temperature,
        @top_k
    ) AS t;

    DECLARE @generatedText NVARCHAR(MAX) = LTRIM(RTRIM(@prompt));
    DECLARE @tokenSuffix NVARCHAR(MAX) = (
        SELECT STRING_AGG(Token, ' ') WITHIN GROUP (ORDER BY StepNumber)
        FROM @sequence
        WHERE Token IS NOT NULL AND LTRIM(RTRIM(Token)) <> ''
    );

    IF @tokenSuffix IS NOT NULL AND LTRIM(RTRIM(@tokenSuffix)) <> ''
    BEGIN
        DECLARE @basePrompt NVARCHAR(MAX) = LTRIM(RTRIM(@generatedText));
        IF RIGHT(@basePrompt, 1) = ' '
        BEGIN
            SET @generatedText = LTRIM(RTRIM(@basePrompt + @tokenSuffix));
        END
        ELSE
        BEGIN
            SET @generatedText = LTRIM(RTRIM(@basePrompt + ' ' + @tokenSuffix));
        END
    END;

    DECLARE @totalDuration INT = DATEDIFF(MILLISECOND, @startTime, SYSUTCDATETIME());
    DECLARE @tokenCount INT = (SELECT COUNT(*) FROM @sequence);

    DECLARE @tokensJson NVARCHAR(MAX) = (
        SELECT StepNumber, AtomId, Token, Score, Distance, ModelCount, DurationMs
        FROM @sequence
        ORDER BY StepNumber
        FOR JSON PATH
    );

    DECLARE @streamId UNIQUEIDENTIFIER = NEWID();

    DECLARE @stream provenance.AtomicStream;
    SET @stream = CONVERT(provenance.AtomicStream, NULL);
    DECLARE @streamCreated DATETIME2(3) = SYSUTCDATETIME();
    DECLARE @primaryModel NVARCHAR(128) = (
        SELECT TOP (1) ModelName FROM @models ORDER BY Weight DESC, ModelId
    );
    DECLARE @modelCount INT = (SELECT COUNT(*) FROM @models);
    DECLARE @streamScope NVARCHAR(128) = 'text_generation';

    DECLARE @streamMetadata NVARCHAR(MAX) = JSON_OBJECT('inference_id': @inferenceId, 'model_count': @modelCount);

    SET @stream = provenance.clr_CreateAtomicStream(@streamId, @streamCreated, @streamScope, @primaryModel, @streamMetadata);
    SET @stream = provenance.clr_AppendAtomicStreamSegment(@stream, 'Input', @streamCreated, 'text/plain; charset=utf-16', JSON_OBJECT('prompt_length': LEN(@prompt)), CAST(@prompt AS VARBINARY(MAX)));
    SET @stream = provenance.clr_AppendAtomicStreamSegment(@stream, 'Control', @streamCreated, 'application/json', JSON_OBJECT('max_tokens': @max_tokens, 'temperature': @temperature, 'top_k': @top_k), CAST(JSON_QUERY(@modelsJson) AS VARBINARY(MAX)));

    IF @promptEmbedding IS NOT NULL
    BEGIN
        -- Store embedding as JSON (VECTOR type can only convert to NVARCHAR/JSON)
        DECLARE @promptEmbeddingBinary VARBINARY(MAX) = CAST(@promptEmbeddingJson AS VARBINARY(MAX));
        SET @stream = provenance.clr_AppendAtomicStreamSegment(
            @stream, 
            'Embedding', 
            @streamCreated, 
            'application/json; charset=utf-16', 
            JSON_OBJECT('dimension': @embeddingDim, 'format': 'vector_json'), 
            @promptEmbeddingBinary
        );
    END;

    SET @stream = provenance.clr_AppendAtomicStreamSegment(@stream, 'Telemetry', SYSUTCDATETIME(), 'application/json', JSON_OBJECT('token_count': @tokenCount, 'duration_ms': @totalDuration), CAST(JSON_QUERY(@tokensJson) AS VARBINARY(MAX)));
    SET @stream = provenance.clr_AppendAtomicStreamSegment(@stream, 'Output', SYSUTCDATETIME(), 'text/plain; charset=utf-16', JSON_OBJECT('token_count': @tokenCount), CAST(@generatedText AS VARBINARY(MAX)));

    SET @GeneratedText = @generatedText;

    UPDATE dbo.InferenceRequests
    SET TotalDurationMs = @totalDuration,
        OutputData = TRY_CAST(JSON_OBJECT('tokens': JSON_QUERY(@tokensJson), 'generatedText': @generatedText) AS JSON),
        OutputMetadata = JSON_OBJECT(
            'status': 'completed',
            'tokens_generated': @tokenCount,
            'temperature': @temperature,
            'prompt_length': LEN(@prompt),
            'result_length': LEN(@generatedText)
        )
    WHERE InferenceId = @inferenceId;

    INSERT INTO dbo.InferenceSteps (InferenceId, StepNumber, OperationType, DurationMs, RowsReturned)
    SELECT @inferenceId, StepNumber, 'text_generation_step', DurationMs, ModelCount
    FROM @sequence;

    -- Note: Stream column removed from GenerationStreams to allow CLR type redeploy
    -- Provenance still tracked in AtomicStream variable above, could be persisted elsewhere if needed
    -- INSERT INTO provenance.GenerationStreams (StreamId, Scope, Model, CreatedUtc)
    -- VALUES (@streamId, @streamScope, @primaryModel, @streamCreated);

    SELECT
        @inferenceId AS InferenceId,
        @streamId AS StreamId,
        @prompt AS OriginalPrompt,
    @generatedText AS GeneratedText,
        @tokenCount AS TokensGenerated,
        @totalDuration AS DurationMs,
        JSON_QUERY(@tokensJson) AS TokenDetails;
END
GO
