CREATE TABLE dbo.ProvenanceAuditResults (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    AuditPeriodStart DATETIME2 NOT NULL,
    AuditPeriodEnd DATETIME2 NOT NULL,
    Scope NVARCHAR(100),
    TotalOperations INT NOT NULL,
    ValidOperations INT NOT NULL,
    WarningOperations INT NOT NULL,
    FailedOperations INT NOT NULL,
    AverageValidationScore FLOAT,
    AverageSegmentCount FLOAT,
    Anomalies JSON, -- JSON array of detected anomalies
    AuditDurationMs INT NOT NULL,
    AuditedAt DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),

    INDEX IX_ProvenanceAuditResults_AuditPeriod (AuditPeriodStart, AuditPeriodEnd),
    INDEX IX_ProvenanceAuditResults_AuditedAt (AuditedAt DESC)
);