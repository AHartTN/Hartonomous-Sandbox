CREATE TABLE [dbo].[InferenceSteps] (
    [StepId]       BIGINT         NOT NULL IDENTITY,
    [InferenceId]  BIGINT         NOT NULL,
    [StepNumber]   INT            NOT NULL,
    [ModelId]      INT            NULL,
    [LayerId]      BIGINT         NULL,
    [OperationType]NVARCHAR (50)  NULL,
    [QueryText]    NVARCHAR (MAX) NULL,
    [IndexUsed]    NVARCHAR (200) NULL,
    [RowsExamined] BIGINT         NULL,
    [RowsReturned] BIGINT         NULL,
    [DurationMs]   INT            NULL,
    [CacheUsed]    BIT            NOT NULL DEFAULT CAST(0 AS BIT),
    CONSTRAINT [PK_InferenceSteps] PRIMARY KEY CLUSTERED ([StepId] ASC),
    CONSTRAINT [FK_InferenceSteps_InferenceRequests_InferenceId] FOREIGN KEY ([InferenceId]) REFERENCES [dbo].[InferenceRequests] ([InferenceId]) ON DELETE CASCADE,
    CONSTRAINT [FK_InferenceSteps_Models_ModelId] FOREIGN KEY ([ModelId]) REFERENCES [dbo].[Models] ([ModelId]) ON DELETE SET NULL
);
