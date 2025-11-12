-- =============================================
-- Table: dbo.BillingRatePlans
-- =============================================
-- Defines billing rate plans for tenants.
-- This table was previously managed by EF Core.
-- =============================================

IF OBJECT_ID('dbo.BillingRatePlans', 'U') IS NOT NULL
    DROP TABLE dbo.BillingRatePlans;
GO

CREATE TABLE dbo.BillingRatePlans
(
    RatePlanId              UNIQUEIDENTIFIER NOT NULL PRIMARY KEY DEFAULT NEWSEQUENTIALID(),
    TenantId                NVARCHAR(64)     NULL,
    PlanCode                NVARCHAR(64)     NOT NULL,
    Name                    NVARCHAR(128)    NOT NULL,
    Description             NVARCHAR(256)    NULL,
    DefaultRate             DECIMAL(18,6)    NOT NULL DEFAULT 0.01,
    MonthlyFee              DECIMAL(18,2)    NOT NULL DEFAULT 0.0,
    UnitPricePerDcu         DECIMAL(18,6)    NOT NULL DEFAULT 0.00008,
    IncludedPublicStorageGb DECIMAL(18,2)    NOT NULL DEFAULT 0.0,
    IncludedPrivateStorageGb DECIMAL(18,2)   NOT NULL DEFAULT 0.0,
    IncludedSeatCount       INT              NOT NULL DEFAULT 1,
    AllowsPrivateData       BIT              NOT NULL DEFAULT 0,
    CanQueryPublicCorpus    BIT              NOT NULL DEFAULT 0,
    IsActive                BIT              NOT NULL DEFAULT 1,
    CreatedUtc              DATETIME2        NOT NULL DEFAULT SYSUTCDATETIME(),
    UpdatedUtc              DATETIME2        NOT NULL DEFAULT SYSUTCDATETIME()
);
GO

CREATE INDEX IX_BillingRatePlans_Tenant_IsActive ON dbo.BillingRatePlans(TenantId, IsActive) INCLUDE (UpdatedUtc);
GO

CREATE UNIQUE INDEX UX_BillingRatePlans_Tenant_PlanCode ON dbo.BillingRatePlans(TenantId, PlanCode) WHERE PlanCode <> '';
GO

PRINT 'Created table dbo.BillingRatePlans';
GO
