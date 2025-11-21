-- =============================================
-- CLR Function: clr_ImageLuminanceHistogram
-- Description: Generates a luminance histogram for an image
-- =============================================
CREATE FUNCTION [dbo].[clr_ImageLuminanceHistogram]
(
    @imageData VARBINARY(MAX),
    @width INT,
    @height INT,
    @binCount INT
)
RETURNS NVARCHAR(MAX)
AS EXTERNAL NAME [Hartonomous.Clr].[Hartonomous.Clr.ImageProcessing].[ImageLuminanceHistogram]
GO
