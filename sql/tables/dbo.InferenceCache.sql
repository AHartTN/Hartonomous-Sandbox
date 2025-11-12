-- =============================================
-- Table: dbo.InferenceCache
-- =============================================
-- Caches inference results to avoid redundant computations.
-- This table was previously managed by EF Core.
-- =============================================

IF OBJECT_ID('dbo.InferenceCache', 'U') IS NOT NULL
    DROP TABLE dbo.InferenceCache;
GO

CREATE TABLE dbo.InferenceCache
(
    CacheId             BIGINT          IDENTITY(1,1) NOT NULL PRIMARY KEY,
    CacheKey            NVARCHAR(64)    NOT NULL,
    ModelId             INT             NOT NULL,
    InferenceType       NVARCHAR(100)   NOT NULL,
    InputHash           VARBINARY(MAX)  NOT NULL,
    OutputData          VARBINARY(MAX)  NOT NULL,
    IntermediateStates  VARBINARY(MAX)  NULL,
    CreatedUtc          DATETIME2       NOT NULL DEFAULT SYSUTCDATETIME(),
    LastAccessedUtc     DATETIME2       NULL,
    AccessCount         BIGINT          NOT NULL DEFAULT 0,
    SizeBytes           BIGINT          NULL,
    ComputeTimeMs       REAL            NULL,

    CONSTRAINT FK_InferenceCache_Models FOREIGN KEY (ModelId) REFERENCES dbo.Models(ModelId) ON DELETE CASCADE
);
GO

CREATE INDEX IX_InferenceCache_CacheKey ON dbo.InferenceCache(CacheKey);
GO

CREATE INDEX IX_InferenceCache_ModelId_InferenceType ON dbo.InferenceCache(ModelId, InferenceType);
GO

CREATE INDEX IX_InferenceCache_LastAccessedUtc ON dbo.InferenceCache(LastAccessedUtc DESC);
GO

PRINT 'Created table dbo.InferenceCache';
GO