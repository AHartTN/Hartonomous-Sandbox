CREATE FUNCTION dbo.fn_VectorCosineSimilarity(
    @vec1 VARBINARY(MAX),
    @vec2 VARBINARY(MAX)
)
RETURNS FLOAT
AS
BEGIN
    IF @vec1 IS NULL OR @vec2 IS NULL
        RETURN NULL;

    RETURN 1.0 - VECTOR_DISTANCE('cosine', @vec1, @vec2);
END;
GO