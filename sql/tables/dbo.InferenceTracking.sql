USE Hartonomous;
GO

-- Inference tracking tables
IF OBJECT_ID(N'dbo.InferenceRequests', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.InferenceRequests (
        InferenceId BIGINT IDENTITY(1,1) NOT NULL,
        TaskType NVARCHAR(100) NOT NULL,
        InputData NVARCHAR(MAX) NULL,
        OutputData NVARCHAR(MAX) NULL,
        ModelsUsed NVARCHAR(MAX) NULL,
        EnsembleStrategy NVARCHAR(50) NULL,
        OutputMetadata NVARCHAR(MAX) NULL,
        UserRating TINYINT NULL CHECK (UserRating BETWEEN 1 AND 5),
        TotalDurationMs INT NULL,
        Status NVARCHAR(50) NOT NULL DEFAULT 'pending',
        ErrorMessage NVARCHAR(MAX) NULL,
        TenantId NVARCHAR(128) NULL,
        RequestTimestamp DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
        CreatedUtc DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
        CompletedUtc DATETIME2 NULL,

        CONSTRAINT PK_InferenceRequests PRIMARY KEY CLUSTERED (InferenceId)
    );
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_InferenceRequests_Status' AND object_id = OBJECT_ID(N'dbo.InferenceRequests'))
BEGIN
    CREATE INDEX IX_InferenceRequests_Status ON dbo.InferenceRequests (Status) INCLUDE (InferenceId, CreatedUtc);
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_InferenceRequests_UserRating' AND object_id = OBJECT_ID(N'dbo.InferenceRequests'))
BEGIN
    CREATE INDEX IX_InferenceRequests_UserRating ON dbo.InferenceRequests (UserRating) WHERE UserRating IS NOT NULL;
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_InferenceRequests_Created' AND object_id = OBJECT_ID(N'dbo.InferenceRequests'))
BEGIN
    CREATE INDEX IX_InferenceRequests_Created ON dbo.InferenceRequests (CreatedUtc DESC);
END
GO

IF OBJECT_ID(N'dbo.InferenceSteps', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.InferenceSteps (
        InferenceStepId BIGINT IDENTITY(1,1) NOT NULL,
        InferenceId BIGINT NOT NULL,
        StepNumber INT NOT NULL,
        LayerId INT NULL,
        AtomId BIGINT NULL,
        StepType NVARCHAR(50) NULL,
        OperationType NVARCHAR(100) NULL,
        DurationMs INT NULL,
        RowsReturned INT NULL,
        Metadata NVARCHAR(MAX) NULL,

        CONSTRAINT PK_InferenceSteps PRIMARY KEY CLUSTERED (InferenceStepId),
        CONSTRAINT FK_InferenceSteps_Inference FOREIGN KEY (InferenceId)
            REFERENCES dbo.InferenceRequests(InferenceId) ON DELETE CASCADE
    );
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_InferenceSteps_Inference' AND object_id = OBJECT_ID(N'dbo.InferenceSteps'))
BEGIN
    CREATE INDEX IX_InferenceSteps_Inference ON dbo.InferenceSteps (InferenceId, StepNumber);
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_InferenceSteps_Layer' AND object_id = OBJECT_ID(N'dbo.InferenceSteps'))
BEGIN
    CREATE INDEX IX_InferenceSteps_Layer ON dbo.InferenceSteps (LayerId) WHERE LayerId IS NOT NULL;
END
GO

PRINT 'Created InferenceRequests and InferenceSteps tables';
