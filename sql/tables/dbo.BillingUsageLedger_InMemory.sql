-- =============================================
-- In-Memory OLTP Version of BillingUsageLedger
-- Eliminates latch contention for high-frequency insert workloads
-- =============================================
-- Prerequisites:
-- 1. Memory-optimized filegroup must exist
-- 2. Sufficient memory allocation
-- =============================================

-- Step 1: Create memory-optimized filegroup (if not exists)
USE Hartonomous;
GO

IF NOT EXISTS (SELECT 1 FROM sys.filegroups WHERE name = N'HartonomousMemoryOptimized' AND type = 'FX')
BEGIN
    ALTER DATABASE Hartonomous
    ADD FILEGROUP HartonomousMemoryOptimized CONTAINS MEMORY_OPTIMIZED_DATA;
    PRINT 'Memory-optimized filegroup created.';
END
GO

IF NOT EXISTS (
    SELECT 1 
    FROM sys.database_files df
    INNER JOIN sys.filegroups fg ON df.data_space_id = fg.data_space_id
    WHERE fg.name = N'HartonomousMemoryOptimized' AND fg.type = 'FX'
)
BEGIN
    ALTER DATABASE Hartonomous
    ADD FILE (
        NAME = N'HartonomousMemoryOptimized_File',
        FILENAME = N'D:\Hartonomous\HartonomousMemoryOptimized'
    ) TO FILEGROUP HartonomousMemoryOptimized;
    
    PRINT 'Memory-optimized file added.';
END
GO

-- Step 2: Create memory-optimized table
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'BillingUsageLedger_InMemory' AND schema_id = SCHEMA_ID('dbo'))
BEGIN
    CREATE TABLE dbo.BillingUsageLedger_InMemory
    (
        LedgerId BIGINT IDENTITY(1,1) NOT NULL,
        TenantId NVARCHAR(128) COLLATE Latin1_General_100_BIN2 NOT NULL,
        PrincipalId NVARCHAR(256) COLLATE Latin1_General_100_BIN2 NOT NULL,
        Operation NVARCHAR(128) COLLATE Latin1_General_100_BIN2 NOT NULL,
        MessageType NVARCHAR(128) COLLATE Latin1_General_100_BIN2 NULL,
        Handler NVARCHAR(256) COLLATE Latin1_General_100_BIN2 NULL,
        Units DECIMAL(18,6) NOT NULL,
        BaseRate DECIMAL(18,6) NOT NULL,
        Multiplier DECIMAL(18,6) NOT NULL DEFAULT 1.0,
        TotalCost DECIMAL(18,6) NOT NULL,
        MetadataJson NVARCHAR(MAX) COLLATE Latin1_General_100_BIN2 NULL,
        TimestampUtc DATETIME2(7) NOT NULL DEFAULT SYSUTCDATETIME(),
        
        CONSTRAINT PK_BillingUsageLedger_InMemory PRIMARY KEY NONCLUSTERED (LedgerId),
        
        -- Hash index for tenant lookups (estimate bucket count based on expected tenant count)
        INDEX IX_TenantId_Hash HASH (TenantId) WITH (BUCKET_COUNT = 10000),
        
        -- Range index for time-based queries
        INDEX IX_Timestamp_Range NONCLUSTERED (TimestampUtc DESC)
    )
    WITH (
        MEMORY_OPTIMIZED = ON,
        DURABILITY = SCHEMA_AND_DATA
    );

    PRINT 'Created dbo.BillingUsageLedger_InMemory table';
END
GO
