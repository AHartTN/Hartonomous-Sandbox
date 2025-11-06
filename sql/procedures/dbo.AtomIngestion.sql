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
        DECLARE @ContentHash NVARCHAR(64) = CONVERT(NVARCHAR(64), HASHBYTES('SHA2_256', @Content), 2);
        
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
        INSERT INTO dbo.Atoms (
            Content,
            ContentType,
            ContentHash,
            Metadata,
            TenantId
        )
        VALUES (
            @Content,
            @ContentType,
            @ContentHash,
            @Metadata,
            @TenantId
        );
        
        SET @AtomId = SCOPE_IDENTITY();
        
        -- Generate embedding (if requested)
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
                DECLARE @Embedding VARBINARY(MAX);
                SET @Embedding = dbo.fn_ComputeEmbedding(@AtomId, @ModelId, @TenantId);
                
                IF @Embedding IS NOT NULL
                BEGIN
                    INSERT INTO dbo.AtomEmbeddings (
                        AtomId,
                        ModelId,
                        EmbeddingVector,
                        TenantId
                    )
                    VALUES (
                        @AtomId,
                        @ModelId,
                        @Embedding,
                        @TenantId
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
        DECLARE @DuplicateGroups TABLE (
            PrimaryAtomId BIGINT,
            DuplicateAtomId BIGINT,
            Similarity FLOAT
        );
        
        -- Find duplicate pairs using self-join on embeddings
        INSERT INTO @DuplicateGroups
        SELECT TOP (@BatchSize)
            ae1.AtomId AS PrimaryAtomId,
            ae2.AtomId AS DuplicateAtomId,
            1.0 - VECTOR_DISTANCE('cosine', ae1.EmbeddingVector, ae2.EmbeddingVector) AS Similarity
        FROM dbo.AtomEmbeddings ae1
        INNER JOIN dbo.AtomEmbeddings ae2 
            ON ae1.ModelId = ae2.ModelId 
            AND ae1.AtomId < ae2.AtomId -- Avoid duplicate pairs
            AND ae1.TenantId = ae2.TenantId
        WHERE ae1.TenantId = @TenantId
              AND (1.0 - VECTOR_DISTANCE('cosine', ae1.EmbeddingVector, ae2.EmbeddingVector)) >= @SimilarityThreshold
        ORDER BY Similarity DESC;
        
        -- Return duplicate groups
        SELECT 
            dg.PrimaryAtomId,
            dg.DuplicateAtomId,
            dg.Similarity,
            a1.ContentHash AS PrimaryHash,
            a2.ContentHash AS DuplicateHash,
            a1.CreatedUtc AS PrimaryCreated,
            a2.CreatedUtc AS DuplicateCreated
        FROM @DuplicateGroups dg
        INNER JOIN dbo.Atoms a1 ON dg.PrimaryAtomId = a1.AtomId
        INNER JOIN dbo.Atoms a2 ON dg.DuplicateAtomId = a2.AtomId
        ORDER BY dg.Similarity DESC;
        
        RETURN 0;
    END TRY
    BEGIN CATCH
        DECLARE @ErrorMessage NVARCHAR(4000) = ERROR_MESSAGE();
        RAISERROR(@ErrorMessage, 16, 1);
        RETURN -1;
    END CATCH
END;
GO

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
        
        DECLARE @ParentAtoms TABLE (AtomId BIGINT);
        
        -- Parse parent atom IDs
        INSERT INTO @ParentAtoms
        SELECT CAST(value AS BIGINT)
        FROM STRING_SPLIT(@ParentAtomIds, ',');
        
        -- Create graph edges (parent → child)
        INSERT INTO provenance.AtomGraphEdges ($from_id, $to_id, DependencyType, TenantId)
        SELECT 
            pa.AtomId,
            @ChildAtomId,
            @DependencyType,
            @TenantId
        FROM @ParentAtoms pa
        WHERE NOT EXISTS (
            -- Avoid duplicate edges
            SELECT 1 
            FROM provenance.AtomGraphEdges edge
            WHERE edge.$from_id = pa.AtomId 
                  AND edge.$to_id = @ChildAtomId
        );
        
        DECLARE @EdgesCreated INT = @@ROWCOUNT;
        
        COMMIT TRANSACTION;
        
        SELECT 
            @ChildAtomId AS ChildAtomId,
            @EdgesCreated AS EdgesCreated,
            @DependencyType AS DependencyType;
        
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

-- sp_ExtractMetadata: NLP-based metadata extraction
-- Extracts entities, sentiment, language from content

CREATE OR ALTER PROCEDURE dbo.sp_ExtractMetadata
    @AtomId BIGINT,
    @TenantId INT = 0
AS
BEGIN
    SET NOCOUNT ON;
    
    BEGIN TRY
        DECLARE @Content NVARCHAR(MAX);
        DECLARE @ExtractedMetadata NVARCHAR(MAX);
        
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
        DECLARE @ErrorMessage NVARCHAR(4000) = ERROR_MESSAGE();
        RAISERROR(@ErrorMessage, 16, 1);
        RETURN -1;
    END CATCH
END;
GO
