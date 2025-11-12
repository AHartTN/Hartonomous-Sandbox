CREATE FUNCTION dbo.clr_CreateMultiLineStringFromWeights(@weightsJson NVARCHAR(MAX), @rows INT, @cols INT)
RETURNS GEOMETRY
AS EXTERNAL NAME SqlClrFunctions.[SqlClrFunctions.ModelIngestionFunctions].CreateMultiLineStringFromWeights;