# Monitoring

**Last Updated**: November 19, 2025  
**Status**: Production Ready

## Overview

Hartonomous monitoring spans SQL Server, Neo4j, and CLR components. Key metrics: OODA loop health, spatial query performance, ingestion throughput, Neo4j sync lag, and CLR assembly errors.

## Architecture

```
┌────────────────────────────────────────────────────────┐
│              Monitoring Stack                          │
│  ┌──────────────┐  ┌──────────────┐  ┌──────────────┐ │
│  │ SQL Server   │  │    Neo4j     │  │  Service     │ │
│  │ DMVs         │  │   Metrics    │  │  Broker      │ │
│  └──────┬───────┘  └──────┬───────┘  └──────┬───────┘ │
│         │                  │                  │         │
│         ▼                  ▼                  ▼         │
│  ┌──────────────────────────────────────────────────┐  │
│  │         Monitoring Views (dbo.vw_*)              │  │
│  └──────────────────┬───────────────────────────────┘  │
│                     │                                   │
│                     ▼                                   │
│  ┌──────────────────────────────────────────────────┐  │
│  │           Grafana Dashboards                     │  │
│  │  - OODA Loop Health                              │  │
│  │  - Spatial Query Performance                     │  │
│  │  - Ingestion Pipeline                            │  │
│  │  - Neo4j Sync Status                             │  │
│  └──────────────────┬───────────────────────────────┘  │
│                     │                                   │
│                     ▼                                   │
│  ┌──────────────────────────────────────────────────┐  │
│  │        Prometheus AlertManager                   │  │
│  │  - Slow queries >50ms                            │  │
│  │  - OODA failures >5%                             │  │
│  │  - Neo4j sync lag >60s                           │  │
│  └──────────────────────────────────────────────────┘  │
└────────────────────────────────────────────────────────┘
```

## SQL Server Monitoring

### View: OODA Loop Health

```sql
CREATE VIEW dbo.vw_OODALoopHealth AS
SELECT 
    Phase,
    COUNT(*) AS ExecutionCount,
    AVG(DATEDIFF(MILLISECOND, StartTime, EndTime)) AS AvgDurationMs,
    MAX(DATEDIFF(MILLISECOND, StartTime, EndTime)) AS MaxDurationMs,
    SUM(CASE WHEN ErrorMessage IS NOT NULL THEN 1 ELSE 0 END) AS ErrorCount,
    CAST(SUM(CASE WHEN ErrorMessage IS NOT NULL THEN 1 ELSE 0 END) AS FLOAT) / COUNT(*) AS ErrorRate
FROM dbo.OODALogs
WHERE StartTime >= DATEADD(HOUR, -1, SYSDATETIME())
GROUP BY Phase;
```

**Usage**:
```sql
-- Check OODA health in last hour
SELECT * FROM dbo.vw_OODALoopHealth
ORDER BY AvgDurationMs DESC;
```

**Expected Output**:

| Phase | ExecutionCount | AvgDurationMs | MaxDurationMs | ErrorCount | ErrorRate |
|-------|----------------|---------------|---------------|------------|-----------|
| Observe | 1247 | 12 | 45 | 3 | 0.0024 |
| Orient | 1247 | 158 | 320 | 8 | 0.0064 |
| Decide | 1247 | 23 | 78 | 1 | 0.0008 |
| Act | 1247 | 67 | 210 | 5 | 0.0040 |

**Alerts**:
- ErrorRate >5%: OODA loop failing
- AvgDurationMs >200: Performance degradation

### View: Spatial Query Performance

```sql
CREATE VIEW dbo.vw_SpatialQueryPerformance AS
SELECT 
    SUBSTRING(QueryText, 1, 100) AS QuerySnippet,
    COUNT(*) AS ExecutionCount,
    AVG(DurationMs) AS AvgDurationMs,
    MAX(DurationMs) AS MaxDurationMs,
    AVG(LogicalReads) AS AvgLogicalReads,
    AVG(RowsReturned) AS AvgRowsReturned
FROM dbo.QueryPerformanceLog
WHERE QueryText LIKE '%STIntersects%'
    OR QueryText LIKE '%STDistance%'
    OR QueryText LIKE '%STBuffer%'
GROUP BY SUBSTRING(QueryText, 1, 100);
```

