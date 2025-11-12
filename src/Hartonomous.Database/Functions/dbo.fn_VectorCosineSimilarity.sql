CREATE FUNCTION dbo.fn_VectorCosineSimilarity(
    @vec1 VECTOR(1998),
    @vec2 VECTOR(1998)
)
RETURNS FLOAT
AS
BEGIN
    IF @vec1 IS NULL OR @vec2 IS NULL
        RETURN NULL;

    RETURN 1.0 - VECTOR_DISTANCE('cosine', @vec1, @vec2);
END;