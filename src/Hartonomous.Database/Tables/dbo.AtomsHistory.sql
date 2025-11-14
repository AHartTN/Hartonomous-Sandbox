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
    [ValidTo] DATETIME2(7) NOT NULL,
    
    -- Clustered index for temporal queries (required for history table)
    INDEX [IX_AtomsHistory_Period] CLUSTERED ([ValidFrom], [ValidTo])
);
GO

-- Nonclustered columnstore index for compression and analytics (excludes spatial types)
CREATE NONCLUSTERED COLUMNSTORE INDEX [NCCI_AtomsHistory] 
    ON [dbo].[AtomsHistory]
    (
        [AtomId], [ContentHash], [Modality], [Subtype], [SourceUri], [SourceType],
        [AtomicValue], [CanonicalText], [ContentType],
        [CreatedAt], [CreatedUtc], [UpdatedAt],
        [IsActive], [IsDeleted], [TenantId], [ReferenceCount],
        [ValidFrom], [ValidTo]
    );
GO

-- Index for point-in-time queries by ContentHash
CREATE NONCLUSTERED INDEX [IX_AtomsHistory_ContentHash]
    ON [dbo].[AtomsHistory]([ContentHash], [ValidFrom], [ValidTo]);
GO

-- Extended properties for documentation
EXEC sys.sp_addextendedproperty 
    @name = N'MS_Description', 
    @value = N'Temporal history table for Atoms. Stores all historical versions with nonclustered columnstore for compression and fast analytics. Spatial columns excluded from columnstore due to type restrictions.', 
    @level0type = N'SCHEMA', @level0name = N'dbo',
    @level1type = N'TABLE', @level1name = N'AtomsHistory';
GO

EXEC sys.sp_addextendedproperty 
    @name = N'MS_Description', 
    @value = N'Nonclustered columnstore provides compression for historical data and enables fast aggregation queries. Excludes SpatialKey and SpatialGeography columns due to type restrictions.', 
    @level0type = N'SCHEMA', @level0name = N'dbo',
    @level1type = N'TABLE', @level1name = N'AtomsHistory',
    @level2type = N'INDEX', @level2name = N'NCCI_AtomsHistory';
GO
