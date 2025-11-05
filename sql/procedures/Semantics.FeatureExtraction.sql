SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;
GO

PRINT '============================================================';
PRINT 'SEMANTIC FEATURE EXTRACTION SYSTEM';
PRINT 'Computing Semantic Coordinates from Raw Data in T-SQL';
PRINT '============================================================';
GO

IF OBJECT_ID('dbo.SemanticFeatures', 'U') IS NOT NULL
BEGIN
    IF COL_LENGTH('dbo.SemanticFeatures', 'AtomEmbeddingId') IS NULL
    BEGIN
        PRINT 'Dropping legacy SemanticFeatures table (schema change).';
        DROP TABLE dbo.SemanticFeatures;
    END
END

IF OBJECT_ID('dbo.SemanticFeatures', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.SemanticFeatures (
        AtomEmbeddingId BIGINT NOT NULL PRIMARY KEY,
        TopicTechnical FLOAT DEFAULT 0,
        TopicBusiness FLOAT DEFAULT 0,
        TopicScientific FLOAT DEFAULT 0,
        TopicCreative FLOAT DEFAULT 0,
        SentimentScore FLOAT DEFAULT 0,
        FormalityScore FLOAT DEFAULT 0,
        ComplexityScore FLOAT DEFAULT 0,
        TemporalRelevance FLOAT DEFAULT 1,
        ReferenceDate DATETIME2,
        TextLength INT,
        WordCount INT,
        UniqueWordRatio FLOAT,
        AvgWordLength FLOAT,
        ComputedAt DATETIME2 DEFAULT SYSUTCDATETIME(),
        CONSTRAINT FK_SemanticFeatures_AtomEmbeddings FOREIGN KEY (AtomEmbeddingId)
            REFERENCES dbo.AtomEmbeddings(AtomEmbeddingId) ON DELETE CASCADE
    );

    PRINT 'Created SemanticFeatures table (Atom substrate).';
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'ix_semantic_topic_technical' AND object_id = OBJECT_ID('dbo.SemanticFeatures'))
    CREATE INDEX ix_semantic_topic_technical ON dbo.SemanticFeatures(TopicTechnical) WHERE TopicTechnical > 0.5;

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'ix_semantic_topic_business' AND object_id = OBJECT_ID('dbo.SemanticFeatures'))
    CREATE INDEX ix_semantic_topic_business ON dbo.SemanticFeatures(TopicBusiness) WHERE TopicBusiness > 0.5;

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'ix_semantic_topic_scientific' AND object_id = OBJECT_ID('dbo.SemanticFeatures'))
    CREATE INDEX ix_semantic_topic_scientific ON dbo.SemanticFeatures(TopicScientific) WHERE TopicScientific > 0.5;

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'ix_semantic_topic_creative' AND object_id = OBJECT_ID('dbo.SemanticFeatures'))
    CREATE INDEX ix_semantic_topic_creative ON dbo.SemanticFeatures(TopicCreative) WHERE TopicCreative > 0.5;

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'ix_semantic_sentiment' AND object_id = OBJECT_ID('dbo.SemanticFeatures'))
    CREATE INDEX ix_semantic_sentiment ON dbo.SemanticFeatures(SentimentScore);

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'ix_semantic_temporal' AND object_id = OBJECT_ID('dbo.SemanticFeatures'))
    CREATE INDEX ix_semantic_temporal ON dbo.SemanticFeatures(TemporalRelevance) WHERE TemporalRelevance > 0.5;
GO

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

IF NOT EXISTS (SELECT 1 FROM dbo.TopicKeywords WHERE topic_name = 'technical')
BEGIN
    INSERT INTO dbo.TopicKeywords (topic_name, keyword, weight) VALUES
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

