/*
 * Large LINESTRING Creation Functions for Model Weights
 * Bypasses NetTopologySuite serialization limits by using SqlGeometryBuilder in CLR
 * 
 * Purpose: Store massive neural network tensors (millions of parameters) as GEOMETRY LINESTRING
 * without hitting MemoryStream capacity limits during serialization.
 */

-- Create function: Single LINESTRING from float array

CREATE FUNCTION dbo.CreateLineStringFromWeights(
    @weightsData VARBINARY(MAX),  -- Binary representation of float[] array
    @srid INT = 0
)
RETURNS GEOMETRY
AS EXTERNAL NAME SqlClrFunctions.[SqlClrFunctions.SpatialOperations].CreateLineStringFromWeights;

-- Create function: MULTILINESTRING from float array with chunking

CREATE FUNCTION dbo.CreateMultiLineStringFromWeights(
    @weightsData VARBINARY(MAX),  -- Binary representation of float[] array
    @srid INT = 0,
    @pointsPerSegment INT = 100000  -- Chunk size for segmentation
)
RETURNS GEOMETRY
AS EXTERNAL NAME SqlClrFunctions.[SqlClrFunctions.SpatialOperations].CreateMultiLineStringFromWeights;

-- Test with small array first
