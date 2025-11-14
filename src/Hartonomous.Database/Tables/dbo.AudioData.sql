CREATE TABLE [dbo].[AudioData] (
    [AudioId]            BIGINT         NOT NULL IDENTITY,
    [SourcePath]         NVARCHAR (500) NULL,
    [SampleRate]         INT            NOT NULL,
    [DurationMs]         BIGINT         NOT NULL,
    [NumChannels]        TINYINT        NOT NULL,
    [Format]             NVARCHAR (20)  NULL,
    [Spectrogram]        GEOMETRY       NULL,
    [MelSpectrogram]     GEOMETRY       NULL,
    [WaveformLeft]       GEOMETRY       NULL,
    [WaveformRight]      GEOMETRY       NULL,
    [GlobalEmbedding]    VECTOR(1998)   NULL,
    [Metadata]           NVARCHAR(MAX)  NULL,
    [IngestionDate]      DATETIME2 (7)  NULL DEFAULT (SYSUTCDATETIME()),
    CONSTRAINT [PK_AudioData] PRIMARY KEY CLUSTERED ([AudioId] ASC)
);