CREATE OR ALTER PROCEDURE dbo.sp_ComputeSemanticFeatures
    @atom_embedding_id BIGINT
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @text NVARCHAR(MAX);
    SELECT
        @text = LOWER(a.CanonicalText)
    FROM dbo.AtomEmbeddings AS ae
    INNER JOIN dbo.Atoms AS a ON a.AtomId = ae.AtomId
    WHERE ae.AtomEmbeddingId = @atom_embedding_id;

    IF @text IS NULL
    BEGIN
        PRINT 'Atom embedding not found or lacks canonical text: ' + CAST(@atom_embedding_id AS NVARCHAR(40));
        RETURN;
    END;

    DECLARE @features_json NVARCHAR(MAX) = dbo.clr_SemanticFeaturesJson(@text);

    IF @features_json IS NULL
    BEGIN
        RAISERROR('Semantic feature computation failed for AtomEmbeddingId %d.', 16, 1, @atom_embedding_id);
        RETURN;
    END;

    DECLARE @topic_technical FLOAT = TRY_CAST(JSON_VALUE(@features_json, '$.topicTechnical') AS FLOAT);
    DECLARE @topic_business FLOAT = TRY_CAST(JSON_VALUE(@features_json, '$.topicBusiness') AS FLOAT);
    DECLARE @topic_scientific FLOAT = TRY_CAST(JSON_VALUE(@features_json, '$.topicScientific') AS FLOAT);
    DECLARE @topic_creative FLOAT = TRY_CAST(JSON_VALUE(@features_json, '$.topicCreative') AS FLOAT);
    DECLARE @sentiment_score FLOAT = TRY_CAST(JSON_VALUE(@features_json, '$.sentimentScore') AS FLOAT);
    DECLARE @formality_score FLOAT = TRY_CAST(JSON_VALUE(@features_json, '$.formalityScore') AS FLOAT);
    DECLARE @complexity_score FLOAT = TRY_CAST(JSON_VALUE(@features_json, '$.complexityScore') AS FLOAT);
    DECLARE @temporal_relevance FLOAT = TRY_CAST(JSON_VALUE(@features_json, '$.temporalRelevance') AS FLOAT);
    DECLARE @text_length INT = TRY_CAST(JSON_VALUE(@features_json, '$.textLength') AS INT);
    DECLARE @word_count INT = TRY_CAST(JSON_VALUE(@features_json, '$.wordCount') AS INT);
    DECLARE @avg_word_length FLOAT = TRY_CAST(JSON_VALUE(@features_json, '$.avgWordLength') AS FLOAT);
    DECLARE @unique_word_ratio FLOAT = TRY_CAST(JSON_VALUE(@features_json, '$.uniqueWordRatio') AS FLOAT);

    SET @topic_technical = ISNULL(@topic_technical, 0);
    SET @topic_business = ISNULL(@topic_business, 0);
    SET @topic_scientific = ISNULL(@topic_scientific, 0);
    SET @topic_creative = ISNULL(@topic_creative, 0);
    SET @sentiment_score = ISNULL(@sentiment_score, 0);
    SET @formality_score = ISNULL(@formality_score, 0.3);
    SET @complexity_score = ISNULL(@complexity_score, 0.3);
    SET @temporal_relevance = ISNULL(@temporal_relevance, 1.0);
    SET @text_length = ISNULL(@text_length, LEN(@text));
    SET @word_count = ISNULL(@word_count, CASE WHEN LEN(LTRIM(RTRIM(@text))) = 0 THEN 0 ELSE LEN(@text) - LEN(REPLACE(@text, ' ', '')) + 1 END);
    SET @avg_word_length = ISNULL(@avg_word_length, CASE WHEN @word_count = 0 THEN 0 ELSE CAST(@text_length AS FLOAT) / NULLIF(@word_count, 0) END);
    SET @unique_word_ratio = ISNULL(@unique_word_ratio, 0);
    DECLARE @reference_date DATETIME2 = SYSUTCDATETIME();

    IF EXISTS (SELECT 1 FROM dbo.SemanticFeatures WHERE AtomEmbeddingId = @atom_embedding_id)
    BEGIN
        UPDATE dbo.SemanticFeatures
        SET
            TopicTechnical = @topic_technical,
            TopicBusiness = @topic_business,
            TopicScientific = @topic_scientific,
            TopicCreative = @topic_creative,
            SentimentScore = @sentiment_score,
            FormalityScore = @formality_score,
            ComplexityScore = @complexity_score,
            TemporalRelevance = @temporal_relevance,
            ReferenceDate = @reference_date,
            TextLength = @text_length,
            WordCount = @word_count,
            AvgWordLength = @avg_word_length,
            UniqueWordRatio = @unique_word_ratio,
            ComputedAt = SYSUTCDATETIME()
        WHERE AtomEmbeddingId = @atom_embedding_id;
    END
    ELSE
    BEGIN
        INSERT INTO dbo.SemanticFeatures (
            AtomEmbeddingId,
            TopicTechnical, TopicBusiness, TopicScientific, TopicCreative,
            SentimentScore, FormalityScore, ComplexityScore,
            TemporalRelevance, ReferenceDate,
            TextLength, WordCount, AvgWordLength, UniqueWordRatio,
            ComputedAt
        )
        VALUES (
            @atom_embedding_id,
            @topic_technical, @topic_business, @topic_scientific, @topic_creative,
            @sentiment_score, @formality_score, @complexity_score,
            @temporal_relevance, @reference_date,
            @text_length, @word_count, @avg_word_length, @unique_word_ratio,
            SYSUTCDATETIME()
        );
    END;
