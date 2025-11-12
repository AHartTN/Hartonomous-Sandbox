CREATE PROCEDURE dbo.sp_SemanticSimilarity
    @SourceAtomId BIGINT,
    @TopK INT = 10,
    @TenantId INT = NULL -- Optional tenant filtering
AS
BEGIN
    SET NOCOUNT ON;
    
    BEGIN TRY
        -- Verify full-text and semantic search is enabled
        IF NOT EXISTS (
            SELECT 1 
            FROM sys.fulltext_indexes fti
            INNER JOIN sys.objects o ON fti.object_id = o.object_id
            WHERE o.name = 'Atoms'
        )
        BEGIN
            RAISERROR('Full-text index not found on Atoms table', 16, 1);
            RETURN -1;
        END
        
        -- Find similar documents
        SELECT TOP (@TopK)
            sst.matched_document_key AS SimilarAtomId,
            sst.score AS SimilarityScore,
            a.ContentHash,
            a.ContentType,
            a.CreatedUtc
        FROM SEMANTICSIMILARITYTABLE(dbo.Atoms, Content, @SourceAtomId) sst
        INNER JOIN dbo.Atoms a ON sst.matched_document_key = a.AtomId
        LEFT JOIN dbo.TenantAtoms ta ON a.AtomId = ta.AtomId
        WHERE (@TenantId IS NULL OR ta.TenantId = @TenantId)
              AND a.IsDeleted = 0
        ORDER BY sst.score DESC;
        
        RETURN 0;
    END TRY
    BEGIN CATCH
        DECLARE @ErrorMessage NVARCHAR(4000) = ERROR_MESSAGE();
        RAISERROR(@ErrorMessage, 16, 1);
        RETURN -1;
    END CATCH
END;