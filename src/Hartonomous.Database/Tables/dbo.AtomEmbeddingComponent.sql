CREATE TABLE [dbo].[AtomEmbeddingComponent] (
    [AtomEmbeddingComponentId] BIGINT NOT NULL IDENTITY,
    [AtomEmbeddingId]          BIGINT NOT NULL,
    [ComponentIndex]           INT    NOT NULL,
    [ComponentValue]           REAL   NOT NULL,
    CONSTRAINT [PK_AtomEmbeddingComponent] PRIMARY KEY CLUSTERED ([AtomEmbeddingComponentId] ASC),
    CONSTRAINT [FK_AtomEmbeddingComponents_AtomEmbeddings_AtomEmbeddingId] FOREIGN KEY ([AtomEmbeddingId]) REFERENCES [dbo].[AtomEmbedding] ([AtomEmbeddingId]) ON DELETE CASCADE
);
