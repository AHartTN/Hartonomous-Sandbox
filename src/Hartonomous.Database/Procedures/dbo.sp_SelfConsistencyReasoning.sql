CREATE OR ALTER PROCEDURE dbo.sp_SelfConsistencyReasoning
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

    -- PARADIGM-COMPLIANT: Generate samples in a table, then use CLR aggregate for consensus
    DECLARE @Samples TABLE (
        SampleId INT,
        Response NVARCHAR(MAX),
        ResponsePathVector VECTOR(1536),
        ResponseAnswerVector VECTOR(1536),
        Confidence FLOAT,
        SampleTime DATETIME2
    );

    -- Generate multiple reasoning samples
    DECLARE @SampleId INT = 1;
    WHILE @SampleId <= @NumSamples
    BEGIN
        DECLARE @SampleResponse NVARCHAR(MAX);
        DECLARE @PathEmbedding VECTOR(1536);
        DECLARE @AnswerEmbedding VECTOR(1536);
        DECLARE @EmbeddingDim INT;

        EXEC dbo.sp_GenerateText
            @prompt = @Prompt,
            @max_tokens = 150,
            @temperature = @Temperature,
            @GeneratedText = @SampleResponse OUTPUT;

        -- Get path embedding (full reasoning)
        EXEC dbo.sp_TextToEmbedding
            @text = @SampleResponse,
            @ModelName = NULL,
            @embedding = @PathEmbedding OUTPUT,
            @dimension = @EmbeddingDim OUTPUT;

        -- Extract final answer (last sentence for simplicity)
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

    -- PARADIGM-COMPLIANT: Use CLR aggregate to find consensus
    DECLARE @ConsensusResult NVARCHAR(MAX);
    
    SELECT @ConsensusResult = dbo.SelfConsistency(
        CAST(ResponsePathVector AS NVARCHAR(MAX)),
        CAST(ResponseAnswerVector AS NVARCHAR(MAX)),
        Confidence
    )
    FROM @Samples;

    -- Extract consensus metrics from JSON
    DECLARE @AgreementRatio FLOAT = TRY_CAST(JSON_VALUE(@ConsensusResult, '$.agreement_ratio') AS FLOAT);
    DECLARE @NumSupportingSamples INT = TRY_CAST(JSON_VALUE(@ConsensusResult, '$.num_supporting_samples') AS INT);
    DECLARE @AvgConfidence FLOAT = TRY_CAST(JSON_VALUE(@ConsensusResult, '$.avg_confidence') AS FLOAT);

    -- Store self-consistency results
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
        @ConsensusResult,
        DATEDIFF(MILLISECOND, @StartTime, SYSUTCDATETIME()),
        SYSUTCDATETIME()
    );

    -- Return results
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