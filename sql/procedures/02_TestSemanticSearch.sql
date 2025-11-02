-- Test Semantic Search Procedure
USE Hartonomous;
GO

PRINT 'Testing semantic search with AI-related query vector...';
GO

-- Query vector similar to AI/ML documents (high in first two dimensions)
DECLARE @ai_query VECTOR(3) = CAST('[0.8, 0.7, 0.2]' AS VECTOR(3));
EXEC dbo.sp_SemanticSearch @query_embedding = @ai_query, @top_k = 3;
GO

PRINT '';
PRINT 'Testing semantic search with Database-related query vector...';
GO

-- Query vector similar to Database documents (high in third dimension)
DECLARE @db_query VECTOR(3) = CAST('[0.2, 0.3, 0.9]' AS VECTOR(3));
EXEC dbo.sp_SemanticSearch @query_embedding = @db_query, @top_k = 3;
GO

PRINT '';
PRINT 'Checking inference logs...';
GO

SELECT TOP 5
	inference_id,
	task_type,
	models_used,
	total_duration_ms,
	output_metadata,
	request_timestamp
FROM dbo.InferenceRequests
ORDER BY inference_id DESC;
GO
