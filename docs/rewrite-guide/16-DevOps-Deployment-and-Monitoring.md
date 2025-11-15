# 16 - DevOps, Deployment, and Monitoring

Complete operational guide for deploying and monitoring Hartonomous in production.

## Part 1: Infrastructure Requirements

### SQL Server Requirements

**Minimum**: SQL Server 2019 Standard Edition
**Recommended**: SQL Server 2022/2025 Enterprise Edition

**Why Enterprise** (optional but recommended):
- In-Memory OLTP (Hekaton) for session state
- Temporal tables for weight versioning
- Better parallelism for spatial queries
- Resource Governor for multi-tenant workloads

**Configuration**:
```sql
-- Enable CLR
EXEC sp_configure 'clr enabled', 1;
RECONFIGURE;

-- Enable CLR strict security (required in modern SQL Server)
EXEC sp_configure 'clr strict security', 1;
RECONFIGURE;

-- Enable Service Broker for OODA loop
ALTER DATABASE Hartonomous SET ENABLE_BROKER;

-- Configure memory (80% of available RAM)
EXEC sp_configure 'max server memory (MB)', 81920;  -- 80GB example
RECONFIGURE;

-- Enable Query Store for automatic regression detection
ALTER DATABASE Hartonomous SET QUERY_STORE = ON;
ALTER DATABASE Hartonomous SET QUERY_STORE (
    OPERATION_MODE = READ_WRITE,
    MAX_STORAGE_SIZE_MB = 10240,
    INTERVAL_LENGTH_MINUTES = 5
);
```

### Neo4j Requirements

**Minimum**: Neo4j 5.x Community Edition
**Recommended**: Neo4j 5.x Enterprise Edition

**Configuration** (`neo4j.conf`):
```properties
# Memory
dbms.memory.heap.initial_size=4g
dbms.memory.heap.max_size=8g
dbms.memory.pagecache.size=8g

# Bolt connector
dbms.connector.bolt.enabled=true
dbms.connector.bolt.listen_address=:7687

# Security (production)
dbms.security.auth_enabled=true
```

### Hardware Sizing

| Component | Small (Dev) | Medium (Prod) | Large (Scale) |
|---|---|---|---|
| SQL Server CPU | 8 cores | 32 cores | 64+ cores |
| SQL Server RAM | 32GB | 128GB | 256GB+ |
| SQL Server Storage | 500GB SSD | 2TB NVMe | 10TB+ NVMe RAID |
| Neo4j CPU | 4 cores | 16 cores | 32 cores |
| Neo4j RAM | 16GB | 64GB | 128GB |
| Workers RAM | 8GB | 32GB | 64GB |
| **Total** | **56GB, 12 cores** | **224GB, 48 cores** | **448GB, 128+ cores** |

**Note**: No GPUs required. Standard server hardware.

## Part 2: Deployment Architecture

### Three-Tier Deployment

```
┌─────────────────────────────────────────────┐
│           Load Balancer (optional)          │
└──────────────────┬──────────────────────────┘
                   │
    ┌──────────────┴──────────────┐
    │                             │
┌───▼────────┐            ┌───────▼────┐
│   API 1    │            │   API 2    │
│ (ASP.NET)  │            │ (ASP.NET)  │
└────┬───────┘            └────┬───────┘
     │                         │
     └──────────┬──────────────┘
                │
     ┌──────────▼──────────┐
     │   SQL Server        │
     │   (Database +       │
     │    CLR Engine)      │
     └──────────┬──────────┘
                │
     ┌──────────┼──────────┐
     │          │          │
┌────▼────┐ ┌──▼───┐ ┌────▼────────┐
│Workers  │ │Neo4j │ │OODA Loop    │
│(BG Svc) │ │Graph │ │(Service Bkr)│
└─────────┘ └──────┘ └─────────────┘
```

### Containerized Deployment (Docker Compose)

