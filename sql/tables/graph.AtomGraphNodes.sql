-- SQL Graph node table for Echelon 2 provenance (hot graph sync)
-- Enables MATCH queries: FROM AtomGraphNodes AS src, AtomGraphEdges, AtomGraphNodes AS dest
-- WHERE MATCH(src-(AtomGraphEdges)->dest)

IF NOT EXISTS (SELECT 1 FROM sys.schemas WHERE name = 'graph')
BEGIN
    EXEC('CREATE SCHEMA graph');
END
GO

-- Drop existing table if it exists (graph tables must be dropped before recreation)
IF OBJECT_ID('graph.AtomGraphNodes', 'U') IS NOT NULL
BEGIN
    DROP TABLE graph.AtomGraphNodes;
END
GO

CREATE TABLE graph.AtomGraphNodes (
    NodeId BIGINT IDENTITY(1,1) PRIMARY KEY,
    AtomId BIGINT NOT NULL, -- FK to dbo.Atoms
    NodeType NVARCHAR(50) NOT NULL, -- 'Atom', 'Model', 'Concept', etc.
    Metadata NVARCHAR(MAX), -- JSON metadata for graph analytics
    
    -- Spatial properties for graph embeddings
    EmbeddingX FLOAT NULL,
    EmbeddingY FLOAT NULL,
    EmbeddingZ FLOAT NULL,
    
    -- Provenance tracking
    CreatedUtc DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
    
    -- Graph node markers
    INDEX IX_AtomGraphNodes_AtomId (AtomId),
    INDEX IX_AtomGraphNodes_NodeType (NodeType)
) AS NODE;
GO

-- Add CHECK constraint for valid NodeType
ALTER TABLE graph.AtomGraphNodes
ADD CONSTRAINT CK_AtomGraphNodes_NodeType 
CHECK (NodeType IN ('Atom', 'Model', 'Concept', 'Component', 'Embedding'));
GO
