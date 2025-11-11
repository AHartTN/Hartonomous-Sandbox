CREATE FUNCTION dbo.CreateLineStringFromWeights(
    @weightsData VARBINARY(MAX),  -- Binary representation of float[] array
    @srid INT = 0
)
RETURNS GEOMETRY
AS EXTERNAL NAME SqlClrFunctions.[SqlClrFunctions.SpatialOperations].CreateLineStringFromWeights;
GO