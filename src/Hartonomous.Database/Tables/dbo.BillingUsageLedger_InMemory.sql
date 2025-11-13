-- =============================================
-- DEPRECATED: This file is no longer used
-- In-Memory tables are now created in post-deployment:
-- Scripts/Post-Deployment/Setup_InMemory_Tables.sql
-- =============================================
-- REASON FOR MOVE:
-- - DACPAC can't handle MEMORY_OPTIMIZED tables properly
-- - Natively-compiled procedures with SCHEMABINDING require table to exist first
-- - In-Memory tables must be created AFTER filegroup setup (pre-deployment)
-- - In-Memory tables can't use ALTER TABLE, so post-deployment is the right place
-- =============================================
/*
-- Original table definition (now in Setup_InMemory_Tables.sql):
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
    [Multiplier]    DECIMAL (18, 6)  NOT NULL CONSTRAINT [DF_BillingUsageLedger_InMemory_Multiplier] DEFAULT (1.0),
    [TotalCost]     DECIMAL (18, 6)  NOT NULL,
    [MetadataJson]  NVARCHAR (MAX)   COLLATE Latin1_General_100_BIN2 NULL,
    [TimestampUtc]  DATETIME2 (7)    NOT NULL CONSTRAINT [DF_BillingUsageLedger_InMemory_TimestampUtc] DEFAULT (SYSUTCDATETIME()),
    
    CONSTRAINT [PK_BillingUsageLedger_InMemory] PRIMARY KEY NONCLUSTERED ([LedgerId]),
    
    -- Hash index for tenant lookups (estimate bucket count based on expected tenant count)
    INDEX [IX_TenantId_Hash] HASH ([TenantId]) WITH (BUCKET_COUNT = 10000),
    
    -- Range index for time-based queries
    INDEX [IX_Timestamp_Range] NONCLUSTERED ([TimestampUtc] DESC)
)
WITH (MEMORY_OPTIMIZED = ON, DURABILITY = SCHEMA_AND_DATA);
*/
