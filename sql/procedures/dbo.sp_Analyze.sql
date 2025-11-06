-- sp_Analyze: Autonomous loop Phase 1 - Observation & Analysis
-- Queries embeddings, detects anomalies, identifies patterns
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
        -- 1. QUERY RECENT INFERENCE ACTIVITY
        DECLARE @RecentInferences TABLE (
            InferenceRequestId BIGINT,
            ModelId INT,
            RequestedAt DATETIME2,
            CompletedAt DATETIME2,
            DurationMs INT,
            TokenCount INT
        );
        
        INSERT INTO @RecentInferences
        SELECT TOP 1000
            InferenceId AS InferenceRequestId,
            CAST(JSON_VALUE(ModelsUsed, '$[0].ModelId') AS INT) AS ModelId,
            RequestTimestamp AS RequestedAt,
            DATEADD(MILLISECOND, ISNULL(TotalDurationMs, 0), RequestTimestamp) AS CompletedAt,
            ISNULL(TotalDurationMs, 0) AS DurationMs,
            -- Estimate token count: ~4 chars per token for English text
            (LEN(ISNULL(InputData, '')) + LEN(ISNULL(OutputData, ''))) / 4 AS TokenCount
        FROM dbo.InferenceRequests
        WHERE RequestTimestamp >= DATEADD(HOUR, -@LookbackHours, SYSUTCDATETIME())
            AND Status IN ('Completed', 'Failed')
        ORDER BY RequestTimestamp DESC;
        
        -- 2. DETECT PERFORMANCE ANOMALIES
        -- Find inferences that took >2x the average duration
        DECLARE @AvgDurationMs FLOAT;
        SELECT @AvgDurationMs = AVG(CAST(DurationMs AS FLOAT))
        FROM @RecentInferences
        WHERE DurationMs > 0;
        
        SELECT @Anomalies = (
            SELECT 
                ir.InferenceRequestId,
                ir.ModelId,
                ir.DurationMs,
                @AvgDurationMs AS AvgDurationMs,
                (ir.DurationMs / @AvgDurationMs) AS SlowdownFactor
            FROM @RecentInferences ir
            WHERE ir.DurationMs > (@AvgDurationMs * 2)
            FOR JSON PATH
        );
        
        -- 3. IDENTIFY EMBEDDING PATTERNS
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
