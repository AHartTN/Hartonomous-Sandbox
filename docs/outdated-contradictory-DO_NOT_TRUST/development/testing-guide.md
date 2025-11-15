# Testing Strategy & Coverage

**Test Projects**: 6  
**Unit Tests**: 39 test files  
**Integration Tests**: 3 test files  
**Database Tests**: 3 test files  
**End-to-End Tests**: 1 test file  
**Coverage**: ~30-40% (unit tests only, integration tests infrastructure-blocked)

---

## Overview

Hartonomous testing follows a **pragmatic pyramid** approach:

```
    /\        E2E Tests (2 tests, smoke tests only)
   /  \       
  /____\      Integration Tests (28 tests, infrastructure-blocked)
 /      \     
/________\    Unit Tests (110+ tests, ALL PASSING, manual stubs)
```

**Philosophy**: Self-improving AI requires robust testing. OODA loop, student model extraction, and autonomous improvements must be validated continuously.

---

## Test Projects

### 1. Hartonomous.UnitTests

**Location**: `tests/Hartonomous.UnitTests/`  
**Test Files**: 39  
**Framework**: xUnit  
**Status**: ✅ 110+ tests passing

**Test Categories:**

**Core Services (7 files):**
- `BaseServiceTests.cs` - Base class validation
- `UtilitiesTests.cs` - Helper function tests
- `SqlVectorAvailabilityTests.cs` - VECTOR type detection
- `ModelCapabilityServiceTests.cs` - Model capability mapping
- `InferenceMetadataServiceTests.cs` - Metadata extraction
- `GenerationStreamConfigurationTests.cs` - Streaming config
- `ConfigurationServiceTests.cs` - App configuration

**Model Ingestion (5 files):**
- `ModelReaderTests.cs` (11,164 bytes) - ONNX parsing
- `IngestionOrchestratorTests.cs` (11,461 bytes) - Pipeline orchestration
- `ModelIngestionProcessorTests.cs` (15,956 bytes) - Layer extraction
- `EmbeddingIngestionServiceTests.cs` - Embedding pipeline
- `AtomicStorageTestServiceTests.cs` - Atomic storage

**Atom Ingestion (2 files):**
- `AtomIngestionServiceTests.cs` (12,369 bytes) - Deduplication, semantic similarity
- `IngestionStatisticsServiceTests.cs` - Statistics tracking

**Messaging (5 files):**
- `ServiceBrokerMessagePumpTests.cs` (9,074 bytes) - Message pump lifecycle
- `SqlMessageBrokerTests.cs` (8,450 bytes) - Service Broker commands
- `ServiceBrokerResilienceStrategyTests.cs` - Retry/circuit breaker
- `ServiceBrokerCommandBuilderTests.cs` - SQL command generation
- `SqlServerTransientErrorDetectorTests.cs` - Transient error detection

**Billing (1 file):**
- `UsageBillingMeterTests.cs` (9,021 bytes) - Metering logic

**Resilience (2 files):**
- `ExponentialBackoffRetryPolicyTests.cs` - Retry policies
- `CircuitBreakerPolicyTests.cs` - Circuit breaker patterns

**Security (1 file):**
- `SecurityServicesTests.cs` - Authentication/authorization helpers

**Other (3 files):**
- `QueryServiceTests.cs` - Query building
- `IdentitySeedDataTests.cs` - Identity seeding
- `ComponentStreamTests.cs` (SqlClr) - CLR stream processing

**Test Approach**: Uses manual stubs (NOT mocking frameworks), validates core logic in isolation

---

### 2. Hartonomous.Core.Tests

**Location**: `tests/Hartonomous.Core.Tests/`  
**Test Files**: 1  
**Framework**: xUnit  
**Status**: ✅ Passing

**Tests:**
- `PerceptualHasherTests.cs` - Perceptual hashing for image deduplication

---

### 3. Hartonomous.IntegrationTests

**Location**: `tests/Hartonomous.IntegrationTests/`  
**Test Files**: 3  
**Framework**: xUnit  
**Status**: ⚠️ 26/28 tests INFRASTRUCTURE-BLOCKED (not code issues)

**Test Files:**

1. **InferenceIntegrationTests.cs**
   - End-to-end inference pipeline
   - Requires: SQL Server connection, models loaded
   - Status: BLOCKED (missing connection string)

