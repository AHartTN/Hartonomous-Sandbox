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
        
        -- Compute content hash (SHA-256 for content-addressable storage)
        DECLARE @ContentHash BINARY(32) = HASHBYTES('SHA2_256', @Content);
        
        -- Check for duplicate globally (true content-addressable deduplication)
        SELECT @AtomId = AtomId
        FROM dbo.Atoms
        WHERE ContentHash = @ContentHash;
        
        IF @AtomId IS NOT NULL
        BEGIN
            -- Atom exists globally, create tenant reference in junction table
            IF NOT EXISTS (SELECT 1 FROM dbo.TenantAtoms WHERE TenantId = @TenantId AND AtomId = @AtomId)
            BEGIN
                INSERT INTO dbo.TenantAtoms (TenantId, AtomId)
                VALUES (@TenantId, @AtomId);
            END
            
            -- Increment reference count
            UPDATE dbo.Atoms SET ReferenceCount = ReferenceCount + 1 WHERE AtomId = @AtomId;
            
            SELECT 
                @AtomId AS AtomId,
                'Deduplicated' AS Status,
                @ContentHash AS ContentHash;
            
            COMMIT TRANSACTION;
            RETURN 0;
        END
        
        -- Insert new atom (globally unique by ContentHash)
        INSERT INTO dbo.Atoms (
            ContentHash,
            Modality,
            Subtype,
            PayloadLocator,
            Metadata,
            IsActive,
            ReferenceCount
        )
        VALUES (
            @ContentHash,
            LEFT(@ContentType, CHARINDEX('/', @ContentType + '/') - 1),
            SUBSTRING(@ContentType, CHARINDEX('/', @ContentType) + 1, 128),
            'atom://' + CONVERT(NVARCHAR(64), @ContentHash, 2),
            @Metadata,
            1,
            1  -- First reference
        );
        
        SET @AtomId = SCOPE_IDENTITY();
        
        -- Create tenant reference
        INSERT INTO dbo.TenantAtoms (TenantId, AtomId)
        VALUES (@TenantId, @AtomId);
        
        -- Phase 2: Deep Atomization (break content into sub-atoms based on type)
        -- This is the "atomize as far down as we can" principle
        BEGIN TRY
            IF @ContentType LIKE 'audio/%'
            BEGIN
                -- Atomize audio into AudioFrames
                EXEC dbo.sp_AtomizeAudio 
                    @AtomId = @AtomId,
                    @TenantId = @TenantId;
            END
            ELSE IF @ContentType LIKE 'image/%'
            BEGIN
                -- Atomize image into ImagePatches
                EXEC dbo.sp_AtomizeImage 
                    @AtomId = @AtomId,
                    @TenantId = @TenantId;
            END
            ELSE IF @ContentType IN ('application/gguf', 'application/safetensors')
            BEGIN
                -- Atomize model into TensorAtoms with GEOMETRY representations
                EXEC dbo.sp_AtomizeModel 
                    @AtomId = @AtomId,
                    @TenantId = @TenantId;
            END
            ELSE IF @ContentType LIKE 'video/%'
            BEGIN
                -- Future: Atomize video into VideoFrames
                -- EXEC dbo.sp_AtomizeVideo @AtomId = @AtomId, @TenantId = @TenantId;
                PRINT 'Video atomization not yet implemented';
            END
            ELSE IF @ContentType LIKE 'text/%'
            BEGIN
                -- Future: Atomize text into semantic chunks/sentences
                -- EXEC dbo.sp_AtomizeText @AtomId = @AtomId, @TenantId = @TenantId;
                PRINT 'Text atomization not yet implemented';
            END
            ELSE IF @ContentType IN ('text/x-csharp', 'application/x-csharp', 'text/x-python', 'application/x-python', 
                                      'text/javascript', 'application/javascript', 'text/x-java', 'application/x-java')
            BEGIN
                -- Atomize code into AST-as-GEOMETRY representation
                DECLARE @detectedLanguage NVARCHAR(50) = CASE
                    WHEN @ContentType LIKE '%csharp%' THEN 'csharp'
                    WHEN @ContentType LIKE '%python%' THEN 'python'
                    WHEN @ContentType LIKE '%javascript%' THEN 'javascript'
                    WHEN @ContentType LIKE '%java' THEN 'java'
                    ELSE 'csharp'  -- Default to C# for now
                END;
                
                EXEC dbo.sp_AtomizeCode 
                    @AtomId = @AtomId,
                    @TenantId = @TenantId,
                    @Language = @detectedLanguage;
            END
        END TRY
        BEGIN CATCH
            -- Log atomization errors but don't fail the entire ingestion
            PRINT 'Atomization failed for AtomId ' + CAST(@AtomId AS NVARCHAR(20)) + ': ' + ERROR_MESSAGE();
        END CATCH
        
        -- Phase 3: Generate embedding (if requested)
        IF @GenerateEmbedding = 1
        BEGIN
            -- Use default model if not specified
            IF @ModelId IS NULL
            BEGIN
                SELECT TOP 1 @ModelId = ModelId
                FROM dbo.Models
                ORDER BY LastUsed DESC;
            END
            
            IF @ModelId IS NOT NULL
            BEGIN
                DECLARE @Embedding VECTOR(1998);
                DECLARE @EmbeddingBytes VARBINARY(MAX);
                SET @EmbeddingBytes = dbo.fn_ComputeEmbedding(@AtomId, @ModelId, @TenantId);
                
                IF @EmbeddingBytes IS NOT NULL
                BEGIN
                    -- Convert VARBINARY(MAX) -> NVARCHAR(MAX) -> VECTOR(1998)
                    SET @Embedding = CAST(CONVERT(NVARCHAR(MAX), @EmbeddingBytes) AS VECTOR(1998));
                END
                
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