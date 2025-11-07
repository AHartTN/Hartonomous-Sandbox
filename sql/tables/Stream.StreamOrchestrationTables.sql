-- Stream Orchestration Tables
-- Tables to support stream orchestration procedures

-- StreamOrchestrationResults: Store sensor stream orchestration results
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

-- StreamFusionResults: Store multi-modal stream fusion results
CREATE TABLE dbo.StreamFusionResults (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    StreamIds NVARCHAR(MAX) NOT NULL,
    FusionType NVARCHAR(50) NOT NULL,
    Weights NVARCHAR(MAX),
    FusedStream VARBINARY(MAX),
    ComponentCount INT,
    DurationMs INT NOT NULL,
    CreatedAt DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),

    INDEX IX_StreamFusionResults_FusionType (FusionType),
    INDEX IX_StreamFusionResults_CreatedAt (CreatedAt DESC)
);

-- EventGenerationResults: Store event generation results
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

-- EventAtoms: Store generated event atoms
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

PRINT 'Stream orchestration tables created successfully';
PRINT 'StreamOrchestrationResults: Sensor stream orchestration storage';
PRINT 'StreamFusionResults: Multi-modal fusion storage';
PRINT 'EventGenerationResults: Event generation metadata';
PRINT 'EventAtoms: Generated event atoms';
GO