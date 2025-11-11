CREATE PROCEDURE dbo.sp_GenerateText
    @prompt NVARCHAR(MAX),
    @max_tokens INT = 64,
    @temperature FLOAT = 0.8,
    @ModelIds NVARCHAR(MAX) = NULL,
    @top_k INT = 6,
    @GeneratedText NVARCHAR(MAX) = NULL OUTPUT
AS
BEGIN
    SET NOCOUNT ON;

    IF @prompt IS NULL OR LTRIM(RTRIM(@prompt)) = ''
        THROW 50090, 'Prompt is required for text generation.', 1;

    IF @max_tokens <= 0
        SET @max_tokens = 1;

    IF @temperature IS NULL OR @temperature <= 0
        SET @temperature = 0.01;

    DECLARE @promptEmbedding VARBINARY(MAX);
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

    DECLARE @requestPayload NVARCHAR(MAX) = (SELECT @prompt as prompt, @max_tokens as max_tokens, @temperature as temperature, @top_k as top_k FOR JSON PATH, WITHOUT_ARRAY_WRAPPER);

    DECLARE @inferenceId BIGINT;
    INSERT INTO dbo.InferenceRequests (TaskType, InputData, ModelsUsed, EnsembleStrategy, OutputMetadata)
    VALUES (
        'text_generation',
        TRY_CAST(@requestPayload AS JSON),
        TRY_CAST(@modelsJson AS JSON),
        'weighted_vector_consensus',
        (SELECT 'running' as status FOR JSON PATH, WITHOUT_ARRAY_WRAPPER)
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

    DECLARE @promptEmbeddingJson NVARCHAR(MAX) = CAST(@promptEmbedding AS NVARCHAR(MAX));

    -- TODO: Fix CLR function call
    -- INSERT INTO @sequence (StepNumber, AtomId, Token, Score, Distance, ModelCount, DurationMs)
    -- SELECT
    --     ROW_NUMBER() OVER (ORDER BY (SELECT NULL)) AS StepNumber,
    --     t.AtomId,
    --     t.Token,
    --     t.Score,
    --     t.Distance,
    --     t.ModelCount,
    --     t.DurationMs
    -- FROM dbo.clr_GenerateTextSequence(
    --     CAST(@promptEmbeddingJson AS VARBINARY(MAX)),
    --     @modelsJson,
    --     @max_tokens,
    --     @temperature,
    --     @top_k
    -- ) AS t;

    SET @GeneratedText = LTRIM(RTRIM(@prompt));
    DECLARE @tokenSuffix NVARCHAR(MAX) = (
        SELECT STRING_AGG(Token, ' ') WITHIN GROUP (ORDER BY StepNumber)
        FROM @sequence
        WHERE Token IS NOT NULL AND LTRIM(RTRIM(Token)) <> ''
    );

    IF @tokenSuffix IS NOT NULL AND LTRIM(RTRIM(@tokenSuffix)) <> ''
    BEGIN
        DECLARE @basePrompt NVARCHAR(MAX) = LTRIM(RTRIM(@GeneratedText));
        IF RIGHT(@basePrompt, 1) = ' '
        BEGIN
            SET @GeneratedText = LTRIM(RTRIM(@basePrompt + @tokenSuffix));
        END
        ELSE
        BEGIN
            SET @GeneratedText = LTRIM(RTRIM(@basePrompt + ' ' + @tokenSuffix));
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

    UPDATE dbo.InferenceRequests
    SET TotalDurationMs = @totalDuration,
        OutputData = TRY_CAST((SELECT JSON_QUERY(@tokensJson) as tokens, @GeneratedText as generatedText FOR JSON PATH, WITHOUT_ARRAY_WRAPPER) AS JSON),
        OutputMetadata = (SELECT 'completed' as status, @tokenCount as tokens_generated, @temperature as temperature, LEN(@prompt) as prompt_length, LEN(@GeneratedText) as result_length FOR JSON PATH, WITHOUT_ARRAY_WRAPPER)
    WHERE InferenceId = @inferenceId;

    INSERT INTO dbo.InferenceSteps (InferenceId, StepNumber, OperationType, DurationMs, RowsReturned)
    SELECT @inferenceId, StepNumber, 'text_generation_step', DurationMs, ModelCount
    FROM @sequence;

    SELECT
        @inferenceId AS InferenceId,
        NEWID() AS StreamId, -- Placeholder
        @prompt AS OriginalPrompt,
        @GeneratedText AS GeneratedText,
        @tokenCount AS TokensGenerated,
        @totalDuration AS DurationMs,
        JSON_QUERY(@tokensJson) AS TokenDetails;
END;
GO