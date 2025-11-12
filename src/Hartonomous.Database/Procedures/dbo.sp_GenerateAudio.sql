CREATE PROCEDURE dbo.sp_GenerateAudio
    @prompt NVARCHAR(MAX),
    @targetDurationMs INT = 5000,
    @sampleRate INT = 44100,
    @ModelIds NVARCHAR(MAX) = NULL,
    @top_k INT = 5,
    @temperature FLOAT = 0.6,
    @OutputAtomId BIGINT = NULL OUTPUT
AS
BEGIN
    SET NOCOUNT ON;

    SET @OutputAtomId = NULL;

    IF @prompt IS NULL OR LTRIM(RTRIM(@prompt)) = ''
        THROW 50110, 'Prompt is required for audio generation.', 1;

    IF @targetDurationMs IS NULL OR @targetDurationMs <= 0
        SET @targetDurationMs = 5000;

    IF @sampleRate IS NULL OR @sampleRate <= 0
        SET @sampleRate = 44100;

    IF @temperature IS NULL OR @temperature <= 0
        SET @temperature = 0.1;

    DECLARE @promptEmbedding VARBINARY(MAX);
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

    DECLARE @requestJson NVARCHAR(MAX) = (SELECT @prompt as prompt, @targetDurationMs as targetDurationMs, @sampleRate as sampleRate, @temperature as temperature, @top_k as top_k FOR JSON PATH, WITHOUT_ARRAY_WRAPPER);

    DECLARE @inferenceId BIGINT;
    INSERT INTO dbo.InferenceRequests (TaskType, InputData, ModelsUsed, EnsembleStrategy, OutputMetadata)
    VALUES (
        'audio_generation',
        TRY_CAST(@requestJson AS JSON),
        TRY_CAST(@modelsJson AS JSON),
        'vector_similarity',
        (SELECT 'running' as status FOR JSON PATH, WITHOUT_ARRAY_WRAPPER)
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

        SET @outputJson = (SELECT 'retrieval_composition' as strategy, JSON_QUERY(@segmentJson) as segments, JSON_QUERY(@candidateJson) as candidates FOR JSON PATH, WITHOUT_ARRAY_WRAPPER);

        UPDATE dbo.InferenceRequests
        SET TotalDurationMs = @durationMs,
            OutputData = TRY_CAST(@outputJson AS JSON),
            OutputMetadata = (SELECT 'completed' as status, (SELECT COUNT(*) FROM @segments) as segment_count, (SELECT COUNT(*) FROM @audioCandidates) as candidate_count FOR JSON PATH, WITHOUT_ARRAY_WRAPPER)
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
        DECLARE @seed BIGINT = CONVERT(BIGINT, CHECKSUM(@prompt));
        IF @seed < 0 SET @seed = -@seed;

        DECLARE @fundamentalHz FLOAT = 220.0 + (@seed % 380); -- map prompt to 220-600 Hz
        DECLARE @secondLevel FLOAT = ((@seed / 11) % 70) / 100.0;
        DECLARE @thirdLevel FLOAT = ((@seed / 23) % 60) / 100.0;
        DECLARE @amplitude FLOAT = 0.55 + ((@seed / 7) % 35) / 100.0;
        IF @amplitude > 0.95 SET @amplitude = 0.95;

        DECLARE @synthetic VARBINARY(MAX) = dbo.clr_GenerateHarmonicTone(@fundamentalHz, @targetDurationMs, @sampleRate, 2, @amplitude, @secondLevel, @thirdLevel);
        DECLARE @waveform GEOMETRY = NULL;
        DECLARE @rms FLOAT = NULL;
        DECLARE @peak FLOAT = NULL;

        IF @synthetic IS NOT NULL
        BEGIN
            SET @waveform = dbo.clr_AudioToWaveform(@synthetic, 2, @sampleRate, 4096);
            SET @rms = dbo.clr_AudioComputeRms(@synthetic, 2);
            SET @peak = dbo.clr_AudioComputePeak(@synthetic, 2);
        END;

        DECLARE @base64 NVARCHAR(MAX) = NULL;
        IF @synthetic IS NOT NULL
        BEGIN
            SET @base64 = (
                SELECT @synthetic
                FOR XML PATH(''), BINARY BASE64
            );
        END;

        SET @outputJson = (
            SELECT
                'synthetic_fallback' AS strategy,
                @fundamentalHz AS fundamentalHz,
                @sampleRate AS sampleRate,
                @targetDurationMs AS durationMs,
                2 AS channels,
                @amplitude AS amplitude,
                @secondLevel AS secondHarmonic,
                @thirdLevel AS thirdHarmonic,
                @rms AS rmsAmplitude,
                @peak AS peakAmplitude,
                @base64 AS base64
            FOR JSON PATH, WITHOUT_ARRAY_WRAPPER
        );

        UPDATE dbo.InferenceRequests
        SET TotalDurationMs = @durationMs,
            OutputData = TRY_CAST(@outputJson AS JSON),
            OutputMetadata = (
                SELECT
                    'completed' AS status,
                    0 AS segment_count,
                    0 AS candidate_count,
                    'synthetic_fallback' AS generation_mode
                FOR JSON PATH, WITHOUT_ARRAY_WRAPPER
            )
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
END;
GO