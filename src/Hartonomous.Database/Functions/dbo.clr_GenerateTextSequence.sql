CREATE FUNCTION dbo.clr_GenerateTextSequence(
    @seedEmbedding VARBINARY(MAX),
    @modelsJson NVARCHAR(MAX),
    @maxTokens INT,
    @temperature FLOAT,
    @topK INT
)
RETURNS TABLE (
    atom_id BIGINT,
    token NVARCHAR(400),
    score FLOAT,
    distance FLOAT,
    model_count INT,
    duration_ms INT
)
AS EXTERNAL NAME SqlClrFunctions.[SqlClrFunctions.GenerationFunctions].GenerateTextSequence;