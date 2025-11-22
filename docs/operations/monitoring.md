# Monitoring and Observability

**Production Monitoring Strategy for Hartonomous**

## Monitoring Architecture

```
┌─────────────────────────────────────────────────────┐
│  Application Insights (Azure)                       │
│  • Request telemetry                                │
│  • Exception tracking                               │
│  • Custom events                                    │
│  • Performance counters                             │
└────────────────┬────────────────────────────────────┘
                 │
┌────────────────▼────────────────────────────────────┐
│  SQL Server DMVs (Dynamic Management Views)         │
│  • Query stats                                      │
│  • Index usage                                      │
│  • Blocking/deadlocks                               │
│  • CLR performance                                  │
└────────────────┬────────────────────────────────────┘
                 │
┌────────────────▼────────────────────────────────────┐
│  Neo4j Monitoring                                   │
│  • Cypher query performance                         │
│  • Memory usage                                     │
│  • Transaction throughput                           │
└────────────────┬────────────────────────────────────┘
                 │
┌────────────────▼────────────────────────────────────┐
│  Windows Performance Counters                       │
│  • CPU, Memory, Disk I/O                            │
│  • .NET CLR metrics                                 │
│  • ASP.NET request queue                            │
└─────────────────────────────────────────────────────┘
```

## Key Metrics

### API Performance

**Request Latency (P50, P95, P99)**:
```csharp
// Tracked automatically by Application Insights
// Query in Azure Portal:
requests
| where timestamp > ago(1h)
| summarize
    P50 = percentile(duration, 50),
    P95 = percentile(duration, 95),
    P99 = percentile(duration, 99)
  by operation_Name
| order by P95 desc
```

**Target SLAs**:
- Ingestion endpoints: P95 < 5s, P99 < 15s
- Search endpoints: P95 < 100ms, P99 < 500ms
- Inference endpoints: P95 < 2s, P99 < 10s

**Throughput**:
```kusto
requests
| where timestamp > ago(1h)
| summarize RequestsPerMinute = count() / 60
```

**Error Rate**:
```kusto
requests
| where timestamp > ago(1h)
| summarize
    Total = count(),
    Errors = countif(success == false),
    ErrorRate = 100.0 * countif(success == false) / count()
```

### Database Performance

**Query Performance** (SQL Server DMV):
```sql
-- Top 20 slowest queries in last hour
SELECT TOP 20
    SUBSTRING(qt.text, (qs.statement_start_offset/2)+1,
        ((CASE qs.statement_end_offset
            WHEN -1 THEN DATALENGTH(qt.text)
            ELSE qs.statement_end_offset
        END - qs.statement_start_offset)/2) + 1) AS QueryText,
    qs.execution_count AS Executions,
    qs.total_elapsed_time / 1000000.0 AS TotalElapsedSec,
    qs.total_elapsed_time / qs.execution_count / 1000.0 AS AvgDurationMs,
    qs.total_logical_reads / qs.execution_count AS AvgLogicalReads,
    qs.last_execution_time
FROM sys.dm_exec_query_stats qs
CROSS APPLY sys.dm_exec_sql_text(qs.sql_handle) qt
WHERE qs.last_execution_time > DATEADD(hour, -1, GETUTCDATE())
ORDER BY qs.total_elapsed_time / qs.execution_count DESC;
```

**Index Usage**:
```sql
-- Unused indices (candidates for removal)
SELECT
    OBJECT_NAME(i.object_id) AS TableName,
    i.name AS IndexName,
    i.type_desc,
    us.user_seeks,
    us.user_scans,
    us.user_lookups,
    us.user_updates
FROM sys.indexes i
LEFT JOIN sys.dm_db_index_usage_stats us ON i.object_id = us.object_id AND i.index_id = us.index_id
WHERE OBJECTPROPERTY(i.object_id, 'IsUserTable') = 1
  AND i.index_id > 0
  AND (us.user_seeks + us.user_scans + us.user_lookups) = 0
  AND us.user_updates > 100  -- High write cost, no read benefit
ORDER BY us.user_updates DESC;
```

