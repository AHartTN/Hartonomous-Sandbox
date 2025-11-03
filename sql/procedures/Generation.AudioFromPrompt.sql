-- Audio generation pipeline with retrieval and synthetic fallback.
GO

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

    DECLARE @promptEmbedding VECTOR(1998);
    DECLARE @embeddingDim INT;
    EXEC dbo.sp_TextToEmbedding @text = @prompt, @ModelName = NULL, @embedding = @promptEmbedding OUTPUT, @dimension = @embeddingDim OUTPUT;

    DECLARE @models TABLE (ModelId INT PRIMARY KEY, Weight FLOAT NOT NULL, ModelName NVARCHAR(200));

    INSERT INTO @models (ModelId, Weight, ModelName)
    SELECT ModelId, Weight, ModelName
    FROM dbo.fn_SelectModelsForTask('audio_generation', @ModelIds, NULL, 'audio,sound', 'audio_generation');

    IF NOT EXISTS (SELECT 1 FROM @models)
        THROW 50111, 'No audio-capable models are configured for generation.', 1;

    DECLARE @modelsJson NVARCHAR(MAX) = (
        SELECT ModelId, Weight, ModelName
        FROM @models
        FOR JSON PATH
    );

    DECLARE @requestJson NVARCHAR(MAX) = JSON_OBJECT(
        'prompt': @prompt,
        'targetDurationMs': @targetDurationMs,
        'sampleRate': @sampleRate,
        'temperature': @temperature,
        'top_k': @top_k
    );

    DECLARE @inferenceId BIGINT;
    INSERT INTO dbo.InferenceRequests (TaskType, InputData, ModelsUsed, EnsembleStrategy, OutputMetadata)
    VALUES (
        'audio_generation',
        TRY_CAST(@requestJson AS JSON),
        TRY_CAST(@modelsJson AS JSON),
        'vector_similarity',
        JSON_OBJECT('status': 'running')
    );
    SET @inferenceId = SCOPE_IDENTITY();

    DECLARE @startTime DATETIME2 = SYSUTCDATETIME();

    DECLARE @audioCandidates TABLE (
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
        INSERT INTO @audioCandidates (AudioId, SourcePath, DurationMs, SampleRate, NumChannels, Score, Distance, Rank)
        SELECT TOP (@top_k)
            AudioId,
            SourcePath,
            DurationMs,
            SampleRate,
            NumChannels,
            1.0 - Distance,
            Distance,
            ROW_NUMBER() OVER (ORDER BY Distance, AudioId)
        FROM AudioBase
        ORDER BY Distance, AudioId;
    END;

    DECLARE @segments TABLE (
        AudioId BIGINT,
        FrameNumber BIGINT,
        TimestampMs BIGINT,
        AmplitudeL FLOAT,
        AmplitudeR FLOAT,
        RmsEnergy FLOAT,
        SpectralCentroid FLOAT
    );

    IF EXISTS (SELECT 1 FROM @audioCandidates)
    BEGIN
        INSERT INTO @segments
    SELECT TOP (CONVERT(INT, CEILING(@targetDurationMs / 50.0)))
            af.AudioId,
            af.FrameNumber,
            af.TimestampMs,
            af.AmplitudeL,
            af.AmplitudeR,
            af.RmsEnergy,
            af.SpectralCentroid
        FROM dbo.AudioFrames af
        INNER JOIN @audioCandidates c ON c.AudioId = af.AudioId
        ORDER BY c.Rank, af.TimestampMs;
    END;

    DECLARE @durationMs INT = DATEDIFF(MILLISECOND, @startTime, SYSUTCDATETIME());
    DECLARE @outputJson NVARCHAR(MAX);

    IF EXISTS (SELECT 1 FROM @segments)
    BEGIN
        DECLARE @segmentJson NVARCHAR(MAX) = (
            SELECT AudioId, FrameNumber, TimestampMs, AmplitudeL, AmplitudeR, RmsEnergy, SpectralCentroid
            FROM @segments
            ORDER BY TimestampMs
            FOR JSON PATH
        );

        DECLARE @candidateJson NVARCHAR(MAX) = (
            SELECT AudioId, SourcePath, DurationMs, SampleRate, NumChannels, Score, Distance, Rank
            FROM @audioCandidates
            ORDER BY Rank
            FOR JSON PATH
        );

        SET @outputJson = JSON_OBJECT(
            'strategy': 'retrieval_composition',
            'segments': JSON_QUERY(@segmentJson),
            'candidates': JSON_QUERY(@candidateJson)
        );

        UPDATE dbo.InferenceRequests
        SET TotalDurationMs = @durationMs,
            OutputData = TRY_CAST(@outputJson AS JSON),
            OutputMetadata = JSON_OBJECT(
                'status': 'completed',
                'segment_count': (SELECT COUNT(*) FROM @segments),
                'candidate_count': (SELECT COUNT(*) FROM @audioCandidates)
            )
        WHERE InferenceId = @inferenceId;

        INSERT INTO dbo.InferenceSteps (InferenceId, StepNumber, OperationType, DurationMs, RowsReturned)
        VALUES (@inferenceId, 1, 'audio_candidate_search', @durationMs, (SELECT COUNT(*) FROM @audioCandidates));

        SELECT
            @inferenceId AS InferenceId,
            c.AudioId,
            c.SourcePath,
            c.DurationMs,
            c.SampleRate,
            c.NumChannels,
            c.Score,
            c.Distance,
            c.Rank,
            JSON_QUERY(@segmentJson) AS SegmentPlan
        FROM @audioCandidates c
        ORDER BY c.Rank;
    END
    ELSE
    BEGIN
        DECLARE @synthetic VARBINARY(MAX) = dbo.clr_GenerateHarmonicTone(440.0, @targetDurationMs, @sampleRate, 2, 0.75, 0.25, 0.1);
        DECLARE @waveform GEOMETRY = dbo.clr_AudioToWaveform(@synthetic, 2, @sampleRate, 4096);
        DECLARE @base64 NVARCHAR(MAX) = (
            SELECT @synthetic
            FOR XML PATH(''), BINARY BASE64
        );

        SET @outputJson = JSON_OBJECT(
            'strategy': 'synthetic_fallback',
            'fundamentalHz': 440.0,
            'sampleRate': @sampleRate,
            'durationMs': @targetDurationMs,
            'channels': 2,
            'base64': @base64
        );

        UPDATE dbo.InferenceRequests
        SET TotalDurationMs = @durationMs,
            OutputData = TRY_CAST(@outputJson AS JSON),
            OutputMetadata = JSON_OBJECT('status': 'completed', 'segment_count': 0, 'candidate_count': 0)
        WHERE InferenceId = @inferenceId;

        INSERT INTO dbo.InferenceSteps (InferenceId, StepNumber, OperationType, DurationMs, RowsReturned)
        VALUES (@inferenceId, 1, 'audio_synthesis_tone', @durationMs, 0);

        SELECT
            @inferenceId AS InferenceId,
            NULL AS AudioId,
            NULL AS SourcePath,
            @targetDurationMs AS DurationMs,
            @sampleRate AS SampleRate,
            2 AS NumChannels,
            1.0 AS Score,
            0.0 AS Distance,
            1 AS Rank,
            @waveform AS WaveformGeometry,
            @base64 AS SynthesizedAudioBase64;
    END;
END
GO
