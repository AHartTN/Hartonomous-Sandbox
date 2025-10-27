-- Semantic Search Stored Procedure
-- Uses VECTOR_DISTANCE for similarity search
USE Hartonomous;
GO

CREATE OR ALTER PROCEDURE dbo.sp_SemanticSearch
    @query_embedding VECTOR(3),
    @top_k INT = 5,
    @category NVARCHAR(50) = NULL
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @start_time DATETIME2 = SYSUTCDATETIME();

    -- Log this inference request
    DECLARE @inference_id BIGINT;
    INSERT INTO dbo.InferenceRequests (task_type, input_data, models_used, ensemble_strategy)
    VALUES ('semantic_search', 'vector_similarity', 'knowledge_base', 'cosine_distance');
    SET @inference_id = SCOPE_IDENTITY();

    -- Perform vector search with cosine similarity
    SELECT TOP (@top_k)
        doc_id,
        content,
        category,
        embedding,
        VECTOR_DISTANCE('cosine', embedding, @query_embedding) as distance,
        (1.0 - VECTOR_DISTANCE('cosine', embedding, @query_embedding)) as similarity_score
    FROM dbo.KnowledgeBase
    WHERE (@category IS NULL OR category = @category)
    ORDER BY distance ASC;

    -- Calculate duration
    DECLARE @duration_ms INT = DATEDIFF(MILLISECOND, @start_time, SYSUTCDATETIME());

    -- Update inference request with completion
    UPDATE dbo.InferenceRequests
    SET total_duration_ms = @duration_ms,
        output_metadata = JSON_OBJECT('status': 'completed', 'results_count': @top_k),
        cache_hit = 0
    WHERE inference_id = @inference_id;

    -- Return the inference ID for tracking
    SELECT @inference_id as inference_id, @duration_ms as duration_ms;
END;
GO

PRINT 'Semantic search procedure created successfully.';
GO
