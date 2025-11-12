-- =============================================
-- Table: dbo.InferenceSteps
-- =============================================
-- Represents a detailed breakdown of a single step in a multi-step inference operation.
-- This table was previously managed by EF Core.
-- =============================================

IF OBJECT_ID('dbo.InferenceSteps', 'U') IS NOT NULL
    DROP TABLE dbo.InferenceSteps;
GO

CREATE TABLE dbo.InferenceSteps
(
    StepId          BIGINT          IDENTITY(1,1) NOT NULL PRIMARY KEY,
    InferenceId     BIGINT          NOT NULL,
    StepNumber      INT             NOT NULL,
    ModelId         INT             NULL,
    LayerId         BIGINT          NULL, -- Added from entity, not in config
    OperationType   NVARCHAR(50)    NULL,
    QueryText       NVARCHAR(MAX)   NULL,
    IndexUsed       NVARCHAR(200)   NULL,
    RowsExamined    BIGINT          NULL, -- Added from entity, not in config
    RowsReturned    BIGINT          NULL, -- Added from entity, not in config
    DurationMs      INT             NULL,
    CacheUsed       BIT             NOT NULL DEFAULT 0,

    CONSTRAINT FK_InferenceSteps_InferenceRequests FOREIGN KEY (InferenceId) REFERENCES dbo.InferenceRequests(InferenceId) ON DELETE CASCADE,
    CONSTRAINT FK_InferenceSteps_Models FOREIGN KEY (ModelId) REFERENCES dbo.Models(ModelId) ON DELETE SET NULL,
    CONSTRAINT FK_InferenceSteps_ModelLayers FOREIGN KEY (LayerId) REFERENCES dbo.ModelLayers(LayerId) ON DELETE SET NULL -- Added FK to ModelLayers
);
GO

CREATE INDEX IX_InferenceSteps_InferenceId_StepNumber ON dbo.InferenceSteps(InferenceId, StepNumber);
GO

PRINT 'Created table dbo.InferenceSteps';
GO
