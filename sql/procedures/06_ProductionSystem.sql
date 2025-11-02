USE Hartonomous;
GO

PRINT '============================================================';
PRINT 'PRODUCTION AI INFERENCE SYSTEM';
PRINT 'SQL Server 2025 Atom Substrate Alignment';
PRINT '============================================================';
GO

-- ==========================================
-- PART 1: Inference Procedures (Atom Substrate)
-- ==========================================
-- Uses dbo.Atoms, dbo.AtomEmbeddings, dbo.TensorAtoms, dbo.TensorAtomCoefficients

-- EXACT search using VECTOR_DISTANCE (for small datasets < 50k)
CREATE OR ALTER PROCEDURE dbo.sp_ExactVectorSearch
    @query_vector VECTOR(1998),
    @top_k INT = 10,
    @distance_metric NVARCHAR(20) = 'cosine',
    @embedding_type NVARCHAR(128) = NULL,
    @ModelId INT = NULL
AS
BEGIN
    SET NOCOUNT ON;

    SELECT TOP (@top_k)
        ae.AtomEmbeddingId,
        ae.AtomId,
        a.Modality,
        a.Subtype,
        a.SourceUri,
        a.SourceType,
        a.CanonicalText,
        ae.EmbeddingType,
        ae.ModelId,
        ae.Dimension,
        VECTOR_DISTANCE(@distance_metric, ae.EmbeddingVector, @query_vector) AS distance,
        1.0 - VECTOR_DISTANCE(@distance_metric, ae.EmbeddingVector, @query_vector) AS similarity,
        ae.CreatedAt
    FROM dbo.AtomEmbeddings AS ae
    INNER JOIN dbo.Atoms AS a ON a.AtomId = ae.AtomId
    WHERE ae.EmbeddingVector IS NOT NULL
      AND (@embedding_type IS NULL OR ae.EmbeddingType = @embedding_type)
      AND (@ModelId IS NULL OR ae.ModelId = @ModelId)
    ORDER BY VECTOR_DISTANCE(@distance_metric, ae.EmbeddingVector, @query_vector);
END;
GO

-- APPROXIMATE search using spatial index (for large datasets)
CREATE OR ALTER PROCEDURE dbo.sp_ApproxSpatialSearch
    @query_x FLOAT,
    @query_y FLOAT,
    @query_z FLOAT,
    @top_k INT = 10,
    @use_coarse BIT = 0,
    @embedding_type NVARCHAR(128) = NULL,
    @ModelId INT = NULL,
    @srid INT = 0
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @wkt NVARCHAR(200) = CONCAT('POINT (', @query_x, ' ', @query_y, ' ', @query_z, ')');
    DECLARE @query_point GEOMETRY = geometry::STGeomFromText(@wkt, @srid);

    IF @use_coarse = 1
    BEGIN
        -- Coarse search: Fast, less accurate
        SELECT TOP (@top_k)
            ae.AtomEmbeddingId,
            ae.AtomId,
            a.Modality,
            a.Subtype,
            a.SourceUri,
            a.SourceType,
            ae.EmbeddingType,
            ae.ModelId,
            ae.SpatialCoarse.STDistance(@query_point) AS spatial_distance
        FROM dbo.AtomEmbeddings AS ae
        INNER JOIN dbo.Atoms AS a ON a.AtomId = ae.AtomId
        WHERE ae.SpatialCoarse IS NOT NULL
          AND (@embedding_type IS NULL OR ae.EmbeddingType = @embedding_type)
          AND (@ModelId IS NULL OR ae.ModelId = @ModelId)
        ORDER BY ae.SpatialCoarse.STDistance(@query_point);
    END
    ELSE
    BEGIN
        -- Fine search: Slower, more accurate
        SELECT TOP (@top_k)
            ae.AtomEmbeddingId,
            ae.AtomId,
            a.Modality,
            a.Subtype,
            a.SourceUri,
            a.SourceType,
            ae.EmbeddingType,
            ae.ModelId,
            ae.SpatialGeometry.STDistance(@query_point) AS spatial_distance
        FROM dbo.AtomEmbeddings AS ae
        INNER JOIN dbo.Atoms AS a ON a.AtomId = ae.AtomId
        WHERE ae.SpatialGeometry IS NOT NULL
          AND (@embedding_type IS NULL OR ae.EmbeddingType = @embedding_type)
          AND (@ModelId IS NULL OR ae.ModelId = @ModelId)
        ORDER BY ae.SpatialGeometry.STDistance(@query_point);
    END;
