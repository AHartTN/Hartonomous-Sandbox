-- =============================================
-- Table: dbo.AtomicAudioSamples
-- =============================================
-- Represents unique atomic audio samples with content-addressable deduplication.
-- This table was previously managed by EF Core.
-- =============================================

IF OBJECT_ID('dbo.AtomicAudioSamples', 'U') IS NOT NULL
    DROP TABLE dbo.AtomicAudioSamples;
GO

CREATE TABLE dbo.AtomicAudioSamples
(
    SampleHash          BINARY(32)      NOT NULL PRIMARY KEY,
    AmplitudeNormalized REAL            NOT NULL,
    AmplitudeInt16      SMALLINT        NOT NULL,
    ReferenceCount      BIGINT          NOT NULL DEFAULT 0,
    FirstSeen           DATETIME2       NOT NULL DEFAULT SYSUTCDATETIME(),
    LastReferenced      DATETIME2       NOT NULL DEFAULT SYSUTCDATETIME()
);
GO

CREATE INDEX IX_AtomicAudioSamples_AmplitudeNormalized ON dbo.AtomicAudioSamples(AmplitudeNormalized);
GO

PRINT 'Created table dbo.AtomicAudioSamples';
GO
