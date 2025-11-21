-- =============================================
-- sp_GenerateOptimalPath: A* Pathfinding for Semantic Generation
-- =============================================
-- Uses A* algorithm to find optimal path through semantic space
-- from start atom to target concept region
-- PHASE 1: Includes semantic path caching
-- IDEMPOTENT: Uses CREATE OR ALTER
-- =============================================

CREATE OR ALTER PROCEDURE [dbo].[sp_GenerateOptimalPath]
    @StartAtomId BIGINT,
    @TargetConceptId INT,
    @MaxSteps INT = 50,
    @NeighborRadius FLOAT = 0.5,  -- Spatial radius for neighbor search
    @CacheTTLMinutes INT = 60     -- Cache expiration time
AS
BEGIN
    SET NOCOUNT ON;

    -- ===== PHASE 1: CHECK CACHE FIRST =====
    DECLARE @CachedPathJson NVARCHAR(MAX);
    DECLARE @CachedCost FLOAT;

    SELECT 
        @CachedPathJson = PathJson,
        @CachedCost = TotalCost
    FROM provenance.SemanticPathCache
    WHERE StartAtomId = @StartAtomId
      AND TargetConceptId = @TargetConceptId
      AND ExpiresAt > SYSUTCDATETIME();

    IF @CachedPathJson IS NOT NULL
    BEGIN
        -- Update cache statistics
        UPDATE provenance.SemanticPathCache
        SET HitCount = HitCount + 1,
            LastAccessedAt = SYSUTCDATETIME()
        WHERE StartAtomId = @StartAtomId
          AND TargetConceptId = @TargetConceptId;

        -- Return cached path
        SELECT 
            [value] AS PathNode,
            @CachedCost AS TotalCost,
            1 AS FromCache
        FROM OPENJSON(@CachedPathJson);
        
        RETURN 0;
    END
    -- ===== END CACHE CHECK =====

    DECLARE @StartPoint GEOMETRY;
    DECLARE @TargetRegion GEOMETRY;
    DECLARE @TargetCentroid GEOMETRY;

    -- Get start point and target region
    SELECT @StartPoint = [SpatialKey]
    FROM [dbo].[AtomEmbedding]
    WHERE [AtomId] = @StartAtomId;

    SELECT 
        @TargetRegion = [ConceptDomain],
        @TargetCentroid = [CentroidSpatialKey]
    FROM [provenance].[Concepts]
    WHERE [ConceptId] = @TargetConceptId;

    IF @StartPoint IS NULL OR @TargetRegion IS NULL OR @TargetCentroid IS NULL
    BEGIN
        RAISERROR('Invalid start atom or target concept. Ensure concept has computed ConceptDomain.', 16, 1);
        RETURN -1;
    END

    -- A* data structures
    DECLARE @OpenSet TABLE (
        AtomId BIGINT PRIMARY KEY,
        ParentAtomId BIGINT,
        gCost FLOAT NOT NULL,      -- Cost from start
        hCost FLOAT NOT NULL,      -- Heuristic to goal
        fCost AS (gCost + hCost)   -- Total estimated cost
    );

    CREATE TABLE #ClosedSet (
        AtomId BIGINT PRIMARY KEY,
        ParentAtomId BIGINT
    );

    DECLARE @CurrentAtomId BIGINT;
    DECLARE @CurrentPoint GEOMETRY;
    DECLARE @gCost FLOAT;
    DECLARE @Steps INT = 0;
    DECLARE @GoalAtomId BIGINT = NULL;

    -- Initialize with start node
    INSERT INTO @OpenSet (AtomId, ParentAtomId, gCost, hCost)
    VALUES (
        @StartAtomId, 
        NULL, 
        0, 
        @StartPoint.STDistance(@TargetCentroid)
    );

    -- A* main loop
    WHILE (EXISTS (SELECT 1 FROM @OpenSet) AND @Steps < @MaxSteps AND @GoalAtomId IS NULL)
    BEGIN
        -- 1. Get node with lowest fCost from Open Set
        SELECT TOP 1 
            @CurrentAtomId = os.AtomId, 
            @gCost = os.gCost,
            @CurrentPoint = ae.[SpatialKey]
        FROM @OpenSet os
        JOIN [dbo].[AtomEmbedding] ae ON os.AtomId = ae.AtomId
        ORDER BY os.fCost ASC, os.hCost ASC; -- Tie-break with heuristic

        -- 2. Check if we've reached the goal (inside target concept domain)
        IF @CurrentPoint.STWithin(@TargetRegion) = 1
        BEGIN
            SET @GoalAtomId = @CurrentAtomId;
            BREAK;
        END

        -- 3. Move current node from Open to Closed
        INSERT INTO #ClosedSet (AtomId, ParentAtomId)
        SELECT AtomId, ParentAtomId 
        FROM @OpenSet 
        WHERE AtomId = @CurrentAtomId;
        
        DELETE FROM @OpenSet WHERE AtomId = @CurrentAtomId;

        -- 4. Find neighbors using spatial index
        DECLARE @NeighborSearchRegion GEOMETRY = @CurrentPoint.STBuffer(@NeighborRadius);

        -- 5. Process each neighbor
        ;WITH Neighbors AS (
            SELECT
                ae.AtomId,
                ae.SpatialKey,
                @CurrentPoint.STDistance(ae.SpatialKey) AS StepCost,
                ae.SpatialKey.STDistance(@TargetCentroid) AS HeuristicCost
            FROM dbo.AtomEmbedding ae WITH(INDEX(SIX_AtomEmbedding_SpatialKey))
            WHERE ae.SpatialKey.STIntersects(@NeighborSearchRegion) = 1
              AND ae.AtomId <> @CurrentAtomId
              AND NOT EXISTS (SELECT 1 FROM #ClosedSet WHERE AtomId = ae.AtomId)
        )
        -- 6. Merge neighbors into Open Set (update if better path found)
        MERGE @OpenSet AS T
        USING Neighbors AS S
        ON T.AtomId = S.AtomId
        WHEN MATCHED AND (S.StepCost + @gCost) < T.gCost THEN
            -- Found a better path to this node
            UPDATE SET
                T.ParentAtomId = @CurrentAtomId,
                T.gCost = S.StepCost + @gCost,
                T.hCost = S.HeuristicCost
        WHEN NOT MATCHED BY TARGET THEN
            -- New node discovered
            INSERT (AtomId, ParentAtomId, gCost, hCost)
            VALUES (S.AtomId, @CurrentAtomId, S.StepCost + @gCost, S.HeuristicCost);

        SET @Steps = @Steps + 1;
    END

    -- 7. Reconstruct and return path
    IF @GoalAtomId IS NOT NULL
    BEGIN
        -- Add goal to closed set
        INSERT INTO #ClosedSet (AtomId, ParentAtomId)
        SELECT AtomId, ParentAtomId 
        FROM @OpenSet 
        WHERE AtomId = @GoalAtomId;

        -- Calculate total path cost
        DECLARE @TotalPathCost FLOAT;
        SELECT @TotalPathCost = gCost 
        FROM @OpenSet 
        WHERE AtomId = @GoalAtomId;

        -- Reconstruct path using recursive CTE
        ;WITH PathCTE AS (
            -- Start from goal
            SELECT AtomId, ParentAtomId, 0 AS Depth
            FROM #ClosedSet
            WHERE AtomId = @GoalAtomId
            
            UNION ALL
            
            -- Walk backwards to start
            SELECT cs.AtomId, cs.ParentAtomId, p.Depth + 1
            FROM #ClosedSet cs
            JOIN PathCTE p ON cs.AtomId = p.ParentAtomId
            WHERE p.ParentAtomId IS NOT NULL
        ),
        PathResults AS (
            SELECT 
                p.Depth AS StepNumber,
                p.AtomId,
                a.Modality,
                a.Subtype,
                a.[AtomicValue],
                ae.[SpatialKey].ToString() AS SpatialPosition,
                ae.[SpatialKey].STDistance(@TargetCentroid) AS DistanceToGoal
            FROM PathCTE p
            JOIN dbo.Atom a ON p.AtomId = a.AtomId
            LEFT JOIN dbo.AtomEmbedding ae ON p.AtomId = ae.AtomId
        )
        -- ===== PHASE 1: CACHE THE PATH RESULT =====
        , PathJson AS (
            SELECT 
                (SELECT * FROM PathResults FOR JSON PATH) AS JsonPath
        )
        INSERT INTO provenance.SemanticPathCache (
            StartAtomId,
            TargetConceptId,
            PathJson,
            TotalCost,
            CreatedAt,
            ExpiresAt
        )
        SELECT 
            @StartAtomId,
            @TargetConceptId,
            JsonPath,
            @TotalPathCost,
            SYSUTCDATETIME(),
            DATEADD(MINUTE, @CacheTTLMinutes, SYSUTCDATETIME())
        FROM PathJson;
        -- ===== END CACHE WRITE =====

        -- Return the path
        SELECT * FROM PathResults
        ORDER BY StepNumber ASC; -- Start to goal order
    END
    ELSE
    BEGIN
        -- No path found
        RAISERROR('No path to target concept found within MaxSteps limit.', 10, 1);
        SELECT NULL AS StepNumber, NULL AS AtomId, NULL AS Modality;
    END

    DROP TABLE #ClosedSet;
    RETURN 0;
END
GO
