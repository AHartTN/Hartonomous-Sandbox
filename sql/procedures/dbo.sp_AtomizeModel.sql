-- sp_AtomizeModel: Deep atomization for ML model content
-- Parses GGUF/safetensors and creates TensorAtom entries with GEOMETRY representations
-- This is Phase 2 of the atomization pipeline for application/gguf and model/* content types

CREATE OR ALTER PROCEDURE dbo.sp_AtomizeModel
    @AtomId BIGINT,
    @TenantId INT = 0,
    @MaxTensorsPerBatch INT = 100, -- Process tensors in batches to avoid memory issues
    @GeometryPointLimit INT = 10000 -- Max points per GEOMETRY (for very large tensors)
AS
BEGIN
    SET NOCOUNT ON;
    
    BEGIN TRY
        BEGIN TRANSACTION;
        
        -- Retrieve the parent model atom
        DECLARE @ContentType NVARCHAR(100);
        DECLARE @Metadata NVARCHAR(MAX);
        DECLARE @PayloadId UNIQUEIDENTIFIER;
        
        SELECT 
            @ContentType = ContentType,
            @Metadata = Metadata
        FROM dbo.Atoms
        WHERE AtomId = @AtomId AND TenantId = @TenantId;
        
        IF @ContentType IS NULL
        BEGIN
            RAISERROR('Model atom not found', 16, 1);
            RETURN -1;
        END
        
        -- Validate it's actually a model content type
        IF @ContentType NOT IN ('application/gguf', 'application/safetensors', 'application/octet-stream')
        BEGIN
            RAISERROR('Atom is not a model content type', 16, 1);
            RETURN -1;
        END
        
        -- Retrieve the FILESTREAM payload location
        SELECT @PayloadId = PayloadId
        FROM dbo.AtomPayloadStore
        WHERE AtomId = @AtomId;
        
        IF @PayloadId IS NULL
        BEGIN
            RAISERROR('Model payload not found in FILESTREAM storage', 16, 1);
            RETURN -1;
        END
        
        -- Extract model metadata
        DECLARE @ModelFormat NVARCHAR(50) = JSON_VALUE(@Metadata, '$.format');
        DECLARE @ModelArchitecture NVARCHAR(100) = JSON_VALUE(@Metadata, '$.architecture');
        DECLARE @ParameterCount BIGINT = JSON_VALUE(@Metadata, '$.parameterCount');
        
        -- Use the CLR GGUF parser to extract tensor metadata
        -- This returns a table with (TensorName, DataType, Shape, Offset, Size)
        DECLARE @TensorMetadata TABLE (
            TensorName NVARCHAR(500),
            DataType NVARCHAR(50),
            Shape NVARCHAR(500),
            ShapeRank INT,
            ElementCount BIGINT,
            ByteOffset BIGINT,
            ByteSize BIGINT
        );
        
        -- Call CLR function to parse GGUF header and return tensor catalog
        INSERT INTO @TensorMetadata
        EXEC dbo.clr_ParseGGUFTensorCatalog @PayloadId;
        
        DECLARE @TotalTensors INT = (SELECT COUNT(*) FROM @TensorMetadata);
        
        IF @TotalTensors = 0
        BEGIN
            RAISERROR('No tensors found in model file', 16, 1);
            RETURN -1;
        END
        
        -- Process each tensor and convert to GEOMETRY
        DECLARE @TensorsCursor CURSOR;
        DECLARE @TensorName NVARCHAR(500);
        DECLARE @DataType NVARCHAR(50);
        DECLARE @Shape NVARCHAR(500);
        DECLARE @ElementCount BIGINT;
        DECLARE @ByteOffset BIGINT;
        DECLARE @ByteSize BIGINT;
        DECLARE @TensorWeights VARBINARY(MAX);
        DECLARE @WeightsGeometry GEOMETRY;
        DECLARE @TensorsProcessed INT = 0;
        
        SET @TensorsCursor = CURSOR FOR
            SELECT TensorName, DataType, Shape, ElementCount, ByteOffset, ByteSize
            FROM @TensorMetadata
            ORDER BY ByteOffset;
        
        OPEN @TensorsCursor;
        FETCH NEXT FROM @TensorsCursor INTO @TensorName, @DataType, @Shape, @ElementCount, @ByteOffset, @ByteSize;
        
        WHILE @@FETCH_STATUS = 0
        BEGIN
            BEGIN TRY
                -- Read the raw tensor weights from FILESTREAM using offset/size
                SET @TensorWeights = dbo.clr_ReadFilestreamChunk(@PayloadId, @ByteOffset, @ByteSize);
                
                IF @TensorWeights IS NOT NULL
                BEGIN
                    -- Convert the tensor weights to GEOMETRY using the existing CLR function
                    -- Limit to @GeometryPointLimit points to avoid SQL Server GEOMETRY size limits
                    DECLARE @ActualPointLimit INT = CASE 
                        WHEN @ElementCount > @GeometryPointLimit THEN @GeometryPointLimit 
                        ELSE @ElementCount 
                    END;
                    
                    SET @WeightsGeometry = dbo.clr_CreateMultiLineStringFromWeights(
                        @TensorWeights, 
                        @DataType, 
                        @ActualPointLimit
                    );
                    
                    -- Insert into TensorAtoms
                    INSERT INTO dbo.TensorAtoms (
                        ModelAtomId,
                        TensorName,
                        TensorShape,
                        DataType,
                        ElementCount,
                        WeightsGeometry,
                        ByteOffset,
                        ByteSize,
                        TenantId
                    )
                    VALUES (
                        @AtomId,
                        @TensorName,
                        @Shape,
                        @DataType,
                        @ElementCount,
                        @WeightsGeometry,
                        @ByteOffset,
                        @ByteSize,
                        @TenantId
                    );
                    
                    SET @TensorsProcessed = @TensorsProcessed + 1;
                    
                    -- Commit in batches to avoid long-running transactions
                    IF @TensorsProcessed % @MaxTensorsPerBatch = 0
                    BEGIN
                        COMMIT TRANSACTION;
                        BEGIN TRANSACTION;
                    END
                END
            END TRY
            BEGIN CATCH
                -- Log tensor processing failure but continue with remaining tensors
                PRINT 'Failed to process tensor [' + @TensorName + ']: ' + ERROR_MESSAGE();
            END CATCH
            
            FETCH NEXT FROM @TensorsCursor INTO @TensorName, @DataType, @Shape, @ElementCount, @ByteOffset, @ByteSize;
        END
        
        CLOSE @TensorsCursor;
        DEALLOCATE @TensorsCursor;
        
        -- Update the parent atom's metadata with atomization results
        UPDATE dbo.Atoms
        SET Metadata = JSON_MODIFY(
            ISNULL(Metadata, '{}'),
            '$.atomization',
            JSON_QUERY(JSON_OBJECT(
                'type': 'model',
                'tensorsProcessed': @TensorsProcessed,
                'totalTensors': @TotalTensors,
                'format': @ModelFormat,
                'architecture': @ModelArchitecture,
                'atomizedUtc': FORMAT(SYSUTCDATETIME(), 'yyyy-MM-ddTHH:mm:ss.fffZ')
            ))
        )
        WHERE AtomId = @AtomId AND TenantId = @TenantId;
        
        COMMIT TRANSACTION;
        
        SELECT 
            @AtomId AS ParentAtomId,
            @TensorsProcessed AS TensorsProcessed,
            @TotalTensors AS TotalTensors,
            @ModelArchitecture AS Architecture,
            'ModelAtomization' AS Status;
        
        RETURN 0;
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0
            ROLLBACK TRANSACTION;
        
        DECLARE @ErrorMessage NVARCHAR(4000) = ERROR_MESSAGE();
        RAISERROR(@ErrorMessage, 16, 1);
        RETURN -1;
    END CATCH
END;
GO
