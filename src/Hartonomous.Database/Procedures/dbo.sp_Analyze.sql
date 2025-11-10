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





    BEGIN TRY
        -- GÖDEL ENGINE: Check for autonomous compute job messages (bypass performance analysis)
        -- This allows the OODA loop to process abstract computational tasks (prime search, proofs, etc.)



        -- Try to receive a message (non-blocking check)
        RECEIVE TOP(1)
            @ConversationHandle = conversation_handle,
            @MessageBody = CAST(message_body AS XML),
            @MessageTypeName = message_type_name
        FROM AnalyzeQueue;
        
        -- If we received a compute job request, route it directly to Hypothesize
        IF @MessageBody IS NOT NULL
        BEGIN

            IF @JobId IS NOT NULL
            BEGIN
                -- This is a compute job request, not a performance analysis trigger
                PRINT 'sp_Analyze: Detected compute job request for JobId: ' + CAST(@JobId AS NVARCHAR(36));

                    SELECT @JobId AS JobId 
                    FOR XML PATH('ComputeJob'), ROOT('Hypothesis')
                );

                BEGIN DIALOG CONVERSATION @HypothesizeHandle
                    FROM SERVICE AnalyzeService
                    TO SERVICE 'HypothesizeService'
                    ON CONTRACT [//Hartonomous/AutonomousLoop/HypothesizeContract]
                    WITH ENCRYPTION = OFF;
                
                SEND ON CONVERSATION @HypothesizeHandle
                    MESSAGE TYPE [//Hartonomous/AutonomousLoop/HypothesizeMessage]
                    (@HypothesisPayload);
                
                END CONVERSATION @ConversationHandle;
                
                RETURN 0;
            END
        END
        END
        
        -- REGULAR OODA LOOP: Continue with standard performance analysis
        -- 1. QUERY RECENT INFERENCE ACTIVITY WITH EMBEDDINGS

            InferenceRequestId BIGINT,
            ModelId INT,
            RequestedAt DATETIME2,
            CompletedAt DATETIME2,
            DurationMs INT,
            TokenCount INT,
            PerformanceVector VECTOR(1998)
        );
        
        
        
        -- 2. PARADIGM-COMPLIANT: DETECT PERFORMANCE ANOMALIES USING ISOLATION FOREST
        -- Replace simple AVG() comparison with advanced CLR aggregate



        SELECT @AvgDurationMs = AVG(CAST(DurationMs AS FLOAT))
        FROM @RecentInferences
        WHERE DurationMs > 0;
        
        -- Build performance metrics as JSON vectors for anomaly detection

            InferenceRequestId BIGINT,
            ModelId INT,
            DurationMs INT,
            MetricVector NVARCHAR(MAX) -- JSON array of normalized metrics
        );
        
        
        
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
        LOFScoresParsed AS (
            SELECT
                pm.InferenceRequestId,
                TRY_CAST(value AS FLOAT) AS LOFScore,
                ROW_NUMBER() OVER (ORDER BY pm.InferenceRequestId) AS RowNum
            FROM @PerformanceMetrics pm
            CROSS APPLY OPENJSON(@LOFScores)
        ),
        CombinedScores AS (
            SELECT
                iso.InferenceRequestId,
                iso.ModelId,
                iso.DurationMs,
                iso.AvgDurationMs,
                iso.IsolationScore,
                lof.LOFScore
            FROM IsolationScores iso
            LEFT JOIN LOFScoresParsed lof ON iso.RowNum = lof.RowNum
        ),
        AnomalousInferences AS (
            -- DUAL DETECTION: Anomaly detected by EITHER IsolationForest OR LOF
            SELECT
                InferenceRequestId,
                ModelId,
                DurationMs,
                AvgDurationMs,
                (DurationMs / NULLIF(AvgDurationMs, 0)) AS SlowdownFactor,
                IsolationScore,
                LOFScore,
                CASE
                    WHEN IsolationScore > @IsolationThreshold AND LOFScore > @LOFThreshold THEN 'both'
                    WHEN IsolationScore > @IsolationThreshold THEN 'isolation_forest'
                    WHEN LOFScore > @LOFThreshold THEN 'lof'
                    ELSE NULL
                END AS DetectionMethod
            FROM CombinedScores
            WHERE IsolationScore > @IsolationThreshold
               OR LOFScore > @LOFThreshold
        )
        SELECT @Anomalies = (
            SELECT
                InferenceRequestId,
                ModelId,
                DurationMs,
                AvgDurationMs,
                SlowdownFactor,
                IsolationScore,
                LOFScore,
                DetectionMethod
            FROM AnomalousInferences
            FOR JSON PATH
        );
        
        -- 3. QUERY STORE: CHECK FOR QUERY REGRESSION RECOMMENDATIONS

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


        BEGIN DIALOG CONVERSATION @AnalyzeConversationHandle
            FROM SERVICE AnalyzeService
            TO SERVICE 'HypothesizeService'
            ON CONTRACT [//Hartonomous/AutonomousLoop/HypothesizeContract]
            WITH ENCRYPTION = OFF;
        
        SEND ON CONVERSATION @AnalyzeConversationHandle
            MESSAGE TYPE [//Hartonomous/AutonomousLoop/HypothesizeMessage]
            (@AnalyzeMessageBody);
        
        -- Don't end conversation - keep it open for reply
        
        -- 6. LOG ANALYSIS COMPLETION
        PRINT 'sp_Analyze completed: ' + CAST(DATEDIFF(MILLISECOND, @StartTime, SYSUTCDATETIME()) AS VARCHAR(10)) + 'ms';
        PRINT 'Observations: ' + @Observations;
        
        RETURN 0;
    END TRY
    BEGIN CATCH



        PRINT 'sp_Analyze ERROR: ' + @ErrorMessage;
        RAISERROR(@ErrorMessage, @ErrorSeverity, @ErrorState);
        RETURN -1;
    END CATCH
END;
