CREATE AGGREGATE dbo.GeometricMedian(@vector NVARCHAR(MAX))
RETURNS NVARCHAR(MAX)
EXTERNAL NAME [SqlClrFunctions].[SqlClrFunctions.GeometricMedian];
GO