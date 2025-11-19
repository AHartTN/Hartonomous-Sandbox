# 23 - Behavioral Analysis: User Journeys as Geometry

This document provides complete technical specifications for Hartonomous's geometric behavioral analysis system.

## Overview

**Traditional UX Analytics**: Separate tools (Google Analytics, Mixpanel), event logs, manual analysis
**Hartonomous Behavioral Analysis**: User journeys stored as GEOMETRY, automatic UX issue detection via OODA loop

**Core Concept**: User behavior IS part of the semantic space
- Each user action → 3D semantic position
- User journey → LINESTRING in geometric space
- UX issues → Geometric patterns (error regions, long failing paths)

---

## Part 1: SessionPaths as GEOMETRY

### Schema

```sql
CREATE TABLE dbo.SessionPaths (
    SessionPathId BIGINT IDENTITY PRIMARY KEY,
    SessionId UNIQUEIDENTIFIER NOT NULL UNIQUE,
    UserId INT,  -- Optional: link to users table
    Path GEOMETRY NOT NULL,  -- LINESTRING: user's journey through semantic space
    PathLength AS (Path.STLength()) PERSISTED,
    StartTime AS (Path.STPointN(1).M) PERSISTED,
    EndTime AS (Path.STPointN(Path.STNumPoints()).M) PERSISTED,
    CreatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    CompletedAt DATETIME2
);

-- Spatial index for path analysis
CREATE SPATIAL INDEX IX_SessionPaths_Path
ON dbo.SessionPaths (Path)
WITH (
    BOUNDING_BOX = (-1000, -1000, 1000, 1000),
    GRIDS = (LEVEL_1 = MEDIUM, LEVEL_2 = MEDIUM, LEVEL_3 = MEDIUM, LEVEL_4 = MEDIUM)
);

-- Index for time-based queries
CREATE NONCLUSTERED INDEX IX_SessionPaths_Times
ON dbo.SessionPaths (StartTime, EndTime) INCLUDE (PathLength);
```

### GEOMETRY Structure

**LINESTRING with M coordinate (timestamp)**:

```
LINESTRING (X Y Z M, X Y Z M, X Y Z M, ...)

where:
- X, Y, Z = semantic position in 3D space
- M = timestamp (UNIX milliseconds)
```

**Example**:

```sql
-- User journey: Home page → Search → Product page → Checkout → Error
geometry::STGeomFromText('
    LINESTRING (
        10 20 5 1700000000000,   -- Home page at t=0
        15 25 8 1700000060000,   -- Search page at t=60s
        18 30 12 1700000120000,  -- Product page at t=120s
        20 35 15 1700000180000,  -- Checkout at t=180s
        5 10 2 1700000240000     -- Error region at t=240s
    )', 0)
```

---

## Part 2: Tracking User Behavior

### Step 1: Map User Actions to Semantic Space

Each user action (page view, click, search query) has a semantic embedding:

```sql
CREATE TABLE dbo.UserActions (
    ActionId BIGINT IDENTITY PRIMARY KEY,
    SessionId UNIQUEIDENTIFIER NOT NULL,
    ActionType NVARCHAR(100),   -- 'PageView', 'Search', 'Click', 'Error'
    ActionData NVARCHAR(MAX),   -- e.g., page URL, search query, element ID
    ActionEmbedding VARBINARY(MAX),  -- High-dimensional embedding
    ActionGeometry GEOMETRY,    -- 3D projected position
    ActionTimestamp DATETIME2 NOT NULL,
    ActionTimestampMs AS (DATEDIFF_BIG(MILLISECOND, '1970-01-01', ActionTimestamp)) PERSISTED
);
```

### Step 2: Embed Actions

```sql
CREATE PROCEDURE dbo.sp_TrackUserAction
    @SessionId UNIQUEIDENTIFIER,
    @ActionType NVARCHAR(100),
    @ActionData NVARCHAR(MAX)
AS
BEGIN
    -- Generate embedding for action
    DECLARE @ActionEmbedding VARBINARY(MAX);

    EXEC dbo.sp_GenerateEmbedding
        @Text = @ActionData,
        @EmbeddingVector = @ActionEmbedding OUTPUT;

    -- Project to 3D geometry
    DECLARE @ActionGeometry GEOMETRY = dbo.fn_ProjectTo3D(@ActionEmbedding);

    -- Store action
    INSERT INTO dbo.UserActions (SessionId, ActionType, ActionData, ActionEmbedding, ActionGeometry, ActionTimestamp)
    VALUES (@SessionId, @ActionType, @ActionData, @ActionEmbedding, @ActionGeometry, GETUTCDATE());
END
```

