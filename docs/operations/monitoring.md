# Hartonomous Monitoring & Observability Guide

**Application Insights | Performance Counters | Health Checks | Alert Rules | Dashboards**

---

## Table of Contents

1. [Overview](#overview)
2. [Application Insights Integration](#application-insights-integration)
3. [Performance Counters](#performance-counters)
4. [Health Check Endpoints](#health-check-endpoints)
5. [Spatial Index Health Metrics](#spatial-index-health-metrics)
6. [OODA Loop Monitoring](#ooda-loop-monitoring)
7. [Alert Rules](#alert-rules)
8. [Dashboard Creation](#dashboard-creation)
9. [Log Analytics Queries](#log-analytics-queries)
10. [Troubleshooting](#troubleshooting)

---

## Overview

Hartonomous monitoring strategy uses **multi-layered observability**:

- **Application Insights**: API telemetry, custom events, distributed tracing
- **SQL Server DMVs**: Query performance, index usage, CLR execution
- **Custom Health Checks**: Database connectivity, spatial index health, Neo4j sync status
- **Performance Counters**: SQL Server metrics, spatial query latency, OODA loop throughput
- **Azure Monitor**: Alerts, dashboards, automated responses

**Key Metrics**:
- ✅ Spatial query latency (target: <18ms for 1B atoms)
- ✅ OODA loop cycle time (15-minute scheduled + on-demand event-driven)
- ✅ CLR function execution time (deterministic projection, Hilbert curves)
- ✅ Spatial index fragmentation (R-Tree rebuilds when >30%)
- ✅ Neo4j sync lag (target: <10 seconds)

---

## Application Insights Integration

### Setup

#### 1. Create Application Insights Resource

```powershell
# Create App Insights in Azure
az monitor app-insights component create `
    --app "hartonomous-prod" `
    --location "eastus" `
    --resource-group "rg-hartonomous-prod" `
    --application-type "web" `
    --kind "web"

# Get instrumentation key
$instrumentationKey = az monitor app-insights component show `
    --app "hartonomous-prod" `
    --resource-group "rg-hartonomous-prod" `
    --query "instrumentationKey" -o tsv

# Get connection string
$connectionString = az monitor app-insights component show `
    --app "hartonomous-prod" `
    --resource-group "rg-hartonomous-prod" `
    --query "connectionString" -o tsv
```

#### 2. Configure ASP.NET Core Application

**File**: `src/Hartonomous.Api/Program.cs`

```csharp
using Microsoft.ApplicationInsights.Extensibility;

var builder = WebApplication.CreateBuilder(args);

// Add Application Insights
builder.Services.AddApplicationInsightsTelemetry(options =>
{
    options.ConnectionString = builder.Configuration["ApplicationInsights:ConnectionString"];
    options.EnableAdaptiveSampling = true;
    options.EnableDependencyTrackingTelemetryModule = true;
    options.EnablePerformanceCounterCollectionModule = true;
});

// Configure telemetry processors
builder.Services.AddSingleton<ITelemetryInitializer, CustomTelemetryInitializer>();

var app = builder.Build();

// ... middleware configuration
```

**Custom Telemetry Initializer**:

```csharp
public class CustomTelemetryInitializer : ITelemetryInitializer
{
    public void Initialize(ITelemetry telemetry)
    {
        // Add custom properties to all telemetry
        if (telemetry is ISupportProperties props)
        {
            props.Properties["Environment"] = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");
            props.Properties["MachineName"] = Environment.MachineName;
            props.Properties["HartonomousVersion"] = Assembly.GetExecutingAssembly().GetName().Version.ToString();
        }
    }
}
```

#### 3. Configure appsettings.json

```json
{
  "ApplicationInsights": {
    "ConnectionString": "InstrumentationKey=xxx;IngestionEndpoint=https://eastus-1.in.applicationinsights.azure.com/;LiveEndpoint=https://eastus.livediagnostics.monitor.azure.com/"
  },
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning",
      "Microsoft.ApplicationInsights": "Warning"
    },
    "ApplicationInsights": {
      "LogLevel": {
        "Default": "Information",
        "Hartonomous": "Trace"
      }
    }
  }
}
```

### Custom Events and Metrics

#### Track Inference Requests

```csharp
using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.DataContracts;

public class InferenceService
{
    private readonly TelemetryClient _telemetry;
    
    public InferenceService(TelemetryClient telemetry)
    {
        _telemetry = telemetry;
    }
    
    public async Task<string> GenerateResponse(string prompt, int maxTokens)
    {
        var stopwatch = Stopwatch.StartNew();
        
        try
        {
            // Execute spatial next token query
            var result = await ExecuteSpatialInference(prompt, maxTokens);
            
            stopwatch.Stop();
            
            // Track successful inference
            _telemetry.TrackEvent("InferenceCompleted", new Dictionary<string, string>
            {
                {"PromptLength", prompt.Length.ToString()},
                {"TokensGenerated", result.TokenCount.ToString()},
                {"Model", "Spatial-O(logN)"},
                {"Duration", stopwatch.ElapsedMilliseconds.ToString()}
            });
            
            // Track custom metric
            _telemetry.TrackMetric("InferenceLatency", stopwatch.ElapsedMilliseconds);
            _telemetry.TrackMetric("TokensPerSecond", result.TokenCount / (stopwatch.ElapsedMilliseconds / 1000.0));
            
            return result.GeneratedText;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            
            // Track exception with context
            var exceptionTelemetry = new ExceptionTelemetry(ex)
            {
                SeverityLevel = SeverityLevel.Error
            };
            exceptionTelemetry.Properties.Add("PromptLength", prompt.Length.ToString());
            exceptionTelemetry.Properties.Add("MaxTokens", maxTokens.ToString());
            exceptionTelemetry.Metrics.Add("Duration", stopwatch.ElapsedMilliseconds);
            
            _telemetry.TrackException(exceptionTelemetry);
            
            throw;
        }
    }
}
```

#### Track OODA Loop Cycles

```csharp
public class OodaLoopService
{
    private readonly TelemetryClient _telemetry;
    
    public async Task ExecuteOodaCycle()
    {
        using var operation = _telemetry.StartOperation<DependencyTelemetry>("OODA-Cycle");
        
        try
        {
            // Observe
            var analyzeStopwatch = Stopwatch.StartNew();
            var observations = await ObserveSystem();
            analyzeStopwatch.Stop();
            _telemetry.TrackMetric("OODA-Analyze-Duration", analyzeStopwatch.ElapsedMilliseconds);
            _telemetry.TrackEvent("OODA-Analyze", new Dictionary<string, string>
            {
                {"ObservationCount", observations.Count.ToString()}
            });
            
            // Orient
            var orientStopwatch = Stopwatch.StartNew();
            var hypotheses = await OrientAndHypothesize(observations);
            orientStopwatch.Stop();
            _telemetry.TrackMetric("OODA-Hypothesize-Duration", orientStopwatch.ElapsedMilliseconds);
            _telemetry.TrackEvent("OODA-Hypothesize", new Dictionary<string, string>
            {
                {"HypothesisCount", hypotheses.Count.ToString()}
            });
            
            // Decide
            var decideStopwatch = Stopwatch.StartNew();
            var actions = await Decide(hypotheses);
            decideStopwatch.Stop();
            _telemetry.TrackMetric("OODA-Decide-Duration", decideStopwatch.ElapsedMilliseconds);
            
            // Act
            var actStopwatch = Stopwatch.StartNew();
            var results = await Act(actions);
            actStopwatch.Stop();
            _telemetry.TrackMetric("OODA-Act-Duration", actStopwatch.ElapsedMilliseconds);
            _telemetry.TrackEvent("OODA-Act", new Dictionary<string, string>
            {
                {"ActionsExecuted", results.Count.ToString()},
                {"SuccessRate", (results.Count(r => r.Success) / (double)results.Count).ToString("P")}
            });
            
            // Learn
            await Learn(results);
            
            operation.Telemetry.Success = true;
        }
        catch (Exception ex)
        {
            operation.Telemetry.Success = false;
            _telemetry.TrackException(ex);
            throw;
        }
    }
}
```

---

## Performance Counters

### SQL Server Performance Counters

#### Critical Counters to Monitor

**Spatial Index Performance**:
```sql
-- Query spatial index statistics
SELECT 
    OBJECT_NAME(i.object_id) AS TableName,
    i.name AS IndexName,
    i.type_desc AS IndexType,
    s.index_level,
    s.page_count,
    s.avg_fragmentation_in_percent,
    s.avg_page_space_used_in_percent,
    s.record_count
FROM sys.dm_db_index_physical_stats(DB_ID(), NULL, NULL, NULL, 'DETAILED') s
INNER JOIN sys.indexes i ON s.object_id = i.object_id AND s.index_id = i.index_id
WHERE i.type_desc = 'SPATIAL'
ORDER BY s.avg_fragmentation_in_percent DESC;
```

**CLR Function Execution Time**:
```sql
-- Monitor CLR function performance
SELECT 
    OBJECT_NAME(object_id) AS FunctionName,
    cached_time,
    last_execution_time,
    execution_count,
    total_worker_time / execution_count AS avg_cpu_time_us,
    total_elapsed_time / execution_count AS avg_elapsed_time_us,
    total_logical_reads / execution_count AS avg_logical_reads,
    total_physical_reads / execution_count AS avg_physical_reads
FROM sys.dm_exec_function_stats
WHERE OBJECT_NAME(object_id) LIKE 'clr_%'
ORDER BY total_worker_time DESC;
```

**Query Performance**:
```sql
-- Top 10 slowest queries
SELECT TOP 10
    qs.execution_count,
    qs.total_elapsed_time / qs.execution_count AS avg_elapsed_time_us,
    qs.total_worker_time / qs.execution_count AS avg_cpu_time_us,
    qs.total_logical_reads / qs.execution_count AS avg_logical_reads,
    SUBSTRING(qt.text, (qs.statement_start_offset/2)+1,
        ((CASE qs.statement_end_offset
            WHEN -1 THEN DATALENGTH(qt.text)
            ELSE qs.statement_end_offset
        END - qs.statement_start_offset)/2) + 1) AS query_text,
    qp.query_plan
FROM sys.dm_exec_query_stats qs
CROSS APPLY sys.dm_exec_sql_text(qs.sql_handle) qt
CROSS APPLY sys.dm_exec_query_plan(qs.plan_handle) qp
WHERE qt.text LIKE '%AtomEmbedding%' OR qt.text LIKE '%SpatialKey%'
ORDER BY avg_elapsed_time_us DESC;
```

**OODA Loop Queue Depth**:
```sql
-- Service Broker queue monitoring
SELECT 
    q.name AS QueueName,
    q.is_receive_enabled,
    q.is_enqueue_enabled,
    COUNT(c.conversation_handle) AS MessageCount,
    MAX(c.state_desc) AS ConversationState
FROM sys.service_queues q
LEFT JOIN sys.conversation_endpoints c ON q.object_id = c.service_id
WHERE q.name IN ('AnalyzeQueue', 'HypothesizeQueue', 'ActQueue', 'LearnQueue')
GROUP BY q.name, q.is_receive_enabled, q.is_enqueue_enabled;
```

### Custom Performance Counters (T-SQL)

#### Create Monitoring Stored Procedure

```sql
CREATE OR ALTER PROCEDURE dbo.sp_CollectPerformanceMetrics
AS
BEGIN
    SET NOCOUNT ON;
    
    -- Spatial query performance
    INSERT INTO dbo.PerformanceMetrics (MetricName, MetricValue, CollectedAt)
    SELECT 
        'SpatialQueryAvgLatency' AS MetricName,
        AVG(total_elapsed_time / execution_count) AS MetricValue,
        GETUTCDATE() AS CollectedAt
    FROM sys.dm_exec_query_stats qs
    CROSS APPLY sys.dm_exec_sql_text(qs.sql_handle) qt
    WHERE qt.text LIKE '%STIntersects%' OR qt.text LIKE '%STDistance%';
    
    -- CLR function execution count
    INSERT INTO dbo.PerformanceMetrics (MetricName, MetricValue, CollectedAt)
    SELECT 
        'CLR_' + OBJECT_NAME(object_id) AS MetricName,
        execution_count AS MetricValue,
        GETUTCDATE() AS CollectedAt
    FROM sys.dm_exec_function_stats
    WHERE OBJECT_NAME(object_id) LIKE 'clr_%';
    
    -- Spatial index fragmentation
    INSERT INTO dbo.PerformanceMetrics (MetricName, MetricValue, CollectedAt)
    SELECT 
        'SpatialIndexFragmentation_' + OBJECT_NAME(s.object_id) AS MetricName,
        s.avg_fragmentation_in_percent AS MetricValue,
        GETUTCDATE() AS CollectedAt
    FROM sys.dm_db_index_physical_stats(DB_ID(), NULL, NULL, NULL, 'LIMITED') s
    INNER JOIN sys.indexes i ON s.object_id = i.object_id AND s.index_id = i.index_id
    WHERE i.type_desc = 'SPATIAL';
    
    -- OODA queue depth
    INSERT INTO dbo.PerformanceMetrics (MetricName, MetricValue, CollectedAt)
    SELECT 
        'OODA_QueueDepth_' + q.name AS MetricName,
        COUNT(c.conversation_handle) AS MetricValue,
        GETUTCDATE() AS CollectedAt
    FROM sys.service_queues q
    LEFT JOIN sys.conversation_endpoints c ON q.object_id = c.service_id
    WHERE q.name IN ('AnalyzeQueue', 'HypothesizeQueue', 'ActQueue', 'LearnQueue')
    GROUP BY q.name;
END
GO

-- Schedule collection every 5 minutes
EXEC msdb.dbo.sp_add_job @job_name = 'CollectPerformanceMetrics';
EXEC msdb.dbo.sp_add_jobstep 
    @job_name = 'CollectPerformanceMetrics',
    @step_name = 'Collect',
    @subsystem = 'TSQL',
    @command = 'EXEC dbo.sp_CollectPerformanceMetrics',
    @database_name = 'Hartonomous';

EXEC msdb.dbo.sp_add_schedule 
    @schedule_name = 'Every5Minutes',
    @freq_type = 4,          -- Daily
    @freq_interval = 1,      -- Every day
    @freq_subday_type = 4,   -- Minutes
    @freq_subday_interval = 5;

EXEC msdb.dbo.sp_attach_schedule 
    @job_name = 'CollectPerformanceMetrics',
    @schedule_name = 'Every5Minutes';

EXEC msdb.dbo.sp_add_jobserver @job_name = 'CollectPerformanceMetrics';
```

---

## Health Check Endpoints

### ASP.NET Core Health Checks

#### Configure Health Checks

**File**: `src/Hartonomous.Api/Program.cs`

```csharp
using Microsoft.Extensions.Diagnostics.HealthChecks;

var builder = WebApplication.CreateBuilder(args);

// Add health checks
builder.Services.AddHealthChecks()
    .AddSqlServer(
        connectionString: builder.Configuration.GetConnectionString("SqlServer"),
        name: "database",
        failureStatus: HealthStatus.Unhealthy,
        tags: new[] { "db", "sql" })
    .AddNeo4j(
        neo4jConnectionString: builder.Configuration.GetConnectionString("Neo4j"),
        neo4jCredentials: (
            builder.Configuration["Neo4j:Username"],
            builder.Configuration["Neo4j:Password"]),
        name: "neo4j",
        failureStatus: HealthStatus.Degraded,
        tags: new[] { "db", "graph" })
    .AddCheck<SpatialIndexHealthCheck>("spatial-indexes", tags: new[] { "spatial" })
    .AddCheck<OodaLoopHealthCheck>("ooda-loop", tags: new[] { "ooda" });

var app = builder.Build();

// Map health check endpoints
app.MapHealthChecks("/health");
app.MapHealthChecks("/health/ready", new HealthCheckOptions
{
    Predicate = check => check.Tags.Contains("db")
});
app.MapHealthChecks("/health/live", new HealthCheckOptions
{
    Predicate = _ => false  // Only basic liveness
});
```

#### Custom Health Checks

**Spatial Index Health Check**:

```csharp
public class SpatialIndexHealthCheck : IHealthCheck
{
    private readonly IConfiguration _configuration;
    
    public SpatialIndexHealthCheck(IConfiguration configuration)
    {
        _configuration = configuration;
    }
    
    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context, 
        CancellationToken cancellationToken = default)
    {
        try
        {
            using var connection = new SqlConnection(_configuration.GetConnectionString("SqlServer"));
            await connection.OpenAsync(cancellationToken);
            
            // Check spatial index fragmentation
            var fragmentationQuery = @"
                SELECT 
                    OBJECT_NAME(s.object_id) AS TableName,
                    i.name AS IndexName,
                    s.avg_fragmentation_in_percent AS Fragmentation
                FROM sys.dm_db_index_physical_stats(DB_ID(), NULL, NULL, NULL, 'LIMITED') s
                INNER JOIN sys.indexes i ON s.object_id = i.object_id AND s.index_id = i.index_id
                WHERE i.type_desc = 'SPATIAL';";
            
            using var command = new SqlCommand(fragmentationQuery, connection);
            using var reader = await command.ExecuteReaderAsync(cancellationToken);
            
            var data = new Dictionary<string, object>();
            var maxFragmentation = 0.0;
            
            while (await reader.ReadAsync(cancellationToken))
            {
                var tableName = reader.GetString(0);
                var indexName = reader.GetString(1);
                var fragmentation = reader.GetDouble(2);
                
                data[$"{tableName}.{indexName}"] = $"{fragmentation:F2}%";
                
                if (fragmentation > maxFragmentation)
                    maxFragmentation = fragmentation;
            }
            
            // Healthy: <10%, Degraded: 10-30%, Unhealthy: >30%
            if (maxFragmentation > 30)
            {
                return HealthCheckResult.Unhealthy(
                    $"Spatial index fragmentation critical: {maxFragmentation:F2}% (rebuild required)",
                    data: data);
            }
            
            if (maxFragmentation > 10)
            {
                return HealthCheckResult.Degraded(
                    $"Spatial index fragmentation elevated: {maxFragmentation:F2}% (consider reorganize)",
                    data: data);
            }
            
            return HealthCheckResult.Healthy(
                $"All spatial indexes healthy (max fragmentation: {maxFragmentation:F2}%)",
                data: data);
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy("Spatial index health check failed", ex);
        }
    }
}
```

**OODA Loop Health Check**:

```csharp
public class OodaLoopHealthCheck : IHealthCheck
{
    private readonly IConfiguration _configuration;
    
    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context, 
        CancellationToken cancellationToken = default)
    {
        try
        {
            using var connection = new SqlConnection(_configuration.GetConnectionString("SqlServer"));
            await connection.OpenAsync(cancellationToken);
            
            // Check Service Broker queue status
            var queueStatusQuery = @"
                SELECT 
                    q.name AS QueueName,
                    q.is_receive_enabled AS ReceiveEnabled,
                    q.is_enqueue_enabled AS EnqueueEnabled,
                    COUNT(c.conversation_handle) AS MessageCount
                FROM sys.service_queues q
                LEFT JOIN sys.conversation_endpoints c ON q.object_id = c.service_id
                WHERE q.name IN ('AnalyzeQueue', 'HypothesizeQueue', 'ActQueue', 'LearnQueue')
                GROUP BY q.name, q.is_receive_enabled, q.is_enqueue_enabled;";
            
            using var command = new SqlCommand(queueStatusQuery, connection);
            using var reader = await command.ExecuteReaderAsync(cancellationToken);
            
            var data = new Dictionary<string, object>();
            var totalMessages = 0;
            var disabledQueues = new List<string>();
            
            while (await reader.ReadAsync(cancellationToken))
            {
                var queueName = reader.GetString(0);
                var receiveEnabled = reader.GetBoolean(1);
                var enqueueEnabled = reader.GetBoolean(2);
                var messageCount = reader.GetInt32(3);
                
                data[queueName] = new
                {
                    MessageCount = messageCount,
                    ReceiveEnabled = receiveEnabled,
                    EnqueueEnabled = enqueueEnabled
                };
                
                totalMessages += messageCount;
                
                if (!receiveEnabled || !enqueueEnabled)
                    disabledQueues.Add(queueName);
            }
            
            // Check last OODA cycle execution
            var lastCycleQuery = @"
                SELECT TOP 1 CompletedAt
                FROM dbo.OodaCycleHistory
                ORDER BY CompletedAt DESC;";
            
            using var cycleCommand = new SqlCommand(lastCycleQuery, connection);
            var lastCycle = await cycleCommand.ExecuteScalarAsync(cancellationToken) as DateTime?;
            
            if (lastCycle.HasValue)
            {
                var timeSinceLastCycle = DateTime.UtcNow - lastCycle.Value;
                data["LastCycleMinutesAgo"] = timeSinceLastCycle.TotalMinutes;
                
                // Alert if no cycle in 30 minutes (2× expected 15-minute interval)
                if (timeSinceLastCycle.TotalMinutes > 30)
                {
                    return HealthCheckResult.Degraded(
                        $"OODA loop hasn't executed in {timeSinceLastCycle.TotalMinutes:F0} minutes",
                        data: data);
                }
            }
            
            if (disabledQueues.Any())
            {
                return HealthCheckResult.Unhealthy(
                    $"OODA queues disabled: {string.Join(", ", disabledQueues)}",
                    data: data);
            }
            
            if (totalMessages > 10000)
            {
                return HealthCheckResult.Degraded(
                    $"OODA queue backlog: {totalMessages} messages pending",
                    data: data);
            }
            
            return HealthCheckResult.Healthy(
                $"OODA loop healthy ({totalMessages} messages in queues)",
                data: data);
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy("OODA loop health check failed", ex);
        }
    }
}
```

### Health Check Response Format

**Endpoint**: `/health`

```json
{
  "status": "Healthy",
  "totalDuration": "00:00:00.1234567",
  "entries": {
    "database": {
      "status": "Healthy",
      "duration": "00:00:00.0456789",
      "tags": ["db", "sql"]
    },
    "neo4j": {
      "status": "Healthy",
      "duration": "00:00:00.0123456",
      "tags": ["db", "graph"]
    },
    "spatial-indexes": {
      "status": "Healthy",
      "description": "All spatial indexes healthy (max fragmentation: 5.23%)",
      "duration": "00:00:00.0891234",
      "data": {
        "AtomEmbedding.IX_AtomEmbedding_Spatial": "5.23%",
        "TensorAtom.IX_TensorAtom_Spatial": "3.87%"
      },
      "tags": ["spatial"]
    },
    "ooda-loop": {
      "status": "Healthy",
      "description": "OODA loop healthy (47 messages in queues)",
      "duration": "00:00:00.0234567",
      "data": {
        "AnalyzeQueue": {
          "MessageCount": 12,
          "ReceiveEnabled": true,
          "EnqueueEnabled": true
        },
        "HypothesizeQueue": {
          "MessageCount": 15,
          "ReceiveEnabled": true,
          "EnqueueEnabled": true
        },
        "ActQueue": {
          "MessageCount": 10,
          "ReceiveEnabled": true,
          "EnqueueEnabled": true
        },
        "LearnQueue": {
          "MessageCount": 10,
          "ReceiveEnabled": true,
          "EnqueueEnabled": true
        },
        "LastCycleMinutesAgo": 7.5
      },
      "tags": ["ooda"]
    }
  }
}
```

---

## Spatial Index Health Metrics

### Monitoring Queries

#### Spatial Index Fragmentation

```sql
-- Check fragmentation levels for all spatial indexes
SELECT 
    OBJECT_SCHEMA_NAME(s.object_id) AS SchemaName,
    OBJECT_NAME(s.object_id) AS TableName,
    i.name AS IndexName,
    i.type_desc AS IndexType,
    s.index_level AS Level,
    s.page_count AS Pages,
    s.avg_fragmentation_in_percent AS FragmentationPercent,
    s.avg_page_space_used_in_percent AS PageFullnessPercent,
    s.record_count AS Records,
    CASE 
        WHEN s.avg_fragmentation_in_percent < 10 THEN 'Healthy'
        WHEN s.avg_fragmentation_in_percent < 30 THEN 'Reorganize'
        ELSE 'Rebuild'
    END AS RecommendedAction
FROM sys.dm_db_index_physical_stats(DB_ID(), NULL, NULL, NULL, 'DETAILED') s
INNER JOIN sys.indexes i ON s.object_id = i.object_id AND s.index_id = i.index_id
WHERE i.type_desc = 'SPATIAL'
ORDER BY s.avg_fragmentation_in_percent DESC;
```

#### Spatial Index Usage Statistics

```sql
-- Monitor spatial index usage patterns
SELECT 
    OBJECT_NAME(s.object_id) AS TableName,
    i.name AS IndexName,
    s.user_seeks AS Seeks,
    s.user_scans AS Scans,
    s.user_lookups AS Lookups,
    s.user_updates AS Updates,
    s.last_user_seek AS LastSeek,
    s.last_user_scan AS LastScan,
    CASE 
        WHEN s.user_seeks + s.user_scans + s.user_lookups = 0 THEN 'Unused'
        WHEN s.user_seeks + s.user_scans + s.user_lookups < s.user_updates THEN 'More writes than reads'
        ELSE 'Actively used'
    END AS UsagePattern
FROM sys.dm_db_index_usage_stats s
INNER JOIN sys.indexes i ON s.object_id = i.object_id AND s.index_id = i.index_id
WHERE i.type_desc = 'SPATIAL'
    AND s.database_id = DB_ID()
ORDER BY (s.user_seeks + s.user_scans + s.user_lookups) DESC;
```

#### Spatial Query Performance

```sql
-- Analyze spatial query performance (last hour)
WITH SpatialQueries AS (
    SELECT 
        qs.execution_count,
        qs.total_elapsed_time / qs.execution_count AS avg_elapsed_time_us,
        qs.total_worker_time / qs.execution_count AS avg_cpu_time_us,
        qs.total_logical_reads / qs.execution_count AS avg_logical_reads,
        qs.total_physical_reads / qs.execution_count AS avg_physical_reads,
        qs.last_execution_time,
        SUBSTRING(qt.text, (qs.statement_start_offset/2)+1,
            ((CASE qs.statement_end_offset
                WHEN -1 THEN DATALENGTH(qt.text)
                ELSE qs.statement_end_offset
            END - qs.statement_start_offset)/2) + 1) AS query_text
    FROM sys.dm_exec_query_stats qs
    CROSS APPLY sys.dm_exec_sql_text(qs.sql_handle) qt
    WHERE (qt.text LIKE '%STIntersects%' OR qt.text LIKE '%STDistance%' OR qt.text LIKE '%STWithin%')
        AND qs.last_execution_time > DATEADD(HOUR, -1, GETUTCDATE())
)
SELECT 
    execution_count,
    avg_elapsed_time_us / 1000.0 AS avg_elapsed_time_ms,
    avg_cpu_time_us / 1000.0 AS avg_cpu_time_ms,
    avg_logical_reads,
    avg_physical_reads,
    last_execution_time,
    LEFT(query_text, 200) AS query_preview
FROM SpatialQueries
ORDER BY avg_elapsed_time_us DESC;
```

### Automated Index Maintenance

```sql
CREATE OR ALTER PROCEDURE dbo.sp_MaintainSpatialIndexes
    @FragmentationThreshold FLOAT = 30.0,
    @ReorganizeThreshold FLOAT = 10.0
AS
BEGIN
    SET NOCOUNT ON;
    
    DECLARE @TableName NVARCHAR(256);
    DECLARE @IndexName NVARCHAR(256);
    DECLARE @Fragmentation FLOAT;
    DECLARE @SQL NVARCHAR(MAX);
    
    DECLARE IndexCursor CURSOR FOR
    SELECT 
        OBJECT_SCHEMA_NAME(s.object_id) + '.' + OBJECT_NAME(s.object_id) AS TableName,
        i.name AS IndexName,
        s.avg_fragmentation_in_percent AS Fragmentation
    FROM sys.dm_db_index_physical_stats(DB_ID(), NULL, NULL, NULL, 'LIMITED') s
    INNER JOIN sys.indexes i ON s.object_id = i.object_id AND s.index_id = i.index_id
    WHERE i.type_desc = 'SPATIAL'
        AND s.avg_fragmentation_in_percent >= @ReorganizeThreshold;
    
    OPEN IndexCursor;
    FETCH NEXT FROM IndexCursor INTO @TableName, @IndexName, @Fragmentation;
    
    WHILE @@FETCH_STATUS = 0
    BEGIN
        IF @Fragmentation >= @FragmentationThreshold
        BEGIN
            -- Rebuild index
            SET @SQL = 'ALTER INDEX [' + @IndexName + '] ON ' + @TableName + ' REBUILD WITH (ONLINE = OFF);';
            PRINT 'Rebuilding: ' + @IndexName + ' (' + CAST(@Fragmentation AS NVARCHAR(10)) + '% fragmentation)';
        END
        ELSE
        BEGIN
            -- Reorganize index
            SET @SQL = 'ALTER INDEX [' + @IndexName + '] ON ' + @TableName + ' REORGANIZE;';
            PRINT 'Reorganizing: ' + @IndexName + ' (' + CAST(@Fragmentation AS NVARCHAR(10)) + '% fragmentation)';
        END
        
        EXEC sp_executesql @SQL;
        
        FETCH NEXT FROM IndexCursor INTO @TableName, @IndexName, @Fragmentation;
    END
    
    CLOSE IndexCursor;
    DEALLOCATE IndexCursor;
    
    -- Update statistics
    EXEC sp_updatestats;
END
GO

-- Schedule weekly spatial index maintenance
EXEC msdb.dbo.sp_add_job @job_name = 'SpatialIndexMaintenance';
EXEC msdb.dbo.sp_add_jobstep 
    @job_name = 'SpatialIndexMaintenance',
    @step_name = 'Maintain',
    @subsystem = 'TSQL',
    @command = 'EXEC dbo.sp_MaintainSpatialIndexes @FragmentationThreshold = 30.0, @ReorganizeThreshold = 10.0',
    @database_name = 'Hartonomous';

EXEC msdb.dbo.sp_add_schedule 
    @schedule_name = 'WeeklySunday2AM',
    @freq_type = 8,          -- Weekly
    @freq_interval = 1,      -- Sunday
    @freq_recurrence_factor = 1,
    @active_start_time = 020000;

EXEC msdb.dbo.sp_attach_schedule 
    @job_name = 'SpatialIndexMaintenance',
    @schedule_name = 'WeeklySunday2AM';

EXEC msdb.dbo.sp_add_jobserver @job_name = 'SpatialIndexMaintenance';
```

---

## OODA Loop Monitoring

### Service Broker Queue Monitoring

```sql
-- Comprehensive OODA loop monitoring query
WITH QueueStats AS (
    SELECT 
        q.name AS QueueName,
        q.is_receive_enabled AS ReceiveEnabled,
        q.is_enqueue_enabled AS EnqueueEnabled,
        COUNT(c.conversation_handle) AS MessageCount,
        MAX(c.state_desc) AS ConversationState,
        MAX(c.is_initiator) AS IsInitiator
    FROM sys.service_queues q
    LEFT JOIN sys.conversation_endpoints c ON q.object_id = c.service_id
    WHERE q.name IN ('AnalyzeQueue', 'HypothesizeQueue', 'ActQueue', 'LearnQueue')
    GROUP BY q.name, q.is_receive_enabled, q.is_enqueue_enabled
),
CycleHistory AS (
    SELECT 
        CompletedAt,
        CycleDurationMs,
        ObservationCount,
        HypothesisCount,
        ActionCount,
        SuccessRate,
        ROW_NUMBER() OVER (ORDER BY CompletedAt DESC) AS RowNum
    FROM dbo.OodaCycleHistory
)
SELECT 
    qs.QueueName,
    qs.MessageCount,
    qs.ReceiveEnabled,
    qs.EnqueueEnabled,
    qs.ConversationState,
    ch.CompletedAt AS LastCycleAt,
    DATEDIFF(MINUTE, ch.CompletedAt, GETUTCDATE()) AS MinutesSinceLastCycle,
    ch.CycleDurationMs,
    ch.ObservationCount,
    ch.HypothesisCount,
    ch.ActionCount,
    ch.SuccessRate
FROM QueueStats qs
CROSS JOIN CycleHistory ch
WHERE ch.RowNum = 1
ORDER BY qs.QueueName;
```

### OODA Performance Metrics

```sql
-- OODA loop performance trends (last 24 hours)
SELECT 
    DATEPART(HOUR, CompletedAt) AS Hour,
    COUNT(*) AS CycleCount,
    AVG(CycleDurationMs) AS AvgDurationMs,
    MIN(CycleDurationMs) AS MinDurationMs,
    MAX(CycleDurationMs) AS MaxDurationMs,
    AVG(ObservationCount) AS AvgObservations,
    AVG(HypothesisCount) AS AvgHypotheses,
    AVG(ActionCount) AS AvgActions,
    AVG(SuccessRate) AS AvgSuccessRate
FROM dbo.OodaCycleHistory
WHERE CompletedAt >= DATEADD(HOUR, -24, GETUTCDATE())
GROUP BY DATEPART(HOUR, CompletedAt)
ORDER BY Hour;
```

### Alert on OODA Stalls

```sql
-- Create alert for stalled OODA loop
CREATE OR ALTER PROCEDURE dbo.sp_AlertOodaStall
AS
BEGIN
    DECLARE @LastCycle DATETIME;
    DECLARE @MinutesSinceLastCycle INT;
    
    SELECT TOP 1 @LastCycle = CompletedAt
    FROM dbo.OodaCycleHistory
    ORDER BY CompletedAt DESC;
    
    SET @MinutesSinceLastCycle = DATEDIFF(MINUTE, @LastCycle, GETUTCDATE());
    
    IF @MinutesSinceLastCycle > 30  -- Alert if no cycle in 30 minutes
    BEGIN
        DECLARE @AlertMessage NVARCHAR(MAX) = 
            'OODA loop has not executed in ' + CAST(@MinutesSinceLastCycle AS NVARCHAR(10)) + ' minutes. Last cycle: ' + CONVERT(NVARCHAR(30), @LastCycle, 121);
        
        -- Send alert (configure Database Mail first)
        EXEC msdb.dbo.sp_send_dbmail 
            @profile_name = 'HartonomousAlerts',
            @recipients = 'ops@hartonomous.com',
            @subject = 'CRITICAL: OODA Loop Stalled',
            @body = @AlertMessage;
        
        -- Log to Application Insights (via HTTP endpoint if configured)
        -- Or write to EventLog table for monitoring service to pick up
        INSERT INTO dbo.EventLog (EventType, Message, Severity, CreatedAt)
        VALUES ('OODA_STALL', @AlertMessage, 'CRITICAL', GETUTCDATE());
    END
END
GO

-- Schedule every 15 minutes
EXEC msdb.dbo.sp_add_job @job_name = 'MonitorOodaLoop';
EXEC msdb.dbo.sp_add_jobstep 
    @job_name = 'MonitorOodaLoop',
    @step_name = 'CheckStall',
    @subsystem = 'TSQL',
    @command = 'EXEC dbo.sp_AlertOodaStall',
    @database_name = 'Hartonomous';

EXEC msdb.dbo.sp_add_schedule 
    @schedule_name = 'Every15Minutes',
    @freq_type = 4,
    @freq_interval = 1,
    @freq_subday_type = 4,
    @freq_subday_interval = 15;

EXEC msdb.dbo.sp_attach_schedule 
    @job_name = 'MonitorOodaLoop',
    @schedule_name = 'Every15Minutes';

EXEC msdb.dbo.sp_add_jobserver @job_name = 'MonitorOodaLoop';
```

---

## Alert Rules

### Azure Monitor Alerts

#### Create Alert Rules (Azure CLI)

**Spatial Query Latency Alert**:
```powershell
# Alert when avg spatial query latency > 50ms
az monitor metrics alert create `
    --name "Spatial Query Latency High" `
    --resource-group "rg-hartonomous-prod" `
    --scopes "/subscriptions/{sub-id}/resourceGroups/rg-hartonomous-prod/providers/Microsoft.Insights/components/hartonomous-prod" `
    --condition "avg customMetrics/SpatialQueryLatency > 50" `
    --window-size 5m `
    --evaluation-frequency 1m `
    --severity 2 `
    --description "Average spatial query latency exceeded 50ms threshold"
```

**OODA Loop Failure Rate Alert**:
```powershell
# Alert when OODA success rate < 80%
az monitor metrics alert create `
    --name "OODA Success Rate Low" `
    --resource-group "rg-hartonomous-prod" `
    --scopes "/subscriptions/{sub-id}/resourceGroups/rg-hartonomous-prod/providers/Microsoft.Insights/components/hartonomous-prod" `
    --condition "avg customMetrics/OodaSuccessRate < 0.8" `
    --window-size 15m `
    --evaluation-frequency 5m `
    --severity 1 `
    --description "OODA loop success rate dropped below 80%"
```

**Neo4j Sync Lag Alert**:
```powershell
# Alert when Neo4j sync lag > 60 seconds
az monitor metrics alert create `
    --name "Neo4j Sync Lag" `
    --resource-group "rg-hartonomous-prod" `
    --scopes "/subscriptions/{sub-id}/resourceGroups/rg-hartonomous-prod/providers/Microsoft.Insights/components/hartonomous-prod" `
    --condition "max customMetrics/Neo4jSyncLagSeconds > 60" `
    --window-size 5m `
    --evaluation-frequency 1m `
    --severity 2 `
    --description "Neo4j sync lag exceeded 60 seconds"
```

### Action Groups

Create action group for alert notifications:

```powershell
# Create action group with email + webhook
az monitor action-group create `
    --name "hartonomous-ops" `
    --resource-group "rg-hartonomous-prod" `
    --short-name "HartOps" `
    --email-receiver name="Ops Team" email-address="ops@hartonomous.com" `
    --webhook-receiver name="Slack" service-uri="https://hooks.slack.com/services/xxx"
```

---

## Dashboard Creation

### Azure Portal Dashboard

#### Create Monitoring Dashboard (JSON)

**File**: `hartonomous-monitoring-dashboard.json`

```json
{
  "properties": {
    "lenses": {
      "0": {
        "order": 0,
        "parts": {
          "0": {
            "position": { "x": 0, "y": 0, "colSpan": 6, "rowSpan": 4 },
            "metadata": {
              "type": "Extension/HubsExtension/PartType/MonitorChartPart",
              "settings": {
                "content": {
                  "options": {
                    "chart": {
                      "metrics": [{
                        "resourceMetadata": { "id": "/subscriptions/{sub-id}/resourceGroups/rg-hartonomous-prod/providers/Microsoft.Insights/components/hartonomous-prod" },
                        "name": "customMetrics/SpatialQueryLatency",
                        "aggregationType": 4,
                        "namespace": "microsoft.insights/components",
                        "metricVisualization": {
                          "displayName": "Spatial Query Latency (ms)"
                        }
                      }],
                      "title": "Spatial Query Performance",
                      "titleKind": 1,
                      "visualization": { "chartType": 2 }
                    }
                  }
                }
              }
            }
          },
          "1": {
            "position": { "x": 6, "y": 0, "colSpan": 6, "rowSpan": 4 },
            "metadata": {
              "type": "Extension/HubsExtension/PartType/MonitorChartPart",
              "settings": {
                "content": {
                  "options": {
                    "chart": {
                      "metrics": [{
                        "resourceMetadata": { "id": "/subscriptions/{sub-id}/resourceGroups/rg-hartonomous-prod/providers/Microsoft.Insights/components/hartonomous-prod" },
                        "name": "customMetrics/InferenceLatency",
                        "aggregationType": 4,
                        "metricVisualization": {
                          "displayName": "Inference Latency (ms)"
                        }
                      }],
                      "title": "Inference Performance",
                      "titleKind": 1,
                      "visualization": { "chartType": 2 }
                    }
                  }
                }
              }
            }
          }
        }
      }
    },
    "metadata": {
      "model": {
        "timeRange": { "value": { "relative": { "duration": 24, "timeUnit": 1 } } }
      }
    }
  },
  "name": "Hartonomous Monitoring",
  "type": "Microsoft.Portal/dashboards",
  "location": "eastus",
  "tags": { "hidden-title": "Hartonomous Monitoring" }
}
```

Deploy dashboard:
```powershell
az portal dashboard create `
    --resource-group "rg-hartonomous-prod" `
    --name "HartonomousMonitoring" `
    --input-path "hartonomous-monitoring-dashboard.json"
```

---

## Log Analytics Queries

### KQL Queries for Common Scenarios

#### Slow Inference Requests

```kusto
customMetrics
| where name == "InferenceLatency"
| where value > 1000  // > 1 second
| extend PromptLength = toint(customDimensions.PromptLength)
| extend TokensGenerated = toint(customDimensions.TokensGenerated)
| project timestamp, value, PromptLength, TokensGenerated
| order by value desc
| take 50
```

#### OODA Loop Failures

```kusto
customEvents
| where name == "OODA-Act"
| extend SuccessRate = todouble(customDimensions.SuccessRate)
| where SuccessRate < 0.8
| project timestamp, SuccessRate, customDimensions.ActionsExecuted
| order by timestamp desc
```

#### Spatial Index Fragmentation Trends

```kusto
customMetrics
| where name startswith "SpatialIndexFragmentation_"
| summarize avg(value), max(value), min(value) by bin(timestamp, 1h), name
| render timechart
```

---

## Troubleshooting

### High Spatial Query Latency

**Symptoms**: Spatial queries taking >100ms

**Diagnosis**:
```sql
-- Check spatial index fragmentation
SELECT 
    OBJECT_NAME(s.object_id) AS TableName,
    i.name AS IndexName,
    s.avg_fragmentation_in_percent
FROM sys.dm_db_index_physical_stats(DB_ID(), NULL, NULL, NULL, 'LIMITED') s
INNER JOIN sys.indexes i ON s.object_id = i.object_id AND s.index_id = i.index_id
WHERE i.type_desc = 'SPATIAL';

-- Check spatial query plan
SET STATISTICS IO ON;
SET STATISTICS TIME ON;

SELECT TOP 100 AtomId
FROM dbo.AtomEmbedding
WHERE SpatialKey.STIntersects(geometry::Point(50, 50, 0).STBuffer(10)) = 1;
```

**Resolution**:
```sql
-- Rebuild fragmented spatial indexes
ALTER INDEX IX_AtomEmbedding_Spatial ON dbo.AtomEmbedding REBUILD;

-- Update statistics
UPDATE STATISTICS dbo.AtomEmbedding;
```

### OODA Loop Queue Backlog

**Symptoms**: MessageCount > 10,000 in OODA queues

**Diagnosis**:
```sql
-- Check queue depths
SELECT 
    q.name,
    COUNT(c.conversation_handle) AS MessageCount,
    q.is_receive_enabled,
    q.is_enqueue_enabled
FROM sys.service_queues q
LEFT JOIN sys.conversation_endpoints c ON q.object_id = c.service_id
WHERE q.name IN ('AnalyzeQueue', 'HypothesizeQueue', 'ActQueue', 'LearnQueue')
GROUP BY q.name, q.is_receive_enabled, q.is_enqueue_enabled;

-- Check internal activation status
SELECT 
    q.name,
    q.max_readers,
    COUNT(ac.conversation_handle) AS ActiveConversations
FROM sys.service_queues q
LEFT JOIN sys.dm_broker_activated_tasks ac ON q.object_id = ac.queue_id
WHERE q.name IN ('AnalyzeQueue', 'HypothesizeQueue', 'ActQueue', 'LearnQueue')
GROUP BY q.name, q.max_readers;
```

**Resolution**:
```sql
-- Increase max readers (if CPU allows)
ALTER QUEUE AnalyzeQueue WITH ACTIVATION (MAX_QUEUE_READERS = 5);

-- Temporarily disable enqueue if overwhelming
ALTER QUEUE AnalyzeQueue WITH STATUS = OFF;

-- Process backlog, then re-enable
ALTER QUEUE AnalyzeQueue WITH STATUS = ON;
```

### Neo4j Sync Lag

**Symptoms**: Neo4jSyncLagSeconds > 60

**Diagnosis**:
```sql
-- Check Neo4j sync queue
SELECT 
    COUNT(*) AS PendingSync,
    MIN(CreatedAt) AS OldestMessage,
    DATEDIFF(SECOND, MIN(CreatedAt), GETUTCDATE()) AS LagSeconds
FROM dbo.Neo4jSyncQueue
WHERE Status = 'Pending';

-- Check failed sync attempts
SELECT TOP 20 
    EntityType,
    EntityId,
    ErrorMessage,
    RetryCount,
    CreatedAt
FROM dbo.Neo4jSyncLog
WHERE Status = 'Failed'
ORDER BY CreatedAt DESC;
```

**Resolution**:
```powershell
# Restart Neo4j service
Restart-Service neo4j

# Verify Neo4j connectivity
Invoke-RestMethod -Uri "http://localhost:7474/db/data/" -Method Get

# Retry failed syncs
Invoke-Sqlcmd -Query "EXEC dbo.sp_RetryFailedNeo4jSync" -ServerInstance localhost -Database Hartonomous
```

---

## Summary

**Key Monitoring Components**:

1. ✅ **Application Insights**: API telemetry, custom events, distributed tracing
2. ✅ **Performance Counters**: SQL Server DMVs, spatial index stats, CLR execution
3. ✅ **Health Checks**: `/health`, `/health/database`, `/health/neo4j`, custom checks
4. ✅ **Alert Rules**: Spatial latency, OODA failures, Neo4j lag
5. ✅ **Dashboards**: Azure Portal dashboards, KQL queries

**Operational Targets**:
- Spatial query latency: <18ms (1B atoms)
- OODA cycle time: 15-minute scheduled interval
- Spatial index fragmentation: <10% (reorganize at 10-30%, rebuild >30%)
- Neo4j sync lag: <10 seconds
- OODA success rate: >80%

**Next Steps**:
- See `docs/operations/backup-recovery.md` for disaster recovery procedures
- See `docs/operations/performance-tuning.md` for optimization strategies
