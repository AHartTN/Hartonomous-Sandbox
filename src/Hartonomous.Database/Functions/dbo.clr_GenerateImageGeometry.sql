-- =============================================
-- CLR Function: clr_GenerateImageGeometry
-- Description: Generates an image as geometry with guided diffusion
-- =============================================
CREATE FUNCTION [dbo].[clr_GenerateImageGeometry]
(
    @width INT,
    @height INT,
    @patchSize INT,
    @steps INT,
    @guidanceScale FLOAT,
    @guideX FLOAT,
    @guideY FLOAT,
    @guideZ FLOAT,
    @seed INT
)
RETURNS GEOMETRY
AS EXTERNAL NAME [Hartonomous.Clr].[Hartonomous.Clr.ImageProcessing].[GenerateImageGeometry]
GO
