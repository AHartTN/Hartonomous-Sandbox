-- Auto-split from Attention.AttentionGeneration.sql
-- Object: PROCEDURE dbo.sp_GenerateWithAttention

CREATE PROCEDURE dbo.sp_GenerateWithAttention
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

    DECLARE @StartTime DATETIME2 = SYSUTCDATETIME();
    DECLARE @GenerationStreamId BIGINT;

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
            PRINT 'Attention generation failed - no stream returned';
        RETURN;
    END

    -- Log the generation
    INSERT INTO dbo.AttentionGenerationLog (
        ModelId,
        InputAtomIds,
        ContextJson,
        MaxTokens,
        Temperature,
        TopK,
        TopP,
        AttentionHeads,
        GenerationStreamId,
        DurationMs,
        TenantId,
        CreatedAt
    )
    VALUES (
        @ModelId,
        @InputAtomIds,
        @ContextJson,
        @MaxTokens,
        @Temperature,
        @TopK,
        @TopP,
        @AttentionHeads,
        @GenerationStreamId,
        DATEDIFF(MILLISECOND, @StartTime, SYSUTCDATETIME()),
        @TenantId,
        SYSUTCDATETIME()
    );

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
GO

-- sp_AttentionInference: Multi-head attention inference for complex reasoning

GO
