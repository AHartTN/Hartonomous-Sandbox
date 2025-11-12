CREATE FUNCTION dbo.clr_VectorCosineSimilarity(@v1 VARBINARY(MAX), @v2 VARBINARY(MAX))
RETURNS FLOAT
AS EXTERNAL NAME SqlClrFunctions.[SqlClrFunctions.VectorOperations].VectorCosineSimilarity;