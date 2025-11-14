-- Reasoning Framework Tables
-- Tables to support AI reasoning procedures

-- ReasoningChains: Store chain-of-thought reasoning results
CREATE TABLE dbo.ReasoningChains (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    ProblemId UNIQUEIDENTIFIER NOT NULL,
    ReasoningType NVARCHAR(50) NOT NULL DEFAULT 'chain_of_thought',
    ChainData JSON, -- JSON array of reasoning steps
    TotalSteps INT NOT NULL,
    DurationMs INT NOT NULL,
    CreatedAt DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),

    INDEX IX_ReasoningChains_ProblemId (ProblemId),
    INDEX IX_ReasoningChains_CreatedAt (CreatedAt DESC)
);

-- SelfConsistencyResults: Store self-consistency reasoning results
CREATE TABLE dbo.SelfConsistencyResults (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    ProblemId UNIQUEIDENTIFIER NOT NULL,
    Prompt NVARCHAR(MAX) NOT NULL,
    NumSamples INT NOT NULL,
    ConsensusAnswer NVARCHAR(MAX),
    AgreementRatio FLOAT NOT NULL,
    SampleData JSON, -- JSON array of samples
    DurationMs INT NOT NULL,
    CreatedAt DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),

    INDEX IX_SelfConsistencyResults_ProblemId (ProblemId),
    INDEX IX_SelfConsistencyResults_CreatedAt (CreatedAt DESC)
);

-- MultiPathReasoning: Store multi-path reasoning results
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