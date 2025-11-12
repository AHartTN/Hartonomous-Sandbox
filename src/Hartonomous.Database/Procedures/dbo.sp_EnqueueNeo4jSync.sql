CREATE PROCEDURE dbo.sp_EnqueueNeo4jSync
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