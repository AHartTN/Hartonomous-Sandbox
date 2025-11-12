-- sp_SpatialVectorSearch: Bounding box pre-filtering + exact k-NN
-- Performance optimized for <50K candidates

CREATE OR ALTER PROCEDURE dbo.sp_SpatialVectorSearch
    @QueryVector VARBINARY(MAX),
    @SpatialCenter GEOMETRY = NULL,
    @RadiusMeters FLOAT = NULL,
    @TopK INT = 10,
    @TenantId INT = NULL, -- Optional tenant filtering: NULL = all tenants, specific value = single tenant
    @MinSimilarity FLOAT = 0.0
AS
BEGIN
    SET NOCOUNT ON;
    
    BEGIN TRY
        DECLARE @CandidateCount INT;
        
        -- Phase 1: Spatial pre-filtering (if spatial params provided)
        DECLARE @Candidates TABLE (
            AtomEmbeddingId BIGINT PRIMARY KEY,
            AtomId BIGINT,
            EmbeddingVector VARBINARY(MAX),
            SpatialDistance FLOAT
        );
        
        IF @SpatialCenter IS NOT NULL AND @RadiusMeters IS NOT NULL
        BEGIN
            -- Spatial filter: Only embeddings within radius
            INSERT INTO @Candidates
            SELECT 
                ae.AtomEmbeddingId,
                ae.AtomId,
                ae.EmbeddingVector,
                @SpatialCenter.STDistance(ae.SpatialProjection3D) AS SpatialDistance
            FROM dbo.AtomEmbeddings ae WITH (INDEX(IX_AtomEmbeddings_Spatial))
            LEFT JOIN dbo.TenantAtoms ta ON ae.AtomId = ta.AtomId
            WHERE (@TenantId IS NULL OR ta.TenantId = @TenantId)
                  AND ae.SpatialProjection3D IS NOT NULL
                  AND @SpatialCenter.STDistance(ae.SpatialProjection3D) <= @RadiusMeters;
            
            SET @CandidateCount = @@ROWCOUNT;
        END
        ELSE
        BEGIN
            -- No spatial filter: Consider all embeddings (fallback to tenant filter only)
            INSERT INTO @Candidates
            SELECT TOP 100000 -- Safety limit
                ae.AtomEmbeddingId,
                ae.AtomId,
                ae.EmbeddingVector,
                0 AS SpatialDistance
            FROM dbo.AtomEmbeddings ae
            LEFT JOIN dbo.TenantAtoms ta ON ae.AtomId = ta.AtomId
            WHERE (@TenantId IS NULL OR ta.TenantId = @TenantId)
            ORDER BY ae.AtomEmbeddingId; -- Deterministic ordering
            
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
        DECLARE @ErrorMessage NVARCHAR(4000) = ERROR_MESSAGE();
        RAISERROR(@ErrorMessage, 16, 1);
        RETURN -1;
    END CATCH
END;
GO

-- sp_TemporalVectorSearch: Point-in-time semantic search
-- Uses temporal tables to query historical embeddings

CREATE OR ALTER PROCEDURE dbo.sp_TemporalVectorSearch
    @QueryVector VARBINARY(MAX),
    @AsOfDate DATETIME2,
    @TopK INT = 10,
    @TenantId INT = NULL -- Optional tenant filtering
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
        LEFT JOIN dbo.TenantAtoms ta ON a.AtomId = ta.AtomId
        WHERE (@TenantId IS NULL OR ta.TenantId = @TenantId)
        ORDER BY Similarity DESC;
        
        RETURN 0;
    END TRY
    BEGIN CATCH
        DECLARE @ErrorMessage NVARCHAR(4000) = ERROR_MESSAGE();
        RAISERROR(@ErrorMessage, 16, 1);
        RETURN -1;
    END CATCH
