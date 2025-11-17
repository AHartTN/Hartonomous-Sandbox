-- =============================================
-- AtomCompositions: Structural Representation for Non-Tensor Data
-- Maps parent atoms to component atoms with spatial XYZM indexing
-- =============================================
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
GO

-- Indexes created as separate index definition files in /Indexes folder
-- IX_AtomCompositions_Parent
-- SIX_AtomCompositions_SpatialKey (spatial index)
