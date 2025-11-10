# Refactor Targets & Aspirational Features

**Inventory of planned features, stub implementations, and architectural gaps requiring completion or removal.**

---

## 1. GPU Acceleration (ILGPU)

### Status: Stub Implementation

**Files:**

- `src/Hartonomous.Core.Performance/GpuVectorAccelerator.cs`
- References in `src/Hartonomous.Infrastructure/Services/EmbeddingService.cs:32`
- References in `src/Hartonomous.Core.Performance/VectorMath.cs:13`

**Current State:**

- `GpuVectorAccelerator` class exists with singleton pattern
- Constructor contains placeholder: `// Future: Initialize ILGPU context and accelerator`
- Methods `CosineSimilarityBatch` and `DotProductBatch` return `false` with comment: `// TODO: Enable when ILGPU kernels are implemented`
- `IsAvailable` property returns `false`

**Planned Architecture:**

- ILGPU kernel compilation for batch vector operations (cosine similarity, dot product, euclidean distance)
- Fallback to CPU SIMD when GPU unavailable
- 100-1000× performance improvement for batch operations (>1M vectors)
- Requires NVIDIA/AMD GPU with compute capability

**Dependencies:**

- ILGPU NuGet package (not currently referenced)
- ILGPU.Algorithms package
- GPU memory interop (requires UNSAFE permission)

**Deployment Impact:**

- Must add `ILGPU.dll` and `ILGPU.Algorithms.dll` to CLR assembly list in `scripts/deploy-database-unified.ps1`
- Update `docs/CLR_DEPLOYMENT.md` to document GPU hardware requirements

**Decision Required:**

- [ ] **Implement:** Add ILGPU dependencies, write GPU kernels, integrate with `VectorMath.cs`
- [ ] **Remove:** Delete `GpuVectorAccelerator.cs`, remove GPU comments from `EmbeddingService.cs` and `VectorMath.cs`

---

## 2. Graph Neural Networks (GNN)

### Status: False Documentation Claim

**Documentation Reference:** `README.md` mentions GNN capabilities

**Search Results:** `grep_search` for `GNN|Graph Neural` returned **0 matches** in `src/**/*.cs`

**Actual Graph Implementation:**

- Neo4j provenance graph (`src/Neo4jSync/Services/ProvenanceGraphBuilder.cs`)
- SQL Server graph tables (`sql/tables/graph.AtomGraphNodes.sql`, `graph.AtomGraphEdges.sql`)
- Neo4j Cypher query endpoints (`GraphQueryController.cs`)
- No neural network architecture for graph learning

**Impact:**

- README.md makes unverifiable claim about GNN capabilities
- No graph convolutional layers, graph attention networks, or message passing neural networks found

**Remediation:**

- [ ] Update README.md to remove GNN claim or clarify as "future planned feature"
- [ ] If GNN implementation is planned, document in this section with architecture sketch

---

## 3. Advanced Anomaly Detection Aggregates

### Status: Partially Implemented

**Current Implementation:**

- `sp_Analyze` uses simple `AVG()` comparison (>2× standard deviations)
- Comments reference "ISOLATION FOREST" and "CLR aggregates for anomaly detection"

**Evidence of Planned Implementation:**

- `src/SqlClr/Core/VectorUtilities.cs:99` comment mentions `AnomalyDetectionAggregates`
- `src/SqlClr/MachineLearning/MahalanobisDistance.cs:9` references "REPLACES: AnomalyDetectionAggregates.cs:497-525 (diagonal-only covariance - mathematically incorrect)"

**Missing Components:**

- `IsolationForestAggregate` CLR aggregate (not found in codebase)
- `LocalOutlierFactorAggregate` (LOF) CLR aggregate (not found)
- `AnomalyDetectionAggregates.cs` file (referenced but does not exist)

**Integration Points:**

- `sql/procedures/dbo.sp_Analyze.sql` (currently uses placeholder logic)
- Autonomous OODA loop (relies on accurate anomaly detection)

