CREATE FUNCTION dbo.fn_ComputeEmbedding(@atomId BIGINT, @modelId INT, @tenantId INT)
RETURNS VARBINARY(MAX)
AS EXTERNAL NAME SqlClrFunctions.[SqlClrFunctions.EmbeddingFunctions].fn_ComputeEmbedding;