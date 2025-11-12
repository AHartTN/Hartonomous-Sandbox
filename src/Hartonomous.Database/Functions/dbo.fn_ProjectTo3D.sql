CREATE FUNCTION dbo.fn_ProjectTo3D(@vector VARBINARY(MAX))
RETURNS GEOMETRY
AS EXTERNAL NAME SqlClrFunctions.[SqlClrFunctions.SpatialOperations].fn_ProjectTo3D;