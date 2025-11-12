-- Auto-split from Attention.AttentionGeneration.sql
-- Object: PROCEDURE dbo.sp_TransformerStyleInference

CREATE PROCEDURE dbo.sp_TransformerStyleInference
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

    DECLARE @StartTime DATETIME2 = SYSUTCDATETIME();
    DECLARE @LayerResults TABLE (
        LayerNumber INT,
        AttentionOutput NVARCHAR(MAX),
        FeedForwardOutput NVARCHAR(MAX),
        LayerTime DATETIME2
    );

    IF @Debug = 1
        PRINT 'Starting transformer-style inference with ' + CAST(@Layers AS NVARCHAR(10)) + ' layers';

    -- Process through transformer layers
    DECLARE @CurrentInput NVARCHAR(MAX) = @InputSequence;
    DECLARE @Layer INT = 1;

    WHILE @Layer <= @Layers
    BEGIN
        DECLARE @LayerStartTime DATETIME2 = SYSUTCDATETIME();

        -- Multi-head attention (using our CLR function)
        DECLARE @LayerContextJson NVARCHAR(MAX) = CONCAT('{"layer":', CAST(@Layer AS NVARCHAR(10)), ',"type":"attention"}');
        DECLARE @AttentionStreamId BIGINT;
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
        DECLARE @AttentionOutput NVARCHAR(MAX);
        SELECT TOP 1 @AttentionOutput = GeneratedAtomIds
        FROM dbo.AttentionGenerationLog
        WHERE ModelId = @ModelId AND CreatedAt >= @LayerStartTime
        ORDER BY CreatedAt DESC;

        -- Feed-forward network (simplified - using text generation as proxy)
        DECLARE @FeedForwardPrompt NVARCHAR(MAX) = CONCAT('Process through feed-forward: ', @AttentionOutput);
        DECLARE @FeedForwardOutput NVARCHAR(MAX);
        EXEC dbo.sp_GenerateText
            @prompt = @FeedForwardPrompt,
            @max_tokens = 25,
            @temperature = 0.5,
            @GeneratedText = @FeedForwardOutput OUTPUT;

        -- Store layer result
        INSERT INTO @LayerResults (LayerNumber, AttentionOutput, FeedForwardOutput, LayerTime)
        VALUES (@Layer, @AttentionOutput, @FeedForwardOutput, @LayerStartTime);

        -- Update input for next layer
        SET @CurrentInput = @FeedForwardOutput;
        SET @Layer = @Layer + 1;
    END

    -- Store transformer inference result
    INSERT INTO dbo.TransformerInferenceResults (
        ProblemId,
        InputSequence,
        ModelId,
        Layers,
        AttentionHeads,
        FeedForwardDim,
        LayerResults,
        DurationMs,
        CreatedAt
    )
    VALUES (
        @ProblemId,
        @InputSequence,
        @ModelId,
        @Layers,
        @AttentionHeads,
        @FeedForwardDim,
        (SELECT * FROM @LayerResults FOR JSON PATH),
        DATEDIFF(MILLISECOND, @StartTime, SYSUTCDATETIME()),
        SYSUTCDATETIME()
    );

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
GO

PRINT 'Attention generation procedures created successfully';
PRINT 'sp_GenerateWithAttention: Core attention-based generation';
PRINT 'sp_AttentionInference: Multi-step attention reasoning';
PRINT 'sp_TransformerStyleInference: Full transformer pipeline';
GO