CREATE TABLE [provenance].[ConceptEvolution] (
    [EvolutionId]      BIGINT         NOT NULL IDENTITY,
    [ConceptId]        BIGINT         NOT NULL,
    [PreviousCentroid] VARBINARY (MAX) NOT NULL,
    [NewCentroid]      VARBINARY (MAX) NOT NULL,
    [CentroidShift]    FLOAT (53)     NOT NULL, -- Magnitude of centroid movement
    [AtomCountDelta]   INT            NOT NULL DEFAULT (0), -- Change in atom count
    [MemberCountChange] INT           NOT NULL, -- Change in number of member atoms
    [CoherenceDelta]   FLOAT (53)     NULL,     -- Change in coherence metric
    [CoherenceChange]  FLOAT (53)     NULL,     -- Change in cluster coherence
    [EvolutionType]    NVARCHAR (50)  NULL,     -- 'expansion', 'contraction', 'refinement', 'split', 'merge'
    [EvolutionReason]  NVARCHAR (200) NULL,     -- 'new_data', 'refinement', 'merge', 'split'
    [RecordedAt]       DATETIME2 (7)  NOT NULL DEFAULT (SYSUTCDATETIME()),
    [TenantId]         INT            NOT NULL DEFAULT (0),
    CONSTRAINT [PK_ConceptEvolution] PRIMARY KEY CLUSTERED ([EvolutionId] ASC),
    CONSTRAINT [FK_ConceptEvolution_Concepts] FOREIGN KEY ([ConceptId]) REFERENCES [provenance].[Concepts] ([ConceptId]) ON DELETE CASCADE
);
GO

CREATE NONCLUSTERED INDEX [IX_ConceptEvolution_ConceptId_RecordedAt] 
    ON [provenance].[ConceptEvolution] ([ConceptId], [RecordedAt] DESC);
GO
