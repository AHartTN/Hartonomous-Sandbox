-- =============================================
-- Service Broker Neo4j Sync Activation
-- =============================================
-- Event-driven Neo4j provenance synchronization
-- Replaces the C# Neo4jSync polling service
-- =============================================

USE Hartonomous;
GO

-- Create message types for Neo4j sync
IF NOT EXISTS (SELECT 1 FROM sys.service_message_types WHERE name = 'Neo4jSyncRequest')
BEGIN
    CREATE MESSAGE TYPE [Neo4jSyncRequest]
    VALIDATION = WELL_FORMED_XML;
END;
GO

-- Create contract for Neo4j sync
IF NOT EXISTS (SELECT 1 FROM sys.service_contracts WHERE name = 'Neo4jSyncContract')
BEGIN
    CREATE CONTRACT [Neo4jSyncContract]
    (
        [Neo4jSyncRequest] SENT BY INITIATOR
    );
END;
GO

-- Create queue for Neo4j sync
IF NOT EXISTS (SELECT 1 FROM sys.service_queues WHERE name = 'Neo4jSyncQueue')
BEGIN
    CREATE QUEUE [Neo4jSyncQueue]
    WITH STATUS = ON,
         RETENTION = OFF;
END;
GO

-- Create service for Neo4j sync
IF EXISTS (SELECT 1 FROM sys.services WHERE name = 'Neo4jSyncService')
    DROP SERVICE [Neo4jSyncService];
GO

CREATE SERVICE [Neo4jSyncService]
ON QUEUE [Neo4jSyncQueue]
([Neo4jSyncContract]);
GO

-- =============================================
-- ACTIVATED PROCEDURE: Forward to Neo4j
-- =============================================
-- This procedure is automatically activated when provenance data needs syncing
-- It replaces the C# Neo4jSync polling service
-- =============================================

CREATE OR ALTER PROCEDURE dbo.sp_ForwardToNeo4j_Activated
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
                DECLARE @EntityId BIGINT = @SyncXml.value('(/Neo4jSync/EntityId)[1]', 'BIGINT');
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
                    EXEC @ResponseJson = sp_invoke_external_rest_endpoint
                        @url = @Neo4jEndpoint,
                        @method = 'POST',
                        @payload = @CypherQuery;
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
                    
                    EXEC @ResponseJson = sp_invoke_external_rest_endpoint
                        @url = @Neo4jEndpoint,
                        @method = 'POST',
                        @payload = @CypherQuery;
                END
                ELSE IF @EntityType = 'AtomProvenance'
                BEGIN
                    -- Sync atom-to-atom provenance edges
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
                -- Log sync failure
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
                    JSON_OBJECT('error': @ErrorMsg),
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

-- =============================================
-- Enable Neo4j sync queue activation
-- =============================================
ALTER QUEUE [Neo4jSyncQueue]
WITH ACTIVATION (
    STATUS = ON,
    PROCEDURE_NAME = dbo.sp_ForwardToNeo4j_Activated,
    MAX_QUEUE_READERS = 3,
    EXECUTE AS OWNER
);
GO

-- =============================================
-- Helper procedure to enqueue Neo4j sync
-- =============================================
CREATE OR ALTER PROCEDURE dbo.sp_EnqueueNeo4jSync
    @EntityType NVARCHAR(50),
    @EntityId BIGINT,
    @SyncType NVARCHAR(50) = 'CREATE'
AS
BEGIN
    SET NOCOUNT ON;
    
    DECLARE @ConversationHandle UNIQUEIDENTIFIER;
    
    BEGIN DIALOG CONVERSATION @ConversationHandle
        FROM SERVICE [Neo4jSyncService]
        TO SERVICE 'Neo4jSyncService'
        ON CONTRACT [Neo4jSyncContract]
        WITH ENCRYPTION = OFF;
    
    DECLARE @MessageXml XML = (
        SELECT 
            @EntityType AS EntityType,
            @EntityId AS EntityId,
            @SyncType AS SyncType
        FOR XML PATH('Neo4jSync'), TYPE
    );
    
    SEND ON CONVERSATION @ConversationHandle
        MESSAGE TYPE [Neo4jSyncRequest]
        (@MessageXml);
END;
GO

PRINT 'Service Broker Neo4j sync queue activated successfully';
PRINT 'Queue: Neo4jSyncQueue';
PRINT 'Activation Procedure: sp_ForwardToNeo4j_Activated';
PRINT 'Max Concurrent Readers: 3';
GO
