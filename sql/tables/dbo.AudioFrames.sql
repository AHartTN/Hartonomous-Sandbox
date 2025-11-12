-- =============================================
-- Table: dbo.AudioFrames
-- =============================================
-- Represents frame-by-frame temporal audio data with spectral features.
-- This table was previously managed by EF Core.
-- =============================================

IF OBJECT_ID('dbo.AudioFrames', 'U') IS NOT NULL
    DROP TABLE dbo.AudioFrames;
GO

CREATE TABLE dbo.AudioFrames
(
    AudioId             BIGINT          NOT NULL,
    FrameNumber         BIGINT          NOT NULL,
    TimestampMs         BIGINT          NOT NULL,
    AmplitudeL          REAL            NULL,
    AmplitudeR          REAL            NULL,
    SpectralCentroid    REAL            NULL,
    SpectralRolloff     REAL            NULL,
    ZeroCrossingRate    REAL            NULL,
    RmsEnergy           REAL            NULL,
    Mfcc                VECTOR(13)      NULL,
    FrameEmbedding      VECTOR(768)     NULL,

    CONSTRAINT PK_AudioFrames PRIMARY KEY CLUSTERED (AudioId, FrameNumber),
    CONSTRAINT FK_AudioFrames_AudioData FOREIGN KEY (AudioId) REFERENCES dbo.AudioData(AudioId) ON DELETE CASCADE
);
GO

PRINT 'Created table dbo.AudioFrames';
GO
