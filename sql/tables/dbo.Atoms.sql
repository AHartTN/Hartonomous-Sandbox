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
    
    -- ContentHash is globally unique - one atom per unique content across ALL tenants
    -- SHA-256 ensures content-addressable storage with true deduplication
    ContentHash         BINARY(32)      NOT NULL,

    -- ReferenceCount tracks how many tenants reference this atom
    ReferenceCount      INT             NOT NULL DEFAULT 0,

    CanonicalText       NVARCHAR(MAX)   NULL,
    Metadata            NVARCHAR(MAX)   NULL, -- Storing as NVARCHAR for broader compatibility, can be validated as JSON.
    Semantics           NVARCHAR(MAX)   NULL, -- Storing as NVARCHAR for broader compatibility, can be validated as JSON.
    
    IsDeleted           BIT             NOT NULL DEFAULT 0,
    
    CreatedAt           DATETIME2       NOT NULL DEFAULT SYSUTCDATETIME(),
    UpdatedAt           DATETIME2       NOT NULL DEFAULT SYSUTCDATETIME(),
    DeletedAt           DATETIME2       NULL
);
GO

-- GLOBAL unique index on ContentHash - one atom per content across entire system
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'UX_Atoms_ContentHash' AND object_id = OBJECT_ID(N'dbo.Atoms'))
BEGIN
    CREATE UNIQUE INDEX UX_Atoms_ContentHash ON dbo.Atoms(ContentHash) WHERE IsDeleted = 0;
END
GO

-- Index for common query patterns
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Atoms_Modality_Subtype' AND object_id = OBJECT_ID(N'dbo.Atoms'))
BEGIN
    CREATE INDEX IX_Atoms_Modality_Subtype ON dbo.Atoms(Modality, Subtype);
END
GO

PRINT 'Created dbo.Atoms table with a clean, consolidated schema.';
GO
