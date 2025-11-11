CREATE SERVICE [Neo4jSyncService]
ON QUEUE [Neo4jSyncQueue]
([Neo4jSyncContract]);
GO