**Blocking and Deadlocks**:
```sql
-- Current blocking chains
SELECT
    t1.resource_type,
    t1.resource_database_id,
    t1.resource_associated_entity_id,
    t1.request_mode,
    t1.request_session_id,
    t2.blocking_session_id,
    OBJECT_NAME(p.object_id) AS BlockedObjectName,
    h1.text AS BlockedQuery,
    h2.text AS BlockingQuery
FROM sys.dm_tran_locks t1
JOIN sys.dm_os_waiting_tasks t2 ON t1.lock_owner_address = t2.resource_address
LEFT JOIN sys.partitions p ON p.partition_id = t1.resource_associated_entity_id
LEFT JOIN sys.dm_exec_connections c1 ON t1.request_session_id = c1.session_id
LEFT JOIN sys.dm_exec_connections c2 ON t2.blocking_session_id = c2.session_id
CROSS APPLY sys.dm_exec_sql_text(c1.most_recent_sql_handle) h1
CROSS APPLY sys.dm_exec_sql_text(c2.most_recent_sql_handle) h2;
```

**Spatial Index Performance**:
```sql
-- Spatial index selectivity
SELECT
    OBJECT_NAME(i.object_id) AS TableName,
    i.name AS IndexName,
    s.user_seeks,
    s.user_scans,
    s.avg_fragmentation_in_percent,
    s.page_count
FROM sys.indexes i
JOIN sys.dm_db_index_physical_stats(DB_ID(), NULL, NULL, NULL, 'DETAILED') s
    ON i.object_id = s.object_id AND i.index_id = s.index_id
WHERE i.type_desc = 'SPATIAL'
ORDER BY s.user_seeks + s.user_scans DESC;
```

### Storage Metrics

**Atom Count Growth**:
```sql
-- Daily atom ingestion rate
SELECT
    CAST(CreatedAt AS DATE) AS Date,
    COUNT(*) AS AtomsCreated,
    COUNT(DISTINCT TenantId) AS ActiveTenants,
    SUM(DATALENGTH(AtomicValue)) / 1024.0 / 1024.0 AS MBIngested
FROM dbo.Atom
WHERE CreatedAt > DATEADD(day, -30, GETUTCDATE())
GROUP BY CAST(CreatedAt AS DATE)
ORDER BY Date DESC;
```

**Deduplication Effectiveness**:
```sql
-- Deduplication savings
WITH AtomStats AS (
    SELECT
        COUNT(*) AS TotalReferences,
        COUNT(DISTINCT ContentHash) AS UniqueAtoms,
        SUM(ReferenceCount) AS TotalReferenceCount,
        SUM(DATALENGTH(AtomicValue)) AS UniqueBytes
    FROM dbo.Atom
)
SELECT
    TotalReferences,
    UniqueAtoms,
    CAST((1.0 - CAST(UniqueAtoms AS FLOAT) / TotalReferences) * 100 AS DECIMAL(5,2)) AS DeduplicationPercent,
    UniqueBytes / 1024.0 / 1024.0 / 1024.0 AS UniqueGB,
    (TotalReferenceCount * 64) / 1024.0 / 1024.0 / 1024.0 AS EquivalentGB_Without_CAS,
    CAST((1.0 - CAST(UniqueBytes AS FLOAT) / (TotalReferenceCount * 64)) * 100 AS DECIMAL(5,2)) AS StorageSavingsPercent
FROM AtomStats;
```

**Database Size**:
```sql
-- Database file sizes and growth
SELECT
    name AS FileName,
    size * 8.0 / 1024 AS SizeMB,
    CASE WHEN max_size = -1 THEN 'Unlimited' ELSE CAST(max_size * 8.0 / 1024 AS VARCHAR) END AS MaxSizeMB,
    growth * 8.0 / 1024 AS GrowthMB,
    type_desc
FROM sys.database_files;

-- Table sizes
SELECT
    s.name AS SchemaName,
    t.name AS TableName,
    p.rows AS RowCount,
    SUM(a.total_pages) * 8 / 1024 AS TotalSpaceMB,
    SUM(a.used_pages) * 8 / 1024 AS UsedSpaceMB,
    (SUM(a.total_pages) - SUM(a.used_pages)) * 8 / 1024 AS UnusedSpaceMB
FROM sys.tables t
JOIN sys.schemas s ON t.schema_id = s.schema_id
JOIN sys.indexes i ON t.object_id = i.object_id
JOIN sys.partitions p ON i.object_id = p.object_id AND i.index_id = p.index_id
JOIN sys.allocation_units a ON p.partition_id = a.container_id
GROUP BY s.name, t.name, p.rows
ORDER BY TotalSpaceMB DESC;
```

### OODA Loop Effectiveness

