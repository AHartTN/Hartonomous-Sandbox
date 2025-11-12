CREATE FUNCTION dbo.clr_GenerateImageFromShapes(
    @shapes GEOMETRY,
    @width INT,
    @height INT
)
RETURNS VARBINARY(MAX)
AS EXTERNAL NAME SqlClrFunctions.[SqlClrFunctions.ImageGeneration].GenerateImageFromShapes;