**Usage**:
```sql
-- Find slow spatial queries
SELECT * FROM dbo.vw_SpatialQueryPerformance
WHERE AvgDurationMs > 50
ORDER BY AvgDurationMs DESC;
```

**Alerts**:
- AvgDurationMs >50ms: Index rebuild needed
- AvgLogicalReads >10000: Query needs optimization

### View: Ingestion Pipeline Status

```sql
CREATE VIEW dbo.vw_IngestionStatus AS
SELECT 
    TenantId,
    ModelId,
    COUNT(*) AS AtomsIngested,
    MIN(IngestTimestamp) AS FirstAtom,
    MAX(IngestTimestamp) AS LastAtom,
    DATEDIFF(SECOND, MIN(IngestTimestamp), MAX(IngestTimestamp)) AS DurationSeconds,
    COUNT(*) * 1.0 / NULLIF(DATEDIFF(SECOND, MIN(IngestTimestamp), MAX(IngestTimestamp)), 0) AS AtomsPerSecond
FROM dbo.Atoms
WHERE IngestTimestamp >= DATEADD(HOUR, -1, SYSDATETIME())
GROUP BY TenantId, ModelId;
```

**Usage**:
```sql
-- Check recent ingestion throughput
SELECT * FROM dbo.vw_IngestionStatus
ORDER BY LastAtom DESC;
```

**Alerts**:
- AtomsPerSecond <100: Ingestion slowdown
- DurationSeconds >3600: Model taking >1 hour to ingest

### DMV: Spatial Index Health

```sql
-- Fragmentation of spatial indexes
SELECT 
    OBJECT_NAME(ips.object_id) AS TableName,
    i.name AS IndexName,
    ips.index_type_desc,
    ips.avg_fragmentation_in_percent,
    ips.page_count
FROM sys.dm_db_index_physical_stats(DB_ID(), NULL, NULL, NULL, 'LIMITED') ips
INNER JOIN sys.indexes i ON ips.object_id = i.object_id AND ips.index_id = i.index_id
WHERE i.type_desc = 'SPATIAL'
    AND ips.avg_fragmentation_in_percent > 30
ORDER BY ips.avg_fragmentation_in_percent DESC;
```

**Action**: Rebuild indexes with >30% fragmentation:
```sql
ALTER INDEX IX_AtomEmbeddings_SpatialGeometry 
ON dbo.AtomEmbeddings REBUILD;
```

### DMV: Slow Queries

```sql
CREATE VIEW dbo.vw_SlowQueries AS
SELECT TOP 20
    SUBSTRING(qt.text, (qs.statement_start_offset / 2) + 1,
        ((CASE qs.statement_end_offset
            WHEN -1 THEN DATALENGTH(qt.text)
            ELSE qs.statement_end_offset
        END - qs.statement_start_offset) / 2) + 1) AS QueryText,
    qs.execution_count AS ExecutionCount,
    qs.total_elapsed_time / 1000 AS TotalElapsedMs,
    qs.total_elapsed_time / qs.execution_count / 1000 AS AvgElapsedMs,
    qs.total_logical_reads AS TotalLogicalReads,
    qs.total_logical_reads / qs.execution_count AS AvgLogicalReads,
    qs.creation_time,
    qs.last_execution_time
FROM sys.dm_exec_query_stats qs
CROSS APPLY sys.dm_exec_sql_text(qs.sql_handle) qt
WHERE qs.total_elapsed_time / qs.execution_count > 50000  -- >50ms average
ORDER BY qs.total_elapsed_time / qs.execution_count DESC;
```

**Usage**:
```sql
SELECT * FROM dbo.vw_SlowQueries;
```

