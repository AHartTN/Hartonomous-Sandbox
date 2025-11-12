CREATE FUNCTION dbo.clr_GenerateImageGeometry(
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
AS EXTERNAL NAME SqlClrFunctions.[SqlClrFunctions.ImageGeneration].GenerateGuidedGeometry;