END
GO

CREATE OR ALTER PROCEDURE dbo.sp_ComputeAllSemanticFeatures
AS
BEGIN
    SET NOCOUNT ON;

    PRINT 'Computing semantic features for all embeddings...';

    DECLARE @atom_embedding_id BIGINT;
    DECLARE @count INT = 0;

    DECLARE cursor_embeddings CURSOR FOR
        SELECT ae.AtomEmbeddingId
        FROM dbo.AtomEmbeddings AS ae
        INNER JOIN dbo.Atoms AS a ON a.AtomId = ae.AtomId
        WHERE a.CanonicalText IS NOT NULL
          AND LEN(LTRIM(RTRIM(a.CanonicalText))) > 0;

    OPEN cursor_embeddings;
    FETCH NEXT FROM cursor_embeddings INTO @atom_embedding_id;

    WHILE @@FETCH_STATUS = 0
    BEGIN
        EXEC dbo.sp_ComputeSemanticFeatures @atom_embedding_id = @atom_embedding_id;

        SET @count = @count + 1;
        IF @count % 100 = 0
            PRINT '  Processed ' + CAST(@count AS VARCHAR) + ' embeddings...';

        FETCH NEXT FROM cursor_embeddings INTO @atom_embedding_id;
    END;

    CLOSE cursor_embeddings;
    DEALLOCATE cursor_embeddings;

    PRINT '  ✓ Computed semantic features for ' + CAST(@count AS VARCHAR) + ' embeddings';

    SELECT
        'Technical' as topic,
        AVG(TopicTechnical) as avg_score,
        MAX(TopicTechnical) as max_score,
        COUNT(CASE WHEN TopicTechnical > 0.5 THEN 1 END) as high_score_count
    FROM dbo.SemanticFeatures

    UNION ALL

    SELECT
        'Business' as topic,
        AVG(TopicBusiness) as avg_score,
        MAX(TopicBusiness) as max_score,
        COUNT(CASE WHEN TopicBusiness > 0.5 THEN 1 END) as high_score_count
    FROM dbo.SemanticFeatures

    UNION ALL

    SELECT
        'Scientific' as topic,
        AVG(TopicScientific) as avg_score,
        MAX(TopicScientific) as max_score,
        COUNT(CASE WHEN TopicScientific > 0.5 THEN 1 END) as high_score_count
    FROM dbo.SemanticFeatures

    UNION ALL

    SELECT
        'Creative' as topic,
        AVG(TopicCreative) as avg_score,
        MAX(TopicCreative) as max_score,
        COUNT(CASE WHEN TopicCreative > 0.5 THEN 1 END) as high_score_count
    FROM dbo.SemanticFeatures;
END
GO

CREATE OR ALTER PROCEDURE dbo.sp_SemanticFilteredSearch
    @query_vector VECTOR(1998),
    @top_k INT = 10,
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
        a.SourceType,
        a.SourceUri,
        a.CanonicalText,
        VECTOR_DISTANCE('cosine', ae.EmbeddingVector, @query_vector) AS vector_distance,
        sf.TopicTechnical,
        sf.TopicBusiness,
        sf.TopicScientific,
        sf.TopicCreative,
        sf.SentimentScore,
        sf.TemporalRelevance
    FROM dbo.AtomEmbeddings AS ae
    INNER JOIN dbo.SemanticFeatures AS sf ON sf.AtomEmbeddingId = ae.AtomEmbeddingId
    INNER JOIN dbo.Atoms AS a ON a.AtomId = ae.AtomId
    WHERE ae.EmbeddingVector IS NOT NULL
        AND (@topic_filter IS NULL OR
            (@topic_filter = 'technical' AND sf.TopicTechnical >= @min_topic_score) OR
            (@topic_filter = 'business' AND sf.TopicBusiness >= @min_topic_score) OR
            (@topic_filter = 'scientific' AND sf.TopicScientific >= @min_topic_score) OR
            (@topic_filter = 'creative' AND sf.TopicCreative >= @min_topic_score))
        AND (@min_sentiment IS NULL OR sf.SentimentScore >= @min_sentiment)
        AND (@max_sentiment IS NULL OR sf.SentimentScore <= @max_sentiment)
        AND sf.TemporalRelevance >= @min_temporal_relevance
    ORDER BY VECTOR_DISTANCE('cosine', ae.EmbeddingVector, @query_vector);

    PRINT '  ✓ Semantic-filtered search complete';
END
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