**Roadmap:**

- [ ] Implement `IsolationForestAggregate` using scikit-learn algorithm port
- [ ] Implement `LocalOutlierFactorAggregate` (density-based outlier detection)
- [ ] Update `sp_Analyze` to use CLR aggregates instead of simple threshold
- [ ] Add unit tests in `tests/Hartonomous.UnitTests/SqlClr/`

---

## 4. CLR Strict Security Hardening

### Status: Development Mode

**Current State:**

- `scripts/deploy-database-unified.ps1` disables `clr strict security` (`EXEC sp_configure 'clr strict security', 0`)
- Assemblies deployed with inline hex (`CREATE ASSEMBLY ... FROM 0x...`)
- No strong-name signing on `SqlClrFunctions.dll` or dependencies
- `TRUSTWORTHY OFF` (correct) but relies on disabled strict security

**Target State:**

- `clr strict security = 1` (SQL Server default)
- All user assemblies registered in `sys.trusted_assemblies` via `sys.sp_add_trusted_assembly`
- Strong-name signed DLLs with SHA-512 hash registration
- `scripts/deploy-clr-secure.ps1` becomes default deployment path

**Gap Closure Plan:**

1. **Strong-Name Signing:**
   - Add `.snk` key file to `src/SqlClr/`
   - Set `AssemblyOriginatorKeyFile` property in `SqlClrFunctions.csproj`
   - Sign dependencies (`Hartonomous.Database.Clr.csproj`)
   - Update `scripts/copy-dependencies.ps1` to copy signed outputs

2. **Trusted Assembly Registration:**
   - Emit SHA-512 manifest during build (JSON/CSV: `{ Name, Hash, Path }`)
   - Update `scripts/deploy-clr-secure.ps1` to consume manifest
   - Register each hash: `EXEC sys.sp_add_trusted_assembly @hash, N'AssemblyName'`

3. **Default Workflow Update:**
   - Make `deploy-clr-secure.ps1` the documented default
   - Retain `deploy-database-unified.ps1 -SkipStrictSecurity` for development

4. **Regression Testing:**
   - Add test in `tests/Hartonomous.DatabaseTests/` to verify `sys.trusted_assemblies` population
   - Assert `clr strict security = 1` in deployment verification

**Documentation Impact:**

- Update `docs/CLR_DEPLOYMENT.md` to reflect new workflow
- Update `DATABASE_DEPLOYMENT_GUIDE.md` with security-first instructions

**Acceptance Criteria:**

- [ ] Running `deploy-database-unified.ps1` leaves `clr strict security = 1`
- [ ] All assemblies present in `sys.trusted_assemblies`
- [ ] `sys.assemblies` shows only signed user assemblies with `UNSAFE` permission
- [ ] Extended Events session (`AI_Operations`) enabled by default for CLR monitoring

**Reference:** `scripts/CLR_SECURITY_ANALYSIS.md`

---

## 5. Unimplemented Interfaces & Services

### 5.1 `ISemanticEnricher` (Event Processing)

**File:** `src/Hartonomous.Core/Interfaces/IEventProcessing.cs:48`

**Status:** Interface defined, no implementation found

**Purpose:** Enrich events with semantic metadata (embeddings, entity extraction, sentiment)

**Required Actions:**

- [ ] Implement `SemanticEnricher` class in `Hartonomous.Infrastructure/Services/`
- [ ] Integrate with ingestion pipeline
- [ ] Register in `DependencyInjection.cs`

---

### 5.2 `ICloudEventPublisher` (Event Processing)

**File:** `src/Hartonomous.Core/Interfaces/IEventProcessing.cs:59`

**Status:** Interface defined, no implementation found

**Purpose:** Publish CloudEvents to external event bus (Azure Event Grid, Kafka, etc.)

**Required Actions:**

