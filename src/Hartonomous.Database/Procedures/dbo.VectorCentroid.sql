CREATE AGGREGATE dbo.VectorCentroid(@vector NVARCHAR(MAX))
RETURNS NVARCHAR(MAX)
EXTERNAL NAME [SqlClrFunctions].[SqlClrFunctions.VectorCentroid];
GO