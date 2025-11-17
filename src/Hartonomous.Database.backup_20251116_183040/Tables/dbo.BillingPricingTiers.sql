CREATE TABLE [dbo].[BillingPricingTiers] (
    [TierId]        INT             NOT NULL IDENTITY,
    [UsageType]     NVARCHAR (50)   NOT NULL,
    [UnitType]      NVARCHAR (50)   NOT NULL,
    [UnitPrice]     DECIMAL (18, 8) NOT NULL,
    [EffectiveFrom] DATETIME2 (7)   NOT NULL,
    [EffectiveTo]   DATETIME2 (7)   NULL,
    [Description]   NVARCHAR (500)  NULL,
    [MetadataJson]  NVARCHAR (MAX)  NULL,
    [CreatedUtc]    DATETIME2 (7)   NOT NULL DEFAULT (SYSUTCDATETIME()),
    CONSTRAINT [PK_BillingPricingTiers] PRIMARY KEY CLUSTERED ([TierId] ASC),
    INDEX [IX_BillingPricingTiers_UsageType] ([UsageType], [UnitType], [EffectiveFrom] DESC)
);
