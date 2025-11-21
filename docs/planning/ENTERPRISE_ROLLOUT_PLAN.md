# Enterprise Implementation Roadmap: Hartonomous Cognitive Engine

**Status**: Active  
**Target**: Production Release (v1.0)  
**Scope**: Database Kernel, CLR Compute Layer, Event Bus, and API Integration  
**Last Updated**: November 20, 2025

## Executive Summary

This roadmap defines the critical path for transitioning the Hartonomous system from architectural prototype to a production-ready, self-optimizing cognitive database. The execution order ensures that the "Physics Engine" (Storage & Compute) is secured and optimized before the "Nervous System" (Event Bus) and "Cortex" (API) are fully activated.

---

## Phase 1: Infrastructure Security & Core Kernel Stabilization

**Objective**: Establish a secure, signed, and schema-stable foundation for high-performance vector operations.

### 1.1 Establish CLR Code Access Security (CAS) Infrastructure

**Requirement**: SQL Server strict security compliance for `UNSAFE`/`EXTERNAL_ACCESS` assemblies.

**Actions**:
* Execute `scripts/Initialize-CLRSigning.ps1` to generate the `.pfx` (build) and `.cer` (deployment) artifacts.
* Commit public certificates to `certificates/` directory.
* Execute `scripts/Deploy-CLRCertificate.ps1` against the target instance to create the server-level Login and Grant `UNSAFE ASSEMBLY` permissions.

**Verification**: 
```sql
-- Ensure sys.server_principals contains the designated CLR login
SELECT pr.name, pe.permission_name
FROM sys.server_principals pr
JOIN sys.server_permissions pe ON pr.principal_id = pe.grantee_principal_id
WHERE pr.name = 'HartonomousCertLogin';
```

### 1.2 Implement Hardware Intrinsics (AVX2 Optimization)

**Requirement**: Maximize vector compute throughput by leveraging CPU SIMD instructions (AVX2/AVX-512).

**Context**: Current implementation utilizes basic SIMD (`Vector<T>`). Production scale requires explicit hardware intrinsics.

**Actions**:
* Refactor `src/Hartonomous.Database/CLR/Core/VectorMath.cs`.
* Implement `DotProductAvx2`, `EuclideanDistanceAvx2`, and `CosineSimilarityAvx2` using `System.Runtime.Intrinsics.X86`.
* Implement runtime hardware detection (`Avx2.IsSupported`) with graceful fallback to standard scalar/SIMD logic.

**Verification**: 
* Validate CLR function execution without exceptions
* Benchmark throughput gains (target: 2-4x improvement for 1536-dimension vectors)

### 1.3 Schema Finalization & Migration

**Requirement**: Lock down core storage primitives to prevent breaking changes during ingestion.

**Actions**:
* Finalize the `Atom` and `AtomRelation` schema definitions.
    * Verify `Atom` table includes nullable `SpatialKey` (GEOMETRY) for centroid storage.
    * Verify `AtomRelation` includes `HilbertValue` (BIGINT) for locality-preserving column indexing.
* Execute schema migration script `src/Hartonomous.Database/Scripts/Post-Deployment/Migration_AtomRelations_EnterpriseUpgrade.sql`.

**Verification**:
```sql
-- Verify schema updates
SELECT COLUMN_NAME, DATA_TYPE, IS_NULLABLE
FROM INFORMATION_SCHEMA.COLUMNS
WHERE TABLE_NAME = 'Atom' AND COLUMN_NAME = 'SpatialKey';

SELECT COLUMN_NAME, DATA_TYPE, IS_NULLABLE
FROM INFORMATION_SCHEMA.COLUMNS
WHERE TABLE_NAME = 'AtomRelation' AND COLUMN_NAME = 'HilbertValue';
```

---

## Phase 2: Asynchronous Event Bus (Service Broker)

**Objective**: Decouple data ingestion from downstream processing (Graph Sync, Analytics) using reliable, asynchronous messaging.

### 2.1 Configure Event Propagation Triggers

**Requirement**: Database operations must emit events to the Service Broker without blocking the write transaction.

**Actions**:
* Update Ingestion Stored Procedures (e.g., `dbo.sp_IngestAtoms`).
* Inject `EXEC dbo.sp_EnqueueNeo4jSync` logic post-insertion.
* Ensure payload serialization matches the expected XML schema defined in the Service Broker Contracts.

**Verification**:
```sql
-- Verify messages are enqueued after ingestion
SELECT COUNT(*) AS PendingMessages
FROM dbo.Neo4jSyncQueue WITH (NOLOCK);
```

### 2.2 Standardize Message Contracts

**Requirement**: Strictly typed message schemas between SQL Service Broker (XML) and .NET Workers (Classes).

**Actions**:
* Audit `src/Hartonomous.Database/Procedures/dbo.sp_EnqueueNeo4jSync.sql` against `Hartonomous.Workers.Neo4jSync/Neo4jSyncWorker.cs`.
* Enforce case-sensitivity matching for XML elements (e.g., `<EntityId>` vs property `EntityId`).
* Validate deserialization logic in the worker consumer loop.

