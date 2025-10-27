-- =============================================
-- Fix Token Vocabulary Primary Key
-- =============================================

USE Hartonomous;
GO

-- Drop the existing primary key
ALTER TABLE dbo.TokenVocabulary
DROP CONSTRAINT PK__TokenVoc__48157D755018AEF0;
GO

-- Alter the vocab_id column to be an INT
ALTER TABLE dbo.TokenVocabulary
ALTER COLUMN vocab_id INT NOT NULL;
GO

-- Create a new clustered primary key on the vocab_id column
ALTER TABLE dbo.TokenVocabulary
ADD CONSTRAINT PK_TokenVocabulary PRIMARY KEY CLUSTERED (vocab_id);
GO
