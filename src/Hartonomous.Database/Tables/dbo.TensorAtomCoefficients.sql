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
    
    -- Temporal columns
    [ValidFrom]       DATETIME2(7)   GENERATED ALWAYS AS ROW START NOT NULL,
    [ValidTo]         DATETIME2(7)   GENERATED ALWAYS AS ROW END NOT NULL,
    
    PERIOD FOR SYSTEM_TIME ([ValidFrom], [ValidTo]),
    
    CONSTRAINT [FK_TensorAtomCoefficients_Atom] FOREIGN KEY ([TensorAtomId]) 
        REFERENCES [dbo].[Atoms] ([AtomId]),
    CONSTRAINT [FK_TensorAtomCoefficients_Model] FOREIGN KEY ([ModelId]) 
        REFERENCES [dbo].[Models] ([ModelId]) ON DELETE CASCADE
)
WITH (SYSTEM_VERSIONING = ON (HISTORY_TABLE = [dbo].[TensorAtomCoefficients_History]));
GO

-- Clustered columnstore for OLAP
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'CCI_TensorAtomCoefficients' AND object_id = OBJECT_ID('dbo.TensorAtomCoefficients'))
    CREATE CLUSTERED COLUMNSTORE INDEX [CCI_TensorAtomCoefficients] 
    ON [dbo].[TensorAtomCoefficients];
GO

-- Spatial index for geometric queries
IF NOT EXISTS (SELECT 1 FROM sys.spatial_indexes WHERE name = 'SIX_TensorAtomCoefficients_SpatialKey' AND object_id = OBJECT_ID('dbo.TensorAtomCoefficients'))
    CREATE SPATIAL INDEX [SIX_TensorAtomCoefficients_SpatialKey] 
    ON [dbo].[TensorAtomCoefficients]([SpatialKey]);
GO