**Verification**:
* Review unit tests for message serialization/deserialization
* Execute end-to-end integration test with sample payloads

### 2.3 Deploy & Orchestrate Worker Services

**Requirement**: Resilient background processing of ingestion queues.

**Actions**:
* Compile and deploy `Hartonomous.Workers.Neo4jSync` and `Hartonomous.Workers.CesConsumer`.
* Update `appsettings.json` connection strings to point to the target SQL instance.
* Configure as system services (SystemD/Windows Service) with auto-restart policies.

**Verification**:
```powershell
# Windows Service status
Get-Service -Name "Hartonomous.Workers.Neo4jSync"

# Linux SystemD status
systemctl status hartonomous-neo4j-sync
```

---

## Phase 3: API Integration & Service Layer

**Objective**: Expose the database kernel via secure, standards-based REST endpoints.

### 3.1 Implement Ingestion Service Logic

**Requirement**: Robust handling of multi-modal input streams (File, URL, Stream).

**Actions**:
* Complete implementation of `IngestionService.cs`.
* Implement `IngestUrlAsync` using `HttpClient` for retrieval and existing `IngestFileAsync` logic for atomization.
* Ensure Tenant Isolation is enforced via `TenantId` propagation from the API Controller to the `Atom` entity.

**Verification**:
```bash
# Test file ingestion
curl -X POST https://localhost:5001/api/ingestion/file \
  -H "Content-Type: multipart/form-data" \
  -F "file=@test.txt" \
  -F "tenantId=0"

# Test URL ingestion
curl -X POST https://localhost:5001/api/ingestion/url \
  -H "Content-Type: application/json" \
  -d '{"url": "https://example.com/document.pdf", "tenantId": 0}'
```

### 3.2 Implement Master/Detail Graph Navigation

**Requirement**: High-performance retrieval of complex atom hierarchies (Parent/Child/Lineage).

**Actions**:
* Implement `GetAtomDetail` logic in `AtomQueryService`.
* Utilize efficient SQL JOINs to fetch `Atom` metadata, `AtomRelation` children, and `AtomRelation` parents in a single round-trip query.
* Avoid "N+1" query patterns.
* Map results to the standardized `AtomDetailResponse` DTO.

**Verification**:
```sql
-- Test query performance (should execute in <100ms for 1000-node graphs)
SET STATISTICS TIME ON;
EXEC dbo.sp_GetAtomDetail @AtomId = 1, @TenantId = 0;
SET STATISTICS TIME OFF;
```

---

## Phase 4: Autonomous Operations (OODA Loop)

**Objective**: Enable self-optimization and background maintenance cycles.

### 4.1 Provision Autonomous Agents

**Requirement**: Scheduled execution of the OODA (Observe, Orient, Decide, Act) cycle.

**Actions**:
* Configure SQL Server Agent Job "Hartonomous_Cognitive_Kernel".
    * **Schedule**: Recurring (e.g., 15-minute interval).
    * **Command**: `EXEC dbo.sp_Analyze;`
* Execute provisioning script `scripts/sql/Create-AutonomousAgentJob.sql`.

**Verification**:
```sql
-- Verify job exists and is enabled
SELECT 
    job.name,
    job.enabled,
    schedule.name AS schedule_name,
    schedule.freq_type,
    schedule.freq_interval
FROM msdb.dbo.sysjobs job
INNER JOIN msdb.dbo.sysjobschedules job_schedule ON job.job_id = job_schedule.job_id
INNER JOIN msdb.dbo.sysschedules schedule ON job_schedule.schedule_id = schedule.schedule_id
WHERE job.name = 'Hartonomous_Cognitive_Kernel';

-- Monitor execution history
SELECT * FROM dbo.AutonomousImprovementHistory
ORDER BY AnalysisTimestamp DESC;
```

---

## Phase 5: End-to-End Validation Gate

**Objective**: Verify system integrity across all layers before production traffic.

### 5.1 Smoke Test Protocol

Perform the following validation sequence in order. Failure at any step requires immediate remediation.

#### Test 1: Physical Layer Validation
Execute CLR scalar function `dbo.fn_VectorDotProduct`.

**Pass Criteria**: Returns correct scalar value; no CLR security exceptions.

```sql
DECLARE @vec1 VARBINARY(MAX) = 0x3F8000003F8000003F800000; -- [1.0, 1.0, 1.0]
DECLARE @vec2 VARBINARY(MAX) = 0x3F8000003F8000003F800000;

SELECT dbo.clr_VectorDotProduct(@vec1, @vec2) AS DotProduct;
-- Expected: 3.0
```

#### Test 2: Nervous System Validation
Verify Worker Service connectivity.

**Pass Criteria**: Console/Logs show active polling of Service Broker queues.

