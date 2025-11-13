CREATE TABLE [billing].[PricingTiers] (
    [PricingTierId] INT IDENTITY(1,1) NOT NULL,
    [UsageType] NVARCHAR(50) NOT NULL,
    [UnitType] NVARCHAR(50) NOT NULL,
    [UnitPrice] DECIMAL(18, 8) NOT NULL,
    [EffectiveFrom] DATETIME2(7) NOT NULL,
    [EffectiveTo] DATETIME2(7) NULL,
    [Description] NVARCHAR(500) NULL,
    [CreatedUtc] DATETIME2(7) NOT NULL CONSTRAINT [DF_PricingTiers_CreatedUtc] DEFAULT (SYSUTCDATETIME()),
    CONSTRAINT [PK_PricingTiers] PRIMARY KEY CLUSTERED ([PricingTierId])
);
GO

CREATE INDEX [IX_PricingTiers_UsageType_EffectiveFrom] ON [billing].[PricingTiers] ([UsageType], [EffectiveFrom]) INCLUDE ([UnitPrice], [EffectiveTo]);
GO
