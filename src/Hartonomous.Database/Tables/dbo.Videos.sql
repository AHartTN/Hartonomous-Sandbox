CREATE TABLE [dbo].[Videos] (
    [VideoId]            BIGINT         NOT NULL IDENTITY,
    [SourcePath]         NVARCHAR (500) NULL,
    [Fps]                INT            NOT NULL,
    [DurationMs]         BIGINT         NOT NULL,
    [ResolutionWidth]    INT            NOT NULL,
    [ResolutionHeight]   INT            NOT NULL,
    [NumFrames]          BIGINT         NOT NULL,
    [Format]             NVARCHAR (20)  NULL,
    [GlobalEmbedding]    VECTOR(1998)   NULL,
    [Metadata]           JSON  NULL,
    [IngestionDate]      DATETIME2 (7)  NULL DEFAULT (SYSUTCDATETIME()),
    CONSTRAINT [PK_Videos] PRIMARY KEY CLUSTERED ([VideoId] ASC)
);
