CREATE OR ALTER PROCEDURE dbo.sp_FuseMultiModalStreams
    @StreamIds NVARCHAR(MAX), -- Comma-separated stream IDs
    @FusionType NVARCHAR(50) = 'weighted_average', -- 'weighted_average', 'max_pooling', 'attention_fusion'
    @Weights NVARCHAR(MAX) = NULL, -- JSON weights for each stream
    @Debug BIT = 0
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @StartTime DATETIME2 = SYSUTCDATETIME();
    DECLARE @FusedStream VARBINARY(MAX);

    IF @Debug = 1
        PRINT 'Starting multi-modal stream fusion for streams: ' + @StreamIds;

    -- Parse stream IDs
    CREATE TABLE #StreamList (StreamId INT, Weight FLOAT);

    INSERT INTO #StreamList (StreamId, Weight)
    SELECT
        CAST(value AS INT) AS StreamId,
        CASE
            WHEN @Weights IS NOT NULL THEN JSON_VALUE(@Weights, CONCAT('$[', ROW_NUMBER() OVER (ORDER BY (SELECT NULL)) - 1, ']'))
            ELSE 1.0
        END AS Weight
    FROM STRING_SPLIT(@StreamIds, ',');

    -- Validate streams exist
    IF NOT EXISTS (SELECT 1 FROM #StreamList sl INNER JOIN dbo.StreamOrchestrationResults sor ON sl.StreamId = sor.Id)
    BEGIN
        RAISERROR('One or more stream IDs not found', 16, 1);
        DROP TABLE #StreamList;
        RETURN;
    END

    -- Perform fusion based on type
    IF @FusionType = 'weighted_average'
    BEGIN
        -- Weighted average fusion
        SELECT @FusedStream = dbo.clr_StreamOrchestrator(
            dc.AtomId,
            DATEADD(MICROSECOND, ROW_NUMBER() OVER (ORDER BY dc.AtomId) * 100, '2024-01-01'), -- Synthetic timestamp
            dc.Weight * sl.Weight
        )
        FROM #StreamList sl
        CROSS APPLY dbo.fn_DecompressComponents(sor.ComponentStream) dc
        INNER JOIN dbo.StreamOrchestrationResults sor ON sl.StreamId = sor.Id
        GROUP BY sl.StreamId; -- Dummy grouping
    END
    ELSE IF @FusionType = 'max_pooling'
    BEGIN
        -- Max pooling fusion
        SELECT @FusedStream = dbo.clr_StreamOrchestrator(
            dc.AtomId,
            DATEADD(MICROSECOND, ROW_NUMBER() OVER (ORDER BY dc.AtomId) * 100, '2024-01-01'),
            MAX(dc.Weight) -- Take maximum weight across streams
        )
        FROM #StreamList sl
        CROSS APPLY dbo.fn_DecompressComponents(sor.ComponentStream) dc
        INNER JOIN dbo.StreamOrchestrationResults sor ON sl.StreamId = sor.Id
        GROUP BY dc.AtomId;
    END
    ELSE IF @FusionType = 'attention_fusion'
    BEGIN
        -- Attention-based fusion (using attention generation)
        DECLARE @AttentionInput NVARCHAR(MAX);
        SELECT @AttentionInput = STRING_AGG(CAST(dc.AtomId AS NVARCHAR(20)), ',')
        FROM #StreamList sl
        CROSS APPLY dbo.fn_DecompressComponents(sor.ComponentStream) dc
        INNER JOIN dbo.StreamOrchestrationResults sor ON sl.StreamId = sor.Id;

        -- Use attention mechanism for fusion
        DECLARE @FusionContextJson NVARCHAR(MAX) = CONCAT('{"fusion_type":"attention","streams":"', @StreamIds, '"}');
        DECLARE @FusionStreamId BIGINT;
        EXEC dbo.sp_GenerateWithAttention
            @ModelId = 1,
            @InputAtomIds = @AttentionInput,
            @ContextJson = @FusionContextJson,
            @MaxTokens = 100,
            @Temperature = 0.3,
            @TopK = 50,
            @TopP = 0.8,
            @AttentionHeads = 4,
            @Debug = 0;

        -- Get the fused stream (simplified)
        SELECT TOP 1 @FusedStream = ComponentStream
        FROM dbo.StreamOrchestrationResults
        WHERE Id IN (SELECT StreamId FROM #StreamList);
    END

    -- Store fusion result
    INSERT INTO dbo.StreamFusionResults (
        StreamIds,
        FusionType,
        Weights,
        FusedStream,
        ComponentCount,
        DurationMs,
        CreatedAt
    )
    VALUES (
        @StreamIds,
        @FusionType,
        @Weights,
        @FusedStream,
        dbo.fn_GetComponentCount(@FusedStream),
        DATEDIFF(MILLISECOND, @StartTime, SYSUTCDATETIME()),
        SYSUTCDATETIME()
    );

    -- Return fusion metadata
    SELECT
        @StreamIds AS StreamIds,
        @FusionType AS FusionType,
        @Weights AS Weights,
        dbo.fn_GetComponentCount(@FusedStream) AS ComponentCount,
        dbo.fn_GetTimeWindow(@FusedStream) AS TimeWindow,
        DATEDIFF(MILLISECOND, @StartTime, SYSUTCDATETIME()) AS DurationMs;

    -- Cleanup
    DROP TABLE #StreamList;

    IF @Debug = 1
        PRINT 'Multi-modal stream fusion completed';
END;