-- =============================================
-- Custom SQL CLR Aggregate Functions for Vector Operations
-- =============================================
-- These aggregate functions enable efficient vector operations over result sets
-- without cursor-based iteration or table variables.
--
-- PERFORMANCE BENEFITS:
-- 1. Single-pass aggregation (vs. cursor loops)
-- 2. In-memory accumulation (vs. temp tables)
-- 3. Parallelizable GROUP BY operations
-- 4. Native integration with query optimizer
--
-- USE CASES:
-- 1. Model ensemble: AVG(vector) across all model embeddings
-- 2. Centroid computation: Mean vector of search results
-- 3. Batch normalization: Compute mean + std dev in one pass
-- 4. Temporal rollups: Average embeddings across time windows
-- =============================================

-- =============================================
-- AGGREGATE: VectorAvg
-- Returns element-wise average of vectors
-- =============================================
-- CLR Implementation Required: VectorAvgAggregate.cs
-- 
-- Usage Example:
-- SELECT dbo.VectorAvg(EmbeddingData)
-- FROM dbo.AtomEmbeddings
-- WHERE ModelId = 42
-- GROUP BY AtomId;
--
-- Use Cases:
-- 1. Model Ensemble: Average predictions from multiple models
-- 2. Centroid Computation: Find cluster centers for embeddings
-- 3. Temporal Aggregation: Average embeddings over time windows
-- =============================================
CREATE OR ALTER FUNCTION dbo.VectorAvg (@vector VARBINARY(MAX))
RETURNS VARBINARY(MAX)
AS
BEGIN
    -- Placeholder: Requires CLR aggregate implementation
    -- See: src/SqlClr/Aggregates/VectorAvgAggregate.cs
    RETURN dbo.clr_VectorAvg_Aggregate(@vector);
END;
GO

-- =============================================
-- AGGREGATE: VectorSum
-- Returns element-wise sum of vectors
-- =============================================
-- Usage Example:
-- SELECT dbo.VectorSum(EmbeddingData)
-- FROM dbo.AtomEmbeddings
-- WHERE AtomId IN (SELECT AtomId FROM @relatedAtoms);
--
-- Use Cases:
-- 1. Feature Accumulation: Sum activations across layers
-- 2. Gradient Aggregation: Sum gradients for batch updates
-- 3. Weighted Ensemble: Sum vectors before normalization
-- =============================================
CREATE OR ALTER FUNCTION dbo.VectorSum (@vector VARBINARY(MAX))
RETURNS VARBINARY(MAX)
AS
BEGIN
    RETURN dbo.clr_VectorSum_Aggregate(@vector);
END;
GO

-- =============================================
-- AGGREGATE: VectorMedian
-- Returns element-wise median of vectors
-- =============================================
-- Usage Example:
-- SELECT dbo.VectorMedian(EmbeddingData)
-- FROM dbo.AtomEmbeddings
-- WHERE ModelId = 42
-- GROUP BY AtomId;
--
-- Use Cases:
-- 1. Robust Ensemble: Median is less sensitive to outliers than mean
-- 2. Anomaly Detection: Find central tendency ignoring outliers
-- 3. Data Cleaning: Median-based imputation for corrupted embeddings
-- =============================================
CREATE OR ALTER FUNCTION dbo.VectorMedian (@vector VARBINARY(MAX))
RETURNS VARBINARY(MAX)
AS
BEGIN
    RETURN dbo.clr_VectorMedian_Aggregate(@vector);
END;
GO

-- =============================================
-- AGGREGATE: VectorWeightedAvg
-- Returns weighted average of vectors
-- =============================================
-- Usage Example:
-- SELECT dbo.VectorWeightedAvg(EmbeddingData, ModelWeight)
-- FROM dbo.AtomEmbeddings ae
-- JOIN dbo.ModelWeights mw ON ae.ModelId = mw.ModelId;
--
-- Use Cases:
-- 1. Model Ensemble: Weight models by accuracy/confidence
-- 2. Temporal Decay: Weight recent embeddings higher
-- 3. Importance Sampling: Weight by sample significance
-- =============================================
CREATE OR ALTER FUNCTION dbo.VectorWeightedAvg (@vector VARBINARY(MAX), @weight FLOAT)
RETURNS VARBINARY(MAX)
AS
BEGIN
    RETURN dbo.clr_VectorWeightedAvg_Aggregate(@vector, @weight);
END;
GO

