CREATE PROCEDURE dbo.sp_ComputeAllSemanticFeatures
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
        WHERE a.Modality = 'text' AND a.AtomicValue IS NOT NULL; -- Changed: CanonicalText → Modality filter

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