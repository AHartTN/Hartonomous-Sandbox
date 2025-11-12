CREATE FUNCTION dbo.fn_GenerateVideo(
    @modelId INT,
    @inputAtomIds NVARCHAR(MAX),
    @contextJson NVARCHAR(MAX),
    @maxTokens INT,
    @temperature FLOAT,
    @topK INT,
    @topP FLOAT,
    @tenantId INT
)
RETURNS BIGINT
AS EXTERNAL NAME SqlClrFunctions.[SqlClrFunctions.MultiModalGeneration].fn_GenerateVideo;