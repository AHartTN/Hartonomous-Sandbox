# Testing Guide

**Last Updated**: November 13, 2025  
**Test Projects**: 6 (UnitTests, Core.Tests, IntegrationTests, DatabaseTests, SqlClr.Tests, EndToEndTests)  
**Unit Tests**: 110 passing  
**Integration Tests**: 28 total (2 passing, 26 infrastructure-blocked)  
**Status**: Comprehensive unit coverage, integration tests blocked by infrastructure setup

---

## Table of Contents

1. [Executive Summary](#executive-summary)
2. [Current Test Coverage](#current-test-coverage)
3. [Test Pyramid Architecture](#test-pyramid-architecture)
4. [Unit Testing](#unit-testing)
5. [Integration Testing](#integration-testing)
6. [Performance Testing](#performance-testing)
7. [Security Testing](#security-testing)
8. [Coverage Goals & Roadmap](#coverage-goals--roadmap)

---

## Executive Summary

### Testing Philosophy

**Core Principle**: An autonomous AI system that improves itself MUST have comprehensive automated testing. Without tests:
- OODA loop could regress performance
- Student models could degrade quality
- Security vulnerabilities could go undetected
- Billing enforcement could fail silently
- Data corruption could cascade

**Test-Driven Development (TDD) Going Forward**:
1. Write test first (red)
2. Implement feature (green)
3. Refactor (maintain green)

**Continuous Testing**:
- Every commit triggers tests
- Every OODA cycle validated
- Every student model measured
- Every deployment smoke-tested

### Current State Assessment

**Unit Tests** (110 tests, ✅ ALL PASSING):
- **Quality**: Lightweight validation using manual stubs (not mocks)
- **Coverage**: ~30-40% estimated (isolated service layer logic only)
- **Missing**: Controllers, repositories, CLR functions, SQL procedures, workers, background services

**Integration Tests** (28 tests, ⚠️ 25 FAILING):
- **Quality**: Real database/Neo4j/auth tests (NOT stubs)
- **Coverage**: Covers critical paths when infrastructure available
- **Blockers**: Missing connection strings, Neo4j not running, External ID not configured
- **Actual Code Quality**: Tests are WELL-WRITTEN, just need infrastructure

**End-to-End Tests** (2 tests, ✅ PASSING):
- **Quality**: Minimal smoke tests
- **Coverage**: ~1% of end-to-end flows

---

## Current Test Coverage

### ✅ What Actually Works (Evidence-Based)

**Unit Tests That Validate Real Logic**:

1. **AtomIngestionServiceTests.cs** (12,369 bytes)
   - Deduplication logic
   - Semantic similarity calculations
   - Reference counting

2. **ModelIngestionProcessorTests.cs** (15,956 bytes)
   - Model layer extraction
   - Atom relation building
   - Knowledge graph construction

3. **IngestionOrchestratorTests.cs** (11,461 bytes)
   - Multi-modal data pipeline orchestration
   - Workflow coordination
   - Error handling

4. **ModelReaderTests.cs** (11,164 bytes)
   - ONNX model parsing
   - Metadata extraction
   - Layer enumeration

5. **ServiceBrokerMessagePumpTests.cs** (9,074 bytes)
   - Message pump lifecycle
   - Circuit breaker patterns
   - Retry policies

6. **SqlMessageBrokerTests.cs** (8,450 bytes)
   - Service Broker command building
   - Transaction management
   - Message routing

7. **UsageBillingMeterTests.cs** (9,021 bytes)
   - Billing calculation
   - Operation rate application
   - Quota enforcement

8. **EmbeddingIngestionServiceTests.cs** (7,144 bytes)
   - Embedding vector validation
   - Dimension checking
   - Normalization

**Integration Tests That Are Production-Ready** (infrastructure needed):

1. `SemanticSearch_ReturnsOriginalEmbedding` - Tests CLR vector operations + EF Core + SQL Server
2. `HybridSearch_ReturnsSpatialCandidates` - Tests spatial projection (geometry) + vector search
3. `ComputeSpatialProjection_ProducesGeometry` - Tests `VectorUtility.Materialize` + CLR spatial functions
4. `MultiResolutionSearch_UsesSpatialCoordinates` - Tests multi-resolution spatial indexing
5. `StudentModelComparison_ReturnsMetrics` - Tests student model extraction (knowledge distillation)

### ❌ Zero Coverage Areas

**Critical Missing Tests**:

1. **API Controllers** (15 controllers in `src/Hartonomous.Api/Controllers/`)
   - AdminController - Model management, weight rollback, snapshots
   - AtomController - Atom CRUD, payload upload (FILESTREAM)
   - BillingController - Usage ledger queries, rate plan management
   - EmbeddingController - Embedding generation, vector operations
   - HealthController - Health checks, readiness probes
   - InferenceController - Model execution, student model selection
   - ModelController - Model registration, versioning
   - SearchController - Semantic search, hybrid search, spatial queries

2. **Repositories** (20+ repositories in `src/Hartonomous.Data/Repositories/`)
   - Only stub interfaces used in unit tests
   - No actual repository implementation tests

3. **SQL CLR Functions** (14 assemblies)
   - Vector operations (dot product, cosine similarity)
   - Embedding generation
   - Spatial projections
   - JSON operations
   - No `Hartonomous.Database.Clr.Tests` project exists

4. **SQL Stored Procedures** (65+ procedures)
   - OODA loop procedures
   - Billing enforcement
   - Temporal queries
   - No database unit tests

5. **Worker Services**
   - CesConsumer (Central Event Streaming)
   - Neo4jSync (Graph database synchronization)
   - ModelIngestion (Model processing pipeline)

6. **Blazor Admin Components** (`src/Hartonomous.Admin/`)
   - No component tests
   - No UI interaction tests

7. **Advanced Features**
   - Azure Arc integration
   - Service Broker cross-server messaging
   - FILESTREAM operations
   - Temporal table queries
   - CDC (Change Data Capture)
   - Neo4j graph projection
   - External ID authentication

### Test Infrastructure Gaps

- ❌ No `appsettings.Testing.json` (connection strings hardcoded or missing)
- ❌ No test database seeding scripts
- ❌ No test data generators (realistic multi-modal data)
- ❌ No containerized test environment (Docker Compose for SQL + Neo4j)
- ❌ No code coverage reporting configured
- ❌ No mutation testing (test quality validation)
- ❌ No performance/benchmark tests (BenchmarkDotNet project exists but unused)

---

## Test Pyramid Architecture

### The Testing Pyramid

```
        /\
       /  \
      / E2E \           10% - End-to-End Tests (2 tests)
     /------\
    /        \
   / Service  \        20% - Service/API Integration (0 tests)
  /------------\
 /              \
/ Integration    \     30% - Cross-Component Integration (28 tests, 25 failing)
/------------------\
/                    \
/   Unit Tests        \  40% - Component Unit Tests (110 tests passing)
/----------------------\
```

**Target Distribution**:
- **Unit Tests**: 40% of total tests (currently ~90% due to missing higher layers)
- **Integration Tests**: 30% of total tests
- **Service Tests**: 20% of total tests
- **E2E Tests**: 10% of total tests

---

## Unit Testing

### Unit Test Standards

**Framework**: xUnit + FluentAssertions  
**Isolation**: Manual stubs (no mocking framework by design)  
**Coverage Target**: 80% overall, 95% for critical paths

### Test Organization

```
tests/
├── Hartonomous.Core.Tests/
│   ├── Services/
│   │   ├── AtomIngestionServiceTests.cs
│   │   ├── ModelIngestionProcessorTests.cs
│   │   └── EmbeddingIngestionServiceTests.cs
│   └── Utilities/
│       └── ModelReaderTests.cs
├── Hartonomous.Infrastructure.Tests/
│   ├── Messaging/
│   │   ├── ServiceBrokerMessagePumpTests.cs
│   │   └── SqlMessageBrokerTests.cs
│   └── Billing/
│       └── UsageBillingMeterTests.cs
└── Hartonomous.Api.Tests/  (⚠️ MISSING)
    └── Controllers/  (⚠️ NEEDS CREATION)
```

### Critical Paths Requiring 95% Coverage

1. **Billing Enforcement**
   - Pre-execution quota checks
   - Usage tracking
   - Rate limit enforcement

2. **UNSAFE CLR Functions**
   - Vector operations (dot product, cosine similarity)
   - Embedding generation
   - Spatial projections

3. **OODA Loop**
   - Hypothesis generation
   - Action execution
   - Performance measurement

4. **Student Model Distillation**
   - Layer extraction
   - Quality validation
   - Performance comparison

5. **Neo4j Synchronization**
   - SQL → Service Broker → Neo4j pipeline
   - Graph projection accuracy
   - Consistency validation

6. **FILESTREAM Operations**
   - Tensor storage
   - Cross-server access
   - Payload retrieval

---

## Integration Testing

### Integration Test Strategy

**Framework**: xUnit + Testcontainers (future)  
**Database**: Real SQL Server 2025 instance  
**Graph DB**: Real Neo4j instance (when available)  
**Coverage Target**: 70% overall, 90% for critical paths

### Current Integration Tests

**Location**: `tests/Hartonomous.IntegrationTests/`

**Passing Tests** (2):
- Basic health check
- Simple database connectivity

**Failing Tests** (25):
- Missing connection strings in configuration
- Neo4j not running locally
- External ID authentication not configured

### Required Integration Test Infrastructure

1. **Test Configuration**
   ```json
   // appsettings.Testing.json (⚠️ NEEDS CREATION)
   {
     "ConnectionStrings": {
       "DefaultConnection": "Server=localhost;Database=Hartonomous_Test;...",
       "Neo4j": "bolt://localhost:7687"
     },
     "ExternalId": {
       "Authority": "https://test-auth.local",
       "MockMode": true
     }
   }
   ```

2. **Test Database Seeding**
   - Create test data generators
   - Realistic multi-modal atom data
   - Pre-populated model registry
   - Sample embedding vectors

3. **Containerized Test Environment**
   ```yaml
   # docker-compose.test.yml (⚠️ NEEDS CREATION)
   version: '3.8'
   services:
     sqlserver:
       image: mcr.microsoft.com/mssql/server:2025-latest
     neo4j:
       image: neo4j:5.15-community
     mockauth:
       image: # External ID mock service
   ```

### Integration Test Priorities

**Phase 1** (Must-Have):
1. API endpoint tests (all 15 controllers)
2. Repository tests (CRUD operations)
3. Service Broker message flow
4. DACPAC deployment validation

**Phase 2** (Critical):
1. CLR function integration tests
2. Stored procedure tests
3. FILESTREAM operations
4. Temporal queries

**Phase 3** (Advanced):
1. Neo4j synchronization
2. Azure Arc integration
3. CDC validation
4. Cross-server messaging

---

## Performance Testing

### Performance Test Strategy

**Framework**: BenchmarkDotNet (project exists but unused)  
**Targets**: All API endpoints, CLR functions, hot paths  
**Coverage Target**: 100% of performance-critical code

### Performance Benchmarks Needed

1. **Vector Operations**
   - Dot product calculation (1M vectors)
   - Cosine similarity (1M vectors)
   - Embedding generation (batches of 1K, 10K, 100K)

2. **Search Operations**
   - Semantic search (varying result set sizes)
   - Hybrid search (vector + spatial + text)
   - Multi-resolution spatial queries

3. **Model Operations**
   - Student model inference
   - Layer extraction performance
   - Weight materialization

4. **Database Operations**
   - Atom ingestion throughput
   - Temporal query performance
   - FILESTREAM read/write

5. **Message Broker**
   - Service Broker throughput
   - Message pump latency
   - Circuit breaker overhead

### Performance SLAs

| Operation | Target Latency | Target Throughput |
|-----------|---------------|-------------------|
| Semantic Search | < 50ms (p95) | > 1000 qps |
| Embedding Generation | < 100ms (p95) | > 500 ops/sec |
| Atom Ingestion | < 200ms (p95) | > 100 atoms/sec |
| Student Inference | < 10ms (p95) | > 5000 infer/sec |
| Vector Dot Product | < 1ms (p95) | > 100K ops/sec |

---

## Security Testing

### Security Test Categories

1. **Authentication & Authorization**
   - External ID token validation
   - Role-based access control
   - API key authentication

2. **SQL Injection Prevention**
   - Parameterized queries validation
   - Stored procedure input sanitization

3. **CLR Security**
   - UNSAFE assembly isolation
   - Permission set validation
   - Assembly signature verification

4. **Data Protection**
   - Encryption at rest validation
   - TLS enforcement
   - PII handling compliance

5. **Rate Limiting**
   - Quota enforcement
   - Throttling effectiveness
   - DDoS resistance

---

## Coverage Goals & Roadmap

### Coverage Targets (6 Months)

| Layer | Current | 3-Month Target | 6-Month Target | Critical Path |
|-------|---------|----------------|----------------|---------------|
| Unit Tests | 30-40% | 60% | 80% | 95% |
| Integration Tests | ~5% | 40% | 70% | 90% |
| API Tests | 0% | 80% | 95% | 100% |
| CLR Tests | 0% | 60% | 80% | 95% |
| Performance Tests | 0% | 50% | 100% | 100% |
| Security Tests | 0% | 40% | 60% | 90% |

### Implementation Roadmap

**Month 1-2: Foundation**
- ✅ Create `appsettings.Testing.json`
- ✅ Set up Testcontainers infrastructure
- ✅ Fix 25 failing integration tests
- ✅ Create API controller test suite (15 controllers)
- ✅ Configure code coverage reporting

**Month 3-4: Critical Paths**
- ✅ Create CLR function test project
- ✅ Test all 14 CLR assemblies
- ✅ Create stored procedure tests (top 20 critical procedures)
- ✅ Set up BenchmarkDotNet for performance testing
- ✅ Achieve 60% overall coverage

**Month 5-6: Advanced & Polish**
- ✅ Worker service tests (CesConsumer, Neo4jSync, ModelIngestion)
- ✅ Neo4j synchronization tests
- ✅ Security penetration testing
- ✅ OODA loop validation tests
- ✅ Student model quality tests
- ✅ Achieve 80% overall coverage

**Ongoing**:
- Mutation testing setup
- Test quality validation
- Coverage maintenance
- Performance regression detection

---

## Running Tests

### Run All Tests

```powershell
# Run all tests
dotnet test

# Run with coverage
dotnet test /p:CollectCoverage=true /p:CoverletOutputFormat=opencover

# Run specific test project
dotnet test tests/Hartonomous.Core.Tests/

# Run tests matching pattern
dotnet test --filter "FullyQualifiedName~AtomIngestion"
```

### Run Integration Tests Only

```powershell
# Ensure test infrastructure is running
docker-compose -f docker-compose.test.yml up -d

# Run integration tests
dotnet test tests/Hartonomous.IntegrationTests/

# Tear down test infrastructure
docker-compose -f docker-compose.test.yml down
```

### Run Performance Benchmarks

```powershell
# Run all benchmarks
dotnet run --project tests/Hartonomous.Benchmarks/ -c Release

# Run specific benchmark
dotnet run --project tests/Hartonomous.Benchmarks/ -c Release --filter *VectorOperations*
```

---

## Test Data Management

### Test Data Generators (⚠️ NEEDS CREATION)

```csharp
// Example test data generator
public class AtomDataGenerator
{
    public static Atom GenerateAtom(string type = "text")
    {
        return new Atom
        {
            AtomId = Guid.NewGuid(),
            AtomType = type,
            ContentHash = GenerateHash(),
            Payload = GeneratePayload(type),
            CreatedAt = DateTime.UtcNow
        };
    }
    
    public static IEnumerable<Atom> GenerateBatch(int count, string type = "text")
    {
        return Enumerable.Range(0, count).Select(_ => GenerateAtom(type));
    }
}
```

### Test Database Management

```sql
-- Test database setup script (⚠️ NEEDS CREATION)
-- Location: tests/TestAssets/sql/test-database-setup.sql

CREATE DATABASE Hartonomous_Test;
GO

USE Hartonomous_Test;
GO

-- Deploy DACPAC to test database
-- Seed with test data
-- Create test-specific stored procedures
```

---

## Continuous Integration

### CI/CD Pipeline Integration

```yaml
# .github/workflows/tests.yml (⚠️ NEEDS CREATION)
name: Tests

on: [push, pull_request]

jobs:
  test:
    runs-on: ubuntu-latest
    services:
      sqlserver:
        image: mcr.microsoft.com/mssql/server:2025-latest
      neo4j:
        image: neo4j:5.15-community
    
    steps:
      - uses: actions/checkout@v3
      - name: Setup .NET
        uses: actions/setup-dotnet@v3
        with:
          dotnet-version: '10.0.x'
      
      - name: Run Unit Tests
        run: dotnet test tests/Hartonomous.Core.Tests/
      
      - name: Run Integration Tests
        run: dotnet test tests/Hartonomous.IntegrationTests/
      
      - name: Generate Coverage Report
        run: dotnet test /p:CollectCoverage=true
      
      - name: Upload Coverage
        uses: codecov/codecov-action@v3
```

---

## References

- **Test Projects**: `tests/` directory
- **Test Assets**: `tests/Common/TestAssets/`
- **Benchmark Project**: `tests/Hartonomous.Benchmarks/` (exists but minimal)
- **Coverage Goals**: See [Coverage Targets](#coverage-targets-6-months)
- **Performance SLAs**: See [Performance SLAs](#performance-slas)
