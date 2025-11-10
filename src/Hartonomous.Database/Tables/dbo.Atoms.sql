-- =============================================
-- dbo.Atoms: The Core Metadata Table for All Atoms
-- =============================================
-- This table is the single source of truth for the metadata of every "atom"
-- in the system. It is the relational core of the atomization concept.
-- Originally defined as a graph node, it is defined here as a standard
-- relational table to serve as a clear, primary component of the schema.
-- =============================================

CREATE TABLE dbo.Atoms
(
    AtomId              BIGINT          IDENTITY(1,1) NOT NULL PRIMARY KEY,
    Modality            NVARCHAR(64)    NOT NULL,
    Subtype             NVARCHAR(64)    NULL,
    SourceType          NVARCHAR(128)   NULL,
    SourceUri           NVARCHAR(2048)  NULL,
    
    -- The ContentHash is the primary key for deduplication and lives with the payload.
    -- This column provides a cached, denormalized reference for convenience.
    ContentHash         BINARY(32)      NOT NULL,

    -- ReferenceCount tracks how many times this exact content has been ingested.
    ReferenceCount      INT             NOT NULL DEFAULT 1,

    CanonicalText       NVARCHAR(MAX)   NULL,
    Metadata            NVARCHAR(MAX)   NULL, -- Storing as NVARCHAR for broader compatibility, can be validated as JSON.
    Semantics           NVARCHAR(MAX)   NULL, -- Storing as NVARCHAR for broader compatibility, can be validated as JSON.
    
    TenantId            INT             NOT NULL DEFAULT 0,
    IsDeleted           BIT             NOT NULL DEFAULT 0,
    
    CreatedAt           DATETIME2       NOT NULL DEFAULT SYSUTCDATETIME(),
    UpdatedAt           DATETIME2       NOT NULL DEFAULT SYSUTCDATETIME(),
    DeletedAt           DATETIME2       NULL
);
GO

CREATE UNIQUE NONCLUSTERED INDEX [UX_Atoms_ContentHash_TenantId]
    ON [dbo].[Atoms]([ContentHash], [TenantId])
    WHERE [IsDeleted] = 0;
GO

CREATE NONCLUSTERED INDEX [IX_Atoms_Modality_Subtype]
    ON [dbo].[Atoms]([Modality], [Subtype]);
GO