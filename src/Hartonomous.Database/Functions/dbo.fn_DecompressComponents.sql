CREATE FUNCTION dbo.fn_DecompressComponents(@streamData VARBINARY(MAX))
RETURNS VARBINARY(MAX)
AS EXTERNAL NAME SqlClrFunctions.[SqlClrFunctions.StreamOrchestrator].fn_DecompressComponents;