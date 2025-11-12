-- =============================================
-- Table: dbo.TokenVocabulary
-- =============================================
-- Represents a token in a model's vocabulary with its associated embedding vector.
-- This table was previously managed by EF Core.
-- =============================================

IF OBJECT_ID('dbo.TokenVocabulary', 'U') IS NOT NULL
    DROP TABLE dbo.TokenVocabulary;
GO

CREATE TABLE dbo.TokenVocabulary
(
    TokenId             BIGINT          IDENTITY(1,1) NOT NULL PRIMARY KEY,
    ModelId             INT             NOT NULL,
    VocabularyName      NVARCHAR(128)   NOT NULL DEFAULT 'default',
    Token               NVARCHAR(256)   NOT NULL,
    DimensionIndex      INT             NOT NULL,
    TokenType           NVARCHAR(50)    NULL, -- Added from entity, not in config
    Embedding           VECTOR(768)     NULL,
    EmbeddingDim        INT             NULL, -- Added from entity, not in config
    Frequency           BIGINT          NOT NULL DEFAULT 1,
    IDF                 FLOAT           NULL,
    CreatedUtc          DATETIME2       NOT NULL DEFAULT SYSUTCDATETIME(),
    UpdatedUtc          DATETIME2       NOT NULL DEFAULT SYSUTCDATETIME(),
    LastUsed            DATETIME2       NULL, -- Added from entity, not in config

    CONSTRAINT FK_TokenVocabulary_Models FOREIGN KEY (ModelId) REFERENCES dbo.Models(ModelId) ON DELETE CASCADE
);
GO

CREATE INDEX IX_TokenVocabulary_Token ON dbo.TokenVocabulary(VocabularyName, Token);
GO

CREATE INDEX IX_TokenVocabulary_Dimension ON dbo.TokenVocabulary(DimensionIndex);
GO

CREATE INDEX IX_TokenVocabulary_ModelId_Token ON dbo.TokenVocabulary(ModelId, Token);
GO

PRINT 'Created table dbo.TokenVocabulary';
GO