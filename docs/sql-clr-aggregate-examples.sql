-- SQL CLR Aggregate Function Examples for Hartonomous Multi-Modal AI Platform
-- These demonstrate revolutionary database-native AI capabilities

-- ============================================================================
-- EXAMPLE 1: Hierarchical Semantic Clustering
-- ============================================================================
-- Discover sub-clusters within document categories using streaming K-means

SELECT 
    category,
    dbo.VectorKMeansCluster(embedding_vector, 5) AS cluster_centroids,
    COUNT(*) as document_count
FROM DocumentEmbeddings
WHERE category IN ('technical', 'legal', 'financial')
GROUP BY category;

-- USE CASE: Automatically discover semantic sub-categories without labeled data
-- Returns JSON array of 5 cluster centroids per category
-- Can be used to initialize next-level clustering or topic modeling


-- ============================================================================
-- EXAMPLE 2: Graph Path Semantic Analysis
-- ============================================================================
-- Analyze how semantic meaning changes as you traverse the knowledge graph

-- Use SQL Server recursive CTE syntax
WITH KnowledgePath AS (
    SELECT 
        1 as depth,
        nodes.$node_id as node_id,
        nodes.AtomId,
        nodes.CanonicalText,
        emb.EmbeddingVector,
        emb.SpatialGeometry
    FROM graph.AtomGraphNodes AS nodes
    INNER JOIN AtomEmbedding emb ON nodes.AtomId = emb.AtomId
    WHERE nodes.AtomId = @StartAtomId
    
    UNION ALL
    
    SELECT 
        kp.depth + 1,
        target.$node_id,
        target.AtomId,
        target.CanonicalText,
        emb.EmbeddingVector,
        emb.SpatialGeometry
    FROM KnowledgePath kp
    INNER JOIN graph.AtomGraphEdges edges ON edges.$from_id = kp.node_id
    INNER JOIN graph.AtomGraphNodes target ON edges.$to_id = target.$node_id
    INNER JOIN AtomEmbedding emb ON target.AtomId = emb.AtomId
    WHERE kp.depth < 5
)
SELECT 
    depth,
    dbo.GraphPathVectorSummary(
        CAST(node_id AS NVARCHAR(MAX)),
        CAST(EmbeddingVector AS NVARCHAR(MAX)),
        SpatialGeometry.STAsText()
    ) AS path_summary,
    COUNT(*) as node_count
FROM KnowledgePath
GROUP BY depth
ORDER BY depth;

-- Returns: For each depth level:
-- {
--   "node_count": 15,
--   "centroid": [0.234, -0.123, ...],
--   "diameter": 0.456,  // Max semantic distance within this level
--   "spatial_extent": {"min_x": -1.2, "max_x": 2.3, ...}
-- }
-- USE CASE: Detect semantic drift in graph traversals, identify when concepts diverge


-- ============================================================================
-- EXAMPLE 3: Weighted Relationship Discovery
-- ============================================================================
-- Find strongest semantic relationships in the knowledge graph

SELECT 
    source.Modality as source_type,
    target.Modality as target_type,
    AVG(edges.Weight) as avg_weight,
    dbo.EdgeWeightedByVectorSimilarity(
        edges.Weight,
        CAST(source_emb.EmbeddingVector AS NVARCHAR(MAX)),
        CAST(target_emb.EmbeddingVector AS NVARCHAR(MAX))
    ) AS similarity_weighted_strength,
    COUNT(*) as edge_count
FROM graph.AtomGraphEdges edges
INNER JOIN graph.AtomGraphNodes source ON edges.$from_id = source.$node_id
INNER JOIN graph.AtomGraphNodes target ON edges.$to_id = target.$node_id
LEFT JOIN AtomEmbedding source_emb ON source.AtomId = source_emb.AtomId
LEFT JOIN AtomEmbedding target_emb ON target.AtomId = target_emb.AtomId
WHERE edges.RelationType = 'semantic_similarity'
GROUP BY source.Modality, target.Modality
HAVING COUNT(*) > 10
ORDER BY similarity_weighted_strength DESC;

-- USE CASE: Discover which modalities (text, image, audio) have strongest semantic links
-- Edge weights get boosted by high cosine similarity between embeddings


-- ============================================================================
-- EXAMPLE 4: Semantic Space Visualization Heatmap
-- ============================================================================
-- Create density grid for visualizing embedding space distribution

