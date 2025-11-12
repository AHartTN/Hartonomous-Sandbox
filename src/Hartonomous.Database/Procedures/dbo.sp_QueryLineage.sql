CREATE PROCEDURE dbo.sp_QueryLineage
    @AtomId BIGINT,
    @Direction NVARCHAR(20) = 'Upstream', -- 'Upstream', 'Downstream', 'Both'
    @MaxDepth INT = 10,
    @TenantId INT = NULL -- Optional tenant filtering
AS
BEGIN
    SET NOCOUNT ON;
    
    BEGIN TRY
        IF @Direction = 'Upstream' OR @Direction = 'Both'
        BEGIN
            -- Traverse upstream: Find all ancestors
            WITH UpstreamLineage AS (
                SELECT 
                    @AtomId AS AtomId,
                    0 AS Depth,
                    CAST(@AtomId AS NVARCHAR(MAX)) AS Path
                
                UNION ALL
                
                SELECT 
                    edge.$from_id AS AtomId,
                    ul.Depth + 1 AS Depth,
                    CAST(edge.$from_id AS NVARCHAR(MAX)) + ' -> ' + ul.Path AS Path
                FROM UpstreamLineage ul
                INNER JOIN provenance.AtomGraphEdges edge ON ul.AtomId = edge.$to_id
                WHERE ul.Depth < @MaxDepth
            )
            SELECT 
                ul.AtomId,
                ul.Depth,
                ul.Path,
                a.ContentHash,
                a.ContentType,
                a.CreatedUtc
            FROM UpstreamLineage ul
            INNER JOIN dbo.Atoms a ON ul.AtomId = a.AtomId
            LEFT JOIN dbo.TenantAtoms ta ON a.AtomId = ta.AtomId
            WHERE (@TenantId IS NULL OR ta.TenantId = @TenantId)
            ORDER BY ul.Depth;
        END
        
        IF @Direction = 'Downstream' OR @Direction = 'Both'
        BEGIN
            -- Traverse downstream: Find all descendants
            WITH DownstreamLineage AS (
                SELECT 
                    @AtomId AS AtomId,
                    0 AS Depth,
                    CAST(@AtomId AS NVARCHAR(MAX)) AS Path
                
                UNION ALL
                
                SELECT 
                    edge.$to_id AS AtomId,
                    dl.Depth + 1 AS Depth,
                    dl.Path + ' -> ' + CAST(edge.$to_id AS NVARCHAR(MAX)) AS Path
                FROM DownstreamLineage dl
                INNER JOIN provenance.AtomGraphEdges edge ON dl.AtomId = edge.$from_id
                WHERE dl.Depth < @MaxDepth
            )
            SELECT 
                dl.AtomId,
                dl.Depth,
                dl.Path,
                a.ContentHash,
                a.ContentType,
                a.CreatedUtc
            FROM DownstreamLineage dl
            INNER JOIN dbo.Atoms a ON dl.AtomId = a.AtomId
            LEFT JOIN dbo.TenantAtoms ta ON a.AtomId = ta.AtomId
            WHERE (@TenantId IS NULL OR ta.TenantId = @TenantId)
            ORDER BY dl.Depth;
        END
        
        RETURN 0;
    END TRY
    BEGIN CATCH
        DECLARE @ErrorMessage NVARCHAR(4000) = ERROR_MESSAGE();
        RAISERROR(@ErrorMessage, 16, 1);
        RETURN -1;
    END CATCH
END;