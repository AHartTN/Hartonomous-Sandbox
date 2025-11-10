-- =============================================
-- Table: dbo.AtomicTextTokens
-- Description: Unique atomic text tokens with content-addressable deduplication.
--              Stores token text and optional embeddings for semantic deduplication.
-- =============================================
CREATE TABLE [dbo].[AtomicTextTokens]
(
    [TokenId]          BIGINT           NOT NULL IDENTITY(1,1),
    [TokenHash]        BINARY(32)       NOT NULL,
    [TokenText]        NVARCHAR(200)    NOT NULL,
    [TokenLength]      INT              NOT NULL,
    [TokenEmbedding]   VECTOR(768)      NULL,
    [EmbeddingModel]   NVARCHAR(100)    NULL,
    [VocabId]          INT              NULL,
    [ReferenceCount]   BIGINT           NOT NULL DEFAULT (0),
    [FirstSeen]        DATETIME2(7)     NOT NULL DEFAULT (SYSUTCDATETIME()),
    [LastReferenced]   DATETIME2(7)     NOT NULL DEFAULT (SYSUTCDATETIME()),

    CONSTRAINT [PK_AtomicTextTokens] PRIMARY KEY CLUSTERED ([TokenId] ASC)
);
GO

CREATE UNIQUE NONCLUSTERED INDEX [IX_AtomicTextTokens_TokenHash]
    ON [dbo].[AtomicTextTokens]([TokenHash] ASC);
GO

CREATE UNIQUE NONCLUSTERED INDEX [IX_AtomicTextTokens_TokenText]
    ON [dbo].[AtomicTextTokens]([TokenText] ASC);
GO