-- SEMANTIC FEATURE EXTRACTION
-- Pure T-SQL analysis to compute semantic coordinates
USE Hartonomous;
GO

SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;
GO

PRINT '============================================================';
PRINT 'SEMANTIC FEATURE EXTRACTION SYSTEM';
PRINT 'Computing Semantic Coordinates from Raw Data in T-SQL';
PRINT '============================================================';
GO

-- ==========================================
-- PART 1: Semantic Features Table
-- ==========================================

IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'SemanticFeatures')
BEGIN
    CREATE TABLE dbo.SemanticFeatures (
        embedding_id BIGINT PRIMARY KEY,

        -- Topic Scores (0-1 normalized)
        topic_technical FLOAT DEFAULT 0,      -- Programming, database, algorithms
        topic_business FLOAT DEFAULT 0,       -- Business, finance, commerce
        topic_scientific FLOAT DEFAULT 0,     -- Science, research, academic
        topic_creative FLOAT DEFAULT 0,       -- Art, design, creative writing

        -- Sentiment & Tone
        sentiment_score FLOAT DEFAULT 0,      -- -1 (negative) to +1 (positive)
        formality_score FLOAT DEFAULT 0,      -- 0 (casual) to 1 (formal)
        complexity_score FLOAT DEFAULT 0,     -- 0 (simple) to 1 (complex)

        -- Temporal Features
        temporal_relevance FLOAT DEFAULT 1,   -- Decay over time
        reference_date DATETIME2,

        -- Statistical Features
        text_length INT,
        word_count INT,
        unique_word_ratio FLOAT,
        avg_word_length FLOAT,

        -- Computed Timestamp
        computed_at DATETIME2 DEFAULT SYSUTCDATETIME(),

        FOREIGN KEY (embedding_id) REFERENCES dbo.Embeddings_Production(embedding_id)
    );

    PRINT 'Created SemanticFeatures table';
END
GO

-- Create indexes on semantic features for fast filtering
CREATE INDEX idx_topic_technical ON dbo.SemanticFeatures(topic_technical) WHERE topic_technical > 0.5;
CREATE INDEX idx_topic_business ON dbo.SemanticFeatures(topic_business) WHERE topic_business > 0.5;
CREATE INDEX idx_topic_scientific ON dbo.SemanticFeatures(topic_scientific) WHERE topic_scientific > 0.5;
CREATE INDEX idx_sentiment ON dbo.SemanticFeatures(sentiment_score);
CREATE INDEX idx_temporal ON dbo.SemanticFeatures(temporal_relevance) WHERE temporal_relevance > 0.5;
GO

PRINT 'Created filtered indexes on semantic features';
GO

-- ==========================================
-- PART 2: Keyword Dictionaries
-- ==========================================

IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'TopicKeywords')
BEGIN
    CREATE TABLE dbo.TopicKeywords (
        keyword_id INT PRIMARY KEY IDENTITY(1,1),
        topic_name NVARCHAR(50),
        keyword NVARCHAR(100),
        weight FLOAT DEFAULT 1.0
    );

    PRINT 'Created TopicKeywords dictionary';
END
GO

