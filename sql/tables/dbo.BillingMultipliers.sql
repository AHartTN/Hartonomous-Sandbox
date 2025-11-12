-- =============================================
-- Table: dbo.BillingMultipliers
-- =============================================
-- Defines billing rate multipliers for dynamic pricing.
-- This table was previously managed by EF Core.
-- =============================================

IF OBJECT_ID('dbo.BillingMultipliers', 'U') IS NOT NULL
    DROP TABLE dbo.BillingMultipliers;
GO

CREATE TABLE dbo.BillingMultipliers
(
    MultiplierId    UNIQUEIDENTIFIER NOT NULL PRIMARY KEY DEFAULT NEWSEQUENTIALID(),
    RatePlanId      UNIQUEIDENTIFIER NOT NULL,
    Dimension       NVARCHAR(32)     NOT NULL,
    [Key]           NVARCHAR(128)    NOT NULL,
    Multiplier      DECIMAL(18,6)    NOT NULL,
    IsActive        BIT              NOT NULL DEFAULT 1,
    CreatedUtc      DATETIME2        NOT NULL DEFAULT SYSUTCDATETIME(),
    UpdatedUtc      DATETIME2        NOT NULL DEFAULT SYSUTCDATETIME(),

    CONSTRAINT FK_BillingMultipliers_RatePlan FOREIGN KEY (RatePlanId) REFERENCES dbo.BillingRatePlans(RatePlanId) ON DELETE CASCADE
);
GO

CREATE UNIQUE INDEX UX_BillingMultipliers_Active ON dbo.BillingMultipliers(RatePlanId, Dimension, [Key]) WHERE IsActive = 1;
GO

PRINT 'Created table dbo.BillingMultipliers';
GO
