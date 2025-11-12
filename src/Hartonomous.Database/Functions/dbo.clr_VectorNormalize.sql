CREATE FUNCTION dbo.clr_VectorNormalize(@vector VARBINARY(MAX))
RETURNS VARBINARY(MAX)
AS EXTERNAL NAME SqlClrFunctions.[SqlClrFunctions.VectorOperations].VectorNormalize;