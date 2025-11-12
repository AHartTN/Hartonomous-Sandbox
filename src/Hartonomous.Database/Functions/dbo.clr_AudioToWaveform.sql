CREATE FUNCTION dbo.clr_AudioToWaveform(@audio VARBINARY(MAX), @channels INT, @sampleRate INT, @maxPoints INT)
RETURNS GEOMETRY
AS EXTERNAL NAME SqlClrFunctions.[SqlClrFunctions.AudioProcessing].AudioToWaveform;