CREATE TABLE dbo.BillingUsageLedger (
    LedgerId BIGINT IDENTITY(1,1) NOT NULL,
    TenantId NVARCHAR(128) NOT NULL,
    PrincipalId NVARCHAR(256) NOT NULL,
    Operation NVARCHAR(128) NOT NULL,
    MessageType NVARCHAR(128) NULL,
    Handler NVARCHAR(256) NULL,
    Units DECIMAL(18,6) NOT NULL,
    BaseRate DECIMAL(18,6) NOT NULL,
    Multiplier DECIMAL(18,6) NOT NULL DEFAULT 1.0,
    TotalCost DECIMAL(18,6) NOT NULL,
    MetadataJson JSON NULL,
    TimestampUtc DATETIME2(7) NOT NULL DEFAULT SYSUTCDATETIME(),
    CONSTRAINT PK_BillingUsageLedger PRIMARY KEY CLUSTERED (LedgerId),
    INDEX IX_BillingUsageLedger_TenantId_Timestamp (TenantId, TimestampUtc DESC) INCLUDE (Operation, TotalCost),
    INDEX IX_BillingUsageLedger_Operation_Timestamp (Operation, TimestampUtc DESC) INCLUDE (TenantId, Units, TotalCost)
);