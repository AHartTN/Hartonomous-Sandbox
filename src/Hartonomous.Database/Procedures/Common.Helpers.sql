-- Common helper functions centralised for reuse across stored procedures.

-- ==========================================
-- Table-Valued Function: Get AtomEmbeddings with Atoms
-- Eliminates repetitive JOIN pattern
-- ==========================================
CREATE OR ALTER FUNCTION dbo.fn_GetAtomEmbeddingsWithAtoms(@dimension INT = NULL)
RETURNS TABLE
AS
RETURN
(
    SELECT
        ae.AtomEmbeddingId,
        ae.AtomId,
        ae.EmbeddingVector,
        ae.SpatialGeometry,
        ae.SpatialCoarse,
        ae.Dimension,
        ae.EmbeddingType,
        ae.ModelId,
        a.ContentHash,
        a.Modality,
        a.Subtype,
        a.SourceType,
        a.SourceUri,
        a.CanonicalText,
        a.PayloadLocator,
        a.Metadata,
        a.ReferenceCount,
        a.SpatialKey,
        a.CanonicalText AS AtomText
    FROM dbo.AtomEmbeddings ae
    INNER JOIN dbo.Atoms a ON ae.AtomId = a.AtomId
    WHERE @dimension IS NULL OR ae.Dimension = @dimension
);
GO

-- ==========================================
-- Scalar Function: Vector Cosine Similarity
-- Wraps VECTOR_DISTANCE for clarity
-- ==========================================
CREATE OR ALTER FUNCTION dbo.fn_VectorCosineSimilarity(
    @vec1 VECTOR(1998),
    @vec2 VECTOR(1998)
)
RETURNS FLOAT
AS
BEGIN
    IF @vec1 IS NULL OR @vec2 IS NULL
        RETURN NULL;

    RETURN 1.0 - VECTOR_DISTANCE('cosine', @vec1, @vec2);
END;
GO

-- ==========================================
-- Scalar Function: Create Spatial Point WKT
-- Standardizes POINT construction
-- ==========================================
CREATE OR ALTER FUNCTION dbo.fn_CreateSpatialPoint(
    @x FLOAT,
    @y FLOAT,
    @z FLOAT = NULL
)
RETURNS GEOMETRY
AS
BEGIN
    DECLARE @result GEOMETRY;

    IF @z IS NULL
        SET @result = geometry::STGeomFromText('POINT(' + CAST(@x AS NVARCHAR(50)) + ' ' + CAST(@y AS NVARCHAR(50)) + ')', 0);
    ELSE
        SET @result = geometry::STGeomFromText('POINT(' + CAST(@x AS NVARCHAR(50)) + ' ' + CAST(@y AS NVARCHAR(50)) + ' ' + CAST(@z AS NVARCHAR(50)) + ')', 0);

    RETURN @result;
END;
GO

-- ==========================================
-- Table-Valued Function: Get Context Centroid
-- Common pattern for computing spatial centroid from atom IDs
-- ==========================================
CREATE OR ALTER FUNCTION dbo.fn_GetContextCentroid(@atom_ids NVARCHAR(MAX))
RETURNS TABLE
AS
RETURN
(
    SELECT
        dbo.fn_CreateSpatialPoint(
            AVG(ae.SpatialGeometry.STX),
            AVG(ae.SpatialGeometry.STY),
            AVG(CAST(COALESCE(ae.SpatialGeometry.Z, 0) AS FLOAT))
        ) AS ContextCentroid,
        COUNT(*) AS AtomCount
    FROM dbo.AtomEmbeddings ae
    WHERE ae.AtomId IN (SELECT CAST(value AS BIGINT) FROM STRING_SPLIT(@atom_ids, ','))
      AND ae.SpatialGeometry IS NOT NULL
);
GO

-- ==========================================
-- Scalar Function: Normalize JSON for hashing
-- Ensures consistent JSON key ordering for InputHash
-- ==========================================
CREATE OR ALTER FUNCTION dbo.fn_NormalizeJSON(@json NVARCHAR(MAX))
RETURNS NVARCHAR(MAX)
AS
BEGIN
    IF @json IS NULL OR ISJSON(@json) = 0
        RETURN @json;

    DECLARE @normalized NVARCHAR(MAX);

    SELECT @normalized = (
        SELECT [key], value
        FROM OPENJSON(@json)
        ORDER BY [key]
        FOR JSON PATH, WITHOUT_ARRAY_WRAPPER
    );

    RETURN @normalized;
END;
GO

-- ==========================================
-- Table-Valued Function: Spatial Nearest Neighbors
-- Generic k-NN search on any spatial geometry column
-- ==========================================
CREATE OR ALTER FUNCTION dbo.fn_SpatialKNN(
    @query_point GEOMETRY,
    @top_k INT,
    @table_name NVARCHAR(128)
)
RETURNS TABLE
AS
RETURN
(
    -- Dynamic SQL would be needed for generic table parameter
    -- For now, specialized for AtomEmbeddings
    SELECT TOP (@top_k)
        ae.AtomEmbeddingId,
        ae.AtomId,
        ae.SpatialGeometry.STDistance(@query_point) AS SpatialDistance
    FROM dbo.AtomEmbeddings ae
    WHERE ae.SpatialGeometry IS NOT NULL
      AND ae.SpatialGeometry.STDistance(@query_point) IS NOT NULL
    ORDER BY ae.SpatialGeometry.STDistance(@query_point) ASC
);
GO

