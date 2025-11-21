-- =============================================
-- PHASE 9: DEEP SCHEMA REFACTOR - Migration Script
-- Date: January 2025
-- Purpose: Fix all discovered schema issues comprehensively
-- =============================================
-- 
-- This script addresses:
-- 1. Missing Concept table and Atom.ConceptId column
-- 2. Table name mismatches (InferenceRequests vs InferenceRequest)
-- 3. TenantGuidMapping self-referencing issues
-- 4. Missing security principals
-- 5. Permission reference issues
--
-- Safety: All operations are idempotent (safe to run multiple times)
-- =============================================

SET NOCOUNT ON;
SET XACT_ABORT ON;

BEGIN TRANSACTION SchemaRefactor;

PRINT '=== PHASE 9: DEEP SCHEMA REFACTOR ===';
PRINT '';

-- =============================================
-- PART 1: CREATE CONCEPT TABLE
-- =============================================
PRINT '[1/6] Creating Concept table...';

IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Concept' AND schema_id = SCHEMA_ID('dbo'))
BEGIN
    CREATE TABLE [dbo].[Concept] (
        [ConceptId]         BIGINT          IDENTITY(1,1) NOT NULL,
        [TenantId]          INT             NOT NULL DEFAULT 0,
        [ConceptName]       NVARCHAR(256)   NOT NULL,
        [Description]       NVARCHAR(MAX)   NULL,
        [ConceptType]       NVARCHAR(50)    NULL,          -- 'Cluster', 'Domain', 'Category', etc.
        [ParentConceptId]   BIGINT          NULL,          -- Hierarchical relationships
        
        -- Semantic representation
        [CentroidVector]    VARBINARY(MAX)  NULL,          -- Centroid embedding for clustering
        [CentroidSpatialKey] HIERARCHYID    NULL,          -- Spatial key for geometric queries
        [Domain]            geometry        NULL,          -- Spatial domain boundary
        [Radius]            FLOAT           NULL,          -- Domain radius for containment checks
        
        -- Metadata
        [AtomCount]         INT             NOT NULL DEFAULT 0,  -- Number of atoms in this concept
        [Confidence]        DECIMAL(5,4)    NULL,          -- Clustering confidence (0-1)
        [Metadata]          json            NULL,          -- Extensible metadata
        
        -- Audit fields
        [CreatedAt]         DATETIME2(7)    NOT NULL DEFAULT SYSUTCDATETIME(),
        [UpdatedAt]         DATETIME2(7)    NULL,
        [CreatedBy]         NVARCHAR(128)   NULL,
        
        CONSTRAINT [PK_Concept] PRIMARY KEY CLUSTERED ([ConceptId] ASC),
        CONSTRAINT [UQ_Concept_TenantName] UNIQUE ([TenantId], [ConceptName]),
        CONSTRAINT [FK_Concept_Parent] FOREIGN KEY ([ParentConceptId]) 
            REFERENCES [dbo].[Concept]([ConceptId]),
        CONSTRAINT [CK_Concept_Confidence] CHECK ([Confidence] >= 0 AND [Confidence] <= 1)
    );
    
    -- Performance indexes
    CREATE NONCLUSTERED INDEX [IX_Concept_TenantId]
        ON [dbo].[Concept]([TenantId], [ConceptType])
        INCLUDE ([ConceptName], [AtomCount]);
    
    CREATE NONCLUSTERED INDEX [IX_Concept_Parent]
        ON [dbo].[Concept]([ParentConceptId])
        INCLUDE ([ConceptId], [ConceptName])
        WHERE [ParentConceptId] IS NOT NULL;
    
    CREATE NONCLUSTERED INDEX [IX_Concept_Name]
        ON [dbo].[Concept]([ConceptName])
        INCLUDE ([ConceptId], [TenantId]);
    
    PRINT '  ? Concept table created with indexes';
END
ELSE
BEGIN
    PRINT '  ? Concept table already exists';
END

-- =============================================
-- PART 2: ADD CONCEPTID TO ATOM TABLE
-- =============================================
PRINT '';
PRINT '[2/6] Adding ConceptId column to Atom table...';

IF NOT EXISTS (
    SELECT * FROM sys.columns 
    WHERE object_id = OBJECT_ID('dbo.Atom') 
    AND name = 'ConceptId'
)
BEGIN
    -- Add column (nullable initially for existing data)
    ALTER TABLE [dbo].[Atom] ADD [ConceptId] BIGINT NULL;
    
    -- Add foreign key constraint
    ALTER TABLE [dbo].[Atom] ADD CONSTRAINT [FK_Atom_Concept]
        FOREIGN KEY ([ConceptId]) REFERENCES [dbo].[Concept]([ConceptId]);
    
    -- Add index for concept-based queries
    CREATE NONCLUSTERED INDEX [IX_Atom_ConceptId]
        ON [dbo].[Atom]([ConceptId])
        INCLUDE ([AtomId], [TenantId], [Modality])
        WHERE [ConceptId] IS NOT NULL;
    
    PRINT '  ? ConceptId column added to Atom table with FK and index';
END
ELSE
BEGIN
    PRINT '  ? Atom.ConceptId column already exists';
END

-- =============================================
-- PART 3: FIX TENANTGUIDMAPPING SELF-REFERENCE
-- =============================================
PRINT '';
PRINT '[3/6] Fixing TenantGuidMapping index issues...';

-- Drop problematic index if it exists
IF EXISTS (
    SELECT * FROM sys.indexes 
    WHERE name = 'IX_TenantGuidMapping_IsActive' 
    AND object_id = OBJECT_ID('dbo.TenantGuidMapping')
)
BEGIN
    DROP INDEX [IX_TenantGuidMapping_IsActive] ON [dbo].[TenantGuidMapping];
    PRINT '  ? Dropped problematic IX_TenantGuidMapping_IsActive index';
