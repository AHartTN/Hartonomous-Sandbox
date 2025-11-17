USE [$(DatabaseName)]
GO

IF EXISTS (SELECT 1 FROM sys.service_queues WHERE name = 'InferenceQueue' AND is_activation_enabled = 0)
BEGIN
    ALTER QUEUE [dbo].[InferenceQueue]
    WITH ACTIVATION (
        STATUS = ON,
        PROCEDURE_NAME = dbo.sp_ExecuteInference_Activated,
        MAX_QUEUE_READERS = 5,
        EXECUTE AS OWNER
    );
    PRINT 'InferenceQueue activation enabled.';
END
ELSE IF EXISTS (SELECT 1 FROM sys.service_queues WHERE name = 'InferenceQueue' AND is_activation_enabled = 1)
BEGIN
    PRINT 'InferenceQueue activation already enabled.';
END
ELSE
BEGIN
    PRINT 'InferenceQueue not found or activation status unknown.';
END
GO