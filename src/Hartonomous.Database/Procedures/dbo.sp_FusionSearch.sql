CREATE PROCEDURE dbo.sp_FusionSearch
    @QueryVector VECTOR(1998),
    @Keywords NVARCHAR(MAX) = NULL,
    @SpatialRegion GEOMETRY = NULL,
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
        FROM dbo.AtomEmbedding ae
        LEFT JOIN dbo.TenantAtoms ta ON ae.AtomId = ta.AtomId
        WHERE (@TenantId IS NULL OR ta.TenantId = @TenantId);
        
        -- Add keyword scores (if keywords provided)
        -- CONTAINSTABLE requires dynamic SQL for NVARCHAR(MAX) parameters
        IF @Keywords IS NOT NULL AND LEN(@Keywords) > 0
        BEGIN
            -- Create temp table for CONTAINSTABLE results since table variables can't be used in dynamic SQL
            CREATE TABLE #FTSResults (AtomId BIGINT, FTSRank INT);
            
            DECLARE @SQL NVARCHAR(MAX) = N'
                INSERT INTO #FTSResults (AtomId, FTSRank)
                SELECT [KEY], RANK 
                FROM CONTAINSTABLE(dbo.Atom, Content, @SearchTerm)';
            
            EXEC sp_executesql @SQL, N'@SearchTerm NVARCHAR(4000)', @Keywords;
            
            UPDATE r
            SET KeywordScore = ISNULL(fts.FTSRank / 1000.0, 0.0)
            FROM @Results r
            INNER JOIN #FTSResults fts ON r.AtomId = fts.AtomId;
            
            DROP TABLE #FTSResults;
        END
        
        -- Add spatial scores (if region provided)
        IF @SpatialRegion IS NOT NULL
        BEGIN
            UPDATE r
            SET SpatialScore = CASE 
                WHEN ae.SpatialKey IS NOT NULL AND ae.SpatialKey.STWithin(@SpatialRegion) = 1 
                THEN 1.0
                ELSE 0.0
            END
            FROM @Results r
            INNER JOIN dbo.AtomEmbedding ae ON r.AtomId = ae.AtomId;
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
            a.CreatedAt
        FROM @Results r
        INNER JOIN dbo.Atom a ON r.AtomId = a.AtomId
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
