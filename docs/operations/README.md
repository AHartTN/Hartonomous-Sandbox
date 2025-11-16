# Operations Runbooks

Monitoring, troubleshooting, and operational procedures for Hartonomous.

---

## Runbooks

### Monitoring

**System Health Monitoring** - Monitor OODA loop, worker services, database performance (coming soon)
**Performance Monitoring** - Track query performance, spatial index usage, reasoning quality (coming soon)
**Alert Configuration** - Configure alerts for failures, regressions, capacity issues (coming soon)

### Troubleshooting

**OODA Loop Not Running** - Diagnose and fix Service Broker issues (coming soon)
**Slow Queries** - Identify and fix query performance problems (coming soon)
**Worker Service Crashes** - Diagnose worker failures and restart procedures (coming soon)
**Spatial Index Issues** - Rebuild indexes, fix geometry errors (coming soon)

### Maintenance

**Database Backup** - Backup procedures for SQL Server and Neo4j (coming soon)
**Index Maintenance** - Rebuild spatial indexes, update statistics (coming soon)
**Log Rotation** - Manage application and database logs (coming soon)
**Cleanup Procedures** - Archive old data, prune models (coming soon)

### Disaster Recovery

**Recovery Procedures** - Restore from backup, point-in-time recovery (coming soon)
**Failover Procedures** - Failover to standby SQL Server, Neo4j cluster (coming soon)

---

## Quick Health Check

Run this SQL script to verify system health:

```sql
USE Hartonomous;

-- 1. Check Service Broker status
SELECT
    'Service Broker' AS Component,
    CASE WHEN is_broker_enabled = 1 THEN 'Healthy' ELSE 'FAILED' END AS Status
FROM sys.databases WHERE name = 'Hartonomous';

-- 2. Check OODA loop execution (last hour)
SELECT
    'OODA Loop' AS Component,
    CASE WHEN COUNT(*) > 0 THEN 'Healthy' ELSE 'FAILED' END AS Status
FROM dbo.OODALoopMetrics
WHERE ExecutedAt >= DATEADD(HOUR, -1, GETUTCDATE());

-- 3. Check spatial indexes
SELECT
    'Spatial Indexes' AS Component,
    CASE WHEN COUNT(*) >= 10 THEN 'Healthy' ELSE 'WARNING' END AS Status
FROM sys.indexes WHERE type_desc = 'SPATIAL';

-- 4. Check worker activity (last 15 minutes)
SELECT
    'Workers' AS Component,
    CASE WHEN COUNT(*) > 0 THEN 'Healthy' ELSE 'WARNING' END AS Status
FROM dbo.Atoms
WHERE CreatedAt >= DATEADD(MINUTE, -15, GETUTCDATE());

-- 5. Check reasoning frameworks (last hour)
SELECT
    'Reasoning Frameworks' AS Component,
    CASE WHEN COUNT(*) >= 0 THEN 'Healthy' ELSE 'N/A' END AS Status
FROM dbo.ReasoningChains
WHERE CreatedAt >= DATEADD(HOUR, -1, GETUTCDATE());
```

---

## Monitoring Metrics

### OODA Loop Health

**Metrics**:
- Execution frequency (target: every 15-60 minutes)
- Success rate (target: >95%)
- Average duration (target: <5 seconds per phase)
- Queue depth (target: <10 messages)

**Query**:
```sql
SELECT
    Phase,
    COUNT(*) AS Executions,
    AVG(DurationMs) AS AvgDuration,
    SUM(CASE WHEN Success = 1 THEN 1 ELSE 0 END) * 100.0 / COUNT(*) AS SuccessRate
FROM dbo.OODALoopMetrics
WHERE ExecutedAt >= DATEADD(HOUR, -24, GETUTCDATE())
GROUP BY Phase;
```

### Spatial Query Performance

**Metrics**:
- Average query time (target: <50ms for 1M atoms)
- Spatial index seek ratio (target: >90%)
- Cache hit rate (target: >80%)

**Query**:
```sql
SELECT
    AVG(CAST(total_elapsed_time AS FLOAT) / execution_count / 1000) AS AvgDurationMs,
    AVG(CAST(total_logical_reads AS FLOAT) / execution_count) AS AvgLogicalReads
FROM sys.dm_exec_query_stats qs
CROSS APPLY sys.dm_exec_sql_text(qs.sql_handle) st
WHERE st.text LIKE '%AtomEmbeddings%'
    AND st.text LIKE '%STIntersects%';
```

### Reasoning Framework Success

