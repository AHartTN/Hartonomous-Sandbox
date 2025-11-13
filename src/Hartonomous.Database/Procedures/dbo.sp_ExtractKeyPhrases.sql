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
        -- Check if semantic index exists
        IF NOT EXISTS (
            SELECT 1 
            FROM sys.fulltext_indexes fi
            INNER JOIN sys.fulltext_index_columns fic ON fi.object_id = fic.object_id
            WHERE fi.object_id = OBJECT_ID('dbo.Atoms')
            AND fic.statistical_semantics = 1
        )
        BEGIN
            SELECT '[]' AS JSON_F52E2B61_18A1_11d1_B105_00805F49916B;
            RETURN 0;
        END
        
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
