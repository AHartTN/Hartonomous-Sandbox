-- sp_IngestAtom: Content-addressable atom ingestion pipeline
-- SHA-256 deduplication + metadata extraction + embedding generation

CREATE OR ALTER PROCEDURE dbo.sp_IngestAtom
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

        -- Check for duplicate (content-addressable deduplication)
        SELECT @AtomId = AtomId
        FROM dbo.Atoms
        WHERE ContentHash = @ContentHash AND TenantId = @TenantId;
        
        IF @AtomId IS NOT NULL
        BEGIN
            -- Atom already exists, return existing ID
            SELECT 
                @AtomId AS AtomId,
                'Duplicate' AS Status,
                @ContentHash AS ContentHash;
            
            COMMIT TRANSACTION;
            RETURN 0;
        END
        
        -- Insert new atom
        
        
        SET @AtomId = SCOPE_IDENTITY();
        
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
                END
            ELSE IF @ContentType LIKE 'text/%'
            BEGIN
                -- Future: Atomize text into semantic chunks/sentences
                -- EXEC dbo.sp_AtomizeText @AtomId = @AtomId, @TenantId = @TenantId;
                END
            ELSE IF @ContentType IN ('text/x-csharp', 'application/x-csharp', 'text/x-python', 'application/x-python', 
                                      'text/javascript', 'application/javascript', 'text/x-java', 'application/x-java')
            BEGIN
                -- Atomize code into AST-as-GEOMETRY representation

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
                WHERE IsActive = 1 AND TenantId = @TenantId
                ORDER BY ModelId DESC;
            END
            
            IF @ModelId IS NOT NULL
            BEGIN

                SET @Embedding = dbo.fn_ComputeEmbedding(@AtomId, @ModelId, @TenantId);
                
                IF @Embedding IS NOT NULL
                BEGIN
                    
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

        RAISERROR(@ErrorMessage, 16, 1);
        RETURN -1;
    END CATCH
END;

-- sp_DetectDuplicates: Semantic similarity-based deduplication
-- Finds atoms with >95% similarity threshold

CREATE OR ALTER PROCEDURE dbo.sp_DetectDuplicates
    @SimilarityThreshold FLOAT = 0.95,
    @BatchSize INT = 1000,
    @TenantId INT = 0
AS
BEGIN
    SET NOCOUNT ON;
    
    BEGIN TRY
        

-- sp_LinkProvenance: Create graph edges for atom lineage
-- Establishes parent-child relationships in provenance graph

CREATE OR ALTER PROCEDURE dbo.sp_LinkProvenance
    @ParentAtomIds NVARCHAR(MAX), -- Comma-separated list
    @ChildAtomId BIGINT,
    @DependencyType NVARCHAR(50) = 'DerivedFrom',
    @TenantId INT = 0
AS
BEGIN
    SET NOCOUNT ON;
    
    BEGIN TRY
        BEGIN TRANSACTION;
        
        

-- sp_ExtractMetadata: NLP-based metadata extraction
-- Extracts entities, sentiment, language from content

CREATE OR ALTER PROCEDURE dbo.sp_ExtractMetadata
    @AtomId BIGINT,
    @TenantId INT = 0
AS
BEGIN
    SET NOCOUNT ON;
    
    BEGIN TRY


        -- Load content
        SELECT @Content = CAST(Content AS NVARCHAR(MAX))
        FROM dbo.Atoms
        WHERE AtomId = @AtomId AND TenantId = @TenantId;
        
        IF @Content IS NULL
        BEGIN
            RAISERROR('Atom not found', 16, 1);
            RETURN -1;
        END
        
        -- Extract metadata (placeholder - would use CLR NLP function in production)
        SET @ExtractedMetadata = JSON_OBJECT(
            'wordCount': LEN(@Content) - LEN(REPLACE(@Content, ' ', '')) + 1,
            'language': 'en', -- Would use language detection
            'sentiment': 0.5,  -- Would use sentiment analysis
            'entities': JSON_ARRAY(), -- Would use NER
            'extractedUtc': FORMAT(SYSUTCDATETIME(), 'yyyy-MM-ddTHH:mm:ss.fffZ')
        );
        
        -- Update atom metadata
        UPDATE dbo.Atoms
        SET Metadata = JSON_MODIFY(
            ISNULL(Metadata, '{}'),
            '$.extracted',
            JSON_QUERY(@ExtractedMetadata)
        )
        WHERE AtomId = @AtomId AND TenantId = @TenantId;
        
        SELECT 
            @AtomId AS AtomId,
            @ExtractedMetadata AS ExtractedMetadata;
        
        RETURN 0;
    END TRY
    BEGIN CATCH

        RAISERROR(@ErrorMessage, 16, 1);
        RETURN -1;
    END CATCH
END;
