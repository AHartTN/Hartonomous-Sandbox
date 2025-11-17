-- =====================================================
-- sp_IngestAtoms
-- Content-Addressable Storage with SHA-256 Deduplication
-- =====================================================
-- Ingests atoms with automatic deduplication based on content hash
-- Triggers Neo4j provenance graph sync for new atoms
-- Returns JSON summary of ingestion results

CREATE OR ALTER PROCEDURE dbo.sp_IngestAtoms
    @atomsJson NVARCHAR(MAX),
    @sourceId BIGINT = NULL,
    @tenantId INT = 0,
    @batchId UNIQUEIDENTIFIER = NULL OUTPUT
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    DECLARE @startTime DATETIME2 = SYSUTCDATETIME();

    -- Generate batch ID for tracking this ingestion
    IF @batchId IS NULL
        SET @batchId = NEWID();

    -- Input validation
    IF @atomsJson IS NULL OR LEN(@atomsJson) = 0
    BEGIN
        RAISERROR('Atoms JSON cannot be null or empty', 16, 1);
        RETURN -1;
    END

    -- Parse JSON into temp table
    DECLARE @atomsTemp TABLE (
        TempId INT IDENTITY(1,1) PRIMARY KEY,
        AtomicValue NVARCHAR(MAX),
        CanonicalText NVARCHAR(MAX),
        Modality NVARCHAR(50),
        Subtype NVARCHAR(50),
        Metadata NVARCHAR(MAX),
        ContentHash VARBINARY(32),
        AtomicValueBinary VARBINARY(MAX)
    );

    BEGIN TRY
        -- Parse JSON array of atoms
        INSERT INTO @atomsTemp (
            AtomicValue,
            CanonicalText,
            Modality,
            Subtype,
            Metadata
        )
        SELECT
            AtomicValue,
            CanonicalText,
            Modality,
            Subtype,
            Metadata
        FROM OPENJSON(@atomsJson)
        WITH (
            AtomicValue NVARCHAR(MAX) '$.atomicValue',
            CanonicalText NVARCHAR(MAX) '$.canonicalText',
            Modality NVARCHAR(50) '$.modality',
            Subtype NVARCHAR(50) '$.subtype',
            Metadata NVARCHAR(MAX) '$.metadata' AS JSON
        );

        IF NOT EXISTS (SELECT 1 FROM @atomsTemp)
        BEGIN
            RAISERROR('No valid atoms found in JSON', 16, 1);
            RETURN -1;
        END

    END TRY
    BEGIN CATCH
        DECLARE @parseError NVARCHAR(4000) = ERROR_MESSAGE();
        RAISERROR('Failed to parse atoms JSON: %s', 16, 1, @parseError);
        RETURN -1;
    END CATCH

    -- Convert AtomicValue to binary and compute SHA-256 content hash
    UPDATE @atomsTemp
    SET AtomicValueBinary = CAST(ISNULL(AtomicValue, '') AS VARBINARY(MAX)),
        ContentHash = HASHBYTES('SHA2_256', CAST(ISNULL(AtomicValue, '') AS VARBINARY(MAX)));

    -- Statistics
    DECLARE @totalAtoms INT = (SELECT COUNT(*) FROM @atomsTemp);
    DECLARE @newAtoms INT = 0;
    DECLARE @duplicateAtoms INT = 0;

    -- Track new atom IDs for Neo4j sync
    DECLARE @newAtomIds TABLE (
        AtomId BIGINT PRIMARY KEY,
        ContentHash VARBINARY(32)
    );

    BEGIN TRANSACTION;

    BEGIN TRY
        -- =====================================================
        -- Content-Addressable Deduplication
        -- =====================================================
        -- Use MERGE for atomic upsert with SHA-256 content hashing
        -- Same content = same hash = deduplicated
        
        MERGE dbo.Atoms AS target
        USING @atomsTemp AS source
        ON target.ContentHash = source.ContentHash 
            AND target.TenantId = @tenantId
        WHEN NOT MATCHED THEN
            INSERT (
                ContentHash,
                AtomicValue,
                CanonicalText,
                Modality,
                Subtype,
                Metadata,
                TenantId,
                SourceId,
                CreatedAt,
                BatchId
            )
            VALUES (
                source.ContentHash,
                source.AtomicValueBinary,
                source.CanonicalText,
                source.Modality,
                source.Subtype,
                source.Metadata,
                @tenantId,
                @sourceId,
                SYSUTCDATETIME(),
                @batchId
            )
        OUTPUT 
            INSERTED.AtomId,
            INSERTED.ContentHash
        INTO @newAtomIds;

        SET @newAtoms = (SELECT COUNT(*) FROM @newAtomIds);
        SET @duplicateAtoms = @totalAtoms - @newAtoms;

        -- =====================================================
        -- Trigger Neo4j Provenance Sync
        -- =====================================================
        -- Background workers will process this queue
        -- Creates nodes in Neo4j for provenance tracking
        
        IF @newAtoms > 0 AND OBJECT_ID('dbo.Neo4jSyncQueue', 'U') IS NOT NULL
        BEGIN
            INSERT INTO dbo.Neo4jSyncQueue (
                EntityType,
                EntityId,
                Operation,
                TenantId,
                QueuedAt,
                BatchId,
                Status
            )
            SELECT
                'Atom' AS EntityType,
                AtomId AS EntityId,
                'INSERT' AS Operation,
                @tenantId AS TenantId,
                SYSUTCDATETIME() AS QueuedAt,
                @batchId AS BatchId,
                'Pending' AS Status
            FROM @newAtomIds;
        END

        -- =====================================================
        -- Log Ingestion Metrics
        -- =====================================================
        
        IF OBJECT_ID('dbo.IngestionMetrics', 'U') IS NOT NULL
        BEGIN
            DECLARE @durationMs INT = DATEDIFF(MILLISECOND, @startTime, SYSUTCDATETIME());
            
            INSERT INTO dbo.IngestionMetrics (
                BatchId,
                SourceId,
                TenantId,
                TotalAtoms,
                NewAtoms,
                DuplicateAtoms,
                DurationMs,
                IngestedAt
            )
            VALUES (
                @batchId,
                @sourceId,
                @tenantId,
                @totalAtoms,
                @newAtoms,
                @duplicateAtoms,
                @durationMs,
                SYSUTCDATETIME()
            );
        END

        COMMIT TRANSACTION;

    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0
            ROLLBACK TRANSACTION;
        
        DECLARE @errorMessage NVARCHAR(4000) = ERROR_MESSAGE();
        DECLARE @errorSeverity INT = ERROR_SEVERITY();
        DECLARE @errorState INT = ERROR_STATE();
        
        RAISERROR('Ingestion failed: %s', @errorSeverity, @errorState, @errorMessage);
        RETURN -1;
    END CATCH

    -- =====================================================
    -- Return Results as JSON
    -- =====================================================
    
    DECLARE @deduplicationRate FLOAT = 
        CASE WHEN @totalAtoms > 0 
        THEN CAST(@duplicateAtoms AS FLOAT) / @totalAtoms 
        ELSE 0 
        END;

    SELECT
        @batchId AS BatchId,
        @totalAtoms AS TotalAtoms,
        @newAtoms AS NewAtoms,
        @duplicateAtoms AS Deduplicated,
        @deduplicationRate AS DeduplicationRate,
        DATEDIFF(MILLISECOND, @startTime, SYSUTCDATETIME()) AS DurationMs
    FOR JSON PATH, WITHOUT_ARRAY_WRAPPER;

    RETURN 0;
END
GO

GRANT EXECUTE ON dbo.sp_IngestAtoms TO PUBLIC;
GO
