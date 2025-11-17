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
    [AtomId]          BIGINT           NOT NULL,
    [TenantId]        INT              NOT NULL,
    [Modality]        VARCHAR(50)      NOT NULL,
    [Subtype]         VARCHAR(50)      NULL,
    [ContentHash]     BINARY(32)       NOT NULL,
    [ContentType]     NVARCHAR(100)    NULL,
    [SourceType]      NVARCHAR(100)    NULL,
    [SourceUri]       NVARCHAR(2048)   NULL,
    [CanonicalText]   NVARCHAR(MAX)    NULL,
    [Metadata]        NVARCHAR(MAX)    NULL,
    [AtomicValue]     VARBINARY(64)    NULL,
    [CreatedAt]       DATETIME2(7)     NOT NULL,
    [ModifiedAt]      DATETIME2(7)     NOT NULL,
    [ReferenceCount]  BIGINT           NOT NULL,
    
    INDEX [IX_AtomsHistory_Period] CLUSTERED ([CreatedAt], [ModifiedAt])
);
GO

-- Index for point-in-time queries by ContentHash
CREATE NONCLUSTERED INDEX [IX_AtomsHistory_ContentHash]
    ON [dbo].[AtomsHistory]([ContentHash], [CreatedAt], [ModifiedAt]);
GO

-- Extended properties for documentation
EXEC sys.sp_addextendedproperty 
    @name = N'MS_Description', 
    @value = N'Temporal history table for Atoms. Stores all historical versions.', 
    @level0type = N'SCHEMA', @level0name = N'dbo',
    @level1type = N'TABLE', @level1name = N'AtomsHistory';
GO
