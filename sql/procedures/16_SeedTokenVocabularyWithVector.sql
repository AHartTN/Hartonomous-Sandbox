-- =============================================
-- Seed Token Vocabulary (with VECTOR)
-- =============================================

USE Hartonomous;
GO

-- Insert some sample tokens and their embeddings
INSERT INTO dbo.TokenVocabulary (ModelId, Token, TokenId, Embedding)
VALUES
    (1, 'hello', 1, CAST('[' + REPLICATE('1.0,', 1) + REPLICATE('0.0,', 767) + ']' AS VECTOR(768))),
    (1, 'world', 2, CAST('[' + REPLICATE('0.5,', 2) + REPLICATE('0.0,', 766) + ']' AS VECTOR(768))),
    (1, '[EOS]', 3, CAST('[' + REPLICATE('0.0,', 768) + ']' AS VECTOR(768)));
GO