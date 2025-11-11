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
        DECLARE @ErrorMessage NVARCHAR(4000) = ERROR_MESSAGE();
        RAISERROR(@ErrorMessage, 16, 1);
        RETURN -1;
    END CATCH
END;
