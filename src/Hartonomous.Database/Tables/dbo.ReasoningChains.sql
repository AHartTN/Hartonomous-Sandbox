CREATE TABLE [dbo].[ReasoningChains]
(
    [Id] INT IDENTITY(1,1) NOT NULL,
    [ProblemId] UNIQUEIDENTIFIER NOT NULL,
    [ReasoningType] NVARCHAR(50) NOT NULL DEFAULT ('chain_of_thought'),
    [ChainData] NVARCHAR(MAX) NULL,
    [TotalSteps] INT NOT NULL,
    [DurationMs] INT NOT NULL,
    [CreatedAt] DATETIME2 NOT NULL DEFAULT (SYSUTCDATETIME()),
    
    CONSTRAINT [PK_ReasoningChains] PRIMARY KEY CLUSTERED ([Id]),
    CONSTRAINT [CK_ReasoningChains_ChainData_IsJson] CHECK ([ChainData] IS NULL OR ISJSON([ChainData]) = 1)
);
GO

CREATE NONCLUSTERED INDEX [IX_ReasoningChains_ProblemId]
    ON [dbo].[ReasoningChains]([ProblemId]);
GO

CREATE NONCLUSTERED INDEX [IX_ReasoningChains_CreatedAt]
    ON [dbo].[ReasoningChains]([CreatedAt] DESC);
GO