END;
GO

-- HYBRID search: Spatial filter → Vector rerank
CREATE OR ALTER PROCEDURE dbo.sp_HybridSearch
    @query_vector VECTOR(1998),
    @query_dimension INT,
    @query_spatial_x FLOAT,
    @query_spatial_y FLOAT,
    @query_spatial_z FLOAT,
    @spatial_candidates INT = 100, -- Retrieve N from spatial index
    @final_top_k INT = 10,          -- Rerank to top K
    @distance_metric NVARCHAR(20) = 'cosine',
    @embedding_type NVARCHAR(128) = NULL,
    @ModelId INT = NULL,
    @srid INT = 0
AS
BEGIN
    SET NOCOUNT ON;

    PRINT 'HYBRID SEARCH: Spatial filter + Vector rerank';

    DECLARE @wkt NVARCHAR(200) = CONCAT('POINT (', @query_spatial_x, ' ', @query_spatial_y, ' ', @query_spatial_z, ')');
    DECLARE @query_point GEOMETRY = geometry::STGeomFromText(@wkt, @srid);

    -- Step 1: Fast spatial filter (O(log n) via index)
    DECLARE @candidates TABLE (
        AtomEmbeddingId BIGINT PRIMARY KEY,
        SpatialDistance FLOAT
    );

    INSERT INTO @candidates (AtomEmbeddingId, SpatialDistance)
    SELECT TOP (@spatial_candidates)
        ae.AtomEmbeddingId,
        ae.SpatialGeometry.STDistance(@query_point) AS spatial_distance
    FROM dbo.AtomEmbeddings AS ae
    WHERE ae.SpatialGeometry IS NOT NULL
      AND ae.Dimension = @query_dimension
      AND (@embedding_type IS NULL OR ae.EmbeddingType = @embedding_type)
      AND (@ModelId IS NULL OR ae.ModelId = @ModelId)
    ORDER BY ae.SpatialGeometry.STDistance(@query_point);

    -- Step 2: Exact vector rerank on candidates (O(k) where k << n)
    SELECT TOP (@final_top_k)
        ae.AtomEmbeddingId,
        ae.AtomId,
        a.Modality,
        a.Subtype,
        a.SourceUri,
        a.SourceType,
        ae.EmbeddingType,
        ae.ModelId,
        VECTOR_DISTANCE(@distance_metric, ae.EmbeddingVector, @query_vector) AS exact_distance,
        c.SpatialDistance AS spatial_distance
    FROM dbo.AtomEmbeddings AS ae
    INNER JOIN @candidates AS c ON c.AtomEmbeddingId = ae.AtomEmbeddingId
    INNER JOIN dbo.Atoms AS a ON a.AtomId = ae.AtomId
    WHERE ae.EmbeddingVector IS NOT NULL
      AND ae.Dimension = @query_dimension
    ORDER BY VECTOR_DISTANCE(@distance_metric, ae.EmbeddingVector, @query_vector);

    PRINT 'Hybrid search complete: Spatial O(log n) + Vector O(k)';
END;
GO

PRINT 'Inference procedures created!';
PRINT '  - sp_ExactVectorSearch: Atom embeddings exact search';
PRINT '  - sp_ApproxSpatialSearch: Atom embeddings spatial search';
PRINT '  - sp_HybridSearch: Spatial filter + vector rerank';
GO

