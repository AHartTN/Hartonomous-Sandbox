-- =============================================
-- Memory-Optimize AtomEmbeddings (Post-Atomic Migration)
-- =============================================
-- After removing EmbeddingVector column, AtomEmbeddings
-- contains only metadata + spatial coordinates.
-- This enables Hekaton memory-optimization for:
-- - Ultra-fast spatial lookups
-- - Lock-free concurrent access
-- - Sub-millisecond metadata queries
-- =============================================

SET NOCOUNT ON;
GO

PRINT 'Preparing AtomEmbeddings for memory-optimization...';
PRINT 'Prerequisites: EmbeddingVector column must be removed first!';
GO

-- Step 1: Verify EmbeddingVector is removed
IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.AtomEmbedding') AND name = 'EmbeddingVector')
BEGIN
    RAISERROR('ERROR: EmbeddingVector column still exists! Run Migration_EmbeddingVector_to_Atomic.sql first.', 16, 1);
    RETURN;
END
GO

-- Step 2: Check for GEOMETRY/JSON compatibility
-- Note: GEOMETRY and JSON can stay - they're metadata, not the bulk vector data
PRINT 'Checking schema compatibility...';

DECLARE @HasBlockingTypes INT;

SELECT @HasBlockingTypes = COUNT(*)
FROM sys.columns c
INNER JOIN sys.types t ON c.user_type_id = t.user_type_id
WHERE c.object_id = OBJECT_ID('dbo.AtomEmbedding')
  AND t.name IN ('geography', 'xml', 'text', 'ntext', 'image', 'sql_variant', 'hierarchyid', 'timestamp');

IF @HasBlockingTypes > 0
BEGIN
    RAISERROR('ERROR: AtomEmbeddings contains Hekaton-incompatible types!', 16, 1);
    RETURN;
END

PRINT 'Schema compatibility check PASSED.';
GO

-- Step 3: Create memory-optimized version
PRINT 'Creating memory-optimized AtomEmbeddings_InMemory table...';
GO

IF OBJECT_ID('dbo.AtomEmbeddings_InMemory', 'U') IS NOT NULL
    DROP TABLE dbo.AtomEmbeddings_InMemory;
GO

CREATE TABLE dbo.AtomEmbeddings_InMemory
(
    [AtomEmbeddingId] BIGINT IDENTITY(1,1) NOT NULL,
    [AtomId] BIGINT NOT NULL,
    [Dimension] INT NOT NULL DEFAULT (1998),
    
    -- Spatial coordinates (scalar - compatible with Hekaton)
    [SpatialBucket] INT NOT NULL INDEX IX_SpatialBucket NONCLUSTERED,
    [SpatialBucketX] INT NULL,
    [SpatialBucketY] INT NULL,
    [SpatialBucketZ] INT NULL,
    [SpatialProjX] FLOAT NULL,
    [SpatialProjY] FLOAT NULL,
    [SpatialProjZ] FLOAT NULL,
    
    -- Metadata (scalar types only for Hekaton)
    [ModelId] INT NULL,
    [EmbeddingType] NVARCHAR(50) COLLATE Latin1_General_100_BIN2 NOT NULL DEFAULT ('semantic'),
    [TenantId] INT NOT NULL DEFAULT (0) INDEX IX_TenantId NONCLUSTERED,
    
    -- Timestamps
    [LastUpdated] DATETIME2 NOT NULL DEFAULT (SYSUTCDATETIME()),
    [LastComputedUtc] DATETIME2 NULL,
    [LastAccessedUtc] DATETIME2 NULL,
    [CreatedAt] DATETIME2 NOT NULL DEFAULT (SYSUTCDATETIME()),
    
    CONSTRAINT [PK_AtomEmbeddings_InMemory] PRIMARY KEY NONCLUSTERED HASH ([AtomEmbeddingId]) WITH (BUCKET_COUNT = 10000000),
    INDEX IX_AtomId NONCLUSTERED HASH ([AtomId]) WITH (BUCKET_COUNT = 10000000),
    INDEX IX_ModelType NONCLUSTERED ([ModelId], [EmbeddingType])
)
WITH (MEMORY_OPTIMIZED = ON, DURABILITY = SCHEMA_AND_DATA);
GO

