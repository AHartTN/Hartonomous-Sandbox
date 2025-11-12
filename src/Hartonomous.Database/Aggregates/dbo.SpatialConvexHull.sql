CREATE AGGREGATE dbo.SpatialConvexHull(@point GEOMETRY)
RETURNS GEOMETRY
EXTERNAL NAME [SqlClrFunctions].[SqlClrFunctions.SpatialConvexHull];