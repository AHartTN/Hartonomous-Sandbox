CREATE FUNCTION dbo.clr_VectorEuclideanDistance(@v1 VARBINARY(MAX), @v2 VARBINARY(MAX))
RETURNS FLOAT
AS EXTERNAL NAME SqlClrFunctions.[SqlClrFunctions.VectorOperations].VectorEuclideanDistance;