- [ ] Implement `CloudEventPublisher` using CloudEvents SDK
- [ ] Add Azure Event Grid integration
- [ ] Configure via `appsettings.json` (`CloudEvents:PublisherEndpoint`)

---

### 5.3 `IModelDiscoveryService`

**File:** `src/Hartonomous.Core/Interfaces/IModelDiscoveryService.cs:7`

**Status:** Interface defined, partial implementation suspected

**Purpose:** Auto-discover ML models from registry, filesystem, or model hub

**Required Actions:**

- [ ] Search for implementation: `grep_search` for `class.*ModelDiscoveryService`
- [ ] If missing, implement model registry scanning logic
- [ ] Integrate with `ModelsController` for dynamic model loading

---

### 5.4 `IIngestionStatisticsService`

**File:** `src/Hartonomous.Core/Interfaces/IIngestionStatisticsService.cs:3`

**Status:** Interface defined, implementation unknown

**Purpose:** Track ingestion metrics (throughput, deduplication rate, error count)

**Required Actions:**

- [ ] Verify if implementation exists in `Hartonomous.Infrastructure/Services/`
- [ ] If missing, implement with Application Insights integration
- [ ] Expose metrics via `OperationsController` (`GET /api/v1/operations/metrics`)

---

## 6. OODA Loop Enhancements

### 6.1 Automated Hypothesis Scoring

**Current State:** `sp_Hypothesize` uses hardcoded scores (0-100) based on simple pattern matching

**Planned:** ML model to predict actual performance impact of proposed optimizations

**Requirements:**

- Historical improvement data from `dbo.AutonomousImprovementHistory`
- Feature engineering: anomaly severity, system load, time of day, hypothesis category
- Regression model (XGBoost, LightGBM, or simple linear regression)
- Training pipeline in `src/Hartonomous.Workers.ModelIngestion/`

**Integration:**

- [ ] Train model on historical OODA cycle outcomes
- [ ] Deploy model as SQL CLR function: `dbo.clr_PredictHypothesisImpact(@features)`
- [ ] Update `sp_Hypothesize` to call CLR function for scoring

---

### 6.2 Automatic Rollback Mechanism

**Current State:** `sp_Learn` measures performance delta but does not revert actions if negative

**Planned:** Automatic rollback if improvement <−5% or failure spike detected

**Requirements:**

- Rollback stored procedure: `sp_RollbackAutonomousAction(@ActionId)`
- Action logging in `dbo.PendingActions` with `RollbackScript` column
- Safety check: only rollback actions <24 hours old

**Integration:**

- [ ] Add `RollbackScript` column to `dbo.PendingActions`
- [ ] Update `sp_Act` to populate rollback logic (e.g., `DROP INDEX` for `CREATE INDEX`)
- [ ] Modify `sp_Learn` to trigger rollback on negative delta

---

### 6.3 Multi-Tenant OODA Isolation

**Current State:** `@TenantId` parameter exists but loops are not isolated per tenant

**Planned:** Separate OODA cycles per tenant with isolated queues and improvement history

**Requirements:**

- Tenant-specific queues: `AnalyzeQueue_TenantId`, `HypothesizeQueue_TenantId`, etc.
- Partitioned `dbo.AutonomousImprovementHistory` by `TenantId`
- Scheduled activation: SQL Agent jobs per tenant or single job with tenant loop

**Integration:**

- [ ] Extend Service Broker setup to create tenant-specific queues
- [ ] Update OODA procedures to filter by `@TenantId` in all queries
- [ ] Add tenant isolation tests in `tests/Hartonomous.IntegrationTests/`

---

### 6.4 OODA Cycle History API

**Current State:** `GET /api/autonomy/cycles/history` returns placeholder response

**Planned:** Persist and expose cycle metadata (observations, hypotheses, actions, outcomes)

**Requirements:**

- Table: `dbo.OodaCycleHistory` (AnalysisId, StartTime, EndTime, Phase, ObservationCount, HypothesesGenerated, ActionsExecuted, PerformanceImprovement)
- Repository: `IAutonomousAnalysisRepository.GetCycleHistoryAsync()`
- Controller: Update `AutonomyController.GetCycleHistory()` to query real data