2. **Api/ApiControllerTests.cs**
   - REST API endpoint validation
   - Requires: Test server, database, auth configured
   - Status: BLOCKED (External ID provider not configured)

3. **Api/AuthenticationAuthorizationTests.cs**
   - Auth flow testing
   - Requires: Identity provider, API keys
   - Status: BLOCKED (External ID not configured)

4. **Neo4j/GraphProjectionIntegrationTests.cs**
   - Neo4j graph sync validation
   - Requires: Neo4j server running
   - Status: BLOCKED (Neo4j not running)

**Blockers:**
- Missing `appsettings.Testing.json` with connection strings
- Neo4j server not configured for CI
- External Identity provider not set up

**Code Quality**: Tests are WELL-WRITTEN, just need infrastructure setup

---

### 4. Hartonomous.DatabaseTests

**Location**: `tests/Hartonomous.DatabaseTests/`  
**Test Files**: 3  
**Framework**: xUnit  
**Status**: ⚠️ Infrastructure-dependent

**Tests:**

1. **SqlServerContainerSmokeTests.cs**
   - SQL Server Docker container startup
   - Validates database connectivity
   - Status: Depends on Docker availability

2. **GenerationProcedureTests.cs**
   - Tests `sp_AttentionInference`, `sp_CognitiveActivation`
   - Validates stored procedure logic
   - Status: Requires SQL Server 2025

3. **Contracts/SqlContractTests.cs**
   - SQL schema contract validation
   - Ensures EF entities match database schema
   - Status: Requires database deployment

---

### 5. Hartonomous.SqlClr.Tests

**Location**: `tests/Hartonomous.SqlClr.Tests/`  
**Test Files**: 1  
**Framework**: xUnit  
**Status**: ⚠️ Placeholder only

**Tests:**
- `PlaceholderTests.cs` - Minimal test to prevent empty project

**Missing Tests:**
- CLR function validation (`clr_VectorSimilarity`, `clr_SpatialProject`)
- IsolationForest/LOF anomaly detection
- Gödel Engine prime search (`clr_FindPrimes`)

---

### 6. Hartonomous.EndToEndTests

**Location**: `tests/Hartonomous.EndToEndTests/`  
**Test Files**: 1  
**Framework**: xUnit  
**Status**: ✅ 2 tests passing (smoke tests)

**Tests:**
- `ModelDistillationFlowTests.cs` - Student model extraction smoke test

**Coverage**: ~1% of end-to-end scenarios

---

## Coverage Analysis

**Current State** (based on test file counts and sizes):

| Component | Unit Tests | Integration Tests | Coverage Estimate |
|-----------|------------|-------------------|-------------------|
| **Core Services** | ✅ 7 files | ❌ None | 40% |
| **Model Ingestion** | ✅ 5 files | ⚠️ Blocked | 50% |
| **Atom Ingestion** | ✅ 2 files | ⚠️ Blocked | 35% |
| **Messaging** | ✅ 5 files | ❌ None | 60% |
| **Billing** | ✅ 1 file | ❌ None | 30% |
| **API Controllers** | ❌ None | ⚠️ Blocked | 5% |
| **Database Procedures** | ❌ None | ⚠️ Blocked | 10% |
| **CLR Functions** | ❌ None | ❌ None | 0% |
| **OODA Loop** | ❌ None | ❌ None | 0% |
| **Student Models** | ❌ None | ✅ 1 smoke test | 5% |
| **Neo4j Sync** | ✅ 1 file (message pump) | ⚠️ Blocked | 20% |

**Overall Coverage**: ~30-40% (unit tests only, core logic)

---

## Testing Strategy by Component

### OODA Loop Testing (CRITICAL - CURRENTLY MISSING)

**Required Tests:**

1. **sp_Analyze Tests**:
   - Anomaly detection with known outliers
   - IsolationForest threshold validation
   - Query Store recommendations parsing

2. **sp_Act Tests**:
   - IndexOptimization action execution
   - QueryRegression plan forcing
   - CacheWarming preload verification

3. **sp_Learn Tests**:
   - Performance delta calculation
   - Outcome classification (HighSuccess, Regressed)
   - Model weight fine-tuning trigger

4. **Service Broker Tests**:
   - Message flow (Analyze → Act → Learn → Analyze)
   - Conversation lifecycle
   - Poison message handling

