-- Auto-split from dbo.ProvenanceFunctions.sql
-- Object: PROCEDURE dbo.sp_FindImpactedAtoms

CREATE PROCEDURE dbo.sp_FindImpactedAtoms
    @AtomId BIGINT,
    @TenantId INT = 0
AS
BEGIN
    SET NOCOUNT ON;
    
    BEGIN TRY
        WITH ImpactedAtoms AS (
            SELECT 
                @AtomId AS AtomId,
                0 AS Depth,
                CAST('Source' AS NVARCHAR(20)) AS ImpactType
            
            UNION ALL
            
            SELECT 
                edge.ToAtomId AS AtomId,
                ia.Depth + 1 AS Depth,
                CAST('Downstream' AS NVARCHAR(20)) AS ImpactType
            FROM ImpactedAtoms ia
            INNER JOIN provenance.AtomGraphEdges edge ON ia.AtomId = edge.FromAtomId
            WHERE ia.Depth < 100 -- Prevent infinite recursion
        )
        SELECT 
            ia.AtomId,
            ia.Depth,
            ia.ImpactType,
            a.ContentHash,
            a.ContentType,
            a.CreatedAt,
            COUNT(*) OVER () AS TotalImpacted
        FROM ImpactedAtoms ia
        INNER JOIN dbo.Atoms a ON ia.AtomId = a.AtomId
        WHERE a.TenantId = @TenantId
        ORDER BY ia.Depth, ia.AtomId;
        
        RETURN 0;
    END TRY
    BEGIN CATCH
        DECLARE @ErrorMessage NVARCHAR(4000) = ERROR_MESSAGE();
        RAISERROR(@ErrorMessage, 16, 1);
        RETURN -1;
    END CATCH
END;
GO

-- sp_ExportProvenance: Export lineage as JSON
-- Compliance/audit export format


GO