**Integration:**

- [ ] Create `dbo.OodaCycleHistory` table
- [ ] Update OODA procedures to insert cycle metadata
- [ ] Implement `GetCycleHistoryAsync` in `AutonmousAnalysisRepository`
- [ ] Wire to `AutonomyController`

---

## 7. Spatial Index Optimization

### 7.1 Adaptive Grid Density

**Current State:** Fixed `GRIDS = (LEVEL_1 = HIGH, LEVEL_2 = HIGH)` in spatial indexes

**Planned:** Auto-adjust grid density based on data distribution

**Requirements:**

- Query spatial data distribution: `STEnvelope()` to compute bounding box
- Calculate optimal grid levels based on point density
- Rebuild indexes with tuned settings

**Integration:**

- [ ] Add stored procedure: `sp_OptimizeSpatialIndexes`
- [ ] Include in `sp_Act` hypothesis list
- [ ] Measure improvement via `sp_Learn` (query duration comparison)

---

### 7.2 Clustered Columnstore on Embeddings

**Current State:** Nonclustered columnstore indexes on `dbo.AtomEmbeddings`

**Planned:** Clustered columnstore for 10× compression and faster bulk scans

**Requirements:**

- Rebuild `dbo.AtomEmbeddings` with clustered columnstore
- Add nonclustered B-tree index on `AtomId` for SEEK operations
- Verify compatibility with spatial indexes (may require ROWSTORE for spatial columns)

**Integration:**

- [ ] Prototype on dev environment
- [ ] Measure compression ratio and query performance
- [ ] Update `sql/tables/dbo.AtomEmbeddings.sql` if successful

---

## 8. Telemetry & Observability Gaps

### 8.1 Extended Events for CLR

**Current State:** Extended Events session defined in `scripts/deploy/04-clr-assembly.ps1` but not enabled by default

**Planned:** Enable `AI_Operations` session automatically and stream to Application Insights

**Requirements:**

- Enable session during deployment
- Configure event file target or live data streaming
- Integrate with OpenTelemetry exporter in `Hartonomous.Api`

**Integration:**

- [ ] Enable session in `deploy-clr-secure.ps1`
- [ ] Add query endpoint: `GET /api/v1/operations/clr-events`
- [ ] Stream to Application Insights via `Hartonomous.Infrastructure/Observability`

---

### 8.2 Service Broker Monitoring

**Current State:** Manual queue depth queries via `GET /api/autonomy/queues/status`

**Planned:** Proactive alerting on queue depth >100 or poison messages

**Requirements:**

- Scheduled SQL Agent job to monitor queue depths
- Alert via Azure Monitor or SMTP on threshold breach
- Dashboard in `Hartonomous.Admin` with real-time queue metrics

**Integration:**

- [ ] Create SQL Agent job: `Monitor_ServiceBroker_Health`
- [ ] Add Application Insights custom metrics for queue depth
- [ ] Build Blazor dashboard component in `Hartonomous.Admin/Pages/ServiceBrokerMonitoring.razor`

---

## 9. Neo4j Graph Analytics

### 9.1 GDS (Graph Data Science) Integration

**Current State:** Neo4j queries via Cypher, no Graph Data Science algorithms deployed

**Planned:** PageRank, community detection, shortest path algorithms

**Requirements:**

- Install Neo4j GDS plugin
- Project graph (`CALL gds.graph.project`)
- Expose GDS procedures via `GraphAnalyticsController`

**Integration:**

- [ ] Document GDS installation in `docs/DEPLOYMENT.md`
- [ ] Add GDS endpoints to `GraphAnalyticsController`: `POST /api/v1/graph/pagerank`, `/community-detection`
- [ ] Update Neo4j sync to maintain graph projections

---

## 10. Documentation Corrections

### 10.1 README.md GNN Claim

