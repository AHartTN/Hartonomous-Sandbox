-- Auto-split from dbo.FullTextSearch.sql
-- Object: PROCEDURE dbo.sp_ExtractKeyPhrases

CREATE PROCEDURE dbo.sp_ExtractKeyPhrases
    @AtomId BIGINT,
    @TopK INT = 20,
    @TenantId INT = 0
AS
BEGIN
    SET NOCOUNT ON;
    
    BEGIN TRY
        SELECT TOP (@TopK)
            keyphrase,
            score
        FROM SEMANTICKEYPHRASETABLE(dbo.Atoms, Content, @AtomId)
        ORDER BY score DESC
        FOR JSON PATH;
        
        RETURN 0;
    END TRY
    BEGIN CATCH
        DECLARE @ErrorMessage NVARCHAR(4000) = ERROR_MESSAGE();
        RAISERROR(@ErrorMessage, 16, 1);
        RETURN -1;
    END CATCH
END;
GO

-- sp_FindRelatedDocuments: Multi-signal document discovery
-- Combines FTS + vector + graph for comprehensive results


GO
