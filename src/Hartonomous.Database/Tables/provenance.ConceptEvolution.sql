CREATE TABLE [provenance].[ConceptEvolution]
(
    [EvolutionId] BIGINT IDENTITY(1,1) NOT NULL,
    [ConceptId] UNIQUEIDENTIFIER NOT NULL,
    [PreviousCentroid] VARBINARY(MAX) NULL,
    [NewCentroid] VARBINARY(MAX) NOT NULL,
    [CentroidShift] FLOAT NULL,
    [AtomCountDelta] INT NULL,
    [CoherenceDelta] FLOAT NULL,
    [EvolutionType] NVARCHAR(50) NOT NULL,
    [UpdatedUtc] DATETIME2 NOT NULL DEFAULT (SYSUTCDATETIME()),
    [TenantId] INT NOT NULL DEFAULT (0),
    
    CONSTRAINT [PK_ConceptEvolution] PRIMARY KEY CLUSTERED ([EvolutionId]),
    CONSTRAINT [FK_ConceptEvolution_Concepts] FOREIGN KEY ([ConceptId])
        REFERENCES [provenance].[Concepts]([ConceptId]) ON DELETE CASCADE,
    CONSTRAINT [CK_ConceptEvolution_Type] CHECK ([EvolutionType] IN ('Discovered', 'Updated', 'Split', 'Merged', 'Archived'))
);
GO

CREATE NONCLUSTERED INDEX [IX_ConceptEvolution_ConceptId_UpdatedUtc]
    ON [provenance].[ConceptEvolution]([ConceptId], [UpdatedUtc] DESC);
GO

CREATE NONCLUSTERED INDEX [IX_ConceptEvolution_Type]
    ON [provenance].[ConceptEvolution]([EvolutionType])
    INCLUDE ([ConceptId], [UpdatedUtc]);
GO

CREATE NONCLUSTERED INDEX [IX_ConceptEvolution_CentroidShift]
    ON [provenance].[ConceptEvolution]([CentroidShift] DESC)
    WHERE [CentroidShift] IS NOT NULL;
GO
GO