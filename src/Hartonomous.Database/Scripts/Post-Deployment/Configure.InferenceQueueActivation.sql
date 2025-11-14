IF EXISTS (SELECT 1 FROM sys.service_queues WHERE name = 'InferenceQueue')
BEGIN
    ALTER QUEUE [dbo].[InferenceQueue]
    WITH ACTIVATION (
        STATUS = ON,
        PROCEDURE_NAME = dbo.sp_ExecuteInference_Activated,
        MAX_QUEUE_READERS = 5,
        EXECUTE AS OWNER
    );
    PRINT 'InferenceQueue activation configured.';
END
ELSE
BEGIN
    PRINT 'WARNING: InferenceQueue does not exist - skipping activation configuration.';
END
GO