END;
GO

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
    @TenantId INT = NULL -- Optional tenant filtering
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
        
        DECLARE @Results TABLE (
            AtomId BIGINT,
            VectorScore FLOAT,
            KeywordScore FLOAT,
            SpatialScore FLOAT,
            CombinedScore FLOAT
        );
        
        -- Compute vector scores
        INSERT INTO @Results (AtomId, VectorScore, KeywordScore, SpatialScore)
        SELECT 
            ae.AtomId,
            1.0 - VECTOR_DISTANCE('cosine', ae.EmbeddingVector, @QueryVector) AS VectorScore,
            0.0 AS KeywordScore,
            0.0 AS SpatialScore
        FROM dbo.AtomEmbeddings ae
        LEFT JOIN dbo.TenantAtoms ta ON ae.AtomId = ta.AtomId
        WHERE (@TenantId IS NULL OR ta.TenantId = @TenantId);
        
        -- Add keyword scores (if keywords provided)
        IF @Keywords IS NOT NULL
        BEGIN
            UPDATE r
            SET KeywordScore = ISNULL(fts.RANK / 1000.0, 0.0) -- Normalize to 0-1
            FROM @Results r
            INNER JOIN CONTAINSTABLE(dbo.Atoms, Content, @Keywords) fts ON r.AtomId = fts.[KEY];
        END
        
        -- Add spatial scores (if region provided)
        IF @SpatialRegion IS NOT NULL
        BEGIN
            UPDATE r
            SET SpatialScore = CASE 
                WHEN a.SpatialLocation IS NOT NULL AND a.SpatialLocation.STWithin(@SpatialRegion) = 1 
                THEN 1.0
                ELSE 0.0
            END
            FROM @Results r
            INNER JOIN dbo.Atoms a ON r.AtomId = a.AtomId;
        END
        
        -- Compute combined score
        UPDATE @Results
        SET CombinedScore = 
            (VectorScore * @VectorWeight) + 
            (KeywordScore * @KeywordWeight) + 
            (SpatialScore * @SpatialWeight);
        
        -- Return top K results
        SELECT TOP (@TopK)
            r.AtomId,
            r.VectorScore,
            r.KeywordScore,
            r.SpatialScore,
            r.CombinedScore,
            a.ContentHash,
            a.ContentType,
            a.CreatedUtc
        FROM @Results r
        INNER JOIN dbo.Atoms a ON r.AtomId = a.AtomId
        -- No need for second TenantAtoms filter - already filtered in vector score computation
        ORDER BY r.CombinedScore DESC;
        
        RETURN 0;
    END TRY
    BEGIN CATCH
        DECLARE @ErrorMessage NVARCHAR(4000) = ERROR_MESSAGE();
        RAISERROR(@ErrorMessage, 16, 1);
        RETURN -1;
    END CATCH
END;
GO

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
    @TenantId INT = NULL -- Optional tenant filtering
AS
BEGIN
    SET NOCOUNT ON;
    
    BEGIN TRY
        DECLARE @EnsembleResults TABLE (
            AtomId BIGINT,
            Model1Score FLOAT,
            Model2Score FLOAT,
            Model3Score FLOAT,
            EnsembleScore FLOAT
        );
        
        -- Get all unique atoms from all models
        DECLARE @AllAtoms TABLE (AtomId BIGINT PRIMARY KEY);
        
        INSERT INTO @AllAtoms
        SELECT DISTINCT ae.AtomId
        FROM dbo.AtomEmbeddings ae
        LEFT JOIN dbo.TenantAtoms ta ON ae.AtomId = ta.AtomId
        WHERE (@TenantId IS NULL OR ta.TenantId = @TenantId)
              AND ae.ModelId IN (@Model1Id, @Model2Id, @Model3Id);
        
        -- Score each atom with each model
        INSERT INTO @EnsembleResults (AtomId, Model1Score, Model2Score, Model3Score)
        SELECT 
            aa.AtomId,
            ISNULL(
                (SELECT 1.0 - VECTOR_DISTANCE('cosine', ae1.EmbeddingVector, @QueryVector1)
                 FROM dbo.AtomEmbeddings ae1
                 WHERE ae1.AtomId = aa.AtomId AND ae1.ModelId = @Model1Id),
                0.0
            ) AS Model1Score,
            ISNULL(
                (SELECT 1.0 - VECTOR_DISTANCE('cosine', ae2.EmbeddingVector, @QueryVector2)
                 FROM dbo.AtomEmbeddings ae2
                 WHERE ae2.AtomId = aa.AtomId AND ae2.ModelId = @Model2Id),
                0.0
            ) AS Model2Score,
            ISNULL(
                (SELECT 1.0 - VECTOR_DISTANCE('cosine', ae3.EmbeddingVector, @QueryVector3)
                 FROM dbo.AtomEmbeddings ae3
                 WHERE ae3.AtomId = aa.AtomId AND ae3.ModelId = @Model3Id),
                0.0
            ) AS Model3Score
        FROM @AllAtoms aa;
        
        -- Compute weighted ensemble score
        UPDATE @EnsembleResults
        SET EnsembleScore = 
            (Model1Score * @Model1Weight) + 
            (Model2Score * @Model2Weight) + 
            (Model3Score * @Model3Weight);
        
        -- Return top K
        SELECT TOP (@TopK)
            er.AtomId,
            er.Model1Score,
            er.Model2Score,
            er.Model3Score,
            er.EnsembleScore,
            a.ContentHash,
            a.ContentType
        FROM @EnsembleResults er
        INNER JOIN dbo.Atoms a ON er.AtomId = a.AtomId
        -- No need for second TenantAtoms filter - already filtered in AllAtoms collection
        ORDER BY er.EnsembleScore DESC;
        
        RETURN 0;
    END TRY
    BEGIN CATCH
        DECLARE @ErrorMessage NVARCHAR(4000) = ERROR_MESSAGE();
        RAISERROR(@ErrorMessage, 16, 1);
        RETURN -1;
    END CATCH
END;
GO

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

    PRINT 'HYBRID SEARCH: Spatial filter + Vector rerank';

    DECLARE @wkt NVARCHAR(200) = CONCAT('POINT (', @query_spatial_x, ' ', @query_spatial_y, ' ', @query_spatial_z, ')');
    DECLARE @query_point GEOMETRY = geometry::STGeomFromText(@wkt, @srid);

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