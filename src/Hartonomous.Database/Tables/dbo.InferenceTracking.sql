CREATE TABLE dbo.InferenceRequests (
    InferenceId BIGINT IDENTITY(1,1) NOT NULL,
    TaskType NVARCHAR(100) NOT NULL,
    InputData NVARCHAR(MAX) NULL,
    OutputData NVARCHAR(MAX) NULL,
    ModelsUsed NVARCHAR(MAX) NULL,
    EnsembleStrategy NVARCHAR(50) NULL,
    OutputMetadata NVARCHAR(MAX) NULL,
    UserRating TINYINT NULL,
    TotalDurationMs INT NULL,
    Status NVARCHAR(50) NOT NULL DEFAULT 'pending',
    ErrorMessage NVARCHAR(MAX) NULL,
    TenantId NVARCHAR(128) NULL,
    RequestTimestamp DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
    CreatedUtc DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
    CompletedUtc DATETIME2 NULL,
    CONSTRAINT PK_InferenceRequests PRIMARY KEY CLUSTERED (InferenceId),
    CONSTRAINT CK_InferenceRequests_UserRating CHECK (UserRating BETWEEN 1 AND 5),
    INDEX IX_InferenceRequests_Status (Status) INCLUDE (InferenceId, CreatedUtc),
    INDEX IX_InferenceRequests_UserRating (UserRating) WHERE (UserRating IS NOT NULL),
    INDEX IX_InferenceRequests_Created (CreatedUtc DESC)
);

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
    CONSTRAINT FK_InferenceSteps_Inference FOREIGN KEY (InferenceId) REFERENCES dbo.InferenceRequests(InferenceId) ON DELETE CASCADE,
    INDEX IX_InferenceSteps_Inference (InferenceId, StepNumber),
    INDEX IX_InferenceSteps_Layer (LayerId) WHERE (LayerId IS NOT NULL)
);