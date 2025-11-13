CREATE TABLE [provenance].[AtomConcepts] (
    [AtomConceptId]  BIGINT        NOT NULL IDENTITY,
    [AtomId]         BIGINT        NOT NULL,
    [ConceptId]      BIGINT        NOT NULL,
    [Similarity]     FLOAT (53)    NULL,     -- Similarity score between atom and concept
    [IsPrimary]      BIT           NOT NULL DEFAULT (0), -- Whether this is the primary concept for this atom
    [MembershipScore] FLOAT (53)   NOT NULL, -- How strongly this atom belongs to this concept (0.0 to 1.0)
    [DistanceToCentroid] FLOAT (53) NULL,    -- Distance from atom embedding to concept centroid
    [AssignedAt]     DATETIME2 (7) NOT NULL DEFAULT (SYSUTCDATETIME()),
    [TenantId]       INT           NOT NULL DEFAULT (0),
    CONSTRAINT [PK_AtomConcepts] PRIMARY KEY CLUSTERED ([AtomConceptId] ASC),
    CONSTRAINT [FK_AtomConcepts_Atoms] FOREIGN KEY ([AtomId]) REFERENCES [dbo].[Atoms] ([AtomId]) ON DELETE CASCADE,
    CONSTRAINT [FK_AtomConcepts_Concepts] FOREIGN KEY ([ConceptId]) REFERENCES [provenance].[Concepts] ([ConceptId]) ON DELETE CASCADE
);
GO

CREATE UNIQUE NONCLUSTERED INDEX [UX_AtomConcepts_AtomId_ConceptId] 
    ON [provenance].[AtomConcepts] ([AtomId], [ConceptId]);
GO

CREATE NONCLUSTERED INDEX [IX_AtomConcepts_ConceptId] 
    ON [provenance].[AtomConcepts] ([ConceptId]) INCLUDE ([AtomId], [MembershipScore]);
GO
