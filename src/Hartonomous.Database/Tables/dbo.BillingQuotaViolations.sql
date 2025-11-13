CREATE TABLE [dbo].[BillingQuotaViolations] (
    [ViolationId]   BIGINT         NOT NULL IDENTITY,
    [TenantId]      INT            NOT NULL,
    [UsageType]     NVARCHAR (50)  NOT NULL,
    [QuotaLimit]    BIGINT         NOT NULL,
    [CurrentUsage]  BIGINT         NOT NULL,
    [ViolatedUtc]   DATETIME2 (7)  NOT NULL DEFAULT (SYSUTCDATETIME()),
    [Resolved]      BIT            NOT NULL DEFAULT 0,
    [ResolvedUtc]   DATETIME2 (7)  NULL,
    [Notes]         NVARCHAR (MAX) NULL,
    CONSTRAINT [PK_BillingQuotaViolations] PRIMARY KEY CLUSTERED ([ViolationId] ASC),
    INDEX [IX_BillingQuotaViolations_Tenant] ([TenantId], [ViolatedUtc] DESC),
    INDEX [IX_BillingQuotaViolations_Unresolved] ([Resolved]) WHERE ([Resolved] = 0)
);
