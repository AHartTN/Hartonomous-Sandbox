CREATE TABLE [dbo].[OperationProvenance]
(
    [Id] INT IDENTITY(1,1) NOT NULL,
    [OperationId] UNIQUEIDENTIFIER NOT NULL,
    [ProvenanceStream] NVARCHAR(MAX) NOT NULL,
    [CreatedAt] DATETIME2 NOT NULL DEFAULT (SYSUTCDATETIME()),
    
    CONSTRAINT [PK_OperationProvenance] PRIMARY KEY CLUSTERED ([Id]),
    CONSTRAINT [UQ_OperationProvenance_OperationId] UNIQUE NONCLUSTERED ([OperationId])
);
GO

CREATE NONCLUSTERED INDEX [IX_OperationProvenance_OperationId]
    ON [dbo].[OperationProvenance]([OperationId]);
GO

CREATE NONCLUSTERED INDEX [IX_OperationProvenance_CreatedAt]
    ON [dbo].[OperationProvenance]([CreatedAt] DESC);
GO