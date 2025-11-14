CREATE TABLE dbo.MultiPathReasoning (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    ProblemId UNIQUEIDENTIFIER NOT NULL,
    BasePrompt NVARCHAR(MAX) NOT NULL,
    NumPaths INT NOT NULL,
    MaxDepth INT NOT NULL,
    BestPathId INT,
    ReasoningTree JSON, -- JSON representation of reasoning tree
    DurationMs INT NOT NULL,
    CreatedAt DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),

    INDEX IX_MultiPathReasoning_ProblemId (ProblemId),
    INDEX IX_MultiPathReasoning_CreatedAt (CreatedAt DESC)
);