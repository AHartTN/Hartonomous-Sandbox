CREATE TABLE [dbo].[ProvenanceAuditResults]
(
    [Id] INT IDENTITY(1,1) NOT NULL,
    [AuditPeriodStart] DATETIME2 NOT NULL,
    [AuditPeriodEnd] DATETIME2 NOT NULL,
    [Scope] NVARCHAR(100) NULL,
    [TotalOperations] INT NOT NULL,
    [ValidOperations] INT NOT NULL,
    [WarningOperations] INT NOT NULL,
    [FailedOperations] INT NOT NULL,
    [AverageValidationScore] FLOAT NULL,
    [AverageSegmentCount] FLOAT NULL,
    [Anomalies] NVARCHAR(MAX) NULL,
    [AuditDurationMs] INT NOT NULL,
    [AuditedAt] DATETIME2 NOT NULL DEFAULT (SYSUTCDATETIME()),
    
    CONSTRAINT [PK_ProvenanceAuditResults] PRIMARY KEY CLUSTERED ([Id])
);
GO

CREATE NONCLUSTERED INDEX [IX_ProvenanceAuditResults_AuditPeriod]
    ON [dbo].[ProvenanceAuditResults]([AuditPeriodStart], [AuditPeriodEnd]);
GO

CREATE NONCLUSTERED INDEX [IX_ProvenanceAuditResults_AuditedAt]
    ON [dbo].[ProvenanceAuditResults]([AuditedAt] DESC);
GO