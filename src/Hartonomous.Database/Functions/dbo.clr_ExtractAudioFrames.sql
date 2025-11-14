-- =============================================
-- CLR Table-Valued Function: Extract Audio Frames
-- =============================================
-- Production-grade WAV audio frame extraction using streaming CLR function.
-- Returns RMS energy and peak amplitude per frame for atomic decomposition.
-- =============================================

CREATE FUNCTION dbo.clr_ExtractAudioFrames
(
    @AudioData VARBINARY(MAX),
    @FrameDurationMs INT,
    @SampleRate INT
)
RETURNS TABLE
(
    FrameIdx INT,
    Channel INT,
    RMS FLOAT,
    PeakAmplitude FLOAT
)
AS EXTERNAL NAME [Hartonomous.Clr].[Hartonomous.SqlClr.AudioFrameExtractor].[ExtractFrames];
GO
