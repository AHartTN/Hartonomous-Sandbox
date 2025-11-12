CREATE FUNCTION dbo.clr_VectorNorm(@vector VARBINARY(MAX))
RETURNS FLOAT
AS EXTERNAL NAME SqlClrFunctions.[SqlClrFunctions.VectorOperations].VectorNorm;
GO
