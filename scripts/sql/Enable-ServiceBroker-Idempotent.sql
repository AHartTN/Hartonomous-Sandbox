-- =============================================
-- Enable Service Broker (Idempotent)
-- =============================================
-- This script safely enables Service Broker and cleans up orphaned conversations
-- Can be run multiple times without issues

USE master;
GO

-- Check if Service Broker is already enabled
DECLARE @IsBrokerEnabled BIT;
SELECT @IsBrokerEnabled = is_broker_enabled 
FROM sys.databases 
WHERE name = 'Hartonomous';

IF @IsBrokerEnabled = 0
BEGIN
    PRINT 'Service Broker is disabled. Enabling...';
    
    -- Set database to SINGLE_USER to enable Service Broker
    -- This requires exclusive access
    IF NOT EXISTS (
        SELECT 1 
        FROM sys.dm_exec_sessions 
        WHERE database_id = DB_ID('Hartonomous') 
        AND session_id <> @@SPID
    )
    BEGIN
        ALTER DATABASE Hartonomous SET SINGLE_USER WITH ROLLBACK IMMEDIATE;
        ALTER DATABASE Hartonomous SET ENABLE_BROKER;
        ALTER DATABASE Hartonomous SET MULTI_USER;
        PRINT 'Service Broker enabled successfully.';
    END
    ELSE
    BEGIN
        PRINT 'Cannot enable Service Broker - database has active connections.';
        PRINT 'Close all connections and try again.';
    END
END
ELSE
BEGIN
    PRINT 'Service Broker is already enabled.';
END
GO

-- Clean up orphaned conversations (can run anytime)
USE Hartonomous;
GO

PRINT 'Checking for orphaned conversations...';

DECLARE @ConversationCount INT;
SELECT @ConversationCount = COUNT(*)
FROM sys.conversation_endpoints ce
INNER JOIN sys.services s ON ce.service_id = s.service_id
WHERE s.name = 'Neo4jSyncService';

IF @ConversationCount > 0
BEGIN
    PRINT 'Found ' + CAST(@ConversationCount AS VARCHAR(10)) + ' orphaned conversations. Cleaning up...';
    
    DECLARE @handle UNIQUEIDENTIFIER;
    DECLARE conv_cursor CURSOR FOR 
    SELECT conversation_handle 
    FROM sys.conversation_endpoints ce
    INNER JOIN sys.services s ON ce.service_id = s.service_id
    WHERE s.name = 'Neo4jSyncService';
    
    OPEN conv_cursor;
    FETCH NEXT FROM conv_cursor INTO @handle;
    
    WHILE @@FETCH_STATUS = 0
    BEGIN
        END CONVERSATION @handle WITH CLEANUP;
        FETCH NEXT FROM conv_cursor INTO @handle;
    END;
    
    CLOSE conv_cursor;
    DEALLOCATE conv_cursor;
    
    PRINT 'Orphaned conversations cleaned up.';
END
ELSE
BEGIN
    PRINT 'No orphaned conversations found.';
END
GO

-- Clear any stuck messages from the queue
PRINT 'Checking for messages in Neo4jSyncQueue...';

DECLARE @MessageCount INT;
SELECT @MessageCount = COUNT(*)
FROM dbo.Neo4jSyncQueue;

IF @MessageCount > 0
BEGIN
    PRINT 'Found ' + CAST(@MessageCount AS VARCHAR(10)) + ' messages in queue. Clearing...';
    
    -- Receive and discard all messages
    DECLARE @ConvHandle UNIQUEIDENTIFIER;
    
    WHILE EXISTS (SELECT 1 FROM dbo.Neo4jSyncQueue)
    BEGIN
        BEGIN TRANSACTION;
        
        RECEIVE TOP(1) 
            @ConvHandle = conversation_handle
        FROM dbo.Neo4jSyncQueue;
        
        IF @ConvHandle IS NOT NULL
        BEGIN
            END CONVERSATION @ConvHandle WITH CLEANUP;
        END
        
        COMMIT TRANSACTION;
    END
    
    PRINT 'Queue cleared.';
END
ELSE
BEGIN
    PRINT 'Queue is empty.';
END
GO

PRINT 'Service Broker setup complete and clean.';