-- ==========================================
-- PART 2: Student Model Extraction (Tensor Atoms)
-- ==========================================

-- Extract a student model by querying specific layers/weights
CREATE OR ALTER PROCEDURE dbo.sp_ExtractStudentModel
    @ParentModelId INT,
    @layer_subset NVARCHAR(MAX) = NULL, -- optional CSV of layer indexes
    @importance_threshold FLOAT = 0.5,  -- minimum importance for tensor atoms
    @NewModelName NVARCHAR(200)
AS
BEGIN
    SET NOCOUNT ON;

    PRINT 'EXTRACTING STUDENT MODEL via T-SQL SELECT';
    PRINT 'Distillation stays inside SQL Server atom substrate.';

    DECLARE @ParentModelType NVARCHAR(100);
    DECLARE @parent_architecture NVARCHAR(100);
    DECLARE @parent_config JSON;
    DECLARE @parent_parameter_count BIGINT;

    SELECT
        @ParentModelType = ModelType,
        @parent_architecture = Architecture,
    @parent_config = Config,
        @parent_parameter_count = ParameterCount
    FROM dbo.Models
    WHERE ModelId = @ParentModelId;

    IF @ParentModelType IS NULL
    BEGIN
        THROW 50001, 'Parent model not found.', 1;
    END;

    INSERT INTO dbo.Models (ModelName, ModelType, Architecture, Config, ParameterCount)
    VALUES (@NewModelName, @ParentModelType, @parent_architecture, @parent_config, @parent_parameter_count);

    DECLARE @StudentModelId INT = SCOPE_IDENTITY();

    DECLARE @SelectedLayers TABLE (LayerIdx INT PRIMARY KEY);
    DECLARE @has_filter BIT = 0;

    IF @layer_subset IS NOT NULL AND LTRIM(RTRIM(@layer_subset)) <> ''
    BEGIN
        SET @has_filter = 1;

        INSERT INTO @SelectedLayers (LayerIdx)
        SELECT DISTINCT TRY_CAST(value AS INT)
        FROM STRING_SPLIT(@layer_subset, ',')
        WHERE TRY_CAST(value AS INT) IS NOT NULL;
    END;

    DECLARE @LayerData TABLE
    (
        LayerId BIGINT PRIMARY KEY,
        LayerIdx INT NOT NULL,
        LayerName NVARCHAR(100) NULL,
        LayerType NVARCHAR(50) NULL,
        WeightsGeometry GEOMETRY NULL,
    TensorShape NVARCHAR(MAX) NULL,
        TensorDtype NVARCHAR(20) NULL,
        QuantizationType NVARCHAR(20) NULL,
        QuantizationScale FLOAT NULL,
        QuantizationZeroPoint FLOAT NULL,
        Parameters JSON NULL,
        ParameterCount BIGINT NULL,
        CacheHitRate FLOAT NULL,
        AvgComputeTimeMs FLOAT NULL
    );

    INSERT INTO @LayerData
    SELECT
        ml.LayerId,
        ml.LayerIdx,
        ml.LayerName,
        ml.LayerType,
        ml.WeightsGeometry,
        ml.TensorShape,
        ml.TensorDtype,
        ml.QuantizationType,
        ml.QuantizationScale,
        ml.QuantizationZeroPoint,
        ml.Parameters,
        ml.ParameterCount,
        ml.CacheHitRate,
        ml.AvgComputeTimeMs
    FROM dbo.ModelLayers AS ml
    WHERE ml.ModelId = @ParentModelId
      AND (
            @has_filter = 0
            OR EXISTS (SELECT 1 FROM @SelectedLayers AS sl WHERE sl.LayerIdx = ml.LayerIdx)
          );

    DECLARE @LayerMap TABLE
    (
        OldLayerId BIGINT PRIMARY KEY,
        NewLayerId BIGINT NOT NULL
    );

    MERGE dbo.ModelLayers AS target
    USING (
        SELECT * FROM @LayerData
    ) AS src
        ON 1 = 0
    WHEN NOT MATCHED THEN
        INSERT (
            ModelId,
            LayerIdx,
            LayerName,
            LayerType,
            WeightsGeometry,
            TensorShape,
            TensorDtype,
            QuantizationType,
            QuantizationScale,
            QuantizationZeroPoint,
            Parameters,
            ParameterCount,
            CacheHitRate,
            AvgComputeTimeMs
        )
        VALUES (
            @StudentModelId,
            src.LayerIdx,
            src.LayerName,
            src.LayerType,
            src.WeightsGeometry,
            src.TensorShape,
            src.TensorDtype,
            src.QuantizationType,
            src.QuantizationScale,
            src.QuantizationZeroPoint,
            src.Parameters,
            src.ParameterCount,
            src.CacheHitRate,
            src.AvgComputeTimeMs
        )
    OUTPUT src.LayerId, inserted.LayerId INTO @LayerMap (OldLayerId, NewLayerId);

    DECLARE @TensorAtomData TABLE
    (
        TensorAtomId BIGINT PRIMARY KEY,
        LayerId BIGINT NOT NULL,
        AtomId BIGINT NOT NULL,
        AtomType NVARCHAR(128) NOT NULL,
        SpatialSignature GEOMETRY NULL,
        GeometryFootprint GEOMETRY NULL,
        Metadata JSON NULL,
        ImportanceScore REAL NULL
    );

    INSERT INTO @TensorAtomData
    SELECT
        ta.TensorAtomId,
        ta.LayerId,
        ta.AtomId,
        ta.AtomType,
        ta.SpatialSignature,
        ta.GeometryFootprint,
        ta.Metadata,
        ta.ImportanceScore
    FROM dbo.TensorAtoms AS ta
    WHERE ta.ModelId = @ParentModelId
      AND ta.LayerId IN (SELECT OldLayerId FROM @LayerMap)
      AND (@importance_threshold IS NULL
           OR ta.ImportanceScore IS NULL
           OR ta.ImportanceScore >= @importance_threshold);

    DECLARE @TensorMap TABLE
    (
        OldTensorAtomId BIGINT PRIMARY KEY,
        NewTensorAtomId BIGINT NOT NULL
    );

    MERGE dbo.TensorAtoms AS target
    USING (
        SELECT
            src.TensorAtomId,
            src.AtomId,
            src.AtomType,
            src.SpatialSignature,
            src.GeometryFootprint,
            src.Metadata,
            src.ImportanceScore,
            lm.NewLayerId
        FROM @TensorAtomData AS src
        INNER JOIN @LayerMap AS lm ON lm.OldLayerId = src.LayerId
    ) AS src
        ON 1 = 0
    WHEN NOT MATCHED THEN
        INSERT (
            AtomId,
            ModelId,
            LayerId,
            AtomType,
            SpatialSignature,
            GeometryFootprint,
            Metadata,
            ImportanceScore
        )
        VALUES (
            src.AtomId,
            @StudentModelId,
            src.NewLayerId,
            src.AtomType,
            src.SpatialSignature,
            src.GeometryFootprint,
            src.Metadata,
            src.ImportanceScore
        )
    OUTPUT src.TensorAtomId, inserted.TensorAtomId INTO @TensorMap (OldTensorAtomId, NewTensorAtomId);

    DECLARE @CoeffData TABLE
    (
        TensorAtomId BIGINT NOT NULL,
        ParentLayerId BIGINT NOT NULL,
        TensorRole NVARCHAR(128) NULL,
        Coefficient REAL NOT NULL
    );

    INSERT INTO @CoeffData
    SELECT
        coeff.TensorAtomId,
        coeff.ParentLayerId,
        coeff.TensorRole,
        coeff.Coefficient
    FROM dbo.TensorAtomCoefficients AS coeff
    WHERE coeff.TensorAtomId IN (SELECT OldTensorAtomId FROM @TensorMap);

    INSERT INTO dbo.TensorAtomCoefficients
    (
        TensorAtomId,
        ParentLayerId,
        TensorRole,
        Coefficient
    )
    SELECT
        tm.NewTensorAtomId,
        lm.NewLayerId,
        cd.TensorRole,
        cd.Coefficient
    FROM @CoeffData AS cd
    INNER JOIN @TensorMap AS tm ON tm.OldTensorAtomId = cd.TensorAtomId
    INNER JOIN @LayerMap AS lm ON lm.OldLayerId = cd.ParentLayerId;

    DECLARE @original_atoms BIGINT = (
        SELECT COUNT(*)
        FROM dbo.TensorAtoms
        WHERE ModelId = @ParentModelId
    );

    DECLARE @student_atoms BIGINT = (
        SELECT COUNT(*)
        FROM dbo.TensorAtoms
        WHERE ModelId = @StudentModelId
    );

    UPDATE dbo.Models
    SET ParameterCount = @student_atoms,
        LastUsed = NULL,
        UsageCount = 0
    WHERE ModelId = @StudentModelId;

    SELECT
        @StudentModelId AS student_model_id,
        @NewModelName AS student_name,
        @original_atoms AS original_tensor_atoms,
        @student_atoms AS student_tensor_atoms,
        CASE
            WHEN @original_atoms = 0 THEN NULL
            ELSE CAST(100.0 * @student_atoms / @original_atoms AS DECIMAL(6, 2))
        END AS atom_retention_percent
    OPTION (MAXDOP 1);

    PRINT 'Student model extracted: tensor atoms and coefficients cloned.';