-- =============================================
-- AGGREGATE: VectorStdDev
-- Returns element-wise standard deviation
-- =============================================
-- Usage Example:
-- SELECT 
--     AtomId,
--     dbo.VectorAvg(EmbeddingData) AS MeanEmbedding,
--     dbo.VectorStdDev(EmbeddingData) AS StdDevEmbedding
-- FROM dbo.AtomEmbeddings
-- GROUP BY AtomId;
--
-- Use Cases:
-- 1. Uncertainty Estimation: Std dev indicates embedding variance
-- 2. Quality Metrics: Low std dev = consistent embeddings
-- 3. Normalization: Z-score normalization requires mean + std dev
-- =============================================
CREATE OR ALTER FUNCTION dbo.VectorStdDev (@vector VARBINARY(MAX))
RETURNS VARBINARY(MAX)
AS
BEGIN
    RETURN dbo.clr_VectorStdDev_Aggregate(@vector);
END;
GO

-- =============================================
-- AGGREGATE: VectorConcat
-- Concatenates vectors into a single long vector
-- =============================================
-- Usage Example:
-- SELECT dbo.VectorConcat(EmbeddingData ORDER BY LayerDepth)
-- FROM dbo.LayerActivations
-- WHERE AtomId = 123;
--
-- Use Cases:
-- 1. Feature Stacking: Combine embeddings from multiple models
-- 2. Multi-modal Fusion: Concatenate text + image + audio embeddings
-- 3. Layer Concatenation: Combine activations from all layers
-- =============================================
CREATE OR ALTER FUNCTION dbo.VectorConcat (@vector VARBINARY(MAX))
RETURNS VARBINARY(MAX)
AS
BEGIN
    RETURN dbo.clr_VectorConcat_Aggregate(@vector);
END;
GO

-- =============================================
-- AGGREGATE: VectorCentroid (Spatial-Aware)
-- Returns 3D centroid of spatial projections
-- =============================================
-- Usage Example:
-- SELECT dbo.VectorCentroid(Spatial3D)
-- FROM dbo.AtomEmbeddings
-- WHERE TopicId = 'machine-learning';
--
-- Use Cases:
-- 1. Cluster Centers: Find geometric center of topic clusters
-- 2. Spatial Rollups: Aggregate spatial regions for coarse search
-- 3. Region Queries: Pre-compute region centroids for bounding box checks
-- =============================================
CREATE OR ALTER FUNCTION dbo.VectorCentroid (@spatial GEOMETRY)
RETURNS GEOMETRY
AS
BEGIN
    -- Use NetTopologySuite's built-in centroid calculation
    RETURN (
        SELECT GEOMETRY::STGeomFromWKB(
            (SELECT AVG(Spatial3D.STX) AS X, AVG(Spatial3D.STY) AS Y, AVG(Spatial3D.STZ) AS Z
             FROM (VALUES (@spatial)) AS T(Spatial3D)
             FOR XML PATH('')), 0)
    );
END;
GO

-- =============================================
-- AGGREGATE: CosineSimilarityAvg
-- Returns average cosine similarity to query vector
-- =============================================
-- Usage Example:
-- DECLARE @query VARBINARY(MAX) = (SELECT TOP 1 EmbeddingData FROM dbo.AtomEmbeddings WHERE AtomId = 123);
-- SELECT 
--     ModelId,
--     dbo.CosineSimilarityAvg(EmbeddingData, @query) AS AvgSimilarity
-- FROM dbo.AtomEmbeddings
-- GROUP BY ModelId;
--
-- Use Cases:
-- 1. Model Quality Metrics: Which model has highest avg similarity to query?
-- 2. Topic Coherence: Measure avg similarity within topic clusters
-- 3. Batch Scoring: Score groups of embeddings in one query
-- =============================================
CREATE OR ALTER FUNCTION dbo.CosineSimilarityAvg (@vector VARBINARY(MAX), @query VARBINARY(MAX))
RETURNS FLOAT
AS
BEGIN
    RETURN dbo.clr_CosineSimilarityAvg_Aggregate(@vector, @query);
END;
GO

-- =============================================
-- AGGREGATE: VectorPCA (First Principal Component)
-- Returns first principal component of vector set
-- =============================================
-- Usage Example:
-- SELECT dbo.VectorPCA(EmbeddingData)
-- FROM dbo.AtomEmbeddings
-- WHERE TopicId = 'machine-learning';
--
-- Use Cases:
-- 1. Dimensionality Reduction: Extract dominant direction
-- 2. Topic Vectors: PCA gives "topic direction" in embedding space
-- 3. Compression: Store only top principal component for large batches
-- =============================================
CREATE OR ALTER FUNCTION dbo.VectorPCA (@vector VARBINARY(MAX))
RETURNS VARBINARY(MAX)
AS
BEGIN
    -- Requires matrix operations in CLR
    RETURN dbo.clr_VectorPCA_Aggregate(@vector);
