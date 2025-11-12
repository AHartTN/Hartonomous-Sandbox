-- =============================================
-- dbo.TenantAtoms: Junction table for many-to-many Tenant ↔ Atom relationship
-- =============================================
-- Enables true content-addressable storage where:
-- - One Atom (e.g., "Llama Maverick model") exists once globally
-- - Multiple Tenants can reference the same Atom
-- - Deduplication happens across ALL tenants
-- - Tenant isolation maintained through this junction table
-- =============================================

IF OBJECT_ID('dbo.TenantAtoms', 'U') IS NOT NULL
    DROP TABLE dbo.TenantAtoms;
GO

CREATE TABLE dbo.TenantAtoms
(
    TenantAtomId        BIGINT          IDENTITY(1,1) NOT NULL PRIMARY KEY,
    TenantId            INT             NOT NULL,
    AtomId              BIGINT          NOT NULL,
    
    -- Tenant-specific metadata (if different from global Atom metadata)
    TenantMetadata      NVARCHAR(MAX)   NULL,
    
    -- Access tracking
    AccessCount         INT             NOT NULL DEFAULT 0,
    LastAccessedAt      DATETIME2       NULL,
    
    -- Lifecycle
    IsDeleted           BIT             NOT NULL DEFAULT 0,
    CreatedAt           DATETIME2       NOT NULL DEFAULT SYSUTCDATETIME(),
    DeletedAt           DATETIME2       NULL,
    
    CONSTRAINT FK_TenantAtoms_Atoms FOREIGN KEY (AtomId)
        REFERENCES dbo.Atoms(AtomId),
    
    -- Ensure one tenant can only reference an atom once
    CONSTRAINT UX_TenantAtoms_Tenant_Atom UNIQUE (TenantId, AtomId)
);
GO

-- Index for tenant queries (find all atoms for a tenant)
CREATE INDEX IX_TenantAtoms_TenantId 
ON dbo.TenantAtoms(TenantId, IsDeleted)
INCLUDE (AtomId, AccessCount);
GO

-- Index for atom queries (find all tenants using an atom)
CREATE INDEX IX_TenantAtoms_AtomId 
ON dbo.TenantAtoms(AtomId, IsDeleted)
INCLUDE (TenantId, AccessCount);
GO

PRINT 'Created dbo.TenantAtoms junction table for true content-addressable storage with tenant isolation.';
GO
