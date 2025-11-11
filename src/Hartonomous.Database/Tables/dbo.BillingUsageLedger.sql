CREATE TABLE [dbo].[BillingUsageLedger] (
    [LedgerId]     BIGINT          NOT NULL IDENTITY,
    [TenantId]     NVARCHAR (128)  NOT NULL,
    [PrincipalId]  NVARCHAR (256)  NOT NULL,
    [Operation]    NVARCHAR (128)  NOT NULL,
    [MessageType]  NVARCHAR (128)  NULL,
    [Handler]      NVARCHAR (256)  NULL,
    [Units]        DECIMAL (18, 6) NOT NULL,
    [BaseRate]     DECIMAL (18, 6) NOT NULL,
    [Multiplier]   DECIMAL (18, 6) NOT NULL DEFAULT 1.0,
    [TotalCost]    DECIMAL (18, 6) NOT NULL,
    [MetadataJson] NVARCHAR (MAX)  NULL,
    [TimestampUtc] DATETIME2 (7)   NOT NULL DEFAULT (SYSUTCDATETIME()),
    CONSTRAINT [PK_BillingUsageLedger] PRIMARY KEY CLUSTERED ([LedgerId] ASC)
);
