CREATE PROCEDURE [dbo].[sp_GetInferenceJobStatus]
    @InferenceId BIGINT,
    @TenantId INT
AS
BEGIN
    SET NOCOUNT ON;

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

    SELECT 
        InferenceId,
        RequestTimestamp,
        CompletionTimestamp,
        TaskType,
        Status,
        Confidence,
        ModelsUsed,
        EnsembleStrategy,
        TotalDurationMs,
        CacheHit,
        UserRating,
        UserFeedback,
        Complexity,
        SlaTier,
        EstimatedResponseTimeMs
    FROM dbo.InferenceRequest
    WHERE InferenceId = @InferenceId;
END;
GO
