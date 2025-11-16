# Monitoring Runbook

## Overview

This runbook defines monitoring strategies, metrics, alert thresholds, and dashboards for the Hartonomous platform. The system requires monitoring across SQL Server, Neo4j, application services, and infrastructure.

## Monitoring Architecture

### Components to Monitor

1. **SQL Server Database Engine**
   - Query performance
   - Index usage
   - Spatial query efficiency
   - CLR execution
   - Resource utilization

2. **Neo4j Provenance Graph**
   - Query latency
   - Graph traversal performance
   - Memory usage
   - Relationship growth

3. **Application Services**
   - Worker service health
   - API response times
   - OODA loop execution
   - Background job queues

4. **Infrastructure**
   - CPU, memory, disk I/O
   - Network latency
   - Storage capacity

## SQL Server Monitoring

### 1. Query Performance Monitoring

```sql
-- Create monitoring view for slow queries
CREATE OR ALTER VIEW dbo.vw_SlowQueries
AS
SELECT TOP 100
    SUBSTRING(
        qt.text,
        (qs.statement_start_offset / 2) + 1,
        ((CASE qs.statement_end_offset
            WHEN -1 THEN DATALENGTH(qt.text)
            ELSE qs.statement_end_offset
        END - qs.statement_start_offset) / 2) + 1
    ) AS query_text,
    qs.execution_count,
    qs.total_elapsed_time / 1000000.0 AS total_elapsed_sec,
    qs.total_elapsed_time / qs.execution_count / 1000.0 AS avg_elapsed_ms,
    qs.total_logical_reads / qs.execution_count AS avg_logical_reads,
    qs.total_worker_time / qs.execution_count / 1000.0 AS avg_cpu_ms,
    qs.creation_time,
    qs.last_execution_time
FROM sys.dm_exec_query_stats qs
CROSS APPLY sys.dm_exec_sql_text(qs.sql_handle) qt
WHERE qs.total_elapsed_time / qs.execution_count > 100000 -- >100ms average
ORDER BY qs.total_elapsed_time / qs.execution_count DESC;
```

**Alert Thresholds**:
- Average query time >100ms: WARNING
- Average query time >500ms: CRITICAL
- Query execution count >10000/min: INVESTIGATE

### 2. Spatial Index Health

```sql
-- Monitor spatial index usage and fragmentation
CREATE OR ALTER VIEW dbo.vw_SpatialIndexHealth
AS
SELECT 
    OBJECT_NAME(i.object_id) AS table_name,
    i.name AS index_name,
    i.type_desc,
    ps.avg_fragmentation_in_percent,
    ps.page_count,
    ius.user_seeks,
    ius.user_scans,
    ius.user_lookups,
    ius.last_user_seek,
    ius.last_user_scan
FROM sys.indexes i
INNER JOIN sys.dm_db_index_physical_stats(
    DB_ID(), 
    NULL, 
    NULL, 
    NULL, 
    'LIMITED'
) ps ON i.object_id = ps.object_id AND i.index_id = ps.index_id
LEFT JOIN sys.dm_db_index_usage_stats ius 
    ON i.object_id = ius.object_id AND i.index_id = ius.index_id
WHERE i.type_desc = 'SPATIAL';
```

**Alert Thresholds**:
- Fragmentation >30%: WARNING (rebuild index)
- Fragmentation >50%: CRITICAL (rebuild immediately)
- user_seeks = 0: INVESTIGATE (index not being used)

### 3. CLR Performance Monitoring

```sql
-- Monitor CLR function execution
CREATE TABLE dbo.ClrPerformanceMetrics (
    MetricId BIGINT IDENTITY PRIMARY KEY,
    FunctionName NVARCHAR(200),
    ExecutionTimeMs INT,
    InputSize INT,
    RecordedAt DATETIME2 DEFAULT SYSUTCDATETIME(),
    INDEX IX_ClrPerformance_RecordedAt (RecordedAt)
);

-- Add logging to CLR functions (example in VectorMath.cs)
-- After dot product calculation:
-- LogPerformance("DotProduct", elapsedMs, vectorSize);
```

**Alert Thresholds**:
- CLR function >50ms: WARNING
- CLR function >200ms: CRITICAL
- CLR exceptions >10/min: CRITICAL

### 4. OODA Loop Health

```sql
-- Monitor OODA loop execution
CREATE OR ALTER VIEW dbo.vw_OODALoopHealth
AS
SELECT 
    Phase,
    COUNT(*) AS executions_last_hour,
    AVG(DurationMs) AS avg_duration_ms,
    MAX(DurationMs) AS max_duration_ms,
    SUM(CASE WHEN Success = 0 THEN 1 ELSE 0 END) AS failures
FROM dbo.OODALoopMetrics
WHERE ExecutedAt >= DATEADD(HOUR, -1, SYSUTCDATETIME())
GROUP BY Phase;
```

