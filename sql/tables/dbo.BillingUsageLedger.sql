-- =============================================
-- Table: dbo.BillingUsageLedger
-- =============================================
-- High-frequency append-only ledger for billing events.
-- This table was previously managed by EF Core.
-- =============================================

IF OBJECT_ID('dbo.BillingUsageLedger', 'U') IS NOT NULL
    DROP TABLE dbo.BillingUsageLedger;
GO

CREATE TABLE dbo.BillingUsageLedger
(
    LedgerId        BIGINT          IDENTITY(1,1) NOT NULL PRIMARY KEY,
    TenantId        NVARCHAR(128)   NOT NULL,
    PrincipalId     NVARCHAR(256)   NOT NULL,
    Operation       NVARCHAR(128)   NOT NULL,
    MessageType     NVARCHAR(128)   NULL,
    Handler         NVARCHAR(256)   NULL,
    Units           DECIMAL(18,6)   NOT NULL,
    BaseRate        DECIMAL(18,6)   NOT NULL,
    Multiplier      DECIMAL(18,6)   NOT NULL DEFAULT 1.0,
    TotalCost       DECIMAL(18,6)   NOT NULL,
    MetadataJson    NVARCHAR(MAX)   NULL,
    TimestampUtc    DATETIME2(7)    NOT NULL DEFAULT SYSUTCDATETIME(),

    CONSTRAINT CK_BillingUsageLedger_MetadataJson_IsJson CHECK (MetadataJson IS NULL OR ISJSON(MetadataJson) = 1)
);
GO

CREATE INDEX IX_BillingUsageLedger_TenantId_Timestamp ON dbo.BillingUsageLedger(TenantId, TimestampUtc DESC) INCLUDE (Operation, TotalCost);
GO

CREATE INDEX IX_BillingUsageLedger_Operation_Timestamp ON dbo.BillingUsageLedger(Operation, TimestampUtc DESC) INCLUDE (TenantId, Units, TotalCost);
GO

PRINT 'Created table dbo.BillingUsageLedger';
GO