```yaml
# docker-compose.yml
version: '3.8'

services:
  sqlserver:
    image: mcr.microsoft.com/mssql/server:2022-latest
    environment:
      - ACCEPT_EULA=Y
      - SA_PASSWORD=YourStrong@Password
      - MSSQL_PID=Standard
    volumes:
      - sqldata:/var/opt/mssql
      - ./dacpac:/dacpac
    ports:
      - "1433:1433"
    deploy:
      resources:
        limits:
          cpus: '16'
          memory: 64G

  neo4j:
    image: neo4j:5.15-enterprise
    environment:
      - NEO4J_AUTH=neo4j/YourPassword
      - NEO4J_dbms_memory_heap_max__size=8G
      - NEO4J_dbms_memory_pagecache_size=8G
    volumes:
      - neo4jdata:/data
    ports:
      - "7474:7474"
      - "7687:7687"
    deploy:
      resources:
        limits:
          cpus: '8'
          memory: 16G

  api:
    build: ./src/Hartonomous.Api
    environment:
      - ConnectionStrings__HartonomousDb=Server=sqlserver;Database=Hartonomous;User=sa;Password=YourStrong@Password
      - ConnectionStrings__Neo4j=bolt://neo4j:7687
    ports:
      - "5000:80"
    depends_on:
      - sqlserver
      - neo4j
    deploy:
      replicas: 2

  worker-ingestion:
    build: ./src/Hartonomous.Workers.Ingestion
    environment:
      - ConnectionStrings__HartonomousDb=Server=sqlserver;Database=Hartonomous;User=sa;Password=YourStrong@Password
    depends_on:
      - sqlserver

  worker-neo4j-sync:
    build: ./src/Hartonomous.Workers.Neo4jSync
    environment:
      - ConnectionStrings__HartonomousDb=Server=sqlserver;Database=Hartonomous;User=sa;Password=YourStrong@Password
      - ConnectionStrings__Neo4j=bolt://neo4j:7687
    depends_on:
      - sqlserver
      - neo4j

  worker-embedding:
    build: ./src/Hartonomous.Workers.EmbeddingGenerator
    environment:
      - ConnectionStrings__HartonomousDb=Server=sqlserver;Database=Hartonomous;User=sa;Password=YourStrong@Password
    depends_on:
      - sqlserver

volumes:
  sqldata:
  neo4jdata:
```

## Part 3: Automated Deployment Pipeline

### GitHub Actions CI/CD

