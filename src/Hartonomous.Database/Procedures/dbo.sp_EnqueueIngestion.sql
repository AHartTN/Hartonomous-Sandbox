CREATE OR ALTER PROCEDURE dbo.sp_EnqueueIngestion
    @FileName NVARCHAR(500),
    @FileData VARBINARY(MAX),
    @TenantId INT,
    @SourceUri NVARCHAR(500) = NULL
AS
BEGIN
    SET NOCOUNT ON;
    
    DECLARE @ConversationHandle UNIQUEIDENTIFIER;
    
    -- Start Service Broker conversation
    BEGIN DIALOG CONVERSATION @ConversationHandle
        FROM SERVICE [IngestionService]
        TO SERVICE 'IngestionService'
        ON CONTRACT [IngestionContract]
        WITH ENCRYPTION = OFF;
    
    -- Construct XML message with file metadata and data
    DECLARE @MessageXml XML = (
        SELECT 
            @FileName AS FileName,
            @TenantId AS TenantId,
            @SourceUri AS SourceUri,
            CONVERT(NVARCHAR(MAX), @FileData, 1) AS FileDataHex -- Convert to hex string
        FOR XML PATH('IngestionRequest'), TYPE
    );
    
    -- Send message to IngestionQueue
    SEND ON CONVERSATION @ConversationHandle
        MESSAGE TYPE [IngestionRequest]
        (@MessageXml);
    
    -- Return conversation handle for tracking
    SELECT @ConversationHandle AS ConversationHandle;
END;
