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
            WHERE ae.TenantId = @TenantId
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
            WHERE ae.TenantId = @TenantId
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
