-- =============================================
-- CLR Function: clr_GenerateImageFromShapes
-- Description: Generates an image from geometry shapes
-- =============================================
CREATE FUNCTION [dbo].[clr_GenerateImageFromShapes]
(
    @shapes GEOMETRY,
    @width INT,
    @height INT
)
RETURNS VARBINARY(MAX)
AS EXTERNAL NAME [Hartonomous.Clr].[Hartonomous.Clr.ImageGeneration].[GenerateImageFromShapes]
GO
