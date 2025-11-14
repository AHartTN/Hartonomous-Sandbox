-- Core atomic storage with temporal support and strict schema-level governance
CREATE TABLE [dbo].[Atoms] (
    [AtomId]          BIGINT           IDENTITY (1, 1) NOT NULL,
    [Modality]        VARCHAR(50)      NOT NULL,
    [Subtype]         VARCHAR(50)      NULL,
    [ContentHash]     BINARY(32)       NOT NULL,
    
    -- Schema-level governance: Max 64 bytes enforces atomic decomposition
    [AtomicValue]     VARBINARY(64)    NULL,
    
    -- DEPRECATED COLUMNS (for backward compatibility during migration)
    -- These will be removed in a future version - use alternatives shown in comments
    [CanonicalText]   NVARCHAR(256)    NULL,  -- DEPRECATED: Use CONVERT(NVARCHAR, AtomicValue)
    [SourceType]      NVARCHAR(128)    NULL,  -- DEPRECATED: Use Modality
    [SourceUri]       NVARCHAR(1024)   NULL,  -- DEPRECATED: Remove or use external metadata
    [PayloadLocator]  NVARCHAR(1024)   NULL,  -- DEPRECATED: No blob storage in v5
    [TenantId]        INT              NULL,  -- DEPRECATED: Use TenantAtoms junction table
    
    -- Temporal columns
    [CreatedAt]       DATETIME2(7)     GENERATED ALWAYS AS ROW START NOT NULL,
    [ModifiedAt]      DATETIME2(7)     GENERATED ALWAYS AS ROW END NOT NULL,
    
    -- Reference counting for deduplication
    [ReferenceCount]  BIGINT           NOT NULL DEFAULT 1,
    
    CONSTRAINT [PK_Atoms] PRIMARY KEY CLUSTERED ([AtomId] ASC),
    CONSTRAINT [UX_Atoms_ContentHash] UNIQUE NONCLUSTERED ([ContentHash] ASC),
    PERIOD FOR SYSTEM_TIME ([CreatedAt], [ModifiedAt])
)
WITH (SYSTEM_VERSIONING = ON (HISTORY_TABLE = [dbo].[AtomsHistory]));
GO

-- Add indexes if they don't exist
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Atoms_Modality' AND object_id = OBJECT_ID('dbo.Atoms'))
    CREATE NONCLUSTERED INDEX [IX_Atoms_Modality] ON [dbo].[Atoms]([Modality], [Subtype]) INCLUDE ([AtomId], [ContentHash]);
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Atoms_ReferenceCount' AND object_id = OBJECT_ID('dbo.Atoms'))
    CREATE NONCLUSTERED INDEX [IX_Atoms_ReferenceCount] ON [dbo].[Atoms]([ReferenceCount] DESC) INCLUDE ([AtomId], [Modality]);
GO

-- DEPRECATED: Backward compatibility index for TenantId (remove in future version)
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Atoms_TenantId_DEPRECATED' AND object_id = OBJECT_ID('dbo.Atoms'))
    CREATE NONCLUSTERED INDEX [IX_Atoms_TenantId_DEPRECATED] ON [dbo].[Atoms]([TenantId]) WHERE [TenantId] IS NOT NULL;
GO
