-- Test results table for autonomous improvement CI/CD parsing
-- Stores test outcomes from automated test runs for Phase 5 evaluation

SET QUOTED_IDENTIFIER ON;

IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE schema_id = SCHEMA_ID('dbo') AND name = 'TestResults')
BEGIN
    CREATE TABLE dbo.TestResults (
    TestResultId BIGINT IDENTITY(1,1) NOT NULL,
    
    -- Link to autonomous improvement run
    ImprovementId UNIQUEIDENTIFIER NULL,
    
    -- CI/CD run information
    BuildId NVARCHAR(200) NULL,
    BuildNumber NVARCHAR(100) NULL,
    BuildUrl NVARCHAR(500) NULL,
    Branch NVARCHAR(200) NULL,
    CommitHash NVARCHAR(64) NULL,
    
    -- Test execution details
    TestSuite NVARCHAR(200) NOT NULL,
    TestName NVARCHAR(500) NOT NULL,
    TestOutcome NVARCHAR(50) NOT NULL, -- 'Passed', 'Failed', 'Skipped', 'Inconclusive'
    ErrorMessage NVARCHAR(MAX) NULL,
    StackTrace NVARCHAR(MAX) NULL,
    
    -- Performance metrics
    DurationMs INT NULL,
    
    -- Timestamps
    RunStartedAt DATETIME2(3) NOT NULL DEFAULT SYSUTCDATETIME(),
    RunCompletedAt DATETIME2(3) NULL,
    RecordedAt DATETIME2(3) NOT NULL DEFAULT SYSUTCDATETIME(),
    
    CONSTRAINT PK_TestResults PRIMARY KEY CLUSTERED (TestResultId),
    CONSTRAINT FK_TestResults_ImprovementId FOREIGN KEY (ImprovementId) 
        REFERENCES dbo.AutonomousImprovementHistory(ImprovementId)
);
GO

CREATE NONCLUSTERED INDEX IX_TestResults_ImprovementId 
ON dbo.TestResults(ImprovementId) 
INCLUDE (TestOutcome, TestSuite, TestName);
GO

CREATE NONCLUSTERED INDEX IX_TestResults_BuildId 
ON dbo.TestResults(BuildId) 
WHERE BuildId IS NOT NULL;
GO

CREATE NONCLUSTERED INDEX IX_TestResults_CommitHash 
ON dbo.TestResults(CommitHash) 
WHERE CommitHash IS NOT NULL;
GO

CREATE NONCLUSTERED INDEX IX_TestResults_Outcome 
ON dbo.TestResults(TestOutcome, RunStartedAt DESC);

END;
GO