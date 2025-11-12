-- =============================================
-- Table: dbo.InferenceRequests
-- =============================================
-- Represents an inference request for auditing and performance monitoring.
-- This table was previously managed by EF Core.
-- =============================================

IF OBJECT_ID('dbo.InferenceRequests', 'U') IS NOT NULL
    DROP TABLE dbo.InferenceRequests;
GO

CREATE TABLE dbo.InferenceRequests
(
    InferenceId             BIGINT          IDENTITY(1,1) NOT NULL PRIMARY KEY,
    RequestTimestamp        DATETIME2       NOT NULL DEFAULT SYSUTCDATETIME(),
    TaskType                NVARCHAR(50)    NULL,
    InputData               NVARCHAR(MAX)   NULL,
    InputHash               BINARY(32)      NULL,
    CorrelationId           NVARCHAR(MAX)   NULL, -- Added from entity, not in config
    Status                  NVARCHAR(50)    NULL, -- Added from entity, not in config
    Confidence              FLOAT           NULL, -- Added from entity, not in config
    ModelsUsed              NVARCHAR(MAX)   NULL,
    EnsembleStrategy        NVARCHAR(50)    NULL,
    OutputData              NVARCHAR(MAX)   NULL,
    OutputMetadata          NVARCHAR(MAX)   NULL,
    TotalDurationMs         INT             NULL,
    CacheHit                BIT             NOT NULL DEFAULT 0,
    UserRating              TINYINT         NULL, -- Added from entity, not in config
    UserFeedback            NVARCHAR(MAX)   NULL, -- Added from entity, not in config
    Complexity              INT             NULL,
    SlaTier                 NVARCHAR(50)    NULL,
    EstimatedResponseTimeMs INT             NULL,

    CONSTRAINT CK_InferenceRequests_InputData_IsJson CHECK (InputData IS NULL OR ISJSON(InputData) = 1),
    CONSTRAINT CK_InferenceRequests_ModelsUsed_IsJson CHECK (ModelsUsed IS NULL OR ISJSON(ModelsUsed) = 1),
    CONSTRAINT CK_InferenceRequests_OutputData_IsJson CHECK (OutputData IS NULL OR ISJSON(OutputData) = 1),
    CONSTRAINT CK_InferenceRequests_OutputMetadata_IsJson CHECK (OutputMetadata IS NULL OR ISJSON(OutputMetadata) = 1)
);
GO

CREATE INDEX IX_InferenceRequests_RequestTimestamp ON dbo.InferenceRequests(RequestTimestamp DESC);
GO

CREATE INDEX IX_InferenceRequests_TaskType ON dbo.InferenceRequests(TaskType);
GO

CREATE INDEX IX_InferenceRequests_InputHash ON dbo.InferenceRequests(InputHash);
GO

CREATE INDEX IX_InferenceRequests_CacheHit ON dbo.InferenceRequests(CacheHit);
GO

PRINT 'Created table dbo.InferenceRequests';
GO
