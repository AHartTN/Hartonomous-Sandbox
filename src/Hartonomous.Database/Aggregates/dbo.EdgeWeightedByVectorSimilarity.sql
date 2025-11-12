CREATE AGGREGATE dbo.EdgeWeightedByVectorSimilarity(@sourceVector NVARCHAR(MAX), @targetVector NVARCHAR(MAX))
RETURNS FLOAT
EXTERNAL NAME [SqlClrFunctions].[SqlClrFunctions.EdgeWeightedByVectorSimilarity];