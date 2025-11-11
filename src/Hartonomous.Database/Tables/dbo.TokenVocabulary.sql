CREATE TABLE dbo.TokenVocabulary (
    TokenId INT IDENTITY(1,1) NOT NULL,
    Token NVARCHAR(256) NOT NULL,
    VocabularyName NVARCHAR(128) NOT NULL DEFAULT 'default',
    Frequency INT NOT NULL DEFAULT 1,
    DimensionIndex INT NOT NULL,
    IDF FLOAT NULL,
    CreatedUtc DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
    UpdatedUtc DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
    CONSTRAINT PK_TokenVocabulary PRIMARY KEY CLUSTERED (TokenId),
    INDEX IX_TokenVocabulary_Token (VocabularyName, Token),
    INDEX IX_TokenVocabulary_Dimension (DimensionIndex)
);