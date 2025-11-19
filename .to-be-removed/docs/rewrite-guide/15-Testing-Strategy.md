# 15 - Testing Strategy: Ensuring Correctness at Every Layer

The Hartonomous platform's complexity demands a comprehensive, multi-layered testing strategy. Testing must validate not just functional correctness, but also performance, security, and data integrity across the entire stack. This document defines the testing approach for the rewrite.

## 1. The Testing Pyramid for Hartonomous

The platform requires a modified testing pyramid that accounts for database-first architecture:

```
        E2E Tests
       /          \
     Integration Tests
    /                  \
  Database Tests    Unit Tests
 /                              \
CLR Function Tests    T-SQL Tests
```

### Test Distribution (Target)
- **Unit Tests (C#):** 60% - Fast, isolated tests of business logic
- **CLR Function Tests:** 15% - Testing CLR functions in-memory without SQL Server
- **T-SQL Tests (tSQLt):** 10% - Testing stored procedures and functions
- **Integration Tests:** 10% - Cross-component tests (API → Database → Neo4j)
- **E2E Tests:** 5% - Full user workflow tests

## 2. Unit Testing the C# Layers

### Testing Core (Interfaces and DTOs)
The `Hartonomous.Core` project contains primarily interfaces and data structures, which don't require extensive testing. However, any domain logic or validation should be tested.

### Testing Infrastructure (Repositories and Services)

```csharp
// Example: Testing the AtomRepository
public class AtomRepositoryTests
{
    [Fact]
    public async Task InsertAtomsAsync_Should_HandleDeduplication()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<HartonomousDbContext>()
            .UseInMemoryDatabase(databaseName: "TestDb")
            .Options;

        await using var context = new HartonomousDbContext(options);
        var repository = new AtomRepository(context);

        var atoms = new List<AtomData>
        {
            new() { AtomHash = "abc123", Content = new byte[] { 1, 2, 3 } },
            new() { AtomHash = "abc123", Content = new byte[] { 1, 2, 3 } } // Duplicate
        };

        // Act
        var result = await repository.InsertAtomsAsync(sourceId: 1, atoms, CancellationToken.None);

        // Assert
        result.InsertedCount.Should().Be(1); // Only one inserted due to deduplication
    }
}
```

### Testing Atomizers

```csharp
public class TextAtomizerTests
{
    [Fact]
    public async Task AtomizeAsync_Should_SplitBySentences()
    {
        // Arrange
        var atomizer = new TextAtomizer();
        var text = "First sentence. Second sentence.";
        var rawData = Encoding.UTF8.GetBytes(text);

        // Act
        var atoms = await atomizer.AtomizeAsync(rawData, CancellationToken.None);

        // Assert
        atoms.Should().HaveCount(2);
        atoms[0].ContentType.Should().Be("text/sentence");
    }
}
```

## 3. Testing SQL CLR Functions (In-Memory, No Database)

The CLR functions should be testable independently of SQL Server using standard xUnit tests.

### Testing VectorMath (SIMD Functions)

```csharp
public class VectorMathTests
{
    [Fact]
    public void DotProduct_Should_ReturnCorrectValue()
    {
        // Arrange
        var a = new float[] { 1.0f, 2.0f, 3.0f };
        var b = new float[] { 4.0f, 5.0f, 6.0f };

        // Act
        var result = VectorMath.DotProduct(a, b);

        // Assert
        result.Should().BeApproximately(32.0f, precision: 0.001f); // 1*4 + 2*5 + 3*6 = 32
    }

    [Fact]
    public void DotProduct_Should_UseSIMD_ForLargeVectors()
    {
        // Arrange
        var size = 10000;
        var a = Enumerable.Range(0, size).Select(i => (float)i).ToArray();
        var b = Enumerable.Range(0, size).Select(i => (float)i).ToArray();

        // Act
        var sw = Stopwatch.StartNew();
        var result = VectorMath.DotProduct(a, b);
        sw.Stop();

        // Assert
        sw.ElapsedMilliseconds.Should().BeLessThan(10); // SIMD should be very fast
    }
}
```

### Testing the O(K) Processing Logic

```csharp
public class AttentionGenerationTests
{
    [Fact]
    public void ProcessCandidates_Should_ReturnTopK()
    {
        // Arrange
        var candidates = new List<CandidateAtom>
        {
            new() { AtomId = 1, EmbeddingVector = new float[] { 1, 0, 0 } },
            new() { AtomId = 2, EmbeddingVector = new float[] { 0, 1, 0 } },
            new() { AtomId = 3, EmbeddingVector = new float[] { 0, 0, 1 } }
        };
        var contextVector = new float[] { 1, 0, 0 };
        var k = 2;

        // Act
        var results = AttentionGeneration.ProcessCandidates(
            candidates,
            contextVector,
            k);

        // Assert
        results.Should().HaveCount(2);
        results[0].AtomId.Should().Be(1); // Closest to context
    }
}
```

## 4. Testing T-SQL Stored Procedures (tSQLt Framework)

For T-SQL logic, use the **tSQLt** framework, which allows unit testing of stored procedures directly in SQL Server.

### Installing tSQLt

```sql
-- Download tSQLt from https://tsqlt.org/
-- Run the installation script
:r tSQLt.class.sql
```

### Example: Testing the O(log N) Multi-Stage Query

```sql
EXEC tSQLt.NewTestClass 'AtomEmbeddingTests';
GO

CREATE PROCEDURE AtomEmbeddingTests.[test sp_FindNearestAtoms Returns Correct K Candidates]
AS
BEGIN
    -- Arrange: Create test data
    EXEC tSQLt.FakeTable 'dbo.AtomEmbeddings';

    INSERT INTO dbo.AtomEmbeddings (AtomId, SpatialGeometry, EmbeddingVector)
    VALUES
        (1, geometry::Point(0, 0, 0), CAST('[1,0,0]' AS VECTOR(3))),
        (2, geometry::Point(10, 10, 0), CAST('[0,1,0]' AS VECTOR(3))),
        (3, geometry::Point(1, 1, 0), CAST('[0.9,0.1,0]' AS VECTOR(3)));

    -- Act: Execute the stored procedure
    DECLARE @search_area geometry = geometry::Point(0, 0, 0).STBuffer(5);
    DECLARE @context_vector VECTOR(3) = CAST('[1,0,0]' AS VECTOR(3));
    DECLARE @k INT = 2;

    DECLARE @results TABLE (AtomId BIGINT, Score FLOAT);
    INSERT INTO @results
    EXEC dbo.sp_FindNearestAtoms @search_area, @context_vector, @k;

    -- Assert: Verify results
    EXEC tSQLt.AssertEquals 2, (SELECT COUNT(*) FROM @results);
    EXEC tSQLt.AssertEquals 1, (SELECT TOP 1 AtomId FROM @results ORDER BY Score);
END;
GO

-- Run all tests
EXEC tSQLt.Run 'AtomEmbeddingTests';
```

### Testing CLR Functions Called from T-SQL

```sql
CREATE PROCEDURE AtomEmbeddingTests.[test fn_VectorDotProduct Calculates Correctly]
AS
BEGIN
    -- Arrange
    DECLARE @vectorA VARBINARY(MAX) = CAST('[1,2,3]' AS VARBINARY(MAX));
    DECLARE @vectorB VARBINARY(MAX) = CAST('[4,5,6]' AS VARBINARY(MAX));

    -- Act
    DECLARE @result FLOAT = dbo.fn_VectorDotProduct(@vectorA, @vectorB);

    -- Assert
    EXEC tSQLt.AssertEquals 32.0, @result;
END;
GO
```

## 5. Integration Testing (Cross-Component)

Integration tests validate the interaction between multiple components, particularly the API, database, and Neo4j.

### Testing the Full Ingestion Pipeline

```csharp
public class IngestionIntegrationTests : IClassFixture<DatabaseFixture>
{
    private readonly DatabaseFixture _fixture;

    public IngestionIntegrationTests(DatabaseFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task AtomizeText_Should_CreateAtomsInDatabaseAndNeo4j()
    {
        // Arrange
        var pipeline = _fixture.Services.GetRequiredService<IAtomIngestionPipeline>();
        var neo4jDriver = _fixture.Services.GetRequiredService<IDriver>();
        var text = "This is a test. This should create atoms.";
        var rawData = Encoding.UTF8.GetBytes(text);

        // Act
        var result = await pipeline.AtomizeAsync(
            sourceId: 999,
            contentType: "text/plain",
            rawData,
            CancellationToken.None);

        // Wait for Neo4j sync (eventual consistency)
        await Task.Delay(TimeSpan.FromSeconds(2));

        // Assert: Check SQL Server
        await using var sqlConnection = new SqlConnection(_fixture.ConnectionString);
        await sqlConnection.OpenAsync();
        var atomCount = await sqlConnection.ExecuteScalarAsync<int>(
            "SELECT COUNT(*) FROM dbo.Atoms WHERE SourceId = 999");
        atomCount.Should().BeGreaterThan(0);

        // Assert: Check Neo4j
        await using var session = neo4jDriver.AsyncSession();
        var neo4jCount = await session.ExecuteReadAsync(async tx =>
        {
            var cursor = await tx.RunAsync(
                "MATCH (a:Atom)-[:INGESTED_FROM]->(s:Source {sourceId: $sourceId}) RETURN COUNT(a)",
                new { sourceId = 999 });
            var record = await cursor.SingleAsync();
            return record[0].As<int>();
        });
        neo4jCount.Should().Be(atomCount);
    }
}
```

### Database Fixture for Integration Tests

```csharp
public class DatabaseFixture : IAsyncLifetime
{
    public string ConnectionString { get; private set; }
    public IServiceProvider Services { get; private set; }

    public async Task InitializeAsync()
    {
        // Use Testcontainers to spin up SQL Server and Neo4j
        var sqlContainer = new MsSqlBuilder()
            .WithImage("mcr.microsoft.com/mssql/server:2025-latest")
            .Build();

        await sqlContainer.StartAsync();

        ConnectionString = sqlContainer.GetConnectionString();

        // Deploy the DACPAC to the test database
        var dacpac = new DacPackage("Hartonomous.Database.dacpac");
        var dacServices = new DacServices(ConnectionString);
        dacServices.Deploy(dacpac, "Hartonomous", upgradeExisting: true);

        // Set up DI container
        var services = new ServiceCollection();
        services.AddDbContext<HartonomousDbContext>(options =>
            options.UseSqlServer(ConnectionString));
        // ... register other services
        Services = services.BuildServiceProvider();
    }

    public async Task DisposeAsync()
    {
        // Cleanup handled by Testcontainers
    }
}
```

## 6. End-to-End Testing (User Workflows)

E2E tests validate complete user workflows, typically through the API.

### Testing a Complete Inference Flow

```csharp
public class InferenceE2ETests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;

    public InferenceE2ETests(WebApplicationFactory<Program> factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task CompleteInferenceFlow_Should_ReturnExplainableResults()
    {
        // Step 1: Upload a source document
        var uploadResponse = await _client.PostAsync("/api/sources", new MultipartFormDataContent
        {
            { new StringContent("The quick brown fox"), "content" },
            { new StringContent("text/plain"), "contentType" }
        });
        uploadResponse.EnsureSuccessStatusCode();
        var sourceId = await uploadResponse.Content.ReadFromJsonAsync<int>();

        // Step 2: Wait for atomization and embedding generation
        await Task.Delay(TimeSpan.FromSeconds(5));

        // Step 3: Run inference
        var inferenceRequest = new
        {
            query = "Tell me about animals",
            k = 10
        };
        var inferenceResponse = await _client.PostAsJsonAsync("/api/inference", inferenceRequest);
        inferenceResponse.EnsureSuccessStatusCode();
        var result = await inferenceResponse.Content.ReadFromJsonAsync<InferenceResult>();

        // Step 4: Verify explainability (provenance)
        var provenanceResponse = await _client.GetAsync($"/api/inference/{result.InferenceId}/provenance");
        provenanceResponse.EnsureSuccessStatusCode();
        var provenance = await provenanceResponse.Content.ReadFromJsonAsync<ProvenanceGraph>();

        // Assert: The result should trace back to our uploaded source
        provenance.Sources.Should().Contain(s => s.SourceId == sourceId);
    }
}
```

## 7. Performance Testing

Performance tests validate that the O(log N) + O(K) model achieves the expected scalability.

### Benchmarking the Multi-Stage Query

```csharp
[MemoryDiagnoser]
public class MultiStageQueryBenchmark
{
    private SqlConnection _connection;

    [GlobalSetup]
    public async Task Setup()
    {
        _connection = new SqlConnection(/* connection string */);
        await _connection.OpenAsync();

        // Seed database with test data (1M atoms)
        // ...
    }

    [Benchmark]
    public async Task MultiStageQuery_1M_Atoms()
    {
        var command = new SqlCommand("dbo.sp_FindNearestAtoms", _connection)
        {
            CommandType = CommandType.StoredProcedure
        };
        command.Parameters.AddWithValue("@search_area", /* geometry */);
        command.Parameters.AddWithValue("@context_vector", /* vector */);
        command.Parameters.AddWithValue("@k", 10);

        await using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            // Consume results
        }
    }
}
```

### Expected Performance Targets

- **Multi-Stage Query (1M atoms):** < 50ms
- **Atomization (1KB text):** < 10ms
- **Embedding Generation (single atom):** < 100ms (local model)
- **Neo4j Provenance Query (5 hops):** < 20ms

## 8. Security Testing

### SQL Injection Testing

```csharp
[Theory]
[InlineData("'; DROP TABLE Atoms; --")]
[InlineData("1' OR '1'='1")]
public async Task API_Should_PreventSQLInjection(string maliciousInput)
{
    var response = await _client.GetAsync($"/api/atoms?search={maliciousInput}");

    // Should either return 400 (validation error) or safely escape the input
    response.StatusCode.Should().NotBe(HttpStatusCode.InternalServerError);

    // Verify the database is intact
    var atomCount = await _dbContext.Atoms.CountAsync();
    atomCount.Should().BeGreaterThan(0);
}
```

### CLR Permission Testing

```sql
-- Verify that the CLR assembly cannot access unauthorized resources
CREATE PROCEDURE SecurityTests.[test CLR Cannot Access FileSystem In SAFE Mode]
AS
BEGIN
    DECLARE @result NVARCHAR(MAX);

    BEGIN TRY
        -- Attempt to call a hypothetical file-reading CLR function
        EXEC @result = dbo.fn_ReadFile 'C:\Windows\System32\config\SAM';
        EXEC tSQLt.Fail 'CLR function should not have access to file system';
    END TRY
    BEGIN CATCH
        -- Expected: Permission denied
        EXEC tSQLt.AssertLike '%permission%', ERROR_MESSAGE();
    END CATCH;
END;
```

## 9. Continuous Testing in CI/CD

All tests should run automatically on every commit.

```yaml
# .github/workflows/ci.yml
name: CI

on: [push, pull_request]

jobs:
  test:
    runs-on: windows-latest
    steps:
      - uses: actions/checkout@v3

      - name: Setup .NET
        uses: actions/setup-dotnet@v3
        with:
          dotnet-version: 8.0.x

      - name: Start SQL Server
        run: |
          docker run -e "ACCEPT_EULA=Y" -e "SA_PASSWORD=YourStrong@Passw0rd" \
            -p 1433:1433 -d mcr.microsoft.com/mssql/server:2025-latest

      - name: Start Neo4j
        run: |
          docker run -e NEO4J_AUTH=none -p 7687:7687 -d neo4j:latest

      - name: Run Unit Tests
        run: dotnet test tests/Hartonomous.Core.Tests

      - name: Run Integration Tests
        run: dotnet test tests/Hartonomous.Integration.Tests

      - name: Deploy DACPAC and Run T-SQL Tests
        run: |
          sqlpackage /Action:Publish /SourceFile:src/Hartonomous.Database/bin/Debug/Hartonomous.Database.dacpac /TargetConnectionString:"Server=localhost;..."
          sqlcmd -S localhost -U sa -P "YourStrong@Passw0rd" -d Hartonomous -i tests/tsqlt/RunAllTests.sql
```

This comprehensive testing strategy ensures the Hartonomous platform is reliable, performant, and secure at every layer.
