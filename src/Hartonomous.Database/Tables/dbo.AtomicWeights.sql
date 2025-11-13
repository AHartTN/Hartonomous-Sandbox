-- =============================================
-- AtomicWeights: Individual float32 weight values (deduplicated)
-- Completes the atomic decomposition for model parameters
-- =============================================
CREATE TABLE [dbo].[AtomicWeights] (
    [WeightHash]        BINARY(32)    NOT NULL,  -- SHA-256 of float32 bytes
    [WeightValue]       REAL          NOT NULL,  -- The actual weight value
    [WeightBytes]       VARBINARY(4)  NOT NULL,  -- Raw IEEE 754 float32 bytes
    
    -- Spatial representation: use value as coordinate for range queries
    [ValuePoint]        GEOMETRY      NULL,      -- POINT(value, quantile, 0, 0)
    
    -- Statistics
    [ReferenceCount]    BIGINT        NOT NULL DEFAULT CAST(0 AS BIGINT),
    [FirstSeen]         DATETIME2(7)  NOT NULL DEFAULT (SYSUTCDATETIME()),
    [LastReferenced]    DATETIME2(7)  NOT NULL DEFAULT (SYSUTCDATETIME()),
    
    CONSTRAINT [PK_AtomicWeights] PRIMARY KEY CLUSTERED ([WeightHash] ASC),
    INDEX [IX_AtomicWeights_Value] NONCLUSTERED ([WeightValue]),
    INDEX [IX_AtomicWeights_References] NONCLUSTERED ([ReferenceCount] DESC)
);
GO

-- Spatial index for value-based range queries
CREATE SPATIAL INDEX [SIDX_AtomicWeights_Value]
ON [dbo].[AtomicWeights] ([ValuePoint])
USING GEOMETRY_GRID
WITH (
    BOUNDING_BOX = (-10, 0, 10, 100),  -- Weight values typically in [-10, 10], quantile [0, 100]
    GRIDS = (LEVEL_1 = HIGH, LEVEL_2 = HIGH, LEVEL_3 = HIGH, LEVEL_4 = HIGH),
    CELLS_PER_OBJECT = 16
);
GO
