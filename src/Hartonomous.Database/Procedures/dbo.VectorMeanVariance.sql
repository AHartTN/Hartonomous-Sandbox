CREATE AGGREGATE dbo.VectorMeanVariance(@vector NVARCHAR(MAX))
RETURNS NVARCHAR(MAX)
EXTERNAL NAME [SqlClrFunctions].[SqlClrFunctions.VectorMeanVariance];
GO