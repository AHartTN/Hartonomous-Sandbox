-- =============================================
-- Stored Procedure: sp_GenerateText
-- Description: T-SQL wrapper for fn_GenerateText CLR function
-- Provides text generation using multi-modal generation with attention
-- =============================================
CREATE PROCEDURE dbo.sp_GenerateText
    @prompt NVARCHAR(MAX),
    @max_tokens INT = 100,
    @temperature FLOAT = 0.7,
    @model_id INT = NULL,
    @tenant_id INT = 0,
    @GeneratedText NVARCHAR(MAX) OUTPUT
AS
BEGIN
    SET NOCOUNT ON;
    
    DECLARE @generationStreamId BIGINT;
    DECLARE @inputAtomIds NVARCHAR(MAX) = '';
    DECLARE @contextJson NVARCHAR(MAX) = N'{"prompt":"' + REPLACE(@prompt, '"', '\"') + '"}';
    
    -- Use default model if not specified
    IF @model_id IS NULL
    BEGIN
        SELECT TOP 1 @model_id = ModelId 
        FROM dbo.Model 
        WHERE IsActive = 1 
          AND ModelType = 'text'
        ORDER BY LastUsed DESC;
    END
    
    -- Validate model exists
    IF @model_id IS NULL
    BEGIN
        RAISERROR('No active text model found', 16, 1);
        RETURN -1;
    END
    
    BEGIN TRY
        -- Call CLR function for text generation
        SET @generationStreamId = dbo.fn_GenerateText(
            @model_id,
            @inputAtomIds,
            @contextJson,
            @max_tokens,
            @temperature,
            50,  -- topK
            0.9, -- topP
            @tenant_id
        );
        
        IF @generationStreamId IS NULL
        BEGIN
            SET @GeneratedText = NULL;
            RETURN -2;
        END
        
        -- Retrieve generated text from provenance stream
        -- This would query the GenerationStream/AtomProvenance tables
        -- For now, return a placeholder indicating success
        SET @GeneratedText = N'[Generation stream ID: ' + CAST(@generationStreamId AS NVARCHAR(20)) + ']';
        
        RETURN 0;
    END TRY
    BEGIN CATCH
        DECLARE @ErrorMessage NVARCHAR(4000) = ERROR_MESSAGE();
        DECLARE @ErrorSeverity INT = ERROR_SEVERITY();
        
        RAISERROR(@ErrorMessage, @ErrorSeverity, 1);
        RETURN -3;
    END CATCH
END
GO
