CREATE AGGREGATE dbo.GraphPathVectorSummary(@nodeVector NVARCHAR(MAX), @pathDepth INT)
RETURNS NVARCHAR(MAX)
EXTERNAL NAME [SqlClrFunctions].[SqlClrFunctions.GraphPathVectorSummary];