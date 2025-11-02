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
        DECLARE @current_embedding VECTOR(1998);
        DECLARE @embedding_dimension INT;
        
        SELECT @current_embedding = Embedding, @embedding_dimension = EmbeddingDim 
        FROM dbo.TokenVocabulary 
        WHERE Token = @current_token;

        -- Find the most likely next token using VECTOR_DISTANCE with dimension filtering
        SELECT TOP 1 @next_token = v2.Token
        FROM dbo.TokenVocabulary v1
        CROSS JOIN dbo.TokenVocabulary v2
        WHERE v1.Token = @current_token
          AND v2.EmbeddingDim = @embedding_dimension
        ORDER BY VECTOR_DISTANCE('cosine', v1.Embedding, v2.Embedding);

        -- Append the next token to the sequence
        SET @generated_text = @generated_text + ' ' + @next_token;
        SET @current_token = @next_token;

        -- Check for end-of-sequence token
        IF @next_token = '[EOS]' -- Assuming '[EOS]' is the end-of-sequence token
        BEGIN
            BREAK;
        END

        SET @i = @i + 1;
    END

    SELECT @generated_text AS generated_text;
END
GO
