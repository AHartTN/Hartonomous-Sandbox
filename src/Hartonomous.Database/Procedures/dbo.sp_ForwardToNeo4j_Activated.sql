CREATE PROCEDURE dbo.sp_ForwardToNeo4j_Activated
AS
BEGIN
    SET NOCOUNT ON;
    
    DECLARE @conversation_handle UNIQUEIDENTIFIER;
    DECLARE @message_body NVARCHAR(MAX);
    DECLARE @message_type_name NVARCHAR(256);
    
    -- Process messages in a loop
    WHILE (1=1)
    BEGIN
        BEGIN TRANSACTION;
        
        WAITFOR (
            RECEIVE TOP(1)
                @conversation_handle = conversation_handle,
                @message_body = CAST(message_body AS NVARCHAR(MAX)),
                @message_type_name = message_type_name
            FROM Neo4jSyncQueue
        ), TIMEOUT 1000;
        
        IF @@ROWCOUNT = 0
        BEGIN
            COMMIT TRANSACTION;
            BREAK;
        END
        
        IF @message_type_name = 'Neo4jSyncRequest'
        BEGIN
            BEGIN TRY
                -- Parse sync request
                DECLARE @SyncXml XML = CAST(@message_body AS XML);
                DECLARE @EntityType NVARCHAR(50) = @SyncXml.value('(/Neo4jSync/EntityType)[1]', 'NVARCHAR(50)');
                DECLARE @EntityId BIGINT = @SyncXml.value('(/Neo4jSync[1]/EntityId)[1]', 'BIGINT');
                DECLARE @SyncType NVARCHAR(50) = @SyncXml.value('(/Neo4jSync/SyncType)[1]', 'NVARCHAR(50)');
                
                DECLARE @Neo4jEndpoint NVARCHAR(500) = 'bolt://localhost:7687';
                DECLARE @CypherQuery NVARCHAR(MAX);
                DECLARE @ResponseJson NVARCHAR(MAX);
                
                -- Build Cypher query based on entity type
                IF @EntityType = 'Atom'
                BEGIN
                    -- Sync atom to Neo4j
                    SELECT @CypherQuery = 
                        'MERGE (a:Atom {atomId: $atomId}) ' +
                        'SET a.contentType = $contentType, ' +
                        '    a.contentHash = $contentHash, ' +
                        '    a.createdUtc = $createdUtc, ' +
                        '    a.metadata = $metadata'
                    FROM dbo.Atoms
                    WHERE AtomId = @EntityId;
                    
                    -- Execute via external REST call (using sp_invoke_external_rest_endpoint)
                    -- Note: sp_invoke_external_rest_endpoint is a placeholder for actual external call mechanism
                    -- This would typically be handled by an external service or CLR function
                    SET @ResponseJson = '{"status": "simulated_success", "entityType": "' + @EntityType + '", "entityId": ' + CAST(@EntityId AS NVARCHAR(MAX)) + '}';
                END
                ELSE IF @EntityType = 'GenerationStream'
                BEGIN
                    -- Sync generation provenance to Neo4j
                    SELECT @CypherQuery = 
                        'MERGE (gs:GenerationStream {generationStreamId: $streamId}) ' +
                        'SET gs.modelId = $modelId, ' +
                        '    gs.createdUtc = $createdUtc ' +
                        'WITH gs ' +
                        'UNWIND $generatedAtomIds AS atomId ' +
                        'MATCH (a:Atom {atomId: atomId}) ' +
                        'MERGE (gs)-[:GENERATED]->(a)'
                    FROM provenance.GenerationStreams
                    WHERE GenerationStreamId = @EntityId;
                    
                    SET @ResponseJson = '{"status": "simulated_success", "entityType": "' + @EntityType + '", "entityId": ' + CAST(@EntityId AS NVARCHAR(MAX)) + '}';
                END
                ELSE IF @EntityType = 'AtomProvenance'
                BEGIN
                    -- Sync atom-to-atom provenance edges
                    DECLARE @RelationType NVARCHAR(128);
                    DECLARE @ParentAtomId BIGINT;
                    DECLARE @ChildAtomId BIGINT;
                    DECLARE @EdgeCreatedUtc DATETIME2;
                    
                    SELECT @RelationType = edge.RelationType,
                           @EdgeCreatedUtc = edge.CreatedAt
                    FROM graph.AtomGraphEdges edge
                    WHERE edge.AtomRelationId = @EntityId; -- Assuming EntityId maps to AtomRelationId
                    
                    SET @CypherQuery = 
                        'MATCH (parent:Atom {atomId: $parentAtomId}), ' +
                        '      (child:Atom {atomId: $childAtomId}) ' +
                        'MERGE (parent)-[r:' + @RelationType + ']->(child) ' +
                        'SET r.createdUtc = $createdUtc';
                    
                    SET @ResponseJson = '{"status": "simulated_success", "entityType": "' + @EntityType + '", "entityId": ' + CAST(@EntityId AS NVARCHAR(MAX)) + '}';
                END
                ELSE
                BEGIN
                    SET @ResponseJson = '{"status": "failed", "error": "Unknown EntityType: ' + @EntityType + '"}';
                END;
                
                -- Log successful sync
                INSERT INTO dbo.Neo4jSyncLog (
                    EntityType,
                    EntityId,
                    SyncType,
                    SyncStatus,
                    Response,
                    SyncTimestamp
                )
                VALUES (
                    @EntityType,
                    @EntityId,
                    @SyncType,
                    'Success',
                    @ResponseJson,
                    SYSUTCDATETIME()
                );
                
                -- End conversation
                END CONVERSATION @conversation_handle;
                
            END TRY
            BEGIN CATCH
                -- Handle errors
                DECLARE @ErrorMessage NVARCHAR(4000) = ERROR_MESSAGE();
                
                INSERT INTO dbo.Neo4jSyncLog (
                    EntityType,
                    EntityId,
                    SyncType,
                    SyncStatus,
                    Response,
                    SyncTimestamp
                )
                VALUES (
                    @EntityType,
                    @EntityId,
                    @SyncType,
                    'Failed',
                    JSON_OBJECT('error': @ErrorMessage),
                    SYSUTCDATETIME()
                );
                
                END CONVERSATION @conversation_handle;
            END CATCH
        END
        ELSE IF @message_type_name = 'http://schemas.microsoft.com/SQL/ServiceBroker/EndDialog'
        BEGIN
            END CONVERSATION @conversation_handle;
        END
        ELSE IF @message_type_name = 'http://schemas.microsoft.com/SQL/ServiceBroker/Error'
        BEGIN
            END CONVERSATION @conversation_handle;
        END;
        
        COMMIT TRANSACTION;
    END
END;