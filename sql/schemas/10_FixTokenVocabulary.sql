-- =============================================
-- Fix Token Vocabulary Table
-- =============================================

USE Hartonomous;
GO

SET QUOTED_IDENTIFIER ON;
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
ADD embedding VECTOR(768);
GO

-- Create a DISKANN index on the new column
CREATE VECTOR INDEX idx_token_embedding ON dbo.TokenVocabulary(embedding)
WITH (METRIC = 'cosine', TYPE = 'DiskANN');
GO