-- =============================================
-- CLR Table-Valued Function: Extract Image Pixels
-- =============================================
-- Production-grade BMP pixel extraction using streaming CLR function.
-- Returns RGB values for each pixel with configurable stride sampling.
-- =============================================

CREATE FUNCTION dbo.clr_ExtractImagePixels
(
    @ImageData VARBINARY(MAX),
    @StrideX INT,
    @StrideY INT
)
RETURNS TABLE
(
    X INT,
    Y INT,
    R TINYINT,
    G TINYINT,
    B TINYINT
)
AS EXTERNAL NAME [Hartonomous.Clr].[Hartonomous.SqlClr.ImagePixelExtractor].[ExtractPixels];
GO
