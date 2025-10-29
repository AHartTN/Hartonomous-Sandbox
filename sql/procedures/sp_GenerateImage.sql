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
        task_type,
        input_data,
        models_used,
        input_metadata
    )
    VALUES (
        'image_generation',
        @prompt,
        'spatial_diffusion',
        JSON_OBJECT(
            'width': @width,
            'height': @height,
            'steps': @steps,
            'guidance_scale': @guidance_scale,
            'output_format': @output_format
        )
    );
    SET @inference_id = SCOPE_IDENTITY();

    -- Get text embedding for the prompt
    DECLARE @prompt_embedding VECTOR(768);
    SELECT @prompt_embedding = embedding
    FROM dbo.Embeddings_Production
    WHERE source_text = @prompt AND source_type = 'text'
    ORDER BY created_at DESC;

    -- If no exact match, find closest text embedding
    IF @prompt_embedding IS NULL
    BEGIN
        SELECT TOP 1 @prompt_embedding = embedding_full
        FROM dbo.Embeddings_Production
        WHERE source_type = 'text'
        ORDER BY VECTOR_DISTANCE('cosine', embedding_full,
            (SELECT TOP 1 embedding_full FROM dbo.Embeddings_Production
             WHERE CHARINDEX(@prompt, source_text) > 0)) ASC;
    END

    -- Initialize noise (random spatial points)
    DECLARE @noise_points TABLE (
        x INT, y INT,
        noise_x FLOAT, noise_y FLOAT, noise_z FLOAT,
        geometry GEOMETRY
    );

    DECLARE @x INT = 0, @y INT = 0;
    WHILE @x < @width
    BEGIN
        SET @y = 0;
        WHILE @y < @height
        BEGIN
            INSERT INTO @noise_points (x, y, noise_x, noise_y, noise_z, geometry)
            VALUES (
                @x, @y,
                RAND() * 2 - 1,  -- Random between -1 and 1
                RAND() * 2 - 1,
                RAND() * 2 - 1,
                geometry::STGeomFromText('POINT(' +
                    CAST(RAND() * 2 - 1 AS NVARCHAR) + ' ' +
                    CAST(RAND() * 2 - 1 AS NVARCHAR) + ' ' +
                    CAST(RAND() * 2 - 1 AS NVARCHAR) + ')', 0)
            );
            SET @y = @y + 16;  -- 16x16 patches
        END
        SET @x = @x + 16;
    END

    -- Denoising diffusion process (simplified spatial version)
    DECLARE @step INT = 0;
    WHILE @step < @steps
    BEGIN
        -- For each noise point, find semantically similar image patches
        UPDATE np
        SET geometry = geometry::STGeomFromText(
            'POINT(' +
            CAST(np.noise_x + @guidance_scale * (
                SELECT TOP 1 spatial_proj_x
                FROM dbo.Embeddings_Production ep
                WHERE ep.source_type = 'image_patch'
                ORDER BY VECTOR_DISTANCE('cosine', ep.embedding_full, @prompt_embedding) ASC
            ) AS NVARCHAR) + ' ' +
            CAST(np.noise_y + @guidance_scale * (
                SELECT TOP 1 spatial_proj_y
                FROM dbo.Embeddings_Production ep
                WHERE ep.source_type = 'image_patch'
                ORDER BY VECTOR_DISTANCE('cosine', ep.embedding_full, @prompt_embedding) ASC
            ) AS NVARCHAR) + ' ' +
            CAST(np.noise_z + @guidance_scale * (
                SELECT TOP 1 spatial_proj_z
                FROM dbo.Embeddings_Production ep
                WHERE ep.source_type = 'image_patch'
                ORDER BY VECTOR_DISTANCE('cosine', ep.embedding_full, @prompt_embedding) ASC
            ) AS NVARCHAR) + ')',
            0
        )
        FROM @noise_points np;

        SET @step = @step + 1;
    END

    -- Generate final image patches or geometries
    IF @output_format = 'patches'
    BEGIN
        -- Return image patches with their spatial coordinates
        SELECT
            x, y,
            geometry.STX as spatial_x,
            geometry.STY as spatial_y,
            geometry.STZ as spatial_z,
            geometry
        FROM @noise_points
        ORDER BY x, y;
    END
    ELSE
    BEGIN
        -- Return as unified geometry collection
        DECLARE @result_geometry GEOMETRY;
        SELECT @result_geometry = geometry::STGeomCollFromText(
            'GEOMETRYCOLLECTION(' +
            STRING_AGG('POINT(' + CAST(geometry.STX AS NVARCHAR) + ' ' +
                       CAST(geometry.STY AS NVARCHAR) + ' ' +
                       CAST(geometry.STZ AS NVARCHAR) + ')', ',') +
            ')', 0)
        FROM @noise_points;

        SELECT @result_geometry as generated_image_geometry;
    END

    -- Calculate duration and update inference request
    DECLARE @duration_ms INT = DATEDIFF(MILLISECOND, @start_time, SYSUTCDATETIME());

    UPDATE dbo.InferenceRequests
    SET total_duration_ms = @duration_ms,
        output_metadata = JSON_OBJECT(
            'status': 'completed',
            'patches_generated': (SELECT COUNT(*) FROM @noise_points),
            'width': @width,
            'height': @height,
            'steps': @steps
        )
    WHERE inference_id = @inference_id;

    -- Log inference step
    INSERT INTO dbo.InferenceSteps (inference_id, step_number, operation_type, duration_ms, metadata)
    VALUES (@inference_id, 1, 'spatial_diffusion', @duration_ms,
        JSON_OBJECT('diffusion_steps': @steps, 'patches': (SELECT COUNT(*) FROM @noise_points)));

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