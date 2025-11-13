-- Provenance Tracking Tables
-- Tables to support provenance tracking procedures

-- OperationProvenance: Store operation provenance streams
CREATE TABLE dbo.OperationProvenance (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    OperationId UNIQUEIDENTIFIER NOT NULL UNIQUE,
    ProvenanceStream dbo.AtomicStream NOT NULL,
    CreatedAt DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),

    INDEX IX_OperationProvenance_OperationId (OperationId),
    INDEX IX_OperationProvenance_CreatedAt (CreatedAt DESC)
);

-- ProvenanceValidationResults: Store provenance validation results
CREATE TABLE dbo.ProvenanceValidationResults (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    OperationId UNIQUEIDENTIFIER NOT NULL,
    ValidationResults NVARCHAR(MAX), -- JSON array of validation checks
    OverallStatus NVARCHAR(20) NOT NULL, -- 'PASS', 'WARN', 'FAIL'
    ValidationDurationMs INT NOT NULL,
    ValidatedAt DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),

    INDEX IX_ProvenanceValidationResults_OperationId (OperationId),
    INDEX IX_ProvenanceValidationResults_Status (OverallStatus),
    INDEX IX_ProvenanceValidationResults_ValidatedAt (ValidatedAt DESC),

    FOREIGN KEY (OperationId) REFERENCES dbo.OperationProvenance(OperationId)
);

-- ProvenanceAuditResults: Store comprehensive audit results
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
    Anomalies NVARCHAR(MAX), -- JSON array of detected anomalies
    AuditDurationMs INT NOT NULL,
    AuditedAt DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),

    INDEX IX_ProvenanceAuditResults_AuditPeriod (AuditPeriodStart, AuditPeriodEnd),
    INDEX IX_ProvenanceAuditResults_AuditedAt (AuditedAt DESC)
);