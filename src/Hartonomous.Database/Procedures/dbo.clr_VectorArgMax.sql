CREATE FUNCTION dbo.clr_VectorArgMax(@vector VARBINARY(MAX))
RETURNS INT
AS EXTERNAL NAME SqlClrFunctions.[SqlClrFunctions.VectorOperations].VectorArgMax;
GO
