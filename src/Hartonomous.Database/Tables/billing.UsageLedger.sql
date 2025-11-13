CREATE TABLE [billing].[UsageLedger] (
    [UsageLedgerId] BIGINT IDENTITY(1,1) NOT NULL,
    [TenantId] INT NOT NULL,
    [UsageType] NVARCHAR(50) NOT NULL,
    [Quantity] BIGINT NOT NULL,
    [UnitType] NVARCHAR(50) NOT NULL,
    [CostPerUnit] DECIMAL(18, 8) NOT NULL,
    [TotalCost] DECIMAL(18, 8) NOT NULL,
    [Metadata] NVARCHAR(MAX) NULL,
    [RecordedUtc] DATETIME2(7) NOT NULL,
    CONSTRAINT [PK_UsageLedger] PRIMARY KEY CLUSTERED ([UsageLedgerId])
);
GO

CREATE INDEX [IX_UsageLedger_TenantId_RecordedUtc] ON [billing].[UsageLedger] ([TenantId], [RecordedUtc]) INCLUDE ([UsageType], [Quantity]);
CREATE INDEX [IX_UsageLedger_UsageType_RecordedUtc] ON [billing].[UsageLedger] ([UsageType], [RecordedUtc]);
GO