**Autonomous Improvement Success Rate**:
```sql
-- OODA loop performance (last 30 days)
SELECT
    ActionType,
    COUNT(*) AS TotalActions,
    SUM(CASE WHEN Status = 'Success' THEN 1 ELSE 0 END) AS SuccessCount,
    CAST(SUM(CASE WHEN Status = 'Success' THEN 1 ELSE 0 END) AS FLOAT) / COUNT(*) * 100 AS SuccessRate,
    AVG(ActualImprovement) AS AvgImprovement,
    AVG(DurationMs) AS AvgDurationMs
FROM dbo.AutonomousImprovementHistory
WHERE CreatedAt > DATEADD(day, -30, GETUTCDATE())
GROUP BY ActionType
ORDER BY TotalActions DESC;
```

**Hypothesis Accuracy**:
```sql
-- Compare expected vs actual improvement
SELECT
    pa.ActionType,
    pa.ExpectedImprovement,
    aih.ActualImprovement,
    CASE
        WHEN aih.ActualImprovement >= CAST(SUBSTRING(pa.ExpectedImprovement, 1, CHARINDEX('%', pa.ExpectedImprovement) - 1) AS DECIMAL)
        THEN 'Met or Exceeded'
        ELSE 'Below Expectation'
    END AS Performance
FROM dbo.PendingActions pa
JOIN dbo.AutonomousImprovementHistory aih ON pa.ActionId = aih.ActionId
WHERE pa.Status = 'Executed'
  AND aih.ActualImprovement IS NOT NULL;
```

## Health Checks

### Built-In Health Check Endpoint

**Endpoint**: `GET /health`

**Implementation** (ASP.NET Core):
```csharp
// Program.cs
builder.Services.AddHealthChecks()
    .AddSqlServer(
        connectionString: builder.Configuration.GetConnectionString("DefaultConnection"),
        name: "database",
        timeout: TimeSpan.FromSeconds(5))
    .AddCheck("neo4j", new Neo4jHealthCheck(builder.Configuration), timeout: TimeSpan.FromSeconds(5));

app.MapHealthChecks("/health", new HealthCheckOptions
{
    ResponseWriter = async (context, report) =>
    {
        context.Response.ContentType = "application/json";
        var result = JsonSerializer.Serialize(new
        {
            status = report.Status.ToString(),
            results = report.Entries.ToDictionary(
                e => e.Key,
                e => e.Value.Status.ToString()
            ),
            totalDuration = report.TotalDuration.ToString()
        });
        await context.Response.WriteAsync(result);
    }
});
```

**Monitoring Health Check**:
```powershell
# PowerShell script to check health
$response = Invoke-RestMethod -Uri "https://api.hartonomous.local/health" -Method Get
if ($response.status -ne "Healthy") {
    Send-MailMessage -To "ops@company.com" -Subject "Hartonomous Health Check Failed" -Body ($response | ConvertTo-Json)
}
```

### Custom Health Checks

**OODA Loop Activity**:
```sql
-- Alert if OODA loop hasn't run in 2 hours
IF NOT EXISTS (
    SELECT 1
    FROM dbo.LearningMetrics
    WHERE CreatedAt > DATEADD(hour, -2, GETUTCDATE())
)
BEGIN
    -- Raise alert (could integrate with monitoring system)
    RAISERROR('OODA loop inactive for 2+ hours', 16, 1);
END
```

**Worker Service Activity**:
```sql
-- Check CES Consumer last activity
IF NOT EXISTS (
    SELECT 1
    FROM dbo.IngestionJob
    WHERE CompletedAt > DATEADD(hour, -1, GETUTCDATE())
      AND JobStatus = 'Completed'
)
BEGIN
    -- Check if there are pending jobs
    IF EXISTS (SELECT 1 FROM dbo.IngestionJob WHERE JobStatus = 'Pending')
    BEGIN
        RAISERROR('CES Consumer may be stuck - pending jobs exist but none completed in last hour', 16, 1);
    END
END
```

## Alerting Rules

### Critical Alerts (Page On-Call)

1. **API Down**: Health check returns Unhealthy for 2 consecutive minutes
2. **Database Down**: Connection failure for 1 minute
3. **Deadlock Storm**: > 10 deadlocks in 5 minutes
4. **Disk Space**: < 10% free on database drive
5. **OODA Loop Failure**: 3+ consecutive failed autonomous actions

### Warning Alerts (Email/Slack)

1. **High Error Rate**: API error rate > 5% for 10 minutes
2. **Slow Queries**: P95 latency > 2× baseline for 15 minutes
3. **Index Fragmentation**: Spatial index fragmentation > 30%
4. **Memory Pressure**: SQL Server available memory < 20%
5. **Worker Lag**: CES queue depth > 1000 messages

### Informational Alerts (Dashboard Only)

