CREATE FUNCTION dbo.clr_GenerateSequence(
    @seedEmbedding VARBINARY(MAX),
    @modelsJson NVARCHAR(MAX),
    @maxTokens INT,
    @temperature FLOAT,
    @topK INT,
    @requiredModality NVARCHAR(64)
)
RETURNS TABLE (
    step_number INT,
    atom_id BIGINT,
    token NVARCHAR(400),
    score FLOAT,
    distance FLOAT,
    model_count INT,
    duration_ms INT
)
AS EXTERNAL NAME SqlClrFunctions.[SqlClrFunctions.GenerationFunctions].GenerateSequence;
GO
