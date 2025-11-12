CREATE FUNCTION dbo.VectorNormalize (@v VARBINARY(MAX))
RETURNS VARBINARY(MAX)
AS
BEGIN
    RETURN dbo.clr_VectorNormalize(@v);
END;