## Neo4j Monitoring

### View: Neo4j Sync Lag

```sql
CREATE VIEW dbo.vw_Neo4jSyncLag AS
SELECT 
    EntityType,
    COUNT(*) AS PendingItems,
    MIN(CreatedAt) AS OldestPending,
    MAX(CreatedAt) AS NewestPending,
    DATEDIFF(SECOND, MIN(CreatedAt), SYSDATETIME()) AS LagSeconds
FROM dbo.Neo4jSyncQueue
WHERE IsSynced = 0
    AND RetryCount < 5
GROUP BY EntityType;
```

**Usage**:
```sql
-- Check Neo4j sync backlog
SELECT * FROM dbo.vw_Neo4jSyncLag
ORDER BY LagSeconds DESC;
```

**Alerts**:
- LagSeconds >60: Neo4j sync falling behind
- PendingItems >10000: Queue overwhelmed

### Neo4j Metrics (via REST API)

```sql
-- Query Neo4j metrics via sp_invoke_external_rest_endpoint
DECLARE @Response NVARCHAR(MAX);
EXEC sp_invoke_external_rest_endpoint
    @url = 'http://neo4j-server:7474/db/data/transaction/commit',
    @method = 'POST',
    @headers = '{"Authorization": "Basic <base64>", "Content-Type": "application/json"}',
    @payload = '{"statements": [{"statement": "CALL dbms.queryJmx(\"org.neo4j:name=Primitive count\") YIELD attributes RETURN attributes.NodeCount.value AS NodeCount"}]}',
    @response = @Response OUTPUT;

SELECT 
    JSON_VALUE(@Response, '$.results[0].data[0].row[0]') AS NodeCount;
```

**Metrics to Track**:
- NodeCount: Total nodes in graph
- RelationshipCount: Total edges
- StoreSize: Disk usage
- TransactionsCommitted: Throughput

## Service Broker Monitoring

### View: Queue Depth

```sql
CREATE VIEW dbo.vw_ServiceBrokerQueueDepth AS
SELECT 
    q.name AS QueueName,
    SUM(CASE WHEN sqm.validation = 'X' THEN 1 ELSE 0 END) AS MessageCount,
    MIN(sqm.queuing_order) AS OldestMessage,
    MAX(sqm.queuing_order) AS NewestMessage
FROM sys.service_queues q
LEFT JOIN sys.transmission_queue sqm ON q.object_id = sqm.to_service_id
WHERE q.name IN ('IngestionQueue', 'Neo4jSyncQueue', 'OODAQueue')
GROUP BY q.name;
```

**Usage**:
```sql
SELECT * FROM dbo.vw_ServiceBrokerQueueDepth;
```

**Alerts**:
- MessageCount >5000: Queue backlog
- OldestMessage age >300 seconds: Processing stalled

### Service Broker Errors

```sql
-- Check transmission queue for errors
SELECT 
    from_service_name,
    to_service_name,
    message_body,
    transmission_status,
    enqueue_time
FROM sys.transmission_queue
WHERE transmission_status <> 'Success'
ORDER BY enqueue_time DESC;
```

## Grafana Dashboards

### Dashboard: OODA Loop Health

**Datasource**: SQL Server (via ODBC)

**Panel 1: OODA Phase Duration**
```sql
SELECT 
    DATEADD(MINUTE, DATEDIFF(MINUTE, 0, StartTime) / 5 * 5, 0) AS TimeBucket,
    Phase,
    AVG(DATEDIFF(MILLISECOND, StartTime, EndTime)) AS AvgDurationMs
FROM dbo.OODALogs
WHERE StartTime >= DATEADD(HOUR, -24, SYSDATETIME())
GROUP BY DATEADD(MINUTE, DATEDIFF(MINUTE, 0, StartTime) / 5 * 5, 0), Phase
ORDER BY TimeBucket, Phase;
```
**Visualization**: Line chart, 4 series (Observe/Orient/Decide/Act)

