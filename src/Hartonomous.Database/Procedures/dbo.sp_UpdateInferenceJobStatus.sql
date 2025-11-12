CREATE PROCEDURE dbo.sp_UpdateInferenceJobStatus
    @inferenceId BIGINT,
    @status NVARCHAR(50),
    @outputData NVARCHAR(MAX) = NULL,
    @confidence DECIMAL(5,4) = NULL,
    @totalDurationMs INT = NULL,
    @completionTimestamp DATETIME2 = NULL
AS
BEGIN
    SET NOCOUNT ON;

    UPDATE dbo.InferenceRequests
    SET Status = @status,
        OutputData = @outputData,
        Confidence = @confidence,
        TotalDurationMs = @totalDurationMs,
        CompletionTimestamp = ISNULL(@completionTimestamp, CASE WHEN @status IN ('Completed', 'Failed') THEN SYSUTCDATETIME() ELSE CompletionTimestamp END)
    WHERE InferenceId = @inferenceId;
END;