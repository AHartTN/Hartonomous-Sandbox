CREATE PROCEDURE [dbo].[sp_UpdateInferenceJobStatus]
    @InferenceId BIGINT,
    @TenantId INT,
    @Status NVARCHAR(MAX),
    @OutputData JSON = NULL,
    @TotalDurationMs INT = NULL,
    @Confidence FLOAT = NULL
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    -- Authorization check
    IF NOT EXISTS (
        SELECT 1 
        FROM dbo.InferenceRequest 
        WHERE InferenceId = @InferenceId 
          AND JSON_VALUE(InputData, '$.TenantId') = CAST(@TenantId AS NVARCHAR(50))
    )
    BEGIN
        RAISERROR('Inference job not found or access denied.', 16, 1);
        RETURN;
    END;

    -- Optimistic concurrency control
    DECLARE @CurrentStatus NVARCHAR(MAX);
    SELECT @CurrentStatus = Status FROM dbo.InferenceRequest WHERE InferenceId = @InferenceId;

    -- Prevent updates to completed jobs
    IF @CurrentStatus IN ('completed', 'failed', 'cancelled')
    BEGIN
        RAISERROR('Cannot update job in terminal state: %s', 16, 1, @CurrentStatus);
        RETURN;
    END;

    UPDATE dbo.InferenceRequest
    SET 
        Status = @Status,
        CompletionTimestamp = CASE WHEN @Status IN ('completed', 'failed', 'cancelled') THEN SYSUTCDATETIME() ELSE CompletionTimestamp END,
        OutputData = ISNULL(@OutputData, OutputData),
        TotalDurationMs = ISNULL(@TotalDurationMs, TotalDurationMs),
        Confidence = ISNULL(@Confidence, Confidence)
    WHERE InferenceId = @InferenceId;

    IF @@ROWCOUNT = 0
    BEGIN
        RAISERROR('InferenceId not found', 16, 1);
    END;

    -- Audit trail (if OperationProvenance table exists)
    IF OBJECT_ID('dbo.OperationProvenance', 'U') IS NOT NULL
    BEGIN
        INSERT INTO dbo.OperationProvenance (OperationType, InputHash, ExecutionTime, AtomCount, TenantId)
        VALUES ('sp_UpdateInferenceJobStatus', HASHBYTES('SHA2_256', CAST(@InferenceId AS VARBINARY(8))), @TotalDurationMs, 1, @TenantId);
    END;
END;
GO