**Panel 2: OODA Error Rate**
```sql
SELECT 
    DATEADD(MINUTE, DATEDIFF(MINUTE, 0, StartTime) / 5 * 5, 0) AS TimeBucket,
    Phase,
    CAST(SUM(CASE WHEN ErrorMessage IS NOT NULL THEN 1 ELSE 0 END) AS FLOAT) / COUNT(*) AS ErrorRate
FROM dbo.OODALogs
WHERE StartTime >= DATEADD(HOUR, -24, SYSDATETIME())
GROUP BY DATEADD(MINUTE, DATEDIFF(MINUTE, 0, StartTime) / 5 * 5, 0), Phase
ORDER BY TimeBucket, Phase;
```
**Visualization**: Line chart, alert threshold at 5%

### Dashboard: Spatial Query Performance

**Panel 1: Query Duration Histogram**
```sql
SELECT 
    CASE 
        WHEN DurationMs < 10 THEN '<10ms'
        WHEN DurationMs < 50 THEN '10-50ms'
        WHEN DurationMs < 100 THEN '50-100ms'
        ELSE '>100ms'
    END AS Bucket,
    COUNT(*) AS QueryCount
FROM dbo.QueryPerformanceLog
WHERE QueryText LIKE '%STIntersects%'
    AND ExecutedAt >= DATEADD(HOUR, -24, SYSDATETIME())
GROUP BY CASE 
    WHEN DurationMs < 10 THEN '<10ms'
    WHEN DurationMs < 50 THEN '10-50ms'
    WHEN DurationMs < 100 THEN '50-100ms'
    ELSE '>100ms'
END;
```
**Visualization**: Bar chart

**Panel 2: Spatial Index Fragmentation**
```sql
SELECT 
    OBJECT_NAME(ips.object_id) AS TableName,
    i.name AS IndexName,
    ips.avg_fragmentation_in_percent AS Fragmentation
FROM sys.dm_db_index_physical_stats(DB_ID(), NULL, NULL, NULL, 'LIMITED') ips
INNER JOIN sys.indexes i ON ips.object_id = i.object_id AND ips.index_id = i.index_id
WHERE i.type_desc = 'SPATIAL';
```
**Visualization**: Gauge, alert at 30%

### Dashboard: Ingestion Pipeline

**Panel 1: Ingestion Throughput**
```sql
SELECT 
    DATEADD(MINUTE, DATEDIFF(MINUTE, 0, IngestTimestamp) / 5 * 5, 0) AS TimeBucket,
    COUNT(*) AS AtomsIngested
FROM dbo.Atoms
WHERE IngestTimestamp >= DATEADD(HOUR, -24, SYSDATETIME())
GROUP BY DATEADD(MINUTE, DATEDIFF(MINUTE, 0, IngestTimestamp) / 5 * 5, 0)
ORDER BY TimeBucket;
```
**Visualization**: Area chart

**Panel 2: Service Broker Queue Depth**
```sql
SELECT 
    q.name AS QueueName,
    SUM(CASE WHEN sqm.validation = 'X' THEN 1 ELSE 0 END) AS MessageCount
FROM sys.service_queues q
LEFT JOIN sys.transmission_queue sqm ON q.object_id = sqm.to_service_id
WHERE q.name IN ('IngestionQueue', 'Neo4jSyncQueue')
GROUP BY q.name;
```
**Visualization**: Bar chart, alert at 5000 messages

## Prometheus AlertManager

### Alert Rules

