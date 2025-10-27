-- =============================================
-- Convert Varbinary to Real
-- =============================================

USE Hartonomous;
GO

CREATE OR ALTER FUNCTION dbo.ConvertVarbinary4ToReal (@BinaryReal VARBINARY(4))
RETURNS REAL
AS
BEGIN
    IF @BinaryReal IS NULL RETURN NULL;

    DECLARE @IntVal INT;
    SET @IntVal = CAST(@BinaryReal AS INT);

    -- Extract sign, exponent, and mantissa
    DECLARE @Sign REAL = IIF((@IntVal & 0x80000000) = 0, 1.0, -1.0);
    DECLARE @Exponent INT = ((@IntVal & 0x7F800000) / 0x00800000) - 127;
    DECLARE @Mantissa INT = (@IntVal & 0x007FFFFF);

    -- Reconstruct the real value
    RETURN @Sign * (1.0 + CAST(@Mantissa AS REAL) * POWER(CAST(2 AS REAL), -23)) * POWER(CAST(2 AS REAL), @Exponent);
END;
GO
