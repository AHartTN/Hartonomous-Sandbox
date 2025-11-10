-- sp_SpatialVectorSearch: Bounding box pre-filtering + exact k-NN
-- Performance optimized for <50K candidates

CREATE OR ALTER PROCEDURE dbo.sp_SpatialVectorSearch
    @QueryVector VARBINARY(MAX),
    @SpatialCenter GEOMETRY = NULL,
    @RadiusMeters FLOAT = NULL,
    @TopK INT = 10,
    @TenantId INT = 0,
    @MinSimilarity FLOAT = 0.0
AS
BEGIN
    SET NOCOUNT ON;
    
    BEGIN TRY

        -- Phase 1: Spatial pre-filtering (if spatial params provided)

            AtomEmbeddingId BIGINT PRIMARY KEY,
            AtomId BIGINT,
            EmbeddingVector VARBINARY(MAX),
            SpatialDistance FLOAT
        );
        
        IF @SpatialCenter IS NOT NULL AND @RadiusMeters IS NOT NULL
        BEGIN
            -- Spatial filter: Only embeddings within radius
            
            
            SET @CandidateCount = @@ROWCOUNT;
        END
        ELSE
        BEGIN
            -- No spatial filter: Consider all embeddings (fallback to tenant filter only)
             -- Deterministic ordering
            
            SET @CandidateCount = @@ROWCOUNT;
        END
        
        -- Phase 2: Exact k-NN on candidates
        SELECT TOP (@TopK)
            c.AtomId,
            1.0 - VECTOR_DISTANCE('cosine', c.EmbeddingVector, @QueryVector) AS Similarity,
            c.SpatialDistance,
            a.ContentHash,
            a.ContentType,
            a.CreatedUtc
        FROM @Candidates c
        INNER JOIN dbo.Atoms a ON c.AtomId = a.AtomId
        WHERE (1.0 - VECTOR_DISTANCE('cosine', c.EmbeddingVector, @QueryVector)) >= @MinSimilarity
        ORDER BY Similarity DESC;
        
        RETURN 0;
    END TRY
    BEGIN CATCH

        RAISERROR(@ErrorMessage, 16, 1);
        RETURN -1;
    END CATCH
END;

-- sp_TemporalVectorSearch: Point-in-time semantic search
-- Uses temporal tables to query historical embeddings

CREATE OR ALTER PROCEDURE dbo.sp_TemporalVectorSearch
    @QueryVector VARBINARY(MAX),
    @AsOfDate DATETIME2,
    @TopK INT = 10,
    @TenantId INT = 0
AS
BEGIN
    SET NOCOUNT ON;
    
    BEGIN TRY
        -- Query historical embeddings using FOR SYSTEM_TIME AS OF
        SELECT TOP (@TopK)
            ae.AtomId,
            1.0 - VECTOR_DISTANCE('cosine', ae.EmbeddingVector, @QueryVector) AS Similarity,
            ae.LastComputedUtc,
            a.ContentHash,
            a.ContentType
        FROM dbo.AtomEmbeddings FOR SYSTEM_TIME AS OF @AsOfDate ae
        INNER JOIN dbo.Atoms FOR SYSTEM_TIME AS OF @AsOfDate a ON ae.AtomId = a.AtomId
        WHERE ae.TenantId = @TenantId
              AND a.TenantId = @TenantId
        ORDER BY Similarity DESC;
        
        RETURN 0;
    END TRY
    BEGIN CATCH

        RAISERROR(@ErrorMessage, 16, 1);
        RETURN -1;
    END CATCH
END;

-- sp_FusionSearch: Full-text + Vector + Spatial fusion
-- Combines multiple ranking signals with weighted scoring

CREATE OR ALTER PROCEDURE dbo.sp_FusionSearch
    @QueryVector VARBINARY(MAX),
    @Keywords NVARCHAR(MAX) = NULL,
    @SpatialRegion GEOGRAPHY = NULL,
    @TopK INT = 10,
    @VectorWeight FLOAT = 0.5,
    @KeywordWeight FLOAT = 0.3,
    @SpatialWeight FLOAT = 0.2,
    @TenantId INT = 0
AS
BEGIN
    SET NOCOUNT ON;
    
    BEGIN TRY
        -- Validate weights sum to 1.0
        IF ABS((@VectorWeight + @KeywordWeight + @SpatialWeight) - 1.0) > 0.01
        BEGIN
            RAISERROR('Weights must sum to 1.0', 16, 1);
            RETURN -1;
        END
        
        

-- sp_MultiModelEnsemble: Blend results from multiple embedding models
-- Weighted voting with configurable model weights

CREATE OR ALTER PROCEDURE dbo.sp_MultiModelEnsemble
    @QueryVector1 VARBINARY(MAX), -- Model 1 embedding
    @QueryVector2 VARBINARY(MAX), -- Model 2 embedding
    @QueryVector3 VARBINARY(MAX), -- Model 3 embedding
    @Model1Id INT,
    @Model2Id INT,
    @Model3Id INT,
    @Model1Weight FLOAT = 0.4,
    @Model2Weight FLOAT = 0.35,
    @Model3Weight FLOAT = 0.25,
    @TopK INT = 10,
    @TenantId INT = 0
AS
BEGIN
    SET NOCOUNT ON;
    
    BEGIN TRY
        

CREATE OR ALTER PROCEDURE dbo.sp_HybridSearch
    @query_vector VECTOR(1998),
    @query_dimension INT,
    @query_spatial_x FLOAT,
    @query_spatial_y FLOAT,
    @query_spatial_z FLOAT,
    @spatial_candidates INT = 100,
    @final_top_k INT = 10,
    @distance_metric NVARCHAR(20) = 'cosine',
    @embedding_type NVARCHAR(128) = NULL,
    @ModelId INT = NULL,
    @srid INT = 0
AS
BEGIN
    SET NOCOUNT ON;

