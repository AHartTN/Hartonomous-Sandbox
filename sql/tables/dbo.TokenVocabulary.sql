USE Hartonomous;
GO

IF OBJECT_ID(N'dbo.TokenVocabulary', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.TokenVocabulary (
        TokenId INT IDENTITY(1,1) NOT NULL,
        Token NVARCHAR(256) NOT NULL,
        VocabularyName NVARCHAR(128) NOT NULL DEFAULT 'default',
        Frequency INT NOT NULL DEFAULT 1,
        DimensionIndex INT NOT NULL,
        IDF FLOAT NULL,
        CreatedUtc DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
        UpdatedUtc DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),

        CONSTRAINT PK_TokenVocabulary PRIMARY KEY CLUSTERED (TokenId)
    );
END
GO
IF NOT EXISTS (
    SELECT 1
    FROM sys.indexes
    WHERE name = N'IX_TokenVocabulary_Token'
      AND object_id = OBJECT_ID(N'dbo.TokenVocabulary', N'U')
)
BEGIN
    CREATE INDEX IX_TokenVocabulary_Token ON dbo.TokenVocabulary (VocabularyName, Token);
END
GO
IF NOT EXISTS (
    SELECT 1
    FROM sys.indexes
    WHERE name = N'IX_TokenVocabulary_Dimension'
      AND object_id = OBJECT_ID(N'dbo.TokenVocabulary', N'U')
)
BEGIN
    CREATE INDEX IX_TokenVocabulary_Dimension ON dbo.TokenVocabulary (DimensionIndex);
END
GO

DECLARE @tokens TABLE (Token NVARCHAR(256), DimIndex INT);
INSERT INTO @tokens VALUES
('the', 0), ('be', 1), ('to', 2), ('of', 3), ('and', 4),
('a', 5), ('in', 6), ('that', 7), ('have', 8), ('I', 9),
('it', 10), ('for', 11), ('not', 12), ('on', 13), ('with', 14),
('he', 15), ('as', 16), ('you', 17), ('do', 18), ('at', 19),
('neural', 97), ('network', 98), ('database', 99);

INSERT INTO dbo.TokenVocabulary (Token, VocabularyName, DimensionIndex, Frequency)
SELECT t.Token, 'default', t.DimIndex, 100 - t.DimIndex
FROM @tokens AS t
WHERE NOT EXISTS (
        SELECT 1
        FROM dbo.TokenVocabulary AS vocab
        WHERE vocab.Token = t.Token
            AND vocab.VocabularyName = N'default'
);
GO

PRINT 'Created TokenVocabulary table with seed data';
