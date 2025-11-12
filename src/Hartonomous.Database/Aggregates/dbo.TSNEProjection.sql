CREATE AGGREGATE dbo.TSNEProjection(@vector NVARCHAR(MAX), @perplexity FLOAT)
RETURNS NVARCHAR(MAX)
EXTERNAL NAME [SqlClrFunctions].[SqlClrFunctions.TSNEProjection];