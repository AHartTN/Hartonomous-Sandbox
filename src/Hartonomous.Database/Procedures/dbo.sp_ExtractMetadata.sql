CREATE OR ALTER PROCEDURE dbo.sp_ExtractMetadata
    @AtomId BIGINT,
    @TenantId INT = 0
AS
BEGIN
    SET NOCOUNT ON;
    
    BEGIN TRY
        DECLARE @Modality NVARCHAR(64);
        DECLARE @CanonicalText NVARCHAR(MAX);
        DECLARE @ExtractedMetadata NVARCHAR(MAX);
        
        -- Load atom metadata
        SELECT 
            @Modality = Modality,
            @CanonicalText = CanonicalText
        FROM dbo.Atom
        WHERE AtomId = @AtomId AND TenantId = @TenantId;
        
        IF @Modality IS NULL
        BEGIN
            RAISERROR('Atom not found', 16, 1);
            RETURN -1;
        END
        
        -- Extract metadata based on modality
        -- For text modality with CanonicalText, perform basic analysis
        IF @Modality = 'text' AND @CanonicalText IS NOT NULL
        BEGIN
            DECLARE @WordCount INT = LEN(@CanonicalText) - LEN(REPLACE(@CanonicalText, ' ', '')) + 1;
            DECLARE @CharCount INT = LEN(@CanonicalText);
            
            SET @ExtractedMetadata = (
                SELECT 
                    @WordCount AS wordCount,
                    @CharCount AS charCount,
                    'en' AS language,
                    FORMAT(SYSUTCDATETIME(), 'yyyy-MM-ddTHH:mm:ss.fffZ') AS extractedAt
                FOR JSON PATH, WITHOUT_ARRAY_WRAPPER
            );
        END
        ELSE
        BEGIN
            -- For other modalities or external payloads, store basic metadata
            SET @ExtractedMetadata = (
                SELECT 
                    @Modality AS modality,
                    'External payload - metadata extraction requires payload loading' AS note,
                    FORMAT(SYSUTCDATETIME(), 'yyyy-MM-ddTHH:mm:ss.fffZ') AS extractedAt
                FOR JSON PATH, WITHOUT_ARRAY_WRAPPER
            );
        END
        
        -- Update atom metadata
        UPDATE dbo.Atom
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