-- Initialize topic dictionaries
IF NOT EXISTS (SELECT 1 FROM dbo.TopicKeywords WHERE topic_name = 'technical')
BEGIN
    INSERT INTO dbo.TopicKeywords (topic_name, keyword, weight) VALUES
    -- Technical keywords
    ('technical', 'database', 1.0),
    ('technical', 'sql', 1.0),
    ('technical', 'server', 0.8),
    ('technical', 'query', 0.9),
    ('technical', 'index', 0.9),
    ('technical', 'algorithm', 1.0),
    ('technical', 'programming', 1.0),
    ('technical', 'code', 0.9),
    ('technical', 'function', 0.8),
    ('technical', 'api', 0.9),
    ('technical', 'system', 0.7),
    ('technical', 'software', 0.9),
    ('technical', 'data', 0.8),
    ('technical', 'computer', 0.8),
    ('technical', 'network', 0.9),

    -- Business keywords
    ('business', 'revenue', 1.0),
    ('business', 'profit', 1.0),
    ('business', 'customer', 0.9),
    ('business', 'market', 0.9),
    ('business', 'sales', 0.9),
    ('business', 'strategy', 0.8),
    ('business', 'management', 0.8),
    ('business', 'finance', 1.0),
    ('business', 'investment', 0.9),
    ('business', 'business', 1.0),
    ('business', 'enterprise', 0.8),
    ('business', 'commerce', 0.9),

    -- Scientific keywords
    ('scientific', 'research', 1.0),
    ('scientific', 'theory', 0.9),
    ('scientific', 'experiment', 1.0),
    ('scientific', 'hypothesis', 1.0),
    ('scientific', 'scientific', 1.0),
    ('scientific', 'analysis', 0.8),
    ('scientific', 'study', 0.8),
    ('scientific', 'method', 0.7),
    ('scientific', 'evidence', 0.9),
    ('scientific', 'quantum', 1.0),
    ('scientific', 'physics', 1.0),
    ('scientific', 'biology', 1.0),
    ('scientific', 'chemistry', 1.0),

    -- Creative keywords
    ('creative', 'design', 1.0),
    ('creative', 'art', 1.0),
    ('creative', 'creative', 1.0),
    ('creative', 'aesthetic', 0.9),
    ('creative', 'visual', 0.8),
    ('creative', 'style', 0.7),
    ('creative', 'artistic', 1.0),
    ('creative', 'beautiful', 0.8),
    ('creative', 'imagine', 0.8),
    ('creative', 'inspire', 0.9);

    PRINT 'Initialized topic keyword dictionaries';
END
GO

-- ==========================================
-- PART 3: Feature Computation
-- ==========================================

CREATE OR ALTER PROCEDURE sp_ComputeSemanticFeatures
    @embedding_id BIGINT
