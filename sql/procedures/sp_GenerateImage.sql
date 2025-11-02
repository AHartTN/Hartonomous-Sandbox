-- =============================================
-- Image Generation via Spatial Diffusion
-- =============================================

USE Hartonomous;
GO

CREATE OR ALTER PROCEDURE dbo.sp_GenerateImage
    @prompt NVARCHAR(MAX),
    @width INT = 64,
    @height INT = 64,
    @steps INT = 20,
    @guidance_scale FLOAT = 7.5,
    @output_format NVARCHAR(10) = 'patches'  -- 'patches' or 'geometry'
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @start_time DATETIME2 = SYSUTCDATETIME();
    DECLARE @inference_id BIGINT;

    -- Log the inference request
    INSERT INTO dbo.InferenceRequests (
        TaskType,
        InputData,
        ModelsUsed,
        OutputMetadata
    )
    VALUES (
        'image_generation',
        JSON_OBJECT(
            'prompt': @prompt,
            'width': @width,
            'height': @height,
            'steps': @steps,
            'guidance_scale': @guidance_scale,
            'output_format': @output_format
        ),
        'spatial_diffusion',
        NULL
    );
    SET @inference_id = SCOPE_IDENTITY();

    -- Get text embedding for the prompt
        DECLARE @prompt_embedding VECTOR(1998);
        SELECT TOP 1 @prompt_embedding = ae.EmbeddingVector
        FROM dbo.AtomEmbeddings AS ae
        INNER JOIN dbo.Atoms AS a ON a.AtomId = ae.AtomId
        WHERE a.CanonicalText = @prompt
            AND a.Modality = 'text'
            AND ae.EmbeddingVector IS NOT NULL
        ORDER BY ae.CreatedAt DESC;

        -- If no exact match, fall back to the most recent text atom containing the prompt
        IF @prompt_embedding IS NULL
        BEGIN
                SELECT TOP 1 @prompt_embedding = ae.EmbeddingVector
                FROM dbo.AtomEmbeddings AS ae
                INNER JOIN dbo.Atoms AS a ON a.AtomId = ae.AtomId
                WHERE a.Modality = 'text'
                    AND a.CanonicalText IS NOT NULL
                    AND a.CanonicalText LIKE '%' + @prompt + '%'
                    AND ae.EmbeddingVector IS NOT NULL
                ORDER BY ae.CreatedAt DESC;
        END

    DECLARE @guide_x FLOAT = 0;
    DECLARE @guide_y FLOAT = 0;
    DECLARE @guide_z FLOAT = 0;

    IF @prompt_embedding IS NOT NULL
    BEGIN
        SELECT TOP 1
            @guide_x = ae.SpatialGeometry.STX,
            @guide_y = ae.SpatialGeometry.STY,
            @guide_z = COALESCE(ae.SpatialGeometry.STPointN(1).Z, 0)
        FROM dbo.AtomEmbeddings AS ae
        INNER JOIN dbo.Atoms AS a ON a.AtomId = ae.AtomId
        WHERE a.Modality = 'image'
          AND (a.Subtype IN ('image_patch', 'patch', 'tile') OR a.Subtype IS NULL)
          AND ae.EmbeddingVector IS NOT NULL
        ORDER BY VECTOR_DISTANCE('cosine', ae.EmbeddingVector, @prompt_embedding);
    END

    DECLARE @seed INT = ABS(CHECKSUM(@prompt + CONVERT(NVARCHAR(50), @start_time)));

    DECLARE @patch_source TABLE (
        patch_x INT,
        patch_y INT,
        spatial_x FLOAT,
        spatial_y FLOAT,
        spatial_z FLOAT,
        geometry GEOMETRY
    );

    INSERT INTO @patch_source
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
        16,
        @steps,
        @guidance_scale,
        @guide_x,
        @guide_y,
        @guide_z,
        @seed
    );

    -- Generate final image patches or geometries
    IF @output_format = 'patches'
    BEGIN
        SELECT
            patch_x AS x,
            patch_y AS y,
            spatial_x,
            spatial_y,
            spatial_z,
            geometry
        FROM @patch_source
        ORDER BY patch_x, patch_y;
    END
    ELSE
    BEGIN
        DECLARE @result_geometry GEOMETRY;
        SELECT @result_geometry = dbo.clr_GenerateImageGeometry(
            @width,
            @height,
            16,
            @steps,
            @guidance_scale,
            @guide_x,
            @guide_y,
            @guide_z,
            @seed
        );

        SELECT @result_geometry as generated_image_geometry;
    END

    -- Calculate duration and update inference request
    DECLARE @duration_ms INT = DATEDIFF(MILLISECOND, @start_time, SYSUTCDATETIME());

    UPDATE dbo.InferenceRequests
    SET TotalDurationMs = @duration_ms,
        OutputMetadata = JSON_OBJECT(
            'status': 'completed',
            'patches_generated': (SELECT COUNT(*) FROM @patch_source),
            'width': @width,
            'height': @height,
            'steps': @steps
        )
    WHERE InferenceId = @inference_id;

    -- Log inference step
    INSERT INTO dbo.InferenceSteps (InferenceId, StepNumber, OperationType, DurationMs, QueryText)
    VALUES (
        @inference_id,
        1,
        'spatial_diffusion',
        @duration_ms,
        'spatial diffusion with semantic guidance'
    );

    -- Return metadata
    SELECT
        @inference_id as inference_id,
        @prompt as prompt,
        @width as width,
        @height as height,
        @steps as diffusion_steps,
        @duration_ms as duration_ms,
        'SPATIAL_DIFFUSION' as method;
END
GO