CREATE FUNCTION dbo.fn_GetComponentCount(@streamData VARBINARY(MAX))
RETURNS INT
AS EXTERNAL NAME SqlClrFunctions.[SqlClrFunctions.StreamOrchestrator].fn_GetComponentCount;