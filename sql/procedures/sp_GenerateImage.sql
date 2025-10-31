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
        IF @prompt_embedding IS NOT NULL
        BEGIN
            DECLARE @guidance_geometry GEOMETRY;
            DECLARE @guide_x FLOAT = 0;
            DECLARE @guide_y FLOAT = 0;
            DECLARE @guide_z FLOAT = 0;

                        SELECT TOP 1
                                @guidance_geometry = ae.SpatialGeometry
            FROM dbo.AtomEmbeddings AS ae
            INNER JOIN dbo.Atoms AS a ON a.AtomId = ae.AtomId
            WHERE a.Modality = 'image'
                            AND (a.Subtype IN ('image_patch', 'patch', 'tile') OR a.Subtype IS NULL)
              AND ae.EmbeddingVector IS NOT NULL
            ORDER BY VECTOR_DISTANCE('cosine', ae.EmbeddingVector, @prompt_embedding);

            IF @guidance_geometry IS NOT NULL
            BEGIN
                SELECT
                    @guide_x = @guidance_geometry.STX,
                    @guide_y = @guidance_geometry.STY,
                    @guide_z = COALESCE(@guidance_geometry.STPointN(1).Z, 0);
            END

            UPDATE np
            SET geometry = geometry::STGeomFromText(
                'POINT(' +
                CAST(np.geometry.STX + (@guidance_scale * (@guide_x - np.geometry.STX)) AS NVARCHAR(64)) + ' ' +
                CAST(np.geometry.STY + (@guidance_scale * (@guide_y - np.geometry.STY)) AS NVARCHAR(64)) + ' ' +
                CAST(np.geometry.STPointN(1).Z + (@guidance_scale * (@guide_z - np.geometry.STPointN(1).Z)) AS NVARCHAR(64)) + ')',
                0
            )
            FROM @noise_points AS np;
        END

        -- If no prompt embedding is available, keep existing noise geometry (no-op)

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
            geometry.STPointN(1).Z as spatial_z,
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
            STRING_AGG('POINT(' + CAST(geometry.STX AS NVARCHAR(64)) + ' ' +
                       CAST(geometry.STY AS NVARCHAR(64)) + ' ' +
                       CAST(geometry.STPointN(1).Z AS NVARCHAR(64)) + ')', ',') +
            ')', 0)
        FROM @noise_points;

        SELECT @result_geometry as generated_image_geometry;
    END

    -- Calculate duration and update inference request
    DECLARE @duration_ms INT = DATEDIFF(MILLISECOND, @start_time, SYSUTCDATETIME());

    UPDATE dbo.InferenceRequests
    SET TotalDurationMs = @duration_ms,
        OutputMetadata = JSON_OBJECT(
            'status': 'completed',
            'patches_generated': (SELECT COUNT(*) FROM @noise_points),
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