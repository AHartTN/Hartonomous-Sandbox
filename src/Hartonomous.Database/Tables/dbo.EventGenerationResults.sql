CREATE TABLE [dbo].[EventGenerationResults]
(
    [Id] INT IDENTITY(1,1) NOT NULL,
    [StreamId] INT NOT NULL,
    [EventType] NVARCHAR(100) NOT NULL,
    [Threshold] FLOAT NOT NULL,
    [ClusteringMethod] NVARCHAR(50) NOT NULL,
    [EventsGenerated] INT NOT NULL,
    [DurationMs] INT NOT NULL,
    [CreatedAt] DATETIME2 NOT NULL DEFAULT (SYSUTCDATETIME()),
    
    CONSTRAINT [PK_EventGenerationResults] PRIMARY KEY CLUSTERED ([Id]),
    CONSTRAINT [FK_EventGenerationResults_StreamOrchestration] 
        FOREIGN KEY ([StreamId]) REFERENCES [dbo].[StreamOrchestrationResults]([Id])
);
GO

CREATE NONCLUSTERED INDEX [IX_EventGenerationResults_StreamId]
    ON [dbo].[EventGenerationResults]([StreamId]);
GO

CREATE NONCLUSTERED INDEX [IX_EventGenerationResults_EventType]
    ON [dbo].[EventGenerationResults]([EventType]);
GO

CREATE NONCLUSTERED INDEX [IX_EventGenerationResults_CreatedAt]
    ON [dbo].[EventGenerationResults]([CreatedAt] DESC);
GO