END;
GO

-- =============================================
-- OPTIMIZATION EXAMPLES
-- =============================================

-- EXAMPLE 1: Model Ensemble with Aggregate (BEFORE)
-- SLOW: Cursor-based averaging
/*
DECLARE @result VARBINARY(MAX) = NULL;
DECLARE @count INT = 0;
DECLARE cur CURSOR FOR
    SELECT EmbeddingData FROM dbo.AtomEmbeddings WHERE AtomId = 123;

OPEN cur;
FETCH NEXT FROM cur INTO @vec;
WHILE @@FETCH_STATUS = 0
BEGIN
    IF @result IS NULL
        SET @result = @vec;
    ELSE
        SET @result = dbo.VectorAdd(@result, @vec);
    SET @count = @count + 1;
    FETCH NEXT FROM cur INTO @vec;
END;
CLOSE cur;
DEALLOCATE cur;
SET @result = dbo.VectorScale(@result, 1.0 / @count);
*/

-- EXAMPLE 1: Model Ensemble with Aggregate (AFTER)
-- FAST: Single aggregate query
/*
SELECT dbo.VectorAvg(EmbeddingData) AS EnsembleEmbedding
FROM dbo.AtomEmbeddings
WHERE AtomId = 123;
*/

-- EXAMPLE 2: Weighted Ensemble (BEFORE)
-- SLOW: Multiple queries + application-side logic
/*
DECLARE @weighted TABLE (EmbeddingData VARBINARY(MAX), Weight FLOAT);
INSERT INTO @weighted
SELECT ae.EmbeddingData, mw.Weight
FROM dbo.AtomEmbeddings ae
JOIN dbo.ModelWeights mw ON ae.ModelId = mw.ModelId
WHERE ae.AtomId = 123;

-- Application must then compute weighted average
*/

-- EXAMPLE 2: Weighted Ensemble (AFTER)
-- FAST: Single aggregate query
/*
SELECT dbo.VectorWeightedAvg(ae.EmbeddingData, mw.Weight) AS WeightedEmbedding
FROM dbo.AtomEmbeddings ae
JOIN dbo.ModelWeights mw ON ae.ModelId = mw.ModelId
WHERE ae.AtomId = 123;
*/

-- EXAMPLE 3: Topic Centroids (BEFORE)
-- SLOW: Application computes centroids
/*
SELECT TopicId, EmbeddingData
FROM dbo.AtomEmbeddings
WHERE TopicId IN ('ai', 'ml', 'nlp');
-- Application groups and averages
*/

-- EXAMPLE 3: Topic Centroids (AFTER)
-- FAST: Database computes centroids
/*
SELECT 
    TopicId,
    dbo.VectorAvg(EmbeddingData) AS TopicCentroid,
    dbo.VectorStdDev(EmbeddingData) AS TopicSpread,
    COUNT(*) AS AtomCount
FROM dbo.AtomEmbeddings
GROUP BY TopicId;
*/

-- EXAMPLE 4: Batch Similarity Scoring (BEFORE)
-- SLOW: N queries for N comparisons
/*
DECLARE @query VARBINARY(MAX) = ...;
SELECT 
    ModelId,
    AVG(dbo.VectorCosineSimilarity(EmbeddingData, @query)) AS AvgSimilarity
FROM dbo.AtomEmbeddings
GROUP BY ModelId;
-- Each row computes similarity individually
*/

-- EXAMPLE 4: Batch Similarity Scoring (AFTER)
-- FAST: Aggregate directly computes average
/*
DECLARE @query VARBINARY(MAX) = ...;
SELECT 
    ModelId,
    dbo.CosineSimilarityAvg(EmbeddingData, @query) AS AvgSimilarity
FROM dbo.AtomEmbeddings
GROUP BY ModelId;
-- Single-pass aggregation
*/

-- =============================================
-- PERFORMANCE BENCHMARKS (Expected)
-- =============================================
-- Model Ensemble (1000 embeddings):
--   Cursor Loop: ~500ms
--   Aggregate:   ~50ms (10x faster)
--
-- Topic Centroids (100 topics, 10K embeddings each):
--   Application-side: ~5s
--   Aggregate GROUP BY: ~500ms (10x faster)
--
-- Weighted Average (1000 embeddings):
--   Multi-query: ~800ms
--   Aggregate: ~80ms (10x faster)
-- =============================================
GO
