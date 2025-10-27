-- =============================================
-- Generate Text Stored Procedure (with VECTOR)
-- =============================================

USE Hartonomous;
GO

CREATE OR ALTER PROCEDURE dbo.sp_GenerateText
    @prompt NVARCHAR(MAX),
    @max_tokens INT = 50
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @generated_text NVARCHAR(MAX) = @prompt;
    DECLARE @current_token NVARCHAR(100) = @prompt;
    DECLARE @next_token NVARCHAR(100);
    DECLARE @i INT = 0;

    WHILE @i < @max_tokens
    BEGIN
        -- Find the embedding for the current token
        DECLARE @current_embedding VECTOR(3);
        SELECT @current_embedding = embedding FROM dbo.TokenVocabulary WHERE token = @current_token;

        -- Find the most likely next token using vector_distance, excluding the current token
        SELECT TOP 1 @next_token = v2.token
        FROM dbo.TokenVocabulary v1
        CROSS JOIN dbo.TokenVocabulary v2
        WHERE v1.token = @current_token AND v2.token <> @current_token
        ORDER BY vector_distance('cosine', v1.embedding, v2.embedding);

        -- Append the next token to the sequence
        SET @generated_text = @generated_text + ' ' + @next_token;
        SET @current_token = @next_token;

        -- Check for end-of-sequence token
        IF @next_token = '[EOS]'
        BEGIN
            BREAK;
        END

        SET @i = @i + 1;
    END

    SELECT @generated_text AS generated_text;
END
GO