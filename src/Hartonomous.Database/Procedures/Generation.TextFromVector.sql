CREATE OR ALTER PROCEDURE dbo.sp_GenerateText
    @prompt NVARCHAR(MAX),
    @max_tokens INT = 64,
    @temperature FLOAT = 0.8,
    @ModelIds NVARCHAR(MAX) = NULL,
    @top_k INT = 6
AS
BEGIN
    SET NOCOUNT ON;

    IF @prompt IS NULL OR LTRIM(RTRIM(@prompt)) = ''
        THROW 50090, 'Prompt is required for text generation.', 1;

    IF @max_tokens <= 0
        SET @max_tokens = 1;

    IF @temperature IS NULL OR @temperature <= 0
        SET @temperature = 0.01;


    EXEC dbo.sp_TextToEmbedding @text = @prompt, @ModelName = NULL, @embedding = @promptEmbedding OUTPUT, @dimension = @embeddingDim OUTPUT;

    IF @promptEmbedding IS NULL
        THROW 50091, 'Failed to derive prompt embedding for generation.', 1;

    

    IF NOT EXISTS (SELECT 1 FROM @models)
        THROW 50092, 'No eligible models found for text generation.', 1;

        SELECT ModelId, Weight, ModelName
        FROM @models
        FOR JSON PATH
    );

        'prompt': @prompt,
        'max_tokens': @max_tokens,
        'temperature': @temperature,
        'top_k': @top_k
    );

    
    SET @inferenceId = SCOPE_IDENTITY();

        StepNumber INT,
        AtomId BIGINT,
        Token NVARCHAR(400),
        Score FLOAT,
        Distance FLOAT,
        ModelCount INT,
        DurationMs INT
    );

    -- Convert VECTOR to NVARCHAR(MAX) (JSON format) for CLR function
    -- SQL Server 2025 VECTOR type can only convert to VARCHAR/NVARCHAR/JSON, not VARBINARY

    


        SELECT STRING_AGG(Token, ' ') WITHIN GROUP (ORDER BY StepNumber)
        FROM @sequence
        WHERE Token IS NOT NULL AND LTRIM(RTRIM(Token)) <> ''
    );

    IF @tokenSuffix IS NOT NULL AND LTRIM(RTRIM(@tokenSuffix)) <> ''
    BEGIN

        IF RIGHT(@basePrompt, 1) = ' '
        BEGIN
            SET @generatedText = LTRIM(RTRIM(@basePrompt + @tokenSuffix));
        END
        ELSE
        BEGIN
            SET @generatedText = LTRIM(RTRIM(@basePrompt + ' ' + @tokenSuffix));
        END
    END;



        SELECT StepNumber, AtomId, Token, Score, Distance, ModelCount, DurationMs
        FROM @sequence
        ORDER BY StepNumber
        FOR JSON PATH
    );


    SET @stream = CONVERT(provenance.AtomicStream, NULL);


        SELECT TOP (1) ModelName FROM @models ORDER BY Weight DESC, ModelId
    );



    SET @stream = provenance.clr_CreateAtomicStream(@streamId, @streamCreated, @streamScope, @primaryModel, @streamMetadata);
    SET @stream = provenance.clr_AppendAtomicStreamSegment(@stream, 'Input', @streamCreated, 'text/plain; charset=utf-16', JSON_OBJECT('prompt_length': LEN(@prompt)), CAST(@prompt AS VARBINARY(MAX)));
    SET @stream = provenance.clr_AppendAtomicStreamSegment(@stream, 'Control', @streamCreated, 'application/json', JSON_OBJECT('max_tokens': @max_tokens, 'temperature': @temperature, 'top_k': @top_k), CAST(JSON_QUERY(@modelsJson) AS VARBINARY(MAX)));

    IF @promptEmbedding IS NOT NULL
    BEGIN
        -- Store embedding as JSON (VECTOR type can only convert to NVARCHAR/JSON)

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

    

    -- Note: Stream column removed from GenerationStreams to allow CLR type redeploy
    -- Provenance still tracked in AtomicStream variable above, could be persisted elsewhere if needed
    -- 

    SELECT
        @inferenceId AS InferenceId,
        @streamId AS StreamId,
        @prompt AS OriginalPrompt,
        @generatedText AS GeneratedText,
        @tokenCount AS TokensGenerated,
        @totalDuration AS DurationMs,
        JSON_QUERY(@tokensJson) AS TokenDetails;
END
