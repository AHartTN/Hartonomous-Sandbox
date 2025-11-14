-- =============================================
-- AtomCompositions: Structural Representation for Non-Tensor Data
-- Maps parent atoms to component atoms with spatial XYZM indexing
-- =============================================
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'AtomCompositions' AND schema_id = SCHEMA_ID('dbo'))
BEGIN
    CREATE TABLE [dbo].[AtomCompositions] (
        [CompositionId]     BIGINT        IDENTITY (1, 1) NOT NULL,
        [ParentAtomId]      BIGINT        NOT NULL,     -- FK to dbo.Atoms (file, document, large number)
        [ComponentAtomId]   BIGINT        NOT NULL,     -- FK to dbo.Atoms (chunk, token, sample)
        [SequenceIndex]     BIGINT        NOT NULL,     -- Order of this component
        
        -- XYZM spatial key enables unified structural queries
        -- X = Position/Index, Y = Value/AtomId, Z = Layer/Depth, M = Measure/Value
        [SpatialKey]        GEOMETRY      NULL,
        
        CONSTRAINT [PK_AtomCompositions] PRIMARY KEY CLUSTERED ([CompositionId] ASC),
        CONSTRAINT [FK_AtomCompositions_Parent] FOREIGN KEY ([ParentAtomId]) 
            REFERENCES [dbo].[Atoms] ([AtomId]) ON DELETE CASCADE,
        CONSTRAINT [FK_AtomCompositions_Component] FOREIGN KEY ([ComponentAtomId]) 
            REFERENCES [dbo].[Atoms] ([AtomId])
    );
END
GO

-- Optimized indexes
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_AtomCompositions_Parent' AND object_id = OBJECT_ID('dbo.AtomCompositions'))
    CREATE NONCLUSTERED INDEX [IX_AtomCompositions_Parent] 
    ON [dbo].[AtomCompositions]([ParentAtomId]) 
    INCLUDE ([ComponentAtomId], [SequenceIndex]);
GO

IF NOT EXISTS (SELECT 1 FROM sys.spatial_indexes WHERE name = 'SIX_AtomCompositions_SpatialKey' AND object_id = OBJECT_ID('dbo.AtomCompositions'))
    CREATE SPATIAL INDEX [SIX_AtomCompositions_SpatialKey] 
    ON [dbo].[AtomCompositions]([SpatialKey]);
GO
