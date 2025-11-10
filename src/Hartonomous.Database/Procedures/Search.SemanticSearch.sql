-- Hybrid spatial + vector semantic search pipeline.

CREATE OR ALTER PROCEDURE dbo.sp_SemanticSearch
    @query_text NVARCHAR(MAX) = NULL,
    @query_embedding VECTOR(1998) = NULL,
    @query_dimension INT = 768,
    @top_k INT = 5,
    @category NVARCHAR(50) = NULL,
    @use_hybrid BIT = 1
AS
BEGIN
    SET NOCOUNT ON;



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

        WHEN @query_text IS NULL THEN NULL
        ELSE CAST(JSON_OBJECT('queryText': @query_text) AS JSON)
    END;

        WHEN @query_text IS NULL THEN NULL
        ELSE HASHBYTES('SHA2_256', CONVERT(VARBINARY(MAX), @query_text))
    END;


    

    SET @InferenceId = SCOPE_IDENTITY();

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




        EXEC dbo.sp_ComputeSpatialProjection
            @input_vector = @query_embedding,
            @input_dimension = @query_dimension,
            @output_x = @spatial_x OUTPUT,
            @output_y = @spatial_y OUTPUT,
            @output_z = @spatial_z OUTPUT;

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

        

        
    END
    ELSE
    BEGIN
        
    END;

    SET @ResultCount = (SELECT COUNT(*) FROM @Results);

        'status': 'completed',
        'results_count': @ResultCount,
        'search_method': @search_method,
        'duration_ms': @DurationMs
    ) AS JSON);

    UPDATE dbo.InferenceRequests
    SET
        TotalDurationMs = @DurationMs,
        OutputMetadata = @OutputMetadata,
        CacheHit = 0
    WHERE InferenceId = @InferenceId;

    SELECT
        r.AtomEmbeddingId AS AtomEmbeddingId,
        r.AtomId AS AtomId,
        r.Modality,
        r.Subtype,
        r.SourceType,
        r.SourceUri,
        r.CanonicalText,
        r.EmbeddingType,
        r.ModelId,
        r.ExactDistance AS CosineDistance,
        r.SimilarityScore,
        r.SpatialDistance,
        r.SearchMethod,
        @InferenceId AS InferenceId
    FROM @Results AS r
    ORDER BY r.ExactDistance;

    SELECT @InferenceId AS InferenceId, @DurationMs AS DurationMs, @ResultCount AS ResultsCount;
END;
