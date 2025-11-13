CREATE PROCEDURE dbo.sp_TemporalVectorSearch
    @QueryVector VECTOR(1998),
    @AsOfDate DATETIME2,
    @TopK INT = 10,
    @TenantId INT = NULL -- Optional tenant filtering
AS
BEGIN
    SET NOCOUNT ON;
    
    BEGIN TRY
        -- Query embeddings filtered by last computation time
        -- NOTE: AtomEmbeddings and Atoms are NOT system-versioned tables
        -- Use LastComputedUtc filter instead for point-in-time queries
        SELECT TOP (@TopK)
            ae.AtomId,
            1.0 - VECTOR_DISTANCE('cosine', ae.EmbeddingVector, @QueryVector) AS Similarity,
            ae.LastComputedUtc,
            a.ContentHash,
            a.ContentType
        FROM dbo.AtomEmbeddings ae
        INNER JOIN dbo.Atoms a ON ae.AtomId = a.AtomId
        LEFT JOIN dbo.TenantAtoms ta ON a.AtomId = ta.AtomId
        WHERE (@TenantId IS NULL OR ta.TenantId = @TenantId)
            AND ae.LastComputedUtc <= @AsOfDate  -- Filter by computation timestamp
        ORDER BY Similarity DESC;
        
        RETURN 0;
    END TRY
    BEGIN CATCH
        DECLARE @ErrorMessage NVARCHAR(4000) = ERROR_MESSAGE();
        RAISERROR(@ErrorMessage, 16, 1);
        RETURN -1;
    END CATCH
END;