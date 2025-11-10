-- =============================================
-- Table: dbo.AtomEmbeddingComponents
-- Description: Individual components of atom embeddings supporting dynamic dimensionality.
--              Enables storage beyond VECTOR type limits (1998 floats) and sparse embeddings.
-- =============================================
CREATE TABLE [dbo].[AtomEmbeddingComponents]
(
    [AtomEmbeddingComponentId]  BIGINT   NOT NULL IDENTITY(1,1),
    [AtomEmbeddingId]           BIGINT   NOT NULL,
    [ComponentIndex]            INT      NOT NULL,
    [ComponentValue]            REAL     NOT NULL,

    CONSTRAINT [PK_AtomEmbeddingComponents] PRIMARY KEY CLUSTERED ([AtomEmbeddingComponentId] ASC),

    CONSTRAINT [FK_AtomEmbeddingComponents_AtomEmbeddings] 
        FOREIGN KEY ([AtomEmbeddingId]) 
        REFERENCES [dbo].[AtomEmbeddings]([AtomEmbeddingId]) 
        ON DELETE CASCADE
);
GO

CREATE UNIQUE NONCLUSTERED INDEX [UX_AtomEmbeddingComponents_Embedding_Index]
    ON [dbo].[AtomEmbeddingComponents]([AtomEmbeddingId] ASC, [ComponentIndex] ASC);
GO