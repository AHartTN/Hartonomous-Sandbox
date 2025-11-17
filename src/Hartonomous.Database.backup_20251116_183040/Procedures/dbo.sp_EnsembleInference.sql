CREATE PROCEDURE dbo.sp_EnsembleInference
    @inputData NVARCHAR(MAX),
    @modelIds NVARCHAR(MAX),
    @TenantId INT, -- V3: Added for security
    @taskType NVARCHAR(50) = 'classification',
    @inputEmbedding VECTOR(1998) = NULL,
    @topK INT = 10
AS
BEGIN
    SET NOCOUNT ON;

    IF (@modelIds IS NULL OR LTRIM(RTRIM(@modelIds)) = '')
    BEGIN
        THROW 50060, 'Model identifiers are required for ensemble inference.', 1;
    END;

    DECLARE @startTime DATETIME2 = SYSUTCDATETIME();
    DECLARE @embedding VECTOR(1998) = @inputEmbedding;
    IF @embedding IS NULL
    BEGIN
        DECLARE @embeddingJson NVARCHAR(MAX) = CONVERT(NVARCHAR(MAX), JSON_QUERY(@inputData, '$.embedding'));
        IF @embeddingJson IS NOT NULL
        BEGIN
            SET @embedding = CAST(@embeddingJson AS VECTOR(1998));
        END;
    END;

    IF @embedding IS NULL
    BEGIN
        THROW 50061, 'inputData must include an embedding JSON array or pass @inputEmbedding explicitly.', 1;
    END;

    DECLARE @models TABLE (ModelId INT PRIMARY KEY, Weight FLOAT NOT NULL, ModelName NVARCHAR(200) NULL);
    DECLARE @weightsJson NVARCHAR(MAX) = JSON_QUERY(@inputData, '$.weights');

    INSERT INTO @models (ModelId, Weight, ModelName)
    SELECT ModelId, Weight, ModelName
    FROM dbo.fn_SelectModelsForTask(@taskType, @modelIds, @weightsJson, NULL, NULL);

    IF NOT EXISTS (SELECT 1 FROM @models)
    BEGIN
        THROW 50062, 'Unable to parse any model identifiers from @modelIds.', 1;
    END;

    DECLARE @modelsJson NVARCHAR(MAX) = (
        SELECT ModelId, Weight FROM @models FOR JSON PATH
    );

    DECLARE @inferenceId BIGINT;
    INSERT INTO dbo.InferenceRequests (TaskType, InputData, ModelsUsed, EnsembleStrategy)
    VALUES (
        @taskType,
        CONVERT(NVARCHAR(MAX), @inputData),
        CONVERT(NVARCHAR(MAX), @modelsJson),
        'weighted_average'
    );
    SET @inferenceId = SCOPE_IDENTITY();

    DECLARE @scored TABLE
    (
        AtomEmbeddingId BIGINT,
        AtomId BIGINT,
        Modality NVARCHAR(128),
        Subtype NVARCHAR(128),
        ModelCount INT,
        AvgDistance FLOAT,
        EnsembleScore FLOAT
    );

    ;WITH Weighted AS (
        -- V3: Pass TenantId to the underlying function
        SELECT *
        FROM dbo.fn_EnsembleAtomScores(@embedding, @modelsJson, @topK * 2, @TenantId)
    )
    INSERT INTO @scored
    SELECT
        AtomEmbeddingId,
        AtomId,
        MAX(Modality) AS Modality,
        MAX(Subtype) AS Subtype,
        COUNT(DISTINCT ModelId) AS ModelCount,
        AVG(Distance) AS AvgDistance,
        SUM(WeightedScore) AS EnsembleScore
    FROM Weighted
    GROUP BY AtomEmbeddingId, AtomId;

    DECLARE @durationMs INT = DATEDIFF(MILLISECOND, @startTime, SYSUTCDATETIME());
    DECLARE @modelCount INT = (SELECT COUNT(*) FROM @models);
    UPDATE dbo.InferenceRequests
    SET TotalDurationMs = @durationMs,
        OutputMetadata = JSON_OBJECT(
            'status': 'completed',
            'result_count': (SELECT COUNT(*) FROM @scored),
            'top_k': @topK,
            'task_type': @taskType
        )
    WHERE InferenceId = @inferenceId;

    INSERT INTO dbo.InferenceSteps (InferenceId, StepNumber, ModelId, OperationType, DurationMs)
    SELECT
        @inferenceId,
        ROW_NUMBER() OVER (ORDER BY ModelId),
        ModelId,
        @taskType,
        CASE WHEN @modelCount > 0 THEN @durationMs / @modelCount ELSE @durationMs END
    FROM @models;

    SELECT TOP (@topK)
        @inferenceId AS InferenceId,
        AtomEmbeddingId,
        AtomId,
        Modality,
        Subtype,
        ModelCount,
        AvgDistance AS CosineDistance,
        EnsembleScore,
        CASE WHEN ModelCount = (SELECT COUNT(*) FROM @models) THEN 1 ELSE 0 END AS IsConsensus
    FROM @scored
    ORDER BY EnsembleScore DESC, AvgDistance ASC;
END;