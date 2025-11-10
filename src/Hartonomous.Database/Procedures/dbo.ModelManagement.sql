-- sp_IngestModel: Model registration and deployment pipeline
-- Handles model metadata, serialization, and version management

CREATE OR ALTER PROCEDURE dbo.sp_IngestModel
    @ModelName NVARCHAR(200),
    @ModelType NVARCHAR(50),
    @Architecture NVARCHAR(100),
    @ConfigJson NVARCHAR(MAX),
    @ModelBytes VARBINARY(MAX) = NULL,
    @FileStreamPath NVARCHAR(500) = NULL,
    @ParameterCount BIGINT = NULL,
    @TenantId INT = 0,
    @SetAsCurrent BIT = 0,
    @ModelId INT OUTPUT
AS
BEGIN
    SET NOCOUNT ON;
    
    BEGIN TRY
        BEGIN TRANSACTION;
        
        -- Validate inputs
        IF @ModelName IS NULL OR @ModelType IS NULL
        BEGIN
            RAISERROR('ModelName and ModelType are required', 16, 1);
            RETURN -1;
        END
        
        IF @ModelBytes IS NULL AND @FileStreamPath IS NULL
        BEGIN
            RAISERROR('Either ModelBytes or FileStreamPath must be provided', 16, 1);
            RETURN -1;
        END
        
        -- Create model record
        
        
        SET @ModelId = SCOPE_IDENTITY();
        
        -- Store model data
        IF @ModelBytes IS NOT NULL
        BEGIN
            -- Small model: Store directly in varbinary column
            UPDATE dbo.Models
            SET SerializedModel = @ModelBytes
            WHERE ModelId = @ModelId;
        END
        ELSE IF @FileStreamPath IS NOT NULL
        BEGIN
            -- Large model: Store in FILESTREAM

            -- Get FILESTREAM path for model
            SELECT @FileStreamToken = SerializedModel.PathName()
            FROM dbo.Models
            WHERE ModelId = @ModelId;
            
            -- Note: Actual file copy would be done via Win32 API in CLR function
            -- For now, just log the path
            PRINT 'Model will be stored at FILESTREAM path: ' + @FileStreamPath;
        END
        
        -- Set as current version if requested
        IF @SetAsCurrent = 1
        BEGIN
            -- Deactivate previous versions
            UPDATE dbo.Models
            SET IsActive = 0
            WHERE ModelName = @ModelName
                  AND ModelId != @ModelId
                  AND TenantId = @TenantId;
        END
        
        -- Log model ingestion
        
        
        COMMIT TRANSACTION;
        
        SELECT 
            @ModelId AS ModelId,
            @ModelName AS ModelName,
            DATALENGTH(@ModelBytes) AS ModelSizeBytes,
            @SetAsCurrent AS IsCurrentVersion;
        
        PRINT 'Model ingested successfully: ' + 
              @ModelName + ' (ModelId: ' + CAST(@ModelId AS VARCHAR(10)) + ')';
        
        RETURN 0;
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0
            ROLLBACK TRANSACTION;



        PRINT 'sp_IngestModel ERROR: ' + @ErrorMessage;
        RAISERROR(@ErrorMessage, @ErrorSeverity, @ErrorState);
        RETURN -1;
    END CATCH
END;

-- sp_OptimizeEmbeddings: Recompute outdated embeddings
-- Batch processing for efficiency

CREATE OR ALTER PROCEDURE dbo.sp_OptimizeEmbeddings
    @ModelId INT,
    @BatchSize INT = 100,
    @MaxAgeHours INT = 24,
    @TenantId INT = 0
AS
BEGIN
    SET NOCOUNT ON;


    BEGIN TRY
        -- Find atoms with outdated or missing embeddings

            AtomId BIGINT PRIMARY KEY,
            Content NVARCHAR(MAX)
        );
        
        
        
        -- Process each atom



        DECLARE atom_cursor CURSOR LOCAL FAST_FORWARD FOR
        SELECT AtomId, Content FROM @AtomsToProcess;
        
        OPEN atom_cursor;
        FETCH NEXT FROM atom_cursor INTO @CurrentAtomId, @CurrentContent;
        
        WHILE @@FETCH_STATUS = 0
        BEGIN
            -- Compute new embedding
            SET @NewEmbedding = dbo.fn_ComputeEmbedding(@CurrentAtomId, @ModelId, @TenantId);
            
            IF @NewEmbedding IS NOT NULL
            BEGIN
                -- Upsert embedding
                MERGE dbo.AtomEmbeddings AS target
                USING (SELECT @CurrentAtomId AS AtomId, @ModelId AS ModelId, @TenantId AS TenantId) AS source
                ON target.AtomId = source.AtomId AND target.ModelId = source.ModelId AND target.TenantId = source.TenantId
                WHEN MATCHED THEN
                    UPDATE SET 
                        EmbeddingVector = @NewEmbedding,
                        LastComputedUtc = SYSUTCDATETIME(),
                        LastAccessedUtc = SYSUTCDATETIME()
                WHEN NOT MATCHED THEN
                    INSERT (AtomId, ModelId, EmbeddingVector, TenantId)
                    VALUES (@CurrentAtomId, @ModelId, @NewEmbedding, @TenantId);
                
                SET @ProcessedCount += 1;
            END
            
            FETCH NEXT FROM atom_cursor INTO @CurrentAtomId, @CurrentContent;
        END
        
        CLOSE atom_cursor;
        DEALLOCATE atom_cursor;
        
        SELECT 
            @ProcessedCount AS EmbeddingsProcessed,
            @BatchSize AS BatchSize,
            DATEDIFF(MILLISECOND, @StartTime, SYSUTCDATETIME()) AS DurationMs;
        
        PRINT 'Embedding optimization complete: ' + CAST(@ProcessedCount AS VARCHAR(10)) + ' embeddings processed';
        
        RETURN 0;
    END TRY
    BEGIN CATCH



        PRINT 'sp_OptimizeEmbeddings ERROR: ' + @ErrorMessage;
        RAISERROR(@ErrorMessage, @ErrorSeverity, @ErrorState);
        RETURN -1;
    END CATCH
END;

-- sp_ScoreWithModel: Real-time inference using PREDICT
-- Uses SQL Server ML Services or ONNX runtime

CREATE OR ALTER PROCEDURE dbo.sp_ScoreWithModel
    @ModelId INT,
    @InputAtomIds NVARCHAR(MAX), -- Comma-separated
    @OutputFormat NVARCHAR(50) = 'JSON',
    @TenantId INT = 0
AS
BEGIN
    SET NOCOUNT ON;
    
    BEGIN TRY
        -- Parse input atoms
        
