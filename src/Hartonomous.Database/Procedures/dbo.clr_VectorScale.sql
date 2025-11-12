CREATE FUNCTION dbo.clr_VectorScale(@vector VARBINARY(MAX), @scalar FLOAT)
RETURNS VARBINARY(MAX)
AS EXTERNAL NAME SqlClrFunctions.[SqlClrFunctions.VectorOperations].VectorScale;
GO
