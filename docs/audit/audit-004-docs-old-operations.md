# Archive Catalog Audit - Segment 004
**docs_old Getting Started & Operations**

**Audit Date**: 2025-11-19  
**Files Reviewed**: 6  
**Segment Range**: Lines 1-500

---

## File: `.archive\docs_old\getting-started\installation.md`

**Type**: Installation Guide  
**Status**: Complete Step-by-Step Guide  
**File Size**: Very Large (~530 lines)

### Purpose
Comprehensive installation guide for setting up Hartonomous platform on Windows with SQL Server 2022+, Neo4j 5.x, and .NET 8.0.

### Coverage

**Prerequisites Documented**:
- SQL Server 2022+ (native VECTOR type required)
- .NET 8.0 SDK
- Neo4j 5.13+ (Community or Enterprise)
- PowerShell 7+
- Git

**10-Step Installation Process**:

1. **Clone Repository** - Git clone from GitHub
2. **Configure SQL Server** - Enable CLR, Service Broker, TRUSTWORTHY
3. **Deploy Database Schema** - DACPAC deployment or migration scripts
4. **Deploy CLR Assembly** - Build and register CLR functions (49 total)
5. **Configure Neo4j** - Create database, indexes, constraints
6. **Deploy Worker Services** - 4 workers (Ingestion, Neo4jSync, EmbeddingGenerator, SpatialProjector)
7. **Configure OODA Loop** - SQL Agent job every 15 minutes
8. **Create Initial Tenant** - Multi-tenant setup
9. **Verify Installation** - Health checks via preflight script
10. **Ingest Sample Data** - Optional Llama-2-7B model ingestion

### Technical Details

**CLR Configuration Critical Section**:
```sql
EXEC sp_configure 'clr enabled', 1;
RECONFIGURE;
ALTER DATABASE Hartonomous SET TRUSTWORTHY ON;  -- Development only!
```

**Service Broker Setup**:
```sql
ALTER DATABASE Hartonomous SET ENABLE_BROKER WITH ROLLBACK IMMEDIATE;
```

**Neo4j Schema**:
- 6 node types (Atom, Source, IngestionJob, User, Pipeline, Inference)
- 11 indexes and constraints for performance

**Worker Services Installation**:
- Development: Run in separate terminals
- Production: Install as Windows Services via New-Service

**OODA Loop Automation**:
- SQL Agent job scheduled every 15 minutes
- Triggers Service Broker message to AnalyzeService

### Critical Warnings

⚠️ **TRUSTWORTHY ON**: Development only, security risk in production  
⚠️ **CLR Dependency Issue**: References System.Collections.Immutable.dll (see clr-deployment.md)  
⚠️ **Recovery Model**: Must set to FULL for transaction log backups

### Verification Steps

**Preflight Check Script**:
```powershell
.\preflight-check.ps1 -SqlServer "localhost" -Database "Hartonomous"
```

Expected checks:
- SQL Server connection ✓
- Database exists ✓
- CLR enabled ✓
- Service Broker enabled ✓
- 49 CLR functions deployed ✓
- Core tables exist ✓
- OODA loop configured ✓
- Neo4j connection ✓
- Worker services running (4/4) ✓

