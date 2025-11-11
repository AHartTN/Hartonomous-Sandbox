CREATE TABLE [dbo].[Models] (
    [ModelId]            INT            NOT NULL IDENTITY,
    [ModelName]          NVARCHAR (200) NOT NULL,
    [ModelType]          NVARCHAR (100) NOT NULL,
    [Architecture]       NVARCHAR (100) NULL,
    [Config]             NVARCHAR(MAX)  NULL,
    [ParameterCount]     BIGINT         NULL,
    [IngestionDate]      DATETIME2 (7)  NOT NULL DEFAULT (SYSUTCDATETIME()),
    [LastUsed]           DATETIME2 (7)  NULL,
    [UsageCount]         BIGINT         NOT NULL DEFAULT CAST(0 AS BIGINT),
    [AverageInferenceMs] FLOAT (53)     NULL,
    CONSTRAINT [PK_Models] PRIMARY KEY CLUSTERED ([ModelId] ASC)
);
