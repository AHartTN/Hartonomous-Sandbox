CREATE FUNCTION dbo.fn_GenerateWithAttention(
    @modelId INT,
    @inputAtomIds NVARCHAR(MAX),
    @contextJson NVARCHAR(MAX),
    @maxTokens INT,
    @temperature FLOAT,
    @topK INT,
    @topP FLOAT,
    @attentionHeads INT,
    @tenantId INT
)
RETURNS BIGINT
AS EXTERNAL NAME SqlClrFunctions.[SqlClrFunctions.AttentionGeneration].fn_GenerateWithAttention;