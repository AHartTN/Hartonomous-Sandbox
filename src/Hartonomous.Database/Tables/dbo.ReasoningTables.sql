-- =============================================
-- PHASE 7.3: Missing Reasoning Tables
-- Tables for advanced reasoning procedures
-- =============================================

-- Table 1: ReasoningChains (for sp_ChainOfThoughtReasoning)
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'ReasoningChains' AND schema_id = SCHEMA_ID('dbo'))
BEGIN
    CREATE TABLE dbo.ReasoningChains (
        ReasoningChainId BIGINT IDENTITY(1,1) PRIMARY KEY,
        ProblemId UNIQUEIDENTIFIER NOT NULL,
        ReasoningType NVARCHAR(50) NOT NULL,
        ChainData NVARCHAR(MAX) NOT NULL,      -- JSON
        TotalSteps INT NOT NULL,
        DurationMs INT NOT NULL,
        CoherenceMetrics NVARCHAR(MAX),        -- JSON
        CreatedAt DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
        CONSTRAINT CHK_ReasoningChains_ChainData CHECK (ISJSON(ChainData) = 1),
        CONSTRAINT CHK_ReasoningChains_CoherenceMetrics CHECK (CoherenceMetrics IS NULL OR ISJSON(CoherenceMetrics) = 1)
    );

    CREATE NONCLUSTERED INDEX IX_ReasoningChains_ProblemId 
        ON dbo.ReasoningChains (ProblemId, CreatedAt DESC);
    
    CREATE NONCLUSTERED INDEX IX_ReasoningChains_ReasoningType 
        ON dbo.ReasoningChains (ReasoningType, CreatedAt DESC);

    PRINT '? Created table: dbo.ReasoningChains';
END
ELSE
    PRINT '? Table dbo.ReasoningChains already exists';
GO

-- Table 2: MultiPathReasoning (for sp_MultiPathReasoning)
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'MultiPathReasoning' AND schema_id = SCHEMA_ID('dbo'))
BEGIN
    CREATE TABLE dbo.MultiPathReasoning (
        ReasoningId BIGINT IDENTITY(1,1) PRIMARY KEY,
        ProblemId UNIQUEIDENTIFIER NOT NULL,
        BasePrompt NVARCHAR(MAX) NOT NULL,
        NumPaths INT NOT NULL,
        MaxDepth INT NOT NULL,
        BestPathId INT NOT NULL,
        ReasoningTree NVARCHAR(MAX) NOT NULL,  -- JSON
        DurationMs INT NOT NULL,
        CreatedAt DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
        CONSTRAINT CHK_MultiPathReasoning_Tree CHECK (ISJSON(ReasoningTree) = 1)
    );

    CREATE NONCLUSTERED INDEX IX_MultiPathReasoning_ProblemId 
        ON dbo.MultiPathReasoning (ProblemId, CreatedAt DESC);

    PRINT '? Created table: dbo.MultiPathReasoning';
END
ELSE
    PRINT '? Table dbo.MultiPathReasoning already exists';
GO

-- Table 3: SelfConsistencyResults (for sp_SelfConsistencyReasoning)
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'SelfConsistencyResults' AND schema_id = SCHEMA_ID('dbo'))
BEGIN
    CREATE TABLE dbo.SelfConsistencyResults (
        ResultId BIGINT IDENTITY(1,1) PRIMARY KEY,
        ProblemId UNIQUEIDENTIFIER NOT NULL,
        Prompt NVARCHAR(MAX) NOT NULL,
        NumSamples INT NOT NULL,
        ConsensusAnswer NVARCHAR(MAX),
        AgreementRatio FLOAT,
        SampleData NVARCHAR(MAX) NOT NULL,     -- JSON
        ConsensusMetrics NVARCHAR(MAX),        -- JSON
        DurationMs INT NOT NULL,
        CreatedAt DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
        CONSTRAINT CHK_SelfConsistency_SampleData CHECK (ISJSON(SampleData) = 1),
        CONSTRAINT CHK_SelfConsistency_Metrics CHECK (ConsensusMetrics IS NULL OR ISJSON(ConsensusMetrics) = 1)
    );

    CREATE NONCLUSTERED INDEX IX_SelfConsistencyResults_ProblemId 
        ON dbo.SelfConsistencyResults (ProblemId, CreatedAt DESC);

    PRINT '? Created table: dbo.SelfConsistencyResults';
END
ELSE
    PRINT '? Table dbo.SelfConsistencyResults already exists';
GO

PRINT '? PHASE 7.3: All reasoning tables created';
GO
