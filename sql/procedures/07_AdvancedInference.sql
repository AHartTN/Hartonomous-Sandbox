-- ADVANCED INFERENCE PROCEDURES
-- Novel query patterns exploiting SQL Server 2025 native capabilities
USE Hartonomous;
GO

PRINT '============================================================';
PRINT 'ADVANCED INFERENCE & COGNITIVE QUERY OPERATIONS';
PRINT '============================================================';
GO

-- ==========================================
-- PART 1: Multi-Resolution Spatial Search
-- ==========================================

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

    PRINT 'MULTI-RESOLUTION SEARCH: Coarse → Fine → Exact';
    PRINT '  Strategy: 3-stage funnel for billion-scale performance';

    DECLARE @query_wkt NVARCHAR(200) = CONCAT('POINT (', @query_x, ' ', @query_y, ' ', @query_z, ')');
    DECLARE @query_pt GEOMETRY = geometry::STGeomFromText(@query_wkt, 0);

    -- Stage 1: Coarse spatial filter (quantized grid)
    DECLARE @coarse_results TABLE (AtomEmbeddingId BIGINT PRIMARY KEY);

    INSERT INTO @coarse_results (AtomEmbeddingId)
    SELECT TOP (@coarse_candidates) ae.AtomEmbeddingId
    FROM dbo.AtomEmbeddings AS ae
    WHERE ae.SpatialCoarse IS NOT NULL
    ORDER BY ae.SpatialCoarse.STDistance(@query_pt);

    DECLARE @coarse_count INT = @@ROWCOUNT;
    PRINT '  Stage 1: ' + CAST(@coarse_count AS NVARCHAR(10)) + ' coarse candidates';

    -- Stage 2: Fine spatial refinement
    DECLARE @fine_results TABLE (AtomEmbeddingId BIGINT PRIMARY KEY);

    INSERT INTO @fine_results (AtomEmbeddingId)
    SELECT TOP (@fine_candidates) ae.AtomEmbeddingId
    FROM dbo.AtomEmbeddings AS ae
    INNER JOIN @coarse_results AS cr ON cr.AtomEmbeddingId = ae.AtomEmbeddingId
    WHERE ae.SpatialGeometry IS NOT NULL
    ORDER BY ae.SpatialGeometry.STDistance(@query_pt);

    DECLARE @fine_count INT = @@ROWCOUNT;
    PRINT '  Stage 2: ' + CAST(@fine_count AS NVARCHAR(10)) + ' fine candidates';

    -- Stage 3: Exact vector rerank (spatial distance ranking shown)
    SELECT TOP (@final_top_k)
        ae.AtomEmbeddingId,
        ae.AtomId,
        a.Modality,
        a.Subtype,
        a.SourceType,
        a.SourceUri,
        a.CanonicalText,
        ae.EmbeddingType,
        ae.ModelId,
        ae.SpatialGeometry.STDistance(@query_pt) AS SpatialDistance,
        ae.SpatialCoarse.STDistance(@query_pt) AS CoarseDistance
    FROM dbo.AtomEmbeddings AS ae
    INNER JOIN @fine_results AS fr ON fr.AtomEmbeddingId = ae.AtomEmbeddingId
    INNER JOIN dbo.Atoms AS a ON a.AtomId = ae.AtomId
    ORDER BY SpatialDistance ASC;

    PRINT '  Stage 3: Top ' + CAST(@final_top_k AS NVARCHAR(10)) + ' results';
    PRINT '✓ Multi-resolution search complete';
END;
GO

-- ==========================================
-- PART 2: Cognitive Query Activation
-- ==========================================

CREATE OR ALTER PROCEDURE dbo.sp_CognitiveActivation
    @query_embedding VECTOR(1998),
    @activation_threshold FLOAT = 0.8,
    @max_activated INT = 50
