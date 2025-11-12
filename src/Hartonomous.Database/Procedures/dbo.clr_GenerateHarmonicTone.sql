CREATE FUNCTION dbo.clr_GenerateHarmonicTone(
    @fundamentalHz FLOAT,
    @durationMs INT,
    @sampleRate INT,
    @channelCount INT,
    @amplitude FLOAT,
    @secondHarmonic FLOAT = NULL,
    @thirdHarmonic FLOAT = NULL
)
RETURNS VARBINARY(MAX)
AS EXTERNAL NAME SqlClrFunctions.[SqlClrFunctions.AudioProcessing].GenerateHarmonicTone;
GO