WITH SpatialGrid AS (
    SELECT 
        FLOOR(SpatialGeometry.STX / 0.5) * 0.5 AS grid_x,
        FLOOR(SpatialGeometry.STY / 0.5) * 0.5 AS grid_y,
        Modality,
        SpatialGeometry.STAsText() as point_wkt
    FROM AtomEmbedding emb
    INNER JOIN Atom a ON emb.AtomId = a.AtomId
    WHERE SpatialGeometry IS NOT NULL
)
SELECT 
    grid_x,
    grid_y,
    Modality,
    dbo.SpatialDensityGrid(point_wkt) as density_info
FROM SpatialGrid
GROUP BY grid_x, grid_y, Modality
ORDER BY JSON_VALUE(density_info, '$.count') DESC;

-- Returns: {
--   "count": 234,
--   "density": 234,
--   "center_x": 1.234,
--   "center_y": -0.567
-- }
-- USE CASE: Generate heatmaps showing where different modalities cluster in semantic space


-- ============================================================================
-- EXAMPLE 5: Concept Drift Detection
-- ============================================================================
-- Track how embeddings evolve over time (requires temporal embedding table)

CREATE TABLE AtomEmbeddingHistory (
    AtomId BIGINT NOT NULL,
    EmbeddingVector VECTOR(1998) NOT NULL,
    CapturedAt DATETIME2 NOT NULL,
    ModelVersion NVARCHAR(50)
);

-- Detect which concepts are changing fastest
SELECT 
    a.AtomId,
    a.CanonicalText,
    a.Modality,
    dbo.VectorDriftOverTime(
        hist.CapturedAt,
        CAST(hist.EmbeddingVector AS NVARCHAR(MAX))
    ) as drift_analysis
FROM Atom a
INNER JOIN AtomEmbeddingHistory hist ON a.AtomId = hist.AtomId
WHERE hist.CapturedAt > DATEADD(month, -6, GETUTCDATE())
GROUP BY a.AtomId, a.CanonicalText, a.Modality
HAVING COUNT(hist.CapturedAt) >= 5
ORDER BY CAST(JSON_VALUE(drift_analysis, '$.velocity') AS FLOAT) DESC;

-- Returns: {
--   "drift_magnitude": 0.234,
--   "velocity": 0.000456,  // units per second
--   "time_span_seconds": 15552000,
--   "snapshots": 12,
--   "drift_direction": [0.023, -0.045, ...]  // first 10 dims
-- }
-- USE CASE: 
-- - Identify concepts whose meaning is evolving rapidly
-- - Track model updates/retraining impact on embeddings
-- - Detect data distribution shift


-- ============================================================================
-- EXAMPLE 6: Multi-Modal Centroid Comparison
-- ============================================================================
-- Compare semantic centers across different content types

SELECT 
    Modality,
    dbo.VectorCentroid(CAST(EmbeddingVector AS NVARCHAR(MAX))) as centroid,
    COUNT(*) as atom_count
FROM AtomEmbedding emb
INNER JOIN Atom a ON emb.AtomId = a.AtomId
WHERE EmbeddingType = 'primary'
GROUP BY Modality;

-- Then compute cross-modal similarity in a second query:
WITH ModalCentroids AS (
    SELECT 
        Modality,
        dbo.VectorCentroid(CAST(EmbeddingVector AS NVARCHAR(MAX))) as centroid
    FROM AtomEmbedding emb
    INNER JOIN Atom a ON emb.AtomId = a.AtomId
    WHERE EmbeddingType = 'primary'
    GROUP BY Modality
)
SELECT 
    m1.Modality as modality_1,
    m2.Modality as modality_2,
    VECTOR_DISTANCE('cosine', CAST(m1.centroid AS VECTOR(1998)), CAST(m2.centroid AS VECTOR(1998))) as distance
FROM ModalCentroids m1
CROSS JOIN ModalCentroids m2
WHERE m1.Modality < m2.Modality;

-- USE CASE: Measure how different modalities cluster in embedding space
-- Low distance = modalities are semantically aligned
-- High distance = modalities occupy different semantic regions


-- ============================================================================
-- EXAMPLE 7: Spatial Convex Hull for Cluster Visualization
-- ============================================================================
-- Create geometric boundaries around semantic clusters

SELECT 
    category,
    subcategory,
    dbo.SpatialConvexHull(SpatialGeometry.STAsText()) as cluster_boundary,
    COUNT(*) as atom_count
FROM AtomEmbedding emb
INNER JOIN Atom a ON emb.AtomId = a.AtomId
WHERE SpatialGeometry IS NOT NULL
  AND category IS NOT NULL
GROUP BY category, subcategory
HAVING COUNT(*) >= 10;