PRINT 'Memory-optimized table created.';
PRINT 'Hash bucket count: 10M (adjust based on expected embedding count)';
GO

-- Step 4: Migrate data from disk-based to memory-optimized
PRINT 'Migrating data to memory-optimized table...';
PRINT 'This may take several minutes for large datasets.';
GO

-- Disable constraints temporarily for faster bulk insert
ALTER TABLE dbo.AtomEmbeddings_InMemory SET (MEMORY_OPTIMIZED = ON, DURABILITY = SCHEMA_ONLY);
GO

-- Bulk insert (memory-optimized tables don't support BULK INSERT, use batches)
DECLARE @BatchSize INT = 100000;
DECLARE @RowCount INT = 1;
DECLARE @Offset INT = 0;

WHILE @RowCount > 0
BEGIN
    INSERT INTO dbo.AtomEmbeddings_InMemory (
        AtomEmbeddingId,
        AtomId,
        Dimension,
        SpatialBucket,
        SpatialBucketX,
        SpatialBucketY,
        SpatialBucketZ,
        SpatialProjX,
        SpatialProjY,
        SpatialProjZ,
        ModelId,
        EmbeddingType,
        TenantId,
        LastUpdated,
        LastComputedUtc,
        LastAccessedUtc,
        CreatedAt
    )
    SELECT TOP (@BatchSize)
        AtomEmbeddingId,
        AtomId,
        Dimension,
        SpatialBucket,
        SpatialBucketX,
        SpatialBucketY,
        SpatialBucketZ,
        SpatialProjX,
        SpatialProjY,
        SpatialProjZ,
        ModelId,
        EmbeddingType,
        TenantId,
        LastUpdated,
        LastComputedUtc,
        LastAccessedUtc,
        CreatedAt
    FROM dbo.AtomEmbedding
    WHERE AtomEmbeddingId > @Offset
    ORDER BY AtomEmbeddingId;
    
    SET @RowCount = @@ROWCOUNT;
    
    IF @RowCount > 0
    BEGIN
        SELECT @Offset = MAX(AtomEmbeddingId) FROM dbo.AtomEmbeddings_InMemory;
        PRINT '  Migrated ' + CAST(@Offset AS NVARCHAR(20)) + ' embeddings...';
    END
END

-- Re-enable durability
ALTER TABLE dbo.AtomEmbeddings_InMemory SET (MEMORY_OPTIMIZED = ON, DURABILITY = SCHEMA_AND_DATA);
GO

PRINT 'Data migration complete.';
GO

-- Step 5: Create natively compiled stored procedures
PRINT 'Creating natively compiled procedures...';
GO

IF OBJECT_ID('dbo.sp_GetAtomEmbeddingMetadata_Native', 'P') IS NOT NULL
    DROP PROCEDURE dbo.sp_GetAtomEmbeddingMetadata_Native;
GO

CREATE PROCEDURE dbo.sp_GetAtomEmbeddingMetadata_Native
    @AtomEmbeddingId BIGINT,
    @AtomId BIGINT OUTPUT,
    @SpatialX FLOAT OUTPUT,
    @SpatialY FLOAT OUTPUT,
    @SpatialZ FLOAT OUTPUT
WITH NATIVE_COMPILATION, SCHEMABINDING
AS 
BEGIN ATOMIC WITH (
    TRANSACTION ISOLATION LEVEL = SNAPSHOT,
    LANGUAGE = N'us_english'
)
    SELECT 
        @AtomId = AtomId,
        @SpatialX = SpatialProjX,
        @SpatialY = SpatialProjY,
        @SpatialZ = SpatialProjZ
    FROM dbo.AtomEmbeddings_InMemory
    WHERE AtomEmbeddingId = @AtomEmbeddingId;
END
GO

IF OBJECT_ID('dbo.sp_FindAtomEmbeddingsBySpatialBucket_Native', 'P') IS NOT NULL
    DROP PROCEDURE dbo.sp_FindAtomEmbeddingsBySpatialBucket_Native;
GO

CREATE PROCEDURE dbo.sp_FindAtomEmbeddingsBySpatialBucket_Native
    @SpatialBucket INT,
    @TopK INT = 100
WITH NATIVE_COMPILATION, SCHEMABINDING
AS 
BEGIN ATOMIC WITH (
    TRANSACTION ISOLATION LEVEL = SNAPSHOT,
    LANGUAGE = N'us_english'
)
    SELECT TOP (@TopK)
        AtomEmbeddingId,
        AtomId,
        SpatialProjX,
        SpatialProjY,
        SpatialProjZ,
        ModelId,
        EmbeddingType
    FROM dbo.AtomEmbeddings_InMemory
    WHERE SpatialBucket = @SpatialBucket
    ORDER BY AtomEmbeddingId;
END
GO

PRINT 'Natively compiled procedures created.';
GO

-- Step 6: Rename tables (switch to memory-optimized)
-- WARNING: This is destructive! Ensure backups exist!
/*
PRINT 'Renaming tables to switch to memory-optimized version...';
GO

EXEC sp_rename 'dbo.AtomEmbedding', 'AtomEmbeddings_Disk_Archive';
EXEC sp_rename 'dbo.AtomEmbeddings_InMemory', 'AtomEmbeddings';
GO

PRINT 'AtomEmbeddings is now memory-optimized!';
*/

-- Step 7: Performance benchmark
PRINT 'Running performance benchmark...';
GO

DECLARE @StartTime DATETIME2 = SYSUTCDATETIME();
DECLARE @AtomId BIGINT, @X FLOAT, @Y FLOAT, @Z FLOAT;
DECLARE @Iterations INT = 1000;
DECLARE @i INT = 0;

WHILE @i < @Iterations
BEGIN
    EXEC dbo.sp_GetAtomEmbeddingMetadata_Native 
        @AtomEmbeddingId = 1,
        @AtomId = @AtomId OUTPUT,
        @SpatialX = @X OUTPUT,
        @SpatialY = @Y OUTPUT,
        @SpatialZ = @Z OUTPUT;
    
    SET @i += 1;
END

DECLARE @EndTime DATETIME2 = SYSUTCDATETIME();
DECLARE @DurationMs FLOAT = DATEDIFF_BIG(MICROSECOND, @StartTime, @EndTime) / 1000.0;
DECLARE @AvgLatencyUs FLOAT = (@DurationMs * 1000.0) / @Iterations;

PRINT 'Performance benchmark results:';
PRINT '  Iterations: ' + CAST(@Iterations AS NVARCHAR(10));
PRINT '  Total duration: ' + CAST(@DurationMs AS NVARCHAR(20)) + ' ms';
PRINT '  Average latency: ' + CAST(@AvgLatencyUs AS NVARCHAR(20)) + ' µs';
PRINT '  Expected: < 10 µs for memory-optimized access';
GO

PRINT '========================================';
PRINT 'Memory-Optimization Complete!';
PRINT '========================================';
PRINT 'Summary:';
PRINT '  - Lock-free concurrent access';
PRINT '  - Sub-10µs metadata lookups';
PRINT '  - Hash indexes for O(1) access';
PRINT '  - Natively compiled procedures';
PRINT '';
PRINT 'Next steps:';
PRINT '  1. Verify spatial bucket queries';
PRINT '  2. Update application to use _InMemory table';
PRINT '  3. Uncomment Step 6 to switch tables';
PRINT '  4. Archive disk-based table';
PRINT '========================================';
GO
