CREATE OR ALTER PROCEDURE dbo.sp_EnsembleInference
    @inputData NVARCHAR(MAX),
    @modelIds NVARCHAR(MAX),
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


    IF @embedding IS NULL
    BEGIN

        IF @embeddingJson IS NOT NULL
        BEGIN
            SET @embedding = CAST(@embeddingJson AS VECTOR(1998));
        END;
    END;

    IF @embedding IS NULL
    BEGIN
        THROW 50061, 'inputData must include an embedding JSON array or pass @inputEmbedding explicitly.', 1;
    END;


    

    IF NOT EXISTS (SELECT 1 FROM @models)
    BEGIN
        THROW 50062, 'Unable to parse any model identifiers from @modelIds.', 1;
    END;

        SELECT ModelId, Weight FROM @models FOR JSON PATH
    );

    
    SET @inferenceId = SCOPE_IDENTITY();

    (
        AtomEmbeddingId BIGINT,
        AtomId BIGINT,
        Modality NVARCHAR(128),
        Subtype NVARCHAR(128),
        SourceType NVARCHAR(128),
        SourceUri NVARCHAR(2048),
        CanonicalText NVARCHAR(MAX),
        ModelCount INT,
        AvgDistance FLOAT,
        EnsembleScore FLOAT
    );

    ;WITH Weighted AS (
        SELECT *
        FROM dbo.fn_EnsembleAtomScores(@embedding, @modelsJson, @topK * 2, NULL)
    )
    


    UPDATE dbo.InferenceRequests
    SET TotalDurationMs = @durationMs,
        OutputMetadata = JSON_OBJECT(
            'status': 'completed',
            'result_count': (SELECT COUNT(*) FROM @scored),
            'top_k': @topK,
            'task_type': @taskType
        )
    WHERE InferenceId = @inferenceId;

    

    SELECT TOP (@topK)
        @inferenceId AS InferenceId,
        AtomEmbeddingId,
        AtomId,
        Modality,
        Subtype,
        SourceType,
        SourceUri,
        CanonicalText,
        ModelCount,
        AvgDistance AS CosineDistance,
        EnsembleScore,
        CASE WHEN ModelCount = (SELECT COUNT(*) FROM @models) THEN 1 ELSE 0 END AS IsConsensus
    FROM @scored
    ORDER BY EnsembleScore DESC, AvgDistance ASC;
END;
