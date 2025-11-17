CREATE TABLE [dbo].[BillingTenantQuota] (
    [QuotaId]       INT            NOT NULL IDENTITY,
    [TenantId]      INT            NOT NULL,
    [UsageType]     NVARCHAR (50)  NOT NULL,
    [QuotaLimit]    BIGINT         NOT NULL,
    [IsActive]      BIT            NOT NULL DEFAULT 1,
    [ResetInterval] NVARCHAR (20)  NULL, -- 'Daily', 'Weekly', 'Monthly', 'Yearly'
    [Description]   NVARCHAR (500) NULL,
    [MetadataJson]  NVARCHAR (MAX) NULL,
    [CreatedUtc]    DATETIME2 (7)  NOT NULL DEFAULT (SYSUTCDATETIME()),
    [UpdatedUtc]    DATETIME2 (7)  NOT NULL DEFAULT (SYSUTCDATETIME()),
    CONSTRAINT [PK_BillingTenantQuotas] PRIMARY KEY CLUSTERED ([QuotaId] ASC),
    INDEX [IX_BillingTenantQuotas_Tenant] ([TenantId], [UsageType], [IsActive])
);
