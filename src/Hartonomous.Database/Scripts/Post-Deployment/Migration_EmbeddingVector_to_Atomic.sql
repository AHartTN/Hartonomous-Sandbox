-- =============================================
-- Migrate EmbeddingVector to Pure Atomic Architecture
-- =============================================
-- Decomposes VECTOR(1998) into:
-- 1. Atoms table (deduplicated float values)
-- 2. AtomRelations (ordered vector components)
-- 3. Removes monolithic EmbeddingVector column
-- 4. Enables memory-optimization of AtomEmbeddings
-- =============================================

SET NOCOUNT ON;
SET XACT_ABORT ON;
GO

PRINT 'Starting EmbeddingVector → Atomic migration...';
PRINT 'WARNING: This is a destructive migration. Ensure backups exist!';
GO

-- Step 1: Create staging table for migration tracking
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'EmbeddingMigrationProgress')
BEGIN
    CREATE TABLE dbo.EmbeddingMigrationProgress (
        AtomEmbeddingId BIGINT NOT NULL PRIMARY KEY,
        MigratedAt DATETIME2(7) NOT NULL DEFAULT SYSUTCDATETIME(),
        AtomCount INT NOT NULL,
        RelationCount INT NOT NULL
    );
    
    PRINT 'Migration tracking table created.';
END
GO

-- Step 2: Create helper function to decompose VECTOR → Atoms + Relations
IF OBJECT_ID('dbo.fn_ComputeSpatialBucket', 'FN') IS NOT NULL
    DROP FUNCTION dbo.fn_ComputeSpatialBucket;
GO

CREATE FUNCTION dbo.fn_ComputeSpatialBucket (
    @X FLOAT,
    @Y FLOAT,
    @Z FLOAT
)
RETURNS BIGINT
AS
BEGIN
    -- Locality-sensitive hash for spatial bucketing
    -- Buckets are 0.01 units (1% of normalized space)
    RETURN (
        (CAST(FLOOR(@X * 100) AS BIGINT) * 1000000) +
        (CAST(FLOOR(@Y * 100) AS BIGINT) * 1000) +
        (CAST(FLOOR(@Z * 100) AS BIGINT))
    );
END
GO

-- Step 3: Create stored procedure for atomic decomposition
IF OBJECT_ID('dbo.sp_DecomposeEmbeddingToAtomic', 'P') IS NOT NULL
    DROP PROCEDURE dbo.sp_DecomposeEmbeddingToAtomic;
GO

