-- Multi-Model Ensemble Inference
-- Demonstrates querying multiple "models" and combining results
USE Hartonomous;
GO

-- Create table to store different AI models
CREATE TABLE dbo.AIModels (
    ModelId INT PRIMARY KEY IDENTITY(1,1),
    ModelName NVARCHAR(200) NOT NULL,
    ModelType NVARCHAR(100) NOT NULL,
    EmbeddingDim INT NOT NULL,
    ConfidenceWeight FLOAT DEFAULT 1.0,
    IsActive BIT DEFAULT 1,
    CreatedDate DATETIME2 DEFAULT SYSUTCDATETIME()
);
GO

-- Insert sample models
INSERT INTO dbo.AIModels (ModelName, ModelType, EmbeddingDim, ConfidenceWeight)
VALUES
    ('KnowledgeBase-V1', 'retrieval', 3, 0.4),
    ('KnowledgeBase-V2', 'retrieval', 3, 0.35),
    ('KnowledgeBase-V3', 'retrieval', 3, 0.25);
GO

-- Create ensemble inference procedure
CREATE OR ALTER PROCEDURE dbo.sp_EnsembleInference
    @query_embedding VECTOR(3),
    @top_k INT = 5
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @start_time DATETIME2 = SYSUTCDATETIME();
    DECLARE @inference_id BIGINT;

    -- Log the ensemble inference request
    INSERT INTO dbo.InferenceRequests (TaskType, InputData, ModelsUsed, EnsembleStrategy)
    VALUES ('ensemble_search', 'vector_ensemble', 'KB-V1,KB-V2,KB-V3', 'weighted_average');
    SET @inference_id = SCOPE_IDENTITY();

    -- Simulate querying multiple models (in reality, these would be different knowledge bases or model versions)
    -- Model 1: Standard search
    DECLARE @model1_results TABLE (
        DocId INT,
        Content NVARCHAR(MAX),
        Similarity FLOAT,
        ModelId INT
    );

    INSERT INTO @model1_results
    SELECT TOP (@top_k)
        DocId,
        Content,
        (1.0 - VECTOR_DISTANCE('cosine', Embedding, @query_embedding)) as Similarity,
        1 as ModelId
    FROM dbo.KnowledgeBase
    ORDER BY VECTOR_DISTANCE('cosine', Embedding, @query_embedding) ASC;

    -- Model 2: Slightly different weighting (simulated)
    DECLARE @model2_results TABLE (
        DocId INT,
        Content NVARCHAR(MAX),
        Similarity FLOAT,
        ModelId INT
    );

    INSERT INTO @model2_results
    SELECT TOP (@top_k)
        DocId,
        Content,
        (1.0 - VECTOR_DISTANCE('cosine', Embedding, @query_embedding)) * 0.95 as Similarity,
        2 as ModelId
    FROM dbo.KnowledgeBase
    ORDER BY VECTOR_DISTANCE('cosine', Embedding, @query_embedding) ASC;

    -- Model 3: Another variant
    DECLARE @model3_results TABLE (
        DocId INT,
        Content NVARCHAR(MAX),
        Similarity FLOAT,
        ModelId INT
    );

    INSERT INTO @model3_results
    SELECT TOP (@top_k)
        DocId,
        Content,
        (1.0 - VECTOR_DISTANCE('cosine', Embedding, @query_embedding)) * 1.02 as Similarity,
        3 as ModelId
    FROM dbo.KnowledgeBase
    ORDER BY VECTOR_DISTANCE('cosine', Embedding, @query_embedding) ASC;

    -- Ensemble: Combine results with weighted averaging
    WITH AllResults AS (
        SELECT * FROM @model1_results
        UNION ALL
        SELECT * FROM @model2_results
        UNION ALL
        SELECT * FROM @model3_results
    ),
    WeightedScores AS (
        SELECT
            ar.DocId,
            ar.Content,
            ar.ModelId,
            ar.Similarity,
            am.ConfidenceWeight,
            ar.Similarity * am.ConfidenceWeight as WeightedScore
        FROM AllResults ar
        JOIN dbo.AIModels am ON ar.ModelId = am.ModelId
    ),
    EnsembleScores AS (
        SELECT
            DocId,
            Content,
            SUM(WeightedScore) / SUM(ConfidenceWeight) as EnsembleScore,
            COUNT(DISTINCT ModelId) as ModelsContributing,
            STRING_AGG(CAST(ModelId AS NVARCHAR), ',') as ModelIds
        FROM WeightedScores
        GROUP BY DocId, Content
    )
    SELECT TOP (@top_k)
        DocId,
        Content,
        EnsembleScore,
        ModelsContributing,
        ModelIds
    FROM EnsembleScores
    ORDER BY EnsembleScore DESC;

    -- Log inference steps for each model
    INSERT INTO dbo.InferenceSteps (InferenceId, StepNumber, ModelId, OperationType, DurationMs)
    VALUES
        (@inference_id, 1, 1, 'vector_search', 50),
        (@inference_id, 2, 2, 'vector_search', 45),
        (@inference_id, 3, 3, 'vector_search', 48);

    DECLARE @duration_ms INT = DATEDIFF(MILLISECOND, @start_time, SYSUTCDATETIME());

    -- Update main inference request
    UPDATE dbo.InferenceRequests
    SET TotalDurationMs = @duration_ms,
        OutputMetadata = JSON_OBJECT('status': 'completed', 'models': 3, 'results': @top_k)
    WHERE InferenceId = @inference_id;

    SELECT @inference_id as InferenceId, @duration_ms as TotalDurationMs;
END;
GO

PRINT 'Multi-model ensemble procedure created successfully.';
GO

-- Test the ensemble
PRINT 'Testing ensemble inference...';
GO

DECLARE @query VECTOR(3) = CAST('[0.75, 0.8, 0.25]' AS VECTOR(3));
EXEC dbo.sp_EnsembleInference @query_embedding = @query, @top_k = 3;
GO

-- Check inference tracking
SELECT
    ir.InferenceId,
    ir.TaskType,
    ir.ModelsUsed,
    ir.EnsembleStrategy,
    ir.TotalDurationMs,
    COUNT(ist.StepId) as StepCount
FROM dbo.InferenceRequests ir
LEFT JOIN dbo.InferenceSteps ist ON ir.InferenceId = ist.InferenceId
WHERE ir.TaskType = 'ensemble_search'
GROUP BY ir.InferenceId, ir.TaskType, ir.ModelsUsed, ir.EnsembleStrategy, ir.TotalDurationMs;
GO
