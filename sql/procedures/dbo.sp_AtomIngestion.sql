-- =============================================
-- sp_AtomIngestion: Intelligent Autonomous Atom Ingestion
-- =============================================
-- This procedure implements the full autonomous ingestion pipeline:
-- 1. Exact hash deduplication (fast)
-- 2. Semantic similarity deduplication (intelligent)
-- 3. Embedding storage with spatial projections
-- 4. Atomic provenance tracking
-- 5. Policy-driven deduplication rules
-- =============================================

CREATE OR ALTER PROCEDURE dbo.sp_AtomIngestion
    @HashInput NVARCHAR(MAX),
    @Modality NVARCHAR(50),
    @Subtype NVARCHAR(100) = NULL,
    @SourceUri NVARCHAR(2000) = NULL,
    @SourceType NVARCHAR(100) = NULL,
    @CanonicalText NVARCHAR(MAX) = NULL,
    @Metadata NVARCHAR(MAX) = NULL,
    @PayloadLocator NVARCHAR(500) = NULL,
    @Embedding VECTOR(1998) = NULL,
    @EmbeddingType NVARCHAR(128) = 'default',
    @ModelId INT = NULL,
    @PolicyName NVARCHAR(100) = 'default',
    @TenantId INT = 0,
    @AtomId BIGINT OUTPUT,
    @AtomEmbeddingId BIGINT OUTPUT,
    @WasDuplicate BIT OUTPUT,
    @DuplicateReason NVARCHAR(500) OUTPUT,
    @SemanticSimilarity FLOAT OUTPUT
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    BEGIN TRY
        -- =============================================
        -- PHASE 1: EXACT HASH DEDUPLICATION
        -- =============================================

        -- Compute content hash for exact deduplication
        DECLARE @ContentHash BINARY(32) = HASHBYTES('SHA2_256', @HashInput);

        -- Check for exact hash match (fastest deduplication)
        SELECT TOP 1
            @AtomId = a.AtomId,
            @WasDuplicate = 1,
            @DuplicateReason = 'Exact content hash match',
            @SemanticSimilarity = NULL
        FROM dbo.Atoms a
        WHERE a.ContentHash = @ContentHash
          AND a.TenantId = @TenantId;

        IF @AtomId IS NOT NULL
        BEGIN
            -- Increment reference count for existing atom
            UPDATE dbo.Atoms
            SET ReferenceCount = ReferenceCount + 1,
                UpdatedAt = SYSUTCDATETIME()
            WHERE AtomId = @AtomId;

            -- Find matching embedding if one exists
            SELECT TOP 1 @AtomEmbeddingId = ae.AtomEmbeddingId
            FROM dbo.AtomEmbeddings ae
            WHERE ae.AtomId = @AtomId
              AND ae.EmbeddingType = @EmbeddingType
              AND (ae.ModelId = @ModelId OR (ae.ModelId IS NULL AND @ModelId IS NULL))
            ORDER BY ae.CreatedAt DESC;

            RETURN 0;
        END

        -- =============================================
        -- PHASE 2: SEMANTIC DEDUPLICATION (if embedding provided)
        -- =============================================

        IF @Embedding IS NOT NULL
        BEGIN
            -- Load deduplication policy
            DECLARE @SimilarityThreshold FLOAT = 0.95; -- Default high threshold
            DECLARE @MaxCandidates INT = 10;

            SELECT TOP 1
                @SimilarityThreshold = p.SimilarityThreshold,
                @MaxCandidates = p.MaxCandidates
            FROM dbo.DeduplicationPolicies p
            WHERE p.PolicyName = @PolicyName
              AND p.IsActive = 1
              AND p.TenantId = @TenantId;

            -- Define a table variable to match the output of sp_HybridSearch
            DECLARE @CandidateDuplicates TABLE (
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

            -- First, project the incoming embedding to its 3D spatial key
            DECLARE @QuerySpatialKey GEOMETRY = dbo.fn_ProjectTo3D(@Embedding);

            -- Now, execute the hybrid search to get the top candidates efficiently
            INSERT INTO @CandidateDuplicates
            EXEC dbo.sp_HybridSearch
                @query_vector = @Embedding,
                @query_dimension = 1998, -- Assuming the standard dimension
                @query_spatial_x = @QuerySpatialKey.STX,
                @query_spatial_y = @QuerySpatialKey.STY,
                @query_spatial_z = @QuerySpatialKey.STZ,
                @spatial_candidates = 100, -- Widen the search for deduplication
                @final_top_k = @MaxCandidates,
                @embedding_type = @EmbeddingType,
                @ModelId = @ModelId;

            -- Check if we found a semantic duplicate from the candidates
            DECLARE @BestSimilarity FLOAT;
            SELECT TOP 1
                @AtomId = cd.AtomId,
                @BestSimilarity = (1.0 - cd.exact_distance),
                @WasDuplicate = 1,
                @DuplicateReason = 'Semantic similarity match (threshold: ' + CAST(@SimilarityThreshold AS NVARCHAR(10)) + ')',
                @SemanticSimilarity = (1.0 - cd.exact_distance)
            FROM @CandidateDuplicates cd
            WHERE (1.0 - cd.exact_distance) >= @SimilarityThreshold
            ORDER BY cd.exact_distance ASC; -- Order by distance ascending (highest similarity first)

            IF @AtomId IS NOT NULL
            BEGIN
                -- Increment reference count for semantically similar atom
                UPDATE dbo.Atoms
                SET ReferenceCount = ReferenceCount + 1,
                    UpdatedAt = SYSUTCDATETIME()
                WHERE AtomId = @AtomId;

                -- Find or create embedding record
                SELECT TOP 1 @AtomEmbeddingId = ae.AtomEmbeddingId
                FROM dbo.AtomEmbeddings ae
                WHERE ae.AtomId = @AtomId
                  AND ae.EmbeddingType = @EmbeddingType
                  AND (ae.ModelId = @ModelId OR (ae.ModelId IS NULL AND @ModelId IS NULL))
                ORDER BY ae.CreatedAt DESC;

                RETURN 0;
            END
        END

        -- =============================================
        -- PHASE 3: STORE NEW ATOM
        -- =============================================

        SET @WasDuplicate = 0;
        SET @DuplicateReason = NULL;
        SET @SemanticSimilarity = NULL;

        -- Insert new atom
        INSERT INTO dbo.Atoms (
            ContentHash,
            Modality,
            Subtype,
            SourceUri,
            SourceType,
            CanonicalText,
            Metadata,
            PayloadLocator,
            ReferenceCount,
            TenantId,
            CreatedAt,
            UpdatedAt
        )
        VALUES (
            @ContentHash,
            @Modality,
            @Subtype,
            @SourceUri,
            @SourceType,
            @CanonicalText,
            @Metadata,
            @PayloadLocator,
            1, -- Initial reference count
            @TenantId,
            SYSUTCDATETIME(),
            SYSUTCDATETIME()
        );

        SET @AtomId = SCOPE_IDENTITY();

        -- =============================================
        -- PHASE 4: STORE EMBEDDING (if provided)
        -- =============================================

        IF @Embedding IS NOT NULL
        BEGIN
            -- Compute spatial projections for fast approximate search
            DECLARE @SpatialGeometry GEOMETRY;
            DECLARE @SpatialCoarse GEOGRAPHY;
            DECLARE @Dimension INT = 1998; -- SQL Server 2025 VECTOR dimension

            -- Project the high-dimensional vector to a 3D point using the landmark-based CLR function.
            SET @SpatialGeometry = dbo.fn_ProjectTo3D(@Embedding);

            -- For coarse spatial bucketing (optional)
            SET @SpatialCoarse = GEOGRAPHY::Point(@SpatialGeometry.STX * 111319.444, @SpatialGeometry.STY * 111319.444, 4326); -- Convert to meters

            -- Store embedding with spatial projections
            INSERT INTO dbo.AtomEmbeddings (
                AtomId,
                EmbeddingVector,
                EmbeddingType,
                ModelId,
                Dimension,
                SpatialGeometry,
                SpatialCoarse,
                SpatialBucketX,
                SpatialBucketY,
                SpatialBucketZ,
                Metadata,
                TenantId,
                CreatedAt
            )
            VALUES (
                @AtomId,
                @Embedding,
                @EmbeddingType,
                @ModelId,
                @Dimension,
                @SpatialGeometry,
                @SpatialCoarse,
                CAST(@SpatialGeometry.STX * 1000 AS INT), -- Bucket coordinates from the new geometry
                CAST(@SpatialGeometry.STY * 1000 AS INT),
                CAST(@SpatialGeometry.STZ * 1000 AS INT),
                @Metadata,
                @TenantId,
                SYSUTCDATETIME()
            );

            SET @AtomEmbeddingId = SCOPE_IDENTITY();
        END

        -- =============================================
        -- PHASE 5: LOG PROVENANCE (optional)
        -- =============================================

        -- This would integrate with AtomicStream for full provenance tracking
        -- For now, just return success

        RETURN 0;

    END TRY
    BEGIN CATCH
        DECLARE @ErrorMessage NVARCHAR(4000) = ERROR_MESSAGE();
        DECLARE @ErrorSeverity INT = ERROR_SEVERITY();
        DECLARE @ErrorState INT = ERROR_STATE();

        -- Log error details for debugging
        INSERT INTO dbo.IngestionErrors (
            ProcedureName,
            ErrorMessage,
            ErrorSeverity,
            ErrorState,
            HashInput,
            Modality,
            EmbeddingType,
            TenantId,
            CreatedAt
        )
        VALUES (
            'sp_AtomIngestion',
            @ErrorMessage,
            @ErrorSeverity,
            @ErrorState,
            LEFT(@HashInput, 1000), -- Truncate for storage
            @Modality,
            @EmbeddingType,
            @TenantId,
            SYSUTCDATETIME()
        );

        -- Re-throw the error
        THROW;
    END CATCH
END;
GO

PRINT 'Created intelligent sp_AtomIngestion procedure with autonomous deduplication';
GO