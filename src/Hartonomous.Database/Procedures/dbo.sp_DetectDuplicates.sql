CREATE PROCEDURE dbo.sp_DetectDuplicates
    @SimilarityThreshold FLOAT = 0.95,
    @BatchSize INT = 1000,
    @TenantId INT = 0
AS
BEGIN
    SET NOCOUNT ON;
    
    BEGIN TRY
        DECLARE @DuplicateGroups TABLE (
            PrimaryAtomId BIGINT,
            DuplicateAtomId BIGINT,
            Similarity FLOAT
        );
        
        -- Find duplicate pairs using self-join on embeddings
        INSERT INTO @DuplicateGroups
        SELECT TOP (@BatchSize)
            ae1.AtomId AS PrimaryAtomId,
            ae2.AtomId AS DuplicateAtomId,
            1.0 - VECTOR_DISTANCE('cosine', ae1.SpatialKey, ae2.SpatialKey) AS Similarity
        FROM dbo.AtomEmbeddings ae1
        INNER JOIN dbo.AtomEmbeddings ae2 
            ON ae1.ModelId = ae2.ModelId 
            AND ae1.AtomId < ae2.AtomId -- Avoid duplicate pairs
        INNER JOIN dbo.Atoms a1 ON ae1.AtomId = a1.AtomId
        INNER JOIN dbo.Atoms a2 ON ae2.AtomId = a2.AtomId
        WHERE a1.TenantId = @TenantId
              AND a2.TenantId = @TenantId
              AND (1.0 - VECTOR_DISTANCE('cosine', ae1.SpatialKey, ae2.SpatialKey)) >= @SimilarityThreshold
        ORDER BY Similarity DESC;
        
        -- Return duplicate groups
        SELECT 
            dg.PrimaryAtomId,
            dg.DuplicateAtomId,
            dg.Similarity,
            a1.ContentHash AS PrimaryHash,
            a2.ContentHash AS DuplicateHash,
            a1.CreatedAt AS PrimaryCreated,
            a2.CreatedAt AS DuplicateCreated
        FROM @DuplicateGroups dg
        INNER JOIN dbo.Atoms a1 ON dg.PrimaryAtomId = a1.AtomId
        INNER JOIN dbo.Atoms a2 ON dg.DuplicateAtomId = a2.AtomId
        ORDER BY dg.Similarity DESC;
        
        RETURN 0;
    END TRY
    BEGIN CATCH
        DECLARE @ErrorMessage NVARCHAR(4000) = ERROR_MESSAGE();
        RAISERROR(@ErrorMessage, 16, 1);
        RETURN -1;
    END CATCH
END;