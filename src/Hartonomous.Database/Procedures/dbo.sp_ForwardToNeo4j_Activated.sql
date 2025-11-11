CREATE PROCEDURE dbo.sp_ForwardToNeo4j_Activated
AS
BEGIN
    SET NOCOUNT ON;
    
    DECLARE @conversation_handle UNIQUEIDENTIFIER;
    DECLARE @message_body NVARCHAR(MAX);
    DECLARE @message_type_name NVARCHAR(256);
    
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
                DECLARE @SyncXml XML = CAST(@message_body AS XML);
                DECLARE @EntityType NVARCHAR(50) = @SyncXml.value('(/Neo4jSync/EntityType)[1]', 'NVARCHAR(50)');
                DECLARE @EntityId BIGINT = @SyncXml.value('(/Neo4jSync/EntityId)[1]', 'BIGINT');
                DECLARE @SyncType NVARCHAR(50) = @SyncXml.value('(/Neo4jSync/SyncType)[1]', 'NVARCHAR(50)');
                
                DECLARE @Neo4jEndpoint NVARCHAR(500) = 'bolt://localhost:7687';
                DECLARE @CypherQuery NVARCHAR(MAX);
                DECLARE @ResponseJson NVARCHAR(MAX);
                
                IF @EntityType = 'Atom'
                BEGIN
                    SELECT @CypherQuery = 
                        'MERGE (a:Atom {atomId: $atomId}) ' +
                        'SET a.contentType = $contentType, ' +
                        '    a.contentHash = $contentHash, ' +
                        '    a.createdUtc = $createdUtc, ' +
                        '    a.metadata = $metadata'
                    FROM dbo.Atoms
                    WHERE AtomId = @EntityId;
                    
                    EXEC @ResponseJson = sp_invoke_external_rest_endpoint
                        @url = @Neo4jEndpoint,
                        @method = 'POST',
                        @payload = @CypherQuery;
                END
                ELSE IF @EntityType = 'GenerationStream'
                BEGIN
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
                    
                    EXEC @ResponseJson = sp_invoke_external_rest_endpoint
                        @url = @Neo4jEndpoint,
                        @method = 'POST',
                        @payload = @CypherQuery;
                END
                ELSE IF @EntityType = 'AtomProvenance'
                BEGIN
                    SELECT @CypherQuery = 
                        'MATCH (parent:Atom {atomId: $parentAtomId}), ' +
                        '      (child:Atom {atomId: $childAtomId}) ' +
                        'MERGE (parent)-[r:' + edge.DependencyType + ']->(child) ' +
                        'SET r.createdUtc = $createdUtc'
                    FROM provenance.AtomGraphEdges edge
                    WHERE edge.$node_id = @EntityId;
                    
                    EXEC @ResponseJson = sp_invoke_external_rest_endpoint
                        @url = @Neo4jEndpoint,
                        @method = 'POST',
                        @payload = @CypherQuery;
                END;
                
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
                
                END CONVERSATION @conversation_handle;
                
            END TRY
            BEGIN CATCH
                DECLARE @ErrorMsg NVARCHAR(4000) = ERROR_MESSAGE();
                
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
                    (SELECT @ErrorMsg FOR JSON PATH, WITHOUT_ARRAY_WRAPPER),
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
GO