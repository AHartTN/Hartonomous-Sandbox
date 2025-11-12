CREATE TABLE dbo.EventGenerationResults (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    StreamId INT NOT NULL,
    EventType NVARCHAR(100) NOT NULL,
    Threshold FLOAT NOT NULL,
    ClusteringMethod NVARCHAR(50) NOT NULL,
    EventsGenerated INT NOT NULL,
    DurationMs INT NOT NULL,
    CreatedAt DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),

    INDEX IX_EventGenerationResults_StreamId (StreamId),
    INDEX IX_EventGenerationResults_EventType (EventType),
    INDEX IX_EventGenerationResults_CreatedAt (CreatedAt DESC),

    FOREIGN KEY (StreamId) REFERENCES dbo.StreamOrchestrationResults(Id)
);