CREATE TABLE [dbo].[AtomicTextTokens] (
    [TokenId]          BIGINT         NOT NULL IDENTITY,
    [TokenHash]        BINARY (32)    NOT NULL,
    [TokenText]        NVARCHAR (200) NOT NULL,
    [TokenLength]      INT            NOT NULL,
    [TokenEmbedding]   VECTOR(1998)   NULL,
    [EmbeddingModel]   NVARCHAR (100) NULL,
    [VocabId]          INT            NULL,
    [ReferenceCount]   BIGINT         NOT NULL DEFAULT CAST(0 AS BIGINT),
    [FirstSeen]        DATETIME2 (7)  NOT NULL DEFAULT (SYSUTCDATETIME()),
    [LastReferenced]   DATETIME2 (7)  NOT NULL DEFAULT (SYSUTCDATETIME()),
    CONSTRAINT [PK_AtomicTextTokens] PRIMARY KEY CLUSTERED ([TokenId] ASC)
);
