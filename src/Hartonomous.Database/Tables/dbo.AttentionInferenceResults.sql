CREATE TABLE [dbo].[AttentionInferenceResults]
(
    [Id] INT IDENTITY(1,1) NOT NULL,
    [ProblemId] UNIQUEIDENTIFIER NOT NULL,
    [Query] NVARCHAR(MAX) NOT NULL,
    [ModelId] INT NOT NULL,
    [MaxReasoningSteps] INT NOT NULL,
    [AttentionHeads] INT NOT NULL,
    [ReasoningSteps] NVARCHAR(MAX) NULL,
    [TotalSteps] INT NOT NULL,
    [DurationMs] INT NOT NULL,
    [CreatedAt] DATETIME2 NOT NULL DEFAULT (SYSUTCDATETIME()),
    
    CONSTRAINT [PK_AttentionInferenceResults] PRIMARY KEY CLUSTERED ([Id])
);
GO

CREATE NONCLUSTERED INDEX [IX_AttentionInferenceResults_ProblemId]
    ON [dbo].[AttentionInferenceResults]([ProblemId]);
GO

CREATE NONCLUSTERED INDEX [IX_AttentionInferenceResults_CreatedAt]
    ON [dbo].[AttentionInferenceResults]([CreatedAt] DESC);
GO