**Alert Thresholds**:
- Failures >0: CRITICAL (investigate immediately)
- Executions = 0 for >15 min: CRITICAL (loop stalled)
- Average duration >1000ms: WARNING

### 5. Resource Utilization

```sql
-- Monitor SQL Server resource usage
SELECT 
    -- CPU
    (SELECT 100 - [SQLProcessUtilization]
     FROM (SELECT TOP 1 SQLProcessUtilization
           FROM sys.dm_os_ring_buffers
           WHERE ring_buffer_type = N'RING_BUFFER_SCHEDULER_MONITOR'
           ORDER BY timestamp DESC) AS cpu) AS cpu_idle_pct,
    
    -- Memory
    (SELECT CAST(physical_memory_in_use_kb AS DECIMAL(20,2)) / 1024 / 1024
     FROM sys.dm_os_process_memory) AS memory_used_gb,
    
    -- I/O
    (SELECT SUM(num_of_reads + num_of_writes)
     FROM sys.dm_io_virtual_file_stats(NULL, NULL)) AS total_io_operations;
```

**Alert Thresholds**:
- CPU >80% sustained: WARNING
- CPU >95% sustained: CRITICAL
- Memory >90% of max: WARNING
- Disk queue length >10: CRITICAL

## Neo4j Monitoring

### 1. Query Performance

```cypher
// Monitor slow queries (requires APOC)
CALL apoc.cypher.runTimeboxed(
    "MATCH (n) RETURN count(n)",
    {},
    10000 // 10 second timeout
) YIELD value
RETURN value;

// Get query log
CALL dbms.queryJmx("org.neo4j:*") 
YIELD name, attributes
WHERE name CONTAINS 'Queries'
RETURN name, attributes;
```

**Alert Thresholds**:
- Query time >1000ms: WARNING
- Query time >5000ms: CRITICAL
- Failed queries >10/min: CRITICAL

### 2. Resource Metrics

```cypher
// Memory usage
CALL dbms.queryJmx("org.neo4j:instance=kernel#0,name=Store sizes") 
YIELD attributes
RETURN attributes.ArrayStoreSize, 
       attributes.NodeStoreSize,
       attributes.RelationshipStoreSize;

// Transaction metrics
CALL dbms.queryJmx("org.neo4j:instance=kernel#0,name=Transactions") 
YIELD attributes
RETURN attributes.NumberOfOpenTransactions,
       attributes.PeakNumberOfConcurrentTransactions;
```

**Alert Thresholds**:
- Open transactions >100: WARNING
- Store size growth >10GB/day: INVESTIGATE
- Page cache hit ratio <90%: WARNING

## Application Monitoring

### 1. Worker Service Health

```csharp
// In worker services, implement health checks
public class WorkerHealthCheck : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        var lastRun = await GetLastSuccessfulRunAsync();
        var timeSinceLastRun = DateTime.UtcNow - lastRun;
        
        if (timeSinceLastRun > TimeSpan.FromMinutes(10))
            return HealthCheckResult.Unhealthy($"No successful run in {timeSinceLastRun}");
        
        return HealthCheckResult.Healthy();
    }
}
```

**Health Endpoint**: `GET /health`

**Alert Thresholds**:
- Health check failed: CRITICAL
- No successful run >10 min: WARNING
- Worker service stopped: CRITICAL

### 2. API Performance

```csharp
// Add Application Insights telemetry
services.AddApplicationInsightsTelemetry(options =>
{
    options.ConnectionString = configuration["ApplicationInsights:ConnectionString"];
});

// Track custom metrics
var telemetry = new TelemetryClient();
telemetry.TrackMetric("SpatialQuery.Duration", queryDurationMs);
telemetry.TrackMetric("AtomIngestion.Throughput", atomsPerSecond);
```

**Alert Thresholds**:
- API response time >500ms (p95): WARNING
- API response time >2000ms (p95): CRITICAL
- API error rate >5%: CRITICAL
- API availability <99.5%: CRITICAL

## Dashboards

### Grafana Dashboard Configuration

**Dashboard 1: SQL Server Performance**

```json
{
  "dashboard": {
    "title": "Hartonomous SQL Server",
    "panels": [
      {
        "title": "Query Performance",
        "targets": [{
          "query": "SELECT avg_elapsed_ms FROM vw_SlowQueries WHERE last_execution_time > DATEADD(MINUTE, -5, GETUTCDATE())"
        }],
        "type": "graph"
      },
      {
        "title": "Spatial Index Usage",
        "targets": [{
          "query": "SELECT user_seeks, user_scans FROM vw_SpatialIndexHealth"
        }],
        "type": "stat"
      },
      {
        "title": "OODA Loop Health",
        "targets": [{
          "query": "SELECT Phase, avg_duration_ms, failures FROM vw_OODALoopHealth"
        }],
        "type": "table"
      }
    ]
  }
}
```

