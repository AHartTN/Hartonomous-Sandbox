-- =============================================
-- Atomic Image Decomposition
-- =============================================
-- Decomposes images into deduplicated atomic RGB values
-- using the new AtomRelations architecture.
--
-- Instead of ImagePatches table, creates:
-- 1. Atoms for each unique RGB triplet (deduplication!)
-- 2. AtomRelations linking parent image to pixel atoms
-- 3. Weights/importance based on saliency
-- =============================================

CREATE PROCEDURE dbo.sp_AtomizeImage_Atomic
    @ParentAtomId BIGINT,
    @TenantId INT = 0,
    @MaxPixels INT = 10000,  -- Subsample large images
    @ComputeImportance BIT = 1  -- Calculate saliency-based importance
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;
    
    BEGIN TRY
        -- Retrieve image metadata
        DECLARE @Metadata NVARCHAR(MAX);
        DECLARE @ImageWidth INT, @ImageHeight INT;
        
        SELECT @Metadata = CAST(lob.Metadata AS NVARCHAR(MAX))
        FROM dbo.Atoms a
        LEFT JOIN dbo.AtomsLOB lob ON a.AtomId = lob.AtomId
        WHERE a.AtomId = @ParentAtomId AND a.TenantId = @TenantId;
        
        SET @ImageWidth = JSON_VALUE(@Metadata, '$.width');
        SET @ImageHeight = JSON_VALUE(@Metadata, '$.height');
        
        IF @ImageWidth IS NULL OR @ImageHeight IS NULL
        BEGIN
            RAISERROR('Image metadata must include width and height', 16, 1);
            RETURN -1;
        END
        
        -- Calculate sampling strategy
        DECLARE @TotalPixels INT = @ImageWidth * @ImageHeight;
        DECLARE @StrideX INT = 1, @StrideY INT = 1;
        
        IF @TotalPixels > @MaxPixels
        BEGIN
            -- Subsample to reduce memory footprint
            DECLARE @TargetStride FLOAT = SQRT(@TotalPixels * 1.0 / @MaxPixels);
            SET @StrideX = CAST(CEILING(@TargetStride) AS INT);
            SET @StrideY = @StrideX;
        END
        
        -- Extract pixel data using CLR function
        -- Returns table: (X INT, Y INT, R TINYINT, G TINYINT, B TINYINT)
        DECLARE @Pixels TABLE (
            X INT NOT NULL,
            Y INT NOT NULL,
            R TINYINT NOT NULL,
            G TINYINT NOT NULL,
            B TINYINT NOT NULL,
            Brightness FLOAT NULL,
            ContentHash BINARY(32) NULL,
            AtomId BIGINT NULL,
            PRIMARY KEY (X, Y)
        );
        
        -- Retrieve image binary data
        DECLARE @ImageData VARBINARY(MAX);
        SELECT @ImageData = lob.ComponentStream
        FROM dbo.AtomsLOB lob
        WHERE lob.AtomId = @ParentAtomId;
        
        IF @ImageData IS NULL
        BEGIN
            RAISERROR('Image binary data not found in AtomsLOB', 16, 1);
            RETURN -1;
        END
        
        -- Extract pixels using production CLR function
        INSERT INTO @Pixels (X, Y, R, G, B)
        SELECT X, Y, R, G, B
        FROM dbo.clr_ExtractImagePixels(@ImageData, @StrideX, @StrideY);
        
        -- Compute brightness for importance weighting
        UPDATE @Pixels
        SET Brightness = (R * 0.299 + G * 0.587 + B * 0.114) / 255.0;
        
        -- Compute ContentHash for each unique RGB value
        UPDATE @Pixels
        SET ContentHash = HASHBYTES('SHA2_256', 
            CAST(R AS BINARY(1)) + CAST(G AS BINARY(1)) + CAST(B AS BINARY(1))
        );
        
        -- Find or create atomic RGB values (deduplicated)
        BEGIN TRANSACTION;
        
        MERGE dbo.Atoms AS target
        USING (
            SELECT DISTINCT ContentHash, R, G, B
            FROM @Pixels
        ) AS source
        ON target.ContentHash = source.ContentHash
        WHEN NOT MATCHED THEN
            INSERT (
                ContentHash,
                Modality,
                Subtype,
                AtomicValue,
                CanonicalText,
                TenantId,
                ReferenceCount
            )
            VALUES (
                source.ContentHash,
                'color',
                'rgb24',
                CAST(source.R AS BINARY(1)) + CAST(source.G AS BINARY(1)) + CAST(source.B AS BINARY(1)),
                'rgb(' + CAST(source.R AS NVARCHAR(3)) + ',' + 
                        CAST(source.G AS NVARCHAR(3)) + ',' + 
                        CAST(source.B AS NVARCHAR(3)) + ')',
                @TenantId,
                0  -- Will increment below
            );
        
        -- Get AtomIds for all RGB values
        UPDATE p
        SET p.AtomId = a.AtomId
        FROM @Pixels p
        INNER JOIN dbo.Atoms a ON a.ContentHash = p.ContentHash;
        
        -- Create AtomRelations for each pixel
        INSERT INTO dbo.AtomRelations (
            SourceAtomId,
            TargetAtomId,
            RelationType,
            SequenceIndex,
            Weight,
            Importance,
            Confidence,
            CoordX,
            CoordY,
            CoordZ,
            TenantId
        )
        SELECT 
            @ParentAtomId,
            p.AtomId,
            'pixel_' + CAST(p.X AS NVARCHAR(10)) + '_' + CAST(p.Y AS NVARCHAR(10)),
            (p.Y * @ImageWidth + p.X),  -- Linear index
            1.0,  -- Weight (uniform for pixels)
            CASE 
                WHEN @ComputeImportance = 1 THEN
                    -- Importance = edge detection proxy (high gradient = high importance)
                    -- For now, use brightness variance
                    ABS(p.Brightness - AVG(p.Brightness) OVER ())
                ELSE 1.0
            END,
            1.0,  -- Confidence (always certain about pixel values)
            p.X * 1.0 / @ImageWidth,  -- Normalized coordinates
            p.Y * 1.0 / @ImageHeight,
            p.Brightness,  -- Z-axis = brightness
            @TenantId
        FROM @Pixels p;
        
        -- Update reference counts
        UPDATE a
        SET ReferenceCount = ReferenceCount + pixel_count
        FROM dbo.Atoms a
        INNER JOIN (
            SELECT AtomId, COUNT(*) AS pixel_count
            FROM @Pixels
            GROUP BY AtomId
        ) AS counts ON counts.AtomId = a.AtomId;
        
        COMMIT TRANSACTION;
        
        DECLARE @PixelCount INT = @@ROWCOUNT;
        DECLARE @UniqueColors INT = (SELECT COUNT(DISTINCT AtomId) FROM @Pixels);
        DECLARE @DeduplicationRatio FLOAT = 
            CASE WHEN @PixelCount > 0 
            THEN (1.0 - (@UniqueColors * 1.0 / @PixelCount)) * 100 
            ELSE 0 END;
        
        SELECT 
            @ParentAtomId AS ParentAtomId,
            @PixelCount AS TotalPixels,
            @UniqueColors AS UniqueColors,
            @DeduplicationRatio AS DeduplicationPct,
            'Atomic' AS StorageMode;
        
        RETURN 0;
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0
            ROLLBACK TRANSACTION;
        
        DECLARE @ErrorMessage NVARCHAR(4000) = ERROR_MESSAGE();
        DECLARE @ErrorSeverity INT = ERROR_SEVERITY();
        DECLARE @ErrorState INT = ERROR_STATE();
        
        RAISERROR(@ErrorMessage, @ErrorSeverity, @ErrorState);
        RETURN -1;
    END CATCH
END
GO
