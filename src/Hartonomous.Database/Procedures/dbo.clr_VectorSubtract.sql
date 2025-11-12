CREATE FUNCTION dbo.clr_VectorSubtract(@v1 VARBINARY(MAX), @v2 VARBINARY(MAX))
RETURNS VARBINARY(MAX)
AS EXTERNAL NAME SqlClrFunctions.[SqlClrFunctions.VectorOperations].VectorSubtract;
GO
