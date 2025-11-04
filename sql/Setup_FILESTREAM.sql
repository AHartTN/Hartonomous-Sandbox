-- =============================================
-- FILESTREAM Setup for Atom Payloads
-- =============================================
-- Migrates PayloadLocator (string path) to Payload (VARBINARY FILESTREAM)
-- Provides transactional ACID guarantees for BLOBs
-- =============================================

USE Hartonomous;
GO

-- Step 1: Enable FILESTREAM at instance level (requires manual config)
-- This is informational - must be done via SQL Server Configuration Manager
PRINT '=======================================================';
PRINT 'FILESTREAM Prerequisites:';
PRINT '1. Enable FILESTREAM in SQL Server Configuration Manager';
PRINT '2. Set FILESTREAM access level (Transact-SQL, File I/O, or Both)';
PRINT '3. Restart SQL Server instance';
PRINT '4. Run: EXEC sp_configure ''filestream access level'', 2; RECONFIGURE;';
PRINT '=======================================================';
GO

-- Step 2: Enable FILESTREAM on database
IF NOT EXISTS (
    SELECT 1 
    FROM sys.filegroups 
    WHERE name = N'HartonomousFileStream' AND type = 'FD'
)
BEGIN
    -- Add FILESTREAM filegroup
    ALTER DATABASE Hartonomous
    ADD FILEGROUP HartonomousFileStream CONTAINS FILESTREAM;
    
    PRINT 'FILESTREAM filegroup created.';
    
    -- Add file to filegroup using SQL Server data directory
    ALTER DATABASE Hartonomous
    ADD FILE (
        NAME = N'HartonomousFileStream_File',
        FILENAME = N'D:\Microsoft SQL Server\MSSQL17.MSSQLSERVER\MSSQL\DATA\HartonomousFileStream'
    ) TO FILEGROUP HartonomousFileStream;
    
    PRINT 'FILESTREAM file added to filegroup at D:\Microsoft SQL Server\MSSQL17.MSSQLSERVER\MSSQL\DATA\HartonomousFileStream';
END
GO

-- Step 3: Create new Atom table with FILESTREAM support
-- Note: This creates a new version; migration strategy needed
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'Atoms' AND schema_id = SCHEMA_ID('dbo'))
BEGIN
    CREATE TABLE dbo.Atoms
    (
        AtomId BIGINT IDENTITY(1,1) NOT NULL,
        RowGuid UNIQUEIDENTIFIER ROWGUIDCOL NOT NULL DEFAULT NEWID(),
        ContentHash VARBINARY(32) NOT NULL,             -- SHA-256
        Modality NVARCHAR(64) NOT NULL,
        Subtype NVARCHAR(64) NULL,
        SourceUri NVARCHAR(2048) NULL,
        SourceType NVARCHAR(128) NULL,
        CanonicalText NVARCHAR(MAX) NULL,
        
        -- FILESTREAM column for large payloads (images, audio, video, model weights)
        Payload VARBINARY(MAX) FILESTREAM NULL,
        
        -- Computed column for payload size
        PayloadSize AS DATALENGTH(Payload) PERSISTED,
        
        Metadata NVARCHAR(MAX) NULL,                    -- JSON
        CreatedAt DATETIME2(7) NOT NULL DEFAULT SYSUTCDATETIME(),
        UpdatedAt DATETIME2(7) NULL,
        IsActive BIT NOT NULL DEFAULT 1,
        ReferenceCount BIGINT NOT NULL DEFAULT 0,
        
        -- Spatial key for hybrid search
        SpatialKey GEOMETRY NULL,
        
        -- ComponentStream for atomic streams
        ComponentStream VARBINARY(MAX) NULL,
        
        CONSTRAINT PK_Atoms PRIMARY KEY CLUSTERED (AtomId),
        CONSTRAINT UX_Atoms_ContentHash UNIQUE NONCLUSTERED (ContentHash),
        CONSTRAINT UX_Atoms_RowGuid UNIQUE (RowGuid)
    );

    -- Indexes
    CREATE NONCLUSTERED INDEX IX_Atoms_Modality
        ON dbo.Atoms(Modality, Subtype)
        INCLUDE (AtomId, ContentHash);

    CREATE NONCLUSTERED INDEX IX_Atoms_CreatedAt
        ON dbo.Atoms(CreatedAt DESC)
        WHERE IsActive = 1;

    -- Spatial index (will be created separately with proper grid settings)
    
    PRINT 'Created dbo.Atoms table with FILESTREAM support';
    PRINT 'Note: Requires FILESTREAM filegroup to be configured first';
END
GO

-- Step 4: Migration procedure to convert PayloadLocator to Payload
IF OBJECT_ID('dbo.sp_MigratePayloadLocatorToFileStream', 'P') IS NOT NULL
    DROP PROCEDURE dbo.sp_MigratePayloadLocatorToFileStream;
GO

CREATE PROCEDURE dbo.sp_MigratePayloadLocatorToFileStream
    @BatchSize INT = 100,
    @Debug BIT = 0
AS
BEGIN
    SET NOCOUNT ON;
    
    DECLARE @MigratedCount INT = 0;
    DECLARE @ErrorCount INT = 0;
    DECLARE @CurrentAtomId BIGINT;
    DECLARE @PayloadPath NVARCHAR(512);
    DECLARE @PayloadBytes VARBINARY(MAX);
    
    -- Note: This is a placeholder for the actual migration logic
    -- Requires CLR function to read files from PayloadLocator path
    
    PRINT 'PayloadLocator to FILESTREAM migration';
    PRINT 'This requires:';
    PRINT '1. CLR function to read file bytes: dbo.clr_ReadFileBytes(@FilePath)';
    PRINT '2. Iterate through graph.AtomGraphNodes with non-null PayloadLocator';
    PRINT '3. Read file bytes via CLR';
    PRINT '4. Insert into dbo.Atoms with Payload column';
    PRINT '5. Update references to use new AtomId';
    
    -- Pseudocode for actual migration:
    /*
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

PRINT 'FILESTREAM setup scripts created';
PRINT 'Manual steps required before execution - see comments above';
GO
