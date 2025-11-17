-- =============================================
-- TensorAtomCoefficients: Structural Representation for Tensor Models
-- Stores explicit, OLAP-queryable weight mappings with spatial indexing
-- =============================================
CREATE TABLE [dbo].[TensorAtomCoefficients] (
    [TensorAtomId]    BIGINT         NOT NULL,  -- FK to dbo.Atoms (the float value)
    [ModelId]         INT            NOT NULL,  -- FK to dbo.Models
    [LayerIdx]        INT            NOT NULL,
    [PositionX]       INT            NOT NULL,  -- e.g., Row
    [PositionY]       INT            NOT NULL,  -- e.g., Column  
    [PositionZ]       INT            NOT NULL DEFAULT 0,
    
    -- Computed spatial key for XYZM queries: X=Pos, Y=Pos, Z=Pos, M=Layer
    [SpatialKey]      AS (GEOMETRY::Point([PositionX], [PositionY], 0)) PERSISTED,
    
    -- DEPRECATED COLUMNS (for backward compatibility during migration)
    [TensorAtomCoefficientId] BIGINT  NULL,  -- DEPRECATED: No identity column in v5
    [ParentLayerId]           BIGINT  NULL,  -- DEPRECATED: Use ModelId + LayerIdx
    [TensorRole]              NVARCHAR(128) NULL,  -- DEPRECATED: Use positional indexing
    [Coefficient]             REAL    NULL,  -- DEPRECATED: The coefficient IS the atom (TensorAtomId)
    
    -- Temporal columns
    [ValidFrom]       DATETIME2(7)   GENERATED ALWAYS AS ROW START NOT NULL,
    [ValidTo]         DATETIME2(7)   GENERATED ALWAYS AS ROW END NOT NULL,
    
    CONSTRAINT [PK_TensorAtomCoefficients] PRIMARY KEY CLUSTERED ([TensorAtomId], [ModelId], [LayerIdx], [PositionX], [PositionY], [PositionZ]),
    PERIOD FOR SYSTEM_TIME ([ValidFrom], [ValidTo]),
    
    CONSTRAINT [FK_TensorAtomCoefficients_Atom] FOREIGN KEY ([TensorAtomId]) 
        REFERENCES [dbo].[Atoms] ([AtomId]),
    CONSTRAINT [FK_TensorAtomCoefficients_Model] FOREIGN KEY ([ModelId]) 
        REFERENCES [dbo].[Models] ([ModelId]) ON DELETE CASCADE
)
WITH (SYSTEM_VERSIONING = ON (HISTORY_TABLE = [dbo].[TensorAtomCoefficients_History]));
GO

-- Clustered columnstore for OLAP (cannot be on temporal table directly)
-- Note: Apply to history table or use non-clustered columnstore
CREATE NONCLUSTERED COLUMNSTORE INDEX [NCCI_TensorAtomCoefficients] 
ON [dbo].[TensorAtomCoefficients]([TensorAtomId], [ModelId], [LayerIdx], [PositionX], [PositionY], [PositionZ]);
GO

-- Spatial index for geometric queries (BOUNDING_BOX required for GEOMETRY type)
CREATE SPATIAL INDEX [SIX_TensorAtomCoefficients_SpatialKey] 
ON [dbo].[TensorAtomCoefficients]([SpatialKey])
WITH (BOUNDING_BOX = (0, 0, 10000, 10000));
GO
