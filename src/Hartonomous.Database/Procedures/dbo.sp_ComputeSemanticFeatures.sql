CREATE PROCEDURE dbo.sp_ComputeSemanticFeatures
    @atom_embedding_id BIGINT
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @text NVARCHAR(MAX);
    SELECT
        @text = CONVERT(NVARCHAR(256), a.AtomicValue) -- Derived from AtomicValue
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