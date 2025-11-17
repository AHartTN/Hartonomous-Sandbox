CREATE TABLE dbo.AttentionInferenceResults (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    ProblemId UNIQUEIDENTIFIER NOT NULL,
    Query NVARCHAR(MAX) NOT NULL,
    ModelId INT NOT NULL,
    MaxReasoningSteps INT NOT NULL,
    AttentionHeads INT NOT NULL,
    ReasoningSteps JSON, -- JSON array of reasoning steps
    TotalSteps INT NOT NULL,
    DurationMs INT NOT NULL,
    CreatedAt DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),

    INDEX IX_AttentionInferenceResults_ProblemId (ProblemId),
    INDEX IX_AttentionInferenceResults_CreatedAt (CreatedAt DESC),

    CONSTRAINT FK_AttentionInferenceResult_Model FOREIGN KEY (ModelId) REFERENCES dbo.Model(ModelId)
);