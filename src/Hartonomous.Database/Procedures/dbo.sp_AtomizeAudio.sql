-- sp_AtomizeAudio: Deep atomization for audio content
-- Breaks audio into AudioFrame atoms using CLR waveform analysis
-- This is Phase 2 of the atomization pipeline for audio/* content types

CREATE OR ALTER PROCEDURE dbo.sp_AtomizeAudio
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



        -- Calculate frame parameters



        -- Convert the full audio to a waveform geometry

        SET @WaveformGeometry = dbo.clr_AudioToWaveform(@Content, @ChannelCount, @SampleRate, 8192);
        
        IF @WaveformGeometry IS NULL
        BEGIN
            RAISERROR('Failed to generate waveform geometry', 16, 1);
            RETURN -1;
        END
        
        -- Compute global audio metrics


        -- Generate frame-level atoms











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
            -- Use GEOMETRY::STLineSubstring to extract the time slice


            BEGIN TRY
                -- Extract the line segment for this time window
                SET @FrameWaveform = @WaveformGeometry.STLineSubstring(@StartFraction, @EndFraction);
                
                -- Compute local RMS for this frame
                -- Approximate using the waveform geometry points
                SET @FrameRms = dbo.fn_ComputeGeometryRms(@FrameWaveform);
                
                -- Only create an atom if this frame is "active" (above threshold)
                SET @IsActiveFrame = CASE WHEN @FrameRms >= @MinRmsThreshold THEN 1 ELSE 0 END;
                
                IF @IsActiveFrame = 1
                BEGIN
                    -- Insert the frame into dbo.AudioFrames
                    
                    
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

        RAISERROR(@ErrorMessage, 16, 1);
        RETURN -1;
    END CATCH
END;

-- Helper function: Compute RMS from a GEOMETRY waveform
CREATE OR ALTER FUNCTION dbo.fn_ComputeGeometryRms(@Waveform GEOMETRY)
RETURNS FLOAT
AS
BEGIN
    IF @Waveform IS NULL OR @Waveform.STGeometryType() <> 'LINESTRING'
        RETURN 0;




    WHILE @PointIndex <= @PointCount
    BEGIN
        -- Y coordinate is amplitude
        SET @Amplitude = @Waveform.STPointN(@PointIndex).STY.Value;
        SET @SumSquares = @SumSquares + (@Amplitude * @Amplitude);
        SET @PointIndex = @PointIndex + 1;
    END
    
    RETURN SQRT(@SumSquares / @PointCount);
END;