AS
BEGIN
    SET NOCOUNT ON;

    IF @query_embedding IS NULL
    BEGIN
        RAISERROR('Query embedding cannot be NULL.', 16, 1);
        RETURN;
    END;

    IF @activation_threshold IS NULL OR @activation_threshold <= -1.0 OR @activation_threshold > 1.0
    BEGIN
        RAISERROR('Activation threshold must be within (-1, 1].', 16, 1);
        RETURN;
    END;

    DECLARE @start_time DATETIME2 = SYSUTCDATETIME();
    DECLARE @max_distance FLOAT = 1.0 - @activation_threshold;

    IF @max_distance <= 0
    BEGIN
        RAISERROR('Activation threshold too high for cosine similarity search.', 16, 1);
        RETURN;
    END;

    PRINT 'COGNITIVE ACTIVATION: Atom embeddings firing based on cosine similarity';
    PRINT '  Threshold: ' + CAST(@activation_threshold AS NVARCHAR(10)) + ' | Max candidates: ' + CAST(@max_activated AS NVARCHAR(10));

    DECLARE @activated TABLE (
        AtomEmbeddingId BIGINT PRIMARY KEY,
        AtomId BIGINT NOT NULL,
        ActivationStrength FLOAT NOT NULL
    );

    INSERT INTO @activated (AtomEmbeddingId, AtomId, ActivationStrength)
    SELECT TOP (@max_activated)
        ae.AtomEmbeddingId,
        ae.AtomId,
        1.0 - VECTOR_DISTANCE('cosine', ae.EmbeddingVector, @query_embedding) AS ActivationStrength
    FROM dbo.AtomEmbeddings AS ae
    WHERE ae.EmbeddingVector IS NOT NULL
      AND VECTOR_DISTANCE('cosine', ae.EmbeddingVector, @query_embedding) <= @max_distance
    ORDER BY VECTOR_DISTANCE('cosine', ae.EmbeddingVector, @query_embedding) ASC;

    DECLARE @activated_count INT = @@ROWCOUNT;
    PRINT '  Activated nodes: ' + CAST(@activated_count AS NVARCHAR(10));

    SELECT
        ae.AtomEmbeddingId,
        ae.AtomId,
        a.Modality,
        a.Subtype,
        a.SourceType,
        a.SourceUri,
        a.CanonicalText,
        act.ActivationStrength,
        CASE
            WHEN act.ActivationStrength >= 0.95 THEN 'VERY_HIGH'
            WHEN act.ActivationStrength >= 0.90 THEN 'HIGH'
            WHEN act.ActivationStrength >= 0.85 THEN 'MEDIUM'
            ELSE 'LOW'
        END AS ActivationLevel
    FROM @activated AS act
    INNER JOIN dbo.AtomEmbeddings AS ae ON ae.AtomEmbeddingId = act.AtomEmbeddingId
    INNER JOIN dbo.Atoms AS a ON a.AtomId = act.AtomId
    ORDER BY act.ActivationStrength DESC;

    DECLARE @duration_ms INT = DATEDIFF(MILLISECOND, @start_time, SYSUTCDATETIME());
    DECLARE @input_json JSON = CAST(JSON_OBJECT('activationThreshold': @activation_threshold, 'maxActivated': @max_activated) AS JSON);
    DECLARE @output_json JSON = CAST(JSON_OBJECT('activatedCount': @activated_count) AS JSON);
    DECLARE @output_metadata JSON = CAST(JSON_OBJECT('status': 'completed', 'durationMs': @duration_ms) AS JSON);
    DECLARE @inference_id BIGINT;

    INSERT INTO dbo.InferenceRequests (TaskType, InputData, ModelsUsed, EnsembleStrategy, OutputData, OutputMetadata, TotalDurationMs)
    VALUES (
        'cognitive_activation',
        @input_json,
        'atom_embeddings',
        'cognitive_activation',
        @output_json,
        @output_metadata,
        @duration_ms
    );

    SET @inference_id = SCOPE_IDENTITY();
    PRINT '✓ Cognitive activation complete - Inference ID: ' + CAST(@inference_id AS NVARCHAR(20));
END;
GO

-- ==========================================
-- PART 3: Dynamic Student Model Extraction
-- ==========================================

CREATE OR ALTER PROCEDURE dbo.sp_DynamicStudentExtraction
    @parent_model_id INT,
    @target_size_ratio FLOAT = 0.5,
    @selection_strategy NVARCHAR(20) = 'importance'
