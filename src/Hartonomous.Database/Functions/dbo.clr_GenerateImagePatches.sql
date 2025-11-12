CREATE FUNCTION dbo.clr_GenerateImagePatches(
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
RETURNS TABLE (
    patch_x INT,
    patch_y INT,
    spatial_x FLOAT,
    spatial_y FLOAT,
    spatial_z FLOAT,
    patch GEOMETRY
)
AS EXTERNAL NAME SqlClrFunctions.[SqlClrFunctions.ImageGeneration].GenerateGuidedPatches;