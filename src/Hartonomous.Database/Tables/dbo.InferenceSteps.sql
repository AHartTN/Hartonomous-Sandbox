-- =============================================
-- Table: dbo.InferenceSteps
-- Description: Detailed breakdown of individual steps in multi-step inference operations.
--              Enables performance profiling and query optimization tracking.
-- =============================================
CREATE TABLE [dbo].[InferenceSteps]
(
    [StepId]         BIGINT           NOT NULL IDENTITY(1,1),
    [InferenceId]    BIGINT           NOT NULL,
    [StepNumber]     INT              NOT NULL,
    [ModelId]        INT              NULL,
    [LayerId]        BIGINT           NULL,
    [OperationType]  NVARCHAR(50)     NULL,
    [QueryText]      NVARCHAR(MAX)    NULL,
    [IndexUsed]      NVARCHAR(200)    NULL,
    [RowsExamined]   BIGINT           NULL,
    [RowsReturned]   BIGINT           NULL,
    [DurationMs]     INT              NULL,
    [CacheUsed]      BIT              NOT NULL DEFAULT (0),

    CONSTRAINT [PK_InferenceSteps] PRIMARY KEY CLUSTERED ([StepId] ASC),

    CONSTRAINT [FK_InferenceSteps_InferenceRequests] 
        FOREIGN KEY ([InferenceId]) 
        REFERENCES [dbo].[InferenceRequests]([InferenceId]) 
        ON DELETE CASCADE,

    CONSTRAINT [FK_InferenceSteps_Models] 
        FOREIGN KEY ([ModelId]) 
        REFERENCES [dbo].[Models]([ModelId]) 
        ON DELETE SET NULL
);
GO

CREATE NONCLUSTERED INDEX [IX_InferenceSteps_InferenceId_StepNumber]
    ON [dbo].[InferenceSteps]([InferenceId] ASC, [StepNumber] ASC);
GO