CREATE TABLE [dbo].[StreamOrchestrationResults]
(
    [Id] INT IDENTITY(1,1) NOT NULL,
    [SensorType] NVARCHAR(100) NOT NULL,
    [TimeWindowStart] DATETIME2 NOT NULL,
    [TimeWindowEnd] DATETIME2 NOT NULL,
    [AggregationLevel] NVARCHAR(50) NOT NULL,
    [ComponentStream] VARBINARY(MAX) NULL,
    [ComponentCount] INT NULL,
    [DurationMs] INT NOT NULL,
    [CreatedAt] DATETIME2 NOT NULL DEFAULT (SYSUTCDATETIME()),
    
    CONSTRAINT [PK_StreamOrchestrationResults] PRIMARY KEY CLUSTERED ([Id])
);
GO

CREATE NONCLUSTERED INDEX [IX_StreamOrchestrationResults_SensorType]
    ON [dbo].[StreamOrchestrationResults]([SensorType]);
GO

CREATE NONCLUSTERED INDEX [IX_StreamOrchestrationResults_TimeWindow]
    ON [dbo].[StreamOrchestrationResults]([TimeWindowStart], [TimeWindowEnd]);
GO

CREATE NONCLUSTERED INDEX [IX_StreamOrchestrationResults_CreatedAt]
    ON [dbo].[StreamOrchestrationResults]([CreatedAt] DESC);
GO