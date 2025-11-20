-- =============================================
-- Add Graph Pseudo-Column Indexes for AtomGraphEdges
-- MS Docs Best Practice: Index $node_id, $from_id, $to_id for 10-100x MATCH speedup
-- Reference: https://learn.microsoft.com/en-us/sql/relational-databases/graphs/sql-graph-architecture
-- =============================================

-- Index 1: $node_id pseudo-column (unique identifier for each edge)
-- Used by: MATCH clauses that reference edge identity
IF NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE name = 'IX_AtomGraphEdges_NodeId'
    AND object_id = OBJECT_ID('graph.AtomGraphEdges')
)
BEGIN
    CREATE NONCLUSTERED INDEX IX_AtomGraphEdges_NodeId
    ON graph.AtomGraphEdges ($node_id);
END;

-- Index 2: $from_id pseudo-column (source node reference)
-- Used by: MATCH (a)-[e]->(b) WHERE a.$node_id = @sourceId
-- Critical for: Forward traversal queries (outgoing edges from a node)
IF NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE name = 'IX_AtomGraphEdges_FromId'
    AND object_id = OBJECT_ID('graph.AtomGraphEdges')
)
BEGIN
    CREATE NONCLUSTERED INDEX IX_AtomGraphEdges_FromId
    ON graph.AtomGraphEdges ($from_id)
    INCLUDE (EdgeType, Weight, Metadata);
END;

-- Index 3: $to_id pseudo-column (destination node reference)
-- Used by: MATCH (a)<-[e]-(b) WHERE b.$node_id = @destId
-- Critical for: Reverse traversal queries (incoming edges to a node)
IF NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE name = 'IX_AtomGraphEdges_ToId'
    AND object_id = OBJECT_ID('graph.AtomGraphEdges')
)
BEGIN
    CREATE NONCLUSTERED INDEX IX_AtomGraphEdges_ToId
    ON graph.AtomGraphEdges ($to_id)
    INCLUDE (EdgeType, Weight, Metadata);
END;

-- Index 4: Composite index for filtered graph traversal
-- Used by: MATCH (a)-[e:DerivedFrom]->(b) WHERE e.Weight > 0.8
-- Covers: EdgeType filtering + Weight filtering + pseudo-column joins
IF NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE name = 'IX_AtomGraphEdges_EdgeType_Weight_FromId'
    AND object_id = OBJECT_ID('graph.AtomGraphEdges')
)
BEGIN
    CREATE NONCLUSTERED INDEX IX_AtomGraphEdges_EdgeType_Weight_FromId
    ON graph.AtomGraphEdges (EdgeType, Weight, $from_id)
    INCLUDE ($to_id, Metadata);
END;

-- =============================================
-- Performance Validation Query
-- =============================================
/*
-- Test query: Find all atoms derived from a specific atom
DECLARE @sourceNodeId NVARCHAR(1000) = (
    SELECT TOP 1 $node_id
    FROM graph.AtomGraphNodes
    WHERE AtomId = 'atom-abc123'
);

SELECT
    downstream.$node_id,
    downstream.AtomId,
    e.EdgeType,
    e.Weight,
    e.Metadata
FROM graph.AtomGraphNodes AS upstream,
     graph.AtomGraphEdges AS e,
     graph.AtomGraphNodes AS downstream
WHERE MATCH(upstream-(e)->downstream)
  AND upstream.$node_id = @sourceNodeId
  AND e.EdgeType = 'DerivedFrom'
ORDER BY e.Weight DESC;

-- Check execution plan: Should use IX_AtomGraphEdges_FromId (seek)
*/