```yaml
# .github/workflows/deploy-production.yml
name: Deploy to Production

on:
  push:
    branches: [main]
    tags: ['v*']

jobs:
  build-and-test:
    runs-on: windows-latest
    steps:
      - uses: actions/checkout@v3

      - name: Setup .NET
        uses: actions/setup-dotnet@v3
        with:
          dotnet-version: '8.0.x'

      - name: Build Solution
        run: dotnet build Hartonomous.sln -c Release

      - name: Validate CLR
        run: ./scripts/validate-clr-build.ps1

      - name: Run Tests
        run: dotnet test --no-build -c Release

      - name: Build DACPAC
        run: |
          msbuild src/Hartonomous.Database/Hartonomous.Database.sqlproj `
            /t:Build /p:Configuration=Release /v:minimal

      - name: Upload Artifacts
        uses: actions/upload-artifact@v3
        with:
          name: deployment-package
          path: |
            src/Hartonomous.Database/bin/Release/*.dacpac
            src/Hartonomous.SqlClr/bin/Release/net481/*.dll

  deploy-staging:
    needs: build-and-test
    runs-on: windows-latest
    environment: staging
    steps:
      - uses: actions/download-artifact@v3
        with:
          name: deployment-package

      - name: Deploy DACPAC to Staging
        run: |
          sqlpackage /Action:Publish `
            /SourceFile:Hartonomous.Database.dacpac `
            /TargetServerName:${{ secrets.STAGING_SQL_SERVER }} `
            /TargetDatabaseName:Hartonomous `
            /TargetUser:${{ secrets.SQL_USER }} `
            /TargetPassword:${{ secrets.SQL_PASSWORD }}

      - name: Run Smoke Tests
        run: |
          sqlcmd -S ${{ secrets.STAGING_SQL_SERVER }} `
            -U ${{ secrets.SQL_USER }} -P ${{ secrets.SQL_PASSWORD }} `
            -d Hartonomous -i tests/smoke-tests.sql

  deploy-production:
    needs: deploy-staging
    runs-on: windows-latest
    environment: production
    if: startsWith(github.ref, 'refs/tags/v')
    steps:
      - uses: actions/download-artifact@v3
        with:
          name: deployment-package

      - name: Deploy DACPAC to Production
        run: |
          sqlpackage /Action:Publish `
            /SourceFile:Hartonomous.Database.dacpac `
            /TargetServerName:${{ secrets.PROD_SQL_SERVER }} `
            /TargetDatabaseName:Hartonomous `
            /TargetUser:${{ secrets.SQL_USER }} `
            /TargetPassword:${{ secrets.SQL_PASSWORD }} `
            /p:BlockOnPossibleDataLoss=True
```

## Part 4: Monitoring and Observability

### Key Metrics to Track

#### 1. Query Performance Metrics

```sql
-- Create monitoring view
CREATE OR ALTER VIEW dbo.vw_QueryPerformanceMetrics
AS
SELECT
    CAST(qsp.query_plan AS XML).value('(//@StatementText)[1]', 'nvarchar(max)') AS QueryText,
    rs.avg_duration / 1000.0 AS avg_duration_ms,
    rs.avg_logical_io_reads,
    rs.avg_cpu_time / 1000.0 AS avg_cpu_ms,
    rs.count_executions,
    rs.last_execution_time
FROM sys.query_store_query q
INNER JOIN sys.query_store_plan qsp ON q.query_id = qsp.query_id
INNER JOIN sys.query_store_runtime_stats rs ON qsp.plan_id = rs.plan_id
WHERE rs.last_execution_time >= DATEADD(HOUR, -24, GETUTCDATE())
    AND CAST(qsp.query_plan AS XML).value('(//@StatementText)[1]', 'nvarchar(max)') LIKE '%AtomEmbeddings%'
ORDER BY rs.avg_duration DESC;
```

**Alert Thresholds**:
- avg_duration_ms > 100ms → Warning
- avg_duration_ms > 500ms → Critical

#### 2. OODA Loop Health

```sql
-- Monitor OODA loop execution
SELECT
    Phase,
    COUNT(*) AS executions_last_hour,
    AVG(DurationMs) AS avg_duration_ms,
    SUM(CASE WHEN Success = 0 THEN 1 ELSE 0 END) AS failures
FROM dbo.OODALoopMetrics
WHERE ExecutedAt >= DATEADD(HOUR, -1, GETUTCDATE())
GROUP BY Phase;
```

**Alert Thresholds**:
- failures > 0 → Investigate immediately
- executions_last_hour = 0 → OODA loop stalled

#### 3. Spatial Index Usage

```sql
-- Verify spatial indexes are being used
SELECT
    OBJECT_NAME(i.object_id) AS TableName,
    i.name AS IndexName,
    ius.user_seeks,
    ius.user_scans,
    ius.user_lookups,
    ius.last_user_seek
FROM sys.indexes i
INNER JOIN sys.dm_db_index_usage_stats ius
    ON i.object_id = ius.object_id AND i.index_id = ius.index_id
WHERE i.type_desc = 'SPATIAL'
    AND OBJECT_NAME(i.object_id) = 'AtomEmbeddings';
```

**Alert Thresholds**:
- user_seeks = 0 AND last_user_seek IS NULL → Index not being used

#### 4. Service Broker Queue Depth

```sql
-- Monitor queue backlogs
SELECT
    q.name AS QueueName,
    COUNT(*) AS messages_waiting
FROM sys.transmission_queue tq
INNER JOIN sys.service_queues q ON tq.to_service_name = q.name
GROUP BY q.name;
```

**Alert Thresholds**:
- messages_waiting > 1000 → Backlog building

### Application Insights Integration

```csharp
// Program.cs
builder.Services.AddApplicationInsightsTelemetry();

// Custom telemetry for OODA loop
public class OODALoopTelemetry
{
    private readonly TelemetryClient _telemetry;

    public void TrackOODAPhaseCompleted(string phase, TimeSpan duration, bool success)
    {
        _telemetry.TrackEvent("OODAPhaseCompleted", new Dictionary<string, string>
        {
            { "Phase", phase },
            { "Success", success.ToString() }
        }, new Dictionary<string, double>
        {
            { "DurationMs", duration.TotalMilliseconds }
        });
    }
}
```

### Grafana Dashboard Configuration

```json
{
  "dashboard": {
    "title": "Hartonomous - System Health",
    "panels": [
      {
        "title": "Inference Latency (p50, p95, p99)",
        "targets": [{
          "query": "SELECT PERCENTILE_CONT(0.5) WITHIN GROUP (ORDER BY TotalDurationMs) FROM InferenceRequests WHERE RequestTimestamp >= DATEADD(MINUTE, -5, GETUTCDATE())"
        }]
      },
      {
        "title": "OODA Loop Cycle Time",
        "targets": [{
          "query": "SELECT Phase, AVG(DurationMs) FROM OODALoopMetrics GROUP BY Phase"
        }]
      },
      {
        "title": "Spatial Index Hit Rate",
        "targets": [{
          "query": "SELECT user_seeks * 100.0 / (user_seeks + user_scans) FROM sys.dm_db_index_usage_stats WHERE object_name = 'AtomEmbeddings'"
        }]
      }
    ]
  }
}
```

## Part 5: Backup and Disaster Recovery

### Backup Strategy

```sql
-- Full backup (nightly)
BACKUP DATABASE Hartonomous
TO DISK = 'D:\Backups\Hartonomous_Full_$(Date).bak'
WITH COMPRESSION, INIT;

-- Differential backup (every 6 hours)
BACKUP DATABASE Hartonomous
TO DISK = 'D:\Backups\Hartonomous_Diff_$(DateTime).bak'
WITH DIFFERENTIAL, COMPRESSION, INIT;

-- Transaction log backup (every 15 minutes)
BACKUP LOG Hartonomous
TO DISK = 'D:\Backups\Hartonomous_Log_$(DateTime).trn'
WITH COMPRESSION, INIT;
```

### Neo4j Backup

```bash
# Using neo4j-admin
neo4j-admin database backup --database=neo4j --to-path=/backups/$(date +%Y%m%d)
```

### Recovery Procedures

**Scenario 1: SQL Server Failure**
```sql
-- Restore latest full + differential + logs
RESTORE DATABASE Hartonomous
FROM DISK = 'D:\Backups\Hartonomous_Full_20250115.bak'
WITH NORECOVERY;

RESTORE DATABASE Hartonomous
FROM DISK = 'D:\Backups\Hartonomous_Diff_20250115_1800.bak'
WITH NORECOVERY;

RESTORE LOG Hartonomous
FROM DISK = 'D:\Backups\Hartonomous_Log_20250115_2345.trn'
WITH RECOVERY;

-- Verify OODA loop
EXEC sp_Analyze @TenantId = 0;
```

**Scenario 2: Neo4j Failure**
```bash
# Restore backup
neo4j-admin database restore --from-path=/backups/20250115 --database=neo4j

# Trigger full resync from SQL Server
EXEC dbo.sp_ResyncAllProvenanceToNeo4j;
```

## Part 6: Scaling Strategies

### Horizontal Scaling (Read Replicas)

```sql
-- Configure Always On Availability Group
CREATE AVAILABILITY GROUP HartonomousAG
WITH (
    AUTOMATED_BACKUP_PREFERENCE = SECONDARY,
    DB_FAILOVER = ON
)
FOR DATABASE Hartonomous
REPLICA ON
    'SQL-PRIMARY' WITH (ENDPOINT_URL = 'TCP://sql-primary:5022', AVAILABILITY_MODE = SYNCHRONOUS_COMMIT),
    'SQL-SECONDARY1' WITH (ENDPOINT_URL = 'TCP://sql-secondary1:5022', AVAILABILITY_MODE = ASYNCHRONOUS_COMMIT, SECONDARY_ROLE(ALLOW_CONNECTIONS = READ_ONLY)),
    'SQL-SECONDARY2' WITH (ENDPOINT_URL = 'TCP://sql-secondary2:5022', AVAILABILITY_MODE = ASYNCHRONOUS_COMMIT, SECONDARY_ROLE(ALLOW_CONNECTIONS = READ_ONLY));
```

**Query Routing**:
- **Writes** (inference, ingestion) → Primary
- **Reads** (search, analytics) → Secondaries

### Vertical Scaling (More Resources)

**SQL Server Memory Optimization**:
```sql
-- Increase max memory as you add RAM
EXEC sp_configure 'max server memory (MB)', 327680;  -- 320GB
RECONFIGURE;

-- Enable In-Memory OLTP for hot tables
ALTER TABLE dbo.InferenceCache ADD MEMORY_OPTIMIZED = ON;
```

### Partitioning Strategy

```sql
-- Partition AtomEmbeddings by creation date
CREATE PARTITION FUNCTION pf_AtomsByMonth (DATETIME2)
AS RANGE RIGHT FOR VALUES (
    '2025-01-01', '2025-02-01', '2025-03-01'
    -- ... monthly boundaries
);

CREATE PARTITION SCHEME ps_AtomsByMonth
AS PARTITION pf_AtomsByMonth ALL TO ([PRIMARY]);

-- Rebuild table on partition scheme
CREATE TABLE dbo.AtomEmbeddings_New (
    AtomEmbeddingId BIGINT IDENTITY PRIMARY KEY,
    -- ... columns ...
    CreatedAt DATETIME2 NOT NULL,
    INDEX IX_SpatialGeometry SPATIAL (SpatialGeometry)
) ON ps_AtomsByMonth(CreatedAt);
```

**Benefits**:
- Archive old partitions to cheaper storage
- Fast partition switching for data lifecycle
- Parallel queries across partitions

## Part 7: Security Hardening

### SQL Server Security

```sql
-- Create application user (not sa)
CREATE LOGIN HartonomousApp WITH PASSWORD = 'StrongPassword123!';
CREATE USER HartonomousApp FOR LOGIN HartonomousApp;

-- Grant minimal permissions
GRANT EXECUTE ON SCHEMA::dbo TO HartonomousApp;
GRANT SELECT, INSERT, UPDATE ON dbo.Atoms TO HartonomousApp;
GRANT SELECT, INSERT, UPDATE ON dbo.AtomEmbeddings TO HartonomousApp;
DENY DELETE ON dbo.Atoms TO HartonomousApp;  -- Prevent accidental data loss

-- Encrypt connections
ALTER DATABASE Hartonomous SET ENCRYPTION ON;
```

### API Authentication

```csharp
// Program.cs
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options => {
        options.Authority = "https://auth.yourdomain.com";
        options.Audience = "hartonomous-api";
    });

// Require authentication on all endpoints
app.MapControllers().RequireAuthorization();
```

### Network Isolation

```
┌─────────────────────┐
│   Public Internet   │
└──────────┬──────────┘
           │
    ┌──────▼──────┐
    │   Firewall  │
    │  (Port 443) │
    └──────┬──────┘
           │
    ┌──────▼──────────┐
    │  DMZ: API Tier  │  ← Public-facing
    └──────┬──────────┘
           │
    ┌──────▼──────────┐
    │ Internal VLAN   │  ← SQL, Neo4j, Workers (private)
    └─────────────────┘
```

## Operational Runbook

### Daily Tasks
- [ ] Check OODA loop metrics (should run every 15-60 min)
- [ ] Review query performance dashboard
- [ ] Verify backup completion

### Weekly Tasks
- [ ] Run performance benchmarks
- [ ] Review spatial index usage stats
- [ ] Check for CLR assembly updates needed
- [ ] Analyze storage growth trends

### Monthly Tasks
- [ ] Test disaster recovery procedure
- [ ] Review and optimize slow queries
- [ ] Archive old partitions
- [ ] Update dependencies (if compatible with CLR)

This provides complete production-ready operations for Hartonomous.
