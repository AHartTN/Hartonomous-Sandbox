CREATE PROCEDURE dbo.sp_IngestAtom
    @Content VARBINARY(MAX),
    @ContentType NVARCHAR(100),
    @Metadata NVARCHAR(MAX) = NULL,
    @TenantId INT = 0,
    @GenerateEmbedding BIT = 1,
    @ModelId INT = NULL,
    @AtomId BIGINT OUTPUT
AS
BEGIN
    SET NOCOUNT ON;
    
    BEGIN TRY
        BEGIN TRANSACTION;
        
        DECLARE @ContentHash BINARY(32) = HASHBYTES('SHA2_256', @Content);
        
        SELECT @AtomId = AtomId
        FROM dbo.Atoms
        WHERE ContentHash = @ContentHash AND TenantId = @TenantId;
        
        IF @AtomId IS NOT NULL
        BEGIN
            SELECT 
                @AtomId AS AtomId,
                'Duplicate' AS Status,
                @ContentHash AS ContentHash;
            
            COMMIT TRANSACTION;
            RETURN 0;
        END
        
        INSERT INTO dbo.Atoms (
            ContentHash,
            Modality,
            Subtype,
            PayloadLocator,
            Metadata,
            TenantId,
            IsActive
        )
        VALUES (
            @ContentHash,
            LEFT(@ContentType, CHARINDEX('/', @ContentType + '/') - 1),
            SUBSTRING(@ContentType, CHARINDEX('/', @ContentType) + 1, 128),
            'atom://' + CONVERT(NVARCHAR(64), @ContentHash, 2),
            @Metadata,
            @TenantId,
            1
        );
        
        SET @AtomId = SCOPE_IDENTITY();
        
        BEGIN TRY
            IF @ContentType LIKE 'audio/%'
            BEGIN
                EXEC dbo.sp_AtomizeAudio 
                    @AtomId = @AtomId,
                    @TenantId = @TenantId;
            END
            ELSE IF @ContentType LIKE 'image/%'
            BEGIN
                EXEC dbo.sp_AtomizeImage 
                    @AtomId = @AtomId,
                    @TenantId = @TenantId;
            END
            ELSE IF @ContentType IN ('application/gguf', 'application/safetensors')
            BEGIN
                EXEC dbo.sp_AtomizeModel 
                    @AtomId = @AtomId,
                    @TenantId = @TenantId;
            END
            ELSE IF @ContentType LIKE 'video/%'
            BEGIN
                PRINT 'Video atomization not yet implemented';
            END
            ELSE IF @ContentType LIKE 'text/%'
            BEGIN
                PRINT 'Text atomization not yet implemented';
            END
            ELSE IF @ContentType IN ('text/x-csharp', 'application/x-csharp', 'text/x-python', 'application/x-python', 
                                      'text/javascript', 'application/javascript', 'text/x-java', 'application/x-java')
            BEGIN
                DECLARE @detectedLanguage NVARCHAR(50) = CASE
                    WHEN @ContentType LIKE '%csharp%' THEN 'csharp'
                    WHEN @ContentType LIKE '%python%' THEN 'python'
                    WHEN @ContentType LIKE '%javascript%' THEN 'javascript'
                    WHEN @ContentType LIKE '%java' THEN 'java'
                    ELSE 'csharp'
                END;
                
                EXEC dbo.sp_AtomizeCode 
                    @AtomId = @AtomId,
                    @TenantId = @TenantId,
                    @Language = @detectedLanguage;
            END
        END TRY
        BEGIN CATCH
            PRINT 'Atomization failed for AtomId ' + CAST(@AtomId AS NVARCHAR(20)) + ': ' + ERROR_MESSAGE();
        END CATCH
        
        IF @GenerateEmbedding = 1
        BEGIN
            IF @ModelId IS NULL
            BEGIN
                SELECT TOP 1 @ModelId = ModelId
                FROM dbo.Models
                ORDER BY LastUsed DESC;
            END
            
            IF @ModelId IS NOT NULL
            BEGIN
                DECLARE @Embedding VARBINARY(MAX);
                -- SET @Embedding = dbo.fn_ComputeEmbedding(@AtomId, @ModelId, @TenantId);
                
                IF @Embedding IS NOT NULL
                BEGIN
                    INSERT INTO dbo.AtomEmbeddings (
                        AtomId,
                        ModelId,
                        EmbeddingVector
                    )
                    VALUES (
                        @AtomId,
                        @ModelId,
                        @Embedding
                    );
                END
            END
        END
        
        COMMIT TRANSACTION;
        
        SELECT 
            @AtomId AS AtomId,
            'Created' AS Status,
            @ContentHash AS ContentHash,
            CASE WHEN @GenerateEmbedding = 1 AND @ModelId IS NOT NULL THEN 1 ELSE 0 END AS EmbeddingGenerated;
        
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