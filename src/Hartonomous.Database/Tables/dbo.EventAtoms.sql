CREATE TABLE dbo.EventAtoms (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    StreamId INT NOT NULL,
    EventType NVARCHAR(100) NOT NULL,
    CentroidAtomId BIGINT NOT NULL,
    AverageWeight FLOAT NOT NULL,
    ClusterSize INT NOT NULL,
    ClusterId INT NOT NULL,
    CreatedAt DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),

    INDEX IX_EventAtoms_StreamId (StreamId),
    INDEX IX_EventAtoms_EventType (EventType),
    INDEX IX_EventAtoms_ClusterId (ClusterId),
    INDEX IX_EventAtoms_CreatedAt (CreatedAt DESC),

    FOREIGN KEY (StreamId) REFERENCES dbo.StreamOrchestrationResults(Id)
);