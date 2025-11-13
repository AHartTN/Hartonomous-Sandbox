CREATE TABLE dbo.SelfConsistencyResults (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    ProblemId UNIQUEIDENTIFIER NOT NULL,
    Prompt NVARCHAR(MAX) NOT NULL,
    NumSamples INT NOT NULL,
    ConsensusAnswer NVARCHAR(MAX),
    AgreementRatio FLOAT NOT NULL,
    ConsensusMetrics NVARCHAR(MAX) NULL, -- JSON metrics
    SampleData NVARCHAR(MAX), -- JSON array of samples
    DurationMs INT NOT NULL,
    CreatedAt DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),

    INDEX IX_SelfConsistencyResults_ProblemId (ProblemId),
    INDEX IX_SelfConsistencyResults_CreatedAt (CreatedAt DESC)
);