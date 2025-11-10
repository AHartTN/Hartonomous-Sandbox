-- Attention Generation Procedures
-- Uses CLR multi-head attention for transformer-style inference
-- Implements context-aware generation with provenance tracking

-- sp_GenerateWithAttention: High-level attention-based generation
CREATE OR ALTER PROCEDURE dbo.sp_GenerateWithAttention
    @ModelId INT,
    @InputAtomIds NVARCHAR(MAX), -- Comma-separated atom IDs
    @ContextJson NVARCHAR(MAX) = '{}',
    @MaxTokens INT = 100,
    @Temperature FLOAT = 1.0,
    @TopK INT = 50,
    @TopP FLOAT = 0.9,
    @AttentionHeads INT = 8,
    @TenantId INT = 0,
    @Debug BIT = 0
AS
BEGIN
    SET NOCOUNT ON;


    IF @Debug = 1
        PRINT 'Starting attention-based generation with model ' + CAST(@ModelId AS NVARCHAR(10));

    -- Validate inputs
    IF @ModelId IS NULL OR @ModelId <= 0
    BEGIN
        RAISERROR('Invalid ModelId: %d', 16, 1, @ModelId);
        RETURN;
    END

    IF @InputAtomIds IS NULL OR LEN(@InputAtomIds) = 0
    BEGIN
        RAISERROR('InputAtomIds cannot be empty', 16, 1);
        RETURN;
    END

    -- Call CLR attention generation function
    SELECT @GenerationStreamId = dbo.fn_GenerateWithAttention(
        @ModelId,
        @InputAtomIds,
        @ContextJson,
        @MaxTokens,
        @Temperature,
        @TopK,
        @TopP,
        @AttentionHeads,
        @TenantId
    );

    IF @GenerationStreamId IS NULL OR @GenerationStreamId <= 0
    BEGIN
        IF @Debug = 1
            RETURN;
    END

    -- Log the generation
    

    -- Return generation results
    SELECT
        @GenerationStreamId AS GenerationStreamId,
        @ModelId AS ModelId,
        @InputAtomIds AS InputAtomIds,
        @ContextJson AS ContextJson,
        @MaxTokens AS MaxTokens,
        @Temperature AS Temperature,
        @TopK AS TopK,
        @TopP AS TopP,
        @AttentionHeads AS AttentionHeads,
        DATEDIFF(MILLISECOND, @StartTime, SYSUTCDATETIME()) AS DurationMs;

    IF @Debug = 1
        PRINT 'Attention-based generation completed, stream ID: ' + CAST(@GenerationStreamId AS NVARCHAR(20));
END;

-- sp_AttentionInference: Multi-head attention inference for complex reasoning
CREATE OR ALTER PROCEDURE dbo.sp_AttentionInference
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


    WHILE @StepNumber <= @MaxReasoningSteps
    BEGIN

        -- Generate reasoning step using attention


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

        SELECT @GeneratedAtoms = GeneratedAtomIds
        FROM provenance.GenerationStreams
        WHERE GenerationStreamId = @StepStreamId;

        -- Store reasoning step
        

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

-- sp_TransformerStyleInference: Full transformer-style inference pipeline
CREATE OR ALTER PROCEDURE dbo.sp_TransformerStyleInference
    @ProblemId UNIQUEIDENTIFIER,
    @InputSequence NVARCHAR(MAX), -- Sequence of atom IDs or text
    @ModelId INT = 1,
    @Layers INT = 6, -- Number of transformer layers
    @AttentionHeads INT = 8,
    @FeedForwardDim INT = 2048,
    @Debug BIT = 0
AS
BEGIN
    SET NOCOUNT ON;


        LayerNumber INT,
        AttentionOutput NVARCHAR(MAX),
        FeedForwardOutput NVARCHAR(MAX),
        LayerTime DATETIME2
    );

    IF @Debug = 1
        PRINT 'Starting transformer-style inference with ' + CAST(@Layers AS NVARCHAR(10)) + ' layers';

    -- Process through transformer layers


    WHILE @Layer <= @Layers
    BEGIN

        -- Multi-head attention (using our CLR function)


        EXEC dbo.sp_GenerateWithAttention
            @ModelId = @ModelId,
            @InputAtomIds = @CurrentInput,
            @ContextJson = @LayerContextJson,
            @MaxTokens = 1, -- Single attention step per layer
            @Temperature = 0.1, -- Low temperature for deterministic attention
            @TopK = 10,
            @TopP = 0.5,
            @AttentionHeads = @AttentionHeads,
            @Debug = 0;

        -- Get attention output

        SELECT TOP 1 @AttentionOutput = GeneratedAtomIds
        FROM dbo.AttentionGenerationLog
        WHERE ModelId = @ModelId AND CreatedAt >= @LayerStartTime
        ORDER BY CreatedAt DESC;

        -- Feed-forward network (simplified - using text generation as proxy)


        EXEC dbo.sp_GenerateText
            @prompt = @FeedForwardPrompt,
            @max_tokens = 25,
            @temperature = 0.5,
            @GeneratedText = @FeedForwardOutput OUTPUT;

        -- Store layer result
        

        -- Update input for next layer
        SET @CurrentInput = @FeedForwardOutput;
        SET @Layer = @Layer + 1;
    END

    -- Store transformer inference result
    

    -- Return layer results
    SELECT
        @ProblemId AS ProblemId,
        'transformer_inference' AS InferenceType,
        LayerNumber,
        AttentionOutput,
        FeedForwardOutput,
        LayerTime
    FROM @LayerResults
    ORDER BY LayerNumber;

    IF @Debug = 1
        PRINT 'Transformer-style inference completed with ' + CAST(@Layers AS NVARCHAR(10)) + ' layers';
END;
