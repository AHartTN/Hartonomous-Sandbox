CREATE TABLE [provenance].[AtomConcepts]
(
    [AtomConceptId] BIGINT IDENTITY(1,1) NOT NULL,
    [AtomId] BIGINT NOT NULL,
    [ConceptId] UNIQUEIDENTIFIER NOT NULL,
    [Similarity] FLOAT NOT NULL,
    [IsPrimary] BIT NOT NULL DEFAULT (0),
    [BindingUtc] DATETIME2 NOT NULL DEFAULT (SYSUTCDATETIME()),
    [TenantId] INT NOT NULL DEFAULT (0),
    
    CONSTRAINT [PK_AtomConcepts] PRIMARY KEY CLUSTERED ([AtomConceptId]),
    CONSTRAINT [FK_AtomConcepts_Atoms] FOREIGN KEY ([AtomId])
        REFERENCES [dbo].[Atoms]([AtomId]) ON DELETE CASCADE,
    CONSTRAINT [FK_AtomConcepts_Concepts] FOREIGN KEY ([ConceptId])
        REFERENCES [provenance].[Concepts]([ConceptId]) ON DELETE CASCADE,
    CONSTRAINT [UQ_AtomConcepts_Atom_Concept] UNIQUE NONCLUSTERED ([AtomId], [ConceptId]),
    CONSTRAINT [CK_AtomConcepts_Similarity] CHECK ([Similarity] >= 0.0 AND [Similarity] <= 1.0)
);
GO

CREATE NONCLUSTERED INDEX [IX_AtomConcepts_AtomId]
    ON [provenance].[AtomConcepts]([AtomId])
    INCLUDE ([ConceptId], [Similarity], [IsPrimary]);
GO

CREATE NONCLUSTERED INDEX [IX_AtomConcepts_ConceptId]
    ON [provenance].[AtomConcepts]([ConceptId])
    INCLUDE ([AtomId], [Similarity], [IsPrimary]);
GO

CREATE NONCLUSTERED INDEX [IX_AtomConcepts_Similarity]
    ON [provenance].[AtomConcepts]([Similarity] DESC)
    INCLUDE ([AtomId], [ConceptId]);
GO

CREATE NONCLUSTERED INDEX [IX_AtomConcepts_TenantId]
    ON [provenance].[AtomConcepts]([TenantId])
    INCLUDE ([AtomId], [ConceptId]);
GO
GO