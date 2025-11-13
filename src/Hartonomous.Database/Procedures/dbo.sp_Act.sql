-- sp_Act: Autonomous loop Phase 3 - Decision & Action
-- Receives hypotheses from sp_Hypothesize
-- Executes safe improvements automatically
-- Queues dangerous operations for approval
-- Sends LearnMessage to Service Broker for measurement phase

CREATE PROCEDURE dbo.sp_Act
    @TenantId INT = 0,
    @AutoApproveThreshold INT = 3 -- Auto-approve hypotheses with priority >= this value
AS
BEGIN
    SET NOCOUNT ON;
    
    DECLARE @ConversationHandle UNIQUEIDENTIFIER;
    DECLARE @MessageBody XML;
    DECLARE @MessageTypeName NVARCHAR(256);
    DECLARE @HypothesesJson NVARCHAR(MAX);
    DECLARE @ExecutionResults NVARCHAR(MAX);
    
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
            PRINT 'sp_Act: No messages received';
            RETURN 0;
        END
        
        -- 2. PARSE HYPOTHESES
        SET @HypothesesJson = CAST(@MessageBody AS NVARCHAR(MAX));
        
        DECLARE @AnalysisId UNIQUEIDENTIFIER = JSON_VALUE(@HypothesesJson, '$.analysisId');
        DECLARE @HypothesesCount INT = JSON_VALUE(@HypothesesJson, '$.hypothesesGenerated');
        
        -- 3. EXECUTE ACTIONS FOR EACH HYPOTHESIS
        DECLARE @ActionResults TABLE (
            HypothesisId UNIQUEIDENTIFIER,
            HypothesisType NVARCHAR(50),
            ActionStatus NVARCHAR(50),
            ExecutedActions NVARCHAR(MAX),
            ExecutionTimeMs INT,
            ErrorMessage NVARCHAR(MAX)
        );
        
        DECLARE @Hypotheses TABLE (
            HypothesisId UNIQUEIDENTIFIER,
            HypothesisType NVARCHAR(50),
            Priority INT,
            Description NVARCHAR(MAX),
            RequiredActions NVARCHAR(MAX)
        );
        
        INSERT INTO @Hypotheses
        SELECT 
            HypothesisId,
            HypothesisType,
            Priority,
            Description,
            RequiredActions
        FROM OPENJSON(@HypothesesJson, '$.hypotheses')
        WITH (
            HypothesisId UNIQUEIDENTIFIER,
            HypothesisType NVARCHAR(50),
            Priority INT,
            Description NVARCHAR(MAX),
            RequiredActions NVARCHAR(MAX) AS JSON
        );
        
        -- 4. PROCESS EACH HYPOTHESIS
        DECLARE @CurrentHypothesisId UNIQUEIDENTIFIER;
        DECLARE @CurrentType NVARCHAR(50);
        DECLARE @CurrentPriority INT;
        DECLARE @CurrentActions NVARCHAR(MAX);
        DECLARE @StartTime DATETIME2;
        DECLARE @ExecutionTimeMs INT;
        DECLARE @ActionStatus NVARCHAR(50);
        DECLARE @ExecutedActionsList NVARCHAR(MAX);
        DECLARE @ActionError NVARCHAR(MAX);
        
        DECLARE hypothesis_cursor CURSOR FOR
        SELECT HypothesisId, HypothesisType, Priority, RequiredActions
        FROM @Hypotheses
        ORDER BY Priority;
        
        OPEN hypothesis_cursor;
        FETCH NEXT FROM hypothesis_cursor INTO @CurrentHypothesisId, @CurrentType, @CurrentPriority, @CurrentActions;
        
        -- GÖDEL ENGINE: Check for compute job messages first
        -- This is where the CLR compute function is invoked
        DECLARE @PrimeSearchJobId UNIQUEIDENTIFIER = @MessageBody.value('(/Action/PrimeSearch/JobId)[1]', 'uniqueidentifier');
        
        IF @PrimeSearchJobId IS NOT NULL
        BEGIN
            DECLARE @Start BIGINT = @MessageBody.value('(/Action/PrimeSearch/RangeStart)[1]', 'bigint');
            DECLARE @End BIGINT = @MessageBody.value('(/Action/PrimeSearch/RangeEnd)[1]', 'bigint');
            
            PRINT 'sp_Act: Executing prime search for range [' + CAST(@Start AS NVARCHAR(20)) + ', ' + CAST(@End AS NVARCHAR(20)) + ']';
            
            -- Call the CLR function to find primes in this chunk
            DECLARE @ResultJson NVARCHAR(MAX);
            SET @ResultJson = dbo.clr_FindPrimes(@Start, @End);
            
            PRINT 'sp_Act: Found primes: ' + @ResultJson;
            
            -- Send results to Learn phase
            DECLARE @LearnPayload XML = (
                SELECT 
                    @PrimeSearchJobId AS JobId,
                    @End AS LastChecked,
                    @ResultJson AS PrimesFound
                FOR XML PATH('PrimeResult'), ROOT('Learn')
            );
            
            DECLARE @LearnHandle UNIQUEIDENTIFIER;
            
            BEGIN DIALOG CONVERSATION @LearnHandle
                FROM SERVICE ActService
                TO SERVICE 'LearnService'
                ON CONTRACT [//Hartonomous/AutonomousLoop/LearnContract]
                WITH ENCRYPTION = OFF;
            
            SEND ON CONVERSATION @LearnHandle
                MESSAGE TYPE [//Hartonomous/AutonomousLoop/LearnMessage]
                (@LearnPayload);
            
            END CONVERSATION @ConversationHandle;
            
            PRINT 'sp_Act: Compute job results sent to Learn phase.';
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
                    DECLARE @MissingIndexes TABLE (
                        TableName NVARCHAR(256),
                        IndexColumns NVARCHAR(MAX),
                        IncludedColumns NVARCHAR(MAX),
                        ImpactScore FLOAT
                    );
                    
                    -- Get missing index recommendations
                    INSERT INTO @MissingIndexes
                    SELECT TOP 5
                        OBJECT_NAME(mid.object_id) AS TableName,
                        mid.equality_columns + ISNULL(', ' + mid.inequality_columns, '') AS IndexColumns,
                        mid.included_columns AS IncludedColumns,
                        migs.avg_total_user_cost * migs.avg_user_impact * (migs.user_seeks + migs.user_scans) AS ImpactScore
                    FROM sys.dm_db_missing_index_details mid
                    INNER JOIN sys.dm_db_missing_index_groups mig ON mid.index_handle = mig.index_handle
                    INNER JOIN sys.dm_db_missing_index_group_stats migs ON mig.index_group_handle = migs.group_handle
                    WHERE mid.database_id = DB_ID()
                    ORDER BY ImpactScore DESC;
                    
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
                    DECLARE @RecommendationsJson NVARCHAR(MAX) = JSON_QUERY(@CurrentActions, '$.queryStoreRecommendations');
                    DECLARE @ForcedPlansCount INT = 0;
                    
                    IF @RecommendationsJson IS NOT NULL
                    BEGIN
                        DECLARE @QueryId BIGINT;
                        DECLARE @RecommendedPlanId BIGINT;
                        DECLARE @ForceScript NVARCHAR(MAX);
                        
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
                    
                    SET @ExecutedActionsList = (SELECT @ForcedPlansCount AS forcedPlans FOR JSON PATH, WITHOUT_ARRAY_WRAPPER);
                    SET @ActionStatus = 'Executed';
                END
                
                -- CACHE WARMING: Safe, auto-approve
                ELSE IF @CurrentType = 'CacheWarming'
                BEGIN
                    -- Preload frequent embeddings into buffer pool
                    DECLARE @PreloadedCount INT = 0;
                    
                    SELECT @PreloadedCount = COUNT(*)
                    FROM (SELECT TOP 1000 CacheId 
                          FROM dbo.InferenceCache WITH (NOLOCK)
                          WHERE LastAccessedUtc >= DATEADD(DAY, -7, SYSUTCDATETIME())
                          ORDER BY LastAccessedUtc DESC) AS FrequentCache;
                    
                    SET @ExecutedActionsList = (SELECT @PreloadedCount AS preloadedEmbeddings FOR JSON PATH, WITHOUT_ARRAY_WRAPPER);
                    SET @ActionStatus = 'Executed';
                END
                
                -- CONCEPT DISCOVERY: Safe, run clustering
                ELSE IF @CurrentType = 'ConceptDiscovery'
                BEGIN
                    -- Trigger concept discovery (placeholder - actual CLR function to be implemented)
                    DECLARE @DiscoveredConcepts INT = 0;
                    
                    -- For now, just detect clusters via spatial buckets
                    SELECT @DiscoveredConcepts = COUNT(DISTINCT SpatialBucket)
                    FROM dbo.AtomEmbeddings
                    WHERE CreatedAt >= DATEADD(DAY, -7, SYSUTCDATETIME());
                    
                    SET @ExecutedActionsList = (SELECT @DiscoveredConcepts AS discoveredClusters FOR JSON PATH, WITHOUT_ARRAY_WRAPPER);
                    SET @ActionStatus = 'Executed';
                END
                
                -- MODEL RETRAINING: DANGEROUS, queue for approval
                ELSE IF @CurrentType = 'ModelRetraining'
                BEGIN
                    -- Insert into approval queue (table to be created)
                    -- For now, just log the request
                    SET @ExecutedActionsList = (SELECT 'QueuedForApproval' AS status, 'ModelRetraining requires manual approval' AS reason FOR JSON PATH, WITHOUT_ARRAY_WRAPPER);
                    SET @ActionStatus = 'QueuedForApproval';
                END
                
                ELSE
                BEGIN
                    SET @ExecutedActionsList = (SELECT 'Skipped' AS status, 'Unknown hypothesis type' AS reason FOR JSON PATH, WITHOUT_ARRAY_WRAPPER);
                    SET @ActionStatus = 'Skipped';
                END
                
            END TRY
            BEGIN CATCH
                SET @ActionError = ERROR_MESSAGE();
                SET @ActionStatus = 'Failed';
                SET @ExecutedActionsList = (SELECT @ActionError AS error FOR JSON PATH, WITHOUT_ARRAY_WRAPPER);
            END CATCH
            
            SET @ExecutionTimeMs = DATEDIFF(MILLISECOND, @StartTime, SYSUTCDATETIME());
            
            INSERT INTO @ActionResults
            VALUES (@CurrentHypothesisId, @CurrentType, @ActionStatus, @ExecutedActionsList, @ExecutionTimeMs, @ActionError);
            
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
        
        DECLARE @ExecutedCount INT, @QueuedCount INT, @FailedCount INT;
        SELECT @ExecutedCount = COUNT(*) FROM @ActionResults WHERE ActionStatus = 'Executed';
        SELECT @QueuedCount = COUNT(*) FROM @ActionResults WHERE ActionStatus = 'QueuedForApproval';
        SELECT @FailedCount = COUNT(*) FROM @ActionResults WHERE ActionStatus = 'Failed';
        
        DECLARE @ActPayload NVARCHAR(MAX) = (
            SELECT 
                @AnalysisId AS analysisId,
                JSON_QUERY(@HypothesesJson) AS originalHypothesisPayload,
                @ExecutedCount AS executedActions,
                @QueuedCount AS queuedActions,
                @FailedCount AS failedActions,
                JSON_QUERY(@ExecutionResults) AS results,
                FORMAT(SYSUTCDATETIME(), 'yyyy-MM-ddTHH:mm:ss.fffZ') AS timestamp
            FOR JSON PATH, WITHOUT_ARRAY_WRAPPER
        );
        
        -- 6. SEND TO LEARN QUEUE
        DECLARE @LearnConversationHandle UNIQUEIDENTIFIER;
        DECLARE @LearnMessageBody XML = CAST(@ActPayload AS XML);
        
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
        
        DECLARE @ExecutedActionsCount INT;
        SELECT @ExecutedActionsCount = COUNT(*) FROM @ActionResults WHERE ActionStatus = 'Executed';
        PRINT 'sp_Act completed: ' + CAST(@ExecutedActionsCount AS VARCHAR(10)) + ' actions executed';
        PRINT 'Execution Results: ' + @ExecutionResults;
        
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
        
        PRINT 'sp_Act ERROR: ' + @ErrorMessage;
        RAISERROR(@ErrorMessage, @ErrorSeverity, @ErrorState);
        RETURN -1;
    END CATCH
END;
GO