### Step 3: Build SessionPath LINESTRING

```sql
CREATE PROCEDURE dbo.sp_BuildSessionPath
    @SessionId UNIQUEIDENTIFIER
AS
BEGIN
    -- Construct LINESTRING from all actions in session
    DECLARE @PathWKT NVARCHAR(MAX) = 'LINESTRING (';

    SELECT @PathWKT = @PathWKT +
        CAST(ActionGeometry.STX AS NVARCHAR(50)) + ' ' +
        CAST(ActionGeometry.STY AS NVARCHAR(50)) + ' ' +
        CAST(ActionGeometry.STZ AS NVARCHAR(50)) + ' ' +
        CAST(ActionTimestampMs AS NVARCHAR(50)) + ', '
    FROM dbo.UserActions
    WHERE SessionId = @SessionId
    ORDER BY ActionTimestamp;

    -- Remove trailing comma and close
    SET @PathWKT = LEFT(@PathWKT, LEN(@PathWKT) - 2) + ')';

    -- Create or update SessionPath
    MERGE dbo.SessionPaths AS target
    USING (SELECT @SessionId AS SessionId) AS source
    ON target.SessionId = source.SessionId
    WHEN MATCHED THEN
        UPDATE SET Path = geometry::STGeomFromText(@PathWKT, 0), CompletedAt = GETUTCDATE()
    WHEN NOT MATCHED THEN
        INSERT (SessionId, Path, CreatedAt)
        VALUES (@SessionId, geometry::STGeomFromText(@PathWKT, 0), GETUTCDATE());
END
```

### Usage Example

```sql
-- User session starts
DECLARE @SessionId UNIQUEIDENTIFIER = NEWID();

-- Track actions as user navigates
EXEC dbo.sp_TrackUserAction @SessionId, 'PageView', '/home';
WAITFOR DELAY '00:00:05';

EXEC dbo.sp_TrackUserAction @SessionId, 'Search', 'wireless headphones';
WAITFOR DELAY '00:00:03';

EXEC dbo.sp_TrackUserAction @SessionId, 'PageView', '/product/123';
WAITFOR DELAY '00:00:10';

EXEC dbo.sp_TrackUserAction @SessionId, 'Click', 'add-to-cart';
WAITFOR DELAY '00:00:02';

EXEC dbo.sp_TrackUserAction @SessionId, 'PageView', '/checkout';
WAITFOR DELAY '00:00:15';

EXEC dbo.sp_TrackUserAction @SessionId, 'Error', 'Payment failed: invalid card';

-- Build path
EXEC dbo.sp_BuildSessionPath @SessionId;

-- Query result
SELECT
    SessionId,
    Path.ToString() AS PathWKT,
    PathLength,
    Path.STNumPoints() AS NumActions,
    DATEDIFF(SECOND, StartTime, EndTime) AS DurationSeconds
FROM dbo.SessionPaths
WHERE SessionId = @SessionId;
```

---

## Part 3: Geometric Path Analysis

### Query 1: Sessions Ending in Error Regions

**Concept**: Define error region as geometric area, find sessions ending there

```sql
-- Define error region (could be computed from historical error locations)
DECLARE @ErrorRegion GEOMETRY = geometry::Point(5, 10, 2, 0).STBuffer(15);

-- Find sessions that ended in error region
SELECT
    sp.SessionId,
    sp.Path.STEndPoint().ToString() AS EndPoint,
    sp.PathLength,
    DATEDIFF(SECOND, sp.StartTime, sp.EndTime) AS DurationSeconds,
    sp.Path.STNumPoints() AS NumActions
FROM dbo.SessionPaths sp
WHERE sp.Path.STEndPoint().STIntersects(@ErrorRegion) = 1
    AND sp.CompletedAt >= DATEADD(DAY, -7, GETUTCDATE())
ORDER BY sp.PathLength DESC;  -- Longest failing journeys first
```

**Result**: Sessions that struggled (many actions) before hitting error

### Query 2: Common Failure Patterns

