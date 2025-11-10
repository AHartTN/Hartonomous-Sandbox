CREATE TABLE [dbo].[MultiPathReasoning]
(
    [Id] INT IDENTITY(1,1) NOT NULL,
    [ProblemId] UNIQUEIDENTIFIER NOT NULL,
    [BasePrompt] NVARCHAR(MAX) NOT NULL,
    [NumPaths] INT NOT NULL,
    [MaxDepth] INT NOT NULL,
    [BestPathId] INT NULL,
    [ReasoningTree] NVARCHAR(MAX) NULL,
    [DurationMs] INT NOT NULL,
    [CreatedAt] DATETIME2 NOT NULL DEFAULT (SYSUTCDATETIME()),
    
    CONSTRAINT [PK_MultiPathReasoning] PRIMARY KEY CLUSTERED ([Id]),
    CONSTRAINT [CK_MultiPathReasoning_ReasoningTree_IsJson] CHECK ([ReasoningTree] IS NULL OR ISJSON([ReasoningTree]) = 1)
);
GO

CREATE NONCLUSTERED INDEX [IX_MultiPathReasoning_ProblemId]
    ON [dbo].[MultiPathReasoning]([ProblemId]);
GO

CREATE NONCLUSTERED INDEX [IX_MultiPathReasoning_CreatedAt]
    ON [dbo].[MultiPathReasoning]([CreatedAt] DESC);
GO