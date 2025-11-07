CREATE OR ALTER PROCEDURE dbo.sp_SubmitInferenceJob
    @taskType NVARCHAR(50),
    @inputData NVARCHAR(MAX),
    @correlationId NVARCHAR(100) = NULL OUTPUT,
    @inferenceId BIGINT = NULL OUTPUT
AS
BEGIN
    SET NOCOUNT ON;

    IF @correlationId IS NULL
    BEGIN
        SET @correlationId = CAST(NEWID() AS NVARCHAR(36));
    END;

    -- Parse input data to extract parameters for autonomous calculations
    DECLARE @tokenCount INT = JSON_VALUE(@inputData, '$.token_count');
    DECLARE @requiresMultiModal BIT = JSON_VALUE(@inputData, '$.requires_multimodal');
    DECLARE @requiresToolUse BIT = JSON_VALUE(@inputData, '$.requires_tools');
    DECLARE @priority NVARCHAR(50) = JSON_VALUE(@inputData, '$.priority');
    DECLARE @modelName NVARCHAR(255) = JSON_VALUE(@inputData, '$.model_name');

    -- Set defaults if not provided
    SET @tokenCount = ISNULL(@tokenCount, 1000);
    SET @requiresMultiModal = ISNULL(@requiresMultiModal, 0);
    SET @requiresToolUse = ISNULL(@requiresToolUse, 0);
    SET @priority = ISNULL(@priority, 'medium');
    SET @modelName = ISNULL(@modelName, '');

    -- Calculate complexity using SQL CLR function
    DECLARE @complexity INT = dbo.fn_CalculateComplexity(@tokenCount, @requiresMultiModal, @requiresToolUse);

    -- Determine SLA using SQL CLR function
    DECLARE @sla NVARCHAR(20) = dbo.fn_DetermineSla(@priority, @complexity);

    -- Estimate response time using SQL CLR function
    DECLARE @estimatedResponseTimeMs INT = dbo.fn_EstimateResponseTime(@modelName, @complexity);

    -- Store enriched metadata
    DECLARE @metadataJson NVARCHAR(MAX) = JSON_MODIFY(
        JSON_MODIFY(
            JSON_MODIFY(
                JSON_MODIFY(@inputData, '$.complexity', @complexity),
                '$.sla_tier', @sla
            ),
            '$.estimated_response_time_ms', @estimatedResponseTimeMs
        ),
        '$.autonomous_metadata', JSON_QUERY('{
            "calculated_at": "' + CONVERT(NVARCHAR(27), SYSUTCDATETIME(), 126) + '",
            "intelligence_level": "database_native"
        }')
    );

    INSERT INTO dbo.InferenceRequests (
        TaskType,
        InputData,
        Status,
        CorrelationId,
        RequestTimestamp,
        Complexity,
        SlaTier,
        EstimatedResponseTimeMs
    )
    VALUES (
        @taskType,
        @metadataJson,
        'Pending',
        @correlationId,
        SYSUTCDATETIME(),
        @complexity,
        @sla,
        @estimatedResponseTimeMs
    );

    SET @inferenceId = SCOPE_IDENTITY();
END;
GO

CREATE OR ALTER PROCEDURE dbo.sp_GetInferenceJobStatus
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
    FROM dbo.InferenceRequests
    WHERE InferenceId = @inferenceId;
END;
GO

CREATE OR ALTER PROCEDURE dbo.sp_UpdateInferenceJobStatus
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
GO