**alertmanager.yml**:
```yaml
groups:
  - name: hartonomous
    interval: 30s
    rules:
      - alert: OODALoopHighErrorRate
        expr: ooda_error_rate > 0.05
        for: 5m
        labels:
          severity: critical
        annotations:
          summary: "OODA Loop error rate >5%"
          description: "Phase {{ $labels.phase }} has error rate {{ $value }}"

      - alert: SpatialQuerySlow
        expr: avg_spatial_query_duration_ms > 50
        for: 10m
        labels:
          severity: warning
        annotations:
          summary: "Spatial queries averaging >50ms"
          description: "Consider rebuilding spatial indexes"

      - alert: Neo4jSyncLag
        expr: neo4j_sync_lag_seconds > 60
        for: 5m
        labels:
          severity: warning
        annotations:
          summary: "Neo4j sync lagging >60 seconds"
          description: "Neo4j sync queue has {{ $value }}s lag"

      - alert: IngestionStalled
        expr: rate(atoms_ingested[5m]) < 100
        for: 10m
        labels:
          severity: critical
        annotations:
          summary: "Ingestion throughput <100 atoms/second"
          description: "Check Service Broker activation"

      - alert: SpatialIndexFragmented
        expr: spatial_index_fragmentation > 30
        for: 1h
        labels:
          severity: warning
        annotations:
          summary: "Spatial index fragmentation >30%"
          description: "Rebuild index {{ $labels.index_name }}"
```

### Metric Exporters

**SQL Server Exporter** (Custom):
```csharp
// Export metrics via HTTP endpoint for Prometheus scraping
app.MapGet("/metrics", () =>
{
    var metrics = new StringBuilder();
    using var conn = new SqlConnection(connectionString);
    conn.Open();

    // OODA error rate
    using var cmd = new SqlCommand(@"
        SELECT Phase, 
               CAST(SUM(CASE WHEN ErrorMessage IS NOT NULL THEN 1 ELSE 0 END) AS FLOAT) / COUNT(*) AS ErrorRate
        FROM dbo.OODALogs
        WHERE StartTime >= DATEADD(HOUR, -1, SYSDATETIME())
        GROUP BY Phase", conn);
    using var reader = cmd.ExecuteReader();
    while (reader.Read())
    {
        metrics.AppendLine($"ooda_error_rate{{phase=\"{reader["Phase"]}\"}} {reader["ErrorRate"]}");
    }

    // Similar exports for spatial queries, ingestion, etc.
    return Results.Text(metrics.ToString(), "text/plain");
});
```

## Serilog Configuration

**appsettings.json**:
```json
{
  "Serilog": {
    "Using": ["Serilog.Sinks.Console", "Serilog.Sinks.MSSqlServer"],
    "MinimumLevel": {
      "Default": "Information",
      "Override": {
        "Microsoft": "Warning",
        "System": "Warning"
      }
    },
    "WriteTo": [
      {
        "Name": "Console",
        "Args": {
          "outputTemplate": "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}"
        }
      },
      {
        "Name": "MSSqlServer",
        "Args": {
          "connectionString": "Server=localhost;Database=Hartonomous;Trusted_Connection=True;",
          "tableName": "Logs",
          "autoCreateSqlTable": true,
          "restrictedToMinimumLevel": "Information"
        }
      }
    ],
    "Enrich": ["FromLogContext", "WithMachineName", "WithThreadId"]
  }
}
```

## Best Practices

1. **OODA Monitoring**: Alert if error rate >5% or Orient phase >200ms
2. **Spatial Index Maintenance**: Rebuild indexes weekly or when fragmentation >30%
3. **Neo4j Sync**: Monitor lag every 30 seconds, alert at 60s
4. **Ingestion Pipeline**: Track atoms/second, alert if <100
5. **Service Broker**: Check queue depth every minute, alert at 5000 messages
6. **Grafana Dashboards**: Refresh every 30 seconds for real-time visibility

## Summary

Hartonomous monitoring covers:

- **OODA Loop**: Health metrics, error rates, phase durations
- **Spatial Queries**: Duration, index fragmentation, logical reads
- **Ingestion**: Throughput, queue depth, Service Broker status
- **Neo4j**: Sync lag, node/edge counts, transaction rates
- **Alerting**: Prometheus AlertManager for proactive issue detection

All metrics accessible via SQL views, DMVs, and Grafana dashboards.
