CREATE TABLE dbo.SemanticFeatures (
    AtomEmbeddingId BIGINT NOT NULL PRIMARY KEY,
    TopicTechnical FLOAT DEFAULT 0,
    TopicBusiness FLOAT DEFAULT 0,
    TopicScientific FLOAT DEFAULT 0,
    TopicCreative FLOAT DEFAULT 0,
    SentimentScore FLOAT DEFAULT 0,
    FormalityScore FLOAT DEFAULT 0,
    ComplexityScore FLOAT DEFAULT 0,
    TemporalRelevance FLOAT DEFAULT 1,
    ReferenceDate DATETIME2,
    TextLength INT,
    WordCount INT,
    UniqueWordRatio FLOAT,
    AvgWordLength FLOAT,
    ComputedAt DATETIME2 DEFAULT SYSUTCDATETIME(),
    CONSTRAINT FK_SemanticFeatures_AtomEmbeddings FOREIGN KEY (AtomEmbeddingId)
        REFERENCES dbo.AtomEmbeddings(AtomEmbeddingId) ON DELETE CASCADE
);
GO