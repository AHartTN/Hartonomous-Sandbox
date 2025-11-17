-- Auto-split from Attention.AttentionGeneration.sql
-- Object: PROCEDURE dbo.sp_AttentionInference

CREATE PROCEDURE dbo.sp_AttentionInference
    @ProblemId UNIQUEIDENTIFIER,
    @ContextAtoms NVARCHAR(MAX), -- Atom IDs providing context
    @Query NVARCHAR(MAX), -- The query/problem to solve
    @ModelId INT = 1,
    @MaxReasoningSteps INT = 10,
    @AttentionHeads INT = 8,
    @Debug BIT = 0
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @StartTime DATETIME2 = SYSUTCDATETIME();
    DECLARE @ReasoningSteps TABLE (
        StepNumber INT,
        GenerationStreamId BIGINT,
        ContextUsed NVARCHAR(MAX),
        Reasoning NVARCHAR(MAX),
        Confidence FLOAT,
        StepTime DATETIME2
    );

    IF @Debug = 1
        PRINT 'Starting attention inference for problem ' + CAST(@ProblemId AS NVARCHAR(36));

    -- Initial context preparation
    DECLARE @CurrentContext NVARCHAR(MAX) = @ContextAtoms;
    DECLARE @StepNumber INT = 1;

    WHILE @StepNumber <= @MaxReasoningSteps
    BEGIN
        DECLARE @StepStartTime DATETIME2 = SYSUTCDATETIME();

        -- Generate reasoning step using attention
        DECLARE @StepContextJson NVARCHAR(MAX) = CONCAT('{"step":', CAST(@StepNumber AS NVARCHAR(10)), ',"query":"', REPLACE(@Query, '"', '\"'), '"}');
        DECLARE @StepStreamId BIGINT;
        EXEC dbo.sp_GenerateWithAttention
            @ModelId = @ModelId,
            @InputAtomIds = @CurrentContext,
            @ContextJson = @StepContextJson,
            @MaxTokens = 50,
            @Temperature = 0.8,
            @TopK = 40,
            @TopP = 0.85,
            @AttentionHeads = @AttentionHeads,
            @Debug = 0;

        -- Get the generated stream ID (this is a simplification - in practice need to capture from sp_GenerateWithAttention)
        SELECT TOP 1 @StepStreamId = GenerationStreamId
        FROM dbo.AttentionGenerationLog
        WHERE ModelId = @ModelId AND CreatedAt >= @StepStartTime
        ORDER BY CreatedAt DESC;

        IF @StepStreamId IS NULL
        BEGIN
            IF @Debug = 1
                PRINT 'No generation stream found for step ' + CAST(@StepNumber AS NVARCHAR(10));
            BREAK;
        END

        -- Get generated atoms for this step
        DECLARE @GeneratedAtoms NVARCHAR(MAX);
        SELECT @GeneratedAtoms = CAST(GeneratedAtomIds AS NVARCHAR(MAX))
        FROM provenance.GenerationStreams
        WHERE GenerationStreamId = @StepStreamId;

        -- Store reasoning step
        INSERT INTO @ReasoningSteps (StepNumber, GenerationStreamId, ContextUsed, Reasoning, Confidence, StepTime)
        VALUES (@StepNumber, @StepStreamId, @CurrentContext, @GeneratedAtoms, 0.8, @StepStartTime);

        -- Update context for next step
        SET @CurrentContext = @CurrentContext + ',' + @GeneratedAtoms;
        SET @StepNumber = @StepNumber + 1;

        -- Check for convergence (simplified)
        IF LEN(@GeneratedAtoms) < 10 -- Very short response might indicate completion
        BEGIN
            BREAK;
        END
    END

    -- Store final attention inference result
    INSERT INTO dbo.AttentionInferenceResults (
        ProblemId,
        Query,
        ModelId,
        MaxReasoningSteps,
        AttentionHeads,
        ReasoningSteps,
        TotalSteps,
        DurationMs,
        CreatedAt
    )
    VALUES (
        @ProblemId,
        @Query,
        @ModelId,
        @MaxReasoningSteps,
        @AttentionHeads,
        (SELECT * FROM @ReasoningSteps FOR JSON PATH),
        @StepNumber - 1,
        DATEDIFF(MILLISECOND, @StartTime, SYSUTCDATETIME()),
        SYSUTCDATETIME()
    );

    -- Return reasoning steps
    SELECT
        @ProblemId AS ProblemId,
        'attention_inference' AS InferenceType,
        StepNumber,
        GenerationStreamId,
        ContextUsed,
        Reasoning,
        Confidence,
        StepTime
    FROM @ReasoningSteps
    ORDER BY StepNumber;

    IF @Debug = 1
        PRINT 'Attention inference completed with ' + CAST(@StepNumber - 1 AS NVARCHAR(10)) + ' steps';
END;
GO

-- sp_TransformerStyleInference: Full transformer-style inference pipeline

GO
