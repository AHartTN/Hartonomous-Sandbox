/*
================================================================================
In-Memory OLTP (Hekaton) Table Setup - Post-Deployment
================================================================================
Purpose: Create memory-optimized tables for high-throughput, latency-sensitive operations

WHY POST-DEPLOYMENT:
- In-Memory tables can't be included in DACPAC (requires filegroup to exist first)
- Natively-compiled procedures with SCHEMABINDING require tables to exist before compilation
- Memory-optimized tables have deployment restrictions (can't use ALTER TABLE)

CANDIDATES FOR HEKATON (High-frequency insert/update with low latency requirements):
1. BillingUsageLedger_InMemory - Billing operations (ALREADY IMPLEMENTED)
2. InferenceCache - Inference result caching (NEW)
3. CachedActivations - Layer activation caching (NEW)
4. SessionPaths - OODA loop session tracking (NEW)
5. AtomEmbeddings (hot subset) - Frequently accessed embeddings (FUTURE)

PERFORMANCE TARGETS:
- Billing: 500x speedup (< 1ms latency for high-volume inserts)
- Inference Cache: 100x speedup (sub-millisecond cache lookups)
- Cached Activations: 50x speedup (eliminate latch contention)
- Session Paths: 10x speedup (faster OODA loop iterations)

HASH INDEX SIZING:
- BUCKET_COUNT should be 1.5-2x expected row count
- Too small = hash collisions, too large = memory waste
- Use range indexes for time-based queries

DURABILITY OPTIONS:
- SCHEMA_AND_DATA: Full durability (default) - transaction log writes
- SCHEMA_ONLY: No durability - rebuilt on restart (for temp caching only)

================================================================================
*/

PRINT '';
PRINT '═══════════════════════════════════════════════════════════════';
PRINT 'HEKATON SETUP: Creating In-Memory OLTP Tables';
PRINT '═══════════════════════════════════════════════════════════════';
PRINT '';

-- =============================================
-- TABLE 1: BillingUsageLedger_InMemory
-- High-volume billing insert workload
-- Target: 500x speedup over disk-based ledger
-- =============================================
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'BillingUsageLedger_InMemory' AND is_memory_optimized = 1)
BEGIN
    PRINT '[1/4] Creating BillingUsageLedger_InMemory...';
    
    CREATE TABLE [dbo].[BillingUsageLedger_InMemory]
    (
        [LedgerId]      BIGINT           IDENTITY (1, 1) NOT NULL,
        [TenantId]      NVARCHAR (128)   COLLATE Latin1_General_100_BIN2 NOT NULL,
        [PrincipalId]   NVARCHAR (256)   COLLATE Latin1_General_100_BIN2 NOT NULL,
        [Operation]     NVARCHAR (128)   COLLATE Latin1_General_100_BIN2 NOT NULL,
        [MessageType]   NVARCHAR (128)   COLLATE Latin1_General_100_BIN2 NULL,
        [Handler]       NVARCHAR (256)   COLLATE Latin1_General_100_BIN2 NULL,
        [Units]         DECIMAL (18, 6)  NOT NULL,
        [BaseRate]      DECIMAL (18, 6)  NOT NULL,
        [Multiplier]    DECIMAL (18, 6)  NOT NULL DEFAULT (1.0),
        [TotalCost]     DECIMAL (18, 6)  NOT NULL,
        [MetadataJson]  NVARCHAR (MAX)   COLLATE Latin1_General_100_BIN2 NULL,
        [TimestampUtc]  DATETIME2 (7)    NOT NULL DEFAULT (SYSUTCDATETIME()),
        
        CONSTRAINT [PK_BillingUsageLedger_InMemory] PRIMARY KEY NONCLUSTERED ([LedgerId]),
        
        -- Hash index: 10K tenants × 100 records/day × 7 days = ~7M expected rows
        -- BUCKET_COUNT = 10M (1.4x expected)
        INDEX [IX_TenantId_Hash] HASH ([TenantId]) WITH (BUCKET_COUNT = 10000000),
        
        -- Range index for time-based queries (archival, reporting)
        INDEX [IX_Timestamp_Range] NONCLUSTERED ([TimestampUtc] DESC)
    )
    WITH (MEMORY_OPTIMIZED = ON, DURABILITY = SCHEMA_AND_DATA);
    
    PRINT '  ✓ BillingUsageLedger_InMemory created (500x target speedup)';
