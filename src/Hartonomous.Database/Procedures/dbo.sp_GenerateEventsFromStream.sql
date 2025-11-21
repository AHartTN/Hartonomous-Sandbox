CREATE OR ALTER PROCEDURE dbo.sp_GenerateEventsFromStream
    @StreamId INT,
    @EventType NVARCHAR(100),
    @Threshold FLOAT = 0.5, -- Minimum weight threshold for event generation
    @Clustering NVARCHAR(50) = 'dbscan', -- 'dbscan', 'kmeans', 'hierarchical'
    @Debug BIT = 0
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @StartTime DATETIME2 = SYSUTCDATETIME();
    DECLARE @EventAtoms TABLE (AtomId BIGINT, Weight FLOAT, ClusterId INT);

    IF @Debug = 1
        PRINT 'Generating events from stream ' + CAST(@StreamId AS NVARCHAR(10)) + ' with threshold ' + CAST(@Threshold AS NVARCHAR(10));

    -- Validate stream exists
    IF NOT EXISTS (SELECT 1 FROM dbo.StreamOrchestrationResults WHERE Id = @StreamId)
    BEGIN
        RAISERROR('Stream ID not found', 16, 1);
        RETURN;
    END

    -- Get stream data
    DECLARE @ComponentStream VARBINARY(MAX);
    SELECT @ComponentStream = ComponentStream
    FROM dbo.StreamOrchestrationResults
    WHERE Id = @StreamId;

    -- Decompress and filter components
    INSERT INTO @EventAtoms (AtomId, Weight)
    SELECT dc.AtomId, dc.Weight
    FROM dbo.fn_DecompressComponents(@ComponentStream) dc
    WHERE dc.Weight >= @Threshold;

    -- Apply clustering to identify event patterns
    IF @Clustering = 'dbscan'
    BEGIN
        -- Use DBSCAN clustering from concept discovery
        UPDATE @EventAtoms
        SET ClusterId = dbo.fn_DiscoverConcepts(AtomId, Weight, 0.3, 3); -- eps=0.3, minPts=3
    END
    ELSE IF @Clustering = 'kmeans'
    BEGIN
        -- Simplified k-means (would need full implementation)
        UPDATE @EventAtoms
        SET ClusterId = ABS(AtomId) % 5; -- Simple hash-based clustering
    END

    -- Generate event atoms for each cluster
    DECLARE @EventCount INT = 0;
    DECLARE @ClusterId INT;

    DECLARE cluster_cursor CURSOR LOCAL FAST_FORWARD FOR
    SELECT DISTINCT ClusterId FROM @EventAtoms WHERE ClusterId IS NOT NULL;

    OPEN cluster_cursor;
    FETCH NEXT FROM cluster_cursor INTO @ClusterId;

    WHILE @@FETCH_STATUS = 0
    BEGIN
        -- Calculate cluster centroid
        DECLARE @CentroidAtomId BIGINT;
        DECLARE @AvgWeight FLOAT;
        DECLARE @ClusterSize INT;

        SELECT
            @CentroidAtomId = AVG(AtomId),
            @AvgWeight = AVG(Weight),
            @ClusterSize = COUNT(*)
        FROM @EventAtoms
        WHERE ClusterId = @ClusterId;

        -- Create event atom (simplified - would integrate with atom creation)
        INSERT INTO dbo.EventAtoms (
            StreamId,
            EventType,
            CentroidAtomId,
            AverageWeight,
            ClusterSize,
            ClusterId,
            CreatedAt
        )
        VALUES (
            @StreamId,
            @EventType,
            @CentroidAtomId,
            @AvgWeight,
            @ClusterSize,
            @ClusterId,
            SYSUTCDATETIME()
        );

        SET @EventCount = @EventCount + 1;
        FETCH NEXT FROM cluster_cursor INTO @ClusterId;
    END

    CLOSE cluster_cursor;
    DEALLOCATE cluster_cursor;

    -- Store event generation result
    INSERT INTO dbo.EventGenerationResults (
        StreamId,
        EventType,
        Threshold,
        ClusteringMethod,
        EventsGenerated,
        DurationMs,
        CreatedAt
    )
    VALUES (
        @StreamId,
        @EventType,
        @Threshold,
        @Clustering,
        @EventCount,
        DATEDIFF(MILLISECOND, @StartTime, SYSUTCDATETIME()),
        SYSUTCDATETIME()
    );

    -- Return event generation summary
    SELECT
        @StreamId AS StreamId,
        @EventType AS EventType,
        @Threshold AS Threshold,
        @Clustering AS ClusteringMethod,
        @EventCount AS EventsGenerated,
        DATEDIFF(MILLISECOND, @StartTime, SYSUTCDATETIME()) AS DurationMs;

    IF @Debug = 1
        PRINT 'Event generation completed, created ' + CAST(@EventCount AS NVARCHAR(10)) + ' events';
END;