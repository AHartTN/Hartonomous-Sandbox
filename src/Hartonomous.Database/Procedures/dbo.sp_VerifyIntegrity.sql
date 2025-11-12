-- Auto-split from dbo.ProvenanceFunctions.sql
-- Object: PROCEDURE dbo.sp_VerifyIntegrity

CREATE PROCEDURE dbo.sp_VerifyIntegrity
    @AtomId BIGINT = NULL,
    @TenantId INT = 0
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
        WHERE a.TenantId = @TenantId
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