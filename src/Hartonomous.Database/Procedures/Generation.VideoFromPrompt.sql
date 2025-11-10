-- Video generation pipeline combining retrieval and synthetic keyframe expansion.

CREATE OR ALTER PROCEDURE dbo.sp_GenerateVideo
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


    EXEC dbo.sp_TextToEmbedding @text = @prompt, @ModelName = NULL, @embedding = @promptEmbedding OUTPUT, @dimension = @embeddingDim OUTPUT;

    

    IF NOT EXISTS (SELECT 1 FROM @models)
        THROW 50121, 'No video-capable models are configured for generation.', 1;

        SELECT ModelId, Weight, ModelName FROM @models FOR JSON PATH
    );

        'prompt': @prompt,
        'targetDurationMs': @targetDurationMs,
        'targetFps': @targetFps,
        'top_k': @top_k
    );

    
    SET @inferenceId = SCOPE_IDENTITY();


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
        
    END;


        FrameIndex INT,
        VideoId BIGINT,
        SourceFrameId BIGINT,
        TimestampMs BIGINT,
        PixelCloud GEOMETRY,
        ObjectRegions GEOMETRY,
        MotionVectors GEOMETRY,
        FrameEmbedding VECTOR(768)
    );

    IF EXISTS (SELECT 1 FROM @videoCandidates)
    BEGIN
        
    END;

    IF NOT EXISTS (SELECT 1 FROM @frames)
    BEGIN
        ;WITH Numbers AS (
            SELECT TOP (@frameCount) ROW_NUMBER() OVER (ORDER BY (SELECT NULL)) - 1 AS idx
            FROM sys.all_objects
        )
        
    END;


        SELECT FrameIndex, VideoId, SourceFrameId, TimestampMs,
               CASE WHEN PixelCloud IS NULL THEN NULL ELSE PixelCloud.STAsText() END AS PixelCloudWkt,
               CASE WHEN ObjectRegions IS NULL THEN NULL ELSE ObjectRegions.STAsText() END AS ObjectRegionsWkt,
               CASE WHEN MotionVectors IS NULL THEN NULL ELSE MotionVectors.STAsText() END AS MotionVectorsWkt
        FROM @frames
        ORDER BY FrameIndex
        FOR JSON PATH
    );

    IF EXISTS (SELECT 1 FROM @videoCandidates)
    BEGIN
        SET @candidatesJson = (
            SELECT VideoId, SourcePath, DurationMs, Fps, Width, Height, Score, Distance, Rank
            FROM @videoCandidates
            ORDER BY Rank
            FOR JSON PATH
        );
    END;

        'frames': JSON_QUERY(@framesJson),
        'candidates': JSON_QUERY(@candidatesJson),
        'targetFps': @targetFps
    );

    UPDATE dbo.InferenceRequests
    SET TotalDurationMs = @durationMs,
        OutputData = TRY_CAST(@outputJson AS JSON),
        OutputMetadata = JSON_OBJECT(
            'status': 'completed',
            'frame_count': (SELECT COUNT(*) FROM @frames),
            'candidate_count': (SELECT COUNT(*) FROM @videoCandidates)
        )
    WHERE InferenceId = @inferenceId;

    

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
