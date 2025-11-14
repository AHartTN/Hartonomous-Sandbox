-- =============================================
-- Atomic Audio Decomposition
-- =============================================
-- Decomposes audio into deduplicated atomic amplitude values
-- using the new AtomRelations architecture.
--
-- Instead of AudioFrames table, creates:
-- 1. Atoms for each unique amplitude bucket (quantized)
-- 2. AtomRelations linking parent audio to frame atoms
-- 3. Importance based on RMS energy (louder = more important)
-- =============================================

CREATE PROCEDURE dbo.sp_AtomizeAudio_Atomic
    @ParentAtomId BIGINT,
    @TenantId INT = 0,
    @FrameDurationMs INT = 100,  -- 100ms frames (10 FPS)
    @AmplitudeBuckets INT = 256,  -- Quantize to 8-bit levels
    @ComputeImportance BIT = 1
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;
    
    BEGIN TRY
        -- Retrieve audio metadata
        DECLARE @Metadata NVARCHAR(MAX);
        DECLARE @DurationMs INT, @SampleRate INT, @Channels INT;
        
        SELECT @Metadata = CAST(lob.Metadata AS NVARCHAR(MAX))
        FROM dbo.Atoms a
        LEFT JOIN dbo.AtomsLOB lob ON a.AtomId = lob.AtomId
        WHERE a.AtomId = @ParentAtomId AND a.TenantId = @TenantId;
        
        SET @DurationMs = JSON_VALUE(@Metadata, '$.durationMs');
        SET @SampleRate = JSON_VALUE(@Metadata, '$.sampleRate');
        SET @Channels = JSON_VALUE(@Metadata, '$.channels');
        
        IF @DurationMs IS NULL OR @SampleRate IS NULL
        BEGIN
            RAISERROR('Audio metadata must include durationMs and sampleRate', 16, 1);
            RETURN -1;
        END
        
        IF @Channels IS NULL SET @Channels = 1;  -- Default mono
        
        -- Calculate frame count
        DECLARE @FrameCount INT = CEILING(@DurationMs * 1.0 / @FrameDurationMs);
        
        -- Extract frame data using CLR function
        -- Returns table: (FrameIdx INT, Channel INT, RMS FLOAT, PeakAmplitude FLOAT)
        DECLARE @Frames TABLE (
            FrameIdx INT NOT NULL,
            Channel INT NOT NULL,
            RMS FLOAT NOT NULL,
            PeakAmplitude FLOAT NOT NULL,
            QuantizedRMS TINYINT NULL,
            ContentHash BINARY(32) NULL,
            AtomId BIGINT NULL,
            PRIMARY KEY (FrameIdx, Channel)
        );
        
        -- Retrieve audio binary data
        DECLARE @AudioData VARBINARY(MAX);
        SELECT @AudioData = lob.ComponentStream
        FROM dbo.AtomsLOB lob
        WHERE lob.AtomId = @ParentAtomId;
        
        IF @AudioData IS NULL
        BEGIN
            RAISERROR('Audio binary data not found in AtomsLOB', 16, 1);
            RETURN -1;
        END
        
        -- Extract frames using production CLR function
        INSERT INTO @Frames (FrameIdx, Channel, RMS, PeakAmplitude)
        SELECT FrameIdx, Channel, RMS, PeakAmplitude
        FROM dbo.clr_ExtractAudioFrames(@AudioData, @FrameDurationMs, @SampleRate);
        
        -- Quantize RMS to reduce atom count (lossy compression)
        UPDATE @Frames
        SET QuantizedRMS = CAST(RMS * (@AmplitudeBuckets - 1) AS TINYINT);
        
        -- Compute ContentHash for each unique quantized RMS
        UPDATE @Frames
        SET ContentHash = HASHBYTES('SHA2_256', 
            CAST(QuantizedRMS AS BINARY(1))
        );
        
        -- Find or create atomic amplitude values (deduplicated)
        BEGIN TRANSACTION;
        
        MERGE dbo.Atoms AS target
        USING (
            SELECT DISTINCT ContentHash, QuantizedRMS
            FROM @Frames
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
                'audio',
                'amplitude_quantized',
                CAST(source.QuantizedRMS AS BINARY(1)),
                'amp(' + CAST(source.QuantizedRMS AS NVARCHAR(3)) + '/' + 
                         CAST(@AmplitudeBuckets AS NVARCHAR(3)) + ')',
                @TenantId,
                0  -- Will increment below
            );
        
        -- Get AtomIds for all amplitude values
        UPDATE f
        SET f.AtomId = a.AtomId
        FROM @Frames f
        INNER JOIN dbo.Atoms a ON a.ContentHash = f.ContentHash;
        
        -- Create AtomRelations for each frame
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
            CoordT,
            TenantId
        )
        SELECT 
            @ParentAtomId,
            f.AtomId,
            'audio_frame',
            f.FrameIdx,
            1.0,  -- Weight (uniform for frames)
            CASE 
                WHEN @ComputeImportance = 1 THEN
                    -- Importance = RMS energy (louder = more important)
                    f.RMS
                ELSE 1.0
            END,
            -- Confidence = inverse of peak/RMS ratio (consistent loudness = high confidence)
            CASE 
                WHEN f.PeakAmplitude > 0 THEN 1.0 - ABS(f.PeakAmplitude - f.RMS) / f.PeakAmplitude
                ELSE 1.0
            END,
            f.FrameIdx * 1.0 / @FrameCount,  -- X = temporal position
            f.Channel * 1.0 / @Channels,      -- Y = channel (0=left, 1=right)
            f.RMS,                             -- Z = amplitude
            (f.FrameIdx * @FrameDurationMs) * 1.0 / @DurationMs,  -- T = normalized timestamp
            @TenantId
        FROM @Frames f;
        
        -- Update reference counts
        UPDATE a
        SET ReferenceCount = ReferenceCount + frame_count
        FROM dbo.Atoms a
        INNER JOIN (
            SELECT AtomId, COUNT(*) AS frame_count
            FROM @Frames
            GROUP BY AtomId
        ) AS counts ON counts.AtomId = a.AtomId;
        
        COMMIT TRANSACTION;
        
        DECLARE @TotalFrames INT = @@ROWCOUNT;
        DECLARE @UniqueAmplitudes INT = (SELECT COUNT(DISTINCT AtomId) FROM @Frames);
        DECLARE @DeduplicationRatio FLOAT = 
            CASE WHEN @TotalFrames > 0 
            THEN (1.0 - (@UniqueAmplitudes * 1.0 / @TotalFrames)) * 100 
            ELSE 0 END;
        
        SELECT 
            @ParentAtomId AS ParentAtomId,
            @TotalFrames AS TotalFrames,
            @UniqueAmplitudes AS UniqueAmplitudes,
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
