-- =============================================
-- Table: dbo.BillingRatePlans
-- Description: Billing rate plans defining pricing structure for tenant services.
--              Includes base fees, DCU pricing, storage quotas, and plan features.
-- =============================================
CREATE TABLE [dbo].[BillingRatePlans]
(
    [RatePlanId]                 UNIQUEIDENTIFIER NOT NULL DEFAULT (NEWSEQUENTIALID()),
    [TenantId]                   NVARCHAR(64)     NULL,
    [PlanCode]                   NVARCHAR(64)     NOT NULL DEFAULT '',
    [Name]                       NVARCHAR(128)    NOT NULL DEFAULT '',
    [Description]                NVARCHAR(256)    NULL,
    [DefaultRate]                DECIMAL(18,6)    NOT NULL DEFAULT (0.01),
    [MonthlyFee]                 DECIMAL(18,2)    NOT NULL DEFAULT (0),
    [UnitPricePerDcu]            DECIMAL(18,6)    NOT NULL DEFAULT (0.00008),
    [IncludedPublicStorageGb]    DECIMAL(18,2)    NOT NULL DEFAULT (0),
    [IncludedPrivateStorageGb]   DECIMAL(18,2)    NOT NULL DEFAULT (0),
    [IncludedSeatCount]          INT              NOT NULL DEFAULT (1),
    [AllowsPrivateData]          BIT              NOT NULL DEFAULT (0),
    [CanQueryPublicCorpus]       BIT              NOT NULL DEFAULT (0),
    [IsActive]                   BIT              NOT NULL DEFAULT (1),
    [CreatedUtc]                 DATETIME2(7)     NOT NULL DEFAULT (SYSUTCDATETIME()),
    [UpdatedUtc]                 DATETIME2(7)     NOT NULL DEFAULT (SYSUTCDATETIME()),

    CONSTRAINT [PK_BillingRatePlans] PRIMARY KEY CLUSTERED ([RatePlanId] ASC)
);
GO

CREATE NONCLUSTERED INDEX [IX_BillingRatePlans_Tenant_IsActive]
    ON [dbo].[BillingRatePlans]([TenantId] ASC, [IsActive] ASC)
    INCLUDE ([UpdatedUtc]);
GO

CREATE UNIQUE NONCLUSTERED INDEX [UX_BillingRatePlans_Tenant_PlanCode]
    ON [dbo].[BillingRatePlans]([TenantId] ASC, [PlanCode] ASC)
    WHERE ([PlanCode] <> '');
GO