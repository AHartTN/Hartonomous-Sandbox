-- Configure Neo4jSyncQueue activation for autonomous graph synchronization
-- Queue created in ServiceBroker\Queues\dbo.Neo4jSyncQueue.sql
ALTER QUEUE [dbo].[Neo4jSyncQueue]
WITH ACTIVATION (
    STATUS = ON,
    PROCEDURE_NAME = dbo.sp_ForwardToNeo4j_Activated,
    MAX_QUEUE_READERS = 3,
    EXECUTE AS OWNER
);
PRINT '✓ Neo4jSyncQueue activation configured (3 readers, autonomous sync to Neo4j)';
GO