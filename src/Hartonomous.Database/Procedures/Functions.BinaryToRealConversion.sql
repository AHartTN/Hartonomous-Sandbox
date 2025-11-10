-- Utility function to interpret 4-byte IEEE-754 real values stored as VARBINARY.

CREATE OR ALTER FUNCTION dbo.ConvertVarbinary4ToReal (@BinaryReal VARBINARY(4))
RETURNS REAL
AS
BEGIN
    IF @BinaryReal IS NULL
        RETURN NULL;




    RETURN @Sign * (1.0 + CAST(@Mantissa AS REAL) * POWER(CAST(2 AS REAL), -23)) * POWER(CAST(2 AS REAL), @Exponent);
END;
