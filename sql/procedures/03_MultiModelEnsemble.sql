-- Multi-Model Ensemble Inference
-- Demonstrates querying multiple "models" and combining results
USE Hartonomous;
GO

-- Create table to store different AI models
CREATE TABLE dbo.AIModels (
    model_id INT PRIMARY KEY IDENTITY(1,1),
    model_name NVARCHAR(200) NOT NULL,
    model_type NVARCHAR(100) NOT NULL,
    embedding_dim INT NOT NULL,
    confidence_weight FLOAT DEFAULT 1.0,
    is_active BIT DEFAULT 1,
    created_date DATETIME2 DEFAULT SYSUTCDATETIME()
);
GO

-- Insert sample models
INSERT INTO dbo.AIModels (model_name, model_type, embedding_dim, confidence_weight)
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
    INSERT INTO dbo.InferenceRequests (task_type, input_data, models_used, ensemble_strategy)
    VALUES ('ensemble_search', 'vector_ensemble', 'KB-V1,KB-V2,KB-V3', 'weighted_average');
    SET @inference_id = SCOPE_IDENTITY();

    -- Simulate querying multiple models (in reality, these would be different knowledge bases or model versions)
    -- Model 1: Standard search
    DECLARE @model1_results TABLE (
        doc_id INT,
        content NVARCHAR(MAX),
        similarity FLOAT,
        model_id INT
    );

    INSERT INTO @model1_results
    SELECT TOP (@top_k)
        doc_id,
        content,
        (1.0 - VECTOR_DISTANCE('cosine', embedding, @query_embedding)) as similarity,
        1 as model_id
    FROM dbo.KnowledgeBase
    ORDER BY VECTOR_DISTANCE('cosine', embedding, @query_embedding) ASC;

    -- Model 2: Slightly different weighting (simulated)
    DECLARE @model2_results TABLE (
        doc_id INT,
        content NVARCHAR(MAX),
        similarity FLOAT,
        model_id INT
    );

    INSERT INTO @model2_results
    SELECT TOP (@top_k)
        doc_id,
        content,
        (1.0 - VECTOR_DISTANCE('cosine', embedding, @query_embedding)) * 0.95 as similarity,
        2 as model_id
    FROM dbo.KnowledgeBase
    ORDER BY VECTOR_DISTANCE('cosine', embedding, @query_embedding) ASC;

    -- Model 3: Another variant
    DECLARE @model3_results TABLE (
        doc_id INT,
        content NVARCHAR(MAX),
        similarity FLOAT,
        model_id INT
    );

    INSERT INTO @model3_results
    SELECT TOP (@top_k)
        doc_id,
        content,
        (1.0 - VECTOR_DISTANCE('cosine', embedding, @query_embedding)) * 1.02 as similarity,
        3 as model_id
    FROM dbo.KnowledgeBase
    ORDER BY VECTOR_DISTANCE('cosine', embedding, @query_embedding) ASC;

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
            ar.doc_id,
            ar.content,
            ar.model_id,
            ar.similarity,
            am.confidence_weight,
            ar.similarity * am.confidence_weight as weighted_score
        FROM AllResults ar
        JOIN dbo.AIModels am ON ar.model_id = am.model_id
    ),
    EnsembleScores AS (
        SELECT
            doc_id,
            content,
            SUM(weighted_score) / SUM(confidence_weight) as ensemble_score,
            COUNT(DISTINCT model_id) as models_contributing,
            STRING_AGG(CAST(model_id AS NVARCHAR), ',') as model_ids
        FROM WeightedScores
        GROUP BY doc_id, content
    )
    SELECT TOP (@top_k)
        doc_id,
        content,
        ensemble_score,
        models_contributing,
        model_ids
    FROM EnsembleScores
    ORDER BY ensemble_score DESC;

    -- Log inference steps for each model
    INSERT INTO dbo.InferenceSteps (inference_id, step_number, model_id, operation_type, duration_ms)
    VALUES
        (@inference_id, 1, 1, 'vector_search', 50),
        (@inference_id, 2, 2, 'vector_search', 45),
        (@inference_id, 3, 3, 'vector_search', 48);

    DECLARE @duration_ms INT = DATEDIFF(MILLISECOND, @start_time, SYSUTCDATETIME());

    -- Update main inference request
    UPDATE dbo.InferenceRequests
    SET total_duration_ms = @duration_ms,
        output_metadata = JSON_OBJECT('status': 'completed', 'models': 3, 'results': @top_k)
    WHERE inference_id = @inference_id;

    SELECT @inference_id as inference_id, @duration_ms as total_duration_ms;
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
    ir.inference_id,
    ir.task_type,
    ir.models_used,
    ir.ensemble_strategy,
    ir.total_duration_ms,
    COUNT(ist.step_id) as step_count
FROM dbo.InferenceRequests ir
LEFT JOIN dbo.InferenceSteps ist ON ir.inference_id = ist.inference_id
WHERE ir.task_type = 'ensemble_search'
GROUP BY ir.inference_id, ir.task_type, ir.models_used, ir.ensemble_strategy, ir.total_duration_ms;
GO
