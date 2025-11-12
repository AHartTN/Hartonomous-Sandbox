CREATE TABLE dbo.StreamOrchestrationResults (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    SensorType NVARCHAR(100) NOT NULL,
    TimeWindowStart DATETIME2 NOT NULL,
    TimeWindowEnd DATETIME2 NOT NULL,
    AggregationLevel NVARCHAR(50) NOT NULL,
    ComponentStream VARBINARY(MAX),
    ComponentCount INT,
    DurationMs INT NOT NULL,
    CreatedAt DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),

    INDEX IX_StreamOrchestrationResults_SensorType (SensorType),
    INDEX IX_StreamOrchestrationResults_TimeWindow (TimeWindowStart, TimeWindowEnd),
    INDEX IX_StreamOrchestrationResults_CreatedAt (CreatedAt DESC)
);