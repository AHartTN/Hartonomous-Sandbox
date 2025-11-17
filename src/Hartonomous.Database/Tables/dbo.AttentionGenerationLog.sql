CREATE TABLE dbo.AttentionGenerationLog (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    ModelId INT NOT NULL,
    InputAtomIds JSON NOT NULL,
    ContextJson JSON, -- Native JSON for attention context
    MaxTokens INT NOT NULL,
    Temperature FLOAT NOT NULL,
    TopK INT NOT NULL,
    TopP FLOAT NOT NULL,
    AttentionHeads INT NOT NULL,
    GenerationStreamId BIGINT NOT NULL,
    GeneratedAtomIds JSON NULL,
    DurationMs INT NOT NULL,
    TenantId INT NOT NULL DEFAULT 0,
    CreatedAt DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),

    INDEX IX_AttentionGenerationLog_ModelId (ModelId),
    INDEX IX_AttentionGenerationLog_GenerationStreamId (GenerationStreamId),
    INDEX IX_AttentionGenerationLog_CreatedAt (CreatedAt DESC),

    CONSTRAINT FK_AttentionGenerationLog_Model FOREIGN KEY (ModelId) REFERENCES dbo.Model(ModelId)
);