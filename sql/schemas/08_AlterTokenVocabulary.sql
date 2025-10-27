-- =============================================
-- Alter Token Vocabulary Table
-- =============================================

USE Hartonomous;
GO

-- Add a new GEOMETRY column for the embeddings
ALTER TABLE dbo.TokenVocabulary
ADD embedding_geometry GEOMETRY;
GO

-- Create a spatial index on the new column
CREATE SPATIAL INDEX SIX_TokenVocabulary_embedding_geometry
ON dbo.TokenVocabulary(embedding_geometry);
GO