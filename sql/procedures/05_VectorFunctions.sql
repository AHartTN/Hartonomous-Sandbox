-- =============================================
-- Vector Functions
-- =============================================

USE Hartonomous;
GO

CREATE OR ALTER FUNCTION dbo.VectorDotProduct (@v1 VARBINARY(MAX), @v2 VARBINARY(MAX))
RETURNS FLOAT
AS
BEGIN
    DECLARE @dot_product FLOAT = 0;
    DECLARE @i INT = 1;
    DECLARE @v1_len INT = DATALENGTH(@v1);
    DECLARE @v2_len INT = DATALENGTH(@v2);

    -- Ensure the vectors are of the same length
    IF @v1_len <> @v2_len
    BEGIN
        RETURN NULL;
    END

    WHILE @i <= @v1_len
    BEGIN
        -- Extract 4 bytes from each vector and convert to a real
        DECLARE @f1 REAL = dbo.ConvertVarbinary4ToReal(SUBSTRING(@v1, @i, 4));
        DECLARE @f2 REAL = dbo.ConvertVarbinary4ToReal(SUBSTRING(@v2, @i, 4));

        -- Add the product to the dot product
        SET @dot_product = @dot_product + (@f1 * @f2);

        -- Move to the next 4-byte chunk
        SET @i = @i + 4;
    END

    RETURN @dot_product;
END
GO