-- ==========================================
-- Scalar Function: Softmax Temperature Scaling
-- ==========================================
CREATE OR ALTER FUNCTION dbo.fn_SoftmaxTemperature(
    @logit FLOAT,
    @max_logit FLOAT,
    @temperature FLOAT
)
RETURNS FLOAT
AS
BEGIN
    RETURN EXP((@logit - @max_logit) / @temperature);
END;
GO

PRINT '============================================================';
PRINT 'COMMON HELPER FUNCTIONS CREATED';
PRINT '============================================================';
PRINT '✓ fn_GetAtomEmbeddingsWithAtoms - Eliminates JOIN duplication';
PRINT '✓ fn_VectorCosineSimilarity - Wraps VECTOR_DISTANCE';
PRINT '✓ fn_CreateSpatialPoint - Standardizes POINT WKT construction';
PRINT '✓ fn_GetContextCentroid - Computes spatial centroid from atom IDs';
PRINT '✓ fn_NormalizeJSON - JSON key ordering for hashing';
PRINT '✓ fn_SpatialKNN - Generic k-NN spatial search';
PRINT '✓ fn_SoftmaxTemperature - Numerically stable softmax scaling';
PRINT '✓ fn_SelectModelsForTask - Centralized model selection & weighting';
PRINT '✓ fn_EnsembleAtomScores - Shared ensemble candidate scoring';
PRINT '============================================================';
GO

-- ==========================================
-- Table-Valued Function: Select Models For Task/Modality
-- Centralizes model discovery and weight normalization logic
-- ==========================================
CREATE OR ALTER FUNCTION dbo.fn_SelectModelsForTask
(
    @task_type NVARCHAR(50) = NULL,
    @model_ids NVARCHAR(MAX) = NULL,
    @weights_json NVARCHAR(MAX) = NULL,
    @required_modalities NVARCHAR(MAX) = NULL,
    @additional_model_types NVARCHAR(MAX) = NULL
)
RETURNS @models TABLE
(
    ModelId INT PRIMARY KEY,
    Weight FLOAT NOT NULL,
    ModelName NVARCHAR(200) NULL
)
AS
BEGIN
    DECLARE @trimmedIds NVARCHAR(MAX) = NULLIF(LTRIM(RTRIM(@model_ids)), '');
    DECLARE @normalizedTask NVARCHAR(50) = NULLIF(LTRIM(RTRIM(@task_type)), '');
    DECLARE @normalizedModality NVARCHAR(50) = NULL;
    DECLARE @explicit BIT = 0;

    IF @trimmedIds IS NOT NULL
    BEGIN
        INSERT INTO @models (ModelId, Weight, ModelName)
        SELECT DISTINCT
            m.ModelId,
            1.0,
            m.ModelName
        FROM STRING_SPLIT(@trimmedIds, ',') vals
        CROSS APPLY (SELECT TRY_CAST(LTRIM(RTRIM(vals.value)) AS INT) AS ParsedId) parsed
        INNER JOIN dbo.Models m ON m.ModelId = parsed.ParsedId
        WHERE parsed.ParsedId IS NOT NULL;

        IF EXISTS (SELECT 1 FROM @models)
        BEGIN
            SET @explicit = 1;
        END;
    END;

    DECLARE @modalities TABLE (Modality NVARCHAR(100) PRIMARY KEY);
    INSERT INTO @modalities (Modality)
    SELECT DISTINCT NULLIF(LTRIM(RTRIM(value)), '')
    FROM STRING_SPLIT(COALESCE(@required_modalities, ''), ',')
    WHERE NULLIF(LTRIM(RTRIM(value)), '') IS NOT NULL;

    IF EXISTS (SELECT 1 FROM @modalities)
    BEGIN
        SELECT TOP (1) @normalizedModality = Modality FROM @modalities ORDER BY Modality;
    END;

    IF @explicit = 0
    BEGIN
        DECLARE @additionalTypes TABLE (ModelType NVARCHAR(100) PRIMARY KEY);
        INSERT INTO @additionalTypes (ModelType)
        SELECT DISTINCT NULLIF(LTRIM(RTRIM(value)), '')
        FROM STRING_SPLIT(COALESCE(@additional_model_types, ''), ',')
        WHERE NULLIF(LTRIM(RTRIM(value)), '') IS NOT NULL;

        INSERT INTO @models (ModelId, Weight, ModelName)
        SELECT DISTINCT
            m.ModelId,
            COALESCE(
                CASE
                    WHEN @normalizedTask IS NOT NULL
                        THEN TRY_CAST(JSON_VALUE(m.Config, CONCAT('$.weights.', @normalizedTask)) AS FLOAT)
                    ELSE NULL
                END,
                1.0
            ) AS Weight,
            m.ModelName
        FROM dbo.Models m
        LEFT JOIN dbo.ModelMetadata md ON md.ModelId = m.ModelId
        LEFT JOIN @additionalTypes at ON at.ModelType = m.ModelType
        WHERE
            m.ModelType IN ('multimodal', 'general')
            OR (@normalizedTask IS NOT NULL AND m.ModelType = @normalizedTask)
            OR at.ModelType IS NOT NULL
            OR (
                @normalizedTask IS NOT NULL
                AND md.SupportedTasks IS NOT NULL
                AND ISJSON(md.SupportedTasks) = 1
                AND EXISTS (
                    SELECT 1 FROM OPENJSON(md.SupportedTasks) WHERE value = @normalizedTask
                )
            )
            OR (
                EXISTS (SELECT 1 FROM @modalities)
                AND md.SupportedModalities IS NOT NULL
                AND ISJSON(md.SupportedModalities) = 1
                AND EXISTS (
                    SELECT 1
                    FROM OPENJSON(md.SupportedModalities)
                    WHERE value IN (SELECT Modality FROM @modalities)
                )
            );
    END;

    IF NOT EXISTS (SELECT 1 FROM @models)
        RETURN;

    DECLARE @weights NVARCHAR(MAX) = NULLIF(@weights_json, 'null');

    IF @weights IS NOT NULL AND ISJSON(@weights) = 1
    BEGIN
        WITH WeightOverrides AS (
            SELECT
                TRY_CAST(JSON_VALUE(value, '$.modelId') AS INT) AS ModelId,
                TRY_CAST(JSON_VALUE(value, '$.weight') AS FLOAT) AS Weight
            FROM OPENJSON(@weights)
            WHERE JSON_VALUE(value, '$.modelId') IS NOT NULL
        )
        UPDATE m
        SET Weight = CASE WHEN w.Weight IS NOT NULL AND w.Weight > 0 THEN w.Weight ELSE m.Weight END
        FROM @models m
        INNER JOIN WeightOverrides w ON w.ModelId = m.ModelId;
    END;

    DECLARE @total FLOAT = (SELECT SUM(Weight) FROM @models);

    IF @total IS NULL OR @total = 0
    BEGIN
        UPDATE @models SET Weight = 1.0;
        SET @total = (SELECT SUM(Weight) FROM @models);
    END;

    UPDATE @models SET Weight = Weight / @total;

    RETURN;