```sql
-- Find clusters of session endpoints (indicates common failure points)
WITH SessionEndpoints AS (
    SELECT
        SessionPathId,
        Path.STEndPoint() AS Endpoint,
        Path.STEndPoint().STX AS EndX,
        Path.STEndPoint().STY AS EndY,
        Path.STEndPoint().STZ AS EndZ
    FROM dbo.SessionPaths
    WHERE CompletedAt >= DATEADD(DAY, -7, GETUTCDATE())
        AND PathLength > 100  -- Filter for long sessions
)
SELECT
    CAST(EndX / 10 AS INT) * 10 AS ClusterX,  -- Bucket by 10-unit grid
    CAST(EndY / 10 AS INT) * 10 AS ClusterY,
    CAST(EndZ / 10 AS INT) * 10 AS ClusterZ,
    COUNT(*) AS SessionCount,
    AVG(PathLength) AS AvgPathLength
FROM SessionEndpoints
GROUP BY CAST(EndX / 10 AS INT), CAST(EndY / 10 AS INT), CAST(EndZ / 10 AS INT)
HAVING COUNT(*) > 10  -- Significant cluster
ORDER BY SessionCount DESC;
```

**Result**: Hotspots where users frequently end (potential UX issues)

### Query 3: Abnormally Long Journeys

```sql
-- Find sessions with excessive path length (user confusion)
DECLARE @AvgPathLength FLOAT = (
    SELECT AVG(PathLength)
    FROM dbo.SessionPaths
    WHERE CompletedAt >= DATEADD(DAY, -30, GETUTCDATE())
);

SELECT
    SessionId,
    PathLength,
    PathLength / @AvgPathLength AS LengthRatio,
    DATEDIFF(SECOND, StartTime, EndTime) AS DurationSeconds,
    Path.STNumPoints() AS NumActions
FROM dbo.SessionPaths
WHERE PathLength > @AvgPathLength * 3  -- 3x longer than average
    AND CompletedAt >= DATEADD(DAY, -7, GETUTCDATE())
ORDER BY PathLength DESC;
```

**Result**: Users who took unusually long paths (indicates confusion or friction)

### Query 4: Path Similarity (Find Similar Journeys)

```sql
-- Find sessions with similar geometric paths
DECLARE @TargetSessionId UNIQUEIDENTIFIER = '...';

DECLARE @TargetPath GEOMETRY = (
    SELECT Path FROM dbo.SessionPaths WHERE SessionId = @TargetSessionId
);

SELECT TOP 10
    SessionId,
    Path.STDistance(@TargetPath) AS PathDistance,  -- Hausdorff distance
    PathLength,
    Path.STNumPoints() AS NumActions
FROM dbo.SessionPaths
WHERE SessionId != @TargetSessionId
    AND CompletedAt >= DATEADD(DAY, -30, GETUTCDATE())
ORDER BY Path.STDistance(@TargetPath);
```

**Result**: Sessions with similar geometric trajectories (common user flows)

---

## Part 4: OODA Loop Integration

### sp_Hypothesize: UX Issue Detection

**Implementation**: sp_Hypothesize.sql lines 239-258 - ACTUAL CODE

```sql
-- sp_Hypothesize.sql:239-258
-- Detect UX issues via geometric path analysis

DECLARE @errorRegion GEOMETRY = geometry::Point(0, 0, 0, 0).STBuffer(10);

DECLARE @failingSessions NVARCHAR(MAX) = (
    SELECT TOP 10
        sp.SessionId,
        sp.Path.STEndPoint().ToString() AS EndPoint,
        sp.PathLength AS JourneyLength,
        DATEDIFF(SECOND,
            sp.Path.STPointN(1).M,
            sp.Path.STPointN(sp.Path.STNumPoints()).M
        ) AS DurationSeconds,
        sp.Path.STNumPoints() AS ActionCount
    FROM dbo.SessionPaths sp
    WHERE sp.Path.STEndPoint().STIntersects(@errorRegion) = 1
        AND sp.PathLength > 50  -- Long, failing journeys
        AND sp.CompletedAt >= DATEADD(DAY, -7, GETUTCDATE())
    ORDER BY sp.PathLength DESC
    FOR JSON PATH
);

IF @failingSessions IS NOT NULL
BEGIN
    INSERT INTO @HypothesisList (HypothesisType, Priority, Description, RequiredActions)
    VALUES (
        'FixUX',
        7,
        'User sessions ending in error regions detected via geometric path analysis. ' +
        'Long journeys suggest UX friction before failure.',
        @failingSessions
    );
END
```

