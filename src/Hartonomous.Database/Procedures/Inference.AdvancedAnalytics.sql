
CREATE OR ALTER PROCEDURE dbo.sp_MultiResolutionSearch
    @query_x FLOAT,
    @query_y FLOAT,
    @query_z FLOAT,
    @coarse_candidates INT = 1000,
    @fine_candidates INT = 100,
    @final_top_k INT = 10
AS
BEGIN
    SET NOCOUNT ON;


CREATE OR ALTER PROCEDURE dbo.sp_CognitiveActivation
    @query_embedding VECTOR(1998),
    @activation_threshold FLOAT = 0.8,
    @max_activated INT = 50
AS
BEGIN
    SET NOCOUNT ON;

    IF @query_embedding IS NULL
    BEGIN
        ;THROW 50010, 'Query embedding cannot be NULL.', 1;
    END;

    IF @activation_threshold IS NULL OR @activation_threshold <= -1.0 OR @activation_threshold > 1.0
    BEGIN
        ;THROW 50011, 'Activation threshold must be within (-1, 1].', 1;
    END;


    IF @max_distance <= 0
    BEGIN
        ;THROW 50012, 'Activation threshold too high for cosine similarity search.', 1;
    END;

    PRINT '  Threshold: ' + CAST(@activation_threshold AS NVARCHAR(10)) + ' | Max candidates: ' + CAST(@max_activated AS NVARCHAR(10));

    

CREATE OR ALTER PROCEDURE dbo.sp_DynamicStudentExtraction
    @ParentModelId INT,
    @target_size_ratio FLOAT = 0.5,
    @selection_strategy NVARCHAR(20) = 'importance'
AS
BEGIN
    SET NOCOUNT ON;

    IF @target_size_ratio <= 0 OR @target_size_ratio > 1
    BEGIN
        ;THROW 50020, 'Target size ratio must be within (0, 1].', 1;
    END;

        SELECT COUNT(*)
        FROM dbo.Models
        WHERE ModelId = @ParentModelId
    );

    IF @parent_exists = 0
    BEGIN
        ;THROW 50021, 'Parent model does not exist.', 1;
    END;

    IF @ratio_percent < 1 SET @ratio_percent = 1;



    IF @selection_strategy = 'layer'
    BEGIN

            SELECT COUNT(*)
            FROM dbo.ModelLayers
            WHERE ModelId = @ParentModelId
        );

        IF @total_layers = 0
        BEGIN
            ;THROW 50022, 'Parent model has no layers to extract.', 1;
        END;

        IF @layers_to_take < 1 SET @layers_to_take = 1;

        SELECT @layer_subset = STRING_AGG(CAST(LayerIdx AS NVARCHAR(10)), ',') WITHIN GROUP (ORDER BY LayerIdx)
        FROM (
            SELECT TOP (@layers_to_take) LayerIdx
            FROM dbo.ModelLayers
            WHERE ModelId = @ParentModelId
            ORDER BY LayerIdx
        ) AS layer_selection;

        PRINT 'DYNAMIC EXTRACTION → Layer subset: ' + ISNULL(@layer_subset, '(empty)');
    END
    ELSE IF @selection_strategy = 'random'
    BEGIN

            SELECT COUNT(*)
            FROM dbo.ModelLayers
            WHERE ModelId = @ParentModelId
        );

        IF @total_layers_random = 0
        BEGIN
            ;THROW 50023, 'Parent model has no layers to extract.', 1;
        END;

        IF @random_take < 1 SET @random_take = 1;

        SELECT @layer_subset = STRING_AGG(CAST(LayerIdx AS NVARCHAR(10)), ',')
        FROM (
            SELECT TOP (@random_take) LayerIdx
            FROM dbo.ModelLayers
            WHERE ModelId = @ParentModelId
            ORDER BY NEWID()
        ) AS random_layers;

        PRINT 'DYNAMIC EXTRACTION → Random layer subset: ' + ISNULL(@layer_subset, '(empty)');
    END
    ELSE
    BEGIN

            SELECT COUNT(*)
            FROM dbo.TensorAtoms
            WHERE ModelId = @ParentModelId
        );

        IF @total_atoms = 0
        BEGIN
            SET @importance_threshold = NULL;
        END
        ELSE
        BEGIN

            IF @atoms_to_take < 1 SET @atoms_to_take = 1;

            SELECT @importance_threshold = MIN(ImportanceScore)
            FROM (
                SELECT TOP (@atoms_to_take) ImportanceScore
                FROM dbo.TensorAtoms
                WHERE ModelId = @ParentModelId
                  AND ImportanceScore IS NOT NULL
                ORDER BY ImportanceScore DESC
            ) AS ranked;

            IF @importance_threshold IS NULL
            BEGIN
                END;
        END;

        PRINT 'DYNAMIC EXTRACTION → Importance threshold: ' + COALESCE(CAST(@importance_threshold AS NVARCHAR(32)), 'none');
    END;

    EXEC dbo.sp_ExtractStudentModel
        @ParentModelId = @ParentModelId,
        @layer_subset = @layer_subset,
        @importance_threshold = @importance_threshold,
        @NewModelName = @NewModelName;
END

CREATE OR ALTER PROCEDURE dbo.sp_CrossModalQuery
    @text_query NVARCHAR(MAX) = NULL,
    @spatial_query_x FLOAT = NULL,
    @spatial_query_y FLOAT = NULL,
    @spatial_query_z FLOAT = NULL,
    @modality_filter NVARCHAR(50) = NULL,
    @top_k INT = 10
