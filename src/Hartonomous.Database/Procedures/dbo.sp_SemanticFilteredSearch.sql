CREATE PROCEDURE dbo.sp_SemanticFilteredSearch
    @query_vector VECTOR(1998),
    @top_k INT = 10,
    @TenantId INT, -- V3: Added for security
    @EmbeddingType NVARCHAR(50) = NULL, -- V3: Added for filtering
    @topic_filter NVARCHAR(50) = NULL,
    @min_topic_score FLOAT = 0.5,
    @min_sentiment FLOAT = NULL,
    @max_sentiment FLOAT = NULL,
    @min_temporal_relevance FLOAT = 0.0
AS
BEGIN
    SET NOCOUNT ON;

    PRINT 'SEMANTIC-FILTERED VECTOR SEARCH';
    PRINT '  Topic filter: ' + ISNULL(@topic_filter, 'none');
    PRINT '  Min topic score: ' + CAST(@min_topic_score AS VARCHAR);
    PRINT '';

    SELECT TOP (@top_k)
        ae.AtomEmbeddingId,
        ae.AtomId,
        a.Modality,
        a.Subtype,
        VECTOR_DISTANCE('cosine', ae.SpatialKey, @query_vector) AS vector_distance,
        sf.TopicTechnical,
        sf.TopicBusiness,
        sf.TopicScientific,
        sf.TopicCreative,
        sf.SentimentScore,
        sf.TemporalRelevance
    FROM dbo.AtomEmbeddings AS ae
    INNER JOIN dbo.SemanticFeatures AS sf ON sf.AtomEmbeddingId = ae.AtomEmbeddingId
    INNER JOIN dbo.Atoms AS a ON a.AtomId = ae.AtomId
    WHERE 
        -- V3: TENANCY MODEL
        (a.TenantId = @TenantId OR EXISTS (SELECT 1 FROM dbo.TenantAtoms ta WHERE ta.AtomId = a.AtomId AND ta.TenantId = @TenantId))
        -- V3: EMBEDDING TYPE FILTER
        AND (@EmbeddingType IS NULL OR ae.EmbeddingType = @EmbeddingType)
        AND ae.SpatialKey IS NOT NULL
        AND (@topic_filter IS NULL OR
            (@topic_filter = 'technical' AND sf.TopicTechnical >= @min_topic_score) OR
            (@topic_filter = 'business' AND sf.TopicBusiness >= @min_topic_score) OR
            (@topic_filter = 'scientific' AND sf.TopicScientific >= @min_topic_score) OR
            (@topic_filter = 'creative' AND sf.TopicCreative >= @min_topic_score))
        AND (@min_sentiment IS NULL OR sf.SentimentScore >= @min_sentiment)
        AND (@max_sentiment IS NULL OR sf.SentimentScore <= @max_sentiment)
        AND sf.TemporalRelevance >= @min_temporal_relevance
    ORDER BY VECTOR_DISTANCE('cosine', ae.SpatialKey, @query_vector);

    PRINT '  ✓ Semantic-filtered search complete';
END