### Quality Assessment
- ✅ Exceptionally comprehensive (530 lines)
- ✅ Step-by-step with code examples (SQL, PowerShell, C#, Cypher)
- ✅ Troubleshooting section for common issues
- ✅ Automated deployment scripts included
- ✅ Production deployment checklist
- ✅ Clear security warnings
- ⚠️ References CRITICAL CLR dependency issue (documented in separate guide)
- ⚠️ Assumes Windows environment (Linux/Mac not covered)

### Relationships
- **Depends On**: clr-deployment.md (CRITICAL dependency issue resolution)
- **References**: quickstart.md (next steps)
- **Enables**: All other operational guides (monitoring, performance, backup)
- **Prerequisites**: SQL Server 2022+, .NET 8.0, Neo4j 5.x, PowerShell 7+

### Recommendation
**ACTION: Promote to Core Documentation**  
- This is **ESSENTIAL** for new deployments
- Promote to `docs/getting-started/01-installation.md`
- Update CLR dependency issue status if resolved
- Add Linux/Docker deployment section if needed
- Verify all scripts and commands are current
- Excellent quality and completeness

---

## File: `.archive\docs_old\getting-started\quickstart.md`

**Type**: Quick Tutorial  
**Status**: Complete 10-Minute Guide  
**File Size**: Large (~480 lines)

### Purpose
Hands-on tutorial to get started with Hartonomous in 10 minutes after installation. Covers tenant creation, model ingestion, and first semantic queries.

### Structure

**5 Core Steps** (10 minutes total):

1. **Verify System Health** (1 minute) - Check CLR functions, Service Broker, OODA loop
2. **Create Tenant** (1 minute) - Multi-tenant setup with quota
3. **Ingest Small Model** (3-5 minutes) - TinyLlama-1.1B via GGUF format
4. **Run First Semantic Query** (2 minutes) - 3 query examples
5. **Test OODA Loop** (2 minutes) - Trigger manual cycle, view logs

**Optional Advanced Features**:
- A* pathfinding between concepts
- DBSCAN clustering
- Temporal queries (Laplace's Demon)
- Performance benchmarking

### Query Examples

**Query 1: Find Similar Weights**
```sql
-- O(log N) spatial pre-filter + O(K) CLR refinement
SELECT TOP 20 ta.TensorAtomId, dbo.clr_CosineSimilarity(...) AS Score
FROM dbo.TensorAtoms ta
WHERE ta.SpatialKey.STIntersects(@QuerySpatialKey.STBuffer(5.0)) = 1
ORDER BY Score DESC;
```
**Expected**: 15-30ms for 500K atoms

**Query 2: Cross-Model Weight Comparison**
```sql
-- Find models with similar attention mechanisms
SELECT m1.ModelName, m2.ModelName, COUNT(*) AS SharedSimilarWeights
FROM dbo.TensorAtoms ta1
INNER JOIN dbo.TensorAtoms ta2 ON ta1.SpatialKey.STDistance(ta2.SpatialKey) < 1.0
WHERE ta1.TensorName LIKE '%attention%' AND ta2.TensorName LIKE '%attention%'
GROUP BY m1.ModelName, m2.ModelName;
```

**Query 3: Provenance Tracking** (Neo4j)
```cypher
MATCH (m:Model {name: 'TinyLlama-1.1B-Chat'})-[:INGESTED_FROM]->(s:Source)
RETURN m.name AS Model, s.identifier AS SourceFile, s.ingestedAt AS IngestedAt;
```

### Performance Expectations

**Ingestion Duration** (TinyLlama-1.1B):
- Parsing: 30-60 seconds
- Atomization: 1-2 minutes
- Spatialization: 1-2 minutes
- **Total**: 3-5 minutes
- **Atoms Created**: ~500,000

**Query Performance**:
- Top-K search: 18ms avg, 35ms p99
- A* pathfinding (42 steps): 127ms
- DBSCAN (100M atoms): 8.3 seconds

**OODA Loop Cycle**:
- sp_Analyze: 2-8 seconds
- sp_Hypothesize: 1-3 seconds
- sp_Act: 5-300 seconds
- sp_Learn: 1-2 seconds

### Benchmarking Section

**O(log N) + O(K) vs. O(N) Comparison**:
```sql
-- Brute-force O(N): ~5 seconds for 500K atoms
-- Optimized O(log N) + O(K): 15-30ms
-- Speedup: 166× at 500K scale
-- Speedup at 3.5B scale: 3,600,000× (as proven in architecture)
```

### Quality Assessment
- ✅ Excellent hands-on tutorial
- ✅ Realistic time estimates (10 minutes)
- ✅ Progressive complexity (basic → advanced)
- ✅ Performance expectations documented
- ✅ Clear code examples (SQL, PowerShell, Cypher)
- ✅ Troubleshooting section included
- ✅ Next steps provided
- ⚠️ Assumes installation.md completed successfully

### Relationships
- **Requires**: installation.md (all prerequisites)
- **Demonstrates**: Semantic-first architecture O(log N) + O(K)
- **References**: Architecture docs (semantic-first, ooda-loop, model-atomization)
- **Entry Point**: For hands-on learning after installation

### Recommendation
**ACTION: Promote to Core Documentation**  
- This is **ESSENTIAL** for onboarding
- Promote to `docs/getting-started/02-quickstart.md`
- Excellent quality and practical focus
- Update performance metrics if benchmarks change
- Consider adding video walkthrough link

---

## File: `.archive\docs_old\operations\clr-deployment.md`

**Type**: Operations Guide - CLR Assembly Deployment  
**Status**: Complete with CRITICAL ISSUE Documentation  
**File Size**: Very Large (~630 lines)

### Purpose
Comprehensive guide for deploying SQL Server CLR assemblies with **CRITICAL documentation of System.Collections.Immutable.dll incompatibility issue**.

### CRITICAL Known Issue (TOP PRIORITY)

**Problem**: CLR assemblies reference `System.Collections.Immutable.dll` and `System.Reflection.Metadata.dll` (.NET Standard 2.0 libraries NOT supported by SQL Server CLR host)

**Impact**:
- CREATE ASSEMBLY FAILS in clean environments
- Blocks production deployment
- No reliable workaround currently

**Root Cause**: SQL Server CLR host only supports limited .NET Framework libraries, not .NET Standard 2.0

**Resolution Strategies**:

1. **Option 1: Refactor Code** (RECOMMENDED)
   - Remove System.Collections.Immutable dependencies
   - Replace ImmutableArray with T[] or List<T>
   - Effort: 3-5 days for 49 CLR functions

2. **Option 2: Out-of-Process Worker Service**
   - Move affected functions to gRPC/HTTP service
   - Keep simple functions (distance metrics) in-database
   - Effort: 1-2 weeks

3. **Option 3: Hybrid Approach**
   - Refactor hot-path functions (distance, embeddings)
   - Move cold-path to worker service
   - Effort: ~1 week

**Current Workaround**: Manual CREATE ASSEMBLY (development only, NOT production-ready)

### Permission Sets

**Three Security Levels**:

1. **SAFE** (Recommended for most functions)
   - Computation, memory access, internal data
   - No network, file system, registry
   - Examples: clr_CosineSimilarity, clr_HilbertIndex, clr_SvdCompress

2. **EXTERNAL_ACCESS** (Elevated)
   - SAFE + network, file system, environment
   - Requires TRUSTWORTHY ON or assembly signing
   - Examples: clr_CallGpuWorker (gRPC), clr_LoadModelFromFile

3. **UNSAFE** (Extremely High Risk)
   - All permissions including P/Invoke, unmanaged memory
   - **DO NOT USE** unless absolutely necessary
   - Requires rigorous code review

### Deployment Process

**Automated Script** (`deploy-clr-assemblies.ps1`):
1. Read assembly binary
2. Convert to hex string
3. Generate CREATE ASSEMBLY statement
4. Execute via Invoke-Sqlcmd
5. Create CLR functions (49 total)

**Manual Steps**:
1. Drop existing functions/assemblies
2. CREATE ASSEMBLY from file via OPENROWSET
3. CREATE FUNCTION AS EXTERNAL NAME for each CLR function

### Assembly Signing (Production)

**Why Sign**: SQL Server `clr strict security` requires signing for EXTERNAL_ACCESS/UNSAFE

**Process**:
1. Generate strong name key (sn -k SqlClrKey.snk)
2. Extract public key
3. Update project file with AssemblyOriginatorKeyFile
4. Rebuild assembly
5. Create asymmetric key in SQL Server master database
6. Create login from key
7. Grant EXTERNAL ACCESS ASSEMBLY permission

### Troubleshooting

**Common Errors**:

1. **"Assembly references assembly 'netstandard'"**
   - **Cause**: CRITICAL dependency issue
   - **Solution**: See resolution strategies above

2. **"Not authorized for PERMISSION_SET = EXTERNAL_ACCESS"**
   - **Cause**: clr strict security enabled, assembly not signed
   - **Solution**: Sign assembly or disable strict security (dev only)

3. **"Assembly 'HartonomousClr' was not found"**
   - **Cause**: CREATE ASSEMBLY failed
   - **Solution**: Check for dependency issues

4. **"Could not load type 'Hartonomous.Clr.DistanceMetrics.CosineSimilarity'"**
   - **Cause**: Namespace mismatch between T-SQL and C#
   - **Solution**: Verify EXTERNAL NAME matches class structure

5. **"Execution of user code in .NET Framework is disabled"**
   - **Cause**: CLR integration not enabled
   - **Solution**: EXEC sp_configure 'clr enabled', 1; RECONFIGURE;

### Performance Optimization

**Diagnostics**:
```sql
-- Find slow CLR function calls via Query Store
SELECT q.query_id, qt.query_sql_text, rs.avg_duration / 1000.0 AS avg_duration_ms
FROM sys.query_store_query q
INNER JOIN sys.query_store_query_text qt ON q.query_text_id = qt.query_text_id
WHERE qt.query_sql_text LIKE '%clr_%'
ORDER BY rs.avg_duration DESC;
```

**Optimizations**:
1. Reduce serialization: Pass VARBINARY(MAX) instead of individual parameters
2. Batch operations: Process multiple vectors per CLR call
3. SIMD vectorization: Use System.Numerics.Vectors
4. Keep O(K) small: K < 100 recommended

### Verification

**Test CLR Functions**:
```sql
-- Test cosine similarity
DECLARE @vec1 VARBINARY(MAX) = 0x3F8000003F0000003E800000;  -- [1.0, 0.5, 0.25]
DECLARE @vec2 VARBINARY(MAX) = 0x3F8000003F0000003E800000;
SELECT dbo.clr_CosineSimilarity(@vec1, @vec2) AS similarity;
-- Expected: 1.0 (identical vectors)

-- List all CLR functions
SELECT o.name AS FunctionName, a.name AS AssemblyName
FROM sys.objects o
INNER JOIN sys.assembly_modules am ON o.object_id = am.object_id
INNER JOIN sys.assemblies a ON am.assembly_id = a.assembly_id
WHERE o.type = 'FS';  -- CLR scalar function
```

### Production Deployment Checklist

- [ ] **Resolve CRITICAL dependency issue** (System.Collections.Immutable)
- [ ] Sign assembly with strong name key
- [ ] Create certificate in master database
- [ ] Enable clr strict security
- [ ] Set TRUSTWORTHY OFF (use certificate-based permissions)
- [ ] Test all 49 CLR functions
- [ ] Run performance benchmarks (O(K) < 20ms for K=50)
- [ ] Configure monitoring (Query Store, Extended Events)
- [ ] Document rollback procedure
- [ ] Review EXTERNAL_ACCESS functions for security risks
- [ ] Ensure no UNSAFE functions (or rigorous review)

### Quality Assessment
- ✅ Comprehensive CLR deployment guide
- ✅ **CRITICAL ISSUE** prominently documented at top
- ✅ Multiple resolution strategies provided
- ✅ Security best practices (permission sets, signing)
- ✅ Troubleshooting section with solutions
- ✅ Performance optimization guidance
- ✅ Production deployment checklist
- ✅ Clear code examples (PowerShell, SQL, C#)
- ⚠️ **BLOCKS PRODUCTION** until dependency issue resolved

### Relationships
- **Critical For**: installation.md (Step 4: Deploy CLR Assembly)
- **Depends On**: .NET 8.0 SDK, SQL Server 2022+
- **Blocks**: Production deployment until resolved
- **Priority**: TOP-PRIORITY TECHNICAL DEBT
- **Tracking**: GitHub Issue #47 referenced

### Recommendation
**ACTION: Promote to Core Documentation + UPDATE STATUS**  
- This is **CRITICAL OPERATIONS** documentation
- Promote to `docs/operations/01-clr-deployment.md`
- Update with resolution status (if issue resolved since Nov 18)
- Add decision record for chosen resolution strategy
- Essential reading before deploying CLR assemblies
- Well-written with clear warnings

---

## File: `.archive\docs_old\operations\backup-recovery.md`

**Type**: Operations Guide - Backup & Disaster Recovery  
**Status**: Production Ready  
**File Size**: Large (~400 lines estimated from structure)

### Purpose
Comprehensive backup and disaster recovery strategy for SQL Server, Neo4j, and CLR components with automated scripts and retention policies.

### Backup Architecture

**Three-Tier Strategy**:

1. **SQL Server Backups**
   - Full: Weekly (Sunday 1 AM) → 4 weeks retention
   - Differential: Daily (2 AM) → 7 days retention
   - Transaction Log: Hourly → 7 days retention

2. **Neo4j Backups**
   - Snapshot: Daily (3 AM) → 30 days retention
   - Online backup supported (no downtime)

3. **CLR Assembly Backups**
   - On-change versioning → Indefinite retention
   - Includes DLL files + deployment scripts

**Storage**: Azure Blob Storage (GRS - geo-redundant)

### SQL Server Backup Details

**Full Backup Script**:
```sql
BACKUP DATABASE Hartonomous
TO URL = 'https://hartstorage.blob.core.windows.net/backups/Hartonomous_FULL_20251119.bak'
WITH CREDENTIAL = 'AzureStorageCredential', COMPRESSION, CHECKSUM;
```

**Automated via SQL Agent Job** - Weekly schedule created

**Differential Backup** - Daily at 2 AM

**Transaction Log Backup** - Hourly for point-in-time recovery

**Recovery Model**: FULL (required for transaction log backups)

### Neo4j Backup Details

**Enterprise Snapshot**:
```bash
sudo neo4j-admin backup \
    --backup-dir=/backups/neo4j \
    --name=hartonomous-$(date +%Y%m%d) \
    --database=neo4j \
    --verbose
```

**Automated via cron** - Daily at 3 AM

**Upload to Azure**:
```bash
azcopy copy "/backups/neo4j/hartonomous-$(date +%Y%m%d)" \
    "https://hartstorage.blob.core.windows.net/neo4j-backups/" --recursive
```

**Online Backup**: Requires `dbms.backup.enabled=true` in neo4j.conf

### CLR Assembly Backup

**Backup Script** (`Backup-CLRAssemblies.ps1`):
```powershell
$BackupDir = "D:\Backups\CLR\$(Get-Date -Format 'yyyyMMdd')"
Copy-Item "D:\Assemblies\Hartonomous.Clr.dll" $BackupDir
azcopy copy $BackupDir "https://hartstorage.blob.core.windows.net/clr-backups/" --recursive
```

**CLR Schema Export**:
```sql
-- Export CLR function definitions
SELECT 'CREATE FUNCTION ' + name + ' AS EXTERNAL NAME [...]' AS Definition
FROM sys.objects WHERE type = 'FS';
```

### Restore Procedures

**Point-in-Time Restore** (to 2025-11-19 14:30):

1. Restore full backup with NORECOVERY
2. Restore differential backup with NORECOVERY
3. Restore transaction logs up to 14:30 with STOPAT
4. Final restore with RECOVERY
5. Verify data at restore point

**Disaster Recovery** (Complete Rebuild):

1. Restore SQL Server databases (full + diff + logs)
2. Restore Neo4j graph from snapshot
3. Restore CLR assemblies from Azure Blob
4. Deploy CLR functions
5. Verify system health (OODA loop, spatial queries, Neo4j sync)

**CLR Assembly Recovery**:
```sql
-- Drop existing CLR objects
DROP FUNCTION dbo.clr_CosineSimilarity;
-- (Drop all 49 functions)

-- Drop assemblies
DROP ASSEMBLY [Hartonomous.Clr];

-- Redeploy from backup
CREATE ASSEMBLY [Hartonomous.Clr]
FROM 'D:\Assemblies\Hartonomous.Clr.dll'
WITH PERMISSION_SET = UNSAFE;

-- Recreate CLR functions (run deploy-clr.sql)
```

### Retention Policy

| Backup Type | Frequency | Retention | Storage Tier |
|-------------|-----------|-----------|--------------|
| SQL Full | Weekly (Sun 1 AM) | 4 weeks | Azure Blob (Cool) |
| SQL Differential | Daily (2 AM) | 7 days | Azure Blob (Hot) |
| SQL Transaction Log | Hourly | 7 days | Azure Blob (Hot) |
| Neo4j Snapshot | Daily (3 AM) | 30 days | Azure Blob (Cool) |
| CLR Assemblies | On change | Indefinite | Azure Blob (Archive) |

**Cleanup Script**: Weekly deletion of expired backups via PowerShell + Azure CLI

### Monitoring Backup Health

**View: Backup Status**:
```sql
CREATE VIEW dbo.vw_BackupStatus AS
SELECT database_name, type, MAX(backup_finish_date) AS LastBackup,
       DATEDIFF(HOUR, MAX(backup_finish_date), SYSDATETIME()) AS HoursSinceLastBackup
FROM msdb.dbo.backupset
WHERE database_name IN ('Hartonomous', 'HartonomousArchive')
GROUP BY database_name, type;
```

**Alerts**:
- Full backup not taken in 8 days (weekly + buffer)
- Differential not taken in 26 hours (daily + buffer)
- Log backup not taken in 2 hours (hourly + buffer)

### Best Practices

1. **Test Restores**: Quarterly restore test to verify integrity
2. **Geo-Redundancy**: Azure Blob GRS for disaster recovery
3. **Encryption**: Enable TDE (Transparent Data Encryption)
4. **Monitoring**: Alert on backup failures or duration spikes
5. **Retention**: Keep 4 weeks full, 7 days logs
6. **CLR Versioning**: Tag assemblies with version numbers

### Quality Assessment
- ✅ Comprehensive backup strategy (SQL, Neo4j, CLR)
- ✅ Automated via SQL Agent and cron
- ✅ Azure Blob Storage with geo-redundancy
- ✅ Point-in-time recovery documented
- ✅ Disaster recovery procedures
- ✅ Monitoring and alerting
- ✅ Clear retention policies
- ✅ Best practices included
- ⚠️ Assumes Azure environment (no on-premises alternatives)

### Relationships
- **Depends On**: Azure Blob Storage, SQL Agent, Neo4j Enterprise
- **Complements**: monitoring.md (backup health alerts)
- **Enables**: Disaster recovery, compliance (audit retention)
- **Critical For**: Production deployments

### Recommendation
**ACTION: Promote to Core Documentation**  
- This is **ESSENTIAL** for production operations
- Promote to `docs/operations/02-backup-recovery.md`
- Add alternative storage options (on-premises, AWS S3)
- Verify retention policies align with compliance requirements
- Excellent quality and production-ready

---

## File: `.archive\docs_old\operations\monitoring.md`

**Type**: Operations Guide - System Monitoring  
**Status**: Production Ready  
**File Size**: Large (~400 lines estimated from structure)

### Purpose
Comprehensive monitoring stack for SQL Server, Neo4j, Service Broker, and CLR components with Grafana dashboards and Prometheus alerting.

### Monitoring Architecture

```
SQL Server DMVs + Neo4j Metrics + Service Broker
         ↓
  Monitoring Views (dbo.vw_*)
         ↓
  Grafana Dashboards
         ↓
  Prometheus AlertManager
```

### Key Monitoring Views

**1. OODA Loop Health**:
```sql
CREATE VIEW dbo.vw_OODALoopHealth AS
SELECT Phase, COUNT(*) AS ExecutionCount,
       AVG(DATEDIFF(MILLISECOND, StartTime, EndTime)) AS AvgDurationMs,
       SUM(CASE WHEN ErrorMessage IS NOT NULL THEN 1 ELSE 0 END) AS ErrorCount
FROM dbo.OODALogs
WHERE StartTime >= DATEADD(HOUR, -1, SYSDATETIME())
GROUP BY Phase;
```

**Alerts**:
- ErrorRate >5%: OODA loop failing
- AvgDurationMs >200: Performance degradation

**2. Spatial Query Performance**:
```sql
CREATE VIEW dbo.vw_SpatialQueryPerformance AS
SELECT QuerySnippet, COUNT(*) AS ExecutionCount,
       AVG(DurationMs) AS AvgDurationMs, AVG(LogicalReads) AS AvgLogicalReads
FROM dbo.QueryPerformanceLog
WHERE QueryText LIKE '%STIntersects%'
GROUP BY QuerySnippet;
```

**Alerts**:
- AvgDurationMs >50ms: Index rebuild needed
- AvgLogicalReads >10000: Query optimization needed

**3. Ingestion Pipeline Status**:
```sql
CREATE VIEW dbo.vw_IngestionStatus AS
SELECT TenantId, ModelId, COUNT(*) AS AtomsIngested,
       COUNT(*) * 1.0 / DATEDIFF(SECOND, MIN(IngestTimestamp), MAX(IngestTimestamp)) AS AtomsPerSecond
FROM dbo.Atoms
WHERE IngestTimestamp >= DATEADD(HOUR, -1, SYSDATETIME())
GROUP BY TenantId, ModelId;
```

**Alerts**:
- AtomsPerSecond <100: Ingestion slowdown
- DurationSeconds >3600: Model taking >1 hour

**4. Neo4j Sync Lag**:
```sql
CREATE VIEW dbo.vw_Neo4jSyncLag AS
SELECT EntityType, COUNT(*) AS PendingItems,
       DATEDIFF(SECOND, MIN(CreatedAt), SYSDATETIME()) AS LagSeconds
FROM dbo.Neo4jSyncQueue
WHERE IsSynced = 0 AND RetryCount < 5
GROUP BY EntityType;
```

**Alerts**:
- LagSeconds >60: Neo4j sync falling behind
- PendingItems >10000: Queue overwhelmed

### DMV Queries

**Spatial Index Health**:
```sql
-- Fragmentation check
SELECT OBJECT_NAME(ips.object_id) AS TableName, i.name AS IndexName,
       ips.avg_fragmentation_in_percent
FROM sys.dm_db_index_physical_stats(...) ips
INNER JOIN sys.indexes i ON ips.object_id = i.object_id
WHERE i.type_desc = 'SPATIAL' AND ips.avg_fragmentation_in_percent > 30;
```

**Slow Queries**:
```sql
-- Top 20 slowest queries
SELECT TOP 20 SUBSTRING(qt.text, ...) AS QueryText,
       qs.execution_count, qs.total_elapsed_time / qs.execution_count AS AvgElapsedMs
FROM sys.dm_exec_query_stats qs
CROSS APPLY sys.dm_exec_sql_text(qs.sql_handle) qt
WHERE qs.total_elapsed_time / qs.execution_count > 50000  -- >50ms
ORDER BY AvgElapsedMs DESC;
```

### Service Broker Monitoring

**Queue Depth**:
```sql
CREATE VIEW dbo.vw_ServiceBrokerQueueDepth AS
SELECT q.name AS QueueName, COUNT(*) AS MessageCount
FROM sys.service_queues q
LEFT JOIN sys.transmission_queue sqm ON q.object_id = sqm.to_service_id
WHERE q.name IN ('IngestionQueue', 'Neo4jSyncQueue', 'OODAQueue')
GROUP BY q.name;
```

**Alerts**: MessageCount >5000 (queue backlog)

**Service Broker Errors**:
```sql
-- Check transmission queue for failures
SELECT from_service_name, to_service_name, transmission_status
FROM sys.transmission_queue
WHERE transmission_status <> 'Success';
```

### Grafana Dashboards

**Dashboard 1: OODA Loop Health**

*Panel 1: OODA Phase Duration* (Line chart)
```sql
SELECT DATEADD(MINUTE, DATEDIFF(MINUTE, 0, StartTime) / 5 * 5, 0) AS TimeBucket,
       Phase, AVG(DATEDIFF(MILLISECOND, StartTime, EndTime)) AS AvgDurationMs
FROM dbo.OODALogs
WHERE StartTime >= DATEADD(HOUR, -24, SYSDATETIME())
GROUP BY DATEADD(...), Phase;
```

*Panel 2: OODA Error Rate* (Line chart with 5% alert threshold)

**Dashboard 2: Spatial Query Performance**

*Panel 1: Query Duration Histogram* (Bar chart)
```sql
SELECT CASE WHEN DurationMs < 10 THEN '<10ms'
            WHEN DurationMs < 50 THEN '10-50ms'
            WHEN DurationMs < 100 THEN '50-100ms'
            ELSE '>100ms' END AS Bucket, COUNT(*) AS QueryCount
FROM dbo.QueryPerformanceLog WHERE QueryText LIKE '%STIntersects%'
GROUP BY CASE...;
```

*Panel 2: Spatial Index Fragmentation* (Gauge, alert at 30%)

**Dashboard 3: Ingestion Pipeline**

*Panel 1: Ingestion Throughput* (Area chart)
*Panel 2: Service Broker Queue Depth* (Bar chart, alert at 5000)

### Prometheus AlertManager

**Alert Rules** (alertmanager.yml):

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

      - alert: SpatialQuerySlow
        expr: avg_spatial_query_duration_ms > 50
        for: 10m
        labels:
          severity: warning

      - alert: Neo4jSyncLag
        expr: neo4j_sync_lag_seconds > 60
        for: 5m
        labels:
          severity: warning

      - alert: IngestionStalled
        expr: rate(atoms_ingested[5m]) < 100
        for: 10m
        labels:
          severity: critical
```

### Metric Exporters

**SQL Server Exporter** (Custom HTTP endpoint for Prometheus):
```csharp
app.MapGet("/metrics", () =>
{
    var metrics = new StringBuilder();
    // Query dbo.vw_OODALoopHealth, export as Prometheus format
    metrics.AppendLine($"ooda_error_rate{{phase=\"{reader["Phase"]}\"}} {reader["ErrorRate"]}");
    return Results.Text(metrics.ToString(), "text/plain");
});
```

### Best Practices

1. **OODA Monitoring**: Alert if error rate >5% or Orient phase >200ms
2. **Spatial Index Maintenance**: Rebuild weekly or fragmentation >30%
3. **Neo4j Sync**: Monitor lag every 30 seconds, alert at 60s
4. **Ingestion Pipeline**: Track atoms/second, alert if <100
5. **Service Broker**: Check queue depth every minute, alert at 5000
6. **Grafana Refresh**: Every 30 seconds for real-time visibility

### Quality Assessment
- ✅ Comprehensive monitoring coverage (OODA, spatial, ingestion, Neo4j)
- ✅ SQL views for efficient metric queries
- ✅ Grafana dashboard specifications
- ✅ Prometheus alert rules with thresholds
- ✅ Custom metric exporter pattern
- ✅ Best practices documented
- ⚠️ Assumes Grafana + Prometheus stack (no alternative monitoring tools)

### Relationships
- **Complements**: performance-tuning.md (identifies bottlenecks)
- **Integrates With**: backup-recovery.md (backup health alerts)
- **Requires**: Grafana, Prometheus, SQL Server DMVs, Neo4j metrics API
- **Critical For**: Production operations, SLA compliance

### Recommendation
**ACTION: Promote to Core Documentation**  
- This is **ESSENTIAL** for production monitoring
- Promote to `docs/operations/03-monitoring.md`
- Add alternative monitoring options (Azure Monitor, Datadog)
- Verify alert thresholds align with SLAs
- Excellent quality and production-ready

---

## File: `.archive\docs_old\operations\performance-tuning.md`

**Type**: Operations Guide - Performance Optimization  
**Status**: Production Ready  
**File Size**: Very Large (~500 lines estimated from structure)

### Purpose
Comprehensive performance tuning guide covering spatial queries, OODA loop, ingestion pipeline, and CLR functions with practical optimization techniques.

### Spatial Query Optimization

**Index Configuration**:
```sql
CREATE SPATIAL INDEX IX_AtomEmbeddings_SpatialGeometry
ON dbo.AtomEmbeddings(SpatialGeometry)
WITH (
    BOUNDING_BOX = (-200, -200, -200, 200, 200, 200),
    GRIDS = (LEVEL_1 = MEDIUM, LEVEL_2 = MEDIUM, LEVEL_3 = MEDIUM, LEVEL_4 = MEDIUM),
    CELLS_PER_OBJECT = 16
);
```

**Covering Index for O(K) Refinement**:
```sql
CREATE NONCLUSTERED INDEX IX_AtomEmbeddings_Covering
ON dbo.AtomEmbeddings(AtomId)
INCLUDE (EmbeddingVector, SpatialGeometry)
WITH (FILLFACTOR = 90, ONLINE = ON);
```
**Benefit**: Avoids key lookup during cosine refinement phase

**Query Patterns**:

1. **Force Spatial Index Hint**:
```sql
SELECT TOP 10 AtomId
FROM dbo.AtomEmbeddings WITH (INDEX(IX_AtomEmbeddings_SpatialGeometry))
WHERE SpatialGeometry.STIntersects(@QueryGeometry.STBuffer(30.0)) = 1;
```

2. **Separate Pre-Filter from Refinement via CTE**:
```sql
WITH SpatialCandidates AS (
    SELECT AtomId, EmbeddingVector FROM dbo.AtomEmbeddings
    WHERE SpatialGeometry.STIntersects(@QueryGeometry.STBuffer(30.0)) = 1
)
SELECT TOP 10 AtomId, dbo.clr_CosineSimilarity(@Query, EmbeddingVector) AS Score
FROM SpatialCandidates ORDER BY Score DESC;
```

3. **Adaptive Radius** (start small, increase if insufficient results):
```sql
DECLARE @Radius FLOAT = 15.0;
WHILE @ResultCount < 10 AND @Radius < 50.0
BEGIN
    SELECT @ResultCount = COUNT(*) FROM dbo.AtomEmbeddings
    WHERE SpatialGeometry.STIntersects(@QueryGeometry.STBuffer(@Radius)) = 1;
    IF @ResultCount < 10 SET @Radius = @Radius * 1.5;
END;
```

### OODA Loop Throughput

**Async Orient Phase** (parallelize LLM calls):
```csharp
var hypothesisTasks = new List<Task<string>>();
for (int i = 0; i < 3; i++)  // Generate 3 hypotheses in parallel
    hypothesisTasks.Add(GenerateHypothesisAsync(input, sessionId));
var hypotheses = await Task.WhenAll(hypothesisTasks);
```

**Hypothesis Caching** (avoid repeated LLM calls):
```sql
CREATE TABLE dbo.HypothesisCache (
    InputHash VARBINARY(32) PRIMARY KEY,
    Hypothesis NVARCHAR(MAX),
    Frequency INT DEFAULT 1,
    LastAccessedAt DATETIME2 DEFAULT SYSDATETIME()
);

-- Check cache before Orient phase
DECLARE @InputHash VARBINARY(32) = HASHBYTES('SHA2_256', @Input);
SELECT @CachedHypothesis = Hypothesis FROM dbo.HypothesisCache
WHERE InputHash = @InputHash AND LastAccessedAt >= DATEADD(HOUR, -24, SYSDATETIME());
```
**Eviction**: Delete entries not accessed in 7 days

**Pre-Compute Decision Metrics**:
```sql
CREATE TABLE dbo.DecisionMetrics (
    SessionId UNIQUEIDENTIFIER PRIMARY KEY,
    TotalHypotheses INT, AvgHypothesisScore FLOAT, TopHypothesisId BIGINT
);
-- Update after Orient phase via MERGE statement
```

### Ingestion Pipeline Tuning

**Atomizer Parallelism**:
```csharp
var workerCount = Environment.ProcessorCount / 2;  // Use 50% of CPU cores
var tasks = new List<Task>();
for (int i = 0; i < workerCount; i++)
    tasks.Add(Task.Run(() => ProcessAtomizerQueue(tenantId, modelId)));
await Task.WhenAll(tasks);
```

**Batch Inserts via Table-Valued Parameters**:
```sql
CREATE TYPE dbo.AtomTableType AS TABLE (
    Content VARBINARY(MAX), ContentType NVARCHAR(100),
    EmbeddingVector VARBINARY(MAX), SpatialGeometry GEOMETRY
);

CREATE PROCEDURE dbo.sp_BulkInsertAtoms @Atoms dbo.AtomTableType READONLY
AS
BEGIN
    INSERT INTO dbo.Atoms (Content, ContentType, IngestTimestamp)
    SELECT Content, ContentType, SYSDATETIME() FROM @Atoms;
    -- Insert embeddings separately for failure isolation
END;
```

**Benchmark**: 1000 atoms in ~800ms (vs 8-10 seconds for 1-by-1)

**Service Broker Throughput**:

1. **Increase Activation Parallelism**:
```sql
ALTER QUEUE dbo.IngestionQueue
WITH ACTIVATION (MAX_QUEUE_READERS = 8);  -- Increased from 4
```

2. **Batch Receive Messages**:
```sql
-- Receive batch of 100 messages
RECEIVE TOP (100) conversation_handle, message_body
INTO @MessageBatch FROM dbo.IngestionQueue;
```

### CLR Function Optimization

**CosineSimilarity with SIMD** (System.Numerics.Vectors):
```csharp
public static SqlDouble clr_CosineSimilarity(SqlBytes vectorA, SqlBytes vectorB)
{
    var a = MemoryMarshal.Cast<byte, float>(vectorA.Value);
    var b = MemoryMarshal.Cast<byte, float>(vectorB.Value);
    
    float dot = 0, magA = 0, magB = 0;
    int vecSize = Vector<float>.Count;
    
    // SIMD loop (process 4-8 floats per iteration)
    for (int i = 0; i <= a.Length - vecSize; i += vecSize)
    {
        var va = new Vector<float>(a.Slice(i, vecSize));
        var vb = new Vector<float>(b.Slice(i, vecSize));
        dot += Vector.Dot(va, vb);
        magA += Vector.Dot(va, va);
        magB += Vector.Dot(vb, vb);
    }
    // Scalar remainder...
}
```
**Benchmark**: 2-3× faster than scalar loop for 1536-dim vectors

**In-Memory Cache for Hot Embeddings**:
```sql
CREATE TABLE dbo.EmbeddingCache (
    AtomId BIGINT PRIMARY KEY, EmbeddingVector VARBINARY(MAX),
    AccessCount INT, LastAccessedAt DATETIME2
) WITH (MEMORY_OPTIMIZED = ON);
```

**LandmarkProjection Caching** (precompute landmark magnitudes):
```csharp
private static float[] _landmarkX, _landmarkY, _landmarkZ;
private static float _magX, _magY, _magZ;

static LandmarkProjection()
{
    // Load landmarks from SQL Server once on assembly load
    using var conn = new SqlConnection("context connection=true");
    // Cache landmark vectors and magnitudes
}
```

### Query Store Auto-Tuning

**Enable Automatic Plan Regression Detection**:
```sql
ALTER DATABASE Hartonomous
SET QUERY_STORE = ON (OPERATION_MODE = READ_WRITE, QUERY_CAPTURE_MODE = AUTO);

ALTER DATABASE SCOPED CONFIGURATION 
SET AUTOMATIC_TUNING (FORCE_LAST_GOOD_PLAN = ON);
```

**Monitor Regressed Queries**:
```sql
SELECT query_id, query_sql_text, avg_duration / 1000 AS AvgDurationMs
FROM sys.query_store_query qsq
INNER JOIN sys.query_store_runtime_stats qsrs ON qsq.query_id = qsrs.query_id
WHERE avg_duration > 50000  -- >50ms
ORDER BY avg_duration DESC;
```

### Index Maintenance

**Weekly Rebuild Schedule** (SQL Agent Job):
```sql
-- Rebuild all spatial indexes
DECLARE index_cursor CURSOR FOR
    SELECT OBJECT_NAME(i.object_id), i.name
    FROM sys.indexes i WHERE i.type_desc = 'SPATIAL';

OPEN index_cursor;
FETCH NEXT FROM index_cursor INTO @TableName, @IndexName;
WHILE @@FETCH_STATUS = 0
BEGIN
    SET @SQL = 'ALTER INDEX ' + @IndexName + ' ON ' + @TableName + 
               ' REBUILD WITH (ONLINE = ON, MAXDOP = 4)';
    EXEC sp_executesql @SQL;
    FETCH NEXT FROM index_cursor INTO @TableName, @IndexName;
END;

EXEC sp_updatestats;  -- Update statistics
```

**Schedule**: Weekly Sunday at 2 AM

### Performance Benchmarks

**Before/After Tuning**:

| Metric | Before | After | Improvement |
|--------|--------|-------|-------------|
| Spatial queries | 50ms | 20-30ms | 40-60% faster |
| OODA loop | 250ms | 120-150ms | 40-50% faster |
| Ingestion | 50 atoms/sec | 150 atoms/sec | 3× throughput |

**Target Performance**:
- Spatial queries: <30ms average
- OODA loop: <150ms per cycle
- Ingestion: >150 atoms/second

### Best Practices

1. **Spatial Indexes**: Rebuild weekly, check fragmentation daily
2. **OODA Caching**: Cache hypotheses for 24 hours
3. **Batch Operations**: 100-1000 atoms per insert, 100 messages per Service Broker receive
4. **CLR SIMD**: Use System.Numerics.Vectors for vector math
5. **Query Store**: Enable auto-tuning for plan regression
6. **In-Memory Tables**: Use for hot data (caches, metrics)
7. **Monitor Throughput**: Alert if atoms/sec <100 or OODA >200ms

### Quality Assessment
- ✅ Comprehensive performance optimization guide
- ✅ Practical code examples (SQL, C#)
- ✅ Clear before/after benchmarks
- ✅ Best practices summarized
- ✅ Covers all major components (spatial, OODA, ingestion, CLR)
- ✅ Index maintenance automation
- ✅ Query Store integration
- ⚠️ SIMD optimization requires .NET 8+ (may conflict with CLR restrictions)

### Relationships
- **Complements**: monitoring.md (identifies bottlenecks to tune)
- **Depends On**: clr-deployment.md (CLR optimizations)
- **Improves**: semantic-first.md (O(log N) + O(K) pattern)
- **Critical For**: Production scalability (3.5B atom target)

### Recommendation
**ACTION: Promote to Core Documentation**  
- This is **ESSENTIAL** for production performance
- Promote to `docs/operations/04-performance-tuning.md`
- Verify SIMD optimizations compatible with SQL Server CLR
- Update benchmarks with current production metrics
- Excellent quality and actionable guidance

---

## Summary Statistics - Segment 004

**Files Reviewed**: 6  
**Total Lines Analyzed**: ~2,540 lines  
**Document Types**:
- Getting Started Guides: 2
- Operations Guides: 4

**Key Findings**:

1. **Getting Started Complete**: Installation + Quickstart provide full onboarding path (1010 lines combined)
2. **CRITICAL ISSUE Documented**: CLR deployment blocked by System.Collections.Immutable dependency
3. **Production-Ready Operations**: Backup, monitoring, performance guides all complete
4. **Comprehensive Coverage**: 10-step installation, 5-step quickstart, 3-tier backup strategy, 4 Grafana dashboards

**Document Quality**:
- ✅ installation.md: Exceptional step-by-step guide (530 lines)
- ✅ quickstart.md: Excellent hands-on tutorial (480 lines)
- ✅ clr-deployment.md: CRITICAL issue prominently documented (630 lines)
- ✅ backup-recovery.md: Production-ready disaster recovery (400 lines)
- ✅ monitoring.md: Comprehensive Grafana + Prometheus setup (400 lines)
- ✅ performance-tuning.md: Actionable optimization techniques (500 lines)

**Actions Required**:
- **Promote All 6 Files**: Move to `docs/getting-started/` (2 files) and `docs/operations/` (4 files)
- **Update CLR Status**: Check if System.Collections.Immutable issue resolved since Nov 18
- **Verify Scripts**: Ensure all PowerShell/SQL scripts are current and tested
- **Add Alternatives**: Consider Linux/Docker for installation, non-Azure for backup/monitoring

**Critical Dependencies**:
- installation.md → clr-deployment.md (CRITICAL dependency issue)
- quickstart.md → installation.md (requires completed setup)
- All operations guides → installation.md (assume deployed system)

---

**Next Segment**: docs_old examples + docs_old/ingestion  
**Estimated Files**: ~5 markdown files
