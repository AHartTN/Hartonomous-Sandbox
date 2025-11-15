-- =============================================
-- AtomEmbeddings: Semantic Representation for All Modalities
-- Stores 1:1 spatial projections for semantic similarity search
-- =============================================
CREATE TABLE [dbo].[AtomEmbeddings] (
    [AtomEmbeddingId]   BIGINT         IDENTITY (1, 1) NOT NULL,
    [AtomId]            BIGINT         NOT NULL,  -- FK to dbo.Atoms (the parent object)
    [ModelId]           INT            NOT NULL,  -- FK to dbo.Models (the embedder)
    [EmbeddingType]     NVARCHAR(50)   NOT NULL DEFAULT 'semantic', -- V3 ARCHITECTURE: Restored for flexible categorization.
    
    -- 3D/4D spatial projection (semantic space)
    [SpatialKey]        GEOMETRY       NOT NULL,
    
    -- Hilbert curve value for 1D indexing (populated by Phase 3.1)
    [HilbertValue]      BIGINT         NULL,

    [CreatedAt]         DATETIME2(7)   DEFAULT (SYSUTCDATETIME()) NOT NULL,
    
    CONSTRAINT [PK_AtomEmbeddings] PRIMARY KEY CLUSTERED ([AtomEmbeddingId] ASC),
    CONSTRAINT [FK_AtomEmbeddings_Atom] FOREIGN KEY ([AtomId]) 
        REFERENCES [dbo].[Atoms] ([AtomId]) ON DELETE CASCADE,
    CONSTRAINT [FK_AtomEmbeddings_Model] FOREIGN KEY ([ModelId]) 
        REFERENCES [dbo].[Models] ([ModelId])
);
GO

-- Indexes created as separate index definition files in /Indexes folder
-- SIX_AtomEmbeddings_SpatialKey (spatial index)
-- IX_AtomEmbeddings_Hilbert
