CREATE FUNCTION dbo.fn_GetTimeWindow(@streamData VARBINARY(MAX))
RETURNS NVARCHAR(MAX)
AS EXTERNAL NAME SqlClrFunctions.[SqlClrFunctions.StreamOrchestrator].fn_GetTimeWindow;