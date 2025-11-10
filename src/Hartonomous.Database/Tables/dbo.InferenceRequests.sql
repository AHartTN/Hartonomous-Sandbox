-- =============================================
-- Table: dbo.InferenceRequests
-- Description: Inference requests for auditing and performance monitoring.
--              Tracks multi-model inference operations with autonomous complexity scoring.
-- =============================================
CREATE TABLE [dbo].[InferenceRequests]
(
    [InferenceId]             BIGINT           NOT NULL IDENTITY(1,1),
    [RequestTimestamp]        DATETIME2(7)     NOT NULL DEFAULT (SYSUTCDATETIME()),
    [TaskType]                NVARCHAR(50)     NULL,
    [InputData]               NVARCHAR(MAX)    NULL, -- JSON column type applied via CHECK constraint
    [InputHash]               BINARY(32)       NULL,
    [CorrelationId]           NVARCHAR(256)    NULL,
    [Status]                  NVARCHAR(50)     NULL,
    [Confidence]              FLOAT            NULL,
    [ModelsUsed]              NVARCHAR(MAX)    NULL, -- JSON column type applied via CHECK constraint
    [EnsembleStrategy]        NVARCHAR(50)     NULL,
    [OutputData]              NVARCHAR(MAX)    NULL, -- JSON column type applied via CHECK constraint
    [OutputMetadata]          NVARCHAR(MAX)    NULL, -- JSON column type applied via CHECK constraint
    [TotalDurationMs]         INT              NULL,
    [CacheHit]                BIT              NOT NULL DEFAULT (0),
    [UserRating]              TINYINT          NULL,
    [UserFeedback]            NVARCHAR(MAX)    NULL,
    [Complexity]              INT              NULL,
    [SlaTier]                 NVARCHAR(50)     NULL,
    [EstimatedResponseTimeMs] INT              NULL,

    CONSTRAINT [PK_InferenceRequests] PRIMARY KEY CLUSTERED ([InferenceId] ASC),

    CONSTRAINT [CK_InferenceRequests_InputData_IsJson] 
        CHECK ([InputData] IS NULL OR ISJSON([InputData]) = 1),

    CONSTRAINT [CK_InferenceRequests_ModelsUsed_IsJson] 
        CHECK ([ModelsUsed] IS NULL OR ISJSON([ModelsUsed]) = 1),

    CONSTRAINT [CK_InferenceRequests_OutputData_IsJson] 
        CHECK ([OutputData] IS NULL OR ISJSON([OutputData]) = 1),

    CONSTRAINT [CK_InferenceRequests_OutputMetadata_IsJson] 
        CHECK ([OutputMetadata] IS NULL OR ISJSON([OutputMetadata]) = 1)
);
GO

CREATE NONCLUSTERED INDEX [IX_InferenceRequests_RequestTimestamp]
    ON [dbo].[InferenceRequests]([RequestTimestamp] DESC);
GO

CREATE NONCLUSTERED INDEX [IX_InferenceRequests_TaskType]
    ON [dbo].[InferenceRequests]([TaskType] ASC);
GO

CREATE NONCLUSTERED INDEX [IX_InferenceRequests_InputHash]
    ON [dbo].[InferenceRequests]([InputHash] ASC);
GO

CREATE NONCLUSTERED INDEX [IX_InferenceRequests_CacheHit]
    ON [dbo].[InferenceRequests]([CacheHit] ASC);
GO