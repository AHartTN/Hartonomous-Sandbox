CREATE TABLE [dbo].[EventAtoms]
(
    [Id] INT IDENTITY(1,1) NOT NULL,
    [StreamId] INT NOT NULL,
    [EventType] NVARCHAR(100) NOT NULL,
    [CentroidAtomId] BIGINT NOT NULL,
    [AverageWeight] FLOAT NOT NULL,
    [ClusterSize] INT NOT NULL,
    [ClusterId] INT NOT NULL,
    [CreatedAt] DATETIME2 NOT NULL DEFAULT (SYSUTCDATETIME()),
    
    CONSTRAINT [PK_EventAtoms] PRIMARY KEY CLUSTERED ([Id]),
    CONSTRAINT [FK_EventAtoms_StreamOrchestration] 
        FOREIGN KEY ([StreamId]) REFERENCES [dbo].[StreamOrchestrationResults]([Id])
);
GO

CREATE NONCLUSTERED INDEX [IX_EventAtoms_StreamId]
    ON [dbo].[EventAtoms]([StreamId]);
GO

CREATE NONCLUSTERED INDEX [IX_EventAtoms_EventType]
    ON [dbo].[EventAtoms]([EventType]);
GO

CREATE NONCLUSTERED INDEX [IX_EventAtoms_ClusterId]
    ON [dbo].[EventAtoms]([ClusterId]);
GO

CREATE NONCLUSTERED INDEX [IX_EventAtoms_CreatedAt]
    ON [dbo].[EventAtoms]([CreatedAt] DESC);
GO