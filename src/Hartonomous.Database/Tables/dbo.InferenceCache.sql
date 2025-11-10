-- High-performance inference cache for frequent operations
-- Uses clustered columnstore for fast scans, nonclustered indexes for point lookups
-- Stores recent inference results to avoid re-computation
-- NOTE: Cannot use MEMORY_OPTIMIZED in CDC-enabled database due to DDL trigger conflict

CREATE TABLE [dbo].[InferenceCache]
(
    [CacheId] BIGINT IDENTITY(1,1) NOT NULL,
    [CacheKey] BINARY(32) NOT NULL,
    [ResultAtomId] BIGINT NOT NULL,
    [ResultPayload] VARBINARY(8000) NULL,
    [ModelId] BIGINT NOT NULL,
    [Modality] NVARCHAR(50) NOT NULL,
    [Confidence] FLOAT NOT NULL,
    [CreatedUtc] DATETIME2 NOT NULL DEFAULT (SYSUTCDATETIME()),
    [ExpiresUtc] DATETIME2 NOT NULL,
    [AccessCount] INT NOT NULL DEFAULT (1),
    [LastAccessUtc] DATETIME2 NOT NULL DEFAULT (SYSUTCDATETIME()),
    
    CONSTRAINT [PK_InferenceCache] PRIMARY KEY CLUSTERED ([CacheId]),
    CONSTRAINT [UQ_InferenceCache_CacheKey] UNIQUE NONCLUSTERED ([CacheKey]),
    CONSTRAINT [CK_InferenceCache_Confidence]
        CHECK ([Confidence] >= 0.0 AND [Confidence] <= 1.0),
    CONSTRAINT [CK_InferenceCache_ExpiresUtc]
        CHECK ([ExpiresUtc] > [CreatedUtc])
);
GO

CREATE NONCLUSTERED INDEX [IX_InferenceCache_ModelId]
    ON [dbo].[InferenceCache]([ModelId]);
GO

CREATE NONCLUSTERED INDEX [IX_InferenceCache_ExpiresUtc]
    ON [dbo].[InferenceCache]([ExpiresUtc]);
GO