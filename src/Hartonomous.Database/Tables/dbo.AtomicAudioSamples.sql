CREATE TABLE [dbo].[AtomicAudioSamples] (
    [SampleHash]          BINARY (32)   NOT NULL,
    [AmplitudeNormalized] REAL          NOT NULL,
    [AmplitudeInt16]      SMALLINT      NOT NULL,
    [ReferenceCount]      BIGINT        NOT NULL DEFAULT CAST(0 AS BIGINT),
    [FirstSeen]           DATETIME2 (7) NOT NULL DEFAULT (SYSUTCDATETIME()),
    [LastReferenced]      DATETIME2 (7) NOT NULL DEFAULT (SYSUTCDATETIME()),
    CONSTRAINT [PK_AtomicAudioSamples] PRIMARY KEY CLUSTERED ([SampleHash] ASC)
);
