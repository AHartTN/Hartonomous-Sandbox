-- =============================================
-- sp_AtomizeImage_Governed: Governed Image Pixel Atomization
-- =============================================
-- Implements chunked, resumable image atomization with XYZM structural storage
-- PHASE 2: Self-Indexing Geometry with M-value (Hilbert)
-- IDEMPOTENT: Uses CREATE OR ALTER
-- =============================================

CREATE OR ALTER PROCEDURE [dbo].[sp_AtomizeImage_Governed]
    @IngestionJobId BIGINT,
    @ImageData VARBINARY(MAX),
    @ImageWidth INT,
    @ImageHeight INT
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    DECLARE @JobStatus VARCHAR(50), @AtomChunkSize INT, @CurrentAtomOffset BIGINT;
    DECLARE @AtomQuota BIGINT, @TotalAtomsProcessed BIGINT;
    DECLARE @ParentAtomId BIGINT;
    DECLARE @RowsInChunk BIGINT;
    DECLARE @TotalPixels BIGINT = CAST(@ImageWidth AS BIGINT) * CAST(@ImageHeight AS BIGINT);
    DECLARE @TenantId INT; -- V3: Added for tenancy

    -- Load job state
    SELECT
        @JobStatus = JobStatus,
        @AtomChunkSize = AtomChunkSize,
        @CurrentAtomOffset = CurrentAtomOffset,
        @AtomQuota = AtomQuota,
        @TotalAtomsProcessed = TotalAtomsProcessed,
        @ParentAtomId = ParentAtomId,
        @TenantId = TenantId -- V3: Retrieve TenantId from the job
    FROM dbo.IngestionJob
    WHERE IngestionJobId = @IngestionJobId;

    IF @JobStatus IS NULL OR @JobStatus IN ('Complete', 'Processing')
    BEGIN
        RAISERROR('Invalid job state.', 16, 1);
        RETURN -1;
    END

    UPDATE dbo.IngestionJob 
    SET JobStatus = 'Processing', LastUpdatedAt = SYSUTCDATETIME() 
    WHERE IngestionJobId = @IngestionJobId;

    -- Create temp tables
    CREATE TABLE #ChunkPixels (
        [PositionX] INT,
        [PositionY] INT,
        [R] TINYINT,
        [G] TINYINT,
        [B] TINYINT,
        [A] TINYINT,
        [SequenceIndex] BIGINT
    );
    
    CREATE TABLE #UniquePixels (
        [R] TINYINT,
        [G] TINYINT,
        [B] TINYINT,
        [A] TINYINT,
        [AtomicValue] VARBINARY(4) NOT NULL,
        [ContentHash] BINARY(32) NOT NULL,
        PRIMARY KEY ([R], [G], [B], [A])
    );
    
    CREATE TABLE #PixelToAtomId (
        [R] TINYINT,
        [G] TINYINT,
        [B] TINYINT,
        [A] TINYINT,
        [AtomId] BIGINT NOT NULL,
        PRIMARY KEY ([R], [G], [B], [A])
    );

    -- State machine loop
    WHILE (1 = 1)
    BEGIN
        BEGIN TRY
            IF @TotalAtomsProcessed > @AtomQuota
            BEGIN
                UPDATE dbo.IngestionJob 
                SET JobStatus = 'Failed', ErrorMessage = 'Atom quota exceeded.'
                WHERE IngestionJobId = @IngestionJobId;
                RAISERROR('Atom quota exceeded.', 16, 1);
                BREAK;
            END

            TRUNCATE TABLE #ChunkPixels;
            TRUNCATE TABLE #UniquePixels;
            TRUNCATE TABLE #PixelToAtomId;

            -- Extract pixel chunk
            -- This is a simplified version - production would use CLR for proper image decoding
            DECLARE @BytesPerPixel INT = 4; -- RGBA
            DECLARE @ChunkStartByte BIGINT = @CurrentAtomOffset * @BytesPerPixel;
            DECLARE @ChunkEndByte BIGINT = (@CurrentAtomOffset + @AtomChunkSize) * @BytesPerPixel;
            
            IF @ChunkStartByte >= DATALENGTH(@ImageData)
                BREAK;

            -- Simple extraction (placeholder - real implementation would decode image format)
            DECLARE @PixelIdx BIGINT = @CurrentAtomOffset;
            DECLARE @ByteIdx BIGINT = @ChunkStartByte;
            
            WHILE @PixelIdx < (@CurrentAtomOffset + @AtomChunkSize) 
                AND @ByteIdx < DATALENGTH(@ImageData) 
                AND @PixelIdx < @TotalPixels
            BEGIN
                DECLARE @R TINYINT = CAST(SUBSTRING(@ImageData, @ByteIdx + 1, 1) AS TINYINT);
                DECLARE @G TINYINT = CAST(SUBSTRING(@ImageData, @ByteIdx + 2, 1) AS TINYINT);
                DECLARE @B TINYINT = CAST(SUBSTRING(@ImageData, @ByteIdx + 3, 1) AS TINYINT);
                DECLARE @A TINYINT = CAST(SUBSTRING(@ImageData, @ByteIdx + 4, 1) AS TINYINT);
                
                DECLARE @X INT = @PixelIdx % @ImageWidth;
                DECLARE @Y INT = @PixelIdx / @ImageWidth;
                
                INSERT INTO #ChunkPixels ([PositionX], [PositionY], [R], [G], [B], [A], [SequenceIndex])
                VALUES (@X, @Y, @R, @G, @B, @A, @PixelIdx);
                
                SET @PixelIdx = @PixelIdx + 1;
                SET @ByteIdx = @ByteIdx + @BytesPerPixel;
            END

            SET @RowsInChunk = @@ROWCOUNT;
            
            IF @RowsInChunk = 0
                BREAK;

            -- Get unique RGBA values
            INSERT INTO #UniquePixels ([R], [G], [B], [A], [AtomicValue], [ContentHash])
            SELECT DISTINCT 
                [R], [G], [B], [A],
                CAST(([R] * 0x1000000 + [G] * 0x10000 + [B] * 0x100 + [A]) AS VARBINARY(4)),
                HASHBYTES('SHA2_256', CAST(([R] * 0x1000000 + [G] * 0x10000 + [B] * 0x100 + [A]) AS VARBINARY(4)))
            FROM #ChunkPixels;

            BEGIN TRANSACTION;

                MERGE [dbo].[Atom] AS T
                USING #UniquePixels AS S
                ON T.[ContentHash] = S.[ContentHash]
                WHEN NOT MATCHED BY TARGET THEN
                    INSERT ([Modality], [Subtype], [ContentHash], [AtomicValue], [ReferenceCount], [TenantId])
                    VALUES ('image', 'rgba-pixel', S.[ContentHash], S.[AtomicValue], 0, @TenantId);

                INSERT INTO #PixelToAtomId ([R], [G], [B], [A], [AtomId])
                SELECT up.[R], up.[G], up.[B], up.[A], a.[AtomId]
                FROM #UniquePixels up
                JOIN [dbo].[Atom] a ON a.[ContentHash] = up.[ContentHash];

                UPDATE a
                SET a.[ReferenceCount] = a.[ReferenceCount] + pixel_count
                FROM [dbo].[Atom] a
                JOIN (
                    SELECT pta.[AtomId], COUNT(*) AS pixel_count
                    FROM #ChunkPixels cp
                    JOIN #PixelToAtomId pta 
                        ON cp.[R] = pta.[R] 
                        AND cp.[G] = pta.[G] 
                        AND cp.[B] = pta.[B] 
                        AND cp.[A] = pta.[A]
                    GROUP BY pta.[AtomId]
                ) AS counts ON a.[AtomId] = counts.[AtomId];

                -- Insert XYZM structural representation with Self-Indexing Geometry
                -- CRITICAL: Store Hilbert value in M dimension for "Self-Aware Atoms"
                INSERT INTO [dbo].[AtomComposition] (
                    [ParentAtomId], 
                    [ComponentAtomId], 
                    [SequenceIndex], 
                    [SpatialKey]
                )
                SELECT 
                    @ParentAtomId,
                    pta.[AtomId],
                    cp.[SequenceIndex],
                    -- MANDATORY: Use STGeomFromText to preserve M-value (Hilbert Index)
                    geometry::STGeomFromText(
                        'POINT (' +
                        CAST(cp.[PositionX] AS VARCHAR(20)) + ' ' +
                        CAST(cp.[PositionY] AS VARCHAR(20)) + ' ' +
                        '0 ' +  -- Z (Layer/Depth - could be channel index for multi-channel images)
                        CAST(dbo.fn_ComputeHilbertValue(
                            geometry::Point(cp.[PositionX], cp.[PositionY], 0), 
                            16  -- 16-bit precision for image dimensions up to 65536x65536
                        ) AS VARCHAR(20)) +  -- M (Hilbert Index for cache locality)
                        ')',
                        0  -- SRID
                    )
                FROM #ChunkPixels cp
                JOIN #PixelToAtomId pta 
                    ON cp.[R] = pta.[R] 
                    AND cp.[G] = pta.[G] 
                    AND cp.[B] = pta.[B] 
                    AND cp.[A] = pta.[A]
                ORDER BY dbo.fn_ComputeHilbertValue(
                    geometry::Point(cp.[PositionX], cp.[PositionY], 0), 
                    16
                );  -- Pre-sort by Hilbert for optimal Columnstore compression

            COMMIT TRANSACTION;

            SET @CurrentAtomOffset = @CurrentAtomOffset + @AtomChunkSize;
            SET @TotalAtomsProcessed = @TotalAtomsProcessed + @RowsInChunk;
            
            UPDATE dbo.IngestionJob 
            SET CurrentAtomOffset = @CurrentAtomOffset, 
                TotalAtomsProcessed = @TotalAtomsProcessed,
                LastUpdatedAt = SYSUTCDATETIME()
            WHERE IngestionJobId = @IngestionJobId;

        END TRY
        BEGIN CATCH
            IF (XACT_STATE() <> 0) ROLLBACK TRANSACTION;
            
            DECLARE @Error NVARCHAR(MAX) = ERROR_MESSAGE();
            UPDATE dbo.IngestionJob 
            SET JobStatus = 'Failed', ErrorMessage = @Error
            WHERE IngestionJobId = @IngestionJobId;
            
            DROP TABLE IF EXISTS #ChunkPixels;
            DROP TABLE IF EXISTS #UniquePixels;
            DROP TABLE IF EXISTS #PixelToAtomId;
            
            RAISERROR(@Error, 16, 1);
            RETURN -1;
        END CATCH
    END

    UPDATE dbo.IngestionJob 
    SET JobStatus = 'Complete', LastUpdatedAt = SYSUTCDATETIME() 
    WHERE IngestionJobId = @IngestionJobId;

    DROP TABLE #ChunkPixels;
    DROP TABLE #UniquePixels;
    DROP TABLE #PixelToAtomId;

    RETURN 0;
END
GO
