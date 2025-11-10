-- sp_Act: Autonomous loop Phase 3 - Decision & Action
-- Receives hypotheses from sp_Hypothesize
-- Executes safe improvements automatically
-- Queues dangerous operations for approval
-- Sends LearnMessage to Service Broker for measurement phase

CREATE OR ALTER PROCEDURE dbo.sp_Act
    @TenantId INT = 0,
    @AutoApproveThreshold INT = 3 -- Auto-approve hypotheses with priority >= this value
AS
BEGIN
    SET NOCOUNT ON;





    BEGIN TRY
        -- 1. RECEIVE MESSAGE FROM HYPOTHESIZE PHASE
        WAITFOR (
            RECEIVE TOP(1)
                @ConversationHandle = conversation_handle,
                @MessageBody = CAST(message_body AS XML),
                @MessageTypeName = message_type_name
            FROM ActQueue
        ), TIMEOUT 5000; -- 5 second timeout
        
        IF @ConversationHandle IS NULL
        BEGIN
            RETURN 0;
        END
        
        -- 2. PARSE HYPOTHESES
        SET @HypothesesJson = CAST(@MessageBody AS NVARCHAR(MAX));


        -- 3. EXECUTE ACTIONS FOR EACH HYPOTHESIS

            HypothesisId UNIQUEIDENTIFIER,
            HypothesisType NVARCHAR(50),
            ActionStatus NVARCHAR(50),
            ExecutedActions NVARCHAR(MAX),
            ExecutionTimeMs INT,
            ErrorMessage NVARCHAR(MAX)
        );

            HypothesisId UNIQUEIDENTIFIER,
            HypothesisType NVARCHAR(50),
            Priority INT,
            Description NVARCHAR(MAX),
            RequiredActions NVARCHAR(MAX)
        );
        
        
        
        -- 4. PROCESS EACH HYPOTHESIS









        DECLARE hypothesis_cursor CURSOR FOR
        SELECT HypothesisId, HypothesisType, Priority, RequiredActions
        FROM @Hypotheses
        ORDER BY Priority;
        
        OPEN hypothesis_cursor;
        FETCH NEXT FROM hypothesis_cursor INTO @CurrentHypothesisId, @CurrentType, @CurrentPriority, @CurrentActions;
        
        -- GÖDEL ENGINE: Check for compute job messages first
        -- This is where the CLR compute function is invoked

        IF @PrimeSearchJobId IS NOT NULL
        BEGIN


            PRINT 'sp_Act: Executing prime search for range [' + CAST(@Start AS NVARCHAR(20)) + ', ' + CAST(@End AS NVARCHAR(20)) + ']';
            
            -- Call the CLR function to find primes in this chunk

            SET @ResultJson = dbo.clr_FindPrimes(@Start, @End);
            
            PRINT 'sp_Act: Found primes: ' + @ResultJson;
            
            -- Send results to Learn phase

                SELECT 
                    @PrimeSearchJobId AS JobId,
                    @End AS LastChecked,
                    @ResultJson AS PrimesFound
                FOR XML PATH('PrimeResult'), ROOT('Learn')
            );

            BEGIN DIALOG CONVERSATION @LearnHandle
                FROM SERVICE ActService
                TO SERVICE 'LearnService'
                ON CONTRACT [//Hartonomous/AutonomousLoop/LearnContract]
                WITH ENCRYPTION = OFF;
            
            SEND ON CONVERSATION @LearnHandle
                MESSAGE TYPE [//Hartonomous/AutonomousLoop/LearnMessage]
                (@LearnPayload);
            
            END CONVERSATION @ConversationHandle;
            
            RETURN 0;
        END
        
        -- REGULAR OODA LOOP: Process performance improvement hypotheses
        WHILE @@FETCH_STATUS = 0
        BEGIN
            SET @StartTime = SYSUTCDATETIME();
            SET @ActionStatus = 'Pending';
            SET @ExecutedActionsList = NULL;
            SET @ActionError = NULL;
            
            BEGIN TRY
                -- INDEX OPTIMIZATION: Safe, auto-approve
                IF @CurrentType = 'IndexOptimization'
                BEGIN
                    -- Analyze missing indexes

                        TableName NVARCHAR(256),
                        IndexColumns NVARCHAR(MAX),
                        IncludedColumns NVARCHAR(MAX),
                        ImpactScore FLOAT
                    );
                    
                    -- Get missing index recommendations
                    
                    
                    -- Update statistics on key tables
                    UPDATE STATISTICS dbo.Atoms WITH FULLSCAN;
                    UPDATE STATISTICS dbo.AtomEmbeddings WITH FULLSCAN;
                    UPDATE STATISTICS dbo.InferenceRequests WITH FULLSCAN;
                    
                    SET @ExecutedActionsList = (
                        SELECT TableName, IndexColumns, ImpactScore
                        FROM @MissingIndexes
                        FOR JSON PATH
                    );
                    SET @ActionStatus = 'Executed';
                END
                
                -- QUERY REGRESSION FIX: Force last good execution plan via Query Store
                ELSE IF @CurrentType = 'QueryRegression'
                BEGIN
                    -- Parse query regression recommendations from sp_Analyze observations


                    IF @RecommendationsJson IS NOT NULL
                    BEGIN



                        DECLARE plan_cursor CURSOR FOR
                        SELECT 
                            TRY_CAST(JSON_VALUE(value, '$.QueryId') AS BIGINT),
                            TRY_CAST(JSON_VALUE(value, '$.RecommendedPlanId') AS BIGINT),
                            JSON_VALUE(value, '$.ForceScript')
                        FROM OPENJSON(@RecommendationsJson);
                        
                        OPEN plan_cursor;
                        FETCH NEXT FROM plan_cursor INTO @QueryId, @RecommendedPlanId, @ForceScript;
                        
                        WHILE @@FETCH_STATUS = 0
                        BEGIN
                            BEGIN TRY
                                -- Force recommended plan via sp_query_store_force_plan
                                IF @QueryId IS NOT NULL AND @RecommendedPlanId IS NOT NULL
                                BEGIN
                                    EXEC sp_query_store_force_plan @QueryId, @RecommendedPlanId;
                                    SET @ForcedPlansCount = @ForcedPlansCount + 1;
                                END
                            END TRY
                            BEGIN CATCH
                                -- Log error but continue processing other plans
                                PRINT 'Failed to force plan ' + CAST(@RecommendedPlanId AS NVARCHAR(20)) + ' for query ' + CAST(@QueryId AS NVARCHAR(20)) + ': ' + ERROR_MESSAGE();
                            END CATCH
                            
                            FETCH NEXT FROM plan_cursor INTO @QueryId, @RecommendedPlanId, @ForceScript;
                        END
                        
                        CLOSE plan_cursor;
                        DEALLOCATE plan_cursor;
                    END
                    
                    SET @ExecutedActionsList = JSON_OBJECT('forcedPlans': @ForcedPlansCount);
                    SET @ActionStatus = 'Executed';
                END
                
                -- CACHE WARMING: Safe, auto-approve
                ELSE IF @CurrentType = 'CacheWarming'
                BEGIN
                    -- Preload frequent embeddings into buffer pool

                    SELECT TOP 1000 @PreloadedCount = COUNT(*)
                    FROM dbo.AtomEmbeddings WITH (NOLOCK)
                    WHERE LastAccessedUtc >= DATEADD(DAY, -7, SYSUTCDATETIME())
                    ORDER BY LastAccessedUtc DESC;
                    
                    SET @ExecutedActionsList = JSON_OBJECT('preloadedEmbeddings': @PreloadedCount);
                    SET @ActionStatus = 'Executed';
                END
                
                -- CONCEPT DISCOVERY: Safe, run clustering
                ELSE IF @CurrentType = 'ConceptDiscovery'
                BEGIN
                    -- Trigger concept discovery (placeholder - actual CLR function to be implemented)

                    -- For now, just detect clusters via spatial buckets
                    SELECT @DiscoveredConcepts = COUNT(DISTINCT SpatialBucket)
                    FROM dbo.AtomEmbeddings
                    WHERE CreatedUtc >= DATEADD(DAY, -7, SYSUTCDATETIME());
                    
                    SET @ExecutedActionsList = JSON_OBJECT('discoveredClusters': @DiscoveredConcepts);
                    SET @ActionStatus = 'Executed';
                END
                
                -- MODEL RETRAINING: DANGEROUS, queue for approval
                ELSE IF @CurrentType = 'ModelRetraining'
                BEGIN
                    -- 
                    SET @ActionStatus = 'QueuedForApproval';
                END
                
                ELSE
                BEGIN
                    SET @ExecutedActionsList = JSON_OBJECT('status': 'Skipped', 'reason': 'Unknown hypothesis type');
                    SET @ActionStatus = 'Skipped';
                END
                
            END TRY
            BEGIN CATCH
                SET @ActionError = ERROR_MESSAGE();
                SET @ActionStatus = 'Failed';
                SET @ExecutedActionsList = JSON_OBJECT('error': @ActionError);
            END CATCH
            
            SET @ExecutionTimeMs = DATEDIFF(MILLISECOND, @StartTime, SYSUTCDATETIME());
            
            
            
            FETCH NEXT FROM hypothesis_cursor INTO @CurrentHypothesisId, @CurrentType, @CurrentPriority, @CurrentActions;
        END
        
        CLOSE hypothesis_cursor;
        DEALLOCATE hypothesis_cursor;
        
        -- 5. COMPILE EXECUTION RESULTS
        SELECT @ExecutionResults = (
            SELECT 
                HypothesisId,
                HypothesisType,
                ActionStatus,
                JSON_QUERY(ExecutedActions) AS ExecutedActions,
                ExecutionTimeMs,
                ErrorMessage
            FROM @ActionResults
            FOR JSON PATH
        );

            'analysisId': @AnalysisId,
            'originalHypothesisPayload': JSON_QUERY(@HypothesesJson),
            'executedActions': (SELECT COUNT(*) FROM @ActionResults WHERE ActionStatus = 'Executed'),
            'queuedActions': (SELECT COUNT(*) FROM @ActionResults WHERE ActionStatus = 'QueuedForApproval'),
            'failedActions': (SELECT COUNT(*) FROM @ActionResults WHERE ActionStatus = 'Failed'),
            'results': JSON_QUERY(@ExecutionResults),
            'timestamp': FORMAT(SYSUTCDATETIME(), 'yyyy-MM-ddTHH:mm:ss.fffZ')
        );
        
        -- 6. SEND TO LEARN QUEUE


        BEGIN DIALOG CONVERSATION @LearnConversationHandle
            FROM SERVICE ActService
            TO SERVICE 'LearnService'
            ON CONTRACT [//Hartonomous/AutonomousLoop/LearnContract]
            WITH ENCRYPTION = OFF;
        
        SEND ON CONVERSATION @LearnConversationHandle
            MESSAGE TYPE [//Hartonomous/AutonomousLoop/LearnMessage]
            (@LearnMessageBody);
        
        -- 7. END ORIGINAL CONVERSATION
        END CONVERSATION @ConversationHandle;
        
        PRINT 'sp_Act completed: ' + CAST((SELECT COUNT(*) FROM @ActionResults WHERE ActionStatus = 'Executed') AS VARCHAR(10)) + ' actions executed';
        PRINT 'Execution Results: ' + @ExecutionResults;
        
        RETURN 0;
    END TRY
    BEGIN CATCH



        IF @ConversationHandle IS NOT NULL
        BEGIN
            END CONVERSATION @ConversationHandle WITH ERROR = 1 DESCRIPTION = @ErrorMessage;
        END
        
        PRINT 'sp_Act ERROR: ' + @ErrorMessage;
        RAISERROR(@ErrorMessage, @ErrorSeverity, @ErrorState);
        RETURN -1;
    END CATCH
END;
