-- Utility function to interpret 4-byte IEEE-754 real values stored as VARBINARY.
GO

CREATE OR ALTER FUNCTION dbo.ConvertVarbinary4ToReal (@BinaryReal VARBINARY(4))
RETURNS REAL
AS
BEGIN
    IF @BinaryReal IS NULL
        RETURN NULL;

    DECLARE @IntVal INT = CAST(@BinaryReal AS INT);
    DECLARE @Sign REAL = CASE WHEN (@IntVal & 0x80000000) = 0 THEN 1.0 ELSE -1.0 END;
    DECLARE @Exponent INT = ((@IntVal & 0x7F800000) / 0x00800000) - 127;
    DECLARE @Mantissa INT = (@IntVal & 0x007FFFFF);

    RETURN @Sign * (1.0 + CAST(@Mantissa AS REAL) * POWER(CAST(2 AS REAL), -23)) * POWER(CAST(2 AS REAL), @Exponent);
END;
GO
