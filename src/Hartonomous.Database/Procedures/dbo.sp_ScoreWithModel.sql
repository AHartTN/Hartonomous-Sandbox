-- Auto-split from dbo.ModelManagement.sql
-- Object: PROCEDURE dbo.sp_ScoreWithModel

CREATE PROCEDURE dbo.sp_ScoreWithModel
    @ModelId INT,
    @InputAtomIds NVARCHAR(MAX), -- Comma-separated
    @OutputFormat NVARCHAR(50) = 'JSON',
    @TenantId INT = 0
AS
BEGIN
    SET NOCOUNT ON;
    
    BEGIN TRY
        -- Parse input atoms
        DECLARE @InputAtoms TABLE (AtomId BIGINT);
        INSERT INTO @InputAtoms
        SELECT CAST(value AS BIGINT)
        FROM STRING_SPLIT(@InputAtomIds, ',');
        
        -- Load model
        DECLARE @ModelBytes VARBINARY(MAX);
        DECLARE @ModelType NVARCHAR(50);
        
        SELECT 
            @ModelBytes = SerializedModel,
            @ModelType = ModelType
        FROM dbo.Model
        WHERE ModelId = @ModelId AND TenantId = @TenantId;
        
        IF @ModelBytes IS NULL
        BEGIN
            RAISERROR('Model not found or not loaded', 16, 1);
            RETURN -1;
        END
        
        -- Prepare input features
        DECLARE @InputFeatures TABLE (
            AtomId BIGINT,
            SpatialKey VECTOR(1536)
        );
        
        INSERT INTO @InputFeatures
        SELECT 
            ae.AtomId,
            ae.EmbeddingVector
        FROM dbo.AtomEmbedding ae
        INNER JOIN @InputAtoms ia ON ae.AtomId = ia.AtomId
        WHERE ae.TenantId = @TenantId;
        
        -- Execute PREDICT (placeholder - actual syntax depends on ML Services setup)
        -- SELECT * FROM PREDICT(MODEL = @ModelBytes, DATA = @InputFeatures);
        
        -- For now, return mock predictions
        SELECT 
            AtomId,
            0.95 AS Score,
            'ClassA' AS PredictedLabel
        FROM @InputFeatures
        FOR JSON PATH;
        
        RETURN 0;
    END TRY
    BEGIN CATCH
        DECLARE @ErrorMessage NVARCHAR(4000) = ERROR_MESSAGE();
        RAISERROR(@ErrorMessage, 16, 1);
        RETURN -1;
    END CATCH
END;
GO
