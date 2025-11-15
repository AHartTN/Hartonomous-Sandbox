-- =============================================
-- Atoms: Core Atomic Storage
-- Enterprise-grade atomic decomposition with full metadata support
-- =============================================
CREATE TABLE [dbo].[Atoms] (
    [AtomId]          BIGINT           IDENTITY (1, 1) NOT NULL,
    [TenantId]        INT              NOT NULL DEFAULT 0,
    [Modality]        VARCHAR(50)      NOT NULL,
    [Subtype]         VARCHAR(50)      NULL,
    [ContentHash]     BINARY(32)       NOT NULL,

    -- Core classification
    [ContentType]     NVARCHAR(100)    NULL,  -- Semantic type: 'text', 'code', 'image', 'weight', etc.
    [SourceType]      NVARCHAR(100)    NULL,  -- Origin type: 'upload', 'generated', 'extracted', etc.

    -- Source tracking
    [SourceUri]       NVARCHAR(2048)   NULL,  -- Original source location or reference
    [CanonicalText]   NVARCHAR(MAX)    NULL,  -- Normalized text representation (for text atoms)

    -- Extensible metadata
    [Metadata]        NVARCHAR(MAX)    NULL,  -- JSON metadata for extensibility

    -- Schema-level governance: Max 64 bytes enforces atomic decomposition
    [AtomicValue]     VARBINARY(64)    NULL,

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

-- Performance indexes
CREATE NONCLUSTERED INDEX [IX_Atoms_Modality]
    ON [dbo].[Atoms]([Modality], [Subtype])
    INCLUDE ([AtomId], [ContentHash], [TenantId]);
GO

CREATE NONCLUSTERED INDEX [IX_Atoms_ContentType]
    ON [dbo].[Atoms]([ContentType])
    INCLUDE ([AtomId], [Modality], [TenantId])
    WHERE [ContentType] IS NOT NULL;
GO

CREATE NONCLUSTERED INDEX [IX_Atoms_TenantId]
    ON [dbo].[Atoms]([TenantId], [Modality])
    INCLUDE ([AtomId], [ContentHash]);
GO

CREATE NONCLUSTERED INDEX [IX_Atoms_ReferenceCount]
    ON [dbo].[Atoms]([ReferenceCount] DESC)
    INCLUDE ([AtomId], [Modality]);
GO