END
ELSE
    PRINT '[1/4] BillingUsageLedger_InMemory already exists';

-- =============================================
-- TABLE 2: InferenceCache_InMemory
-- Hot inference result cache
-- Target: Sub-millisecond cache hits
-- =============================================
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'InferenceCache_InMemory' AND is_memory_optimized = 1)
BEGIN
    PRINT '[2/4] Creating InferenceCache_InMemory...';
    
    CREATE TABLE [dbo].[InferenceCache_InMemory]
    (
        [CacheId]            BIGINT          IDENTITY (1, 1) NOT NULL,
        [CacheKey]           NVARCHAR (64)   COLLATE Latin1_General_100_BIN2 NOT NULL,
        [ModelId]            INT             NOT NULL,
        [InferenceType]      NVARCHAR (100)  COLLATE Latin1_General_100_BIN2 NOT NULL,
        [InputHash]          BINARY (32)     NOT NULL, -- SHA256 hash
        [OutputData]         VARBINARY (MAX) NOT NULL,
        [IntermediateStates] VARBINARY (MAX) NULL,
        [CreatedUtc]         DATETIME2 (7)   NOT NULL DEFAULT (SYSUTCDATETIME()),
        [LastAccessedUtc]    DATETIME2 (7)   NOT NULL DEFAULT (SYSUTCDATETIME()),
        [AccessCount]        BIGINT          NOT NULL DEFAULT (0),
        [SizeBytes]          BIGINT          NULL,
        [ComputeTimeMs]      FLOAT           NULL,
        
        CONSTRAINT [PK_InferenceCache_InMemory] PRIMARY KEY NONCLUSTERED ([CacheId]),
        
        -- Hash index for cache key lookups (most common operation)
        -- Expect ~1M hot cache entries
        INDEX [IX_CacheKey_Hash] HASH ([CacheKey]) WITH (BUCKET_COUNT = 2000000),
        
        -- Composite hash for model+input lookups
        INDEX [IX_ModelInput_Hash] HASH ([ModelId], [InputHash]) WITH (BUCKET_COUNT = 2000000),
        
        -- Range index for LRU eviction (least recently used)
        INDEX [IX_LastAccessed_Range] NONCLUSTERED ([LastAccessedUtc] ASC)
    )
    WITH (MEMORY_OPTIMIZED = ON, DURABILITY = SCHEMA_AND_DATA);
    
    PRINT '  ✓ InferenceCache_InMemory created (100x target speedup)';
    PRINT '  NOTE: Migrate hot entries from disk-based InferenceCache via background job';
END
ELSE
    PRINT '[2/4] InferenceCache_InMemory already exists';

-- =============================================
-- TABLE 3: CachedActivations_InMemory
-- Layer-level activation caching
-- Target: Eliminate latch contention on HitCount updates
-- =============================================
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'CachedActivations_InMemory' AND is_memory_optimized = 1)
BEGIN
    PRINT '[3/4] Creating CachedActivations_InMemory...';
    
    CREATE TABLE [dbo].[CachedActivations_InMemory]
    (
        [CacheId]            BIGINT         IDENTITY (1, 1) NOT NULL,
        [ModelId]            INT            NOT NULL,
        [LayerId]            BIGINT         NOT NULL,
        [InputHash]          BINARY (32)    NOT NULL,
        [ActivationOutput]   VARBINARY(MAX) NULL,
        [OutputShape]        NVARCHAR (100) COLLATE Latin1_General_100_BIN2 NULL,
        [HitCount]           BIGINT         NOT NULL DEFAULT (0),
        [CreatedDate]        DATETIME2 (7)  NOT NULL DEFAULT (SYSUTCDATETIME()),
        [LastAccessed]       DATETIME2 (7)  NOT NULL DEFAULT (SYSUTCDATETIME()),
        [ComputeTimeSavedMs] BIGINT         NOT NULL DEFAULT (0),
        
        CONSTRAINT [PK_CachedActivations_InMemory] PRIMARY KEY NONCLUSTERED ([CacheId]),
        
        -- Composite hash for layer+input lookups
        -- Expect ~500K cached activations per model
        INDEX [IX_LayerInput_Hash] HASH ([LayerId], [InputHash]) WITH (BUCKET_COUNT = 1000000),
        
        -- Hash index for model-level queries
        INDEX [IX_ModelId_Hash] HASH ([ModelId]) WITH (BUCKET_COUNT = 1000),
        
        -- Range index for eviction based on access time
        INDEX [IX_LastAccessed_Range] NONCLUSTERED ([LastAccessed] ASC)
    )
    WITH (MEMORY_OPTIMIZED = ON, DURABILITY = SCHEMA_AND_DATA);
    
    PRINT '  ✓ CachedActivations_InMemory created (50x target speedup)';
    PRINT '  NOTE: Migrate hot entries from CachedActivations via background job';
