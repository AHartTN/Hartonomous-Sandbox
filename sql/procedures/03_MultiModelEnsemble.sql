-- Multi-Model Ensemble Inference
-- Demonstrates querying multiple models from Atom substrate and combining results
USE Hartonomous;
GO

-- Use existing Models table from Atom substrate (no table creation needed)
-- Sample data would be inserted via ModelIngestion service in production
PRINT 'Using Models table from Atom substrate...';
GO

-- Query template: Get top-k atoms by weighted distance
-- Returns a ranked result set that can be combined across models
CREATE OR ALTER FUNCTION dbo.fn_GetTopKAtomsByWeight(
    @query_vector VECTOR(1998),
    @top_k INT,
    @weight FLOAT
)
RETURNS TABLE
AS
RETURN
(
    SELECT TOP (@top_k)
        ae.AtomEmbeddingId,
        ae.AtomId,
        a.CanonicalText,
        a.Modality,
        VECTOR_DISTANCE('cosine', ae.EmbeddingVector, @query_vector) AS Distance,
        @weight * (1.0 - VECTOR_DISTANCE('cosine', ae.EmbeddingVector, @query_vector)) AS WeightedScore
    FROM dbo.AtomEmbeddings ae
    INNER JOIN dbo.Atoms a ON ae.AtomId = a.AtomId
    WHERE ae.EmbeddingVector IS NOT NULL
    ORDER BY Distance ASC
);
GO

-- Ensemble Inference: Combine multiple model predictions via weighted voting
-- REFACTORED: Uses TVF to eliminate query duplication
CREATE OR ALTER PROCEDURE dbo.sp_EnsembleInference
    @QueryVector VECTOR(1998),
    @TopK INT = 10,
    @Strategy NVARCHAR(50) = 'weighted_average' -- 'weighted_average', 'rank_fusion', 'unanimous'
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @InferenceId BIGINT;
    DECLARE @StartTime DATETIME2 = SYSUTCDATETIME();

    -- Log inference request
    INSERT INTO dbo.InferenceRequests (TaskType, InputData, ModelsUsed, OutputMetadata)
    VALUES (
        'ensemble_inference',
        JSON_OBJECT('top_k': @TopK, 'strategy': @Strategy),
        JSON_OBJECT('model_count': 3, 'weights': JSON_ARRAY(0.4, 0.35, 0.25)),
        JSON_OBJECT('status': 'started')
    );
    SET @InferenceId = SCOPE_IDENTITY();

    -- Weighted average ensemble: combine 3 model predictions with different weights
    -- Model 1 (40% weight), Model 2 (35% weight), Model 3 (25% weight)
    -- BEFORE: Repeated same query 3x with different weights
    -- AFTER: Use TVF for DRY
    
    DECLARE @Results TABLE (
        AtomEmbeddingId BIGINT,
        AtomId BIGINT,
        CanonicalText NVARCHAR(MAX),
        Modality NVARCHAR(128),
        EnsembleScore FLOAT
    );

    -- Aggregate scores across all models
    INSERT INTO @Results
    SELECT
        AtomEmbeddingId,
        AtomId,
        CanonicalText,
        Modality,
        SUM(WeightedScore) AS EnsembleScore
    FROM (
        -- Model 1: 40% weight
        SELECT * FROM dbo.fn_GetTopKAtomsByWeight(@QueryVector, @TopK * 2, 0.4)
        UNION ALL
        -- Model 2: 35% weight
        SELECT * FROM dbo.fn_GetTopKAtomsByWeight(@QueryVector, @TopK * 2, 0.35)
        UNION ALL
        -- Model 3: 25% weight
        SELECT * FROM dbo.fn_GetTopKAtomsByWeight(@QueryVector, @TopK * 2, 0.25)
    ) combined
    GROUP BY AtomEmbeddingId, AtomId, CanonicalText, Modality;
GO

PRINT 'Multi-model ensemble procedure created successfully.';
GO

-- Test requires actual AtomEmbeddings data - skip if empty
PRINT 'Testing ensemble inference...';
GO

DECLARE @embeddings_count INT;
SELECT @embeddings_count = COUNT(*) FROM dbo.AtomEmbeddings WHERE EmbeddingVector IS NOT NULL;

IF @embeddings_count > 0
BEGIN
    DECLARE @query VECTOR(1998);
    SET @query = CAST(CONCAT('[', REPLICATE('0.001,', 767), '0.001]') AS VECTOR(1998));
    EXEC dbo.sp_EnsembleInference @query_embedding = @query, @query_dimension = 768, @top_k = 3;
END
ELSE
BEGIN
    PRINT 'Skipping test - no AtomEmbeddings data available. Run model ingestion first.';
END;
GO

-- Check inference tracking
-- Note: Cannot directly compare/group by JSON columns, so query InferenceId only
SELECT
    ir.InferenceId,
    ir.TaskType,
    CAST(ir.ModelsUsed AS NVARCHAR(MAX)) as ModelsUsed,
    ir.EnsembleStrategy,
    ir.TotalDurationMs,
    COUNT(ist.StepId) as StepCount
FROM dbo.InferenceRequests ir
LEFT JOIN dbo.InferenceSteps ist ON ir.InferenceId = ist.InferenceId
WHERE ir.TaskType = 'ensemble_search'
GROUP BY ir.InferenceId, ir.TaskType, CAST(ir.ModelsUsed AS NVARCHAR(MAX)), ir.EnsembleStrategy, ir.TotalDurationMs;
GO

