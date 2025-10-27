-- =============================================
-- Cleanup Script
-- =============================================

USE Hartonomous;
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

-- Drop the functions and procedures if they exist
IF OBJECT_ID('dbo.VectorDotProduct', 'FN') IS NOT NULL
BEGIN
    DROP FUNCTION dbo.VectorDotProduct;
END
GO

IF OBJECT_ID('dbo.ConvertVarbinary4ToReal', 'FN') IS NOT NULL
BEGIN
    DROP FUNCTION dbo.ConvertVarbinary4ToReal;
END
GO

IF OBJECT_ID('dbo.sp_GenerateText', 'P') IS NOT NULL
BEGIN
    DROP PROCEDURE dbo.sp_GenerateText;
END
GO
