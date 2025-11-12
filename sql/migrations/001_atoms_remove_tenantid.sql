-- =============================================
-- Migration: Remove TenantId from Atoms table for true CAS
-- =============================================
-- Before: Atoms has TenantId, ContentHash duplicated per tenant
-- After:  Atoms is globally deduplicated, TenantAtoms junction provides tenant isolation
-- =============================================

BEGIN TRANSACTION;

-- Step 1: Create TenantAtoms junction table if doesn't exist
IF OBJECT_ID('dbo.TenantAtoms', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.TenantAtoms
    (
        TenantAtomId        BIGINT          IDENTITY(1,1) NOT NULL PRIMARY KEY,
        TenantId            INT             NOT NULL,
        AtomId              BIGINT          NOT NULL,
        TenantMetadata      NVARCHAR(MAX)   NULL,
        AccessCount         INT             NOT NULL DEFAULT 0,
        LastAccessedAt      DATETIME2       NULL,
        IsDeleted           BIT             NOT NULL DEFAULT 0,
        CreatedAt           DATETIME2       NOT NULL DEFAULT SYSUTCDATETIME(),
        DeletedAt           DATETIME2       NULL,
        
        CONSTRAINT UX_TenantAtoms_Tenant_Atom UNIQUE (TenantId, AtomId)
    );
    
    CREATE INDEX IX_TenantAtoms_TenantId ON dbo.TenantAtoms(TenantId, IsDeleted) INCLUDE (AtomId);
    CREATE INDEX IX_TenantAtoms_AtomId ON dbo.TenantAtoms(AtomId, IsDeleted) INCLUDE (TenantId);
    
    PRINT 'Created dbo.TenantAtoms junction table';
END

-- Step 2: Migrate existing Atom data
-- Find all unique (ContentHash, TenantId) pairs and create junction entries
INSERT INTO dbo.TenantAtoms (TenantId, AtomId, CreatedAt)
SELECT DISTINCT 
    TenantId,
    AtomId,
    CreatedAt
FROM dbo.Atoms
WHERE NOT EXISTS (
    SELECT 1 FROM dbo.TenantAtoms ta 
    WHERE ta.TenantId = Atoms.TenantId AND ta.AtomId = Atoms.AtomId
);

PRINT 'Migrated ' + CAST(@@ROWCOUNT AS VARCHAR(10)) + ' tenant-atom relationships';

-- Step 3: Drop old unique index on (ContentHash, TenantId)
IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'UX_Atoms_ContentHash_TenantId' AND object_id = OBJECT_ID(N'dbo.Atoms'))
BEGIN
    DROP INDEX UX_Atoms_ContentHash_TenantId ON dbo.Atoms;
    PRINT 'Dropped old UX_Atoms_ContentHash_TenantId index';
END

-- Step 4: Create new unique index on ContentHash alone (global deduplication)
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'UX_Atoms_ContentHash' AND object_id = OBJECT_ID(N'dbo.Atoms'))
BEGIN
    CREATE UNIQUE INDEX UX_Atoms_ContentHash ON dbo.Atoms(ContentHash) WHERE IsDeleted = 0;
    PRINT 'Created new UX_Atoms_ContentHash unique index for global deduplication';
END

-- Step 5: Drop TenantId column from Atoms (data already migrated to junction)
-- NOTE: This is commented out for safety - review before executing
-- ALTER TABLE dbo.Atoms DROP COLUMN TenantId;
PRINT 'WARNING: TenantId column NOT dropped from Atoms - do this manually after verification';

COMMIT TRANSACTION;

PRINT 'Migration complete: Atoms now uses true content-addressable storage';
PRINT 'Next steps:';
PRINT '  1. Update sp_IngestAtom to use ContentHash-only lookup';
PRINT '  2. Update CLR functions to JOIN through TenantAtoms';
PRINT '  3. After verification, manually drop Atoms.TenantId column';
GO
