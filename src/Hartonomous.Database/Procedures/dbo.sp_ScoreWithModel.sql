CREATE PROCEDURE dbo.sp_ScoreWithModel
    @ModelId INT,
    @InputAtomIds NVARCHAR(MAX), -- Comma-separated
    @OutputFormat NVARCHAR(50) = 'JSON',
    @TenantId INT = NULL -- Optional tenant filtering
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
        FROM dbo.Models
        WHERE ModelId = @ModelId AND TenantId = @TenantId;
        
        IF @ModelBytes IS NULL
        BEGIN
            RAISERROR('Model not found or not loaded', 16, 1);
            RETURN -1;
        END
        
        -- Prepare input features (AtomEmbeddings has no TenantId column)
        DECLARE @InputFeatures TABLE (
            AtomId BIGINT,
            EmbeddingVector VARBINARY(MAX)
        );
        
        INSERT INTO @InputFeatures
        SELECT 
            ae.AtomId,
            ae.EmbeddingVector
        FROM dbo.AtomEmbeddings ae
        INNER JOIN @InputAtoms ia ON ae.AtomId = ia.AtomId;
        
        -- Execute PREDICT using reconstructed model from LayerTensorSegments
        -- Model is already materialized as TensorAtom graph with coefficients
        -- Run forward pass through reconstructed layers
        
        DECLARE @PredictionResults TABLE (
            AtomId BIGINT,
            Score FLOAT,
            PredictedLabel NVARCHAR(100)
        );
        
        -- Inference pipeline: embedding → model layers → output
        -- Use CLR function to execute tensor operations through reconstructed graph
        
        DECLARE @CurrentAtomId BIGINT;
        DECLARE @EmbeddingVec VARBINARY(MAX);
        DECLARE @InferenceResult NVARCHAR(MAX);
        
        DECLARE input_cursor CURSOR FOR
        SELECT AtomId, EmbeddingVector FROM @InputFeatures;
        
        OPEN input_cursor;
        FETCH NEXT FROM input_cursor INTO @CurrentAtomId, @EmbeddingVec;
        
        WHILE @@FETCH_STATUS = 0
        BEGIN
            -- Forward pass through model using TensorAtom graph
            -- CLR function executes: layer1(x) → activation → layer2 → ... → output
            SET @InferenceResult = dbo.clr_ExecuteModelInference(@ModelId, @EmbeddingVec);
            
            -- Parse inference result JSON
            INSERT INTO @PredictionResults
            SELECT 
                @CurrentAtomId AS AtomId,
                CAST(JSON_VALUE(@InferenceResult, '$.score') AS FLOAT) AS Score,
                JSON_VALUE(@InferenceResult, '$.label') AS PredictedLabel;
            
            FETCH NEXT FROM input_cursor INTO @CurrentAtomId, @EmbeddingVec;
        END
        
        CLOSE input_cursor;
        DEALLOCATE input_cursor;
        
        -- Return predictions
        SELECT 
            AtomId,
            Score,
            PredictedLabel
        FROM @PredictionResults
        FOR JSON PATH;
        
        RETURN 0;
    END TRY
    BEGIN CATCH
        DECLARE @ErrorMessage NVARCHAR(4000) = ERROR_MESSAGE();
        RAISERROR(@ErrorMessage, 16, 1);
        RETURN -1;
    END CATCH
END;