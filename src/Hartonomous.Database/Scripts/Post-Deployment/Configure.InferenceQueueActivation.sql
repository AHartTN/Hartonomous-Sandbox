ALTER QUEUE [InferenceQueue]
WITH ACTIVATION (
    STATUS = ON,
    PROCEDURE_NAME = dbo.sp_ExecuteInference_Activated,
    MAX_QUEUE_READERS = 5,
    EXECUTE AS OWNER
);
GO