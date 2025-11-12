CREATE FUNCTION dbo.clr_VectorLerp(@v1 VARBINARY(MAX), @v2 VARBINARY(MAX), @t FLOAT)
RETURNS VARBINARY(MAX)
AS EXTERNAL NAME SqlClrFunctions.[SqlClrFunctions.VectorOperations].VectorLerp;