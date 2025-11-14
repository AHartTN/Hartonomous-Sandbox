IF EXISTS (SELECT 1 FROM sys.service_queues WHERE name = 'Neo4jSyncQueue')
BEGIN
    ALTER QUEUE [dbo].[Neo4jSyncQueue]
    WITH ACTIVATION (
        STATUS = ON,
        PROCEDURE_NAME = dbo.sp_ForwardToNeo4j_Activated,
        MAX_QUEUE_READERS = 3,
        EXECUTE AS OWNER
    );
    PRINT 'Neo4jSyncQueue activation configured.';
END
ELSE
BEGIN
    PRINT 'WARNING: Neo4jSyncQueue does not exist - skipping activation configuration.';
END
GO