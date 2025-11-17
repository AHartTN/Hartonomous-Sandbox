-- =============================================
-- Add Graph Pseudo-Column Indexes for AtomGraphEdges (EDGE TABLE)
-- MS Docs Best Practice: Index $from_id, $to_id for 10-100x MATCH speedup
-- Reference: https://learn.microsoft.com/en-us/sql/relational-databases/graphs/sql-graph-architecture
-- NOTE: Edge tables have $edge_id, $from_id, $to_id (NOT $node_id - that's for node tables only)
-- =============================================

-- Index 1: $edge_id pseudo-column (unique identifier for each edge)
-- Used by: MATCH clauses that reference specific edges
IF NOT EXISTS (
    SELECT 1 FROM sys.indexes 
    WHERE name = 'IX_AtomGraphEdges_EdgeId' 
    AND object_id = OBJECT_ID('graph.AtomGraphEdges')
)
BEGIN
    CREATE NONCLUSTERED INDEX IX_AtomGraphEdges_EdgeId
    ON graph.AtomGraphEdges ($edge_id);  -- Changed from $node_id to $edge_id
    
    PRINT 'Created index IX_AtomGraphEdges_EdgeId on $edge_id pseudo-column';
END
GO

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
    INCLUDE (RelationType, Weight, Metadata);  -- Changed EdgeType to RelationType
    
    PRINT 'Created index IX_AtomGraphEdges_FromId on $from_id pseudo-column';
END
GO

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
    INCLUDE (RelationType, Weight, Metadata);  -- Changed EdgeType to RelationType
    
    PRINT 'Created index IX_AtomGraphEdges_ToId on $to_id pseudo-column';
END
GO

-- Index 4: Composite index for filtered graph traversal
-- Used by: MATCH (a)-[e:DerivedFrom]->(b) WHERE e.Weight > 0.8
-- Covers: RelationType filtering + Weight filtering + pseudo-column joins
IF NOT EXISTS (
    SELECT 1 FROM sys.indexes 
    WHERE name = 'IX_AtomGraphEdges_RelationType_Weight_FromId' 
    AND object_id = OBJECT_ID('graph.AtomGraphEdges')
)
BEGIN
    CREATE NONCLUSTERED INDEX IX_AtomGraphEdges_RelationType_Weight_FromId
    ON graph.AtomGraphEdges (RelationType, Weight, $from_id)
    INCLUDE ($to_id, Metadata);
    
    PRINT 'Created composite index IX_AtomGraphEdges_RelationType_Weight_FromId for filtered traversal';
END
GO

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
    e.RelationType,  -- Changed EdgeType to RelationType
    e.Weight,
    e.Metadata
FROM graph.AtomGraphNodes AS upstream,
     graph.AtomGraphEdges AS e,
     graph.AtomGraphNodes AS downstream
WHERE MATCH(upstream-(e)->downstream)
  AND upstream.$node_id = @sourceNodeId
  AND e.RelationType = 'DerivedFrom'  -- Changed EdgeType to RelationType
ORDER BY e.Weight DESC;

-- Check execution plan: Should use IX_AtomGraphEdges_FromId (seek)
*/
GO