AS
BEGIN
    SET NOCOUNT ON;

    IF @target_size_ratio <= 0 OR @target_size_ratio > 1
    BEGIN
        RAISERROR('Target size ratio must be within (0, 1].', 16, 1);
        RETURN;
    END;

    DECLARE @parent_exists INT = (
        SELECT COUNT(*)
        FROM dbo.Models
        WHERE ModelId = @parent_model_id
    );

    IF @parent_exists = 0
    BEGIN
        RAISERROR('Parent model does not exist.', 16, 1);
        RETURN;
    END;

    DECLARE @ratio_percent INT = CEILING(@target_size_ratio * 100);
    IF @ratio_percent < 1 SET @ratio_percent = 1;
    DECLARE @new_model_name NVARCHAR(200) = CONCAT('Student_', @parent_model_id, '_', @selection_strategy, '_', @ratio_percent, 'pct');
    DECLARE @layer_subset NVARCHAR(MAX) = NULL;
    DECLARE @importance_threshold FLOAT = NULL;

    IF @selection_strategy = 'layer'
    BEGIN
        DECLARE @total_layers INT = (
            SELECT COUNT(*)
            FROM dbo.ModelLayers
            WHERE ModelId = @parent_model_id
        );

        IF @total_layers = 0
        BEGIN
            RAISERROR('Parent model has no layers to extract.', 16, 1);
            RETURN;
        END;

        DECLARE @layers_to_take INT = CEILING(@total_layers * @target_size_ratio);
        IF @layers_to_take < 1 SET @layers_to_take = 1;

        SELECT @layer_subset = STRING_AGG(CAST(LayerIdx AS NVARCHAR(10)), ',') WITHIN GROUP (ORDER BY LayerIdx)
        FROM (
            SELECT TOP (@layers_to_take) LayerIdx
            FROM dbo.ModelLayers
            WHERE ModelId = @parent_model_id
            ORDER BY LayerIdx
        ) AS layer_selection;

        PRINT 'DYNAMIC EXTRACTION → Layer subset: ' + ISNULL(@layer_subset, '(empty)');
    END
    ELSE IF @selection_strategy = 'random'
    BEGIN
        DECLARE @total_layers_random INT = (
            SELECT COUNT(*)
            FROM dbo.ModelLayers
            WHERE ModelId = @parent_model_id
        );

        IF @total_layers_random = 0
        BEGIN
            RAISERROR('Parent model has no layers to extract.', 16, 1);
            RETURN;
        END;

        DECLARE @random_take INT = CEILING(@total_layers_random * @target_size_ratio);
        IF @random_take < 1 SET @random_take = 1;

        SELECT @layer_subset = STRING_AGG(CAST(LayerIdx AS NVARCHAR(10)), ',')
        FROM (
            SELECT TOP (@random_take) LayerIdx
            FROM dbo.ModelLayers
            WHERE ModelId = @parent_model_id
            ORDER BY NEWID()
        ) AS random_layers;

        PRINT 'DYNAMIC EXTRACTION → Random layer subset: ' + ISNULL(@layer_subset, '(empty)');
    END
    ELSE -- default to importance-based filtering
    BEGIN
        DECLARE @total_atoms INT = (
            SELECT COUNT(*)
            FROM dbo.TensorAtoms
            WHERE ModelId = @parent_model_id
        );

        IF @total_atoms = 0
        BEGIN
            PRINT 'Parent model has no tensor atoms; falling back to full copy.';
            SET @importance_threshold = NULL;
        END
        ELSE
        BEGIN
            DECLARE @atoms_to_take INT = CEILING(@total_atoms * @target_size_ratio);
            IF @atoms_to_take < 1 SET @atoms_to_take = 1;

            SELECT @importance_threshold = MIN(ImportanceScore)
            FROM (
                SELECT TOP (@atoms_to_take) ImportanceScore
                FROM dbo.TensorAtoms
                WHERE ModelId = @parent_model_id
                  AND ImportanceScore IS NOT NULL
                ORDER BY ImportanceScore DESC
            ) AS ranked;

            IF @importance_threshold IS NULL
            BEGIN
                PRINT 'Importance scores missing; no threshold applied.';
            END
        END

        PRINT 'DYNAMIC EXTRACTION → Importance threshold: ' + COALESCE(CAST(@importance_threshold AS NVARCHAR(32)), 'none');
    END;

    EXEC dbo.sp_ExtractStudentModel
        @parent_model_id = @parent_model_id,
        @layer_subset = @layer_subset,
        @importance_threshold = @importance_threshold,
        @new_model_name = @new_model_name;
END;
GO

