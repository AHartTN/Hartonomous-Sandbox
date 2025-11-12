-- =============================================
-- Table: dbo.AtomEmbeddingComponents
-- =============================================
-- Stores individual components of an embedding to support dynamic dimensionality.
-- This table was previously managed by EF Core.
-- =============================================

IF OBJECT_ID('dbo.AtomEmbeddingComponents', 'U') IS NOT NULL
    DROP TABLE dbo.AtomEmbeddingComponents;
GO

CREATE TABLE dbo.AtomEmbeddingComponents
(
    AtomEmbeddingComponentId BIGINT          IDENTITY(1,1) NOT NULL PRIMARY KEY,
    AtomEmbeddingId          BIGINT          NOT NULL,
    ComponentIndex           INT             NOT NULL,
    ComponentValue           REAL            NOT NULL,

    CONSTRAINT FK_AtomEmbeddingComponents_AtomEmbedding FOREIGN KEY (AtomEmbeddingId) REFERENCES dbo.AtomEmbeddings(AtomEmbeddingId) ON DELETE CASCADE
);
GO

CREATE UNIQUE INDEX UX_AtomEmbeddingComponents_Embedding_Index ON dbo.AtomEmbeddingComponents(AtomEmbeddingId, ComponentIndex);
GO

PRINT 'Created table dbo.AtomEmbeddingComponents';
GO