**Test Approach**: Use SQL Server LocalDB or Docker container for database tests

---

### Student Model Extraction Testing

**Current Tests**: 1 smoke test in `ModelDistillationFlowTests.cs`

**Required Tests:**

1. **ExtractByImportanceAsync**:
   - Verify 30% ratio extracts correct layer count
   - Validate student model naming convention
   - Confirm layer data copied correctly

2. **ExtractByLayersAsync**:
   - Test SQL bulk insert performance
   - Validate top N layers selected

3. **ExtractBySpatialRegionAsync**:
   - Filter layers by weight range
   - Verify spatial index queries

4. **CompareModelsAsync**:
   - Calculate compression ratio correctly
   - Count shared layers accurately

**Test Data**: Create test models with known layer counts (e.g., 10-layer test model)

---

### CLR Function Testing (CRITICAL - CURRENTLY MISSING)

**Required Tests:**

1. **Vector Similarity**:
   ```csharp
   [Fact]
   public void VectorSimilarity_IdenticalVectors_ReturnsOne()
   {
       var vec = new float[1998];
       Array.Fill(vec, 0.5f);
       var result = SqlClrFunctions.clr_VectorSimilarity(vec, vec);
       Assert.Equal(1.0, result, 0.0001);
   }
   ```

2. **Anomaly Detection**:
   ```csharp
   [Fact]
   public void IsolationForest_DetectsOutliers()
   {
       var normalData = GenerateNormalDistribution(1000);
       var outliers = new[] { 100.0, 200.0, 300.0 }; // Clear outliers
       var scores = SqlClrFunctions.IsolationForestScore(normalData.Concat(outliers));
       Assert.True(scores[1003] > 0.7); // Outlier score > threshold
   }
   ```

3. **Spatial Projection**:
   ```csharp
   [Fact]
   public void SpatialProject_Vector_ReturnsGeometry()
   {
       var vec = CreateTestVector(1998);
       var geometry = SqlClrFunctions.clr_SpatialProject(vec);
       Assert.NotNull(geometry);
       Assert.Equal(3, geometry.Dimensions); // 3D point
   }
   ```

4. **Prime Search (Gödel Engine)**:
   ```csharp
   [Fact]
   public void FindPrimes_Range_ReturnsKnownPrimes()
   {
       var result = SqlClrFunctions.clr_FindPrimes(1, 100);
       var primes = JsonConvert.DeserializeObject<long[]>(result);
       Assert.Contains(2, primes);
       Assert.Contains(97, primes);
       Assert.DoesNotContain(100, primes);
   }
   ```

---

### API Controller Testing (CURRENTLY MISSING)

**Required Tests** (once infrastructure unblocked):

1. **AnalyticsController**: 18 endpoints
2. **AutonomyController**: OODA loop triggers
3. **BillingController**: Usage tracking, quota enforcement
4. **EmbeddingsController**: Embedding generation
5. **GenerationController**: Text generation
6. **InferenceController**: Inference requests
7. **ModelsController**: Model management
8. **ProvenanceController**: Graph queries

**Test Approach**:
```csharp
[Fact]
public async Task GenerateText_ValidRequest_ReturnsCompletion()
{
    var request = new GenerationRequest
    {
        Prompt = "Hello world",
        MaxTokens = 50
    };
    
    var response = await _client.PostAsJsonAsync("/api/generation/generate", request);
    
    response.EnsureSuccessStatusCode();
    var result = await response.Content.ReadFromJsonAsync<GenerationResponse>();
    Assert.NotNull(result.GeneratedText);
}
```

---

## Performance Testing (PLANNED)

**Load Tests:**

1. **Inference Throughput**:
   - Target: 100 req/sec sustained
   - Measure: 95th percentile latency < 500ms

2. **Billing Ledger**:
   - Target: 10,000 inserts/sec (In-Memory OLTP)
   - Measure: < 1ms avg insert time

3. **Spatial Search**:
   - Target: 10ms for KNN (k=10) on 1M embeddings
   - Measure: R-tree index performance

**Tools**: NBomber, BenchmarkDotNet

---

## Security Testing (PLANNED)

**Attack Scenarios:**

1. **SQL Injection**:
   - Test all API endpoints with malicious input
   - Validate parameterized queries

