CREATE TABLE [dbo].[SelfConsistencyResults]
(
    [Id] INT IDENTITY(1,1) NOT NULL,
    [ProblemId] UNIQUEIDENTIFIER NOT NULL,
    [Prompt] NVARCHAR(MAX) NOT NULL,
    [NumSamples] INT NOT NULL,
    [ConsensusAnswer] NVARCHAR(MAX) NULL,
    [AgreementRatio] FLOAT NOT NULL,
    [SampleData] NVARCHAR(MAX) NULL,
    [DurationMs] INT NOT NULL,
    [CreatedAt] DATETIME2 NOT NULL DEFAULT (SYSUTCDATETIME()),
    
    CONSTRAINT [PK_SelfConsistencyResults] PRIMARY KEY CLUSTERED ([Id]),
    CONSTRAINT [CK_SelfConsistencyResults_SampleData_IsJson] CHECK ([SampleData] IS NULL OR ISJSON([SampleData]) = 1)
);
GO

CREATE NONCLUSTERED INDEX [IX_SelfConsistencyResults_ProblemId]
    ON [dbo].[SelfConsistencyResults]([ProblemId]);
GO

CREATE NONCLUSTERED INDEX [IX_SelfConsistencyResults_CreatedAt]
    ON [dbo].[SelfConsistencyResults]([CreatedAt] DESC);
GO