AS
BEGIN
    SET NOCOUNT ON;

    PRINT '  Text filter: ' + ISNULL(@text_query, '(none)');
    PRINT '  Target modality: ' + ISNULL(@modality_filter, 'all');

    IF @spatial_query_x IS NOT NULL AND @spatial_query_y IS NOT NULL
    BEGIN



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
            ae.SpatialGeometry.STDistance(@query_pt) AS SpatialDistance
        FROM dbo.AtomEmbeddings AS ae
        INNER JOIN dbo.Atoms AS a ON a.AtomId = ae.AtomId
        WHERE ae.SpatialGeometry IS NOT NULL
          AND (@modality_filter IS NULL OR a.SourceType = @modality_filter OR a.Modality = @modality_filter)
          AND (@text_query IS NULL OR a.CanonicalText LIKE '%' + @text_query + '%')
        ORDER BY ae.SpatialGeometry.STDistance(@query_pt);
    END
    ELSE
    BEGIN
        SELECT TOP (@top_k)
            ae.AtomEmbeddingId,
            ae.AtomId,
            a.Modality,
            a.Subtype,
            a.SourceType,
            a.SourceUri,
            a.CanonicalText,
            ae.EmbeddingType,
            ae.ModelId
        FROM dbo.AtomEmbeddings AS ae
        INNER JOIN dbo.Atoms AS a ON a.AtomId = ae.AtomId
        WHERE (@modality_filter IS NULL OR a.SourceType = @modality_filter OR a.Modality = @modality_filter)
          AND (@text_query IS NULL OR a.CanonicalText LIKE '%' + @text_query + '%')
        ORDER BY NEWID();
    END;

    END

CREATE OR ALTER PROCEDURE dbo.sp_CompareModelKnowledge
    @ModelAId INT,
    @ModelBId INT
AS
BEGIN
    SET NOCOUNT ON;

    PRINT 'KNOWLEDGE COMPARISON: Model ' + CAST(@ModelAId AS NVARCHAR(20)) + ' vs Model ' + CAST(@ModelBId AS NVARCHAR(20));

    SELECT
        'Model A' AS model_name,
        @ModelAId AS model_id,
        COUNT(*) AS total_tensor_atoms,
        AVG(CAST(ImportanceScore AS FLOAT)) AS avg_importance,
        STDEV(CAST(ImportanceScore AS FLOAT)) AS stdev_importance,
        MIN(ImportanceScore) AS min_importance,
        MAX(ImportanceScore) AS max_importance
    FROM dbo.TensorAtoms
    WHERE ModelId = @ModelAId

    UNION ALL

    SELECT
        'Model B' AS model_name,
        @ModelBId AS model_id,
        COUNT(*) AS total_tensor_atoms,
        AVG(CAST(ImportanceScore AS FLOAT)) AS avg_importance,
        STDEV(CAST(ImportanceScore AS FLOAT)) AS stdev_importance,
        MIN(ImportanceScore) AS min_importance,
        MAX(ImportanceScore) AS max_importance
    FROM dbo.TensorAtoms
    WHERE ModelId = @ModelBId;

    SELECT
        'Layer Comparison' AS analysis_type,
        COALESCE(a.LayerIdx, b.LayerIdx) AS layer_idx,
        a.LayerType AS model_a_type,
        b.LayerType AS model_b_type,
        a.ParameterCount AS model_a_parameters,
        b.ParameterCount AS model_b_parameters,
        a.TensorShape AS model_a_shape,
        b.TensorShape AS model_b_shape
    FROM dbo.ModelLayers AS a
    FULL OUTER JOIN dbo.ModelLayers AS b
        ON a.LayerIdx = b.LayerIdx
       AND a.ModelId = @ModelAId
       AND b.ModelId = @ModelBId
    WHERE a.ModelId = @ModelAId
       OR b.ModelId = @ModelBId
    ORDER BY COALESCE(a.LayerIdx, b.LayerIdx);

    SELECT
        'Coefficient Coverage' AS analysis_type,
        stats.model_id,
        stats.total_coefficients,
        stats.avg_value,
        stats.max_value,
        stats.min_value
    FROM (
        SELECT
            ta.ModelId AS model_id,
            COUNT(tc.Coefficient) AS total_coefficients,
            AVG(CAST(tc.Coefficient AS FLOAT)) AS avg_value,
            MAX(tc.Coefficient) AS max_value,
            MIN(tc.Coefficient) AS min_value
        FROM dbo.TensorAtoms AS ta
        LEFT JOIN dbo.TensorAtomCoefficients AS tc ON tc.TensorAtomId = ta.TensorAtomId
        WHERE ta.ModelId IN (@ModelAId, @ModelBId)
        GROUP BY ta.ModelId
    ) AS stats
    ORDER BY stats.model_id;

    END

CREATE OR ALTER PROCEDURE dbo.sp_InferenceHistory
    @time_window_hours INT = 24,
    @TaskType NVARCHAR(50) = NULL
AS
BEGIN
    SET NOCOUNT ON;

    PRINT '  Time window: Last ' + CAST(@time_window_hours AS VARCHAR(10)) + ' hours';

    SELECT
        TaskType,
        COUNT(*) AS request_count,
        AVG(TotalDurationMs) AS avg_duration_ms,
        MIN(TotalDurationMs) AS min_duration_ms,
        MAX(TotalDurationMs) AS max_duration_ms,
        SUM(CASE WHEN OutputData IS NOT NULL THEN 1 ELSE 0 END) AS successful_count,
        SUM(CASE WHEN OutputData IS NULL THEN 1 ELSE 0 END) AS failed_count,
        SUM(CASE WHEN CacheHit = 1 THEN 1 ELSE 0 END) AS cache_hits
    FROM dbo.InferenceRequests
    WHERE RequestTimestamp >= @cutoff_time
        AND (@TaskType IS NULL OR TaskType = @TaskType)
    GROUP BY TaskType
    ORDER BY request_count DESC;

    END