2. **CLR Exploit**:
   - Attempt unsafe memory access
   - Test assembly load restrictions

3. **Quota Bypass**:
   - Try to exceed tenant quotas
   - Validate enforcement in billing layer

4. **OODA Loop Sabotage**:
   - Inject malicious hypotheses
   - Test auto-approve threshold protection

---

## Continuous Integration

**GitHub Actions** (PLANNED):

```yaml
name: Test Suite

on: [push, pull_request]

jobs:
  unit-tests:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v3
      - name: Run Unit Tests
        run: dotnet test tests/Hartonomous.UnitTests --logger "console;verbosity=detailed"
  
  integration-tests:
    runs-on: ubuntu-latest
    services:
      sqlserver:
        image: mcr.microsoft.com/mssql/server:2025-latest
        env:
          ACCEPT_EULA: Y
          SA_PASSWORD: Test123!
      neo4j:
        image: neo4j:5.15
        env:
          NEO4J_AUTH: neo4j/test
    steps:
      - uses: actions/checkout@v3
      - name: Deploy Database
        run: ./scripts/deploy-database-unified.ps1 -Server localhost -Database Hartonomous
      - name: Run Integration Tests
        run: dotnet test tests/Hartonomous.IntegrationTests
```

---

## Coverage Goals

**Short-Term (1 month):**
- ✅ Unit test coverage: 60% (from 40%)
- ✅ Fix integration test infrastructure (connection strings, Neo4j setup)
- ✅ Add CLR function tests (vector similarity, anomaly detection)

**Medium-Term (3 months):**
- ✅ OODA loop database tests (sp_Analyze, sp_Act, sp_Learn)
- ✅ API controller tests (all 18 controllers)
- ✅ Student model extraction tests
- ✅ Performance benchmarks

**Long-Term (6 months):**
- ✅ 80% code coverage across all components
- ✅ E2E test suite (10+ scenarios)
- ✅ Security penetration testing
- ✅ Load testing (1000 req/sec)

---

## Running Tests

**Unit Tests:**
```powershell
cd tests/Hartonomous.UnitTests
dotnet test --logger "console;verbosity=detailed"
```

**Integration Tests** (after infrastructure setup):
```powershell
# Start SQL Server Docker container
docker run -e "ACCEPT_EULA=Y" -e "SA_PASSWORD=Test123!" -p 1433:1433 mcr.microsoft.com/mssql/server:2025-latest

# Deploy database
.\scripts\deploy-database-unified.ps1 -Server localhost -Database HartonomousTest

# Run tests
cd tests/Hartonomous.IntegrationTests
dotnet test
```

**All Tests:**
```powershell
dotnet test Hartonomous.Tests.sln
```

---

## Test Data Management

**Fixture Data:**
- Test models: 10-layer, 20-layer, 50-layer models
- Test embeddings: Known vectors for similarity validation
- Test atoms: Sample text, image, audio atoms

**Database Seeding:**
```sql
-- Create test tenant
INSERT INTO dbo.Tenants (TenantName) VALUES ('TestTenant');

-- Create test model
INSERT INTO dbo.Models (ModelName, ModelType, Architecture)
VALUES ('TestModel-10L', 'test', 'transformer');

-- Create test layers
INSERT INTO dbo.ModelLayers (ModelId, LayerIdx, LayerName, LayerType, ParameterCount)
SELECT 
    (SELECT ModelId FROM dbo.Models WHERE ModelName = 'TestModel-10L'),
    n,
    'layer_' + CAST(n AS NVARCHAR(10)),
    'attention',
    1000000
FROM (SELECT TOP 10 ROW_NUMBER() OVER (ORDER BY (SELECT NULL)) - 1 AS n FROM sys.objects) AS numbers;
```

---

## Next Steps

1. **Unblock Integration Tests**: Configure connection strings, start Neo4j, set up External ID
2. **Add CLR Tests**: Vector similarity, anomaly detection, prime search
3. **Add OODA Loop Tests**: Database tests for sp_Analyze, sp_Act, sp_Learn
4. **Add API Tests**: Controller tests for all 18 endpoints
5. **Performance Benchmarks**: Inference throughput, spatial search, billing inserts
6. **CI/CD Pipeline**: GitHub Actions with SQL Server + Neo4j containers
