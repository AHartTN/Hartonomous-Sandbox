CREATE TABLE [dbo].[InferenceCache] (
    [CacheId]            BIGINT          NOT NULL IDENTITY,
    [CacheKey]           NVARCHAR (64)   NOT NULL,
    [ModelId]            INT             NOT NULL,
    [InferenceType]      NVARCHAR (100)  NOT NULL,
    [InputHash]          VARBINARY (MAX) NOT NULL,
    [OutputData]         VARBINARY (MAX) NOT NULL,
    [IntermediateStates] VARBINARY (MAX) NULL,
    [CreatedUtc]         DATETIME2 (7)   NOT NULL DEFAULT (SYSUTCDATETIME()),
    [LastAccessedUtc]    DATETIME2 (7)   NULL,
    [AccessCount]        BIGINT          NOT NULL DEFAULT CAST(0 AS BIGINT),
    [SizeBytes]          BIGINT          NULL,
    [ComputeTimeMs]      FLOAT (53)      NULL,
    CONSTRAINT [PK_InferenceCache] PRIMARY KEY CLUSTERED ([CacheId] ASC),
    CONSTRAINT [FK_InferenceCache_Models_ModelId] FOREIGN KEY ([ModelId]) REFERENCES [dbo].[Model] ([ModelId]) ON DELETE CASCADE
);