-- ==========================================
-- PART 4: Cross-Modal Inference
-- ==========================================

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

    PRINT 'CROSS-MODAL INFERENCE';
    PRINT '  Text filter: ' + ISNULL(@text_query, '(none)');
    PRINT '  Target modality: ' + ISNULL(@modality_filter, 'all');

    IF @spatial_query_x IS NOT NULL AND @spatial_query_y IS NOT NULL
    BEGIN
        DECLARE @z FLOAT = ISNULL(@spatial_query_z, 0);
    DECLARE @query_wkt NVARCHAR(200) = CONCAT('POINT (', @spatial_query_x, ' ', @spatial_query_y, ' ', @z, ')');
        DECLARE @query_pt GEOMETRY = geometry::STGeomFromText(@query_wkt, 0);

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

    PRINT '✓ Cross-modal results returned';
END;
GO

-- ==========================================
-- PART 5: Knowledge Distillation Metrics
-- ==========================================

CREATE OR ALTER PROCEDURE dbo.sp_CompareModelKnowledge
    @model_a_id INT,
    @model_b_id INT
AS
BEGIN
    SET NOCOUNT ON;

    PRINT 'KNOWLEDGE COMPARISON: Model ' + CAST(@model_a_id AS NVARCHAR(20)) + ' vs Model ' + CAST(@model_b_id AS NVARCHAR(20));

    -- Compare tensor atom distributions
    SELECT
        'Model A' AS model_name,
        @model_a_id AS model_id,
        COUNT(*) AS total_tensor_atoms,
        AVG(CAST(ImportanceScore AS FLOAT)) AS avg_importance,
        STDEV(CAST(ImportanceScore AS FLOAT)) AS stdev_importance,
        MIN(ImportanceScore) AS min_importance,
        MAX(ImportanceScore) AS max_importance
    FROM dbo.TensorAtoms
    WHERE ModelId = @model_a_id

    UNION ALL

    SELECT
        'Model B' AS model_name,
        @model_b_id AS model_id,
        COUNT(*) AS total_tensor_atoms,
        AVG(CAST(ImportanceScore AS FLOAT)) AS avg_importance,
        STDEV(CAST(ImportanceScore AS FLOAT)) AS stdev_importance,
        MIN(ImportanceScore) AS min_importance,
        MAX(ImportanceScore) AS max_importance
    FROM dbo.TensorAtoms
    WHERE ModelId = @model_b_id;

    -- Compare layer structures using ModelLayers metadata
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
       AND a.ModelId = @model_a_id
       AND b.ModelId = @model_b_id
    WHERE a.ModelId = @model_a_id
       OR b.ModelId = @model_b_id
    ORDER BY COALESCE(a.LayerIdx, b.LayerIdx);

    -- Compare tensor atom coefficient counts
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
        WHERE ta.ModelId IN (@model_a_id, @model_b_id)
        GROUP BY ta.ModelId
    ) AS stats
    ORDER BY stats.model_id;

    PRINT '✓ Knowledge comparison complete';
END;
GO

-- ==========================================
-- PART 6: Temporal Inference Tracking
-- ==========================================

CREATE OR ALTER PROCEDURE dbo.sp_InferenceHistory
    @time_window_hours INT = 24,
    @task_type NVARCHAR(50) = NULL
AS
BEGIN
    SET NOCOUNT ON;

    PRINT 'INFERENCE HISTORY ANALYSIS';
    PRINT '  Time window: Last ' + CAST(@time_window_hours AS VARCHAR(10)) + ' hours';

    DECLARE @cutoff_time DATETIME2 = DATEADD(HOUR, -@time_window_hours, SYSUTCDATETIME());

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
        AND (@task_type IS NULL OR TaskType = @task_type)
    GROUP BY TaskType
    ORDER BY request_count DESC;

    PRINT '✓ Inference history analysis complete';
END;
GO

PRINT '';
PRINT '============================================================';
PRINT 'ADVANCED PROCEDURES CREATED';
PRINT '============================================================';
PRINT 'Available procedures:';
PRINT '  1. sp_MultiResolutionSearch     - 3-stage funnel search';
PRINT '  2. sp_CognitiveActivation        - Neural activation pattern';
PRINT '  3. sp_DynamicStudentExtraction   - Flexible model distillation';
PRINT '  4. sp_CrossModalQuery            - Query across modalities';
PRINT '  5. sp_CompareModelKnowledge      - Compare model weights';
PRINT '  6. sp_InferenceHistory           - Temporal analysis';
PRINT '============================================================';
GO
