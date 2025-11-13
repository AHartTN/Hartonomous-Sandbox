CREATE PROCEDURE dbo.sp_GenerateImage
    @prompt NVARCHAR(MAX),
    @width INT = 512,
    @height INT = 512,
    @patch_size INT = 32,
    @steps INT = 32,
    @guidance_scale FLOAT = 6.5,
    @ModelIds NVARCHAR(MAX) = NULL,
    @top_k INT = 4,
    @output_format NVARCHAR(10) = 'patches'
AS
BEGIN
    SET NOCOUNT ON;

    IF @prompt IS NULL OR LTRIM(RTRIM(@prompt)) = ''
        THROW 50100, 'Prompt is required for image generation.', 1;

    DECLARE @promptEmbedding VECTOR(1998);
    DECLARE @embeddingDim INT;
    EXEC dbo.sp_TextToEmbedding @text = @prompt, @ModelName = NULL, @embedding = @promptEmbedding OUTPUT, @dimension = @embeddingDim OUTPUT;

    DECLARE @models TABLE (ModelId INT PRIMARY KEY, Weight FLOAT NOT NULL, ModelName NVARCHAR(200));

    INSERT INTO @models (ModelId, Weight, ModelName)
    SELECT ModelId, Weight, ModelName
    FROM dbo.fn_SelectModelsForTask('image_generation', @ModelIds, NULL, 'image,vision', 'image_generation');

    IF NOT EXISTS (SELECT 1 FROM @models)
        THROW 50101, 'No image-capable models are configured for generation.', 1;

    DECLARE @modelsJson NVARCHAR(MAX) = (
        SELECT ModelId, Weight, ModelName FROM @models FOR JSON PATH
    );

    DECLARE @requestInfo NVARCHAR(MAX) = JSON_OBJECT(
        'prompt': @prompt,
        'width': @width,
        'height': @height,
        'patch_size': @patch_size,
        'steps': @steps,
        'guidance_scale': @guidance_scale,
        'top_k': @top_k,
        'output_format': @output_format
    );

    DECLARE @inferenceId BIGINT;
    INSERT INTO dbo.InferenceRequests (TaskType, InputData, ModelsUsed, EnsembleStrategy, OutputMetadata)
    VALUES (
        'image_generation',
        TRY_CAST(@requestInfo AS JSON),
        TRY_CAST(@modelsJson AS JSON),
        'spatial_diffusion',
        JSON_OBJECT('status': 'running')
    );
    SET @inferenceId = SCOPE_IDENTITY();

    DECLARE @startTime DATETIME2 = SYSUTCDATETIME();

    DECLARE @imageCandidates TABLE (
        ImageId BIGINT,
        SourcePath NVARCHAR(500),
        Width INT,
        Height INT,
        Score FLOAT,
        Distance FLOAT,
        Rank INT
    );

    IF @promptEmbedding IS NOT NULL
    BEGIN
        ;WITH ImageBase AS (
            SELECT
                img.ImageId,
                img.SourcePath,
                img.Width,
                img.Height,
                VECTOR_DISTANCE('cosine', img.GlobalEmbedding, @promptEmbedding) AS Distance
            FROM dbo.Images img
            WHERE img.GlobalEmbedding IS NOT NULL
        )
        INSERT INTO @imageCandidates (ImageId, SourcePath, Width, Height, Score, Distance, Rank)
        SELECT TOP (@top_k)
            ImageId,
            SourcePath,
            Width,
            Height,
            1.0 - Distance,
            Distance,
            ROW_NUMBER() OVER (ORDER BY Distance, ImageId)
        FROM ImageBase
        ORDER BY Distance, ImageId;
    END;

    DECLARE @guideX FLOAT = 0;
    DECLARE @guideY FLOAT = 0;
    DECLARE @guideZ FLOAT = 0;

    IF EXISTS (SELECT 1 FROM @imageCandidates)
    BEGIN
        SELECT
            @guideX = AVG(CAST(p.PatchRegion.STCentroid().STX AS FLOAT)),
            @guideY = AVG(CAST(p.PatchRegion.STCentroid().STY AS FLOAT)),
            @guideZ = AVG(CAST(COALESCE(p.PatchRegion.STCentroid().Z, 0.0) AS FLOAT))
        FROM dbo.ImagePatches p
        INNER JOIN @imageCandidates c ON c.ImageId = p.ImageId;

        IF @guideX IS NULL SET @guideX = 0;
        IF @guideY IS NULL SET @guideY = 0;
        IF @guideZ IS NULL SET @guideZ = 0;
    END;

    DECLARE @seed INT = ABS(CHECKSUM(@prompt + CONVERT(NVARCHAR(50), @startTime)));

    DECLARE @patches TABLE (
        patch_x INT,
        patch_y INT,
        spatial_x FLOAT,
        spatial_y FLOAT,
        spatial_z FLOAT,
        geometry GEOMETRY
    );

    INSERT INTO @patches (patch_x, patch_y, spatial_x, spatial_y, spatial_z, geometry)
    SELECT
        patch_x,
        patch_y,
        spatial_x,
        spatial_y,
        spatial_z,
        patch
    FROM dbo.clr_GenerateImagePatches(
        @width,
        @height,
        @patch_size,
        @steps,
        @guidance_scale,
        @guideX,
        @guideY,
        @guideZ,
        @seed
    );

    DECLARE @durationMs INT = DATEDIFF(MILLISECOND, @startTime, SYSUTCDATETIME());
    DECLARE @patchCount INT = (SELECT COUNT(*) FROM @patches);

    DECLARE @patchesJson NVARCHAR(MAX) = (
        SELECT patch_x, patch_y, spatial_x, spatial_y, spatial_z, geometry.STAsText() AS patch_wkt
        FROM @patches
        ORDER BY patch_y, patch_x
        FOR JSON PATH
    );

    DECLARE @candidatesJson NVARCHAR(MAX) = NULL;
    IF EXISTS (SELECT 1 FROM @imageCandidates)
    BEGIN
        SET @candidatesJson = (
            SELECT ImageId, SourcePath, Width, Height, Score, Distance, Rank
            FROM @imageCandidates
            ORDER BY Rank
            FOR JSON PATH
        );
    END;

    DECLARE @outputJson NVARCHAR(MAX) = JSON_OBJECT(
        'generated_format': @output_format,
        'patches': JSON_QUERY(@patchesJson),
        'candidates': JSON_QUERY(@candidatesJson)
    );

    UPDATE dbo.InferenceRequests
    SET TotalDurationMs = @durationMs,
        OutputData = @outputJson,
        OutputMetadata = JSON_OBJECT(
            'status': 'completed',
            'patch_count': @patchCount,
            'width': @width,
            'height': @height,
            'steps': @steps,
            'guidance': JSON_OBJECT('x': @guideX, 'y': @guideY, 'z': @guideZ)
        )
    WHERE InferenceId = @inferenceId;

    INSERT INTO dbo.InferenceSteps (InferenceId, StepNumber, OperationType, DurationMs, RowsReturned)
    VALUES (@inferenceId, 1, 'image_candidate_search', @durationMs, ISNULL((SELECT COUNT(*) FROM @imageCandidates), 0));

    IF @output_format = 'patches'
    BEGIN
        SELECT
            @inferenceId AS InferenceId,
            patch_x AS PatchX,
            patch_y AS PatchY,
            spatial_x,
            spatial_y,
            spatial_z,
            geometry
        FROM @patches
        ORDER BY patch_y, patch_x;
    END
    ELSE
    BEGIN
        DECLARE @resultGeometry GEOMETRY = dbo.clr_GenerateImageGeometry(
            @width,
            @height,
            @patch_size,
            @steps,
            @guidance_scale,
            @guideX,
            @guideY,
            @guideZ,
            @seed
        );

        SELECT @inferenceId AS InferenceId, @resultGeometry AS GeneratedImageGeometry;
    END;

    SELECT
        @inferenceId AS InferenceId,
        @prompt AS Prompt,
        @width AS Width,
        @height AS Height,
        @steps AS Steps,
        @durationMs AS DurationMs,
        JSON_QUERY(@candidatesJson) AS CandidateImages;
END;