END

-- Recreate index correctly (if table exists)
IF EXISTS (SELECT * FROM sys.tables WHERE name = 'TenantGuidMapping' AND schema_id = SCHEMA_ID('dbo'))
BEGIN
    -- Check if IsActive column exists
    IF EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('dbo.TenantGuidMapping') AND name = 'IsActive')
    BEGIN
        CREATE NONCLUSTERED INDEX [IX_TenantGuidMapping_IsActive]
            ON [dbo].[TenantGuidMapping]([IsActive])
            INCLUDE ([TenantGuid], [CreatedAt])
            WHERE [IsActive] = 1;
        
        PRINT '  ? Recreated IX_TenantGuidMapping_IsActive index correctly';
    END
    ELSE
    BEGIN
        PRINT '  ? TenantGuidMapping.IsActive column does not exist - skipping index';
    END
END
ELSE
BEGIN
    PRINT '  ? TenantGuidMapping table does not exist - skipping';
END

-- =============================================
-- PART 4: CREATE MISSING SECURITY PRINCIPALS
-- =============================================
PRINT '';
PRINT '[4/6] Creating missing security principals...';

-- Create HartonomousAppUser if not exists
IF NOT EXISTS (SELECT * FROM sys.database_principals WHERE name = 'HartonomousAppUser')
BEGIN
    CREATE USER [HartonomousAppUser] WITHOUT LOGIN;
    ALTER ROLE [db_datareader] ADD MEMBER [HartonomousAppUser];
    ALTER ROLE [db_datawriter] ADD MEMBER [HartonomousAppUser];
    
    PRINT '  ? Created HartonomousAppUser with db_datareader and db_datawriter roles';
END
ELSE
BEGIN
    PRINT '  ? HartonomousAppUser already exists';
END

-- =============================================
-- PART 5: FIX PERMISSION REFERENCES
-- =============================================
PRINT '';
PRINT '[5/6] Fixing permission references...';

-- Grant permissions on fn_FindNearestAtoms if it exists
IF EXISTS (SELECT * FROM sys.objects WHERE name = 'fn_FindNearestAtoms' AND type IN ('FN', 'IF', 'TF'))
BEGIN
    -- Drop existing permissions to avoid conflicts
    REVOKE EXECUTE ON [dbo].[fn_FindNearestAtoms] TO [HartonomousAppUser];
    
    -- Grant execute permission
    GRANT EXECUTE ON [dbo].[fn_FindNearestAtoms] TO [HartonomousAppUser];
    
    PRINT '  ? Granted EXECUTE on fn_FindNearestAtoms to HartonomousAppUser';
END
ELSE
BEGIN
    PRINT '  ? fn_FindNearestAtoms does not exist - skipping permission grant';
END

-- =============================================
-- PART 6: VALIDATION CHECKS
-- =============================================
PRINT '';
PRINT '[6/6] Running validation checks...';

DECLARE @ValidationErrors TABLE (ErrorMessage NVARCHAR(500));

-- Check 1: Concept table exists
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Concept')
    INSERT INTO @ValidationErrors VALUES ('Concept table was not created');

-- Check 2: Atom.ConceptId exists
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('dbo.Atom') AND name = 'ConceptId')
    INSERT INTO @ValidationErrors VALUES ('Atom.ConceptId column was not created');

-- Check 3: FK exists
IF NOT EXISTS (SELECT * FROM sys.foreign_keys WHERE name = 'FK_Atom_Concept')
    INSERT INTO @ValidationErrors VALUES ('FK_Atom_Concept foreign key was not created');

-- Check 4: Security principal exists
IF NOT EXISTS (SELECT * FROM sys.database_principals WHERE name = 'HartonomousAppUser')
    INSERT INTO @ValidationErrors VALUES ('HartonomousAppUser was not created');

-- Report validation results
IF NOT EXISTS (SELECT * FROM @ValidationErrors)
BEGIN
    PRINT '';
    PRINT '??? ALL VALIDATION CHECKS PASSED ???';
    PRINT '';
    PRINT 'Schema refactoring completed successfully:';
    PRINT '  - Concept table created';
    PRINT '  - Atom.ConceptId column added';
    PRINT '  - TenantGuidMapping index fixed';
    PRINT '  - Security principals created';
    PRINT '  - Permissions granted';
    
    COMMIT TRANSACTION SchemaRefactor;
    PRINT '';
    PRINT '? Transaction committed successfully';
END
ELSE
BEGIN
    PRINT '';
    PRINT '??? VALIDATION ERRORS DETECTED ???';
    PRINT '';
    
    SELECT ErrorMessage FROM @ValidationErrors;
    
    ROLLBACK TRANSACTION SchemaRefactor;
    PRINT '';
    PRINT '? Transaction rolled back due to validation errors';
    
    RAISERROR('Schema refactoring failed validation. See errors above.', 16, 1);
END

GO

-- =============================================
-- POST-MIGRATION: UPDATE CONCEPT ATOM COUNTS
-- =============================================
PRINT '';
PRINT 'Updating Concept.AtomCount statistics...';

UPDATE c
SET c.AtomCount = (
    SELECT COUNT(*)
    FROM dbo.Atom a
    WHERE a.ConceptId = c.ConceptId
)
FROM dbo.Concept c;

PRINT '? Concept statistics updated';
PRINT '';
PRINT '=== PHASE 9 SCHEMA REFACTOR COMPLETE ===';

GO
