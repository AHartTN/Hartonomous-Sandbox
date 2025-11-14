-- =============================================
-- ROLLBACK: Atomic Vector Decomposition
-- =============================================
-- Safely reverts atomic migration back to monolithic
-- EmbeddingVector storage. Only works if EmbeddingVector
-- column was not dropped (Step 7 in migration script).
-- =============================================

SET NOCOUNT ON;
SET XACT_ABORT ON;
GO

PRINT '========================================';
PRINT 'ATOMIC MIGRATION ROLLBACK';
PRINT '========================================';
PRINT 'This script will:';
PRINT '  1. Verify EmbeddingVector column exists';
PRINT '  2. Delete atomic relations';
PRINT '  3. Cleanup orphaned atoms';
PRINT '  4. Reset migration tracking';
PRINT '  5. Restore original state';
PRINT '';
PRINT 'WARNING: Ensure database backup exists!';
PRINT '========================================';
GO

-- Step 1: Verify EmbeddingVector exists
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.AtomEmbeddings') AND name = 'EmbeddingVector')
BEGIN
    RAISERROR('FATAL: EmbeddingVector column does not exist! Cannot rollback.', 16, 1);
    RAISERROR('Restore from backup to recover monolithic storage.', 16, 1);
    RETURN;
END

PRINT '✓ EmbeddingVector column exists - rollback possible.';
GO

-- Step 2: Count atomic relations to be deleted
DECLARE @RelationCount INT;

SELECT @RelationCount = COUNT(*)
FROM dbo.AtomRelations
WHERE RelationType = 'embedding_dimension';

PRINT 'Found ' + CAST(@RelationCount AS NVARCHAR(20)) + ' atomic relations to delete.';
GO

-- Step 3: Disable system-versioning temporarily
IF EXISTS (
    SELECT 1 FROM sys.tables 
    WHERE name = 'AtomRelations' 
    AND temporal_type = 2
)
BEGIN
    PRINT 'Disabling system-versioning on AtomRelations...';
    ALTER TABLE dbo.AtomRelations SET (SYSTEM_VERSIONING = OFF);
END
GO

-- Step 4: Delete atomic relations
PRINT 'Deleting atomic embedding relations...';
GO

BEGIN TRANSACTION;

-- Track affected atoms for cleanup
DECLARE @AffectedAtoms TABLE (AtomId BIGINT PRIMARY KEY);

INSERT INTO @AffectedAtoms (AtomId)
SELECT DISTINCT TargetAtomId
FROM dbo.AtomRelations
WHERE RelationType = 'embedding_dimension';

DECLARE @AffectedCount INT = @@ROWCOUNT;
PRINT '  Identified ' + CAST(@AffectedCount AS NVARCHAR(20)) + ' potentially orphaned atoms.';

-- Delete relations
DELETE FROM dbo.AtomRelations
WHERE RelationType = 'embedding_dimension';

DECLARE @DeletedRelations INT = @@ROWCOUNT;
PRINT '  Deleted ' + CAST(@DeletedRelations AS NVARCHAR(20)) + ' atomic relations.';

-- Update reference counts
UPDATE a
SET ReferenceCount = ReferenceCount - (
    SELECT COUNT(*)
    FROM dbo.AtomRelations ar_del
    WHERE ar_del.TargetAtomId = a.AtomId
      AND ar_del.RelationType = 'embedding_dimension'
)
FROM dbo.Atoms a
INNER JOIN @AffectedAtoms aa ON aa.AtomId = a.AtomId;

COMMIT TRANSACTION;

PRINT '  Reference counts updated.';
GO

-- Step 5: Cleanup orphaned atoms
PRINT 'Cleaning up orphaned atoms...';
GO

BEGIN TRANSACTION;

DELETE FROM dbo.Atoms
WHERE ReferenceCount <= 0
  AND Modality = 'numeric'
  AND Subtype = 'float64';

DECLARE @DeletedAtoms INT = @@ROWCOUNT;

COMMIT TRANSACTION;

PRINT '  Deleted ' + CAST(@DeletedAtoms AS NVARCHAR(20)) + ' orphaned atoms.';
GO

