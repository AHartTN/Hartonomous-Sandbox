-- =============================================
-- Seed Token Vocabulary
-- =============================================

USE Hartonomous;
GO

-- Insert some sample tokens and their embeddings
INSERT INTO dbo.TokenVocabulary (model_id, token, token_id, embedding)
VALUES
    (1, 'hello', 1, CAST(CAST(1.0 AS REAL) AS BINARY(4))),
    (1, 'world', 2, CAST(CAST(0.5 AS REAL) AS BINARY(4))),
    (1, '[EOS]', 3, CAST(CAST(0.0 AS REAL) AS BINARY(4)));
GO
