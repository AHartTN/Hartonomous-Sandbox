CREATE FUNCTION dbo.CreateMultiLineStringFromWeights(
    @weightsData VARBINARY(MAX),  -- Binary representation of float[] array
    @srid INT = 0,
    @pointsPerSegment INT = 100000  -- Chunk size for segmentation
)
RETURNS GEOMETRY
AS EXTERNAL NAME SqlClrFunctions.[SqlClrFunctions.SpatialOperations].CreateMultiLineStringFromWeights;
GO