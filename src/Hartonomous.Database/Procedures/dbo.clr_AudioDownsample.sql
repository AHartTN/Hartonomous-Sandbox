CREATE FUNCTION dbo.clr_AudioDownsample(@audio VARBINARY(MAX), @channels INT, @factor INT)
RETURNS VARBINARY(MAX)
AS EXTERNAL NAME SqlClrFunctions.[SqlClrFunctions.AudioProcessing].AudioDownsample;
GO
