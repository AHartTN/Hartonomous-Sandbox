CREATE TABLE [dbo].[TokenVocabulary] (
    [VocabId]     BIGINT         NOT NULL IDENTITY,
    [ModelId]     INT            NOT NULL,
    [Token]       NVARCHAR (100) NOT NULL,
    [TokenId]     INT            NOT NULL,
    [TokenType]   NVARCHAR (20)  NULL,
    [Embedding]   VECTOR(1998)   NULL,
    [Frequency]   BIGINT         NOT NULL DEFAULT CAST(0 AS BIGINT),
    [LastUsed]    DATETIME2 (7)  NULL,
    CONSTRAINT [PK_TokenVocabulary] PRIMARY KEY CLUSTERED ([VocabId] ASC),
    CONSTRAINT [FK_TokenVocabulary_Models_ModelId] FOREIGN KEY ([ModelId]) REFERENCES [dbo].[Model] ([ModelId]) ON DELETE CASCADE
);
