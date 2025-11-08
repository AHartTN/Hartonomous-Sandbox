-- sp_Learn: Autonomous loop Phase 4 - Learn & Adapt
-- Receives execution results from sp_Act
-- Measures performance delta (before/after)
-- Stores improvement history
-- Sends AnalyzeMessage to restart OODA loop

CREATE OR ALTER PROCEDURE dbo.sp_Learn
    @TenantId INT = 0
AS
BEGIN
    SET NOCOUNT ON;
    
    DECLARE @ConversationHandle UNIQUEIDENTIFIER;
    DECLARE @MessageBody XML;
    DECLARE @MessageTypeName NVARCHAR(256);
    DECLARE @ExecutionResultsJson NVARCHAR(MAX);
    DECLARE @LearningOutcomes NVARCHAR(MAX);
    
    BEGIN TRY
        -- 1. RECEIVE MESSAGE FROM ACT PHASE
        WAITFOR (
            RECEIVE TOP(1)
                @ConversationHandle = conversation_handle,
                @MessageBody = CAST(message_body AS XML),
                @MessageTypeName = message_type_name
            FROM LearnQueue
        ), TIMEOUT 5000; -- 5 second timeout
        
        IF @ConversationHandle IS NULL
        BEGIN
            PRINT 'sp_Learn: No messages received';
            RETURN 0;
        END
        
        -- 2. PARSE EXECUTION RESULTS
        SET @ExecutionResultsJson = CAST(@MessageBody AS NVARCHAR(MAX));
        
        DECLARE @AnalysisId UNIQUEIDENTIFIER = JSON_VALUE(@ExecutionResultsJson, '$.analysisId');
        DECLARE @ExecutedActions INT = JSON_VALUE(@ExecutionResultsJson, '$.executedActions');
        DECLARE @QueuedActions INT = JSON_VALUE(@ExecutionResultsJson, '$.queuedActions');
        DECLARE @FailedActions INT = JSON_VALUE(@ExecutionResultsJson, '$.failedActions');
        
        -- 3. MEASURE PERFORMANCE DELTA
        -- Baseline metrics (from before actions)
        DECLARE @BaselineAvgDurationMs FLOAT;
        DECLARE @BaselineThroughput INT;
        
        SELECT 
            @BaselineAvgDurationMs = AVG(DurationMs),
            @BaselineThroughput = COUNT(*)
        FROM dbo.InferenceRequests
        WHERE RequestedAt >= DATEADD(HOUR, -24, SYSUTCDATETIME())
              AND RequestedAt < DATEADD(MINUTE, -5, SYSUTCDATETIME()); -- Baseline: 24 hours ago to 5 minutes ago
        
        -- Current metrics (after actions)
        DECLARE @CurrentAvgDurationMs FLOAT;
        DECLARE @CurrentThroughput INT;
        
        SELECT 
            @CurrentAvgDurationMs = AVG(DurationMs),
            @CurrentThroughput = COUNT(*)
        FROM dbo.InferenceRequests
        WHERE RequestedAt >= DATEADD(MINUTE, -5, SYSUTCDATETIME()); -- Recent: last 5 minutes
        
        -- Calculate deltas
        DECLARE @LatencyImprovement FLOAT = 
            CASE WHEN @BaselineAvgDurationMs > 0 
                 THEN ((@BaselineAvgDurationMs - @CurrentAvgDurationMs) / @BaselineAvgDurationMs) * 100
                 ELSE 0 
            END;
        
        DECLARE @ThroughputChange FLOAT = 
            CASE WHEN @BaselineThroughput > 0 
                 THEN ((@CurrentThroughput - @BaselineThroughput) / CAST(@BaselineThroughput AS FLOAT)) * 100
                 ELSE 0 
            END;
        
        -- 4. ANALYZE ACTION OUTCOMES
        DECLARE @ActionOutcomes TABLE (
            HypothesisId UNIQUEIDENTIFIER,
            HypothesisType NVARCHAR(50),
            ActionStatus NVARCHAR(50),
            Outcome NVARCHAR(50),
            ImpactScore FLOAT
        );
        
        INSERT INTO @ActionOutcomes
        SELECT 
            HypothesisId,
            HypothesisType,
            ActionStatus,
            CASE 
                WHEN ActionStatus = 'Executed' AND @LatencyImprovement > 10 THEN 'HighSuccess'
                WHEN ActionStatus = 'Executed' AND @LatencyImprovement > 0 THEN 'Success'
                WHEN ActionStatus = 'Executed' AND @LatencyImprovement < 0 THEN 'Regressed'
                WHEN ActionStatus = 'Failed' THEN 'Failed'
                ELSE 'Neutral'
            END AS Outcome,
            @LatencyImprovement AS ImpactScore
        FROM OPENJSON(@ExecutionResultsJson, '$.results')
        WITH (
            HypothesisId UNIQUEIDENTIFIER,
            HypothesisType NVARCHAR(50),
            ActionStatus NVARCHAR(50)
        );
        
        -- 5. STORE IMPROVEMENT HISTORY
        -- (Note: AutonomousImprovementHistory table to be created in future migration)
        -- For now, just compile the learning outcomes
        
        DECLARE @SuccessfulActions INT = (SELECT COUNT(*) FROM @ActionOutcomes WHERE Outcome IN ('Success', 'HighSuccess'));
        DECLARE @RegressedActions INT = (SELECT COUNT(*) FROM @ActionOutcomes WHERE Outcome = 'Regressed');
        
        SELECT @LearningOutcomes = (
            SELECT 
                HypothesisId,
                HypothesisType,
                ActionStatus,
                Outcome,
                ImpactScore
            FROM @ActionOutcomes
            FOR JSON PATH
        );
        
        -- PARADIGM-COMPLIANT FIX: Close the feedback loop by fine-tuning the model
        -- This is the "L" (Learn) in OODA - actually update model weights based on success/failure
        IF EXISTS (
            SELECT 1 
            FROM dbo.AutonomousImprovementHistory 
            WHERE AnalysisId = @AnalysisId 
                AND GeneratedCode IS NOT NULL
                AND SuccessScore > 0.7 -- Only learn from successful improvements
        )
        BEGIN
            -- Trigger model weight updates for successful code generations
            DECLARE @ImprovementCursor CURSOR;
            DECLARE @ImprovementId UNIQUEIDENTIFIER;
            DECLARE @GeneratedCode NVARCHAR(MAX);
            DECLARE @SuccessScore DECIMAL(5,4);
            
            SET @ImprovementCursor = CURSOR FOR
                SELECT ImprovementId, GeneratedCode, SuccessScore
                FROM dbo.AutonomousImprovementHistory
                WHERE AnalysisId = @AnalysisId
                    AND SuccessScore > 0.7
                ORDER BY SuccessScore DESC;
            
            OPEN @ImprovementCursor;
            FETCH NEXT FROM @ImprovementCursor INTO @ImprovementId, @GeneratedCode, @SuccessScore;
            
            WHILE @@FETCH_STATUS = 0
            BEGIN
                -- Update model weights using the successful code as a training sample
                -- This fine-tunes the code generation model (e.g., Qwen3-Coder)
                BEGIN TRY
                    EXEC dbo.sp_UpdateModelWeightsFromFeedback
                        @ModelName = 'Qwen3-Coder-32B',
                        @TrainingSample = @GeneratedCode,
                        @RewardSignal = @SuccessScore,
                        @LearningRate = 0.0001,
                        @TenantId = @TenantId;
                    
                    -- Log the weight update
                    PRINT 'Model weights updated for improvement: ' + CAST(@ImprovementId AS NVARCHAR(36));
                END TRY
                BEGIN CATCH
                    -- Log but don't fail the learning cycle
                    PRINT 'Weight update failed for ' + CAST(@ImprovementId AS NVARCHAR(36)) + ': ' + ERROR_MESSAGE();
                END CATCH
                
                FETCH NEXT FROM @ImprovementCursor INTO @ImprovementId, @GeneratedCode, @SuccessScore;
            END
            
            CLOSE @ImprovementCursor;
            DEALLOCATE @ImprovementCursor;
        END
        
        DECLARE @LearningPayload NVARCHAR(MAX) = JSON_OBJECT(
            'analysisId': @AnalysisId,
            'learningCycleComplete': 1,
            'performanceMetrics': JSON_OBJECT(
                'baselineLatencyMs': @BaselineAvgDurationMs,
                'currentLatencyMs': @CurrentAvgDurationMs,
                'latencyImprovement': @LatencyImprovement,
                'throughputChange': @ThroughputChange
            ),
            'actionOutcomes': JSON_OBJECT(
                'successfulActions': @SuccessfulActions,
                'regressedActions': @RegressedActions,
                'totalActions': @ExecutedActions
            ),
            'outcomes': JSON_QUERY(@LearningOutcomes),
            'timestamp': FORMAT(SYSUTCDATETIME(), 'yyyy-MM-ddTHH:mm:ss.fffZ')
        );
        
        -- 6. DETERMINE NEXT ACTION
        -- If significant improvement, restart cycle immediately
        -- If regression, wait longer before next cycle
        DECLARE @NextCycleDelayMinutes INT = 
            CASE 
                WHEN @LatencyImprovement > 20 THEN 5  -- High success: check again soon
                WHEN @LatencyImprovement > 0 THEN 15  -- Success: normal interval
                WHEN @LatencyImprovement < 0 THEN 60  -- Regression: wait longer
                ELSE 30                               -- Neutral: moderate interval
            END;
        
        -- 7. RESTART OODA LOOP (send back to Analyze)
        DECLARE @AnalyzeConversationHandle UNIQUEIDENTIFIER;
        
        -- Build analysis trigger message
        DECLARE @AnalyseTrigger NVARCHAR(MAX) = JSON_OBJECT(
            'triggerReason': 'LearningCycleComplete',
            'previousAnalysisId': @AnalysisId,
            'latencyImprovement': @LatencyImprovement,
            'delayMinutes': @NextCycleDelayMinutes,
            'timestamp': FORMAT(SYSUTCDATETIME(), 'yyyy-MM-ddTHH:mm:ss.fffZ')
        );
        
        DECLARE @AnalyzeMessageBody XML = CAST(@AnalyseTrigger AS XML);
        
        -- Only restart if we have meaningful data
        IF @ExecutedActions > 0
        BEGIN
            -- Wait for the calculated delay before restarting
            WAITFOR DELAY @NextCycleDelayMinutes;
            
            BEGIN DIALOG CONVERSATION @AnalyzeConversationHandle
                FROM SERVICE LearnService
                TO SERVICE 'AnalyzeService'
                ON CONTRACT [//Hartonomous/AutonomousLoop/AnalyzeContract]
                WITH ENCRYPTION = OFF;
            
            SEND ON CONVERSATION @AnalyzeConversationHandle
                MESSAGE TYPE [//Hartonomous/AutonomousLoop/AnalyzeMessage]
                (@AnalyzeMessageBody);
        END
        
        -- 8. END ORIGINAL CONVERSATION
        END CONVERSATION @ConversationHandle;
        
        PRINT 'sp_Learn completed: ' + 
              CAST(@LatencyImprovement AS VARCHAR(20)) + '% latency improvement, ' +
              CAST(@SuccessfulActions AS VARCHAR(10)) + ' successful actions';
        PRINT 'Learning Outcomes: ' + @LearningOutcomes;
        PRINT 'Next OODA cycle starts in ' + CAST(@NextCycleDelayMinutes AS VARCHAR(10)) + ' minutes';
        
        RETURN 0;
    END TRY
    BEGIN CATCH
        DECLARE @ErrorMessage NVARCHAR(4000) = ERROR_MESSAGE();
        DECLARE @ErrorSeverity INT = ERROR_SEVERITY();
        DECLARE @ErrorState INT = ERROR_STATE();
        
        IF @ConversationHandle IS NOT NULL
        BEGIN
            END CONVERSATION @ConversationHandle WITH ERROR = 1 DESCRIPTION = @ErrorMessage;
        END
        
        PRINT 'sp_Learn ERROR: ' + @ErrorMessage;
        RAISERROR(@ErrorMessage, @ErrorSeverity, @ErrorState);
        RETURN -1;
    END CATCH
END;
GO
