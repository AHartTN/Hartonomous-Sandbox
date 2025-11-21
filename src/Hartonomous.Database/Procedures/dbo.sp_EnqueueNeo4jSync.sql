CREATE PROCEDURE dbo.sp_EnqueueNeo4jSync
    @EntityType NVARCHAR(50),
    @EntityId BIGINT,
    @SyncType NVARCHAR(50) = 'CREATE'
AS
BEGIN
    SET NOCOUNT ON;
    
    -- Get TenantId based on entity type
    DECLARE @TenantId INT = 0;
    
    IF @EntityType = 'Atom'
    BEGIN
        SELECT @TenantId = TenantId 
        FROM dbo.Atom 
        WHERE AtomId = @EntityId;
    END
    -- Add more entity types as needed
    -- ELSE IF @EntityType = 'Transform' BEGIN ... END
    
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
            @SyncType AS SyncType,
            @TenantId AS TenantId
        FOR XML PATH('Neo4jSync'), TYPE
    );
    
    -- Convert XML to VARBINARY for Service Broker message
    DECLARE @MessageBody VARBINARY(MAX) = CAST(@MessageXml AS VARBINARY(MAX));
    
    SEND ON CONVERSATION @ConversationHandle
        MESSAGE TYPE [Neo4jSyncRequest]
        (@MessageBody);
END;