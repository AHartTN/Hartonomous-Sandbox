-- =============================================
-- Table: dbo.CachedActivations
-- =============================================
-- Represents a cached layer activation to speed up inference.
-- This table was previously managed by EF Core.
-- =============================================

IF OBJECT_ID('dbo.CachedActivations', 'U') IS NOT NULL
    DROP TABLE dbo.CachedActivations;
GO

CREATE TABLE dbo.CachedActivations
(
    CacheId             BIGINT          IDENTITY(1,1) NOT NULL PRIMARY KEY,
    ModelId             INT             NOT NULL,
    LayerId             BIGINT          NOT NULL,
    InputHash           BINARY(32)      NOT NULL,
    ActivationOutput    VECTOR(1998)    NULL,
    OutputShape         NVARCHAR(100)   NULL,
    HitCount            BIGINT          NOT NULL DEFAULT 0,
    CreatedDate         DATETIME2       NOT NULL DEFAULT SYSUTCDATETIME(),
    LastAccessed        DATETIME2       NOT NULL DEFAULT SYSUTCDATETIME(),
    ComputeTimeSavedMs  BIGINT          NOT NULL DEFAULT 0,

    CONSTRAINT FK_CachedActivations_Models FOREIGN KEY (ModelId) REFERENCES dbo.Models(ModelId) ON DELETE NO ACTION,
    CONSTRAINT FK_CachedActivations_ModelLayers FOREIGN KEY (LayerId) REFERENCES dbo.ModelLayers(LayerId) ON DELETE CASCADE
);
GO

CREATE UNIQUE INDEX IX_CachedActivations_Model_Layer_InputHash ON dbo.CachedActivations(ModelId, LayerId, InputHash);
GO

CREATE INDEX IX_CachedActivations_LastAccessed_HitCount ON dbo.CachedActivations(LastAccessed DESC, HitCount DESC);
GO

PRINT 'Created table dbo.CachedActivations';
GO
