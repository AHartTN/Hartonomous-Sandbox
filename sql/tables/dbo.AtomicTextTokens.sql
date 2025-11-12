-- =============================================
-- Table: dbo.AtomicTextTokens
-- =============================================
-- Represents unique atomic text tokens with content-addressable deduplication.
-- This table was previously managed by EF Core.
-- =============================================

IF OBJECT_ID('dbo.AtomicTextTokens', 'U') IS NOT NULL
    DROP TABLE dbo.AtomicTextTokens;
GO

CREATE TABLE dbo.AtomicTextTokens
(
    TokenId         BIGINT          IDENTITY(1,1) NOT NULL PRIMARY KEY,
    TokenHash       BINARY(32)      NOT NULL,
    TokenText       NVARCHAR(200)   NOT NULL,
    TokenLength     INT             NOT NULL,
    TokenEmbedding  VECTOR(768)     NULL,
    EmbeddingModel  NVARCHAR(100)   NULL,
    VocabId         INT             NULL,
    ReferenceCount  BIGINT          NOT NULL DEFAULT 0,
    FirstSeen       DATETIME2       NOT NULL DEFAULT SYSUTCDATETIME(),
    LastReferenced  DATETIME2       NOT NULL DEFAULT SYSUTCDATETIME()
);
GO

CREATE UNIQUE INDEX UX_AtomicTextTokens_TokenHash ON dbo.AtomicTextTokens(TokenHash);
GO

CREATE UNIQUE INDEX IX_AtomicTextTokens_TokenText ON dbo.AtomicTextTokens(TokenText);
GO

PRINT 'Created table dbo.AtomicTextTokens';
GO
