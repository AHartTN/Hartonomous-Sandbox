CREATE FUNCTION dbo.VectorDotProduct (@v1 VARBINARY(MAX), @v2 VARBINARY(MAX))
RETURNS FLOAT
AS
BEGIN
    RETURN dbo.clr_VectorDotProduct(@v1, @v2);
END;