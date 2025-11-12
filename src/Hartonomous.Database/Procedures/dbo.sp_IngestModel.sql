CREATE PROCEDURE dbo.sp_IngestModel
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
        INSERT INTO dbo.Models (
            ModelName,
            ModelType,
            Architecture,
            Config,
            ParameterCount,
            TenantId,
            IsActive
        )
        VALUES (
            @ModelName,
            @ModelType,
            @Architecture,
            @ConfigJson,
            @ParameterCount,
            @TenantId,
            1
        );
        
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
            DECLARE @FileStreamToken VARBINARY(MAX);
            
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
        INSERT INTO provenance.ModelVersionHistory (
            ModelId,
            VersionTag,
            ChangeDescription,
            TenantId
        )
        VALUES (
            @ModelId,
            '1.0',
            'Initial model ingestion via sp_IngestModel',
            @TenantId
        );
        
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
        
        DECLARE @ErrorMessage NVARCHAR(4000) = ERROR_MESSAGE();
        DECLARE @ErrorSeverity INT = ERROR_SEVERITY();
        DECLARE @ErrorState INT = ERROR_STATE();
        
        PRINT 'sp_IngestModel ERROR: ' + @ErrorMessage;
        RAISERROR(@ErrorMessage, @ErrorSeverity, @ErrorState);
        RETURN -1;
    END CATCH
END;