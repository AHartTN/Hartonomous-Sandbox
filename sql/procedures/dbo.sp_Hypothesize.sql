-- sp_Hypothesize: Autonomous loop Phase 2 - Orient & Hypothesize
-- Receives observations from sp_Analyze
-- Generates hypotheses about system improvements
-- Sends ActMessage to Service Broker for execution phase

CREATE OR ALTER PROCEDURE dbo.sp_Hypothesize
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

        -- Check for specific job types first
        DECLARE @JobType NVARCHAR(100) = JSON_VALUE(@Observations, '$.jobType');

        IF @JobType = 'LongRunningPrimeSearch'
        BEGIN
            DECLARE @NextChunkStart BIGINT = JSON_VALUE(@Observations, '$.nextChunkStart');
            DECLARE @ChunkSize BIGINT = JSON_VALUE(@Observations, '$.chunkSize');
            DECLARE @FullRangeEnd BIGINT = JSON_VALUE(@Observations, '$.fullRangeEnd');
            DECLARE @ChunkEnd BIGINT = @NextChunkStart + @ChunkSize - 1;

            IF @NextChunkStart > @FullRangeEnd
            BEGIN
                -- Job is complete, no more hypotheses needed.
                PRINT 'Prime search job complete.';
            END
            ELSE
            BEGIN
                IF @ChunkEnd > @FullRangeEnd
                BEGIN
                    SET @ChunkEnd = @FullRangeEnd;
                END

                INSERT INTO @HypothesisList (HypothesisType, Priority, Description, ExpectedImpact, RequiredActions)
                VALUES (
                    'ProcessPrimeChunk',
                    0, -- Highest priority to ensure it gets processed
                    'Process a chunk of a long-running prime number search job.',
                    JSON_OBJECT('progress': 'chunk processing'),
                    JSON_OBJECT('rangeStart': @NextChunkStart, 'rangeEnd': @ChunkEnd)
                );
            END
        END
        ELSE
        BEGIN
            -- HYPOTHESIS 1: If anomalies detected, suggest index optimization
            IF @AnomalyCount > 5
            BEGIN
                INSERT INTO @HypothesisList (HypothesisType, Priority, Description, ExpectedImpact, RequiredActions)
                VALUES (
                    'IndexOptimization',
                    1,
                    'High number of performance anomalies detected. Missing indexes may be causing slow queries.',
                    JSON_OBJECT('latencyReduction': '50-70%', 'throughputIncrease': '30-50%'),
                    JSON_ARRAY('AnalyzeMissingIndexes', 'CreateRecommendedIndexes', 'UpdateStatistics')
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
                    JSON_OBJECT('latencyReduction': '30-40%', 'cacheHitRate': '60-80%'),
                    JSON_ARRAY('PreloadFrequentEmbeddings', 'EnableInMemoryOLTP', 'OptimizeBufferPool')
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
                    JSON_OBJECT('newConceptsExpected': @PatternCount, 'accuracyImprovement': '5-15%'),
                    JSON_ARRAY('RunClusterAnalysis', 'ExtractConceptEmbeddings', 'BindConceptsToAtoms')
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
                    JSON_OBJECT('accuracyImprovement': '3-8%', 'recentDataCoverage': '95%'),
                    JSON_ARRAY('CollectRecentFeedback', 'IncrementalRetrain', 'ValidateModelDrift')
                );
            END

            -- HYPOTHESIS 5: Prune model based on low-importance tensor atoms
            DECLARE @PruneThreshold FLOAT = 0.01; -- Define a threshold for low importance
            DECLARE @PruneableAtoms NVARCHAR(MAX) = (
                SELECT ta.TensorAtomId, tac.Coefficient
                FROM dbo.TensorAtom ta
                JOIN dbo.TensorAtomCoefficient tac ON ta.TensorAtomId = tac.TensorAtomId
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
                    JSON_OBJECT('modelSizeReduction': '5-10%', 'inferenceSpeedup': '3-5%'),
                    JSON_QUERY(@PruneableAtoms)
                );
            END

            -- HYPOTHESIS 6: Refactor code based on duplicate AST signatures
            DECLARE @DuplicateCodeAtoms NVARCHAR(MAX) = (
                SELECT TOP 10 SpatialSignature.ToString() AS Signature, COUNT(*) AS DuplicateCount
                FROM dbo.CodeAtom
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
                    JSON_OBJECT('codebaseReduction': '1-2%', 'maintainabilityIncrease': 'medium'),
                    JSON_QUERY(@DuplicateCodeAtoms)
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
                    JSON_OBJECT('userErrorRateReduction': '10-20%', 'sessionCompletionIncrease': '5%'),
                    JSON_QUERY(@FailingSessions)
                );
            END
        END
        
        -- 4. COMPILE HYPOTHESES
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
        
        DECLARE @HypothesisPayload NVARCHAR(MAX) = JSON_OBJECT(
            'analysisId': @AnalysisId,
            'hypothesesGenerated': (SELECT COUNT(*) FROM @HypothesisList),
            'hypotheses': JSON_QUERY(@Hypotheses),
            'timestamp': FORMAT(SYSUTCDATETIME(), 'yyyy-MM-ddTHH:mm:ss.fffZ')
        );
        
        -- 5. SEND TO ACT QUEUE
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
        
        -- 6. END ORIGINAL CONVERSATION
        END CONVERSATION @ConversationHandle;
        
        PRINT 'sp_Hypothesize completed: ' + CAST((SELECT COUNT(*) FROM @HypothesisList) AS VARCHAR(10)) + ' hypotheses generated';
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
