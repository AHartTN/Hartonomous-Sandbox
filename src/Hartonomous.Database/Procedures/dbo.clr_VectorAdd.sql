CREATE FUNCTION dbo.clr_VectorAdd(@v1 VARBINARY(MAX), @v2 VARBINARY(MAX))
RETURNS VARBINARY(MAX)
AS EXTERNAL NAME SqlClrFunctions.[SqlClrFunctions.VectorOperations].VectorAdd;
GO
