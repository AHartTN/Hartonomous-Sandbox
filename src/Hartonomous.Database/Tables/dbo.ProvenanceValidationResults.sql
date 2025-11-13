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

    CONSTRAINT FK_ProvenanceValidationResults_OperationProvenance FOREIGN KEY (OperationId) 
        REFERENCES dbo.OperationProvenance(OperationId)
);