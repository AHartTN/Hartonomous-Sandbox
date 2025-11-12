CREATE TABLE dbo.OperationProvenance (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    OperationId UNIQUEIDENTIFIER NOT NULL UNIQUE,
    ProvenanceStream provenance.AtomicStream NOT NULL,
    CreatedAt DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),

    INDEX IX_OperationProvenance_OperationId (OperationId),
    INDEX IX_OperationProvenance_CreatedAt (CreatedAt DESC)
);