-- =============================================
-- AtomCompositions: Maps source content to atomic components
-- Used for reconstruction and spatial queries
-- =============================================
CREATE TABLE [dbo].[AtomCompositions] (
    [CompositionId]     BIGINT        NOT NULL IDENTITY,
    [SourceAtomId]      BIGINT        NOT NULL,  -- Parent atom (image, document, model, etc.)
    [ComponentAtomId]   BIGINT        NOT NULL,  -- Atomic component (pixel, char, weight, etc.)
    [ComponentType]     NVARCHAR(64)  NOT NULL,  -- 'pixel', 'audio-sample', 'text-token', 'weight'
    
    -- Spatial position in source (exploits GEOMETRY for multi-dimensional indexing)
    [PositionKey]       GEOMETRY      NOT NULL,
    
    -- Additional positional metadata
    [SequenceIndex]     BIGINT        NULL,      -- Linear ordering if needed
    [DimensionX]        INT           NULL,      -- Explicit coordinates for clarity
    [DimensionY]        INT           NULL,
    [DimensionZ]        INT           NULL,
    [DimensionM]        INT           NULL,
    
    -- Metadata
    [Metadata]          NVARCHAR(MAX) NULL,      -- JSON for component-specific data
    [CreatedAt]         DATETIME2(7)  NOT NULL DEFAULT (SYSUTCDATETIME()),
    
    CONSTRAINT [PK_AtomCompositions] PRIMARY KEY CLUSTERED ([CompositionId] ASC),
    CONSTRAINT [FK_AtomCompositions_Source] FOREIGN KEY ([SourceAtomId]) 
        REFERENCES [dbo].[Atoms] ([AtomId]) ON DELETE CASCADE,
    CONSTRAINT [FK_AtomCompositions_Component] FOREIGN KEY ([ComponentAtomId]) 
        REFERENCES [dbo].[Atoms] ([AtomId]),
    
    INDEX [IX_AtomCompositions_Source] NONCLUSTERED ([SourceAtomId]),
    INDEX [IX_AtomCompositions_Component] NONCLUSTERED ([ComponentAtomId]),
    INDEX [IX_AtomCompositions_Type] NONCLUSTERED ([ComponentType], [SourceAtomId])
);
GO

-- Spatial index for multi-dimensional position queries
CREATE SPATIAL INDEX [SIDX_AtomCompositions_Position] 
ON [dbo].[AtomCompositions] ([PositionKey])
USING GEOMETRY_GRID
WITH (
    BOUNDING_BOX = (0, 0, 65535, 65535),  -- Large enough for most coordinates
    GRIDS = (LEVEL_1 = MEDIUM, LEVEL_2 = MEDIUM, LEVEL_3 = MEDIUM, LEVEL_4 = MEDIUM),
    CELLS_PER_OBJECT = 16
);
GO
