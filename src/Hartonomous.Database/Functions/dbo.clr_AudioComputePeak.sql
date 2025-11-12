CREATE FUNCTION dbo.clr_AudioComputePeak(@audio VARBINARY(MAX), @channels INT)
RETURNS FLOAT
AS EXTERNAL NAME SqlClrFunctions.[SqlClrFunctions.AudioProcessing].AudioComputePeak;