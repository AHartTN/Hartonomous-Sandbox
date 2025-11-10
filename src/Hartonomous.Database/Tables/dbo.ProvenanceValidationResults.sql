CREATE TABLE [dbo].[ProvenanceValidationResults]
(
    [Id] INT IDENTITY(1,1) NOT NULL,
    [OperationId] UNIQUEIDENTIFIER NOT NULL,
    [ValidationResults] NVARCHAR(MAX) NULL,
    [OverallStatus] NVARCHAR(20) NOT NULL,
    [ValidationDurationMs] INT NOT NULL,
    [ValidatedAt] DATETIME2 NOT NULL DEFAULT (SYSUTCDATETIME()),
    
    CONSTRAINT [PK_ProvenanceValidationResults] PRIMARY KEY CLUSTERED ([Id]),
    CONSTRAINT [FK_ProvenanceValidationResults_OperationProvenance] 
        FOREIGN KEY ([OperationId]) REFERENCES [dbo].[OperationProvenance]([OperationId])
);
GO

CREATE NONCLUSTERED INDEX [IX_ProvenanceValidationResults_OperationId]
    ON [dbo].[ProvenanceValidationResults]([OperationId]);
GO

CREATE NONCLUSTERED INDEX [IX_ProvenanceValidationResults_Status]
    ON [dbo].[ProvenanceValidationResults]([OverallStatus]);
GO

CREATE NONCLUSTERED INDEX [IX_ProvenanceValidationResults_ValidatedAt]
    ON [dbo].[ProvenanceValidationResults]([ValidatedAt] DESC);
GO