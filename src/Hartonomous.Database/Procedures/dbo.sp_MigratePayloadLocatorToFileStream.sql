USE [$(DatabaseName)]
GO

IF OBJECT_ID('dbo.sp_MigratePayloadLocatorToFileStream', 'P') IS NOT NULL
    DROP PROCEDURE dbo.sp_MigratePayloadLocatorToFileStream;
GO

-- DEPRECATED: PayloadLocator removed in Core v5 (atomic decomposition)
-- No migration needed - use atomic values only
CREATE PROCEDURE dbo.sp_MigratePayloadLocatorToFileStream
    @BatchSize INT = 100,
    @Debug BIT = 0
AS
BEGIN
    SET NOCOUNT ON;
    
    -- DEPRECATED: No blob storage in Core v5
    RAISERROR('This procedure is deprecated. PayloadLocator removed in Core v5.', 16, 1);
    RETURN -1;
    
    DECLARE @MigratedCount INT = 0;
    DECLARE @ErrorCount INT = 0;
    DECLARE @CurrentAtomId BIGINT;
    DECLARE @PayloadPath NVARCHAR(512);
    DECLARE @PayloadBytes VARBINARY(MAX);
    
    -- Production migration logic using CLR file reader
    DECLARE atom_cursor CURSOR FOR
        SELECT AtomId, PayloadLocator
        FROM graph.AtomGraphNodes
        WHERE PayloadLocator IS NOT NULL
        AND AtomId NOT IN (SELECT AtomId FROM dbo.Atoms);
    
    OPEN atom_cursor;
    FETCH NEXT FROM atom_cursor INTO @CurrentAtomId, @PayloadPath;
    
    WHILE @@FETCH_STATUS = 0
    BEGIN
        BEGIN TRY
            -- Read file bytes using CLR function
            SET @PayloadBytes = dbo.clr_ReadFileBytes(@PayloadPath);
            
            IF @PayloadBytes IS NOT NULL
            BEGIN
                -- Insert into Atoms table with FILESTREAM payload
                INSERT INTO dbo.Atoms (AtomId, Payload, Modality, TenantId, CreatedAt)
                SELECT @CurrentAtomId, @PayloadBytes, 'binary', 0, GETUTCDATE()
                WHERE NOT EXISTS (SELECT 1 FROM dbo.Atoms WHERE AtomId = @CurrentAtomId);
                
                SET @MigratedCount = @MigratedCount + 1;
            END
        END TRY
        BEGIN CATCH
            SET @ErrorCount = @ErrorCount + 1;
            PRINT 'Migration error for AtomId ' + CAST(@CurrentAtomId AS NVARCHAR(20)) + ': ' + ERROR_MESSAGE();
        END CATCH
        
        FETCH NEXT FROM atom_cursor INTO @CurrentAtomId, @PayloadPath;
    END
    
    CLOSE atom_cursor;
    DEALLOCATE atom_cursor;
    
    PRINT 'FILESTREAM migration complete: ' + CAST(@MigratedCount AS NVARCHAR(20)) + ' migrated, ' + CAST(@ErrorCount AS NVARCHAR(20)) + ' errors';
    /* The commented out section below seems to be a duplicate or alternative logic.
       Keeping only the first complete cursor-based migration logic.
    BEGIN
        BEGIN TRY
            -- Read file bytes via CLR
            SET @PayloadBytes = dbo.clr_ReadFileBytes(@PayloadPath);
            
            -- Insert into new table with FILESTREAM
            INSERT INTO dbo.Atoms (..., Payload)
            SELECT ..., @PayloadBytes
            FROM graph.AtomGraphNodes
            WHERE AtomId = @CurrentAtomId;
            
            SET @MigratedCount = @MigratedCount + 1;
            
            IF @MigratedCount % @BatchSize = 0
                PRINT 'Migrated ' + CAST(@MigratedCount AS NVARCHAR(10)) + ' atoms...';
                
        END TRY
        BEGIN CATCH
            SET @ErrorCount = @ErrorCount + 1;
            IF @Debug = 1
                PRINT 'Error migrating AtomId ' + CAST(@CurrentAtomId AS NVARCHAR(20)) + ': ' + ERROR_MESSAGE();
        END CATCH;
        
        FETCH NEXT FROM atom_cursor INTO @CurrentAtomId, @PayloadPath;
    END;
    
    CLOSE atom_cursor;
    DEALLOCATE atom_cursor;
    */
    
    PRINT 'Migration complete: ' + CAST(@MigratedCount AS NVARCHAR(10)) + ' migrated, ' + CAST(@ErrorCount AS NVARCHAR(10)) + ' errors';
END;
GO