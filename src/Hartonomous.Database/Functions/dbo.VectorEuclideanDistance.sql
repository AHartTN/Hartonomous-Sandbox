CREATE FUNCTION dbo.VectorEuclideanDistance (@v1 VARBINARY(MAX), @v2 VARBINARY(MAX))
RETURNS FLOAT
AS
BEGIN
    RETURN dbo.clr_VectorEuclideanDistance(@v1, @v2);
END;