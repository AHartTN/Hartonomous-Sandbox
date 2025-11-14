-- sp_Analyze: Autonomous loop Phase 1 - Observation & Analysis
-- PARADIGM: Uses advanced CLR aggregates for anomaly detection
-- Queries embeddings, detects anomalies using IsolationForest/LOF, identifies patterns
-- Sends HypothesizeMessage to Service Broker for next phase

CREATE PROCEDURE dbo.sp_Analyze
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
        -- GÖDEL ENGINE: Check for autonomous compute job messages (bypass performance analysis)
        -- This allows the OODA loop to process abstract computational tasks (prime search, proofs, etc.)
        DECLARE @ConversationHandle UNIQUEIDENTIFIER;
        DECLARE @MessageBody XML;
        DECLARE @MessageTypeName NVARCHAR(256);
        
        -- Try to receive a message (non-blocking check)
        RECEIVE TOP(1)
            @ConversationHandle = conversation_handle,
            @MessageBody = CAST(message_body AS XML),
            @MessageTypeName = message_type_name
        FROM AnalyzeQueue;
        
        -- If we received a compute job request, route it directly to Hypothesize
        IF @MessageBody IS NOT NULL
        BEGIN
            DECLARE @JobId UNIQUEIDENTIFIER = @MessageBody.value('(/JobRequest/JobId)[1]', 'uniqueidentifier');
            
            IF @JobId IS NOT NULL
            BEGIN
                -- This is a compute job request, not a performance analysis trigger
                PRINT 'sp_Analyze: Detected compute job request for JobId: ' + CAST(@JobId AS NVARCHAR(36));
                
                DECLARE @HypothesisPayload XML = (
                    SELECT @JobId AS JobId 
                    FOR XML PATH('ComputeJob'), ROOT('Hypothesis')
                );
                
                DECLARE @HypothesizeHandle UNIQUEIDENTIFIER;
                
                BEGIN DIALOG CONVERSATION @HypothesizeHandle
                    FROM SERVICE AnalyzeService
                    TO SERVICE 'HypothesizeService'
                    ON CONTRACT [//Hartonomous/AutonomousLoop/HypothesizeContract]
                    WITH ENCRYPTION = OFF;
                
                SEND ON CONVERSATION @HypothesizeHandle
                    MESSAGE TYPE [//Hartonomous/AutonomousLoop/HypothesizeMessage]
                    (@HypothesisPayload);
                
                END CONVERSATION @ConversationHandle;
                
                PRINT 'sp_Analyze: Compute job routed to Hypothesize phase.';
                RETURN 0;
            END
        END
        
        -- REGULAR OODA LOOP: Continue with standard performance analysis
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
            (LEN(CAST(ir.InputData AS NVARCHAR(MAX))) + LEN(CAST(ir.OutputData AS NVARCHAR(MAX)))) / 4 AS TokenCount,
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
        SELECT @IsolationForestScores = dbo.IsolationForestScore(MetricVector)
        FROM @PerformanceMetrics;
        
        -- PARADIGM-COMPLIANT: Use LocalOutlierFactor for density-based detection
        SELECT @LOFScores = dbo.LocalOutlierFactor(
            MetricVector,
            5 -- k neighbors
        )
        FROM @PerformanceMetrics;
        
        -- Parse scores and identify anomalies (scores > 0.7 for IsolationForest, > 1.5 for LOF)
        DECLARE @IsolationThreshold FLOAT = 0.7;
        DECLARE @LOFThreshold FLOAT = 1.5;

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
        DECLARE @QueryStoreRecommendations NVARCHAR(MAX);
        
        SELECT @QueryStoreRecommendations = (
            SELECT 
                reason AS RecommendationType,
                score AS ImpactScore,
                state AS RecommendationState,
                JSON_VALUE(details, '$.planForceDetails.queryId') AS QueryId,
                JSON_VALUE(details, '$.planForceDetails.regressedPlanId') AS RegressedPlanId,
                JSON_VALUE(details, '$.planForceDetails.recommendedPlanId') AS RecommendedPlanId,
                JSON_VALUE(details, '$.implementationDetails.script') AS ForceScript
            FROM sys.dm_db_tuning_recommendations
            WHERE is_executable_action = 1
                AND state = 1  -- Active state
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
        
        -- 5. SPATIO-TEMPORAL ANALYTICS: Detect "Untapped Knowledge" regions
        -- High Pressure (many embeddings) + Low Velocity (rarely used) = Untapped potential
        DECLARE @UntappedKnowledge NVARCHAR(MAX);
        
        SELECT @UntappedKnowledge = (
            SELECT TOP 20
                RankedRegions.HilbertValue,
                RankedRegions.Pressure,
                RankedRegions.Velocity,
                RankedRegions.PressureRank,
                RankedRegions.VelocityRank
            FROM (
                SELECT 
                    ae.[HilbertValue],
                    COUNT_BIG(ae.AtomId) AS Pressure,
                    ISNULL((
                        SELECT COUNT_BIG(1) 
                        FROM [dbo].[InferenceTracking] it 
                        WHERE it.AtomId = ae.AtomId
                    ), 0) AS Velocity,
                    PERCENT_RANK() OVER (ORDER BY COUNT_BIG(ae.AtomId) DESC) AS PressureRank,
                    PERCENT_RANK() OVER (ORDER BY ISNULL((
                        SELECT COUNT_BIG(1) 
                        FROM [dbo].[InferenceTracking] it 
                        WHERE it.AtomId = ae.AtomId
                    ), 0) ASC) AS VelocityRank
                FROM [dbo].[AtomEmbeddings] ae
                WHERE ae.[HilbertValue] IS NOT NULL 
                  AND ae.[HilbertValue] <> 0
                GROUP BY ae.[HilbertValue], ae.AtomId
            ) AS RankedRegions
            WHERE RankedRegions.PressureRank < 0.1  -- Top 10% most dense
              AND RankedRegions.VelocityRank < 0.1  -- Bottom 10% least used
            ORDER BY (RankedRegions.PressureRank + RankedRegions.VelocityRank) ASC
            FOR JSON PATH
        );
        
        -- 6. COMPILE OBSERVATIONS
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
            'untappedKnowledge': JSON_QUERY(@UntappedKnowledge),
            'timestamp': FORMAT(SYSUTCDATETIME(), 'yyyy-MM-ddTHH:mm:ss.fffZ')
        );
        
        -- 7. SEND TO HYPOTHESIZE QUEUE
        DECLARE @AnalyzeConversationHandle UNIQUEIDENTIFIER;
        DECLARE @AnalyzeMessageBody XML = CAST(@Observations AS XML);
        
        BEGIN DIALOG CONVERSATION @AnalyzeConversationHandle
            FROM SERVICE AnalyzeService
            TO SERVICE 'HypothesizeService'
            ON CONTRACT [//Hartonomous/AutonomousLoop/HypothesizeContract]
            WITH ENCRYPTION = OFF;
        
        SEND ON CONVERSATION @AnalyzeConversationHandle
            MESSAGE TYPE [//Hartonomous/AutonomousLoop/HypothesizeMessage]
            (@AnalyzeMessageBody);
        
        -- Don't end conversation - keep it open for reply
        
        -- 8. LOG ANALYSIS COMPLETION
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