**Issue:** README.md mentions Graph Neural Networks but no implementation exists

**Action Required:**

- [ ] Remove GNN claim from README.md
- [ ] OR add disclaimer: "Graph Neural Networks (planned for future release)"

---

### 10.2 CLR Deployment Guide SIMD Claim

**Issue:** `docs/CLR_DEPLOYMENT.md` mentions `DotProductAvx2` using raw AVX2 intrinsics, but actual `VectorMath.cs` uses `System.Numerics.Vector<float>`

**Action Required:**

- [ ] Update CLR_DEPLOYMENT.md to clarify SIMD implementation uses `System.Numerics.Vector` (portable SIMD)
- [ ] Note: Hardware-specific AVX2/AVX-512 optimizations are JIT-compiled, not hand-written intrinsics

---

## Decision Matrix

| Feature | Status | Priority | Effort | Action |
|---------|--------|----------|--------|--------|
| **ILGPU GPU Acceleration** | Stub | Medium | High | Implement or Remove |
| **GNN Implementation** | False Claim | High | Very High | Document as Future or Remove Claim |
| **Advanced Anomaly Aggregates** | Partial | High | Medium | Complete Implementation |
| **CLR Strict Security** | Development Mode | **Critical** | Medium | **Complete Before Production** |
| **ISemanticEnricher** | Missing | Low | Low | Implement or Remove Interface |
| **ICloudEventPublisher** | Missing | Low | Low | Implement or Remove Interface |
| **Automated Hypothesis Scoring** | Hardcoded | Medium | High | ML Model Integration |
| **Automatic Rollback** | Missing | High | Medium | Implement Safety Mechanism |
| **Multi-Tenant OODA** | Not Isolated | Medium | Medium | Partition Queues & History |
| **OODA Cycle History API** | Placeholder | Low | Low | Persist & Expose Data |
| **Adaptive Spatial Grid** | Fixed | Low | Low | Auto-Tune Algorithm |
| **Clustered Columnstore** | Nonclustered | Medium | Medium | Prototype & Measure |
| **Extended Events Default** | Disabled | Medium | Low | Enable in Deployment Script |
| **Service Broker Alerts** | Manual | Medium | Low | Proactive Monitoring |
| **Neo4j GDS Algorithms** | Not Deployed | Low | Medium | Install Plugin & Expose APIs |

---

## Timeline & Milestones

### Sprint 2025-Q1 (Critical Path)

- **Week 1-2:** CLR strict security hardening (strong-name signing, trusted assembly registration)
- **Week 3:** Advanced anomaly detection aggregates (IsolationForest, LOF)
- **Week 4:** Automatic rollback mechanism for OODA loop

### Sprint 2025-Q2 (Enhancement)

- ILGPU GPU acceleration implementation
- Multi-tenant OODA isolation
- Automated hypothesis scoring ML model

### Sprint 2025-Q3 (Observability & Polish)

- Extended Events default enablement
- Service Broker proactive monitoring
- Neo4j GDS algorithm integration

### Backlog (Future Consideration)

- ISemanticEnricher, ICloudEventPublisher implementation
- Adaptive spatial grid density
- Clustered columnstore migration

---

## References

- **CLR Security:** `scripts/CLR_SECURITY_ANALYSIS.md`, `docs/CLR_DEPLOYMENT.md`
- **OODA Loop:** `docs/AUTONOMOUS_GOVERNANCE.md`, `sql/procedures/dbo.sp_*.sql`
- **Performance Audit:** `docs/PERFORMANCE_ARCHITECTURE_AUDIT.md`
- **Deployment:** `docs/DEPLOYMENT.md`, `scripts/deploy-database-unified.ps1`
- **Stub Files:** `src/Hartonomous.Core.Performance/GpuVectorAccelerator.cs`
- **Interface Inventory:** `src/Hartonomous.Core/Interfaces/*.cs`, `src/Hartonomous.Core/Messaging/*.cs`
