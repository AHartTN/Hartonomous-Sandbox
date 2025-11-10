-- Audio generation pipeline with retrieval and synthetic fallback.

CREATE OR ALTER PROCEDURE dbo.sp_GenerateAudio
    @prompt NVARCHAR(MAX),
    @targetDurationMs INT = 5000,
    @sampleRate INT = 44100,
    @ModelIds NVARCHAR(MAX) = NULL,
    @top_k INT = 5,
    @temperature FLOAT = 0.6
AS
BEGIN
    SET NOCOUNT ON;

    IF @prompt IS NULL OR LTRIM(RTRIM(@prompt)) = ''
        THROW 50110, 'Prompt is required for audio generation.', 1;

    IF @targetDurationMs IS NULL OR @targetDurationMs <= 0
        SET @targetDurationMs = 5000;

    IF @sampleRate IS NULL OR @sampleRate <= 0
        SET @sampleRate = 44100;

    IF @temperature IS NULL OR @temperature <= 0
        SET @temperature = 0.1;


    EXEC dbo.sp_TextToEmbedding @text = @prompt, @ModelName = NULL, @embedding = @promptEmbedding OUTPUT, @dimension = @embeddingDim OUTPUT;

    

    IF NOT EXISTS (SELECT 1 FROM @models)
        THROW 50111, 'No audio-capable models are configured for generation.', 1;

        SELECT ModelId, Weight, ModelName
        FROM @models
        FOR JSON PATH
    );

        'prompt': @prompt,
        'targetDurationMs': @targetDurationMs,
        'sampleRate': @sampleRate,
        'temperature': @temperature,
        'top_k': @top_k
    );

    
    SET @inferenceId = SCOPE_IDENTITY();


        AudioId BIGINT,
        SourcePath NVARCHAR(500),
        DurationMs BIGINT,
        SampleRate INT,
        NumChannels TINYINT,
        Score FLOAT,
        Distance FLOAT,
        Rank INT
    );

    IF @promptEmbedding IS NOT NULL
    BEGIN
        ;WITH AudioBase AS (
            SELECT
                ad.AudioId,
                ad.SourcePath,
                ad.DurationMs,
                ad.SampleRate,
                ad.NumChannels,
                VECTOR_DISTANCE('cosine', ad.GlobalEmbedding, @promptEmbedding) AS Distance
            FROM dbo.AudioData ad
            WHERE ad.GlobalEmbedding IS NOT NULL
        )
        
    END;

    