END;
GO

-- Query specific weights from a model (for analysis or inference)
CREATE OR ALTER PROCEDURE dbo.sp_QueryModelWeights
    @ModelId INT,
    @LayerIdx INT = NULL,
    @atom_type NVARCHAR(128) = NULL
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        ta.TensorAtomId,
        ml.LayerIdx,
        ml.LayerType,
        ta.AtomType,
        ta.ImportanceScore,
        ta.Metadata AS TensorMetadata,
        a.Modality,
        a.Subtype,
        a.SourceType,
        coeff.ParentLayerId,
        mlParent.LayerIdx AS ParentLayerIdx,
        coeff.TensorRole,
        coeff.Coefficient
    FROM dbo.TensorAtoms AS ta
    INNER JOIN dbo.ModelLayers AS ml ON ml.LayerId = ta.LayerId
    INNER JOIN dbo.Atoms AS a ON a.AtomId = ta.AtomId
    LEFT JOIN dbo.TensorAtomCoefficients AS coeff ON coeff.TensorAtomId = ta.TensorAtomId
    LEFT JOIN dbo.ModelLayers AS mlParent ON mlParent.LayerId = coeff.ParentLayerId
    WHERE ta.ModelId = @ModelId
      AND (@LayerIdx IS NULL OR ml.LayerIdx = @LayerIdx)
      AND (@atom_type IS NULL OR ta.AtomType = @atom_type)
    ORDER BY ml.LayerIdx, ta.ImportanceScore DESC, ta.TensorAtomId, coeff.TensorRole;
END;
GO

PRINT 'Student model extraction procedures created!';
PRINT '  - sp_ExtractStudentModel: SELECT out a distilled model';
PRINT '  - sp_QueryModelWeights: Inspect model internals';
GO

PRINT '';
PRINT '============================================================';
PRINT 'PRODUCTION SYSTEM READY';
PRINT '============================================================';
PRINT 'Capabilities:';
PRINT '  1. Atom-aware vector search (exact / spatial / hybrid)';
PRINT '  2. Tensor atom student extraction aligned with Models/ModelLayers';
PRINT '  3. Model inspection via tensor atom and coefficient queries';
PRINT '';
PRINT 'Next: Ensure spatial indexes exist on AtomEmbeddings for production latency.';
PRINT '============================================================';
GO
