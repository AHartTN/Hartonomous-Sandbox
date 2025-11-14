-- sp_AtomizeImage: Deep atomization for image content
-- Breaks images into ImagePatch atoms using spatial grid decomposition
-- This is Phase 2 of the atomization pipeline for image/* content types

CREATE PROCEDURE dbo.sp_AtomizeImage
    @AtomId BIGINT,
    @TenantId INT = 0,
    @PatchSize INT = 16,      -- Patch dimensions (e.g., 16x16 patches for ViT-like processing)
    @StrideSize INT = 16,      -- Stride between patches (set = @PatchSize for non-overlapping)
    @MinVarianceThreshold FLOAT = 0.001 -- Skip patches with very low variance (uniform color)
AS
BEGIN
    SET NOCOUNT ON;
    
    BEGIN TRY
        BEGIN TRANSACTION;
        
        -- Retrieve the parent image atom metadata and its payload
        DECLARE @Content VARBINARY(MAX);
        DECLARE @ContentType NVARCHAR(100);
        DECLARE @Metadata NVARCHAR(MAX);
        
        SELECT 
            @Metadata = CAST(a.Metadata AS NVARCHAR(MAX)),
            @Content = p.PayloadData,
            @ContentType = p.ContentType
        FROM dbo.Atoms a
        JOIN dbo.AtomPayloadStore p ON a.AtomId = p.AtomId
        WHERE a.AtomId = @AtomId AND a.TenantId = @TenantId;
        
        IF @Content IS NULL
        BEGIN
            RAISERROR('Image atom not found', 16, 1);
            RETURN -1;
        END
        
        -- Validate it's actually image content
        IF @ContentType NOT LIKE 'image/%'
        BEGIN
            RAISERROR('Atom is not image content', 16, 1);
            RETURN -1;
        END
        
        -- Extract image dimensions from metadata (required)
        DECLARE @ImageWidth INT = JSON_VALUE(@Metadata, '$.width');
        DECLARE @ImageHeight INT = JSON_VALUE(@Metadata, '$.height');
        DECLARE @ColorDepth INT = ISNULL(JSON_VALUE(@Metadata, '$.colorDepth'), 24);
        
        IF @ImageWidth IS NULL OR @ImageHeight IS NULL
        BEGIN
            RAISERROR('Image metadata must include width and height', 16, 1);
            RETURN -1;
        END
        
        -- Calculate patch grid dimensions
        DECLARE @PatchesX INT = CEILING((@ImageWidth - @PatchSize) * 1.0 / @StrideSize) + 1;
        DECLARE @PatchesY INT = CEILING((@ImageHeight - @PatchSize) * 1.0 / @StrideSize) + 1;
        DECLARE @TotalPatches INT = @PatchesX * @PatchesY;
        
        -- Create spatial representation of the full image
        -- Use geometry POLYGON where corners are at (0,0) and (width, height)
        DECLARE @ImageBoundary geometry;
        SET @ImageBoundary = geometry::STGeomFromText(
            'POLYGON((0 0, ' + CAST(@ImageWidth AS NVARCHAR(20)) + ' 0, ' + 
            CAST(@ImageWidth AS NVARCHAR(20)) + ' ' + CAST(@ImageHeight AS NVARCHAR(20)) + ', ' +
            '0 ' + CAST(@ImageHeight AS NVARCHAR(20)) + ', 0 0))', 
            0
        );
        
        -- Process all patches in a single, set-based operation using the high-performance CLR function
        DECLARE @PatchesCreated INT;

        INSERT INTO dbo.ImagePatches (
            ParentAtomId,
            PatchIndex,
            RowIndex,
            ColIndex,
            PatchGeometry,
            PatchX,
            PatchY,
            PatchWidth,
            PatchHeight,
            MeanR,
            MeanG,
            MeanB,
            Variance,
            DominantColor,
            TenantId
        )
        SELECT
            @AtomId,
            patches.PatchIndex,
            patches.RowIndex,
            patches.ColIndex,
            patches.PatchGeometry,
            patches.PatchX,
            patches.PatchY,
            patches.PatchWidth,
            patches.PatchHeight,
            patches.MeanR,
            patches.MeanG,
            patches.MeanB,
            patches.Variance,
            NULL, -- DominantColor (future enhancement)
            @TenantId
        FROM dbo.clr_DeconstructImageToPatches(@Content, @ImageWidth, @ImageHeight, @PatchSize, @StrideSize) AS patches;

        SET @PatchesCreated = @@ROWCOUNT;

        
        -- Update the parent atom's metadata with atomization results
        UPDATE dbo.Atoms
        SET Metadata = JSON_MODIFY(
            ISNULL(Metadata, '{}'),
            '$.atomization',
            JSON_QUERY(JSON_OBJECT(
                'type': 'image',
                'patchCount': @PatchesCreated,
                'patchSize': @PatchSize,
                'strideSize': @StrideSize,
                'gridDimensions': JSON_OBJECT(
                    'x': @PatchesX,
                    'y': @PatchesY
                ),
                'atomizedUtc': FORMAT(SYSUTCDATETIME(), 'yyyy-MM-ddTHH:mm:ss.fffZ')
            ))
        )
        WHERE AtomId = @AtomId AND TenantId = @TenantId;
        
        COMMIT TRANSACTION;
        
        SELECT 
            @AtomId AS ParentAtomId,
            @PatchesCreated AS PatchesCreated,
            @PatchesX AS GridWidth,
            @PatchesY AS GridHeight,
            @PatchSize AS PatchSize,
            'ImageAtomization' AS Status;
        
        RETURN 0;
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0
            ROLLBACK TRANSACTION;
        
        DECLARE @ErrorMessage NVARCHAR(4000) = ERROR_MESSAGE();
        RAISERROR(@ErrorMessage, 16, 1);
        RETURN -1;
    END CATCH
END;
GO
