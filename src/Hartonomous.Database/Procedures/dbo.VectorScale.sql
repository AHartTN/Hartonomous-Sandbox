CREATE FUNCTION dbo.VectorScale (@v VARBINARY(MAX), @scalar FLOAT)
RETURNS VARBINARY(MAX)
AS
BEGIN
    RETURN dbo.clr_VectorScale(@v, @scalar);
END;
GO