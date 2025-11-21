-- =============================================
-- CLR Function: clr_GenerateGuidedPatches (TVF)
-- Description: Generates guided diffusion patches
-- =============================================
CREATE FUNCTION [dbo].[clr_GenerateGuidedPatches]
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
RETURNS TABLE
(
    PatchIndex INT,
    X INT,
    Y INT,
    PatchData VARBINARY(MAX)
)
AS EXTERNAL NAME [Hartonomous.Clr].[Hartonomous.Clr.ImageGeneration].[GenerateGuidedPatches]
GO
