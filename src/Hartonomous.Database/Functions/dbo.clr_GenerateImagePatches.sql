-- =============================================
-- CLR Function: clr_GenerateImagePatches (TVF)
-- Description: Generates image patches with guided diffusion
-- =============================================
CREATE FUNCTION [dbo].[clr_GenerateImagePatches]
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
AS EXTERNAL NAME [Hartonomous.Clr].[Hartonomous.Clr.ImageProcessing].[GenerateImagePatches]
GO
