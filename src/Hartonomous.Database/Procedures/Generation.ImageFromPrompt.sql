-- Image generation pipeline using spatial diffusion with retrieval-guided geometry.

CREATE OR ALTER PROCEDURE dbo.sp_GenerateImage
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


    EXEC dbo.sp_TextToEmbedding @text = @prompt, @ModelName = NULL, @embedding = @promptEmbedding OUTPUT, @dimension = @embeddingDim OUTPUT;

    

    IF NOT EXISTS (SELECT 1 FROM @models)
        THROW 50101, 'No image-capable models are configured for generation.', 1;

        SELECT ModelId, Weight, ModelName FROM @models FOR JSON PATH
    );

        'prompt': @prompt,
        'width': @width,
        'height': @height,
        'patch_size': @patch_size,
        'steps': @steps,
        'guidance_scale': @guidance_scale,
        'top_k': @top_k,
        'output_format': @output_format
    );

    
    SET @inferenceId = SCOPE_IDENTITY();


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
        
    END;



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
