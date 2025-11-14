CREATE TABLE dbo.TransformerInferenceResults (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    ProblemId UNIQUEIDENTIFIER NOT NULL,
    InputSequence NVARCHAR(MAX) NOT NULL,
    ModelId INT NOT NULL,
    Layers INT NOT NULL,
    AttentionHeads INT NOT NULL,
    FeedForwardDim INT NOT NULL,
    LayerResults JSON, -- JSON array of layer results
    DurationMs INT NOT NULL,
    CreatedAt DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),

    INDEX IX_TransformerInferenceResults_ProblemId (ProblemId),
    INDEX IX_TransformerInferenceResults_CreatedAt (CreatedAt DESC),

    CONSTRAINT FK_TransformerInferenceResults_Models FOREIGN KEY (ModelId) REFERENCES dbo.Models(ModelId)
);