CREATE TABLE [billing].[TenantQuotas] (
    [TenantQuotaId] INT IDENTITY(1,1) NOT NULL,
    [TenantId] INT NOT NULL,
    [UsageType] NVARCHAR(50) NOT NULL,
    [QuotaLimit] BIGINT NOT NULL,
    [IsActive] BIT NOT NULL CONSTRAINT [DF_TenantQuotas_IsActive] DEFAULT (1),
    [EffectiveFrom] DATETIME2(7) NOT NULL CONSTRAINT [DF_TenantQuotas_EffectiveFrom] DEFAULT (SYSUTCDATETIME()),
    [EffectiveTo] DATETIME2(7) NULL,
    [CreatedUtc] DATETIME2(7) NOT NULL CONSTRAINT [DF_TenantQuotas_CreatedUtc] DEFAULT (SYSUTCDATETIME()),
    CONSTRAINT [PK_TenantQuotas] PRIMARY KEY CLUSTERED ([TenantQuotaId])
);
GO

CREATE INDEX [IX_TenantQuotas_TenantId_UsageType] ON [billing].[TenantQuotas] ([TenantId], [UsageType]) WHERE ([IsActive] = 1);
GO
