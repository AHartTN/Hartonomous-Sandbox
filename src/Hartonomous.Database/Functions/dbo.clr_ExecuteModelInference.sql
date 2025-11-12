CREATE FUNCTION dbo.clr_ExecuteModelInference(@modelId INT, @embeddingVector VARBINARY(MAX))
RETURNS NVARCHAR(MAX)
AS EXTERNAL NAME SqlClrFunctions.[SqlClrFunctions.ModelInference].ExecuteModelInference;