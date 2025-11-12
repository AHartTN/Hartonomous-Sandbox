CREATE FUNCTION dbo.fn_GenerateEnsemble(
    @modelIdsJson NVARCHAR(MAX),
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
AS EXTERNAL NAME SqlClrFunctions.[SqlClrFunctions.MultiModalGeneration].fn_GenerateEnsemble;