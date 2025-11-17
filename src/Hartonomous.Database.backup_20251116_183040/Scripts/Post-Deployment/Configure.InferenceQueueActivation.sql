-- Configure InferenceQueue activation for asynchronous model inference
-- Queue created in ServiceBroker\Queues\dbo.InferenceQueue.sql
ALTER QUEUE [dbo].[InferenceQueue]
WITH ACTIVATION (
    STATUS = ON,
    PROCEDURE_NAME = dbo.sp_ExecuteInference_Activated,
    MAX_QUEUE_READERS = 5,
    EXECUTE AS OWNER
);
PRINT '✓ InferenceQueue activation configured (5 readers, high-throughput inference)';
GO