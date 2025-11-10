-- =============================================
-- Table: dbo.AtomicAudioSamples
-- Description: Unique atomic audio samples with content-addressable deduplication.
--              Stores normalized and raw amplitude values for audio processing.
-- =============================================
CREATE TABLE [dbo].[AtomicAudioSamples]
(
    [SampleHash]           BINARY(32)       NOT NULL,
    [AmplitudeNormalized]  REAL             NOT NULL,
    [AmplitudeInt16]       SMALLINT         NOT NULL,
    [ReferenceCount]       BIGINT           NOT NULL DEFAULT (0),
    [FirstSeen]            DATETIME2(7)     NOT NULL DEFAULT (SYSUTCDATETIME()),
    [LastReferenced]       DATETIME2(7)     NOT NULL DEFAULT (SYSUTCDATETIME()),

    CONSTRAINT [PK_AtomicAudioSamples] PRIMARY KEY CLUSTERED ([SampleHash] ASC)
);
GO

CREATE NONCLUSTERED INDEX [IX_AtomicAudioSamples_AmplitudeNormalized]
    ON [dbo].[AtomicAudioSamples]([AmplitudeNormalized] ASC);
GO