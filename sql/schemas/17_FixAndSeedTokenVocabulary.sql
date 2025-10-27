-- =============================================
-- Fix and Seed Token Vocabulary Table
-- =============================================

USE Hartonomous;
GO

-- Enable preview features
ALTER DATABASE SCOPED CONFIGURATION SET PREVIEW_FEATURES = ON;
GO

SET QUOTED_IDENTIFIER ON;
GO

-- Drop the index if it exists
IF EXISTS (SELECT * FROM sys.indexes WHERE name = 'idx_token_embedding')
BEGIN
    DROP INDEX idx_token_embedding ON dbo.TokenVocabulary;
END
GO

-- Drop the old embedding columns if they exist
IF COL_LENGTH('dbo.TokenVocabulary', 'embedding_vector') IS NOT NULL
BEGIN
    ALTER TABLE dbo.TokenVocabulary DROP COLUMN embedding_vector;
END
GO

IF COL_LENGTH('dbo.TokenVocabulary', 'embedding') IS NOT NULL
BEGIN
    ALTER TABLE dbo.TokenVocabulary DROP COLUMN embedding;
END
GO

-- Add the new VECTOR column for the embeddings
ALTER TABLE dbo.TokenVocabulary
ADD embedding VECTOR(3);
GO

-- Create a DISKANN index on the new column
CREATE VECTOR INDEX idx_token_embedding ON dbo.TokenVocabulary(embedding)
WITH (METRIC = 'COSINE', TYPE = 'DISKANN');
GO

-- Insert some sample tokens and their embeddings
INSERT INTO dbo.TokenVocabulary (model_id, token, token_id, embedding)
VALUES
    (1, 'hello', 1, CAST('[1.0, 0.0, 0.0]' AS VECTOR(3))),
    (1, 'world', 2, CAST('[0.5, 0.5, 0.0]' AS VECTOR(3))),
    (1, '[EOS]', 3, CAST('[0.0, 0.0, 0.0]' AS VECTOR(3)));
GO
