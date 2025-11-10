CREATE TABLE [dbo].[StreamFusionResults]
(
    [Id] INT IDENTITY(1,1) NOT NULL,
    [StreamIds] NVARCHAR(MAX) NOT NULL,
    [FusionType] NVARCHAR(50) NOT NULL,
    [Weights] NVARCHAR(MAX) NULL,
    [FusedStream] VARBINARY(MAX) NULL,
    [ComponentCount] INT NULL,
    [DurationMs] INT NOT NULL,
    [CreatedAt] DATETIME2 NOT NULL DEFAULT (SYSUTCDATETIME()),
    
    CONSTRAINT [PK_StreamFusionResults] PRIMARY KEY CLUSTERED ([Id])
);
GO

CREATE NONCLUSTERED INDEX [IX_StreamFusionResults_FusionType]
    ON [dbo].[StreamFusionResults]([FusionType]);
GO

CREATE NONCLUSTERED INDEX [IX_StreamFusionResults_CreatedAt]
    ON [dbo].[StreamFusionResults]([CreatedAt] DESC);
GO