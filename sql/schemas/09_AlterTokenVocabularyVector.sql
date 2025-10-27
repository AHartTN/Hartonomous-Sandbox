-- =============================================
-- Alter Token Vocabulary Table for VECTOR Type
-- =============================================

USE Hartonomous;
GO

-- Add a new VECTOR column for the embeddings
ALTER TABLE dbo.TokenVocabulary
ADD embedding_vector VECTOR(768);
GO

-- Create a DISKANN index on the new column
CREATE VECTOR INDEX idx_token_vector ON dbo.TokenVocabulary(embedding_vector)
WITH (backend = DISKANN);
GO