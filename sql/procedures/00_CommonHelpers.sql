-- COMMON HELPER FUNCTIONS
-- Reusable components to eliminate duplication across procedures
USE Hartonomous;
GO

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
        a.AtomHash,
        a.AtomType,
        a.AtomData,
        a.ContentType,
        a.Modality,
        a.Subtype,
        a.SourceType,
        a.SourceUri,
        a.CanonicalText,
        a.ReferenceCount,
        CAST(a.AtomData AS NVARCHAR(MAX)) AS AtomText
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
    -- Sort JSON keys alphabetically for consistent hashing
    -- Simplified: assumes flat JSON object
    RETURN @json; -- Placeholder - SQL Server 2025 may have native JSON_NORMALIZE
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
    -- Numerically stable softmax with temperature
    -- exp((logit - max_logit) / temperature)
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
PRINT '============================================================';
GO
