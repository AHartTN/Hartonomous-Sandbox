-- =============================================
-- CLR Function: clr_GenerateHarmonicTone
-- Description: Generates a harmonic tone with configurable harmonics
-- =============================================
CREATE FUNCTION [dbo].[clr_GenerateHarmonicTone]
(
    @fundamentalHz FLOAT,
    @durationMs INT,
    @sampleRate INT,
    @channelCount INT,
    @amplitude FLOAT,
    @secondHarmonicLevel FLOAT,
    @thirdHarmonicLevel FLOAT
)
RETURNS VARBINARY(MAX)
AS EXTERNAL NAME [Hartonomous.Clr].[Hartonomous.Clr.AudioProcessing].[GenerateHarmonicTone]
GO
