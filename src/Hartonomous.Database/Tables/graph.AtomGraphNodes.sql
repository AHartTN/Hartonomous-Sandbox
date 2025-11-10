-- SQL Graph node table for Echelon 2 provenance (hot graph sync)
-- Enables MATCH queries: FROM AtomGraphNodes AS src, AtomGraphEdges, AtomGraphNodes AS dest
-- WHERE MATCH(src-(AtomGraphEdges)->dest)

CREATE TABLE [graph].[AtomGraphNodes]
(
    [NodeId] BIGINT IDENTITY(1,1) NOT NULL,
    [AtomId] BIGINT NOT NULL,
    [NodeType] NVARCHAR(50) NOT NULL,
    [Metadata] NVARCHAR(MAX) NULL,
    [EmbeddingX] FLOAT NULL,
    [EmbeddingY] FLOAT NULL,
    [EmbeddingZ] FLOAT NULL,
    [CreatedUtc] DATETIME2 NOT NULL DEFAULT (SYSUTCDATETIME()),
    
    CONSTRAINT [PK_AtomGraphNodes] PRIMARY KEY CLUSTERED ([NodeId]),
    CONSTRAINT [CK_AtomGraphNodes_NodeType] 
        CHECK ([NodeType] IN ('Atom', 'Model', 'Concept', 'Component', 'Embedding'))
) AS NODE;
GO

CREATE NONCLUSTERED INDEX [IX_AtomGraphNodes_AtomId]
    ON [graph].[AtomGraphNodes]([AtomId]);
GO

CREATE NONCLUSTERED INDEX [IX_AtomGraphNodes_NodeType]
    ON [graph].[AtomGraphNodes]([NodeType]);
GO