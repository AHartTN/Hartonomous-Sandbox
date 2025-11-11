CREATE AGGREGATE dbo.VectorKMeansCluster(@vector NVARCHAR(MAX), @k INT)
RETURNS NVARCHAR(MAX)
EXTERNAL NAME [SqlClrFunctions].[SqlClrFunctions.VectorKMeansCluster];
GO