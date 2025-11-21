CREATE QUEUE [dbo].[Neo4jSyncQueue]
WITH STATUS = ON,
     POISON_MESSAGE_HANDLING (STATUS = OFF), -- Enterprise: Manual error handling in worker, no auto-disable
     RETENTION = OFF;