**Dashboard 2: System Resources**

Panels:
- CPU utilization (%)
- Memory usage (GB)
- Disk I/O (IOPS)
- Network throughput (Mbps)
- Active connections
- Blocking queries

**Dashboard 3: Application Metrics**

Panels:
- API request rate (/min)
- API response time (p50, p95, p99)
- API error rate (%)
- Worker service health
- Background job queue depth
- Atom ingestion rate

## Alerting Rules

### Critical Alerts (Page Immediately)

```yaml
# AlertManager configuration
groups:
  - name: hartonomous_critical
    interval: 30s
    rules:
      - alert: DatabaseDown
        expr: up{job="sql_server"} == 0
        for: 1m
        labels:
          severity: critical
        annotations:
          summary: "SQL Server is down"
          
      - alert: OODALoopStalled
        expr: ooda_loop_executions{phase="Analyze"} == 0
        for: 15m
        labels:
          severity: critical
        annotations:
          summary: "OODA loop has not executed in 15 minutes"
          
      - alert: HighErrorRate
        expr: rate(http_requests_total{status=~"5.."}[5m]) > 0.05
        for: 5m
        labels:
          severity: critical
        annotations:
          summary: "API error rate above 5%"
```

### Warning Alerts (Email/Slack)

```yaml
  - name: hartonomous_warnings
    interval: 1m
    rules:
      - alert: SlowQueries
        expr: avg(sql_query_duration_ms) > 100
        for: 10m
        labels:
          severity: warning
        annotations:
          summary: "Average query time above 100ms"
          
      - alert: HighCPU
        expr: cpu_usage_percent > 80
        for: 10m
        labels:
          severity: warning
        annotations:
          summary: "CPU usage above 80%"
```

## Logging Strategy

### Structured Logging

```csharp
// Use Serilog for structured logging
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .Enrich.FromLogContext()
    .Enrich.WithMachineName()
    .WriteTo.Console()
    .WriteTo.File("logs/hartonomous-.log", rollingInterval: RollingInterval.Day)
    .WriteTo.Seq("http://seq-server:5341")
    .CreateLogger();

// Log with context
_logger.LogInformation(
    "Spatial query completed in {Duration}ms for {AtomCount} atoms",
    durationMs,
    atomCount
);
```

### Log Retention

- Application logs: 30 days
- SQL Server error log: 90 days  
- Performance logs: 7 days
- Audit logs: 1 year

## Monitoring Checklist

**Daily**:
- [ ] Check dashboard for anomalies
- [ ] Review critical alerts
- [ ] Verify OODA loop is running
- [ ] Check backup completion

**Weekly**:
- [ ] Review slow query trends
- [ ] Check index fragmentation
- [ ] Review error logs
- [ ] Validate alert thresholds

**Monthly**:
- [ ] Performance trend analysis
- [ ] Capacity planning review
- [ ] Update runbooks if needed
- [ ] Test alerting pipeline

## Troubleshooting Common Issues

### High CPU Usage

1. Check `vw_SlowQueries` for expensive queries
2. Review execution plans
3. Check for missing indexes
4. Investigate OODA loop frequency

### Memory Pressure

1. Check SQL Server max memory configuration
2. Review buffer pool usage
3. Check for memory leaks in workers
4. Investigate CLR memory allocation

### Slow Spatial Queries

1. Check `vw_SpatialIndexHealth` for fragmentation
2. Verify index hints are being used
3. Review BOUNDING_BOX configuration
4. Check statistics currency

## Monitoring Tools Integration

### Application Insights

```bash
# Install CLI
npm install -g @microsoft/applicationinsights

# Configure instrumentation key
export APPINSIGHTS_INSTRUMENTATIONKEY="your-key"
```

### Prometheus

```yaml
# prometheus.yml
scrape_configs:
  - job_name: 'sql_server'
    static_configs:
      - targets: ['localhost:9399']
  
  - job_name: 'neo4j'
    static_configs:
      - targets: ['localhost:2004']
      
  - job_name: 'hartonomous_api'
    static_configs:
      - targets: ['localhost:5000']
```

### Seq (Structured Logs)

```bash
# Docker deployment
docker run -d \
  --name seq \
  -e ACCEPT_EULA=Y \
  -p 5341:80 \
  -v seq-data:/data \
  datalust/seq:latest
```

## Performance Baselines

Document baseline metrics for comparison:

| Metric | Baseline | Threshold |
|--------|----------|-----------|
| Spatial query (10K atoms) | 15ms | 50ms |
| OODA Analyze phase | 200ms | 1000ms |
| API response time (p95) | 100ms | 500ms |
| Atom ingestion rate | 1000/sec | 100/sec |
| Neo4j query (3 hops) | 50ms | 200ms |

Update these baselines quarterly based on actual performance data.
