-- =============================================
-- PREDICT Integration for Discriminative Models
-- =============================================
-- Integrates ONNX/RevoScale discriminative models for autonomous improvement evaluation
-- Scoring, ranking, quality gates, change success prediction
-- =============================================

USE Hartonomous;
GO

PRINT '=======================================================';
PRINT 'PREDICT Integration Setup';
PRINT 'Phase 1: Train and deploy discriminative models';
PRINT 'Phase 2: CREATE EXTERNAL MODEL statements';
PRINT 'Phase 3: Integrate PREDICT() into procedures';
PRINT '=======================================================';
GO

-- =============================================
-- Model 1: Change Success Predictor
-- =============================================
-- Predicts probability that a code change will pass CI/CD
-- Training: AutonomousImprovementHistory → Features: ChangeType, TestResults, PerfDelta → Target: Success/Failure
-- =============================================

PRINT '';
PRINT '--- Change Success Predictor Model ---';
PRINT 'Training Data Source: dbo.AutonomousImprovementHistory';
PRINT 'Features: ChangeType, TestCoverage, PerfDelta, CodeComplexity';
PRINT 'Target: Success (1) / Failure (0)';
PRINT '
CREATE EXTERNAL MODEL ChangeSuccessPredictor
WITH (
    LOCATION = ''C:\Models\change_success_predictor.onnx'',
    API_FORMAT = ''ONNX Runtime'',
    MODEL_TYPE = ''Classification'',
    MODEL = ''change_success_logistic_regression'',
    LOCAL_RUNTIME_PATH = ''C:\onnx_runtime\''
);
';
GO

-- Example training SQL (using RevoScaleR for Windows compatibility)
PRINT '';
PRINT 'Example Training SQL (RevoScaleR):';
PRINT '
DECLARE @model VARBINARY(MAX);

EXEC sp_execute_external_script
    @language = N''R'',
    @script = N''
        library(RevoScaleR)
        
        # Prepare training data
        train_data <- InputDataSet
        train_data$Success <- as.factor(train_data$Success)
        
        # Train logistic regression
        model <- rxLogit(Success ~ ChangeType + TestCoverage + PerfDelta + CodeComplexity,
                         data = train_data)
        
        # Serialize for native scoring
        model_serialized <- rxSerializeModel(model, realtimeScoringOnly = TRUE)
    '',
    @input_data_1 = N''
        SELECT 
            CASE WHEN Outcome = ''''success'''' THEN 1 ELSE 0 END AS Success,
            ChangeType,
            TestResults AS TestCoverage,
            ISNULL(PerformanceDelta, 0) AS PerfDelta,
            DATALENGTH(CodeDiff) AS CodeComplexity
        FROM dbo.AutonomousImprovementHistory
        WHERE Outcome IS NOT NULL
    '',
    @params = N''@model VARBINARY(MAX) OUTPUT'',
    @model = @model OUTPUT;

-- Store model
INSERT INTO dbo.PredictiveModels (ModelName, ModelType, ModelBinary, CreatedAt)
VALUES (''ChangeSuccessPredictor'', ''RevoScale'', @model, SYSUTCDATETIME());
';
GO

-- =============================================
-- Model 2: Quality Scorer
-- =============================================
-- Scores generated code/changes for quality (0-100)
-- Training: InferenceRequests with UserRating → Features: Complexity, TestCoverage, PatternMatch → Target: Rating
-- =============================================

PRINT '';
PRINT '--- Quality Scorer Model ---';
PRINT 'Training Data Source: dbo.InferenceRequests + dbo.GenerationStreams';
PRINT 'Features: TokenCount, ModelCount, Complexity, SemanticSimilarity';
PRINT 'Target: UserRating (0-5) scaled to 0-100';
PRINT '
CREATE EXTERNAL MODEL QualityScorer
WITH (
    LOCATION = ''C:\Models\quality_scorer.onnx'',
    API_FORMAT = ''ONNX Runtime'',
    MODEL_TYPE = ''Regression'',
    MODEL = ''quality_scoring_linear_regression'',
    LOCAL_RUNTIME_PATH = ''C:\onnx_runtime\''
);
';
GO