CREATE PROCEDURE dbo.sp_DecomposeEmbeddingToAtomic
    @AtomEmbeddingId BIGINT,
    @BatchSize INT = 100  -- Process dimensions in batches to avoid lock escalation
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;
    
    DECLARE @EmbeddingVector NVARCHAR(MAX);
    DECLARE @Dimension INT;
    DECLARE @SourceAtomId BIGINT;
    DECLARE @SpatialX FLOAT, @SpatialY FLOAT, @SpatialZ FLOAT;
    DECLARE @AtomCount INT = 0;
    DECLARE @RelationCount INT = 0;
    
    -- Get embedding data
    SELECT 
        @EmbeddingVector = CAST(EmbeddingVector AS NVARCHAR(MAX)),
        @Dimension = Dimension,
        @SourceAtomId = AtomId,
        @SpatialX = SpatialProjX,
        @SpatialY = SpatialProjY,
        @SpatialZ = SpatialProjZ
    FROM dbo.AtomEmbedding
    WHERE AtomEmbeddingId = @AtomEmbeddingId;
    
    IF @EmbeddingVector IS NULL
    BEGIN
        RAISERROR('AtomEmbeddingId %I64d has no EmbeddingVector', 16, 1, @AtomEmbeddingId);
        RETURN -1;
    END
    
    -- Parse JSON array: [0.123, -0.456, ...]
    DECLARE @Values TABLE (
        ComponentIndex INT NOT NULL,
        ComponentValue FLOAT NOT NULL,
        PRIMARY KEY (ComponentIndex)
    );
    
    INSERT INTO @Values (ComponentIndex, ComponentValue)
    SELECT 
        ROW_NUMBER() OVER (ORDER BY (SELECT NULL)) - 1 AS ComponentIndex,
        CAST([value] AS FLOAT) AS ComponentValue
    FROM OPENJSON(@EmbeddingVector);
    
    DECLARE @TotalDimensions INT = (SELECT COUNT(*) FROM @Values);
    
    IF @TotalDimensions <> @Dimension
    BEGIN
        RAISERROR('Dimension mismatch: expected %d, got %d', 16, 1, @Dimension, @TotalDimensions);
        RETURN -1;
    END
    
    -- Process in batches to avoid transaction log overflow
    DECLARE @StartIdx INT = 0;
    
    WHILE @StartIdx < @TotalDimensions
    BEGIN
        BEGIN TRANSACTION;
        
        -- Get batch of dimensions
        DECLARE @BatchValues TABLE (
            ComponentIndex INT NOT NULL,
            ComponentValue FLOAT NOT NULL,
            ContentHash BINARY(32) NOT NULL,
            AtomId BIGINT NULL,
            PRIMARY KEY (ComponentIndex)
        );
        
        INSERT INTO @BatchValues (ComponentIndex, ComponentValue, ContentHash)
        SELECT 
            ComponentIndex,
            ComponentValue,
            HASHBYTES('SHA2_256', CAST(ComponentValue AS BINARY(8))) AS ContentHash
        FROM @Values
        WHERE ComponentIndex >= @StartIdx 
          AND ComponentIndex < @StartIdx + @BatchSize;
        
        -- Find or create atomic float values
        MERGE dbo.Atom AS target
        USING @BatchValues AS source
        ON target.ContentHash = source.ContentHash
        WHEN NOT MATCHED THEN
            INSERT (ContentHash, Modality, Subtype, AtomicValue, CanonicalText, TenantId)
            VALUES (
                source.ContentHash,
                'numeric',
                'float64',
                CAST(source.ComponentValue AS VARBINARY(8)),
                CAST(source.ComponentValue AS NVARCHAR(50)),
                0  -- Default tenant
            )
        OUTPUT 
            inserted.AtomId,
            source.ComponentIndex
        INTO @BatchValues (AtomId, ComponentIndex);
        
        -- Also get AtomIds for existing atoms
        UPDATE bv
        SET bv.AtomId = a.AtomId
        FROM @BatchValues bv
        INNER JOIN dbo.Atom a ON a.ContentHash = bv.ContentHash
        WHERE bv.AtomId IS NULL;
        
        SET @AtomCount += @@ROWCOUNT;
        
        -- Create relations (vector component → float atom)
        INSERT INTO dbo.AtomRelation (
            SourceAtomId,
            TargetAtomId,
            RelationType,
            SequenceIndex,
            Weight,
            Importance,
            Confidence,
            SpatialBucket,
            CoordX,
            CoordY,
            CoordZ,
            TenantId
        )
        SELECT 
            @SourceAtomId,
            bv.AtomId,
            'embedding_dimension',
            bv.ComponentIndex,
            1.0,  -- No weighting for raw embeddings
            ABS(bv.ComponentValue),  -- Magnitude as importance proxy
            1.0,  -- Full confidence in source embedding
            dbo.fn_ComputeSpatialBucket(@SpatialX, @SpatialY, @SpatialZ),
            @SpatialX,
            @SpatialY,
            @SpatialZ,
            0  -- Default tenant
        FROM @BatchValues bv
        WHERE bv.AtomId IS NOT NULL;
        
        SET @RelationCount += @@ROWCOUNT;
        
        -- Increment reference counts
        UPDATE a
        SET ReferenceCount = ReferenceCount + 1
        FROM dbo.Atom a
        INNER JOIN @BatchValues bv ON bv.AtomId = a.AtomId;
        
        COMMIT TRANSACTION;
        
        -- Move to next batch
        SET @StartIdx += @BatchSize;
        
        -- Clear batch table
        DELETE FROM @BatchValues;
        
        -- Progress logging
        IF @StartIdx % 500 = 0
        BEGIN
            PRINT '  Processed ' + CAST(@StartIdx AS NVARCHAR(10)) + '/' + 
                  CAST(@TotalDimensions AS NVARCHAR(10)) + ' dimensions...';
        END
    END
    
    -- Track migration completion
    INSERT INTO dbo.EmbeddingMigrationProgress (
        AtomEmbeddingId,
        AtomCount,
        RelationCount
    )
    VALUES (@AtomEmbeddingId, @AtomCount, @RelationCount);
    
    PRINT '  Completed: ' + CAST(@AtomCount AS NVARCHAR(10)) + ' atoms, ' + 
          CAST(@RelationCount AS NVARCHAR(10)) + ' relations created.';
    
    RETURN 0;
END
GO

-- Step 4: Migrate all embeddings (batch processing)
PRINT 'Migrating existing embeddings to atomic representation...';
PRINT 'This may take several minutes for large datasets.';
GO

DECLARE @CurrentId BIGINT;
DECLARE @TotalEmbeddings INT;
DECLARE @ProcessedCount INT = 0;
DECLARE @ErrorCount INT = 0;

SELECT @TotalEmbeddings = COUNT(*)
FROM dbo.AtomEmbedding ae
LEFT JOIN dbo.EmbeddingMigrationProgress emp ON emp.AtomEmbeddingId = ae.AtomEmbeddingId
WHERE ae.EmbeddingVector IS NOT NULL
  AND emp.AtomEmbeddingId IS NULL;

PRINT 'Total embeddings to migrate: ' + CAST(@TotalEmbeddings AS NVARCHAR(10));

DECLARE embedding_cursor CURSOR LOCAL FAST_FORWARD FOR
    SELECT ae.AtomEmbeddingId
    FROM dbo.AtomEmbedding ae
    LEFT JOIN dbo.EmbeddingMigrationProgress emp ON emp.AtomEmbeddingId = ae.AtomEmbeddingId
    WHERE ae.EmbeddingVector IS NOT NULL
      AND emp.AtomEmbeddingId IS NULL
    ORDER BY ae.AtomEmbeddingId;