```powershell
# Check worker logs for connection confirmation
Get-Content "C:\Logs\Hartonomous.Workers.Neo4jSync\log-latest.txt" -Tail 50
# Expected: "Successfully connected to Service Broker queue: Neo4jSyncQueue"
```

#### Test 3: Ingestion Validation
Submit text payload via `POST /api/ingestion/file`.

**Pass Criteria**: HTTP 200 OK.

```powershell
$response = Invoke-WebRequest -Uri "https://localhost:5001/api/ingestion/file" `
    -Method POST `
    -Form @{
        file = Get-Item "test.txt"
        tenantId = 0
    }

$response.StatusCode  # Expected: 200
```

#### Test 4: Storage Verification
Query `dbo.Atom`.

**Pass Criteria**: Row count incremented; `ContentHash` populated.

```sql
SELECT COUNT(*) AS TotalAtoms, 
       COUNT(DISTINCT ContentHash) AS UniqueHashes
FROM dbo.Atom
WHERE TenantId = 0;
-- Expected: Row counts increased after ingestion
```

#### Test 5: Propagation Verification
Check Worker Logs.

**Pass Criteria**: "Received message..." and "Syncing to Neo4j..." events logged.

```powershell
Get-Content "C:\Logs\Hartonomous.Workers.Neo4jSync\log-latest.txt" -Tail 20 | 
    Select-String -Pattern "Received message|Syncing to Neo4j"
# Expected: Recent log entries showing message processing
```

#### Test 6: Cognition Verification
Wait for OODA loop interval.

**Pass Criteria**: `dbo.PendingActions` populated with system-generated optimization tasks.

```sql
-- Wait 15 minutes for next OODA cycle, then check
SELECT TOP 10 
    ActionType,
    Priority,
    Reasoning,
    CreatedTimestamp
FROM dbo.PendingActions
WHERE Status = 'Pending'
ORDER BY CreatedTimestamp DESC;
-- Expected: Records with ActionType like 'CreateIndex', 'PruneEmbeddings', etc.
```

---

## Phase 6: Performance Baselines

### 6.1 Establish Key Performance Indicators (KPIs)

| Metric | Target | Measurement Method |
|--------|--------|-------------------|
| Vector Similarity Search (1536-dim, 1M vectors) | < 50ms p95 | `SELECT dbo.clr_CosineSimilarity(...)` |
| Atom Ingestion Throughput | > 1000 atoms/sec | Monitor `dbo.Atom` insert rate |
| Service Broker Latency | < 100ms queue-to-worker | Timestamp diff between enqueue and dequeue |
| OODA Cycle Completion | < 5 minutes | `AutonomousImprovementHistory.DurationMs` |
| API Response Time (p95) | < 200ms | Application Insights telemetry |

### 6.2 Load Testing

Execute load tests using `tests/Hartonomous.PerformanceTests/`:

```powershell
cd tests\Hartonomous.PerformanceTests
dotnet run --configuration Release -- `
    --test VectorSimilaritySearch `
    --concurrency 50 `
    --duration 300
```

---

## Phase 7: Production Readiness Checklist

- [ ] **Security**: CLR assemblies signed and certificate deployed
- [ ] **Performance**: All KPIs meet target thresholds
- [ ] **Resilience**: Worker services configured with auto-restart
- [ ] **Monitoring**: Application Insights instrumentation enabled
- [ ] **Backup**: Automated backup strategy configured (RPO < 1 hour)
- [ ] **Documentation**: Runbooks created for common operational tasks
- [ ] **Disaster Recovery**: Tested restore procedure from backup
- [ ] **Capacity Planning**: Resource utilization monitored under load
- [ ] **Security Scanning**: No critical vulnerabilities in dependencies
- [ ] **Compliance**: Data retention policies configured per tenant

---

## Rollback Strategy

In case of critical issues post-deployment:

1. **Immediate**: Disable SQL Server Agent Job to pause autonomous operations
2. **API Layer**: Revert to previous Docker image/deployment slot
3. **Worker Services**: Stop services via `systemctl stop` or `Stop-Service`
4. **Database**: Restore from last known-good backup (max data loss: 1 hour)
5. **CLR Assemblies**: Execute rollback script `scripts/sql/Rollback-CLRAssemblies.sql`

---

## Success Metrics

**Definition of Done**:
* All smoke tests pass without manual intervention
* System operates autonomously for 72 hours without errors
* Performance KPIs consistently meet targets under simulated production load
* Zero critical/high severity security findings
* Complete operational documentation reviewed by stakeholders

---

## Document Owner

**System Architect**: Core Engineering Team  
**Stakeholders**: Database Administration, DevOps, Security, Product Management  
**Review Cycle**: Bi-weekly during implementation; Monthly post-production  

---

## Related Documents

* [CLR Assembly Deployment Guide](../deployment/CLR_ASSEMBLY_DEPLOYMENT.md)
* [Database Schema Documentation](../architecture/catalog-management.md)
* [OODA Loop Architecture](../architecture/ooda-loop.md)
* [Service Broker Integration](../api/streaming.md)
