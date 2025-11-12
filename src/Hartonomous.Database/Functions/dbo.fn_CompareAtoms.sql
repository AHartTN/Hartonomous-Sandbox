CREATE FUNCTION dbo.fn_CompareAtoms(@atomId1 BIGINT, @atomId2 BIGINT, @tenantId INT)
RETURNS FLOAT
AS EXTERNAL NAME SqlClrFunctions.[SqlClrFunctions.EmbeddingFunctions].fn_CompareAtoms;