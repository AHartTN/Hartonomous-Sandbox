USE [$(DatabaseName)]
GO

IF EXISTS (SELECT 1 FROM sys.service_queues WHERE name = 'Neo4jSyncQueue' AND is_activation_enabled = 0)
BEGIN
    ALTER QUEUE [dbo].[Neo4jSyncQueue]
    WITH ACTIVATION (
        STATUS = ON,
        PROCEDURE_NAME = dbo.sp_ForwardToNeo4j_Activated,
        MAX_QUEUE_READERS = 3,
        EXECUTE AS OWNER
    );
    PRINT 'Neo4jSyncQueue activation enabled.';
END
ELSE IF EXISTS (SELECT 1 FROM sys.service_queues WHERE name = 'Neo4jSyncQueue' AND is_activation_enabled = 1)
BEGIN
    PRINT 'Neo4jSyncQueue activation already enabled.';
END
ELSE
BEGIN
    PRINT 'Neo4jSyncQueue not found or activation status unknown.';
END
GO