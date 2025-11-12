CREATE PROCEDURE dbo.sp_FindImpactedAtoms
    @AtomId BIGINT,
    @TenantId INT = NULL -- Optional tenant filtering
AS
BEGIN
    SET NOCOUNT ON;
    
    BEGIN TRY
        WITH ImpactedAtoms AS (
            SELECT 
                @AtomId AS AtomId,
                0 AS Depth,
                'Source' AS ImpactType
            
            UNION ALL
            
            SELECT 
                edge.$to_id AS AtomId,
                ia.Depth + 1 AS Depth,
                'Downstream' AS ImpactType
            FROM ImpactedAtoms ia
            INNER JOIN provenance.AtomGraphEdges edge ON ia.AtomId = edge.$from_id
            WHERE ia.Depth < 100 -- Prevent infinite recursion
        )
        SELECT 
            ia.AtomId,
            ia.Depth,
            ia.ImpactType,
            a.ContentHash,
            a.ContentType,
            a.CreatedUtc,
            COUNT(*) OVER () AS TotalImpacted
        FROM ImpactedAtoms ia
        INNER JOIN dbo.Atoms a ON ia.AtomId = a.AtomId
        LEFT JOIN dbo.TenantAtoms ta ON a.AtomId = ta.AtomId
        WHERE (@TenantId IS NULL OR ta.TenantId = @TenantId)
        ORDER BY ia.Depth, ia.AtomId;
        
        RETURN 0;
    END TRY
    BEGIN CATCH
        DECLARE @ErrorMessage NVARCHAR(4000) = ERROR_MESSAGE();
        RAISERROR(@ErrorMessage, 16, 1);
        RETURN -1;
    END CATCH
END;