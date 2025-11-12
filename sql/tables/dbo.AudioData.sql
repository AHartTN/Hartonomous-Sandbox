-- =============================================
-- Table: dbo.AudioData
-- =============================================
-- Represents audio data with spectral and waveform geometric representations.
-- This table was previously managed by EF Core.
-- =============================================

IF OBJECT_ID('dbo.AudioData', 'U') IS NOT NULL
    DROP TABLE dbo.AudioData;
GO

CREATE TABLE dbo.AudioData
(
    AudioId             BIGINT          IDENTITY(1,1) NOT NULL PRIMARY KEY,
    SourcePath          NVARCHAR(500)   NULL,
    SampleRate          INT             NOT NULL,
    DurationMs          BIGINT          NOT NULL,
    NumChannels         TINYINT         NOT NULL,
    Format              NVARCHAR(20)    NULL,
    Spectrogram         GEOMETRY        NULL,
    MelSpectrogram      GEOMETRY        NULL,
    WaveformLeft        GEOMETRY        NULL,
    WaveformRight       GEOMETRY        NULL,
    GlobalEmbedding     VECTOR(768)     NULL,
    GlobalEmbeddingDim  INT             NULL,
    Metadata            NVARCHAR(MAX)   NULL,
    IngestionDate       DATETIME2       NULL DEFAULT SYSUTCDATETIME(),

    CONSTRAINT CK_AudioData_Metadata_IsJson CHECK (Metadata IS NULL OR ISJSON(Metadata) = 1)
);
GO

CREATE INDEX IX_AudioData_DurationMs ON dbo.AudioData(DurationMs);
GO

CREATE INDEX IX_AudioData_IngestionDate ON dbo.AudioData(IngestionDate DESC);
GO

PRINT 'Created table dbo.AudioData';
GO
