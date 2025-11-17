CREATE TABLE dbo.ReasoningChains (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    ProblemId UNIQUEIDENTIFIER NOT NULL,
    ReasoningType NVARCHAR(50) NOT NULL DEFAULT 'chain_of_thought',
    ChainData JSON, -- JSON array of reasoning steps
    CoherenceMetrics JSON NULL, -- JSON metrics
    TotalSteps INT NOT NULL,
    DurationMs INT NOT NULL,
    CreatedAt DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),

    INDEX IX_ReasoningChains_ProblemId (ProblemId),
    INDEX IX_ReasoningChains_CreatedAt (CreatedAt DESC)
);