END;
GO

-- ==========================================
-- Table-Valued Function: Ensemble Atom Scores
-- Provides per-model weighted scoring for embeddings
-- ==========================================
CREATE OR ALTER FUNCTION dbo.fn_EnsembleAtomScores
(
    @embedding VECTOR(1998),
    @models_json NVARCHAR(MAX),
    @top_per_model INT = 10,
    @required_modality NVARCHAR(64) = NULL
)
RETURNS TABLE
AS
RETURN
(
    WITH ParsedModels AS (
        SELECT
            TRY_CAST(JSON_VALUE(value, '$.ModelId') AS INT) AS ModelId,
            TRY_CAST(JSON_VALUE(value, '$.Weight') AS FLOAT) AS Weight
        FROM OPENJSON(@models_json)
        WHERE JSON_VALUE(value, '$.ModelId') IS NOT NULL
    ),
    Normalized AS (
        SELECT
            ModelId,
            CASE
                WHEN SUM(Weight) OVER () IS NULL OR SUM(Weight) OVER () = 0
                    THEN 1.0 / NULLIF(COUNT(*) OVER (), 0)
                ELSE Weight / SUM(Weight) OVER ()
            END AS Weight
        FROM ParsedModels
    ),
    RankedCandidates AS (
        SELECT
            n.ModelId,
            n.Weight,
            ae.AtomEmbeddingId,
            ae.AtomId,
            a.Modality,
            a.Subtype,
            a.SourceType,
            a.SourceUri,
            a.CanonicalText,
            VECTOR_DISTANCE('cosine', ae.EmbeddingVector, @embedding) AS Distance,
            ROW_NUMBER() OVER (
                PARTITION BY n.ModelId
                ORDER BY VECTOR_DISTANCE('cosine', ae.EmbeddingVector, @embedding), ae.AtomEmbeddingId
            ) AS RankWithinModel
        FROM Normalized n
        INNER JOIN dbo.AtomEmbeddings ae ON ae.ModelId = n.ModelId
        INNER JOIN dbo.Atoms a ON a.AtomId = ae.AtomId
        WHERE ae.EmbeddingVector IS NOT NULL
          AND (@required_modality IS NULL OR a.Modality = @required_modality)
    )
    SELECT
        ModelId,
        AtomEmbeddingId,
        AtomId,
        Modality,
        Subtype,
        SourceType,
        SourceUri,
        CanonicalText,
        Distance,
        Weight * (1.0 - Distance) AS WeightedScore
    FROM RankedCandidates
    WHERE RankWithinModel <= CASE WHEN @top_per_model IS NULL OR @top_per_model <= 0 THEN 10 ELSE @top_per_model END
);
GO