1. **Ingestion Spike**: 10× normal ingestion rate
2. **New Concept Discovered**: OODA loop creates new concept cluster
3. **Storage Growth**: Daily growth > 100GB
4. **Deduplication Drop**: Deduplication rate < 50% (baseline: 80%)

## Dashboard Queries

### Application Insights Workbook

```kusto
// Ingestion Pipeline Performance
requests
| where operation_Name startswith "POST /api/v1/ingestion"
| summarize
    Count = count(),
    P50 = percentile(duration, 50),
    P95 = percentile(duration, 95),
    ErrorRate = 100.0 * countif(success == false) / count()
  by bin(timestamp, 5m), operation_Name
| render timechart

// Atom Growth Rate
customEvents
| where name == "AtomIngested"
| summarize AtomsPerMinute = count() / 60 by bin(timestamp, 1h)
| render timechart

// OODA Loop Actions
customEvents
| where name == "OODA_Action_Executed"
| extend ActionType = tostring(customDimensions.ActionType)
| summarize Count = count() by ActionType, bin(timestamp, 1d)
| render barchart
```

### SQL Server Monitoring View

```sql
CREATE VIEW monitoring.SystemHealth AS
SELECT
    'QueryPerformance' AS MetricCategory,
    'SlowQueries' AS MetricName,
    COUNT(*) AS Value,
    GETUTCDATE() AS Timestamp
FROM sys.dm_exec_query_stats
WHERE total_elapsed_time / execution_count > 1000000  -- > 1 second avg

UNION ALL

SELECT
    'Storage',
    'DeduplicationRate',
    CAST((1.0 - CAST(COUNT(DISTINCT ContentHash) AS FLOAT) / COUNT(*)) * 100 AS INT),
    GETUTCDATE()
FROM dbo.Atom

UNION ALL

SELECT
    'OODA',
    'SuccessRate',
    CAST(SUM(CASE WHEN Status = 'Success' THEN 1 ELSE 0 END) * 100.0 / COUNT(*) AS INT),
    GETUTCDATE()
FROM dbo.AutonomousImprovementHistory
WHERE CreatedAt > DATEADD(hour, -24, GETUTCDATE());
```

## Log Aggregation

### Structured Logging (Serilog)

**Configuration**:
```json
{
  "Serilog": {
    "Using": ["Serilog.Sinks.Console", "Serilog.Sinks.ApplicationInsights"],
    "MinimumLevel": {
      "Default": "Information",
      "Override": {
        "Microsoft": "Warning",
        "System": "Warning",
        "Hartonomous": "Debug"
      }
    },
    "WriteTo": [
      { "Name": "Console" },
      {
        "Name": "ApplicationInsights",
        "Args": {
          "connectionString": "InstrumentationKey=...",
          "telemetryConverter": "Serilog.Sinks.ApplicationInsights.TelemetryConverters.TraceTelemetryConverter, Serilog.Sinks.ApplicationInsights"
        }
      }
    ],
    "Enrich": ["FromLogContext", "WithMachineName", "WithThreadId"]
  }
}
```

### Log Queries

**Error Investigation**:
```kusto
traces
| where severityLevel >= 3  -- Error and above
| where timestamp > ago(1h)
| where message contains "Atomization"
| project timestamp, message, severityLevel, customDimensions
| order by timestamp desc
```

**Performance Investigation**:
```kusto
dependencies
| where name contains "SQL"
| where duration > 1000  -- > 1 second
| summarize
    Count = count(),
    AvgDuration = avg(duration),
    P95 = percentile(duration, 95)
  by target, name
| order by P95 desc
```

## Grafana Dashboard (Alternative to Application Insights)

### SQL Server Data Source

**Query for Ingestion Rate**:
```sql
SELECT
    $__timeGroup(CreatedAt, '5m', 0) AS time,
    COUNT(*) AS "Atoms Ingested"
FROM dbo.Atom
WHERE $__timeFilter(CreatedAt)
GROUP BY $__timeGroup(CreatedAt, '5m', 0)
ORDER BY time;
```

**Query for OODA Success Rate**:
```sql
SELECT
    $__timeGroup(CreatedAt, '1h', 0) AS time,
    CAST(SUM(CASE WHEN Status = 'Success' THEN 1 ELSE 0 END) * 100.0 / COUNT(*) AS INT) AS "Success Rate %"
FROM dbo.AutonomousImprovementHistory
WHERE $__timeFilter(CreatedAt)
GROUP BY $__timeGroup(CreatedAt, '1h', 0)
ORDER BY time;
```

---

**Document Version**: 2.0
**Last Updated**: January 2025
