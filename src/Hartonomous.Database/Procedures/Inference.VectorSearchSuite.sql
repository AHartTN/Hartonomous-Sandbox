
-- ==========================================
-- PART 1: Inference Procedures (Atom Substrate)
-- ==========================================

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
END

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


    IF @use_coarse = 1
    BEGIN
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

-- NOTE: sp_HybridSearch has been moved to dbo.VectorSearch.sql to consolidate search procedures.

-- ==========================================
-- PART 2: Student Model Extraction (Tensor Atoms)
-- ==========================================

CREATE OR ALTER PROCEDURE dbo.sp_ExtractStudentModel
    @ParentModelId INT,
    @layer_subset NVARCHAR(MAX) = NULL,
    @importance_threshold FLOAT = 0.5,
    @NewModelName NVARCHAR(200)
AS
BEGIN
    SET NOCOUNT ON;

    IF @@TRANCOUNT = 0
    BEGIN
        SET @startedTransaction = 1;
        BEGIN TRANSACTION;
    END
    ELSE
    BEGIN
        SAVE TRANSACTION ExtractStudentModelSavepoint;
    END;

    BEGIN TRY




        SELECT
            @ParentModelType = ModelType,
            @parent_architecture = Architecture,
            @parent_config = Config,
            @parent_parameter_count = ParameterCount
        FROM dbo.Models
        WHERE ModelId = @ParentModelId;

        IF @ParentModelType IS NULL
        BEGIN
            ;THROW 50001, 'Parent model not found.', 1;
        END;

        

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

