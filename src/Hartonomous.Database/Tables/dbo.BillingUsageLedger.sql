-- =============================================
-- BillingUsageLedger: High-frequency append-only ledger for billing events
-- Will be migrated to In-Memory OLTP to eliminate latch contention
-- =============================================

CREATE TABLE [dbo].[BillingUsageLedger]
(
    [LedgerId] BIGINT IDENTITY(1,1) NOT NULL,
    [TenantId] NVARCHAR(128) NOT NULL,
    [PrincipalId] NVARCHAR(256) NOT NULL,
    [Operation] NVARCHAR(128) NOT NULL,
    [MessageType] NVARCHAR(128) NULL,
    [Handler] NVARCHAR(256) NULL,
    [Units] DECIMAL(18,6) NOT NULL,
    [BaseRate] DECIMAL(18,6) NOT NULL,
    [Multiplier] DECIMAL(18,6) NOT NULL DEFAULT (1.0),
    [TotalCost] DECIMAL(18,6) NOT NULL,
    [MetadataJson] NVARCHAR(MAX) NULL,
    [TimestampUtc] DATETIME2(7) NOT NULL DEFAULT (SYSUTCDATETIME()),
    
    CONSTRAINT [PK_BillingUsageLedger] PRIMARY KEY CLUSTERED ([LedgerId])
);
GO

CREATE NONCLUSTERED INDEX [IX_BillingUsageLedger_TenantId_Timestamp]
    ON [dbo].[BillingUsageLedger]([TenantId], [TimestampUtc] DESC)
    INCLUDE ([Operation], [TotalCost]);
GO

CREATE NONCLUSTERED INDEX [IX_BillingUsageLedger_Operation_Timestamp]
    ON [dbo].[BillingUsageLedger]([Operation], [TimestampUtc] DESC)
    INCLUDE ([TenantId], [Units], [TotalCost]);
GO