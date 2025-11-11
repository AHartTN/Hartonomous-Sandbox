CREATE PROCEDURE dbo.sp_SubmitInferenceJob
    @taskType NVARCHAR(50),
    @inputData NVARCHAR(MAX),
    @modelId INT = NULL,
    @tenantId INT = 0,
    @correlationId NVARCHAR(100) = NULL OUTPUT,
    @inferenceId BIGINT = NULL OUTPUT
AS
BEGIN
    SET NOCOUNT ON;

    IF @correlationId IS NULL
    BEGIN
        SET @correlationId = CAST(NEWID() AS NVARCHAR(36));
    END;

    DECLARE @tokenCount INT = JSON_VALUE(@inputData, '$.token_count');
    DECLARE @requiresMultiModal BIT = JSON_VALUE(@inputData, '$.requires_multimodal');
    DECLARE @requiresToolUse BIT = JSON_VALUE(@inputData, '$.requires_tools');
    DECLARE @priority NVARCHAR(50) = JSON_VALUE(@inputData, '$.priority');
    DECLARE @modelName NVARCHAR(255) = JSON_VALUE(@inputData, '$.model_name');

    SET @tokenCount = ISNULL(@tokenCount, 1000);
    SET @requiresMultiModal = ISNULL(@requiresMultiModal, 0);
    SET @requiresToolUse = ISNULL(@requiresToolUse, 0);
    SET @priority = ISNULL(@priority, 'medium');
    SET @modelName = ISNULL(@modelName, '');

    DECLARE @complexity INT = dbo.fn_CalculateComplexity(@tokenCount, @requiresMultiModal, @requiresToolUse);

    DECLARE @sla NVARCHAR(20) = dbo.fn_DetermineSla(@priority, @complexity);

    DECLARE @estimatedResponseTimeMs INT = dbo.fn_EstimateResponseTime(@modelName, @complexity);

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
        'Queued',
        @correlationId,
        SYSUTCDATETIME(),
        @complexity,
        @sla,
        @estimatedResponseTimeMs
    );

    SET @inferenceId = SCOPE_IDENTITY();

    BEGIN DIALOG CONVERSATION @correlationId
        FROM SERVICE [InferenceService]
        TO SERVICE 'InferenceService'
        ON CONTRACT [InferenceJobContract]
        WITH ENCRYPTION = OFF;
    
    DECLARE @MessageXml XML = (
        SELECT 
            @inferenceId AS InferenceId,
            @taskType AS TaskType,
            @metadataJson AS InputData,
            @modelId AS ModelId,
            @tenantId AS TenantId,
            @correlationId AS CorrelationId
        FOR XML PATH('InferenceJob'), TYPE
    );
    
    SEND ON CONVERSATION @correlationId
        MESSAGE TYPE [InferenceJobRequest]
        (@MessageXml);
    
    SELECT 
        @inferenceId AS InferenceId,
        @correlationId AS CorrelationId,
        'Queued' AS Status,
        @complexity AS Complexity,
        @sla AS SlaTier,
        @estimatedResponseTimeMs AS EstimatedResponseTimeMs;
END;
GO