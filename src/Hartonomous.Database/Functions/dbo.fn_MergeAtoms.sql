CREATE FUNCTION dbo.fn_MergeAtoms(@primaryAtomId BIGINT, @duplicateAtomId BIGINT, @tenantId INT)
RETURNS BIGINT
AS EXTERNAL NAME SqlClrFunctions.[SqlClrFunctions.EmbeddingFunctions].fn_MergeAtoms;