-- Step 6: Delete migration tracking records
IF EXISTS (SELECT 1 FROM sys.tables WHERE name = 'EmbeddingMigrationProgress')
BEGIN
    PRINT 'Clearing migration tracking data...';
    
    TRUNCATE TABLE dbo.EmbeddingMigrationProgress;
    
    PRINT '  Migration progress reset.';
END
GO

-- Step 7: Drop indexed view (if exists)
IF OBJECT_ID('dbo.vw_EmbeddingVectors', 'V') IS NOT NULL
BEGIN
    PRINT 'Dropping atomic reconstruction view...';
    DROP VIEW dbo.vw_EmbeddingVectors;
    PRINT '  View dropped.';
END
GO

-- Step 8: Drop atomic procedures
PRINT 'Dropping atomic stored procedures...';
GO

DROP PROCEDURE IF EXISTS dbo.sp_DecomposeEmbeddingToAtomic;
DROP PROCEDURE IF EXISTS dbo.sp_ReconstructVector;
DROP PROCEDURE IF EXISTS dbo.sp_AtomicSpatialSearch;
DROP PROCEDURE IF EXISTS dbo.sp_InsertAtomicVector;
DROP PROCEDURE IF EXISTS dbo.sp_DeleteAtomicVectors;
DROP PROCEDURE IF EXISTS dbo.sp_GetAtomicDeduplicationStats;
DROP FUNCTION IF EXISTS dbo.fn_ComputeSpatialBucket;

PRINT '  Procedures dropped.';
GO

-- Step 9: Re-enable system-versioning (if it was enabled)
IF EXISTS (SELECT 1 FROM sys.tables WHERE name = 'AtomRelations_History')
BEGIN
    PRINT 'Re-enabling system-versioning on AtomRelations...';
    
    ALTER TABLE dbo.AtomRelations
    SET (SYSTEM_VERSIONING = ON (
        HISTORY_TABLE = dbo.AtomRelations_History,
        DATA_CONSISTENCY_CHECK = ON,
        HISTORY_RETENTION_PERIOD = 90 DAYS
    ));
    
    PRINT '  System-versioning re-enabled.';
END
GO

-- Step 10: Verify rollback integrity
PRINT 'Verifying rollback integrity...';
GO

DECLARE @RemainingRelations INT;
SELECT @RemainingRelations = COUNT(*)
FROM dbo.AtomRelations
WHERE RelationType = 'embedding_dimension';

IF @RemainingRelations > 0
BEGIN
    RAISERROR('WARNING: %d atomic relations still exist!', 10, 1, @RemainingRelations);
END
ELSE
BEGIN
    PRINT '✓ All atomic relations removed.';
END

DECLARE @OrphanAtoms INT;
SELECT @OrphanAtoms = COUNT(*)
FROM dbo.Atoms
WHERE ReferenceCount = 0
  AND Modality = 'numeric'
  AND Subtype = 'float64';

IF @OrphanAtoms > 0
BEGIN
    RAISERROR('WARNING: %d orphaned float atoms still exist.', 10, 1, @OrphanAtoms);
END
ELSE
BEGIN
    PRINT '✓ No orphaned atoms remaining.';
END
GO

-- Step 11: Update statistics
PRINT 'Updating statistics...';
GO

UPDATE STATISTICS dbo.Atoms WITH FULLSCAN;
UPDATE STATISTICS dbo.AtomRelations WITH FULLSCAN;
UPDATE STATISTICS dbo.AtomEmbeddings WITH FULLSCAN;
GO

PRINT '========================================';
PRINT 'ROLLBACK COMPLETE!';
PRINT '========================================';
PRINT 'Summary:';
PRINT '  - Atomic relations: DELETED';
PRINT '  - Orphaned atoms: CLEANED UP';
PRINT '  - Migration tracking: RESET';
PRINT '  - EmbeddingVector: PRESERVED';
PRINT '';
PRINT 'Database restored to pre-migration state.';
PRINT 'Original monolithic VECTOR(1998) storage active.';
PRINT '========================================';
GO