**Metrics**:
- Chain of Thought coherence score (target: >0.7)
- Tree of Thought path diversity (target: >3 paths explored)
- Reflexion agreement ratio (target: >0.5)

**Query**:
```sql
SELECT
    'Chain of Thought' AS Framework,
    AVG(CoherenceScore) AS AvgScore
FROM dbo.ReasoningChains
WHERE CreatedAt >= DATEADD(HOUR, -24, GETUTCDATE())
UNION ALL
SELECT
    'Tree of Thought',
    AVG(CAST(NumPaths AS FLOAT))
FROM dbo.MultiPathReasoning
WHERE CreatedAt >= DATEADD(HOUR, -24, GETUTCDATE())
UNION ALL
SELECT
    'Reflexion',
    AVG(AgreementRatio)
FROM dbo.SelfConsistencyResults
WHERE CreatedAt >= DATEADD(HOUR, -24, GETUTCDATE());
```

---

## Common Issues

### Issue: OODA Loop Stopped

**Symptoms**: No entries in OODALoopMetrics for >1 hour

**Diagnosis**:
```sql
-- Check Service Broker enabled
SELECT is_broker_enabled FROM sys.databases WHERE name = 'Hartonomous';

-- Check queue depth
SELECT COUNT(*) FROM dbo.AnalyzeQueue;
SELECT COUNT(*) FROM dbo.HypothesizeQueue;
SELECT COUNT(*) FROM dbo.ActQueue;
SELECT COUNT(*) FROM dbo.LearnQueue;
```

**Fix**:
```sql
-- Restart OODA loop
DECLARE @handle UNIQUEIDENTIFIER;
BEGIN DIALOG CONVERSATION @handle
    FROM SERVICE [AnalyzeService]
    TO SERVICE 'AnalyzeService'
    ON CONTRACT [//Hartonomous/AutonomousLoop/AnalyzeContract]
    WITH ENCRYPTION = OFF;

SEND ON CONVERSATION @handle
    MESSAGE TYPE [//Hartonomous/AutonomousLoop/AnalyzeMessage]
    (N'{"trigger": "manual"}');
```

### Issue: Slow Spatial Queries

**Symptoms**: Queries taking >100ms on <1M atoms

**Diagnosis**:
```sql
-- Check if spatial index is being used
SET STATISTICS IO ON;
SET STATISTICS TIME ON;

SELECT TOP 10 *
FROM dbo.AtomEmbeddings WITH (INDEX(IX_AtomEmbeddings_SpatialGeometry))
WHERE SpatialGeometry.STIntersects(@query.STBuffer(10)) = 1;
```

**Fix**:
```sql
-- Rebuild spatial index
ALTER INDEX IX_AtomEmbeddings_SpatialGeometry
ON dbo.AtomEmbeddings REBUILD;

-- Update statistics
UPDATE STATISTICS dbo.AtomEmbeddings WITH FULLSCAN;
```

### Issue: Worker Service Not Processing

**Symptoms**: No new atoms created for >15 minutes

**Diagnosis**:
- Check worker service logs for exceptions
- Verify SQL Server connection string
- Check Service Broker queue for backlog

**Fix**:
- Restart worker service
- Check `appsettings.json` connection string
- Clear Service Broker queue if backlogged

---

## Backup Procedures

### SQL Server Backup

```sql
-- Full backup
BACKUP DATABASE Hartonomous
TO DISK = 'C:\Backups\Hartonomous_Full.bak'
WITH COMPRESSION, INIT;

-- Differential backup
BACKUP DATABASE Hartonomous
TO DISK = 'C:\Backups\Hartonomous_Diff.bak'
WITH DIFFERENTIAL, COMPRESSION, INIT;

-- Transaction log backup
BACKUP LOG Hartonomous
TO DISK = 'C:\Backups\Hartonomous_Log.bak'
WITH COMPRESSION, INIT;
```

### Neo4j Backup

```bash
# Stop Neo4j
neo4j stop

# Backup database directory
cp -r /var/lib/neo4j/data /backups/neo4j-$(date +%Y%m%d)

# Start Neo4j
neo4j start
```

---

## Performance Tuning

**Coming soon**:
- Spatial index tuning guide
- Query optimization techniques
- Memory configuration recommendations
- Worker service scaling strategies

---

For detailed architecture information, see **[ARCHITECTURE.md](../../ARCHITECTURE.md)**.

For setup and configuration, see **[Setup Guides](../setup/)**.

For complete technical details, see **[Rewrite Guide](../rewrite-guide/)**.
