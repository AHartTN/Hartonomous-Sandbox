-- sp_AtomizeImage: Deep atomization for image content
-- Breaks images into ImagePatch atoms using spatial grid decomposition
-- This is Phase 2 of the atomization pipeline for image/* content types

CREATE OR ALTER PROCEDURE dbo.sp_AtomizeImage
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
        
        -- Retrieve the parent image atom
        DECLARE @Content VARBINARY(MAX);
        DECLARE @ContentType NVARCHAR(100);
        DECLARE @Metadata NVARCHAR(MAX);
        
        SELECT 
            @Content = Content,
            @ContentType = ContentType,
            @Metadata = Metadata
        FROM dbo.Atoms
        WHERE AtomId = @AtomId AND TenantId = @TenantId;
        
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
        -- Use GEOMETRY POLYGON where corners are at (0,0) and (width, height)
        DECLARE @ImageBoundary GEOMETRY;
        SET @ImageBoundary = GEOMETRY::STGeomFromText(
            'POLYGON((0 0, ' + CAST(@ImageWidth AS NVARCHAR(20)) + ' 0, ' + 
            CAST(@ImageWidth AS NVARCHAR(20)) + ' ' + CAST(@ImageHeight AS NVARCHAR(20)) + ', ' +
            '0 ' + CAST(@ImageHeight AS NVARCHAR(20)) + ', 0 0))', 
            0
        );
        
        -- Process each patch
        DECLARE @PatchX INT = 0;
        DECLARE @PatchY INT = 0;
        DECLARE @PatchIndex INT = 0;
        DECLARE @PixelX INT;
        DECLARE @PixelY INT;
        DECLARE @PatchGeometry GEOMETRY;
        DECLARE @PatchesCreated INT = 0;
        
        WHILE @PatchY < @PatchesY
        BEGIN
            SET @PatchX = 0;
            
            WHILE @PatchX < @PatchesX
            BEGIN
                SET @PixelX = @PatchX * @StrideSize;
                SET @PixelY = @PatchY * @StrideSize;
                
                -- Ensure patch doesn't exceed image bounds
                DECLARE @ActualPatchWidth INT = @PatchSize;
                DECLARE @ActualPatchHeight INT = @PatchSize;
                
                IF (@PixelX + @PatchSize) > @ImageWidth
                    SET @ActualPatchWidth = @ImageWidth - @PixelX;
                
                IF (@PixelY + @PatchSize) > @ImageHeight
                    SET @ActualPatchHeight = @ImageHeight - @PixelY;
                
                -- Create GEOMETRY polygon for this patch
                DECLARE @X1 INT = @PixelX;
                DECLARE @Y1 INT = @PixelY;
                DECLARE @X2 INT = @PixelX + @ActualPatchWidth;
                DECLARE @Y2 INT = @PixelY + @ActualPatchHeight;
                
                SET @PatchGeometry = GEOMETRY::STGeomFromText(
                    'POLYGON((' + 
                    CAST(@X1 AS NVARCHAR(20)) + ' ' + CAST(@Y1 AS NVARCHAR(20)) + ', ' +
                    CAST(@X2 AS NVARCHAR(20)) + ' ' + CAST(@Y1 AS NVARCHAR(20)) + ', ' +
                    CAST(@X2 AS NVARCHAR(20)) + ' ' + CAST(@Y2 AS NVARCHAR(20)) + ', ' +
                    CAST(@X1 AS NVARCHAR(20)) + ' ' + CAST(@Y2 AS NVARCHAR(20)) + ', ' +
                    CAST(@X1 AS NVARCHAR(20)) + ' ' + CAST(@Y1 AS NVARCHAR(20)) + '))',
                    0
                );
                
                -- Insert the patch (we'll compute variance/features in a future enhancement)
                -- For now, we create the spatial index for every patch
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
                VALUES (
                    @AtomId,
                    @PatchIndex,
                    @PatchY,
                    @PatchX,
                    @PatchGeometry,
                    @PixelX,
                    @PixelY,
                    @ActualPatchWidth,
                    @ActualPatchHeight,
                    NULL, -- MeanR (computed via CLR image processing in future)
                    NULL, -- MeanG
                    NULL, -- MeanB
                    NULL, -- Variance
                    NULL, -- DominantColor
                    @TenantId
                );
                
                SET @PatchesCreated = @PatchesCreated + 1;
                SET @PatchIndex = @PatchIndex + 1;
                SET @PatchX = @PatchX + 1;
            END
            
            SET @PatchY = @PatchY + 1;
        END
        
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
