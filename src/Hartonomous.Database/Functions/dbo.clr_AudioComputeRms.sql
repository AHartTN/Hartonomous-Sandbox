CREATE FUNCTION dbo.clr_AudioComputeRms(@audio VARBINARY(MAX), @channels INT)
RETURNS FLOAT
AS EXTERNAL NAME SqlClrFunctions.[SqlClrFunctions.AudioProcessing].AudioComputeRms;