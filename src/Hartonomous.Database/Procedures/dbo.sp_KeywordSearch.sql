CREATE PROCEDURE dbo.sp_KeywordSearch
    @Keywords NVARCHAR(MAX),
    @TopK INT = 10,
    @TenantId INT = NULL, -- Optional tenant filtering
    @ContentTypeFilter NVARCHAR(100) = NULL
AS
BEGIN
    SET NOCOUNT ON;
    
    BEGIN TRY
        SELECT TOP (@TopK)
            a.AtomId,
            a.ContentHash,
            a.ContentType,
            a.CreatedUtc,
            fts.RANK AS RelevanceScore,
            CAST(a.Content AS NVARCHAR(MAX)) AS ContentPreview
        FROM CONTAINSTABLE(dbo.Atoms, Content, @Keywords) fts
        INNER JOIN dbo.Atoms a ON fts.[KEY] = a.AtomId
        LEFT JOIN dbo.TenantAtoms ta ON a.AtomId = ta.AtomId
        WHERE (@TenantId IS NULL OR ta.TenantId = @TenantId)
              AND a.IsDeleted = 0
              AND (@ContentTypeFilter IS NULL OR a.ContentType = @ContentTypeFilter)
        ORDER BY fts.RANK DESC;
        
        RETURN 0;
    END TRY
    BEGIN CATCH
        DECLARE @ErrorMessage NVARCHAR(4000) = ERROR_MESSAGE();
        RAISERROR(@ErrorMessage, 16, 1);
        RETURN -1;
    END CATCH
END;