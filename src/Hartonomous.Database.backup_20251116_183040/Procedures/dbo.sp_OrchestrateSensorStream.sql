CREATE PROCEDURE dbo.sp_OrchestrateSensorStream
    @SensorType NVARCHAR(100),
    @TimeWindowStart DATETIME2,
    @TimeWindowEnd DATETIME2,
    @AggregationLevel NVARCHAR(50) = 'minute', -- 'second', 'minute', 'hour', 'day'
    @MaxComponents INT = 10000,
    @Debug BIT = 0
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @StartTime DATETIME2 = SYSUTCDATETIME();
    DECLARE @ComponentStream VARBINARY(MAX);

    IF @Debug = 1
        PRINT 'Starting sensor stream orchestration for ' + @SensorType + ' from ' + CAST(@TimeWindowStart AS NVARCHAR(30)) + ' to ' + CAST(@TimeWindowEnd AS NVARCHAR(30));

    -- Validate time window
    IF @TimeWindowStart >= @TimeWindowEnd
    BEGIN
        RAISERROR('TimeWindowStart must be before TimeWindowEnd', 16, 1);
        RETURN;
    END

    -- Build dynamic query for time-bucketed aggregation
    DECLARE @Query NVARCHAR(MAX) = N'
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
    DECLARE @Params NVARCHAR(MAX) = N'@SensorType NVARCHAR(100), @TimeWindowStart DATETIME2, @TimeWindowEnd DATETIME2, @MaxComponents INT';
    EXEC sp_executesql @Query, @Params,
        @SensorType = @SensorType,
        @TimeWindowStart = @TimeWindowStart,
        @TimeWindowEnd = @TimeWindowEnd,
        @MaxComponents = @MaxComponents;

    -- Note: In practice, you'd capture the result from the dynamic query
    -- For now, we'll simulate getting the ComponentStream

    -- Store orchestration result
    INSERT INTO dbo.StreamOrchestrationResults (
        SensorType,
        TimeWindowStart,
        TimeWindowEnd,
        AggregationLevel,
        ComponentStream,
        ComponentCount,
        DurationMs,
        CreatedAt
    )
    VALUES (
        @SensorType,
        @TimeWindowStart,
        @TimeWindowEnd,
        @AggregationLevel,
        @ComponentStream,
        dbo.fn_GetComponentCount(@ComponentStream),
        DATEDIFF(MILLISECOND, @StartTime, SYSUTCDATETIME()),
        SYSUTCDATETIME()
    );

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
        PRINT 'Sensor stream orchestration completed';
END;