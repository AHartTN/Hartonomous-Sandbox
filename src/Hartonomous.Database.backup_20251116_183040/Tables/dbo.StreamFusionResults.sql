CREATE TABLE dbo.StreamFusionResults (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    StreamIds JSON NOT NULL,
    FusionType NVARCHAR(50) NOT NULL,
    Weights JSON,
    FusedStream VARBINARY(MAX),
    ComponentCount INT,
    DurationMs INT NOT NULL,
    CreatedAt DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),

    INDEX IX_StreamFusionResults_FusionType (FusionType),
    INDEX IX_StreamFusionResults_CreatedAt (CreatedAt DESC)
);