AS
BEGIN
    SET NOCOUNT ON;

    -- Get the source text
    DECLARE @text NVARCHAR(MAX);
    SELECT @text = LOWER(source_text)
    FROM dbo.Embeddings_Production
    WHERE embedding_id = @embedding_id;

    IF @text IS NULL
    BEGIN
        PRINT 'Embedding ID not found: ' + CAST(@embedding_id AS VARCHAR);
        RETURN;
    END;

    -- Statistical features
    DECLARE @text_length INT = LEN(@text);
    DECLARE @word_count INT = LEN(@text) - LEN(REPLACE(@text, ' ', '')) + 1;
    DECLARE @avg_word_length FLOAT = CAST(@text_length AS FLOAT) / NULLIF(@word_count, 0);

    -- Topic scores (keyword matching)
    DECLARE @topic_technical FLOAT = 0;
    DECLARE @topic_business FLOAT = 0;
    DECLARE @topic_scientific FLOAT = 0;
    DECLARE @topic_creative FLOAT = 0;

    -- Count keyword matches for each topic
    SELECT @topic_technical = SUM(
        CASE
            WHEN @text LIKE '%' + keyword + '%' THEN weight
            ELSE 0
        END
    ) / COUNT(*)
    FROM dbo.TopicKeywords
    WHERE topic_name = 'technical';

    SELECT @topic_business = SUM(
        CASE
            WHEN @text LIKE '%' + keyword + '%' THEN weight
            ELSE 0
        END
    ) / COUNT(*)
    FROM dbo.TopicKeywords
    WHERE topic_name = 'business';

    SELECT @topic_scientific = SUM(
        CASE
            WHEN @text LIKE '%' + keyword + '%' THEN weight
            ELSE 0
        END
    ) / COUNT(*)
    FROM dbo.TopicKeywords
    WHERE topic_name = 'scientific';

    SELECT @topic_creative = SUM(
        CASE
            WHEN @text LIKE '%' + keyword + '%' THEN weight
            ELSE 0
        END
    ) / COUNT(*)
    FROM dbo.TopicKeywords
    WHERE topic_name = 'creative';

    -- Normalize topic scores (0-1 range)
    SET @topic_technical = ISNULL(@topic_technical, 0);
    SET @topic_business = ISNULL(@topic_business, 0);
    SET @topic_scientific = ISNULL(@topic_scientific, 0);
    SET @topic_creative = ISNULL(@topic_creative, 0);

    -- Sentiment score (simple heuristic based on keywords)
    DECLARE @sentiment_score FLOAT = 0;
    -- Positive words add, negative words subtract
    IF @text LIKE '%good%' OR @text LIKE '%great%' OR @text LIKE '%excellent%'
        SET @sentiment_score = @sentiment_score + 0.3;
    IF @text LIKE '%bad%' OR @text LIKE '%poor%' OR @text LIKE '%terrible%'
        SET @sentiment_score = @sentiment_score - 0.3;
    IF @text LIKE '%improve%' OR @text LIKE '%better%' OR @text LIKE '%optimize%'
        SET @sentiment_score = @sentiment_score + 0.2;

    -- Clamp sentiment to [-1, 1]
    SET @sentiment_score = CASE
        WHEN @sentiment_score > 1 THEN 1
        WHEN @sentiment_score < -1 THEN -1
        ELSE @sentiment_score
    END;

    -- Complexity score (based on word length and sentence structure)
    DECLARE @complexity_score FLOAT =
        CASE
            WHEN @avg_word_length > 7 THEN 0.8
            WHEN @avg_word_length > 5 THEN 0.5
            ELSE 0.3
        END;

    -- Formality score (presence of technical/formal language)
    DECLARE @formality_score FLOAT =
        CASE
            WHEN @topic_technical > 0.5 OR @topic_scientific > 0.5 THEN 0.8
            WHEN @topic_business > 0.3 THEN 0.6
            ELSE 0.3
        END;

    -- Temporal relevance (default to 1.0 for now, will decay over time)
    DECLARE @temporal_relevance FLOAT = 1.0;
    DECLARE @reference_date DATETIME2 = SYSUTCDATETIME();

    -- Insert or update semantic features
    IF EXISTS (SELECT 1 FROM dbo.SemanticFeatures WHERE embedding_id = @embedding_id)
    BEGIN
        UPDATE dbo.SemanticFeatures
        SET
            topic_technical = @topic_technical,
            topic_business = @topic_business,
            topic_scientific = @topic_scientific,
            topic_creative = @topic_creative,
            sentiment_score = @sentiment_score,
            formality_score = @formality_score,
            complexity_score = @complexity_score,
            temporal_relevance = @temporal_relevance,
            reference_date = @reference_date,
            text_length = @text_length,
            word_count = @word_count,
            avg_word_length = @avg_word_length,
            computed_at = SYSUTCDATETIME()
        WHERE embedding_id = @embedding_id;
    END
    ELSE
    BEGIN
        INSERT INTO dbo.SemanticFeatures (
            embedding_id,
            topic_technical, topic_business, topic_scientific, topic_creative,
            sentiment_score, formality_score, complexity_score,
            temporal_relevance, reference_date,
            text_length, word_count, avg_word_length,
            computed_at
        ) VALUES (
            @embedding_id,
            @topic_technical, @topic_business, @topic_scientific, @topic_creative,
            @sentiment_score, @formality_score, @complexity_score,
            @temporal_relevance, @reference_date,
            @text_length, @word_count, @avg_word_length,
            SYSUTCDATETIME()
        );
    END;
END;
GO

-- ==========================================
-- PART 4: Batch Feature Computation
-- ==========================================

