CREATE FUNCTION dbo.VectorCosineSimilarity (@v1 VARBINARY(MAX), @v2 VARBINARY(MAX))
RETURNS FLOAT
AS
BEGIN
    RETURN dbo.clr_VectorCosineSimilarity(@v1, @v2);
END;
GO