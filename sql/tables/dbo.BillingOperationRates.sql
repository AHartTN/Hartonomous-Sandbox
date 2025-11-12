-- =============================================
-- Table: dbo.BillingOperationRates
-- =============================================
-- Defines rates for specific operations within a billing plan.
-- This table was previously managed by EF Core.
-- =============================================

IF OBJECT_ID('dbo.BillingOperationRates', 'U') IS NOT NULL
    DROP TABLE dbo.BillingOperationRates;
GO

CREATE TABLE dbo.BillingOperationRates
(
    OperationRateId   UNIQUEIDENTIFIER NOT NULL PRIMARY KEY DEFAULT NEWSEQUENTIALID(),
    RatePlanId        UNIQUEIDENTIFIER NOT NULL,
    Operation         NVARCHAR(128)    NOT NULL,
    UnitOfMeasure     NVARCHAR(64)     NOT NULL,
    Category          NVARCHAR(64)     NULL,
    Description       NVARCHAR(256)    NULL,
    Rate              DECIMAL(18,6)    NOT NULL,
    IsActive          BIT              NOT NULL DEFAULT 1,
    CreatedUtc        DATETIME2        NOT NULL DEFAULT SYSUTCDATETIME(),
    UpdatedUtc        DATETIME2        NOT NULL DEFAULT SYSUTCDATETIME(),

    CONSTRAINT FK_BillingOperationRates_RatePlan FOREIGN KEY (RatePlanId) REFERENCES dbo.BillingRatePlans(RatePlanId) ON DELETE CASCADE
);
GO

CREATE UNIQUE INDEX UX_BillingOperationRates_Active ON dbo.BillingOperationRates(RatePlanId, Operation) WHERE IsActive = 1;
GO

PRINT 'Created table dbo.BillingOperationRates';
GO
