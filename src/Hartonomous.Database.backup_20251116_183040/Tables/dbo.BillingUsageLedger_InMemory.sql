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
    
    INDEX [IX_TenantId_Hash] HASH ([TenantId]) WITH (BUCKET_COUNT = 10000000),
    INDEX [IX_Timestamp_Range] NONCLUSTERED ([TimestampUtc] DESC)
)
WITH (MEMORY_OPTIMIZED = ON, DURABILITY = SCHEMA_AND_DATA);