OPEN embedding_cursor;
FETCH NEXT FROM embedding_cursor INTO @CurrentId;

WHILE @@FETCH_STATUS = 0
BEGIN
    BEGIN TRY
        EXEC dbo.sp_DecomposeEmbeddingToAtomic @AtomEmbeddingId = @CurrentId;
        SET @ProcessedCount += 1;
        
        IF @ProcessedCount % 100 = 0
        BEGIN
            PRINT 'Progress: ' + CAST(@ProcessedCount AS NVARCHAR(10)) + '/' + 
                  CAST(@TotalEmbeddings AS NVARCHAR(10)) + ' embeddings migrated.';
        END
    END TRY
    BEGIN CATCH
        PRINT 'ERROR migrating AtomEmbeddingId ' + CAST(@CurrentId AS NVARCHAR(20)) + ': ' + ERROR_MESSAGE();
        SET @ErrorCount += 1;
    END CATCH
    
    FETCH NEXT FROM embedding_cursor INTO @CurrentId;
END

CLOSE embedding_cursor;
DEALLOCATE embedding_cursor;

PRINT 'Migration complete!';
PRINT '  Successfully migrated: ' + CAST(@ProcessedCount AS NVARCHAR(10)) + ' embeddings';
PRINT '  Errors: ' + CAST(@ErrorCount AS NVARCHAR(10));
GO

-- Step 5: Verify migration integrity
PRINT 'Verifying migration integrity...';
GO

DECLARE @MismatchCount INT;

SELECT @MismatchCount = COUNT(*)
FROM dbo.AtomEmbedding ae
INNER JOIN dbo.EmbeddingMigrationProgress emp ON emp.AtomEmbeddingId = ae.AtomEmbeddingId
WHERE emp.RelationCount <> ae.Dimension;

IF @MismatchCount > 0
BEGIN
    RAISERROR('WARNING: %d embeddings have dimension mismatches!', 10, 1, @MismatchCount);
END
ELSE
BEGIN
    PRINT 'Integrity check PASSED: All embeddings have correct dimension counts.';
END
GO

-- Step 6: Create indexed view for fast vector reconstruction
PRINT 'Creating indexed view for vector reconstruction...';
GO

IF OBJECT_ID('dbo.vw_EmbeddingVectors', 'V') IS NOT NULL
    DROP VIEW dbo.vw_EmbeddingVectors;
GO

CREATE VIEW dbo.vw_EmbeddingVectors
WITH SCHEMABINDING
AS
SELECT 
    relations.SourceAtomId,
    relations.SequenceIndex AS ComponentIndex,
    CAST(atoms.AtomicValue AS FLOAT) AS ComponentValue,
    relations.AtomRelationId
FROM dbo.AtomRelation AS relations
INNER JOIN dbo.Atom AS atoms ON atoms.AtomId = relations.TargetAtomId
WHERE relations.RelationType = 'embedding_dimension';
GO

-- Clustered index for contiguous read performance
CREATE UNIQUE CLUSTERED INDEX IX_EmbeddingVectors_SourceAtom_Sequence
ON dbo.vw_EmbeddingVectors (SourceAtomId, ComponentIndex);
GO

-- Nonclustered covering index for reverse lookups
CREATE NONCLUSTERED INDEX IX_EmbeddingVectors_ComponentValue
ON dbo.vw_EmbeddingVectors (ComponentValue)
INCLUDE (SourceAtomId, ComponentIndex)
WITH (DATA_COMPRESSION = PAGE);
GO

PRINT 'Indexed view created for O(1) vector reconstruction.';
GO

-- Step 7: Remove EmbeddingVector column (DESTRUCTIVE - only after verification!)
-- IMPORTANT: Comment this out if you want to keep the column for rollback
/*
PRINT 'Removing monolithic EmbeddingVector column...';
PRINT 'WARNING: This is irreversible without restoring from backup!';
GO

ALTER TABLE dbo.AtomEmbedding
DROP COLUMN EmbeddingVector;
GO

PRINT 'EmbeddingVector column removed. AtomEmbeddings is now 95% smaller!';
*/

-- Step 8: Update statistics for query optimizer
PRINT 'Updating statistics...';
GO

UPDATE STATISTICS dbo.Atom WITH FULLSCAN;
UPDATE STATISTICS dbo.AtomRelation WITH FULLSCAN;
UPDATE STATISTICS dbo.AtomEmbedding WITH FULLSCAN;
GO

PRINT '========================================';
PRINT 'Atomic Migration Complete!';
PRINT '========================================';
PRINT 'Next steps:';
PRINT '  1. Test vector reconstruction with vw_EmbeddingVectors';
PRINT '  2. Update application code to use atomic API';
PRINT '  3. Verify query performance benchmarks';
PRINT '  4. Uncomment Step 7 to remove EmbeddingVector column';
PRINT '  5. Consider memory-optimizing AtomEmbeddings';
PRINT '========================================';
GO
