CREATE TABLE [dbo].[AtomicAudioSamples] (
    [SampleHash]          BINARY (32)   NOT NULL,
    [AmplitudeInt16]      SMALLINT      NOT NULL,
    [AmplitudeNormalized] REAL          NOT NULL,
    [SampleBytes]         VARBINARY(2)  NOT NULL,  -- Raw 2-byte int16 for reconstruction
    
    -- Spatial indexing: amplitude as coordinate for range queries
    [AmplitudePoint]      GEOMETRY      NULL,  -- POINT(amplitude, 0, 0, 0)
    
    -- Statistics
    [ReferenceCount]      BIGINT        NOT NULL DEFAULT CAST(0 AS BIGINT),
    [FirstSeen]           DATETIME2 (7) NOT NULL DEFAULT (SYSUTCDATETIME()),
    [LastReferenced]      DATETIME2 (7) NOT NULL DEFAULT (SYSUTCDATETIME()),
    
    CONSTRAINT [PK_AtomicAudioSamples] PRIMARY KEY CLUSTERED ([SampleHash] ASC),
    INDEX [IX_AtomicAudioSamples_Amplitude] NONCLUSTERED ([AmplitudeInt16]),
    INDEX [IX_AtomicAudioSamples_References] NONCLUSTERED ([ReferenceCount] DESC)
);
GO

-- Spatial index for amplitude-based queries (find spikes, silence, etc.)
CREATE SPATIAL INDEX [SIDX_AtomicAudioSamples_Amplitude]
ON [dbo].[AtomicAudioSamples] ([AmplitudePoint])
USING GEOMETRY_GRID
WITH (
    BOUNDING_BOX = (-32768, 0, 32767, 0),  -- int16 range
    GRIDS = (LEVEL_1 = MEDIUM, LEVEL_2 = MEDIUM, LEVEL_3 = MEDIUM, LEVEL_4 = MEDIUM),
    CELLS_PER_OBJECT = 8
);
GO
