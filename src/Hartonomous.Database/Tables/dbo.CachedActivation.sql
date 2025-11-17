CREATE TABLE [dbo].[CachedActivation] (
    [CacheId]            BIGINT         NOT NULL IDENTITY,
    [ModelId]            INT            NOT NULL,
    [LayerId]            BIGINT         NOT NULL,
    [InputHash]          BINARY (32)    NOT NULL,
    [ActivationOutput]   VARBINARY(MAX) NULL,
    [OutputShape]        NVARCHAR (100) NULL,
    [HitCount]           BIGINT         NOT NULL DEFAULT CAST(0 AS BIGINT),
    [CreatedDate]        DATETIME2 (7)  NOT NULL DEFAULT (SYSUTCDATETIME()),
    [LastAccessed]       DATETIME2 (7)  NOT NULL DEFAULT (SYSUTCDATETIME()),
    [ComputeTimeSavedMs] BIGINT         NOT NULL DEFAULT CAST(0 AS BIGINT),
    CONSTRAINT [PK_CachedActivations] PRIMARY KEY CLUSTERED ([CacheId] ASC),
    CONSTRAINT [FK_CachedActivations_ModelLayers_LayerId] FOREIGN KEY ([LayerId]) REFERENCES [dbo].[ModelLayer] ([LayerId]) ON DELETE CASCADE,
    CONSTRAINT [FK_CachedActivations_Models_ModelId] FOREIGN KEY ([ModelId]) REFERENCES [dbo].[Model] ([ModelId])
);