END
ELSE
    PRINT '[3/4] CachedActivations_InMemory already exists';

-- =============================================
-- TABLE 4: SessionPaths_InMemory
-- OODA loop session tracking
-- Target: Faster autonomous improvement cycles
-- =============================================
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'SessionPaths_InMemory' AND is_memory_optimized = 1)
BEGIN
    PRINT '[4/4] Creating SessionPaths_InMemory...';
    
    CREATE TABLE [dbo].[SessionPaths_InMemory]
    (
        [SessionPathId]     BIGINT           IDENTITY (1, 1) NOT NULL,
        [SessionId]         UNIQUEIDENTIFIER COLLATE Latin1_General_100_BIN2 NOT NULL,
        [PathNumber]        INT              NOT NULL,
        [HypothesisId]      UNIQUEIDENTIFIER COLLATE Latin1_General_100_BIN2 NULL,
        [ResponseText]      NVARCHAR (MAX)   COLLATE Latin1_General_100_BIN2 NULL,
        [ResponseVector]    VARBINARY (MAX)  NULL, -- Will migrate to VECTOR(1998) when hot
        [Score]             FLOAT            NULL,
        [IsSelected]        BIT              NOT NULL DEFAULT (0),
        [CreatedUtc]        DATETIME2 (7)    NOT NULL DEFAULT (SYSUTCDATETIME()),
        
        CONSTRAINT [PK_SessionPaths_InMemory] PRIMARY KEY NONCLUSTERED ([SessionPathId]),
        
        -- Hash index for session lookups (OODA loop operations)
        -- Expect ~10K active sessions × 10 paths = 100K rows
        INDEX [IX_SessionId_Hash] HASH ([SessionId]) WITH (BUCKET_COUNT = 200000),
        
        -- Composite hash for session+path lookups
        INDEX [IX_SessionPath_Hash] HASH ([SessionId], [PathNumber]) WITH (BUCKET_COUNT = 200000)
    )
    WITH (MEMORY_OPTIMIZED = ON, DURABILITY = SCHEMA_AND_DATA);
    
    PRINT '  ✓ SessionPaths_InMemory created (10x target speedup)';
    PRINT '  NOTE: OODA loop operations will automatically use in-memory version';
END
ELSE
    PRINT '[4/4] SessionPaths_InMemory already exists';

PRINT '';
PRINT '═══════════════════════════════════════════════════════════════';
PRINT 'HEKATON SETUP COMPLETE';
PRINT '';
PRINT 'Memory-Optimized Tables Created:';
PRINT '  ✓ BillingUsageLedger_InMemory  (500x speedup target)';
PRINT '  ✓ InferenceCache_InMemory      (100x speedup target)';
PRINT '  ✓ CachedActivations_InMemory   (50x speedup target)';
PRINT '  ✓ SessionPaths_InMemory        (10x speedup target)';
PRINT '';
PRINT 'Next Steps:';
PRINT '  1. Create natively-compiled stored procedures';
PRINT '  2. Migrate hot data from disk-based tables';
PRINT '  3. Monitor memory consumption (sys.dm_db_xtp_table_memory_stats)';
PRINT '  4. Set up background eviction jobs for LRU cache management';
PRINT '═══════════════════════════════════════════════════════════════';
PRINT '';
GO
