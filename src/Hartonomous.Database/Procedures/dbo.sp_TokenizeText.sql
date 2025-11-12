USE Hartonomous;
GO

CREATE PROCEDURE dbo.sp_TokenizeText
    @text NVARCHAR(MAX),
    @tokenIdsJson NVARCHAR(MAX) OUTPUT
AS
BEGIN
    SET NOCOUNT ON;

    IF @text IS NULL OR LTRIM(RTRIM(@text)) = ''
    BEGIN
        SET @tokenIdsJson = '[]';
        RETURN;
    END

    -- Normalize the text (lowercase, remove punctuation)
    -- This logic is adapted from the proven fallback path in sp_TextToEmbedding
    DECLARE @normalized NVARCHAR(MAX) = LOWER(@text);
    DECLARE @punctuation NVARCHAR(50) = N'.,;:!?()[]{}''`~|\/';
    SET @normalized = TRANSLATE(@normalized, @punctuation, REPLICATE(' ', LEN(@punctuation)));

    -- Use an intermediate table to preserve the order of tokens
    DECLARE @OrderedTokens TABLE (
        OrderId INT IDENTITY(1,1) PRIMARY KEY,
        TokenValue NVARCHAR(100)
    );

    INSERT INTO @OrderedTokens (TokenValue)
    SELECT LTRIM(RTRIM(value))
    FROM STRING_SPLIT(@normalized, ' ', 1)
    WHERE value IS NOT NULL AND value <> '';

    -- Look up the tokens in the vocabulary and construct the JSON array, preserving order.
    -- For tokens not found in the vocabulary, they will be excluded from the output,
    -- which is the expected behavior for this type of tokenization.
    SELECT @tokenIdsJson = (
        SELECT
            tv.TokenId
        FROM @OrderedTokens ot
        JOIN dbo.TokenVocabulary tv ON ot.TokenValue = tv.Token
        ORDER BY ot.OrderId
        FOR JSON PATH
    );

    -- If no tokens were found in the vocabulary, return an empty JSON array.
    IF @tokenIdsJson IS NULL
    BEGIN
        SET @tokenIdsJson = '[]';
    END
END;
GO

PRINT 'Created procedure dbo.sp_TokenizeText using approved project logic.';
GO