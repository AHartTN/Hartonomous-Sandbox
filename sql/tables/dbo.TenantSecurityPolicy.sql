-- =============================================
-- Table: dbo.TenantSecurityPolicy
-- =============================================
-- Security policies and access controls per tenant.
-- This table was previously managed by EF Core.
-- =============================================

IF OBJECT_ID('dbo.TenantSecurityPolicy', 'U') IS NOT NULL
    DROP TABLE dbo.TenantSecurityPolicy;
GO

CREATE TABLE dbo.TenantSecurityPolicy
(
    PolicyId        INT             IDENTITY(1,1) NOT NULL PRIMARY KEY,
    TenantId        NVARCHAR(128)   NOT NULL,
    PolicyName      NVARCHAR(100)   NOT NULL,
    PolicyType      NVARCHAR(50)    NOT NULL,
    PolicyRules     NVARCHAR(MAX)   NOT NULL,
    IsActive        BIT             NOT NULL DEFAULT 1,
    EffectiveFrom   DATETIME2       NULL,
    EffectiveTo     DATETIME2       NULL,
    CreatedUtc      DATETIME2       NOT NULL DEFAULT SYSUTCDATETIME(),
    UpdatedUtc      DATETIME2       NULL,
    CreatedBy       NVARCHAR(256)   NULL,
    UpdatedBy       NVARCHAR(256)   NULL,

    CONSTRAINT CK_TenantSecurityPolicy_PolicyRules_IsJson CHECK (PolicyRules IS NULL OR ISJSON(PolicyRules) = 1)
);
GO

CREATE INDEX IX_TenantSecurityPolicy_TenantId_PolicyType ON dbo.TenantSecurityPolicy(TenantId, PolicyType);
GO

CREATE INDEX IX_TenantSecurityPolicy_IsActive ON dbo.TenantSecurityPolicy(IsActive);
GO

CREATE INDEX IX_TenantSecurityPolicy_EffectiveDates ON dbo.TenantSecurityPolicy(EffectiveFrom, EffectiveTo);
GO

PRINT 'Created table dbo.TenantSecurityPolicy';
GO