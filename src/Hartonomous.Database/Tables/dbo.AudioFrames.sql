CREATE TABLE [dbo].[AudioFrames] (
    [AudioId]           BIGINT         NOT NULL,
    [FrameNumber]       BIGINT         NOT NULL,
    [TimestampMs]       BIGINT         NOT NULL,
    [AmplitudeL]        REAL           NULL,
    [AmplitudeR]        REAL           NULL,
    [SpectralCentroid]  REAL           NULL,
    [SpectralRolloff]   REAL           NULL,
    [ZeroCrossingRate]  REAL           NULL,
    [RmsEnergy]         REAL           NULL,
    [Mfcc]              VARBINARY(MAX) NULL,
    [FrameEmbedding]    VARBINARY(MAX) NULL,
    CONSTRAINT [PK_AudioFrames] PRIMARY KEY CLUSTERED ([AudioId] ASC, [FrameNumber] ASC),
    CONSTRAINT [FK_AudioFrames_AudioData_AudioId] FOREIGN KEY ([AudioId]) REFERENCES [dbo].[AudioData] ([AudioId]) ON DELETE CASCADE
);
