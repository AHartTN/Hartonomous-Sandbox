CREATE TABLE [dbo].[AtomEmbeddingComponents] (
    [AtomEmbeddingComponentId] BIGINT NOT NULL IDENTITY,
    [AtomEmbeddingId]          BIGINT NOT NULL,
    [ComponentIndex]           INT    NOT NULL,
    [ComponentValue]           REAL   NOT NULL,
    CONSTRAINT [PK_AtomEmbeddingComponents] PRIMARY KEY CLUSTERED ([AtomEmbeddingComponentId] ASC),
    CONSTRAINT [FK_AtomEmbeddingComponents_AtomEmbeddings_AtomEmbeddingId] FOREIGN KEY ([AtomEmbeddingId]) REFERENCES [dbo].[AtomEmbeddings] ([AtomEmbeddingId]) ON DELETE CASCADE
);
