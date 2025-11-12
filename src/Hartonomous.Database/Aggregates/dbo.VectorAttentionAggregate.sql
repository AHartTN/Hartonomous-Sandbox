CREATE AGGREGATE dbo.VectorAttentionAggregate(
    @query NVARCHAR(MAX),
    @key NVARCHAR(MAX),
    @value NVARCHAR(MAX),
    @numHeads INT)
RETURNS NVARCHAR(MAX)
EXTERNAL NAME [SqlClrFunctions].[SqlClrFunctions.VectorAttentionAggregate];