-- =============================================
-- Model 3: Search Reranker
-- =============================================
-- Reranks hybrid search results based on relevance signals
-- Training: Click-through data → Features: CosineSimilarity, SpatialDistance, Recency → Target: Click (1/0)
-- =============================================

PRINT '';
PRINT '--- Search Reranker Model ---';
PRINT 'Training Data Source: dbo.InferenceRequests + dbo.AtomEmbeddings';
PRINT 'Features: CosineSimilarity, SpatialDistance, Recency, Modality';
PRINT 'Target: Clicked (1) or Skipped (0)';
PRINT '
CREATE EXTERNAL MODEL SearchReranker
WITH (
    LOCATION = ''C:\Models\search_reranker.onnx'',
    API_FORMAT = ''ONNX Runtime'',
    MODEL_TYPE = ''Classification'',
    MODEL = ''search_rerank_gradient_boosting'',
    LOCAL_RUNTIME_PATH = ''C:\onnx_runtime\''
);
';
GO

-- =============================================
-- Supporting Infrastructure
-- =============================================

-- Models catalog table
IF OBJECT_ID('dbo.PredictiveModels', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.PredictiveModels
    (
        ModelId INT IDENTITY(1,1) PRIMARY KEY,
        ModelName NVARCHAR(128) NOT NULL UNIQUE,
        ModelType NVARCHAR(64) NOT NULL, -- 'RevoScale', 'ONNX', 'ExternalAPI'
        ModelBinary VARBINARY(MAX) NULL, -- For native models
        ExternalLocation NVARCHAR(512) NULL, -- For ONNX files
        ModelMetadata NVARCHAR(MAX) NULL, -- JSON: features, target, performance metrics
        CreatedAt DATETIME2(7) NOT NULL DEFAULT SYSUTCDATETIME(),
        UpdatedAt DATETIME2(7) NULL,
        IsActive BIT NOT NULL DEFAULT 1
    );

    PRINT 'Created dbo.PredictiveModels catalog table';
END;
GO

-- =============================================
-- Integration Example: sp_AutonomousImprovement Phase 5 (Evaluate)
-- =============================================

PRINT '';
PRINT '--- Integration Example: Autonomous Improvement Evaluation ---';
PRINT '
-- Phase 5: Evaluate change success probability

DECLARE @model VARBINARY(MAX) = (
    SELECT ModelBinary 
    FROM dbo.PredictiveModels 
    WHERE ModelName = ''ChangeSuccessPredictor'' AND IsActive = 1
);

-- Extract features from current improvement attempt
DECLARE @features TABLE (
    ChangeType NVARCHAR(64),
    TestCoverage FLOAT,
    PerfDelta FLOAT,
    CodeComplexity INT
);

INSERT INTO @features
SELECT 
    @change_type,
    @test_coverage,
    @performance_delta,
    DATALENGTH(@code_diff);

-- Score using PREDICT
DECLARE @successProbability FLOAT;

SELECT @successProbability = Score
FROM PREDICT(
    MODEL = @model,
    DATA = @features AS d
) WITH (Score FLOAT);

IF @successProbability < 0.7
BEGIN
    INSERT INTO dbo.AutonomousImprovementHistory (...)
    VALUES (..., ''rejected_low_confidence'', @successProbability);
    
    RETURN; -- Don''t deploy low-confidence changes
END;

-- Proceed with deployment if confidence >= 70%
';
GO

-- =============================================
-- Integration Example: sp_HybridSearch Reranking
-- =============================================

PRINT '';
PRINT '--- Integration Example: Hybrid Search Reranking ---';
PRINT '
-- After spatial filter + vector rerank, apply ML reranker

-- Get initial candidates
DECLARE @candidates TABLE (
    AtomEmbeddingId BIGINT,
    CosineSimilarity FLOAT,
    SpatialDistance FLOAT,
    Recency INT,
    Modality NVARCHAR(64)
);

INSERT INTO @candidates
EXEC dbo.sp_HybridSearch 
    @query_vector = @query_embedding,
    @query_point = @query_spatial,
    @top_k = 20,
    @distance_metric = ''cosine'';

-- Load reranker model
DECLARE @reranker_model VARBINARY(MAX) = (
    SELECT ModelBinary 
    FROM dbo.PredictiveModels 
    WHERE ModelName = ''SearchReranker'' AND IsActive = 1
);

-- Apply PREDICT for final ranking
SELECT TOP 10
    c.AtomEmbeddingId,
    c.CosineSimilarity,
    c.SpatialDistance,
    p.RerankScore
FROM @candidates AS c
CROSS APPLY (
    SELECT Score AS RerankScore
    FROM PREDICT(MODEL = @reranker_model, DATA = c) WITH (Score FLOAT)
) AS p
ORDER BY p.RerankScore DESC;
';
GO

-- =============================================
-- Training Automation Procedure
-- =============================================

IF OBJECT_ID('dbo.sp_TrainPredictiveModels', 'P') IS NOT NULL
    DROP PROCEDURE dbo.sp_TrainPredictiveModels;
GO

CREATE PROCEDURE dbo.sp_TrainPredictiveModels
    @ModelName NVARCHAR(128) = NULL, -- Train specific model or all if NULL
    @RetrainThresholdDays INT = 30   -- Retrain if model older than this
AS
BEGIN
    SET NOCOUNT ON;
    
    DECLARE @ModelsToTrain TABLE (
        ModelName NVARCHAR(128),
        TrainingSQL NVARCHAR(MAX)
    );
    
    -- Define training configurations
    INSERT INTO @ModelsToTrain (ModelName, TrainingSQL)
    VALUES 
    ('ChangeSuccessPredictor', '
        DECLARE @model VARBINARY(MAX);
        EXEC sp_execute_external_script
            @language = N''R'',
            @script = N''
                library(RevoScaleR)
                train_data <- InputDataSet
                train_data$Success <- as.factor(train_data$Success)
                model <- rxLogit(Success ~ ChangeType + TestCoverage + PerfDelta + CodeComplexity, data = train_data)
                model_serialized <- rxSerializeModel(model, realtimeScoringOnly = TRUE)
            '',
            @input_data_1 = N''SELECT ... FROM dbo.AutonomousImprovementHistory ...'',
            @params = N''@model VARBINARY(MAX) OUTPUT'',
            @model = @model OUTPUT;
        UPDATE dbo.PredictiveModels SET ModelBinary = @model, UpdatedAt = SYSUTCDATETIME() WHERE ModelName = ''ChangeSuccessPredictor'';
    ');
    
    -- Execute training for each model
    DECLARE @sql NVARCHAR(MAX);
    DECLARE @currentModel NVARCHAR(128);
    
    DECLARE model_cursor CURSOR FOR
    SELECT ModelName, TrainingSQL
    FROM @ModelsToTrain
    WHERE (@ModelName IS NULL OR ModelName = @ModelName);
    
    OPEN model_cursor;
    FETCH NEXT FROM model_cursor INTO @currentModel, @sql;
    
    WHILE @@FETCH_STATUS = 0
    BEGIN
        PRINT 'Training model: ' + @currentModel;
        
        BEGIN TRY
            EXEC sp_executesql @sql;
            PRINT '  ✓ Model trained successfully';
        END TRY
        BEGIN CATCH
            PRINT '  ✗ Error training model: ' + ERROR_MESSAGE();
        END CATCH;
        
        FETCH NEXT FROM model_cursor INTO @currentModel, @sql;
    END;
    
    CLOSE model_cursor;
    DEALLOCATE model_cursor;
    
    PRINT '';
    PRINT 'Model training complete';
END;
GO

PRINT '';
PRINT '=======================================================';
PRINT 'PREDICT Integration Setup Complete';
PRINT 'Next Steps:';
PRINT '1. Install SQL Server ML Services (R) if not present';
PRINT '2. Enable external scripts: sp_configure ''external scripts enabled'', 1; RECONFIGURE;';
PRINT '3. Download ONNX Runtime to C:\onnx_runtime\';
PRINT '4. Train initial models: EXEC dbo.sp_TrainPredictiveModels;';
PRINT '5. Update sp_AutonomousImprovement to use PREDICT in Phase 5';
PRINT '6. Update sp_HybridSearch to use reranking PREDICT model';
PRINT '=======================================================';
GO
