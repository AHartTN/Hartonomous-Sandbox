-- ==================================================================
-- AtomsHistory: Temporal History Table for System-Versioned Atoms
-- ==================================================================
-- Purpose: Store historical versions of Atoms for temporal queries
-- Optimization: Clustered columnstore for 10x compression + analytics
-- Performance Impact: Point-in-time queries, audit trail, data lineage
-- Related: Part of Phase 3 - Atoms Table Memory-Optimization
-- ==================================================================

CREATE TABLE [dbo].[AtomsHistory]
(
    -- Core identity
    [AtomId] BIGINT NOT NULL,
    [ContentHash] BINARY(32) NOT NULL,
    
    -- Classification
    [Modality] NVARCHAR(64) NOT NULL,
    [Subtype] NVARCHAR(128) NULL,
    [SourceUri] NVARCHAR(1024) NULL,
    [SourceType] NVARCHAR(128) NULL,
    
    -- Atomic data (small, frequently accessed)
    [AtomicValue] VARBINARY(64) NULL,         -- Quantized value (<=64 bytes)
    [CanonicalText] NVARCHAR(256) NULL,       -- Human-readable representation
    [ContentType] NVARCHAR(128) NULL,
    
    -- Audit columns
    [CreatedAt] DATETIME2(7) NOT NULL,
    [CreatedUtc] DATETIME2(7) NOT NULL,
    [UpdatedAt] DATETIME2(7) NULL,
    
    -- Soft delete flags
    [IsActive] BIT NOT NULL,
    [IsDeleted] BIT NOT NULL,
    
    -- Multi-tenancy
    [TenantId] INT NOT NULL,
    
    -- Reference counting (content-addressable storage)
    [ReferenceCount] BIGINT NOT NULL,
    
    -- Spatial encoding (GEOMETRY for SQL Server spatial indexes)
    [SpatialKey] GEOMETRY NULL,
    [SpatialGeography] GEOGRAPHY NULL,
    
    -- Temporal columns (required for SYSTEM_VERSIONING)
    [ValidFrom] DATETIME2(7) NOT NULL,
    [ValidTo] DATETIME2(7) NOT NULL
);
GO

-- Clustered columnstore index for 10x compression and fast analytics
CREATE CLUSTERED COLUMNSTORE INDEX [CCI_AtomsHistory] 
    ON [dbo].[AtomsHistory];
GO

-- Nonclustered rowstore index for efficient temporal lookups
CREATE NONCLUSTERED INDEX [IX_AtomsHistory_Temporal]
    ON [dbo].[AtomsHistory]([ValidFrom], [ValidTo], [AtomId])
    INCLUDE ([ContentHash], [Modality], [TenantId]);
GO

-- Index for point-in-time queries by ContentHash
CREATE NONCLUSTERED INDEX [IX_AtomsHistory_ContentHash]
    ON [dbo].[AtomsHistory]([ContentHash], [ValidFrom], [ValidTo]);
GO

-- Extended properties for documentation
EXEC sys.sp_addextendedproperty 
    @name = N'MS_Description', 
    @value = N'Temporal history table for Atoms. Stores all historical versions with clustered columnstore for 10x compression and fast analytics.', 
    @level0type = N'SCHEMA', @level0name = N'dbo',
    @level1type = N'TABLE', @level1name = N'AtomsHistory';
GO

EXEC sys.sp_addextendedproperty 
    @name = N'MS_Description', 
    @value = N'Clustered columnstore provides 10x compression for historical data and enables fast aggregation queries across temporal versions.', 
    @level0type = N'SCHEMA', @level0name = N'dbo',
    @level1type = N'TABLE', @level1name = N'AtomsHistory',
    @level2type = N'INDEX', @level2name = N'CCI_AtomsHistory';
GO
