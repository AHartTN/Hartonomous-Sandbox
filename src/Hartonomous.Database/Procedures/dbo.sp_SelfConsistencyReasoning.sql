CREATE PROCEDURE dbo.sp_SelfConsistencyReasoning
    @ProblemId UNIQUEIDENTIFIER,
    @Prompt NVARCHAR(MAX),
    @NumSamples INT = 5,
    @Temperature FLOAT = 0.8,
    @Debug BIT = 0
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @StartTime DATETIME2 = SYSUTCDATETIME();

    IF @Debug = 1
        PRINT 'Starting self-consistency reasoning with ' + CAST(@NumSamples AS NVARCHAR(10)) + ' samples';

    DECLARE @Samples TABLE (
        SampleId INT,
        Response NVARCHAR(MAX),
        ResponsePathVector VARBINARY(MAX),
        ResponseAnswerVector VARBINARY(MAX),
        Confidence FLOAT,
        SampleTime DATETIME2
    );

    DECLARE @SampleId INT = 1;
    WHILE @SampleId <= @NumSamples
    BEGIN
        DECLARE @SampleResponse NVARCHAR(MAX);
        DECLARE @PathEmbedding VARBINARY(MAX);
        DECLARE @AnswerEmbedding VARBINARY(MAX);
        DECLARE @EmbeddingDim INT;

        EXEC dbo.sp_GenerateText
            @prompt = @Prompt,
            @max_tokens = 150,
            @temperature = @Temperature,
            @GeneratedText = @SampleResponse OUTPUT;

        EXEC dbo.sp_TextToEmbedding
            @text = @SampleResponse,
            @ModelName = NULL,
            @embedding = @PathEmbedding OUTPUT,
            @dimension = @EmbeddingDim OUTPUT;

        DECLARE @FinalAnswer NVARCHAR(MAX) = REVERSE(SUBSTRING(REVERSE(@SampleResponse), 1, CHARINDEX('.', REVERSE(@SampleResponse)) - 1));
        
        EXEC dbo.sp_TextToEmbedding
            @text = @FinalAnswer,
            @ModelName = NULL,
            @embedding = @AnswerEmbedding OUTPUT,
            @dimension = @EmbeddingDim OUTPUT;

        INSERT INTO @Samples (SampleId, Response, ResponsePathVector, ResponseAnswerVector, Confidence, SampleTime)
        VALUES (@SampleId, @SampleResponse, @PathEmbedding, @AnswerEmbedding, 0.8, SYSUTCDATETIME());

        SET @SampleId = @SampleId + 1;
    END

    DECLARE @ConsensusResult NVARCHAR(MAX);
    
    SELECT @ConsensusResult = dbo.SelfConsistency(
        CAST(ResponsePathVector AS NVARCHAR(MAX)),
        CAST(ResponseAnswerVector AS NVARCHAR(MAX)),
        Confidence
    )
    FROM @Samples;

    DECLARE @AgreementRatio FLOAT = TRY_CAST(JSON_VALUE(@ConsensusResult, '$.agreement_ratio') AS FLOAT);
    DECLARE @NumSupportingSamples INT = TRY_CAST(JSON_VALUE(@ConsensusResult, '$.num_supporting_samples') AS INT);
    DECLARE @AvgConfidence FLOAT = TRY_CAST(JSON_VALUE(@ConsensusResult, '$.avg_confidence') AS FLOAT);

    INSERT INTO dbo.SelfConsistencyResults (
        ProblemId,
        Prompt,
        NumSamples,
        ConsensusAnswer,
        AgreementRatio,
        SampleData,
        ConsensusMetrics,
        DurationMs,
        CreatedAt
    )
    VALUES (
        @ProblemId,
        @Prompt,
        @NumSamples,
        JSON_VALUE(@ConsensusResult, '$.consensus_answer'),
        @AgreementRatio,
        (SELECT * FROM @Samples FOR JSON PATH),
        TRY_CAST(@ConsensusResult AS JSON),
        DATEDIFF(MILLISECOND, @StartTime, SYSUTCDATETIME()),
        SYSUTCDATETIME()
    );

    SELECT
        @ProblemId AS ProblemId,
        'self_consistency' AS ReasoningType,
        JSON_VALUE(@ConsensusResult, '$.consensus_answer') AS ConsensusAnswer,
        @AgreementRatio AS AgreementRatio,
        @NumSupportingSamples AS NumSupportingSamples,
        @AvgConfidence AS AvgConfidence,
        SampleId,
        Response,
        Confidence,
        SampleTime,
        @ConsensusResult AS ConsensusMetrics
    FROM @Samples
    ORDER BY SampleId;

    IF @Debug = 1
    BEGIN
        PRINT 'Self-consistency reasoning completed with agreement ratio: ' + CAST(@AgreementRatio AS NVARCHAR(10));
        PRINT 'Consensus Metrics: ' + ISNULL(@ConsensusResult, 'N/A');
    END
END;
GO