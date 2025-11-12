CREATE FUNCTION dbo.clr_VectorSoftmax(@vector VARBINARY(MAX))
RETURNS VARBINARY(MAX)
AS EXTERNAL NAME SqlClrFunctions.[SqlClrFunctions.VectorOperations].VectorSoftmax;