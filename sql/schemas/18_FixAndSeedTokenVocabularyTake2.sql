-- =============================================
-- Fix and Seed Token Vocabulary Table (Take 2)
-- =============================================

USE Hartonomous;
GO

-- Disable the index
ALTER INDEX idx_token_embedding ON dbo.TokenVocabulary DISABLE;
GO

-- Insert some sample tokens and their embeddings
INSERT INTO dbo.TokenVocabulary (model_id, token, token_id, embedding)
VALUES
    (1, 'hello', 1, CAST('[1.0, 0.0, 0.0]' AS VECTOR(3))),
    (1, 'world', 2, CAST('[0.5, 0.5, 0.0]' AS VECTOR(3))),
    (1, '[EOS]', 3, CAST('[0.0, 0.0, 0.0]' AS VECTOR(3)));
GO

-- Rebuild the index
ALTER INDEX idx_token_embedding ON dbo.TokenVocabulary REBUILD;
GO
