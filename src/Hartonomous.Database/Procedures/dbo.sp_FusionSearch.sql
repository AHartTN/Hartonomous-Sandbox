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
        WHERE ae.TenantId = @TenantId;
        
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
        WHERE a.TenantId = @TenantId
        ORDER BY r.CombinedScore DESC;
        
        RETURN 0;
    END TRY
    BEGIN CATCH
        DECLARE @ErrorMessage NVARCHAR(4000) = ERROR_MESSAGE();
        RAISERROR(@ErrorMessage, 16, 1);
        RETURN -1;
    END CATCH
END;
