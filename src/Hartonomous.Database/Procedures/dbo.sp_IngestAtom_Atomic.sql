-- =============================================
-- Atomic Ingestion Coordinator
-- =============================================
-- Unified ingestion pipeline that routes content to
-- atomic decomposition procedures based on ContentType.
--
-- Replaces sp_IngestAtom with atomic-first approach.
-- All content is decomposed into AtomRelations with:
-- - Weights (relationship strength)
-- - Importance (saliency/significance)
-- - Confidence (certainty of value)
-- - Spatial coordinates (trilateration)
-- =============================================

CREATE PROCEDURE dbo.sp_IngestAtom_Atomic
    @ContentType NVARCHAR(100),
    @Content VARBINARY(MAX),
    @Metadata NVARCHAR(MAX) = NULL,
    @TenantId INT = 0,
    @EnableDeduplication BIT = 1,
    @EnableAtomization BIT = 1,
    @AtomId BIGINT OUTPUT
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;
    
    BEGIN TRY
        -- Compute ContentHash for deduplication
        DECLARE @ContentHash BINARY(32) = HASHBYTES('SHA2_256', @Content);
        
        -- Extract modality and subtype from ContentType
        DECLARE @Modality NVARCHAR(50) = PARSENAME(@ContentType, 2);
        DECLARE @Subtype NVARCHAR(50) = PARSENAME(@ContentType, 1);
        
        IF @Modality IS NULL
        BEGIN
            -- Single-part ContentType (e.g., 'text')
            SET @Modality = @ContentType;
            SET @Subtype = 'raw';
        END
        
        BEGIN TRANSACTION;
        
        -- Check for existing atom (deduplication)
        IF @EnableDeduplication = 1
        BEGIN
            SELECT @AtomId = AtomId
            FROM dbo.Atoms
            WHERE ContentHash = @ContentHash 
              AND TenantId = @TenantId;
            
            IF @AtomId IS NOT NULL
            BEGIN
                -- Already exists, increment reference count
                UPDATE dbo.Atoms
                SET ReferenceCount = ReferenceCount + 1,
                    UpdatedAt = SYSDATETIME()
                WHERE AtomId = @AtomId;
                
                COMMIT TRANSACTION;
                
                SELECT 
                    @AtomId AS AtomId,
                    1 AS Deduplicated,
                    'Content already exists' AS Message;
                
                RETURN 0;  -- Success (deduplicated)
            END
        END
        
        -- Create parent atom
        INSERT INTO dbo.Atoms (
            ContentHash,
            Modality,
            Subtype,
            TenantId,
            ReferenceCount
        )
        VALUES (
            @ContentHash,
            @Modality,
            @Subtype,
            @TenantId,
            1  -- Initial reference
        );
        
        SET @AtomId = SCOPE_IDENTITY();
        
        -- Store large content in LOB table
        IF LEN(@Content) > 8000 OR LEN(@Metadata) > 8000
        BEGIN
            INSERT INTO dbo.AtomsLOB (AtomId, ComponentStream, Metadata)
            VALUES (@AtomId, @Content, CAST(@Metadata AS NVARCHAR(MAX)));
        END
        ELSE
        BEGIN
            -- Store small content inline
            UPDATE dbo.Atoms
            SET AtomicValue = @Content,
                CanonicalText = TRY_CAST(@Content AS NVARCHAR(MAX))
            WHERE AtomId = @AtomId;
        END
        
        COMMIT TRANSACTION;
        
        -- Atomize content based on modality
        IF @EnableAtomization = 1
        BEGIN
            IF @Modality = 'image'
            BEGIN
                EXEC dbo.sp_AtomizeImage_Atomic 
                    @ParentAtomId = @AtomId,
                    @TenantId = @TenantId;
            END
            ELSE IF @Modality = 'audio'
            BEGIN
                EXEC dbo.sp_AtomizeAudio_Atomic 
                    @ParentAtomId = @AtomId,
                    @TenantId = @TenantId;
            END
            ELSE IF @Modality = 'embedding'
            BEGIN
                -- Use existing vector decomposition
                EXEC dbo.sp_InsertAtomicVector 
                    @SourceAtomId = @AtomId,
                    @Vector = @Content,  -- Assuming @Content is VECTOR type
                    @RelationType = 'embedding_dim',
                    @TenantId = @TenantId;
            END
            ELSE IF @Modality = 'model'
            BEGIN
                -- Extract tensor weights
                EXEC dbo.sp_AtomizeModel_Atomic 
                    @ParentAtomId = @AtomId,
                    @TenantId = @TenantId,
                    @QuantizationBits = 8,
                    @ComputeImportance = 1;
            END
            ELSE IF @Modality = 'text'
            BEGIN
                -- Tokenize text into words/subwords
                EXEC dbo.sp_AtomizeText_Atomic 
                    @ParentAtomId = @AtomId,
                    @TenantId = @TenantId,
                    @TokenizerType = 'bpe',
                    @ComputeImportance = 1;
            END
        END
        
        -- Return ingestion summary
        SELECT 
            @AtomId AS AtomId,
            0 AS Deduplicated,
            @Modality AS Modality,
            @Subtype AS Subtype,
            CASE WHEN @EnableAtomization = 1 THEN 'Atomized' ELSE 'Raw' END AS Status;
        
        RETURN 0;
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0
            ROLLBACK TRANSACTION;
        
        DECLARE @ErrorMessage NVARCHAR(4000) = ERROR_MESSAGE();
        DECLARE @ErrorSeverity INT = ERROR_SEVERITY();
        DECLARE @ErrorState INT = ERROR_STATE();
        
        RAISERROR(@ErrorMessage, @ErrorSeverity, @ErrorState);
        RETURN -1;
    END CATCH
