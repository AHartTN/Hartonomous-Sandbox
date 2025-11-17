CREATE TABLE [dbo].[CachedActivations_InMemory]
(
    [CacheId]            BIGINT         IDENTITY (1, 1) NOT NULL,
    [ModelId]            INT            NOT NULL,
    [LayerId]            BIGINT         NOT NULL,
    [InputHash]          BINARY (32)    NOT NULL,
    [ActivationOutput]   VARBINARY(MAX) NULL,
    [OutputShape]        NVARCHAR (100) COLLATE Latin1_General_100_BIN2 NULL,
    [HitCount]           BIGINT         NOT NULL DEFAULT (0),
    [CreatedDate]        DATETIME2 (7)  NOT NULL DEFAULT (SYSUTCDATETIME()),
    [LastAccessed]       DATETIME2 (7)  NOT NULL DEFAULT (SYSUTCDATETIME()),
    [ComputeTimeSavedMs] BIGINT         NOT NULL DEFAULT (0),
    
    CONSTRAINT [PK_CachedActivations_InMemory] PRIMARY KEY NONCLUSTERED ([CacheId]),
    INDEX [IX_LayerInput_Hash] HASH ([LayerId], [InputHash]) WITH (BUCKET_COUNT = 1000000),
    INDEX [IX_ModelId_Hash] HASH ([ModelId]) WITH (BUCKET_COUNT = 1000),
    INDEX [IX_LastAccessed_Range] NONCLUSTERED ([LastAccessed] ASC)
)
WITH (MEMORY_OPTIMIZED = ON, DURABILITY = SCHEMA_AND_DATA);
