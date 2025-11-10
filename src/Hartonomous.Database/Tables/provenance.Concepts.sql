CREATE TABLE [provenance].[Concepts]
(
    [ConceptId] UNIQUEIDENTIFIER NOT NULL DEFAULT (NEWID()),
    [ConceptName] NVARCHAR(200) NULL,
    [Centroid] VARBINARY(MAX) NOT NULL,
    [AtomCount] INT NOT NULL DEFAULT (0),
    [Coherence] FLOAT NOT NULL,
    [SpatialBucket] INT NULL,
    [DiscoveredUtc] DATETIME2 NOT NULL DEFAULT (SYSUTCDATETIME()),
    [LastUpdatedUtc] DATETIME2 NOT NULL DEFAULT (SYSUTCDATETIME()),
    [TenantId] INT NOT NULL DEFAULT (0),
    [IsActive] BIT NOT NULL DEFAULT (1),
    
    CONSTRAINT [PK_Concepts] PRIMARY KEY CLUSTERED ([ConceptId])
);
GO

CREATE NONCLUSTERED INDEX [IX_Concepts_TenantId_IsActive]
    ON [provenance].[Concepts]([TenantId], [IsActive])
    INCLUDE ([ConceptId], [Coherence]);
GO

CREATE NONCLUSTERED INDEX [IX_Concepts_Coherence]
    ON [provenance].[Concepts]([Coherence] DESC)
    WHERE [IsActive] = 1;
GO

CREATE NONCLUSTERED INDEX [IX_Concepts_SpatialBucket]
    ON [provenance].[Concepts]([SpatialBucket])
    WHERE [IsActive] = 1 AND [SpatialBucket] IS NOT NULL;
GO
GO