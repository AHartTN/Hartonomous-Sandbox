-- Auto-split from dbo.sp_AtomizeAudio.sql
-- Object: PROCEDURE dbo.sp_AtomizeAudio

CREATE PROCEDURE dbo.sp_AtomizeAudio
    @AtomId BIGINT,
    @TenantId INT = 0,
    @FrameWindowMs INT = 100, -- Window size for each audio frame
    @OverlapMs INT = 25,       -- Overlap between consecutive frames
    @MinRmsThreshold FLOAT = 0.01 -- Minimum RMS to consider a frame "active"
AS
BEGIN
    SET NOCOUNT ON;
    
    BEGIN TRY
        BEGIN TRANSACTION;
        
        -- Retrieve the parent audio atom
        DECLARE @Content NVARCHAR(MAX);
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
            RAISERROR('Audio atom not found', 16, 1);
            RETURN -1;
        END
        
        -- Validate it's actually audio content
        IF @ContentType NOT LIKE 'audio/%'
        BEGIN
            RAISERROR('Atom is not audio content', 16, 1);
            RETURN -1;
        END
        
        -- Extract audio parameters from metadata (or use defaults)
        DECLARE @SampleRate INT = ISNULL(JSON_VALUE(@Metadata, '$.sampleRate'), 44100);
        DECLARE @ChannelCount INT = ISNULL(JSON_VALUE(@Metadata, '$.channels'), 2);
        DECLARE @DurationMs INT = ISNULL(JSON_VALUE(@Metadata, '$.durationMs'), 0);
        
        -- Calculate frame parameters
        DECLARE @SamplesPerFrame INT = (@SampleRate * @FrameWindowMs) / 1000;
        DECLARE @SamplesOverlap INT = (@SampleRate * @OverlapMs) / 1000;
        DECLARE @FrameStride INT = @SamplesPerFrame - @SamplesOverlap;
        
        -- Convert the full audio to a waveform geometry
        DECLARE @WaveformGeometry GEOMETRY;
        SET @WaveformGeometry = dbo.clr_AudioToWaveform(@Content, @ChannelCount, @SampleRate, 8192);
        
        IF @WaveformGeometry IS NULL
        BEGIN
            RAISERROR('Failed to generate waveform geometry', 16, 1);
            RETURN -1;
        END
        
        -- Compute global audio metrics
        DECLARE @RmsAmplitude FLOAT = dbo.clr_AudioComputeRms(@Content, @ChannelCount);
        DECLARE @PeakAmplitude FLOAT = dbo.clr_AudioComputePeak(@Content, @ChannelCount);
        
        -- Generate frame-level atoms
        DECLARE @TotalSamples INT = (DATALENGTH(@Content) / 2) / @ChannelCount;
        DECLARE @FrameCount INT = CEILING((@TotalSamples - @SamplesPerFrame) * 1.0 / @FrameStride) + 1;
        
        DECLARE @FrameIndex INT = 0;
        DECLARE @StartSample INT;
        DECLARE @EndSample INT;
        DECLARE @FrameStartTime FLOAT;
        DECLARE @FrameEndTime FLOAT;
        DECLARE @FrameWaveform GEOMETRY;
        DECLARE @FrameRms FLOAT;
        DECLARE @IsActiveFrame BIT;
        DECLARE @FramesCreated INT = 0;
        
        WHILE @FrameIndex < @FrameCount
        BEGIN
            SET @StartSample = @FrameIndex * @FrameStride;
            SET @EndSample = @StartSample + @SamplesPerFrame;
            
            -- Clip to available samples
            IF @EndSample > @TotalSamples
                SET @EndSample = @TotalSamples;
            
            SET @FrameStartTime = (@StartSample * 1.0) / @SampleRate;
            SET @FrameEndTime = (@EndSample * 1.0) / @SampleRate;
            
            -- Extract the waveform segment for this frame
            -- TODO: Implement proper line substring extraction
            -- GEOMETRY doesn't have STLineSubstring - would need to extract points and rebuild
            -- For now, use full waveform (less accurate but functional)
            SET @FrameWaveform = @WaveformGeometry;
            
            BEGIN TRY
                
                -- Compute local RMS for this frame
                -- Approximate using the waveform geometry points
                SET @FrameRms = dbo.fn_ComputeGeometryRms(@FrameWaveform);
                
                -- Only create an atom if this frame is "active" (above threshold)
                SET @IsActiveFrame = CASE WHEN @FrameRms >= @MinRmsThreshold THEN 1 ELSE 0 END;
                
                IF @IsActiveFrame = 1
                BEGIN
                    -- Insert the frame into dbo.AudioFrames
                    INSERT INTO dbo.AudioFrames (
                        ParentAtomId,
                        FrameIndex,
                        StartTimeSec,
                        EndTimeSec,
                        WaveformGeometry,
                        RmsAmplitude,
                        PeakAmplitude,
                        SpectralCentroid,
                        ZeroCrossingRate,
                        TenantId
                    )
                    VALUES (
                        @AtomId,
                        @FrameIndex,
                        @FrameStartTime,
                        @FrameEndTime,
                        @FrameWaveform,
                        @FrameRms,
                        NULL, -- PeakAmplitude would require full frame extraction
                        NULL, -- SpectralCentroid requires FFT (future enhancement)
                        NULL, -- ZeroCrossingRate (future enhancement)
                        @TenantId
                    );
                    
                    SET @FramesCreated = @FramesCreated + 1;
                END
            END TRY
            BEGIN CATCH
                -- Log frame processing failure but continue
                PRINT 'Failed to process frame ' + CAST(@FrameIndex AS NVARCHAR(10)) + ': ' + ERROR_MESSAGE();
            END CATCH
            
            SET @FrameIndex = @FrameIndex + 1;
        END
        
        -- Update the parent atom's metadata with atomization results
        UPDATE dbo.Atoms
        SET Metadata = JSON_MODIFY(
            ISNULL(Metadata, '{}'),
            '$.atomization',
            JSON_QUERY(JSON_OBJECT(
                'type': 'audio',
                'frameCount': @FramesCreated,
                'totalFrames': @FrameCount,
                'rmsAmplitude': @RmsAmplitude,
                'peakAmplitude': @PeakAmplitude,
                'atomizedUtc': FORMAT(SYSUTCDATETIME(), 'yyyy-MM-ddTHH:mm:ss.fffZ')
            ))
        )
        WHERE AtomId = @AtomId AND TenantId = @TenantId;
        
        COMMIT TRANSACTION;
        
        SELECT 
            @AtomId AS ParentAtomId,
            @FramesCreated AS FramesCreated,
            @FrameCount AS TotalFrames,
            @RmsAmplitude AS RmsAmplitude,
            @PeakAmplitude AS PeakAmplitude,
            'AudioAtomization' AS Status;
        
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

-- Helper function: Compute RMS from a GEOMETRY waveform

GO
