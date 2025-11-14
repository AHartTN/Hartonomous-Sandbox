CREATE PROCEDURE dbo.sp_Hypothesize
    @TenantId INT = 0
AS
BEGIN
    SET NOCOUNT ON;
    
    DECLARE @ConversationHandle UNIQUEIDENTIFIER;
    DECLARE @MessageBody XML;
    DECLARE @MessageTypeName NVARCHAR(256);
    DECLARE @Observations NVARCHAR(MAX);
    DECLARE @Hypotheses NVARCHAR(MAX);
    
    BEGIN TRY
        -- 1. RECEIVE MESSAGE FROM ANALYZE PHASE
        WAITFOR (
            RECEIVE TOP(1)
                @ConversationHandle = conversation_handle,
                @MessageBody = CAST(message_body AS XML),
                @MessageTypeName = message_type_name
            FROM HypothesizeQueue
        ), TIMEOUT 5000; -- 5 second timeout
        
        IF @ConversationHandle IS NULL
        BEGIN
            PRINT 'sp_Hypothesize: No messages received';
            RETURN 0;
        END
        
        -- 2. PARSE OBSERVATIONS
        SET @Observations = CAST(@MessageBody AS NVARCHAR(MAX));
        
        DECLARE @AnalysisId UNIQUEIDENTIFIER = JSON_VALUE(@Observations, '$.analysisId');
        DECLARE @AnomalyCount INT = JSON_VALUE(@Observations, '$.anomalyCount');
        DECLARE @AvgDurationMs FLOAT = JSON_VALUE(@Observations, '$.avgDurationMs');
        
        -- 3. GENERATE HYPOTHESES BASED ON OBSERVATIONS
        DECLARE @HypothesisList TABLE (
            HypothesisId UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
            HypothesisType NVARCHAR(50),
            Priority INT,
            Description NVARCHAR(MAX),
            ExpectedImpact NVARCHAR(MAX),
            RequiredActions NVARCHAR(MAX)
        );

        -- GÖDEL ENGINE: Check for compute job messages first
        -- This allows the OODA loop to plan the next chunk of a long-running computational task
        DECLARE @ComputeJobId UNIQUEIDENTIFIER = @MessageBody.value('(/Hypothesis/ComputeJob/JobId)[1]', 'uniqueidentifier');
        
        IF @ComputeJobId IS NOT NULL
        BEGIN
            PRINT 'sp_Hypothesize: Processing compute job: ' + CAST(@ComputeJobId AS NVARCHAR(36));
            
            DECLARE @JobParams NVARCHAR(MAX);
            DECLARE @CurrentState NVARCHAR(MAX);
            DECLARE @JobType NVARCHAR(100);
            
            SELECT 
                @JobParams = CAST(JobParameters AS NVARCHAR(MAX)),
                @CurrentState = CAST(CurrentState AS NVARCHAR(MAX)),
                @JobType = JobType
            FROM dbo.AutonomousComputeJobs
            WHERE JobId = @ComputeJobId AND Status = 'Running';
            
            IF @JobParams IS NULL
            BEGIN
                PRINT 'sp_Hypothesize: Job not found or already completed.';
                END CONVERSATION @ConversationHandle;
                RETURN 0;
            END
            
            -- Job type: PrimeSearch
            IF @JobType = 'PrimeSearch'
            BEGIN
                DECLARE @RangeEnd BIGINT = JSON_VALUE(@JobParams, '$.rangeEnd');
                DECLARE @LastChecked BIGINT = ISNULL(JSON_VALUE(@CurrentState, '$.lastChecked'), JSON_VALUE(@JobParams, '$.rangeStart') - 1);
                DECLARE @ChunkSize INT = 10000; -- Process 10k numbers per chunk
                
                IF @LastChecked >= @RangeEnd
                BEGIN
                    -- Job complete
                    UPDATE dbo.AutonomousComputeJobs
                    SET Status = 'Completed',
                        CompletedAt = SYSUTCDATETIME(),
                        CreatedAt = SYSUTCDATETIME()
                    WHERE JobId = @ComputeJobId;
                    
                    PRINT 'sp_Hypothesize: Job completed.';
                    END CONVERSATION @ConversationHandle;
                    RETURN 0;
                END
                
                -- Plan next chunk
                DECLARE @NextStart BIGINT = @LastChecked + 1;
                DECLARE @NextEnd BIGINT = @LastChecked + @ChunkSize;
                IF @NextEnd > @RangeEnd SET @NextEnd = @RangeEnd;
                
                DECLARE @ActPayload XML = (
                    SELECT 
                        @ComputeJobId AS JobId,
                        @NextStart AS RangeStart,
                        @NextEnd AS RangeEnd
                    FOR XML PATH('PrimeSearch'), ROOT('Action')
                );
                
                DECLARE @ActHandle UNIQUEIDENTIFIER;
                
                BEGIN DIALOG CONVERSATION @ActHandle
                    FROM SERVICE HypothesizeService
                    TO SERVICE 'ActService'
                    ON CONTRACT [//Hartonomous/AutonomousLoop/ActContract]
                    WITH ENCRYPTION = OFF;
                
                SEND ON CONVERSATION @ActHandle
                    MESSAGE TYPE [//Hartonomous/AutonomousLoop/ActMessage]
                    (@ActPayload);
                
                END CONVERSATION @ConversationHandle;
                
                PRINT 'sp_Hypothesize: Planned chunk [' + CAST(@NextStart AS NVARCHAR(20)) + ', ' + CAST(@NextEnd AS NVARCHAR(20)) + ']';
                RETURN 0;
            END
        END
        
        -- REGULAR OODA LOOP: Generate hypotheses based on performance observations
        DECLARE @JobTypeOld NVARCHAR(100) = JSON_VALUE(@Observations, '$.jobType');

        IF @JobTypeOld IS NOT NULL AND @JobTypeOld != ''
        BEGIN
            -- Old-style job messages (for backward compatibility with sp_StartPrimeSearch)
            -- These will be migrated to use AutonomousComputeJobs table
            PRINT 'sp_Hypothesize: Received old-style job message. Consider migrating to AutonomousComputeJobs.';
        END
        
        -- HYPOTHESIS GENERATION: Analyze system performance and suggest improvements
        -- HYPOTHESIS 1: If anomalies detected, suggest index optimization
            IF @AnomalyCount > 5
            BEGIN
                INSERT INTO @HypothesisList (HypothesisType, Priority, Description, ExpectedImpact, RequiredActions)
                VALUES (
                    'IndexOptimization',
                    1,
                    'High number of performance anomalies detected. Missing indexes may be causing slow queries.',
                    '{"latencyReduction": "50-70%", "throughputIncrease": "30-50%"}',
                    '["AnalyzeMissingIndexes", "CreateRecommendedIndexes", "UpdateStatistics"]'
                );
            END
        
            -- HYPOTHESIS 2: If average duration increasing, suggest cache warming
            IF @AvgDurationMs > 1000
            BEGIN
                INSERT INTO @HypothesisList (HypothesisType, Priority, Description, ExpectedImpact, RequiredActions)
                VALUES (
                    'CacheWarming',
                    2,
                    'Average inference duration exceeds 1 second. Cache warming could reduce cold-start latency.',
                    '{"latencyReduction": "30-40%", "cacheHitRate": "60-80%"}',
                    '["PreloadFrequentEmbeddings", "EnableInMemoryOLTP", "OptimizeBufferPool"]'
                );
            END
        
            -- HYPOTHESIS 3: If embedding clusters detected, suggest concept discovery
            DECLARE @PatternCount INT = (
                SELECT COUNT(*) 
                FROM OPENJSON(@Observations, '$.patterns')
            );
        
            IF @PatternCount > 3
            BEGIN
                INSERT INTO @HypothesisList (HypothesisType, Priority, Description, ExpectedImpact, RequiredActions)
                VALUES (
                    'ConceptDiscovery',
                    3,
                    'Embedding clustering patterns detected. Unsupervised concept learning may reveal emergent semantics.',
                    '{"newConceptsExpected": ' + CAST(@PatternCount AS NVARCHAR(10)) + ', "accuracyImprovement": "5-15%"}',
                    '["RunClusterAnalysis", "ExtractConceptEmbeddings", "BindConceptsToAtoms"]'
                );
            END
        
            -- HYPOTHESIS 4: Model retraining if drift detected
            DECLARE @InferenceCount INT = JSON_VALUE(@Observations, '$.totalInferences');
            IF @InferenceCount > 10000
            BEGIN
                INSERT INTO @HypothesisList (HypothesisType, Priority, Description, ExpectedImpact, RequiredActions)
                VALUES (
                    'ModelRetraining',
                    4,
                    'High inference volume detected. Incremental model retraining may capture recent patterns.',
                    '{"accuracyImprovement": "3-8%", "recentDataCoverage": "95%"}',
                    '["CollectRecentFeedback", "IncrementalRetrain", "ValidateModelDrift"]'
                );
            END

            -- HYPOTHESIS 5: Prune model based on low-importance tensor atoms
            DECLARE @PruneThreshold FLOAT = 0.01; -- Define a threshold for low importance
            DECLARE @PruneableAtoms NVARCHAR(MAX) = (
                SELECT ta.TensorAtomId, tac.Coefficient
                FROM dbo.TensorAtoms ta
                JOIN dbo.TensorAtomCoefficients tac ON ta.TensorAtomId = tac.TensorAtomId
                WHERE tac.Coefficient < @PruneThreshold
                FOR JSON PATH
            );

            IF @PruneableAtoms IS NOT NULL AND @PruneableAtoms <> '[]'
            BEGIN
                INSERT INTO @HypothesisList (HypothesisType, Priority, Description, ExpectedImpact, RequiredActions)
                VALUES (
                    'PruneModel',
                    5,
                    'Identified tensor atoms with low importance coefficients. Pruning these may reduce model size and improve performance.',
                    '{"modelSizeReduction": "5-10%", "inferenceSpeedup": "3-5%"}',
                    @PruneableAtoms
                );
            END

            -- HYPOTHESIS 6: Refactor code based on duplicate AST signatures
            DECLARE @DuplicateCodeAtoms NVARCHAR(MAX) = (
                SELECT TOP 10 SpatialSignature.ToString() AS Signature, COUNT(*) AS DuplicateCount
                FROM dbo.CodeAtoms
                GROUP BY SpatialSignature.ToString()
                HAVING COUNT(*) > 1
                ORDER BY COUNT(*) DESC
                FOR JSON PATH
            );

            IF @DuplicateCodeAtoms IS NOT NULL AND @DuplicateCodeAtoms <> '[]'
            BEGIN
                INSERT INTO @HypothesisList (HypothesisType, Priority, Description, ExpectedImpact, RequiredActions)
                VALUES (
                    'RefactorCode',
                    6,
                    'Detected multiple code blocks with identical abstract syntax tree (AST) signatures, indicating duplicated logic.',
                    '{"codebaseReduction": "1-2%", "maintainabilityIncrease": "medium"}',
                    @DuplicateCodeAtoms
                );
            END

            -- HYPOTHESIS 7: Fix UX based on sessions ending in an error state
            -- This assumes an @ErrorRegion GEOMETRY variable is defined, representing error states.
            DECLARE @ErrorRegion GEOMETRY = geometry::Point(0, 0, 0).STBuffer(10); -- Placeholder for error region
            DECLARE @FailingSessions NVARCHAR(MAX) = (
                SELECT TOP 10 SessionId, Path.STEndPoint().ToString() AS EndPoint
                FROM dbo.SessionPaths
                WHERE Path.STEndPoint().STIntersects(@ErrorRegion) = 1
                FOR JSON PATH
            );

            IF @FailingSessions IS NOT NULL AND @FailingSessions <> '[]'
            BEGIN
                INSERT INTO @HypothesisList (HypothesisType, Priority, Description, ExpectedImpact, RequiredActions)
                VALUES (
                    'FixUX',
                    7,
                    'Detected user sessions terminating in a known error region of the state space.',
                    '{"userErrorRateReduction": "10-20%", "sessionCompletionIncrease": "5%"}',
                    @FailingSessions
                );
            END
        
        
        -- 4. PERSIST HYPOTHESES AS PENDING ACTIONS
        -- This enables the Act phase to execute them in priority order
        INSERT INTO dbo.PendingActions (ActionType, Parameters, Priority)
        SELECT 
            HypothesisType,
            JSON_OBJECT(
                'description': Description,
                'expectedImpact': JSON_QUERY(ExpectedImpact),
                'requiredActions': JSON_QUERY(RequiredActions)
            ),
            Priority
        FROM @HypothesisList
        WHERE NOT EXISTS (
            -- Avoid duplicate actions
            SELECT 1 FROM dbo.PendingActions pa
            WHERE pa.ActionType = [@HypothesisList].HypothesisType
              AND pa.Status = 'Pending'
        );
        
        -- 5. COMPILE HYPOTHESES FOR MESSAGING
        SELECT @Hypotheses = (
            SELECT 
                HypothesisId,
                HypothesisType,
                Priority,
                Description,
                JSON_QUERY(ExpectedImpact) AS ExpectedImpact,
                JSON_QUERY(RequiredActions) AS RequiredActions
            FROM @HypothesisList
            ORDER BY Priority
            FOR JSON PATH
        );
        
        DECLARE @HypothesisCount INT = (SELECT COUNT(*) FROM @HypothesisList);
        
        DECLARE @HypothesisPayload NVARCHAR(MAX) = JSON_OBJECT(
            'analysisId': @AnalysisId,
            'hypothesesGenerated': @HypothesisCount,
            'hypotheses': JSON_QUERY(@Hypotheses),
            'timestamp': FORMAT(SYSUTCDATETIME(), 'yyyy-MM-ddTHH:mm:ss.fffZ')
        );
        
        -- 6. SEND TO ACT QUEUE
        DECLARE @ActConversationHandle UNIQUEIDENTIFIER;
        DECLARE @ActMessageBody XML = CAST(@HypothesisPayload AS XML);
        
        BEGIN DIALOG CONVERSATION @ActConversationHandle
            FROM SERVICE HypothesizeService
            TO SERVICE 'ActService'
            ON CONTRACT [//Hartonomous/AutonomousLoop/ActContract]
            WITH ENCRYPTION = OFF;
        
        SEND ON CONVERSATION @ActConversationHandle
            MESSAGE TYPE [//Hartonomous/AutonomousLoop/ActMessage]
            (@ActMessageBody);
        
        -- 7. END ORIGINAL CONVERSATION
        END CONVERSATION @ConversationHandle;
        
        PRINT 'sp_Hypothesize completed: ' + CAST(@HypothesisCount AS VARCHAR(10)) + ' hypotheses generated';
        PRINT 'Hypotheses: ' + @Hypotheses;
        
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
        
        PRINT 'sp_Hypothesize ERROR: ' + @ErrorMessage;
        RAISERROR(@ErrorMessage, @ErrorSeverity, @ErrorState);
        RETURN -1;
    END CATCH
END;
GO