CREATE OR ALTER PROCEDURE sp_ComputeAllSemanticFeatures
AS
BEGIN
    SET NOCOUNT ON;

    PRINT 'Computing semantic features for all embeddings...';

    DECLARE @embedding_id BIGINT;
    DECLARE @count INT = 0;

    DECLARE cursor_embeddings CURSOR FOR
        SELECT embedding_id
        FROM dbo.Embeddings_Production
        WHERE source_text IS NOT NULL;

    OPEN cursor_embeddings;
    FETCH NEXT FROM cursor_embeddings INTO @embedding_id;

    WHILE @@FETCH_STATUS = 0
    BEGIN
        EXEC sp_ComputeSemanticFeatures @embedding_id = @embedding_id;

        SET @count = @count + 1;
        IF @count % 100 = 0
            PRINT '  Processed ' + CAST(@count AS VARCHAR) + ' embeddings...';

        FETCH NEXT FROM cursor_embeddings INTO @embedding_id;
    END;

    CLOSE cursor_embeddings;
    DEALLOCATE cursor_embeddings;

    PRINT '  ✓ Computed semantic features for ' + CAST(@count AS VARCHAR) + ' embeddings';

    -- Show feature distribution
    SELECT
        'Technical' as topic,
        AVG(topic_technical) as avg_score,
        MAX(topic_technical) as max_score,
        COUNT(CASE WHEN topic_technical > 0.5 THEN 1 END) as high_score_count
    FROM dbo.SemanticFeatures

    UNION ALL

    SELECT
        'Business' as topic,
        AVG(topic_business) as avg_score,
        MAX(topic_business) as max_score,
        COUNT(CASE WHEN topic_business > 0.5 THEN 1 END) as high_score_count
    FROM dbo.SemanticFeatures

    UNION ALL

    SELECT
        'Scientific' as topic,
        AVG(topic_scientific) as avg_score,
        MAX(topic_scientific) as max_score,
        COUNT(CASE WHEN topic_scientific > 0.5 THEN 1 END) as high_score_count
    FROM dbo.SemanticFeatures

    UNION ALL

    SELECT
        'Creative' as topic,
        AVG(topic_creative) as avg_score,
        MAX(topic_creative) as max_score,
        COUNT(CASE WHEN topic_creative > 0.5 THEN 1 END) as high_score_count
    FROM dbo.SemanticFeatures;
END;
GO

-- ==========================================
-- PART 5: Semantic-Filtered Vector Search
-- ==========================================

CREATE OR ALTER PROCEDURE sp_SemanticFilteredSearch
    @query_vector VECTOR(768),
    @top_k INT = 10,
    @topic_filter NVARCHAR(50) = NULL,  -- 'technical', 'business', 'scientific', 'creative'
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

    -- Build dynamic filter based on topic
    DECLARE @topic_column NVARCHAR(50) = 'topic_' + ISNULL(@topic_filter, 'technical');

    SELECT TOP (@top_k)
        e.embedding_id,
        e.source_text,
        VECTOR_DISTANCE('cosine', e.embedding_full, @query_vector) as vector_distance,
        sf.topic_technical,
        sf.topic_business,
        sf.topic_scientific,
        sf.topic_creative,
        sf.sentiment_score,
        sf.temporal_relevance
    FROM dbo.Embeddings_Production e
    JOIN dbo.SemanticFeatures sf ON e.embedding_id = sf.embedding_id
    WHERE e.embedding_full IS NOT NULL
        AND (@topic_filter IS NULL OR
            (@topic_filter = 'technical' AND sf.topic_technical >= @min_topic_score) OR
            (@topic_filter = 'business' AND sf.topic_business >= @min_topic_score) OR
            (@topic_filter = 'scientific' AND sf.topic_scientific >= @min_topic_score) OR
            (@topic_filter = 'creative' AND sf.topic_creative >= @min_topic_score))
        AND (@min_sentiment IS NULL OR sf.sentiment_score >= @min_sentiment)
        AND (@max_sentiment IS NULL OR sf.sentiment_score <= @max_sentiment)
        AND sf.temporal_relevance >= @min_temporal_relevance
    ORDER BY VECTOR_DISTANCE('cosine', e.embedding_full, @query_vector);

    PRINT '  ✓ Semantic-filtered search complete';
END;
GO

PRINT '';
PRINT '============================================================';
PRINT 'SEMANTIC FEATURE EXTRACTION SYSTEM CREATED';
PRINT '============================================================';
PRINT 'Tables:';
PRINT '  - SemanticFeatures (topic, sentiment, temporal scores)';
PRINT '  - TopicKeywords (keyword dictionaries)';
PRINT '';
PRINT 'Procedures:';
PRINT '  1. sp_ComputeSemanticFeatures - Compute for single embedding';
PRINT '  2. sp_ComputeAllSemanticFeatures - Batch compute all';
PRINT '  3. sp_SemanticFilteredSearch - Filter by semantic features';
PRINT '';
PRINT 'Usage:';
PRINT '  EXEC sp_ComputeAllSemanticFeatures;';
PRINT '  EXEC sp_SemanticFilteredSearch @query_vector, @topic_filter=''technical'';';
PRINT '============================================================';
GO
