-- Hartonomous Semantic Search with Spatial Optimization Strategy
-- Implements hybrid spatial filter + vector rerank for O(log n) + exact similarity
USE Hartonomous;
GO

CREATE OR ALTER PROCEDURE dbo.sp_SemanticSearch
    @query_text NVARCHAR(MAX) = NULL,
    @query_embedding VECTOR(768) = NULL,
    @top_k INT = 5,
    @category NVARCHAR(50) = NULL,
    @use_hybrid BIT = 1
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @start_time DATETIME2 = SYSUTCDATETIME();
    DECLARE @inference_id BIGINT;
    DECLARE @result_count INT = 0;

    IF @query_embedding IS NULL
    BEGIN
        IF @query_text IS NULL
        BEGIN
            RAISERROR('Either @query_embedding or @query_text must be provided.', 16, 1);
            RETURN;
        END;

        RAISERROR('Text-to-embedding inference not implemented. Please provide @query_embedding parameter.', 16, 1);
        RETURN;
    END;

    DECLARE @input_data JSON = CASE
        WHEN @query_text IS NULL THEN NULL
        ELSE CAST(JSON_OBJECT('queryText': @query_text) AS JSON)
    END;

    DECLARE @input_hash BINARY(32) = CASE
        WHEN @query_text IS NULL THEN NULL
        ELSE HASHBYTES('SHA2_256', CONVERT(VARBINARY(MAX), @query_text))
    END;

    DECLARE @search_method NVARCHAR(50) = CASE WHEN @use_hybrid = 1 THEN 'hybrid_spatial_vector' ELSE 'vector_only' END;

    INSERT INTO dbo.InferenceRequests (
        TaskType,
        InputData,
        InputHash,
        ModelsUsed,
        EnsembleStrategy
    )
    VALUES (
        'semantic_search',
        @input_data,
        @input_hash,
        @search_method,
        'spatial_filter_vector_rerank'
    );

    SET @inference_id = SCOPE_IDENTITY();

    DECLARE @Results TABLE
    (
        AtomEmbeddingId BIGINT NOT NULL,
        AtomId BIGINT NOT NULL,
        Modality NVARCHAR(128) NULL,
        Subtype NVARCHAR(128) NULL,
        SourceType NVARCHAR(128) NULL,
        SourceUri NVARCHAR(2048) NULL,
        CanonicalText NVARCHAR(MAX) NULL,
        EmbeddingType NVARCHAR(128) NULL,
        ModelId INT NULL,
        ExactDistance FLOAT NULL,
        SimilarityScore FLOAT NULL,
        SpatialDistance FLOAT NULL,
        SearchMethod NVARCHAR(50) NOT NULL
    );

    IF @use_hybrid = 1
    BEGIN
        DECLARE @spatial_x FLOAT = 0.0;
        DECLARE @spatial_y FLOAT = 0.0;
        DECLARE @spatial_z FLOAT = 0.0;
        DECLARE @spatial_candidates INT = CASE WHEN @top_k IS NULL OR @top_k <= 0 THEN 10 ELSE @top_k * 10 END;

        EXEC dbo.sp_ComputeSpatialProjection
            @input_vector = @query_embedding,
            @input_dimension = 768,
            @output_x = @spatial_x OUTPUT,
            @output_y = @spatial_y OUTPUT,
            @output_z = @spatial_z OUTPUT;

        DECLARE @Hybrid TABLE
        (
            AtomEmbeddingId BIGINT,
            AtomId BIGINT,
            Modality NVARCHAR(128),
            Subtype NVARCHAR(128),
            SourceUri NVARCHAR(2048),
            SourceType NVARCHAR(128),
            EmbeddingType NVARCHAR(128),
            ModelId INT,
            exact_distance FLOAT,
            spatial_distance FLOAT
        );

        INSERT INTO @Hybrid
        EXEC dbo.sp_HybridSearch
            @query_vector = @query_embedding,
            @query_spatial_x = @spatial_x,
            @query_spatial_y = @spatial_y,
            @query_spatial_z = @spatial_z,
            @spatial_candidates = @spatial_candidates,
            @final_top_k = @top_k;

        INSERT INTO @Results
        (
            AtomEmbeddingId,
            AtomId,
            Modality,
            Subtype,
            SourceType,
            SourceUri,
            CanonicalText,
            EmbeddingType,
            ModelId,
            ExactDistance,
            SimilarityScore,
            SpatialDistance,
            SearchMethod
        )
        SELECT
            h.AtomEmbeddingId,
            h.AtomId,
            h.Modality,
            h.Subtype,
            h.SourceType,
            h.SourceUri,
            a.CanonicalText,
            h.EmbeddingType,
            h.ModelId,
            h.exact_distance,
            CASE WHEN h.exact_distance IS NULL THEN NULL ELSE 1.0 - h.exact_distance END,
            h.spatial_distance,
            'HYBRID_SPATIAL_VECTOR'
        FROM @Hybrid AS h
        INNER JOIN dbo.Atoms AS a ON a.AtomId = h.AtomId
        WHERE @category IS NULL
           OR a.SourceType = @category
           OR a.Subtype = @category;
    END
    ELSE
    BEGIN
        INSERT INTO @Results
        (
            AtomEmbeddingId,
            AtomId,
            Modality,
            Subtype,
            SourceType,
            SourceUri,
            CanonicalText,
            EmbeddingType,
            ModelId,
            ExactDistance,
            SimilarityScore,
            SpatialDistance,
            SearchMethod
        )
        SELECT TOP (@top_k)
            ae.AtomEmbeddingId,
            ae.AtomId,
            a.Modality,
            a.Subtype,
            a.SourceType,
            a.SourceUri,
            a.CanonicalText,
            ae.EmbeddingType,
            ae.ModelId,
            VECTOR_DISTANCE('cosine', ae.EmbeddingVector, @query_embedding) AS distance,
            1.0 - VECTOR_DISTANCE('cosine', ae.EmbeddingVector, @query_embedding) AS similarity,
            NULL,
            'VECTOR_ONLY'
        FROM dbo.AtomEmbeddings AS ae
        INNER JOIN dbo.Atoms AS a ON a.AtomId = ae.AtomId
        WHERE ae.EmbeddingVector IS NOT NULL
          AND (@category IS NULL OR a.SourceType = @category OR a.Subtype = @category)
        ORDER BY distance ASC;
    END;

    DECLARE @duration_ms INT = DATEDIFF(MILLISECOND, @start_time, SYSUTCDATETIME());
    SET @result_count = (SELECT COUNT(*) FROM @Results);

    DECLARE @output_metadata JSON = CAST(JSON_OBJECT(
        'status': 'completed',
        'results_count': @result_count,
        'search_method': @search_method,
        'duration_ms': @duration_ms
    ) AS JSON);

    UPDATE dbo.InferenceRequests
    SET
        TotalDurationMs = @duration_ms,
        OutputMetadata = @output_metadata,
        CacheHit = 0
    WHERE InferenceId = @inference_id;

    SELECT
        r.AtomEmbeddingId AS atom_embedding_id,
        r.AtomId AS atom_id,
        r.Modality,
        r.Subtype,
        r.SourceType,
        r.SourceUri,
        r.CanonicalText,
        r.EmbeddingType,
        r.ModelId,
        r.ExactDistance AS cosine_distance,
        r.SimilarityScore,
        r.SpatialDistance,
        r.SearchMethod,
        @inference_id AS inference_id
    FROM @Results AS r
    ORDER BY r.ExactDistance;

    SELECT @inference_id AS inference_id, @duration_ms AS duration_ms, @result_count AS results_count;
END;
GO

PRINT 'Hartonomous semantic search procedure with spatial optimization implemented.';
PRINT 'Novel approach: Spatial O(log n) filter + Vector exact rerank';
GO
