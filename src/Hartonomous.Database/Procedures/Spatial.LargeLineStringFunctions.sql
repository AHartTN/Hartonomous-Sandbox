/*
 * Large LINESTRING Creation Functions for Model Weights
 * Bypasses NetTopologySuite serialization limits by using SqlGeometryBuilder in CLR
 * 
 * Purpose: Store massive neural network tensors (millions of parameters) as GEOMETRY LINESTRING
 * without hitting MemoryStream capacity limits during serialization.
 */

USE Hartonomous;
GO



-- Create function: Single LINESTRING from float array
IF OBJECT_ID('dbo.CreateLineStringFromWeights', 'FS') IS NOT NULL DROP FUNCTION dbo.CreateLineStringFromWeights;
GO
CREATE FUNCTION dbo.CreateLineStringFromWeights(
    @weightsData VARBINARY(MAX),  -- Binary representation of float[] array
    @srid INT = 0
)
RETURNS GEOMETRY
AS EXTERNAL NAME SqlClrFunctions.[SqlClrFunctions.SpatialOperations].CreateLineStringFromWeights;
GO

-- Create function: MULTILINESTRING from float array with chunking
IF OBJECT_ID('dbo.CreateMultiLineStringFromWeights', 'FS') IS NOT NULL DROP FUNCTION dbo.CreateMultiLineStringFromWeights;
GO
CREATE FUNCTION dbo.CreateMultiLineStringFromWeights(
    @weightsData VARBINARY(MAX),  -- Binary representation of float[] array
    @srid INT = 0,
    @pointsPerSegment INT = 100000  -- Chunk size for segmentation
)
RETURNS GEOMETRY
AS EXTERNAL NAME SqlClrFunctions.[SqlClrFunctions.SpatialOperations].CreateMultiLineStringFromWeights;
GO

-- Test with small array first
DECLARE @testData VARBINARY(MAX);
DECLARE @floats TABLE (idx INT, val FLOAT);

-- Generate 100 test points
INSERT INTO @floats
SELECT 
    ROW_NUMBER() OVER (ORDER BY (SELECT NULL)) - 1 AS idx,
    RAND(CHECKSUM(NEWID())) * 10 - 5 AS val  -- Random values between -5 and 5
FROM master.dbo.spt_values
WHERE type = 'P' AND number < 100;

-- Convert to binary (float array)
-- SQL Server doesn't have direct float[] serialization, so we'll test in C# first
-- This test is commented out until we integrate from C# side

-- PRINT 'Large LINESTRING functions deployed successfully!';
-- PRINT 'Use dbo.CreateLineStringFromWeights() for single LINESTRING';
-- PRINT 'Use dbo.CreateMultiLineStringFromWeights() for chunked MULTILINESTRING';
GO