-- Returns: "POLYGON((1.2 3.4, 2.3 4.5, ..., 1.2 3.4))"
-- USE CASE: 
-- - Visualize cluster boundaries in 2D projected space
-- - Detect cluster overlap (use GEOMETRY .STIntersects())
-- - Identify outliers (points outside all convex hulls)


-- ============================================================================
-- EXAMPLE 8: Covariance Analysis for Dimensionality Reduction
-- ============================================================================
-- Compute covariance matrix to identify principal components

SELECT 
    category,
    dbo.VectorCovariance(CAST(EmbeddingVector AS NVARCHAR(MAX))) as cov_matrix
FROM AtomEmbedding emb
INNER JOIN Atom a ON emb.AtomId = a.AtomId
WHERE EmbeddingType = 'primary'
  AND category = 'scientific_papers'
GROUP BY category;

-- Returns: Sparse JSON with upper triangle of covariance matrix
-- {
--   "0,0": 0.234,
--   "0,1": -0.045,
--   "1,1": 0.567,
--   ...
-- }
-- USE CASE: 
-- - Feed into PCA for dimensionality reduction
-- - Identify most important dimensions for each category
-- - Detect correlated dimensions (candidates for compression)


-- ============================================================================
-- EXAMPLE 9: Combining Aggregates for Multi-Level Analysis
-- ============================================================================
-- Cluster within clusters: hierarchical semantic organization

-- Step 1: Top-level clustering
WITH TopLevelClusters AS (
    SELECT 
        Modality,
        dbo.VectorKMeansCluster(CAST(EmbeddingVector AS NVARCHAR(MAX)), 3) as clusters
    FROM AtomEmbedding emb
    INNER JOIN Atom a ON emb.AtomId = a.AtomId
    WHERE Modality = 'text'
    GROUP BY Modality
),
-- Step 2: Assign atoms to nearest cluster
AtomClusterAssignment AS (
    SELECT 
        a.AtomId,
        emb.EmbeddingVector,
        -- Would need CLR function to extract nearest cluster from JSON
        -- Simplified: use first cluster as example
        JSON_QUERY((SELECT clusters FROM TopLevelClusters), '$[0]') as cluster_center
    FROM Atom a
    INNER JOIN AtomEmbedding emb ON a.AtomId = emb.AtomId
    WHERE a.Modality = 'text'
)
-- Step 3: Sub-cluster within each top-level cluster
SELECT 
    cluster_id,
    dbo.VectorKMeansCluster(CAST(EmbeddingVector AS NVARCHAR(MAX)), 5) as sub_clusters,
    COUNT(*) as member_count
FROM AtomClusterAssignment
GROUP BY cluster_id;

-- USE CASE: Hierarchical topic modeling without labeled data


-- ============================================================================
-- EXAMPLE 10: Real-Time Streaming Analytics
-- ============================================================================
-- Use aggregates in windowed queries for live monitoring

-- Monitor semantic drift in real-time ingestion
SELECT 
    DATEADD(minute, DATEDIFF(minute, 0, CreatedAt) / 15 * 15, 0) as time_window,
    Modality,
    dbo.VectorCentroid(CAST(EmbeddingVector AS NVARCHAR(MAX))) as window_centroid,
    COUNT(*) as ingestion_count
FROM Atom a
INNER JOIN AtomEmbedding emb ON a.AtomId = emb.AtomId
WHERE CreatedAt > DATEADD(hour, -24, GETUTCDATE())
GROUP BY DATEADD(minute, DATEDIFF(minute, 0, CreatedAt) / 15 * 15, 0), Modality
ORDER BY time_window DESC;

-- USE CASE: 
-- - Detect sudden shifts in incoming data distribution
-- - Alert when new content diverges from historical patterns
-- - Monitor data pipeline health


-- ============================================================================
-- REVOLUTIONARY IMPLICATIONS
-- ============================================================================
-- These SQL CLR aggregates enable:
--
-- 1. **Zero-ETL AI**: No need to export to Python/R for clustering, PCA, drift detection
-- 2. **Streaming ML**: Real-time analytics on vectors as data arrives
-- 3. **Graph + Vector fusion**: Analyze semantic relationships during graph traversal
-- 4. **Multi-modal understanding**: Compare and cluster across text, image, audio, video
-- 5. **Temporal AI**: Track how knowledge evolves over time, detect concept drift
-- 6. **Spatial intelligence**: Use GEOMETRY convex hulls to visualize high-dimensional clusters
-- 7. **Self-organizing knowledge**: Database automatically discovers patterns, hierarchies, relationships
--
-- This is not "RAG with a database."
-- This is THE DATABASE AS THE AI PLATFORM.
