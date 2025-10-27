-- =============================================
-- Create and Seed Token Vocabulary Table (with VECTOR)
-- =============================================

USE Hartonomous;
GO

-- Drop the table if it exists
IF OBJECT_ID('dbo.TokenVocabulary', 'U') IS NOT NULL
BEGIN
    DROP TABLE dbo.TokenVocabulary;
END
GO

-- Create the table with the VECTOR column
CREATE TABLE dbo.TokenVocabulary (
    vocab_id INT PRIMARY KEY CLUSTERED,
    model_id INT NOT NULL,
    token NVARCHAR(100) NOT NULL,
    token_id INT NOT NULL,
    embedding VECTOR(3) -- Using a small dimension for testing
);
GO

-- Insert some sample tokens and their embeddings
INSERT INTO dbo.TokenVocabulary (vocab_id, model_id, token, token_id, embedding)
VALUES
    (1, 1, 'hello', 1, CAST('[1.0, 0.0, 0.0]' AS VECTOR(3))),
    (2, 1, 'world', 2, CAST('[0.5, 0.5, 0.0]' AS VECTOR(3))),
    (3, 1, '[EOS]', 3, CAST('[0.0, 0.0, 0.0]' AS VECTOR(3)));
GO
