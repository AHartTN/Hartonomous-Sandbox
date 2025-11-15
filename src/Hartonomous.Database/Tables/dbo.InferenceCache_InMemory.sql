CREATE TABLE [dbo].[InferenceCache_InMemory]
(
    [CacheId]            BIGINT          IDENTITY (1, 1) NOT NULL,
    [CacheKey]           NVARCHAR (64)   COLLATE Latin1_General_100_BIN2 NOT NULL,
    [ModelId]            INT             NOT NULL,
    [InferenceType]      NVARCHAR (100)  COLLATE Latin1_General_100_BIN2 NOT NULL,
    [InputHash]          BINARY (32)     NOT NULL,
    [OutputData]         VARBINARY (MAX) NOT NULL,
    [IntermediateStates] VARBINARY (MAX) NULL,
    [CreatedUtc]         DATETIME2 (7)   NOT NULL DEFAULT (SYSUTCDATETIME()),
    [LastAccessedUtc]    DATETIME2 (7)   NOT NULL DEFAULT (SYSUTCDATETIME()),
    [AccessCount]        BIGINT          NOT NULL DEFAULT (0),
    [SizeBytes]          BIGINT          NULL,
    [ComputeTimeMs]      FLOAT           NULL,
    
    CONSTRAINT [PK_InferenceCache_InMemory] PRIMARY KEY NONCLUSTERED ([CacheId]),
    INDEX [IX_CacheKey_Hash] HASH ([CacheKey]) WITH (BUCKET_COUNT = 2000000),
    INDEX [IX_ModelInput_Hash] HASH ([ModelId], [InputHash]) WITH (BUCKET_COUNT = 2000000),
    INDEX [IX_LastAccessed_Range] NONCLUSTERED ([LastAccessedUtc] ASC)
)
WITH (MEMORY_OPTIMIZED = ON, DURABILITY = SCHEMA_AND_DATA);
