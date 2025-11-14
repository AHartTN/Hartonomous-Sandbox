CREATE TABLE [dbo].[IngestionJobs] (
    [IngestionJobId] BIGINT          NOT NULL IDENTITY,
    [PipelineName]   NVARCHAR (256)  NOT NULL,
    [StartedAt]      DATETIME2 (7)   NOT NULL DEFAULT (SYSUTCDATETIME()),
    [CompletedAt]    DATETIME2 (7)   NULL,
    [Status]         NVARCHAR (64)   NULL,
    [SourceUri]      NVARCHAR (1024) NULL,
    [Metadata]       JSON   NULL,
    CONSTRAINT [PK_IngestionJobs] PRIMARY KEY CLUSTERED ([IngestionJobId] ASC)
);
