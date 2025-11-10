-- Stream Orchestration Procedures
-- Uses CLR stream orchestrator for real-time sensor fusion
-- Time-windowed atom accumulation with run-length encoding

-- sp_OrchestrateSensorStream: Real-time sensor data fusion
CREATE OR ALTER PROCEDURE dbo.sp_OrchestrateSensorStream
    @SensorType NVARCHAR(100),
    @TimeWindowStart DATETIME2,
    @TimeWindowEnd DATETIME2,
    @AggregationLevel NVARCHAR(50) = 'minute', -- 'second', 'minute', 'hour', 'day'
    @MaxComponents INT = 10000,
    @Debug BIT = 0
AS
BEGIN
    SET NOCOUNT ON;


    IF @Debug = 1
        PRINT 'Starting sensor stream orchestration for ' + @SensorType + ' from ' + CAST(@TimeWindowStart AS NVARCHAR(30)) + ' to ' + CAST(@TimeWindowEnd AS NVARCHAR(30));

    -- Validate time window
    IF @TimeWindowStart >= @TimeWindowEnd
    BEGIN
        RAISERROR('TimeWindowStart must be before TimeWindowEnd', 16, 1);
        RETURN;
    END

    -- Build dynamic query for time-bucketed aggregation

    SELECT
        ComponentStream = dbo.clr_StreamOrchestrator(AtomId, Timestamp, Weight)
    FROM (
        SELECT TOP (@MaxComponents)
            se.AtomId,
            se.Timestamp,
            se.Weight
        FROM dbo.SensorEvents se
        WHERE se.SensorType = @SensorType
        AND se.Timestamp BETWEEN @TimeWindowStart AND @TimeWindowEnd
        ORDER BY se.Timestamp
    ) AS WindowedEvents
    GROUP BY @SensorType; -- Dummy grouping for aggregate
    ';

    -- Execute the aggregation query

    EXEC sp_executesql @Query, @Params,
        @SensorType = @SensorType,
        @TimeWindowStart = @TimeWindowStart,
        @TimeWindowEnd = @TimeWindowEnd,
        @MaxComponents = @MaxComponents;

    -- Note: In practice, you'd capture the result from the dynamic query
    -- For now, we'll simulate getting the ComponentStream

    -- Store orchestration result
    

    -- Return orchestration metadata
    SELECT
        @SensorType AS SensorType,
        @TimeWindowStart AS TimeWindowStart,
        @TimeWindowEnd AS TimeWindowEnd,
        @AggregationLevel AS AggregationLevel,
        dbo.fn_GetComponentCount(@ComponentStream) AS ComponentCount,
        dbo.fn_GetTimeWindow(@ComponentStream) AS TimeWindow,
        DATEDIFF(MILLISECOND, @StartTime, SYSUTCDATETIME()) AS DurationMs;

    IF @Debug = 1
        END;

-- sp_FuseMultiModalStreams: Combine multiple sensor streams
CREATE OR ALTER PROCEDURE dbo.sp_FuseMultiModalStreams
    @StreamIds NVARCHAR(MAX), -- Comma-separated stream IDs
    @FusionType NVARCHAR(50) = 'weighted_average', -- 'weighted_average', 'max_pooling', 'attention_fusion'
    @Weights NVARCHAR(MAX) = NULL, -- JSON weights for each stream
    @Debug BIT = 0
AS
BEGIN
    SET NOCOUNT ON;


    IF @Debug = 1
        PRINT 'Starting multi-modal stream fusion for streams: ' + @StreamIds;

    -- Parse stream IDs
    CREATE TABLE #StreamList (StreamId INT, Weight FLOAT);

    

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

        SELECT @AttentionInput = STRING_AGG(CAST(dc.AtomId AS NVARCHAR(20)), ',')
        FROM #StreamList sl
        CROSS APPLY dbo.fn_DecompressComponents(sor.ComponentStream) dc
        INNER JOIN dbo.StreamOrchestrationResults sor ON sl.StreamId = sor.Id;

        -- Use attention mechanism for fusion


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
        END;

-- sp_GenerateEventsFromStream: Create event atoms from orchestrated streams
CREATE OR ALTER PROCEDURE dbo.sp_GenerateEventsFromStream
    @StreamId INT,
    @EventType NVARCHAR(100),
    @Threshold FLOAT = 0.5, -- Minimum weight threshold for event generation
    @Clustering NVARCHAR(50) = 'dbscan', -- 'dbscan', 'kmeans', 'hierarchical'
    @Debug BIT = 0
AS
BEGIN
    SET NOCOUNT ON;
