CREATE TABLE [dbo].[TransformerInferenceResults]
(
    [Id] INT IDENTITY(1,1) NOT NULL,
    [ProblemId] UNIQUEIDENTIFIER NOT NULL,
    [InputSequence] NVARCHAR(MAX) NOT NULL,
    [ModelId] INT NOT NULL,
    [Layers] INT NOT NULL,
    [AttentionHeads] INT NOT NULL,
    [FeedForwardDim] INT NOT NULL,
    [LayerResults] NVARCHAR(MAX) NULL,
    [DurationMs] INT NOT NULL,
    [CreatedAt] DATETIME2 NOT NULL DEFAULT (SYSUTCDATETIME()),
    
    CONSTRAINT [PK_TransformerInferenceResults] PRIMARY KEY CLUSTERED ([Id])
);
GO

CREATE NONCLUSTERED INDEX [IX_TransformerInferenceResults_ProblemId]
    ON [dbo].[TransformerInferenceResults]([ProblemId]);
GO

CREATE NONCLUSTERED INDEX [IX_TransformerInferenceResults_CreatedAt]
    ON [dbo].[TransformerInferenceResults]([CreatedAt] DESC);
GO