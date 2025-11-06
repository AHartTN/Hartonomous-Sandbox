-- SQL Graph edge table for Echelon 2 provenance (hot graph sync)
-- Connects AtomGraphNodes with typed relationships
-- Enables bi-directional graph traversal: src->dest and src<-dest

IF OBJECT_ID('graph.AtomGraphEdges', 'U') IS NOT NULL
BEGIN
    DROP TABLE graph.AtomGraphEdges;
END
GO

CREATE TABLE graph.AtomGraphEdges (
    EdgeId BIGINT IDENTITY(1,1) PRIMARY KEY,
    EdgeType NVARCHAR(50) NOT NULL, -- 'DerivedFrom', 'ComponentOf', 'SimilarTo', 'Uses', etc.
    Weight FLOAT NOT NULL DEFAULT 1.0, -- Edge weight for graph algorithms (PageRank, shortest path)
    
    -- Provenance metadata
    Metadata NVARCHAR(MAX), -- JSON: {"confidence": 0.95, "method": "cosine_similarity", "timestamp": "..."}
    CreatedUtc DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
    
    -- Optional temporal validity
    ValidFrom DATETIME2 NULL,
    ValidTo DATETIME2 NULL,
    
    INDEX IX_AtomGraphEdges_EdgeType (EdgeType),
    INDEX IX_AtomGraphEdges_Weight (Weight)
) AS EDGE;
GO

-- Add connection constraint (AtomGraphNodes to AtomGraphNodes)
ALTER TABLE graph.AtomGraphEdges
ADD CONSTRAINT EC_AtomGraphEdges 
CONNECTION (graph.AtomGraphNodes TO graph.AtomGraphNodes);
GO

-- Add CHECK constraint for valid EdgeType
ALTER TABLE graph.AtomGraphEdges
ADD CONSTRAINT CK_AtomGraphEdges_EdgeType
CHECK (EdgeType IN (
    'DerivedFrom',      -- Atom was generated from another atom
    'ComponentOf',      -- Atom is component of composite (Event/Generation)
    'SimilarTo',        -- Semantic similarity edge
    'Uses',             -- Model uses another model (ensemble)
    'InputTo',          -- Atom was input to inference
    'OutputFrom',       -- Atom was output from inference
    'BindsToConcept'    -- Atom binds to discovered concept (unsupervised learning)
));
GO

-- Add CHECK constraint for weight range
ALTER TABLE graph.AtomGraphEdges
ADD CONSTRAINT CK_AtomGraphEdges_Weight
CHECK (Weight >= 0.0 AND Weight <= 1.0);
GO
