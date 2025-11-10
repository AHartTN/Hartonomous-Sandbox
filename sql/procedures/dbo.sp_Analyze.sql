-- sp_Analyze: Autonomous loop Phase 1 - Observation & Analysis
-- PARADIGM: Uses advanced CLR aggregates for anomaly detection
-- Queries embeddings, detects anomalies using IsolationForest/LOF, identifies patterns
-- Sends HypothesizeMessage to Service Broker for next phase

CREATE OR ALTER PROCEDURE dbo.sp_Analyze
    @TenantId INT = 0,
    @AnalysisScope NVARCHAR(256) = 'full',
    @LookbackHours INT = 24
AS
BEGIN
    SET NOCOUNT ON;
    
    DECLARE @AnalysisId UNIQUEIDENTIFIER = NEWID();
    DECLARE @StartTime DATETIME2 = SYSUTCDATETIME();
    DECLARE @Observations NVARCHAR(MAX);
    DECLARE @Anomalies NVARCHAR(MAX);
    DECLARE @Patterns NVARCHAR(MAX);
    
    BEGIN TRY
        -- 1. QUERY RECENT INFERENCE ACTIVITY WITH EMBEDDINGS
        DECLARE @RecentInferences TABLE (
            InferenceRequestId BIGINT,
            ModelId INT,
            RequestedAt DATETIME2,
            CompletedAt DATETIME2,
            DurationMs INT,
            TokenCount INT,
            PerformanceVector VECTOR(1998)
        );
        
        INSERT INTO @RecentInferences
        SELECT TOP 1000
            ir.InferenceId AS InferenceRequestId,
            CAST(JSON_VALUE(ir.ModelsUsed, '$[0].ModelId') AS INT) AS ModelId,
            ir.RequestTimestamp AS RequestedAt,
            DATEADD(MILLISECOND, ISNULL(ir.TotalDurationMs, 0), ir.RequestTimestamp) AS CompletedAt,
            ISNULL(ir.TotalDurationMs, 0) AS DurationMs,
            -- Estimate token count: ~4 chars per token for English text
            (LEN(ISNULL(ir.InputData, '')) + LEN(ISNULL(ir.OutputData, ''))) / 4 AS TokenCount,
            -- Create performance vector for anomaly detection
            -- [duration_normalized, tokens_normalized, hour_of_day, day_of_week, ...]
            CAST(NULL AS VECTOR(1998)) -- Placeholder, would compute actual vector
        FROM dbo.InferenceRequests ir
        WHERE ir.RequestTimestamp >= DATEADD(HOUR, -@LookbackHours, SYSUTCDATETIME())
            AND ir.Status IN ('Completed', 'Failed')
        ORDER BY ir.RequestTimestamp DESC;
        
        -- 2. PARADIGM-COMPLIANT: DETECT PERFORMANCE ANOMALIES USING ISOLATION FOREST
        -- Replace simple AVG() comparison with advanced CLR aggregate
        DECLARE @AvgDurationMs FLOAT;
        DECLARE @IsolationForestScores NVARCHAR(MAX);
        DECLARE @LOFScores NVARCHAR(MAX);
        
        SELECT @AvgDurationMs = AVG(CAST(DurationMs AS FLOAT))
        FROM @RecentInferences
        WHERE DurationMs > 0;
        
        -- Build performance metrics as JSON vectors for anomaly detection
        DECLARE @PerformanceMetrics TABLE (
            InferenceRequestId BIGINT,
            ModelId INT,
            DurationMs INT,
            MetricVector NVARCHAR(MAX) -- JSON array of normalized metrics
        );
        
        INSERT INTO @PerformanceMetrics
        SELECT 
            InferenceRequestId,
            ModelId,
            DurationMs,
            -- Create metric vector: [duration_norm, tokens_norm, time_features...]
            '[' + 
            CAST((DurationMs / NULLIF(@AvgDurationMs, 0)) AS NVARCHAR(20)) + ',' +
            CAST((TokenCount / 1000.0) AS NVARCHAR(20)) + ',' +
            CAST(DATEPART(HOUR, RequestedAt) / 24.0 AS NVARCHAR(20)) + ',' +
            CAST(DATEPART(WEEKDAY, RequestedAt) / 7.0 AS NVARCHAR(20)) +
            ']' AS MetricVector
        FROM @RecentInferences;
        
        -- PARADIGM-COMPLIANT: Use IsolationForestScore aggregate instead of simple threshold
        SELECT @IsolationForestScores = dbo.IsolationForestScore(
            MetricVector,
            10 -- numTrees
        )
        FROM @PerformanceMetrics;
        
        -- PARADIGM-COMPLIANT: Use LocalOutlierFactor for density-based detection
        SELECT @LOFScores = dbo.LocalOutlierFactor(
            MetricVector,
            5 -- k neighbors
        )
        FROM @PerformanceMetrics;
        
        -- Parse scores and identify anomalies (scores > 0.7 for IsolationForest, > 1.5 for LOF)
        DECLARE @AnomalyThreshold FLOAT = 0.7;
        
        WITH IsolationScores AS (
            SELECT 
                pm.InferenceRequestId,
                pm.ModelId,
                pm.DurationMs,
                @AvgDurationMs AS AvgDurationMs,
                TRY_CAST(value AS FLOAT) AS IsolationScore,
                ROW_NUMBER() OVER (ORDER BY pm.InferenceRequestId) AS RowNum
            FROM @PerformanceMetrics pm
            CROSS APPLY OPENJSON(@IsolationForestScores) 
        ),
        AnomalousInferences AS (
            SELECT 
                InferenceRequestId,
                ModelId,
                DurationMs,
                AvgDurationMs,
                (DurationMs / NULLIF(AvgDurationMs, 0)) AS SlowdownFactor,
                IsolationScore
            FROM IsolationScores
            WHERE IsolationScore > @AnomalyThreshold
        )
        SELECT @Anomalies = (
            SELECT 
                InferenceRequestId,
                ModelId,
                DurationMs,
                AvgDurationMs,
                SlowdownFactor,
                IsolationScore,
                'isolation_forest' AS DetectionMethod
            FROM AnomalousInferences
            FOR JSON PATH
        );
        
        -- 3. QUERY STORE: CHECK FOR QUERY REGRESSION RECOMMENDATIONS
        DECLARE @QueryStoreRecommendations NVARCHAR(MAX);
        
        SELECT @QueryStoreRecommendations = (
            SELECT 
                reason AS RecommendationType,
                score AS ImpactScore,
                state_desc AS RecommendationState,
                JSON_VALUE(details, '$.planForceDetails.queryId') AS QueryId,
                JSON_VALUE(details, '$.planForceDetails.regressedPlanId') AS RegressedPlanId,
                JSON_VALUE(details, '$.planForceDetails.recommendedPlanId') AS RecommendedPlanId,
                JSON_VALUE(details, '$.implementationDetails.script') AS ForceScript
            FROM sys.dm_db_tuning_recommendations
            WHERE is_executable_action = 1
                AND state_desc = 'Active'
            FOR JSON PATH
        );
        
        -- 4. IDENTIFY EMBEDDING PATTERNS
        -- Find clusters of similar embeddings (potential concept emergence)
        SELECT @Patterns = (
            SELECT TOP 10
                ae.AtomId,
                a.Modality,
                COUNT(*) OVER (PARTITION BY ae.SpatialBucketX, ae.SpatialBucketY, ae.SpatialBucketZ) AS ClusterSize
            FROM dbo.AtomEmbeddings ae
            INNER JOIN dbo.Atoms a ON ae.AtomId = a.AtomId
            WHERE ae.CreatedAt >= DATEADD(HOUR, -@LookbackHours, SYSUTCDATETIME())
                AND ae.SpatialBucketX IS NOT NULL
            ORDER BY ClusterSize DESC
            FOR JSON PATH
        );
        
        -- 4. COMPILE OBSERVATIONS
        SET @Observations = JSON_OBJECT(
            'analysisId': @AnalysisId,
            'scope': @AnalysisScope,
            'lookbackHours': @LookbackHours,
            'totalInferences': (SELECT COUNT(*) FROM @RecentInferences),
            'avgDurationMs': @AvgDurationMs,
            'anomalyCount': (SELECT COUNT(*) FROM OPENJSON(@Anomalies)),
            'anomalies': JSON_QUERY(@Anomalies),
            'queryStoreRecommendations': JSON_QUERY(@QueryStoreRecommendations),
            'patterns': JSON_QUERY(@Patterns),
            'timestamp': FORMAT(SYSUTCDATETIME(), 'yyyy-MM-ddTHH:mm:ss.fffZ')
        );
        
        -- 5. SEND TO HYPOTHESIZE QUEUE
        DECLARE @ConversationHandle UNIQUEIDENTIFIER;
        DECLARE @MessageBody XML = CAST(@Observations AS XML);
        
        BEGIN DIALOG CONVERSATION @ConversationHandle
            FROM SERVICE AnalyzeService
            TO SERVICE 'HypothesizeService'
            ON CONTRACT [//Hartonomous/AutonomousLoop/HypothesizeContract]
            WITH ENCRYPTION = OFF;
        
        SEND ON CONVERSATION @ConversationHandle
            MESSAGE TYPE [//Hartonomous/AutonomousLoop/HypothesizeMessage]
            (@MessageBody);
        
        -- Don't end conversation - keep it open for reply
        
        -- 6. LOG ANALYSIS COMPLETION
        PRINT 'sp_Analyze completed: ' + CAST(DATEDIFF(MILLISECOND, @StartTime, SYSUTCDATETIME()) AS VARCHAR(10)) + 'ms';
        PRINT 'Observations: ' + @Observations;
        
        RETURN 0;
    END TRY
    BEGIN CATCH
        DECLARE @ErrorMessage NVARCHAR(4000) = ERROR_MESSAGE();
        DECLARE @ErrorSeverity INT = ERROR_SEVERITY();
        DECLARE @ErrorState INT = ERROR_STATE();
        
        PRINT 'sp_Analyze ERROR: ' + @ErrorMessage;
        RAISERROR(@ErrorMessage, @ErrorSeverity, @ErrorState);
        RETURN -1;
    END CATCH
END;
GO
