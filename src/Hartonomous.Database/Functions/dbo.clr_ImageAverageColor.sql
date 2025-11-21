-- =============================================
-- CLR Function: clr_ImageAverageColor
-- Description: Computes the average color of an image
-- =============================================
CREATE FUNCTION [dbo].[clr_ImageAverageColor]
(
    @imageData VARBINARY(MAX),
    @width INT,
    @height INT
)
RETURNS NVARCHAR(50)
AS EXTERNAL NAME [Hartonomous.Clr].[Hartonomous.Clr.ImageProcessing].[ImageAverageColor]
GO
