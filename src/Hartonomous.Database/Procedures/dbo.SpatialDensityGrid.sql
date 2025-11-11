CREATE AGGREGATE dbo.SpatialDensityGrid(@point GEOMETRY, @gridSize FLOAT)
RETURNS NVARCHAR(MAX)
EXTERNAL NAME [SqlClrFunctions].[SqlClrFunctions.SpatialDensityGrid];
GO