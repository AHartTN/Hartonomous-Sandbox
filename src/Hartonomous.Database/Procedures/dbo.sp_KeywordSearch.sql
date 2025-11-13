-- Auto-split from dbo.FullTextSearch.sql
-- Object: PROCEDURE dbo.sp_KeywordSearch

CREATE PROCEDURE dbo.sp_KeywordSearch
    @Keywords NVARCHAR(MAX),
    @TopK INT = 10,
    @TenantId INT = 0,
    @ContentTypeFilter NVARCHAR(100) = NULL
AS
BEGIN
    SET NOCOUNT ON;
    
    BEGIN TRY
        -- CONTAINSTABLE doesn't accept NVARCHAR(MAX) parameters
        -- Must use dynamic SQL with temp table for results
        CREATE TABLE #FTSResults (AtomId BIGINT, RANK INT);
        
        DECLARE @SQL NVARCHAR(MAX) = N'
            INSERT INTO #FTSResults (AtomId, RANK)
            SELECT [KEY], RANK
            FROM CONTAINSTABLE(dbo.Atoms, Content, @KeywordsParam)';
        
        EXEC sp_executesql @SQL, N'@KeywordsParam NVARCHAR(MAX)', @KeywordsParam = @Keywords;
        
        SELECT TOP (@TopK)
            a.AtomId,
            a.ContentHash,
            a.ContentType,
            a.CreatedUtc,
            fts.RANK AS RelevanceScore,
            CAST(a.Content AS NVARCHAR(MAX)) AS ContentPreview
        FROM #FTSResults fts
        INNER JOIN dbo.Atoms a ON fts.AtomId = a.AtomId
        WHERE a.TenantId = @TenantId
              AND a.IsDeleted = 0
              AND (@ContentTypeFilter IS NULL OR a.ContentType = @ContentTypeFilter)
        ORDER BY fts.RANK DESC;
        
        DROP TABLE #FTSResults;
        RETURN 0;
    END TRY
    BEGIN CATCH
        IF OBJECT_ID('tempdb..#FTSResults') IS NOT NULL
            DROP TABLE #FTSResults;
        DECLARE @ErrorMessage NVARCHAR(4000) = ERROR_MESSAGE();
        RAISERROR(@ErrorMessage, 16, 1);
        RETURN -1;
    END CATCH
END;
GO

-- sp_SemanticSimilarity: Document similarity using semantic search
-- Finds documents similar to a given document


GO
