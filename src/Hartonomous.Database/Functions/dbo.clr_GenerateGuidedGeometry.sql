-- =============================================
-- CLR Function: clr_GenerateGuidedGeometry
-- Description: Generates guided geometry from diffusion process
-- =============================================
CREATE FUNCTION [dbo].[clr_GenerateGuidedGeometry]
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
AS EXTERNAL NAME [Hartonomous.Clr].[Hartonomous.Clr.ImageGeneration].[GenerateGuidedGeometry]
GO