END
GO


-- =============================================
-- Example Usage
-- =============================================
/*

-- Ingest image (will decompose into RGB atoms)
DECLARE @ImageAtomId BIGINT;
EXEC dbo.sp_IngestAtom_Atomic 
    @ContentType = 'image.jpeg',
    @Content = 0xFFD8FFE0...,  -- JPEG binary
    @Metadata = '{"width": 1920, "height": 1080}',
    @TenantId = 1,
    @AtomId = @ImageAtomId OUTPUT;

SELECT @ImageAtomId;

-- Ingest audio (will decompose into amplitude atoms)
DECLARE @AudioAtomId BIGINT;
EXEC dbo.sp_IngestAtom_Atomic 
    @ContentType = 'audio.wav',
    @Content = 0x52494646...,  -- WAV binary
    @Metadata = '{"durationMs": 5000, "sampleRate": 44100, "channels": 2}',
    @TenantId = 1,
    @AtomId = @AudioAtomId OUTPUT;

SELECT @AudioAtomId;

-- Ingest embedding (will decompose into dimension atoms)
DECLARE @EmbeddingAtomId BIGINT;
EXEC dbo.sp_IngestAtom_Atomic 
    @ContentType = 'embedding.float32',
    @Content = VECTOR(...),  -- 1998-dim vector
    @TenantId = 1,
    @AtomId = @EmbeddingAtomId OUTPUT;

SELECT @EmbeddingAtomId;

-- Query unified substrate: "Show me images that share RGB values with this audio"
SELECT DISTINCT
    img.AtomId AS SharedImageId,
    COUNT(*) AS SharedComponents
FROM dbo.AtomRelations audio_rel
INNER JOIN dbo.Atoms shared_atom 
    ON shared_atom.AtomId = audio_rel.TargetAtomId
INNER JOIN dbo.AtomRelations img_rel 
    ON img_rel.TargetAtomId = shared_atom.AtomId
INNER JOIN dbo.Atoms img 
    ON img.AtomId = img_rel.SourceAtomId
WHERE audio_rel.SourceAtomId = @AudioAtomId
  AND img.Modality = 'image'
GROUP BY img.AtomId
ORDER BY SharedComponents DESC;

*/
