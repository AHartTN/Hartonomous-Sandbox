-- sp_Learn: Autonomous loop Phase 4 - Learn & Measure
-- Receives results from sp_Act
-- Measures improvement effectiveness
-- Updates learning metrics
-- Completes OODA loop cycle

CREATE PROCEDURE dbo.sp_Learn
AS
BEGIN
    SET NOCOUNT ON;
    
    -- Service Broker activation procedures cannot have parameters (Msg 9653)
    -- Operates autonomously on messages from ActQueue
    
    DECLARE @ConversationHandle UNIQUEIDENTIFIER;
    DECLARE @MessageBody XML;
    DECLARE @MessageTypeName NVARCHAR(256);
    DECLARE @ResultsJson NVARCHAR(MAX);
    
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
        
        -- 2. PARSE RESULTS
        SET @ResultsJson = CAST(@MessageBody AS NVARCHAR(MAX));
        
        DECLARE @AnalysisId UNIQUEIDENTIFIER = JSON_VALUE(@ResultsJson, '$.analysisId');
        DECLARE @ExecutedCount INT = JSON_VALUE(@ResultsJson, '$.executedActions');
        DECLARE @QueuedCount INT = JSON_VALUE(@ResultsJson, '$.queuedActions');
        DECLARE @FailedCount INT = JSON_VALUE(@ResultsJson, '$.failedActions');
        
        -- GÖDEL ENGINE: Check for compute job results first
        DECLARE @ComputeJobId UNIQUEIDENTIFIER = @MessageBody.value('(/Learn/PrimeResult/JobId)[1]', 'uniqueidentifier');
        
        IF @ComputeJobId IS NOT NULL
        BEGIN
            DECLARE @PrimesJson NVARCHAR(MAX) = @MessageBody.value('(/Learn/PrimeResult/PrimesFound)[1]', 'nvarchar(max)');
            DECLARE @PrimeCount INT = JSON_VALUE(@PrimesJson, '$.count');
            
            PRINT 'sp_Learn: Processing compute job results: ' + CAST(@ComputeJobId AS NVARCHAR(36));
            PRINT '  Primes found in chunk: ' + CAST(@PrimeCount AS NVARCHAR(10));
            
            -- Update job state with results
            DECLARE @CurrentState NVARCHAR(MAX);
            SELECT @CurrentState = CAST(CurrentState AS NVARCHAR(MAX))
            FROM dbo.AutonomousComputeJobs
            WHERE JobId = @ComputeJobId;
            
            IF @CurrentState IS NULL
                SET @CurrentState = '{}';
            
            DECLARE @LastChecked BIGINT = JSON_VALUE(@PrimesJson, '$.rangeEnd');
            DECLARE @TotalFound INT = ISNULL(JSON_VALUE(@CurrentState, '$.totalPrimes'), 0) + @PrimeCount;
            
            -- Append primes to results
            DECLARE @ExistingPrimes NVARCHAR(MAX) = JSON_QUERY(@CurrentState, '$.primes');
            IF @ExistingPrimes IS NULL
                SET @ExistingPrimes = '[]';
            
            DECLARE @NewPrimes NVARCHAR(MAX) = JSON_QUERY(@PrimesJson, '$.primes');
            
            -- Merge prime arrays (simplified - in production would use proper JSON array manipulation)
            DECLARE @UpdatedState NVARCHAR(MAX) = JSON_MODIFY(
                JSON_MODIFY(
                    JSON_MODIFY(@CurrentState, '$.lastChecked', @LastChecked),
                    '$.totalPrimes', @TotalFound
                ),
                '$.primes', JSON_QUERY(@NewPrimes)
            );
            
            UPDATE dbo.AutonomousComputeJobs
            SET CurrentState = CAST(@UpdatedState AS NVARCHAR(MAX)),
                LastHeartbeat = SYSUTCDATETIME(),
                UpdatedAt = SYSUTCDATETIME()
            WHERE JobId = @ComputeJobId;
            
            PRINT '  Updated job state: lastChecked=' + CAST(@LastChecked AS NVARCHAR(20)) + ', totalPrimes=' + CAST(@TotalFound AS NVARCHAR(10));
            
            -- Send message back to Hypothesize to plan next chunk
            DECLARE @HypothesizePayload XML = (
                SELECT 
                    @ComputeJobId AS JobId
                FOR XML PATH('ComputeJob'), ROOT('Hypothesis')
            );
            
            DECLARE @HypothesizeHandle UNIQUEIDENTIFIER;
            
            BEGIN DIALOG CONVERSATION @HypothesizeHandle
                FROM SERVICE LearnService
                TO SERVICE 'HypothesizeService'
                ON CONTRACT [//Hartonomous/AutonomousLoop/HypothesizeContract]
                WITH ENCRYPTION = OFF;
            
            SEND ON CONVERSATION @HypothesizeHandle
                MESSAGE TYPE [//Hartonomous/AutonomousLoop/HypothesizeMessage]
                (@HypothesizePayload);
            
            END CONVERSATION @ConversationHandle;
            
            PRINT 'sp_Learn: Sent continuation message to Hypothesize phase';
            RETURN 0;
        END
        
        -- REGULAR OODA LOOP: Measure effectiveness of runtime improvements
        PRINT 'sp_Learn: Processing OODA loop results...';
        PRINT '  Executed: ' + CAST(@ExecutedCount AS NVARCHAR(10));
        PRINT '  Queued: ' + CAST(@QueuedCount AS NVARCHAR(10));
        PRINT '  Failed: ' + CAST(@FailedCount AS NVARCHAR(10));
        
        -- 3. MEASURE IMPROVEMENT EFFECTIVENESS
        -- Compare metrics before/after actions were executed
        -- This requires querying AutonomousImprovementHistory with @AnalysisId
        
        DECLARE @SuccessRate DECIMAL(5,2);
        
        SELECT @SuccessRate = 
            CASE 
                WHEN (@ExecutedCount + @FailedCount) > 0 
                THEN (CAST(@ExecutedCount AS FLOAT) / (@ExecutedCount + @FailedCount)) * 100
                ELSE 0
            END;
        
        PRINT '  Success Rate: ' + CAST(@SuccessRate AS NVARCHAR(10)) + '%';
        
        -- 4. UPDATE LEARNING METRICS
        -- In future: Update reinforcement learning weights based on success/failure
        -- In future: Adjust hypothesis generation parameters
        -- In future: Feed results back to LLM for meta-learning
        
        -- For now, just log completion
        INSERT INTO dbo.LearningMetrics (
            AnalysisId,
            MetricType,
            MetricValue,
            MeasuredAt
        )
        VALUES 
            (@AnalysisId, 'SuccessRate', @SuccessRate, SYSUTCDATETIME()),
            (@AnalysisId, 'ActionsExecuted', @ExecutedCount, SYSUTCDATETIME()),
            (@AnalysisId, 'ActionsQueued', @QueuedCount, SYSUTCDATETIME()),
            (@AnalysisId, 'ActionsFailed', @FailedCount, SYSUTCDATETIME());
        
        -- 5. RLHF FEEDBACK LOOP: Process accumulated feedback
        -- This is the critical connection between user feedback and system learning
        DECLARE @PendingFeedbackCount INT;

        SELECT @PendingFeedbackCount = COUNT(*)
        FROM dbo.InferenceFeedback f
        WHERE f.FeedbackTimestamp >= DATEADD(HOUR, -1, SYSUTCDATETIME())
          AND NOT EXISTS (
              -- Check if feedback was already processed
              SELECT 1 FROM dbo.AutonomousImprovementHistory aih
              WHERE aih.TargetEntity = 'AtomRelation'
                AND aih.ImprovementType = 'FeedbackWeightAdjustment'
                AND aih.TargetId = f.InferenceRequestId
          );

        IF @PendingFeedbackCount >= 5 -- Batch threshold
        BEGIN
            PRINT 'sp_Learn: Triggering weight adjustments for ' + CAST(@PendingFeedbackCount AS NVARCHAR(10)) + ' feedback items';
            
            -- Process feedback in batch
            DECLARE @FeedbackInferenceId BIGINT, @FeedbackRating INT, @FeedbackComments NVARCHAR(2000), @FeedbackUserId NVARCHAR(128);
            
            DECLARE feedback_cursor CURSOR LOCAL FAST_FORWARD FOR
                SELECT f.InferenceRequestId, f.Rating, f.Comments, f.UserId
                FROM dbo.InferenceFeedback f
                WHERE f.FeedbackTimestamp >= DATEADD(HOUR, -1, SYSUTCDATETIME())
                  AND NOT EXISTS (
                      SELECT 1 FROM dbo.AutonomousImprovementHistory aih
                      WHERE aih.TargetEntity = 'AtomRelation'
                        AND aih.ImprovementType = 'FeedbackWeightAdjustment'
                        AND aih.TargetId = f.InferenceRequestId
                  );
            
            OPEN feedback_cursor;
            FETCH NEXT FROM feedback_cursor INTO @FeedbackInferenceId, @FeedbackRating, @FeedbackComments, @FeedbackUserId;
            
            WHILE @@FETCH_STATUS = 0
            BEGIN
                -- Call sp_ProcessFeedback for each pending feedback
                BEGIN TRY
                    EXEC dbo.sp_ProcessFeedback 
                        @InferenceId = @FeedbackInferenceId,
                        @Rating = @FeedbackRating,
                        @Comments = @FeedbackComments,
                        @UserId = @FeedbackUserId;
                END TRY
                BEGIN CATCH
                    PRINT 'sp_Learn: Error processing feedback for inference ' + CAST(@FeedbackInferenceId AS NVARCHAR(20)) + ': ' + ERROR_MESSAGE();
                END CATCH;
                
                FETCH NEXT FROM feedback_cursor INTO @FeedbackInferenceId, @FeedbackRating, @FeedbackComments, @FeedbackUserId;
            END;
            
            CLOSE feedback_cursor;
            DEALLOCATE feedback_cursor;
            
            PRINT 'sp_Learn: Weight adjustments completed';
            
            -- Track feedback processing metrics
            INSERT INTO dbo.LearningMetrics (
                AnalysisId,
                MetricType,
                MetricValue,
                MeasuredAt
            )
            VALUES 
                (@AnalysisId, 'FeedbackProcessed', @PendingFeedbackCount, SYSUTCDATETIME());
        END
        ELSE IF @PendingFeedbackCount > 0
        BEGIN
            PRINT 'sp_Learn: ' + CAST(@PendingFeedbackCount AS NVARCHAR(10)) + ' feedback items pending (threshold: 5)';
        END;
        
        -- 6. END CONVERSATION
        END CONVERSATION @ConversationHandle;
        
        PRINT 'sp_Learn completed: OODA loop cycle finished';
        PRINT '  AnalysisId: ' + CAST(@AnalysisId AS NVARCHAR(36));
        PRINT '  Success Rate: ' + CAST(@SuccessRate AS NVARCHAR(10)) + '%';
        
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
