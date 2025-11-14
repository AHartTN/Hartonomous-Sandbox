-- =============================================
-- AtomEmbeddings: Semantic Representation for All Modalities
-- Stores 1:1 spatial projections for semantic similarity search
-- =============================================
CREATE TABLE [dbo].[AtomEmbeddings] (
    [AtomEmbeddingId]   BIGINT         IDENTITY (1, 1) NOT NULL,
    [AtomId]            BIGINT         NOT NULL,  -- FK to dbo.Atoms (the parent object)
    [ModelId]           INT            NOT NULL,  -- FK to dbo.Models (the embedder)
    
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

-- Spatial index for semantic similarity search
IF NOT EXISTS (SELECT 1 FROM sys.spatial_indexes WHERE name = 'SIX_AtomEmbeddings_SpatialKey' AND object_id = OBJECT_ID('dbo.AtomEmbeddings'))
    CREATE SPATIAL INDEX [SIX_AtomEmbeddings_SpatialKey] 
    ON [dbo].[AtomEmbeddings]([SpatialKey]);
GO

-- Hilbert index (will be populated in Phase 3.1)
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_AtomEmbeddings_Hilbert' AND object_id = OBJECT_ID('dbo.AtomEmbeddings'))
    CREATE NONCLUSTERED INDEX [IX_AtomEmbeddings_Hilbert] 
    ON [dbo].[AtomEmbeddings]([HilbertValue] ASC) 
    INCLUDE ([AtomId], [ModelId]) 
    WHERE [HilbertValue] IS NOT NULL;
GO
