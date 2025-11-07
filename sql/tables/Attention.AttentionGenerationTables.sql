-- Attention Generation Tables
-- Tables to support attention-based generation procedures

-- AttentionGenerationLog: Log attention-based generation calls
CREATE TABLE dbo.AttentionGenerationLog (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    ModelId INT NOT NULL,
    InputAtomIds NVARCHAR(MAX) NOT NULL,
    ContextJson NVARCHAR(MAX),
    MaxTokens INT NOT NULL,
    Temperature FLOAT NOT NULL,
    TopK INT NOT NULL,
    TopP FLOAT NOT NULL,
    AttentionHeads INT NOT NULL,
    GenerationStreamId BIGINT NOT NULL,
    DurationMs INT NOT NULL,
    TenantId INT NOT NULL DEFAULT 0,
    CreatedAt DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),

    INDEX IX_AttentionGenerationLog_ModelId (ModelId),
    INDEX IX_AttentionGenerationLog_GenerationStreamId (GenerationStreamId),
    INDEX IX_AttentionGenerationLog_CreatedAt (CreatedAt DESC)
);

-- AttentionInferenceResults: Store attention inference results
CREATE TABLE dbo.AttentionInferenceResults (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    ProblemId UNIQUEIDENTIFIER NOT NULL,
    Query NVARCHAR(MAX) NOT NULL,
    ModelId INT NOT NULL,
    MaxReasoningSteps INT NOT NULL,
    AttentionHeads INT NOT NULL,
    ReasoningSteps NVARCHAR(MAX), -- JSON array of reasoning steps
    TotalSteps INT NOT NULL,
    DurationMs INT NOT NULL,
    CreatedAt DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),

    INDEX IX_AttentionInferenceResults_ProblemId (ProblemId),
    INDEX IX_AttentionInferenceResults_CreatedAt (CreatedAt DESC)
);

-- TransformerInferenceResults: Store transformer-style inference results
CREATE TABLE dbo.TransformerInferenceResults (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    ProblemId UNIQUEIDENTIFIER NOT NULL,
    InputSequence NVARCHAR(MAX) NOT NULL,
    ModelId INT NOT NULL,
    Layers INT NOT NULL,
    AttentionHeads INT NOT NULL,
    FeedForwardDim INT NOT NULL,
    LayerResults NVARCHAR(MAX), -- JSON array of layer results
    DurationMs INT NOT NULL,
    CreatedAt DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),

    INDEX IX_TransformerInferenceResults_ProblemId (ProblemId),
    INDEX IX_TransformerInferenceResults_CreatedAt (CreatedAt DESC)
);

PRINT 'Attention generation tables created successfully';
PRINT 'AttentionGenerationLog: Generation call logging';
PRINT 'AttentionInferenceResults: Attention reasoning storage';
PRINT 'TransformerInferenceResults: Transformer pipeline storage';
GO