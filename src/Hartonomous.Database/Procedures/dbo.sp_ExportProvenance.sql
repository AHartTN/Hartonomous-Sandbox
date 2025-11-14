-- Auto-split from dbo.ProvenanceFunctions.sql
-- Object: PROCEDURE dbo.sp_ExportProvenance

CREATE PROCEDURE dbo.sp_ExportProvenance
    @AtomId BIGINT,
    @Format NVARCHAR(20) = 'JSON', -- 'JSON', 'GraphML', 'CSV'
    @TenantId INT = 0
AS
BEGIN
    SET NOCOUNT ON;
    
    BEGIN TRY
        IF @Format = 'JSON'
        BEGIN
            -- Export as nested JSON structure
            WITH Lineage AS (
                SELECT 
                    @AtomId AS AtomId,
                    0 AS Depth
                
                UNION ALL
                
                SELECT 
                    edge.FromAtomId AS AtomId,
                    l.Depth + 1 AS Depth
                FROM Lineage l
                INNER JOIN provenance.AtomGraphEdges edge ON l.AtomId = edge.ToAtomId
                WHERE l.Depth < 50
            )
            SELECT 
                a.AtomId,
                a.ContentHash,
                a.ContentType,
                a.CreatedAt,
                l.Depth AS LineageDepth,
                (
                    SELECT 
                        parent.AtomId,
                        parent.ContentHash,
                        parent.CreatedAt
                      FROM provenance.AtomGraphEdges edge
                      INNER JOIN dbo.Atoms parent ON edge.FromAtomId = parent.AtomId
                      WHERE edge.ToAtomId = a.AtomId
                    FOR JSON PATH
                ) AS Parents
            FROM Lineage l
            INNER JOIN dbo.Atoms a ON l.AtomId = a.AtomId
            WHERE a.TenantId = @TenantId
            FOR JSON PATH, ROOT('provenance');
        END
        ELSE IF @Format = 'GraphML'
        BEGIN
            -- Export as GraphML XML (simplified)
            SELECT 
                edge.FromAtomId AS SourceAtomId,
                edge.ToAtomId AS TargetAtomId,
                'DerivedFrom' AS EdgeType
            FROM provenance.AtomGraphEdges edge
            INNER JOIN dbo.Atoms a1 ON edge.FromAtomId = a1.AtomId
            INNER JOIN dbo.Atoms a2 ON edge.ToAtomId = a2.AtomId
            WHERE a1.TenantId = @TenantId
                  AND a2.TenantId = @TenantId
                  AND (a1.AtomId = @AtomId OR a2.AtomId = @AtomId)
            FOR XML PATH('edge'), ROOT('graph');
        END
        
        RETURN 0;
    END TRY
    BEGIN CATCH
        DECLARE @ErrorMessage NVARCHAR(4000) = ERROR_MESSAGE();
        RAISERROR(@ErrorMessage, 16, 1);
        RETURN -1;
    END CATCH
END;
GO

-- sp_VerifyIntegrity: Tamper detection via checksum validation
-- Compares stored checksums with recomputed values


GO
