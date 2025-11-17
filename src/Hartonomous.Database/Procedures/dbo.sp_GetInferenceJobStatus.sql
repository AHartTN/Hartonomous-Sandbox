CREATE PROCEDURE dbo.sp_GetInferenceJobStatus
    @inferenceId BIGINT
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        InferenceId,
        TaskType,
        Status,
        OutputData,
        Confidence,
        TotalDurationMs,
        RequestTimestamp,
        CompletionTimestamp,
        CorrelationId
    FROM dbo.InferenceRequest
    WHERE InferenceId = @inferenceId;
END;