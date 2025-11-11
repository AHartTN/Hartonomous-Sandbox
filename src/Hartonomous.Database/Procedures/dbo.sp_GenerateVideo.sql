CREATE PROCEDURE dbo.sp_GenerateVideo
    @prompt NVARCHAR(MAX),
    @targetDurationMs INT = 4000,
    @targetFps INT = 24,
    @ModelIds NVARCHAR(MAX) = NULL,
    @top_k INT = 3
AS
BEGIN
    SET NOCOUNT ON;

    IF @prompt IS NULL OR LTRIM(RTRIM(@prompt)) = ''
        THROW 50120, 'Prompt is required for video generation.', 1;

    IF @targetDurationMs IS NULL OR @targetDurationMs <= 0
        SET @targetDurationMs = 4000;

    IF @targetFps IS NULL OR @targetFps <= 0
        SET @targetFps = 24;

    DECLARE @promptEmbedding VARBINARY(MAX);
    DECLARE @embeddingDim INT;
    EXEC dbo.sp_TextToEmbedding @text = @prompt, @ModelName = NULL, @embedding = @promptEmbedding OUTPUT, @dimension = @embeddingDim OUTPUT;

    DECLARE @models TABLE (ModelId INT PRIMARY KEY, Weight FLOAT NOT NULL, ModelName NVARCHAR(200));

    INSERT INTO @models (ModelId, Weight, ModelName)
    SELECT ModelId, Weight, ModelName
    FROM dbo.fn_SelectModelsForTask('video_generation', @ModelIds, NULL, 'video,vision', 'video_generation');

    IF NOT EXISTS (SELECT 1 FROM @models)
        THROW 50121, 'No video-capable models are configured for generation.', 1;

    DECLARE @modelsJson NVARCHAR(MAX) = (
        SELECT ModelId, Weight, ModelName FROM @models FOR JSON PATH
    );

    DECLARE @requestJson NVARCHAR(MAX) = (SELECT @prompt as prompt, @targetDurationMs as targetDurationMs, @targetFps as targetFps, @top_k as top_k FOR JSON PATH, WITHOUT_ARRAY_WRAPPER);

    DECLARE @inferenceId BIGINT;
    INSERT INTO dbo.InferenceRequests (TaskType, InputData, ModelsUsed, EnsembleStrategy, OutputMetadata)
    VALUES (
        'video_generation',
        TRY_CAST(@requestJson AS JSON),
        TRY_CAST(@modelsJson AS JSON),
        'temporal_recombination',
        (SELECT 'running' as status FOR JSON PATH, WITHOUT_ARRAY_WRAPPER)
    );
    SET @inferenceId = SCOPE_IDENTITY();

    DECLARE @startTime DATETIME2 = SYSUTCDATETIME();

    DECLARE @videoCandidates TABLE (
        VideoId BIGINT,
        SourcePath NVARCHAR(500),
        DurationMs BIGINT,
        Fps INT,
        Width INT,
        Height INT,
        Score FLOAT,
        Distance FLOAT,
        Rank INT
    );

    IF @promptEmbedding IS NOT NULL
    BEGIN
        ;WITH VideoBase AS (
            SELECT
                vid.VideoId,
                vid.SourcePath,
                vid.DurationMs,
                vid.Fps,
                vid.ResolutionWidth,
                vid.ResolutionHeight,
                VECTOR_DISTANCE('cosine', vid.GlobalEmbedding, @promptEmbedding) AS Distance
            FROM dbo.Videos vid
            WHERE vid.GlobalEmbedding IS NOT NULL
        )
        INSERT INTO @videoCandidates (VideoId, SourcePath, DurationMs, Fps, Width, Height, Score, Distance, Rank)
        SELECT TOP (@top_k)
            VideoId,
            SourcePath,
            DurationMs,
            Fps,
            ResolutionWidth,
            ResolutionHeight,
            1.0 - Distance,
            Distance,
            ROW_NUMBER() OVER (ORDER BY Distance, VideoId)
        FROM VideoBase
        ORDER BY Distance, VideoId;
    END;

    DECLARE @frameCount INT = CEILING(@targetDurationMs / (1000.0 / @targetFps));

    DECLARE @frames TABLE (
        FrameIndex INT,
        VideoId BIGINT,
        SourceFrameId BIGINT,
        TimestampMs BIGINT,
        PixelCloud GEOMETRY,
        ObjectRegions GEOMETRY,
        MotionVectors GEOMETRY,
        FrameEmbedding VARBINARY(MAX)
    );

    IF EXISTS (SELECT 1 FROM @videoCandidates)
    BEGIN
        INSERT INTO @frames (FrameIndex, VideoId, SourceFrameId, TimestampMs, PixelCloud, ObjectRegions, MotionVectors, FrameEmbedding)
        SELECT TOP (@frameCount)
            ROW_NUMBER() OVER (ORDER BY c.Rank, vf.TimestampMs) - 1 AS FrameIndex,
            vf.VideoId,
            vf.FrameId,
            vf.TimestampMs,
            vf.PixelCloud,
            vf.ObjectRegions,
            vf.MotionVectors,
            vf.FrameEmbedding
        FROM dbo.VideoFrames vf
        INNER JOIN @videoCandidates c ON c.VideoId = vf.VideoId
        ORDER BY c.Rank, vf.TimestampMs;
    END;

    IF NOT EXISTS (SELECT 1 FROM @frames)
    BEGIN
        ;WITH Numbers AS (
            SELECT TOP (@frameCount) ROW_NUMBER() OVER (ORDER BY (SELECT NULL)) - 1 AS idx
            FROM sys.all_objects
        )
        INSERT INTO @frames (FrameIndex, VideoId, SourceFrameId, TimestampMs, PixelCloud, ObjectRegions, MotionVectors, FrameEmbedding)
        SELECT
            idx,
            NULL,
            NULL,
            idx * (1000 / @targetFps),
            geometry::STGeomFromText(
                'POINT(' + CAST(COS(idx * 0.25) AS NVARCHAR(40)) + ' ' + CAST(SIN(idx * 0.25) AS NVARCHAR(40)) + ')',
                0
            ),
            NULL,
            NULL,
            NULL
        FROM Numbers;
    END;

    DECLARE @durationMs INT = DATEDIFF(MILLISECOND, @startTime, SYSUTCDATETIME());
    DECLARE @framesJson NVARCHAR(MAX) = (
        SELECT FrameIndex, VideoId, SourceFrameId, TimestampMs,
               CASE WHEN PixelCloud IS NULL THEN NULL ELSE PixelCloud.STAsText() END AS PixelCloudWkt,
               CASE WHEN ObjectRegions IS NULL THEN NULL ELSE ObjectRegions.STAsText() END AS ObjectRegionsWkt,
               CASE WHEN MotionVectors IS NULL THEN NULL ELSE MotionVectors.STAsText() END AS MotionVectorsWkt
        FROM @frames
        ORDER BY FrameIndex
        FOR JSON PATH
    );

    DECLARE @candidatesJson NVARCHAR(MAX) = NULL;
    IF EXISTS (SELECT 1 FROM @videoCandidates)
    BEGIN
        SET @candidatesJson = (
            SELECT VideoId, SourcePath, DurationMs, Fps, Width, Height, Score, Distance, Rank
            FROM @videoCandidates
            ORDER BY Rank
            FOR JSON PATH
        );
    END;

    DECLARE @outputJson NVARCHAR(MAX) = (SELECT JSON_QUERY(@framesJson) as frames, JSON_QUERY(@candidatesJson) as candidates, @targetFps as targetFps FOR JSON PATH, WITHOUT_ARRAY_WRAPPER);

    UPDATE dbo.InferenceRequests
    SET TotalDurationMs = @durationMs,
        OutputData = TRY_CAST(@outputJson AS JSON),
        OutputMetadata = (SELECT 'completed' as status, (SELECT COUNT(*) FROM @frames) as frame_count, (SELECT COUNT(*) FROM @videoCandidates) as candidate_count FOR JSON PATH, WITHOUT_ARRAY_WRAPPER)
    WHERE InferenceId = @inferenceId;

    INSERT INTO dbo.InferenceSteps (InferenceId, StepNumber, OperationType, DurationMs, RowsReturned)
    VALUES (@inferenceId, 1, 'video_candidate_search', @durationMs, ISNULL((SELECT COUNT(*) FROM @videoCandidates), 0));

    SELECT
        @inferenceId AS InferenceId,
        FrameIndex,
        VideoId,
        SourceFrameId,
        TimestampMs,
        PixelCloud,
        ObjectRegions,
        MotionVectors,
        FrameEmbedding
    FROM @frames
    ORDER BY FrameIndex;

    SELECT
        @inferenceId AS InferenceId,
        @prompt AS Prompt,
        @targetDurationMs AS TargetDurationMs,
        @targetFps AS TargetFps,
        @durationMs AS DurationMs,
        JSON_QUERY(@candidatesJson) AS CandidateVideos;
END;
GO