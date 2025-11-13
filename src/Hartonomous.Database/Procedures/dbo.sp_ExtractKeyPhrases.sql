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
        DECLARE @Content NVARCHAR(MAX);
        
        -- Get atom content
        SELECT @Content = CAST(Content AS NVARCHAR(MAX))
        FROM dbo.Atoms
        WHERE AtomId = @AtomId AND TenantId = @TenantId;
        
        IF @Content IS NULL
        BEGIN
            SELECT '[]' AS JSON_F52E2B61_18A1_11d1_B105_00805F49916B;
            RETURN 0;
        END;
        
        -- Use SQL Server string functions to extract phrases
        -- This is a simplified implementation - for production, use your CLR transformer for NER
        WITH Words AS (
            SELECT 
                value AS word,
                ROW_NUMBER() OVER (ORDER BY (SELECT NULL)) AS position
            FROM STRING_SPLIT(REPLACE(REPLACE(@Content, '.', ' '), ',', ' '), ' ')
            WHERE LEN(value) > 3 -- Filter short words
        ),
        WordCounts AS (
            SELECT 
                LOWER(word) AS keyphrase,
                COUNT(*) AS frequency
            FROM Words
            WHERE word NOT IN ('the', 'and', 'for', 'with', 'this', 'that', 'from', 'have', 'been', 'will')
            GROUP BY LOWER(word)
        )
        SELECT TOP (@TopK)
            keyphrase,
            CAST(frequency AS FLOAT) / (SELECT MAX(frequency) FROM WordCounts) * 100 AS score
        FROM WordCounts
        ORDER BY frequency DESC
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
