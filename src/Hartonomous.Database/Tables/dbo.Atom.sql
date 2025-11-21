-- =============================================
-- Atoms: Core Atomic Storage
-- Enterprise-grade atomic decomposition with full metadata support
-- =============================================
CREATE TABLE [dbo].[Atom] (
    [AtomId]          BIGINT           IDENTITY (1, 1) NOT NULL,
    [TenantId]        INT              NOT NULL DEFAULT 0,
    [Modality]        VARCHAR(50)      NOT NULL,
    [Subtype]         VARCHAR(50)      NULL,
    [ContentHash]     BINARY(32)       NOT NULL,

    -- Core classification
    [ContentType]     NVARCHAR(100)    NULL,  -- Semantic type: 'text', 'code', 'image', 'weight', etc.
    [SourceType]      NVARCHAR(100)    NULL,  -- Origin type: 'upload', 'generated', 'extracted', etc.
    [ConceptId]       BIGINT           NULL,  -- Semantic concept/cluster assignment

    -- Source tracking
    [SourceUri]       NVARCHAR(2048)   NULL,  -- Original source location or reference
    [SourceId]        BIGINT           NULL,  -- Optional source entity reference
    [CanonicalText]   NVARCHAR(MAX)    NULL,  -- Normalized text representation (for text atoms)

    -- Extensible metadata (native JSON for SQL Server 2025+)
    [Metadata]        json             NULL,  -- JSON metadata for extensibility (binary format, optimized storage)

    -- Schema-level governance: Max 64 bytes enforces atomic decomposition
    [AtomicValue]     VARBINARY(64)    NULL,

    -- Ingestion tracking
    [BatchId]         UNIQUEIDENTIFIER NULL,  -- Ingestion batch identifier for bulk operations

    -- Temporal columns
    [CreatedAt]       DATETIME2(7)     GENERATED ALWAYS AS ROW START NOT NULL,
    [ModifiedAt]      DATETIME2(7)     GENERATED ALWAYS AS ROW END NOT NULL,

    -- Reference counting for deduplication
    [ReferenceCount]  BIGINT           NOT NULL DEFAULT 1,

    CONSTRAINT [PK_Atom] PRIMARY KEY CLUSTERED ([AtomId] ASC),
    CONSTRAINT [UX_Atom_ContentHash] UNIQUE NONCLUSTERED ([ContentHash] ASC),
    CONSTRAINT [FK_Atom_Concept] FOREIGN KEY ([ConceptId]) REFERENCES [dbo].[Concept]([ConceptId]),
    PERIOD FOR SYSTEM_TIME ([CreatedAt], [ModifiedAt])
)
WITH (SYSTEM_VERSIONING = ON (HISTORY_TABLE = [dbo].[AtomHistory]));
GO

-- Performance indexes
CREATE NONCLUSTERED INDEX [IX_Atom_Modality]
    ON [dbo].[Atom]([Modality], [Subtype])
    INCLUDE ([AtomId], [ContentHash], [TenantId]);
GO

CREATE NONCLUSTERED INDEX [IX_Atom_ContentType]
    ON [dbo].[Atom]([ContentType])
    INCLUDE ([AtomId], [Modality], [TenantId])
    WHERE [ContentType] IS NOT NULL;
GO

CREATE NONCLUSTERED INDEX [IX_Atom_TenantId]
    ON [dbo].[Atom]([TenantId], [Modality])
    INCLUDE ([AtomId], [ContentHash]);
GO

CREATE NONCLUSTERED INDEX [IX_Atom_ReferenceCount]
    ON [dbo].[Atom]([ReferenceCount] DESC)
    INCLUDE ([AtomId], [Modality]);
GO

CREATE NONCLUSTERED INDEX [IX_Atom_BatchId]
    ON [dbo].[Atom]([BatchId])
    INCLUDE ([AtomId], [TenantId], [CreatedAt])
    WHERE [BatchId] IS NOT NULL;
GO

CREATE NONCLUSTERED INDEX [IX_Atom_ConceptId]
    ON [dbo].[Atom]([ConceptId])
    INCLUDE ([AtomId], [TenantId], [Modality])
    WHERE [ConceptId] IS NOT NULL;
GO