### sp_Act: Generate UX Improvement Recommendations

```sql
-- sp_Act executes FixUX hypothesis
IF @CurrentType = 'FixUX'
BEGIN
    -- Parse failing sessions
    DECLARE @FailingSessionsTable TABLE (
        SessionId UNIQUEIDENTIFIER,
        EndPoint NVARCHAR(MAX),
        JourneyLength FLOAT,
        DurationSeconds INT,
        ActionCount INT
    );

    INSERT INTO @FailingSessionsTable
    SELECT
        SessionId,
        JSON_VALUE(value, '$.EndPoint'),
        JSON_VALUE(value, '$.JourneyLength'),
        JSON_VALUE(value, '$.DurationSeconds'),
        JSON_VALUE(value, '$.ActionCount')
    FROM OPENJSON(@CurrentActions, '$.failingSessions');

    -- Analyze common action sequences in failing paths
    DECLARE @CommonFailurePattern NVARCHAR(MAX);

    WITH FailingActions AS (
        SELECT
            ua.SessionId,
            ua.ActionType,
            ua.ActionData,
            ROW_NUMBER() OVER (PARTITION BY ua.SessionId ORDER BY ua.ActionTimestamp) AS ActionSeq
        FROM dbo.UserActions ua
        INNER JOIN @FailingSessionsTable fst ON ua.SessionId = fst.SessionId
    )
    SELECT TOP 1 @CommonFailurePattern =
        STRING_AGG(ActionType + ':' + ActionData, ' → ')
        WITHIN GROUP (ORDER BY ActionSeq)
    FROM FailingActions
    GROUP BY SessionId
    HAVING COUNT(*) > 5
    ORDER BY COUNT(*) DESC;

    -- Log recommendation
    INSERT INTO dbo.UXImprovementRecommendations (
        HypothesisId,
        IssueType,
        FailurePattern,
        AffectedSessions,
        RecommendedAction,
        CreatedAt
    )
    VALUES (
        @CurrentHypothesisId,
        'GeometricPathFailure',
        @CommonFailurePattern,
        (SELECT COUNT(*) FROM @FailingSessionsTable),
        'Investigate common action sequence: ' + @CommonFailurePattern +
        '. Consider simplifying flow or adding guidance.',
        GETUTCDATE()
    );

    SET @ActionStatus = 'Executed';
END
```

---

## Part 5: Visualization and Reporting

### Path Heatmap Query

```sql
-- Generate 3D heatmap of all session paths
WITH PathPoints AS (
    SELECT
        sp.SessionPathId,
        sp.SessionId,
        pt.intValue AS PointNumber,
        sp.Path.STPointN(pt.intValue).STX AS X,
        sp.Path.STPointN(pt.intValue).STY AS Y,
        sp.Path.STPointN(pt.intValue).STZ AS Z,
        sp.Path.STPointN(pt.intValue).M AS Timestamp
    FROM dbo.SessionPaths sp
    CROSS APPLY dbo.fn_IntSequence(1, sp.Path.STNumPoints()) pt
    WHERE sp.CompletedAt >= DATEADD(DAY, -7, GETUTCDATE())
)
SELECT
    CAST(X / 5 AS INT) * 5 AS GridX,  -- 5-unit grid
    CAST(Y / 5 AS INT) * 5 AS GridY,
    CAST(Z / 5 AS INT) * 5 AS GridZ,
    COUNT(*) AS TrafficDensity
FROM PathPoints
GROUP BY CAST(X / 5 AS INT), CAST(Y / 5 AS INT), CAST(Z / 5 AS INT)
ORDER BY TrafficDensity DESC;
```

**Result**: 3D density map showing where users spend time in semantic space

### Funnel Analysis (Geometric)

