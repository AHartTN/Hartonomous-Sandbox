CREATE SERVICE [Neo4jSyncService]
ON QUEUE [dbo].[Neo4jSyncQueue]
([Neo4jSyncContract]);