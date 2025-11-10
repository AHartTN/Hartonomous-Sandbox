-- High-performance inference cache for frequent operations
-- Uses clustered columnstore for fast scans, nonclustered indexes for point lookups
-- Stores recent inference results to avoid re-computation
-- NOTE: Cannot use MEMORY_OPTIMIZED in CDC-enabled database due to DDL trigger conflict

CREATE TABLE dbo.InferenceCache (
    CacheId BIGINT IDENTITY(1,1) NOT NULL,
    CacheKey BINARY(32) NOT NULL UNIQUE, -- SHA-256 hash for deduplication
    
    -- Cached result
    ResultAtomId BIGINT NOT NULL,
    ResultPayload VARBINARY(8000), -- Inline small payloads (< 8KB)
    
    -- Cache metadata
    ModelId BIGINT NOT NULL,
    Modality NVARCHAR(50) NOT NULL,
    Confidence FLOAT NOT NULL,
    
    -- TTL and eviction
    CreatedUtc DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
    ExpiresUtc DATETIME2 NOT NULL,
    AccessCount INT NOT NULL DEFAULT 1,
    LastAccessUtc DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
    
    PRIMARY KEY CLUSTERED (CacheId),
    INDEX IX_InferenceCache_CacheKey NONCLUSTERED (CacheKey),
    INDEX IX_InferenceCache_ModelId NONCLUSTERED (ModelId),
    INDEX IX_InferenceCache_ExpiresUtc NONCLUSTERED (ExpiresUtc)
);
GO

-- CHECK constraint for positive confidence
ALTER TABLE dbo.InferenceCache
ADD CONSTRAINT CK_InferenceCache_Confidence
CHECK (Confidence >= 0.0 AND Confidence <= 1.0);
GO

-- CHECK constraint for valid TTL
ALTER TABLE dbo.InferenceCache
ADD CONSTRAINT CK_InferenceCache_ExpiresUtc
CHECK (ExpiresUtc > CreatedUtc);
GO
