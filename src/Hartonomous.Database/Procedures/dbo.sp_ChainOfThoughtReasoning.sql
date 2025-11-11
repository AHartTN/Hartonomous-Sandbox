CREATE PROCEDURE dbo.sp_ChainOfThoughtReasoning
    @ProblemId UNIQUEIDENTIFIER,
    @InitialPrompt NVARCHAR(MAX),
    @MaxSteps INT = 5,
    @Temperature FLOAT = 0.7,
    @Debug BIT = 0
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @StartTime DATETIME2 = SYSUTCDATETIME();

    IF @Debug = 1
        PRINT 'Starting chain-of-thought reasoning for problem ' + CAST(@ProblemId AS NVARCHAR(36));

    DECLARE @ReasoningSteps TABLE (
        StepNumber INT,
        Prompt NVARCHAR(MAX),
        Response NVARCHAR(MAX),
        ResponseVector VARBINARY(MAX),
        Confidence FLOAT,
        StepTime DATETIME2
    );

    DECLARE @CurrentStep INT = 1;
    DECLARE @CurrentPrompt NVARCHAR(MAX) = @InitialPrompt;

    WHILE @CurrentStep <= @MaxSteps
    BEGIN
        DECLARE @StepStartTime DATETIME2 = SYSUTCDATETIME();
        DECLARE @StepResponse NVARCHAR(MAX);
        DECLARE @ResponseEmbedding VARBINARY(MAX);
        DECLARE @EmbeddingDim INT;

        EXEC dbo.sp_GenerateText
            @prompt = @CurrentPrompt,
            @max_tokens = 100,
            @temperature = @Temperature,
            @GeneratedText = @StepResponse OUTPUT;

        EXEC dbo.sp_TextToEmbedding
            @text = @StepResponse,
            @ModelName = NULL,
            @embedding = @ResponseEmbedding OUTPUT,
            @dimension = @EmbeddingDim OUTPUT;

        DECLARE @Confidence FLOAT = 0.8;

        INSERT INTO @ReasoningSteps (StepNumber, Prompt, Response, ResponseVector, Confidence, StepTime)
        VALUES (@CurrentStep, @CurrentPrompt, @StepResponse, @ResponseEmbedding, @Confidence, @StepStartTime);

        SET @CurrentPrompt = 'Continue reasoning: ' + @StepResponse;
        SET @CurrentStep = @CurrentStep + 1;

        IF @Debug = 1
            PRINT 'Completed step ' + CAST(@CurrentStep - 1 AS NVARCHAR(10));
    END

    DECLARE @CoherenceAnalysis NVARCHAR(MAX);
    
    SELECT @CoherenceAnalysis = dbo.ChainOfThoughtCoherence(
        StepNumber,
        CAST(ResponseVector AS NVARCHAR(MAX))
    )
    FROM @ReasoningSteps;

    INSERT INTO dbo.ReasoningChains (
        ProblemId,
        ReasoningType,
        ChainData,
        TotalSteps,
        DurationMs,
        CoherenceMetrics,
        CreatedAt
    )
    VALUES (
        @ProblemId,
        'chain_of_thought',
        (SELECT * FROM @ReasoningSteps FOR JSON PATH),
        @MaxSteps,
        DATEDIFF(MILLISECOND, @StartTime, SYSUTCDATETIME()),
        TRY_CAST(@CoherenceAnalysis AS JSON),
        SYSUTCDATETIME()
    );

    SELECT
        @ProblemId AS ProblemId,
        'chain_of_thought' AS ReasoningType,
        StepNumber,
        Prompt,
        Response,
        Confidence,
        StepTime,
        @CoherenceAnalysis AS CoherenceAnalysis
    FROM @ReasoningSteps
    ORDER BY StepNumber;

    IF @Debug = 1
    BEGIN
        PRINT 'Chain-of-thought reasoning completed';
        PRINT 'Coherence Analysis: ' + ISNULL(@CoherenceAnalysis, 'N/A');
    END
END;
GO