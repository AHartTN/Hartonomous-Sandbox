-- SQL Graph edge table for Echelon 2 provenance (hot graph sync)
-- Connects AtomGraphNodes with typed relationships
-- Enables bi-directional graph traversal: src->dest and src<-dest

CREATE TABLE [graph].[AtomGraphEdges]
(
    [EdgeId] BIGINT IDENTITY(1,1) NOT NULL,
    [EdgeType] NVARCHAR(50) NOT NULL,
    [Weight] FLOAT NOT NULL DEFAULT (1.0),
    [Metadata] NVARCHAR(MAX) NULL,
    [CreatedUtc] DATETIME2 NOT NULL DEFAULT (SYSUTCDATETIME()),
    [ValidFrom] DATETIME2 NULL,
    [ValidTo] DATETIME2 NULL,
    
    CONSTRAINT [PK_AtomGraphEdges] PRIMARY KEY CLUSTERED ([EdgeId]),
    CONSTRAINT [EC_AtomGraphEdges] 
        CONNECTION ([graph].[AtomGraphNodes] TO [graph].[AtomGraphNodes]),
    CONSTRAINT [CK_AtomGraphEdges_EdgeType]
        CHECK ([EdgeType] IN (
            'DerivedFrom', 'ComponentOf', 'SimilarTo', 'Uses',
            'InputTo', 'OutputFrom', 'BindsToConcept'
        )),
    CONSTRAINT [CK_AtomGraphEdges_Weight]
        CHECK ([Weight] >= 0.0 AND [Weight] <= 1.0)
) AS EDGE;
GO

CREATE NONCLUSTERED INDEX [IX_AtomGraphEdges_EdgeType]
    ON [graph].[AtomGraphEdges]([EdgeType]);
GO

CREATE NONCLUSTERED INDEX [IX_AtomGraphEdges_Weight]
    ON [graph].[AtomGraphEdges]([Weight]);
GO