```sql
-- Define funnel stages as geometric regions
DECLARE @Stage1Region GEOMETRY = geometry::Point(10, 20, 5, 0).STBuffer(10);  -- Home
DECLARE @Stage2Region GEOMETRY = geometry::Point(15, 25, 8, 0).STBuffer(10);  -- Search
DECLARE @Stage3Region GEOMETRY = geometry::Point(20, 30, 12, 0).STBuffer(10); -- Product
DECLARE @Stage4Region GEOMETRY = geometry::Point(25, 35, 15, 0).STBuffer(10); -- Checkout

WITH StagePenetration AS (
    SELECT
        SessionId,
        CASE WHEN Path.STIntersects(@Stage1Region) = 1 THEN 1 ELSE 0 END AS ReachedStage1,
        CASE WHEN Path.STIntersects(@Stage2Region) = 1 THEN 1 ELSE 0 END AS ReachedStage2,
        CASE WHEN Path.STIntersects(@Stage3Region) = 1 THEN 1 ELSE 0 END AS ReachedStage3,
        CASE WHEN Path.STIntersects(@Stage4Region) = 1 THEN 1 ELSE 0 END AS ReachedStage4
    FROM dbo.SessionPaths
    WHERE CompletedAt >= DATEADD(DAY, -7, GETUTCDATE())
)
SELECT
    'Stage 1 (Home)' AS Stage, SUM(ReachedStage1) AS SessionCount, CAST(SUM(ReachedStage1) AS FLOAT) / COUNT(*) AS Penetration
FROM StagePenetration
UNION ALL
SELECT 'Stage 2 (Search)', SUM(ReachedStage2), CAST(SUM(ReachedStage2) AS FLOAT) / NULLIF(SUM(ReachedStage1), 0)
FROM StagePenetration
UNION ALL
SELECT 'Stage 3 (Product)', SUM(ReachedStage3), CAST(SUM(ReachedStage3) AS FLOAT) / NULLIF(SUM(ReachedStage2), 0)
FROM StagePenetration
UNION ALL
SELECT 'Stage 4 (Checkout)', SUM(ReachedStage4), CAST(SUM(ReachedStage4) AS FLOAT) / NULLIF(SUM(ReachedStage3), 0)
FROM StagePenetration;
```

**Result**: Geometric funnel showing drop-off rates between semantic regions

---

## Part 6: Provenance Tracking

### Neo4j Integration

```cypher
// Create session path node
CREATE (session:SessionPath {
    sessionId: $sessionId,
    pathWKT: $pathWKT,
    pathLength: $pathLength,
    startTime: $startTime,
    endTime: $endTime,
    numActions: $numActions
})

// Link each action in path
FOREACH (action IN $actions |
    CREATE (a:UserAction {
        actionId: action.actionId,
        actionType: action.actionType,
        actionData: action.actionData,
        timestamp: action.timestamp,
        geometryWKT: action.geometryWKT
    })
    CREATE (session)-[:HAD_ACTION {sequenceNumber: action.sequenceNumber}]->(a)
)

// Link to user
MATCH (user:User {userId: $userId})
CREATE (user)-[:HAD_SESSION]->(session)

// Link to detected UX issue if applicable
MATCH (issue:UXIssue {issueId: $issueId})
CREATE (session)-[:CONTRIBUTED_TO_ISSUE]->(issue)
```

### Trace User Journey to UX Issue

```cypher
// Find all sessions that contributed to a specific UX issue
MATCH (session:SessionPath)-[:CONTRIBUTED_TO_ISSUE]->(issue:UXIssue {issueId: $issueId})
MATCH (session)-[r:HAD_ACTION]->(action:UserAction)
RETURN session, r, action
ORDER BY session.startTime, r.sequenceNumber;
```

---

## Conclusion

**Hartonomous behavioral analysis is not external analytics - it's geometric path analysis.**

✅ **SessionPaths as GEOMETRY**: User journeys stored as LINESTRING in 3D semantic space
✅ **Spatial Queries**: Find sessions ending in error regions, abnormally long paths, common failure patterns
✅ **OODA Integration**: Automatic UX issue detection via sp_Hypothesize (lines 239-258)
✅ **Geometric Funnels**: Define stages as spatial regions, measure penetration
✅ **Full Provenance**: Every user action tracked in Neo4j Merkle DAG

**The Breakthrough**: User behavior IS part of the semantic space. UX issues are geometric patterns.

**The Detection**: sp_Hypothesize automatically generates "FixUX" hypotheses from geometric path analysis.

**The Fix**: sp_Act generates UX improvement recommendations from common failure patterns.

This is behavioral analysis as spatial navigation.
