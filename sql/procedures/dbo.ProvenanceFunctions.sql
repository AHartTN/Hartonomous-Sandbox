-- sp_QueryLineage: Graph-based provenance traversal
-- Uses SHORTEST_PATH to find atom ancestry

CREATE OR ALTER PROCEDURE dbo.sp_QueryLineage
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
GO

-- sp_FindImpactedAtoms: Impact analysis for data deletion
-- Returns all downstream atoms that would be affected

CREATE OR ALTER PROCEDURE dbo.sp_FindImpactedAtoms
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
GO

-- sp_ExportProvenance: Export lineage as JSON
-- Compliance/audit export format

CREATE OR ALTER PROCEDURE dbo.sp_ExportProvenance
    @AtomId BIGINT,
    @Format NVARCHAR(20) = 'JSON', -- 'JSON', 'GraphML', 'CSV'
    @TenantId INT = NULL -- Optional tenant filtering
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
                    edge.$from_id AS AtomId,
                    l.Depth + 1 AS Depth
                FROM Lineage l
                INNER JOIN provenance.AtomGraphEdges edge ON l.AtomId = edge.$to_id
                WHERE l.Depth < 50
            )
            SELECT 
                a.AtomId,
                a.ContentHash,
                a.ContentType,
                a.CreatedUtc,
                l.Depth AS LineageDepth,
                (
                    SELECT 
                        parent.AtomId,
                        parent.ContentHash,
                        parent.CreatedUtc
                    FROM provenance.AtomGraphEdges edge
                    INNER JOIN dbo.Atoms parent ON edge.$from_id = parent.AtomId
                    WHERE edge.$to_id = a.AtomId
                    FOR JSON PATH
                ) AS Parents
            FROM Lineage l
            INNER JOIN dbo.Atoms a ON l.AtomId = a.AtomId
            LEFT JOIN dbo.TenantAtoms ta ON a.AtomId = ta.AtomId
            WHERE (@TenantId IS NULL OR ta.TenantId = @TenantId)
            FOR JSON PATH, ROOT('provenance');
        END
        ELSE IF @Format = 'GraphML'
        BEGIN
            -- Export as GraphML XML (simplified)
            SELECT 
                edge.$from_id AS SourceAtomId,
                edge.$to_id AS TargetAtomId,
                'DerivedFrom' AS EdgeType
            FROM provenance.AtomGraphEdges edge
            INNER JOIN dbo.Atoms a1 ON edge.$from_id = a1.AtomId
            INNER JOIN dbo.Atoms a2 ON edge.$to_id = a2.AtomId
            LEFT JOIN dbo.TenantAtoms ta1 ON a1.AtomId = ta1.AtomId
            LEFT JOIN dbo.TenantAtoms ta2 ON a2.AtomId = ta2.AtomId
            WHERE (a1.AtomId = @AtomId OR a2.AtomId = @AtomId)
                  AND (@TenantId IS NULL OR ta1.TenantId = @TenantId OR ta2.TenantId = @TenantId)
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

CREATE OR ALTER PROCEDURE dbo.sp_VerifyIntegrity
    @AtomId BIGINT = NULL,
    @TenantId INT = NULL -- Optional tenant filtering
AS
BEGIN
    SET NOCOUNT ON;
    
    BEGIN TRY
        DECLARE @CorruptedCount INT = 0;
        
        -- Create temp table for integrity check results
        DECLARE @IntegrityResults TABLE (
            AtomId BIGINT,
            StoredChecksum NVARCHAR(64),
            ComputedChecksum NVARCHAR(64),
            IsCorrupted BIT,
            CheckedUtc DATETIME2 DEFAULT SYSUTCDATETIME()
        );
        
        -- Check specific atom or all atoms
        INSERT INTO @IntegrityResults (AtomId, StoredChecksum, ComputedChecksum, IsCorrupted)
        SELECT 
            a.AtomId,
            a.ContentHash AS StoredChecksum,
            CONVERT(NVARCHAR(64), HASHBYTES('SHA2_256', a.Content), 2) AS ComputedChecksum,
            CASE 
                WHEN a.ContentHash = CONVERT(NVARCHAR(64), HASHBYTES('SHA2_256', a.Content), 2) THEN 0
                ELSE 1
            END AS IsCorrupted
        FROM dbo.Atoms a
        LEFT JOIN dbo.TenantAtoms ta ON a.AtomId = ta.AtomId
        WHERE (@TenantId IS NULL OR ta.TenantId = @TenantId)
              AND a.IsDeleted = 0
              AND (@AtomId IS NULL OR a.AtomId = @AtomId);
        
        SET @CorruptedCount = (SELECT COUNT(*) FROM @IntegrityResults WHERE IsCorrupted = 1);
        
        -- Return results
        SELECT 
            AtomId,
            StoredChecksum,
            ComputedChecksum,
            IsCorrupted,
            CheckedUtc
        FROM @IntegrityResults
        ORDER BY IsCorrupted DESC, AtomId;
        
        -- Log integrity check
        IF @CorruptedCount > 0
        BEGIN
            PRINT 'WARNING: ' + CAST(@CorruptedCount AS VARCHAR(10)) + ' corrupted atoms detected!';
        END
        ELSE
        BEGIN
            PRINT 'Integrity check passed: All checksums valid';
        END
        
        RETURN @CorruptedCount;
    END TRY
    BEGIN CATCH
        DECLARE @ErrorMessage NVARCHAR(4000) = ERROR_MESSAGE();
        RAISERROR(@ErrorMessage, 16, 1);
        RETURN -1;
    END CATCH
END;
GO
