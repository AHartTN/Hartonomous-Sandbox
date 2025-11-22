# Comprehensive Test Strategy for Hartonomous

**Date**: January 2025  
**Status**: Planning  
**Vision**: Complete test coverage across all layers and all modalities

---

## Executive Summary

This document defines a **complete, systematic test strategy** for the Hartonomous Cognitive Database Platform. The goal is to test **every service, every atomizer, every API endpoint, every CLR function, and every stored procedure** to ensure the system works as designed.

### Test Coverage Goals

| Layer | Current | Target | Priority |
|-------|---------|--------|----------|
| **Unit Tests** | 89% | 95% | HIGH |
| **Integration Tests** | 30% | 90% | HIGH |
| **Database Tests** | 0% | 80% | HIGH |
| **End-to-End Tests** | 0% | 70% | MEDIUM |
| **CLR Function Tests** | 0% | 100% | CRITICAL |
| **Atomizer Tests** | Partial | 100% | CRITICAL |

---

## Test Pyramid Structure

```
                 E2E Tests (Playwright)
                      /     \
                     /       \
            Integration Tests (WebApplicationFactory)
                  /             \
                 /               \
        Unit Tests          Database Tests
       (In-Memory)          (Testcontainers)
```

---

## 1. Unit Tests (Fast - No External Dependencies)

### 1.1 Core Services

#### IIngestionService Tests
**File**: `tests/Hartonomous.UnitTests/Tests/Infrastructure/Services/IngestionServiceTests.cs`

Test scenarios:
- ? Null/empty file data validation
- ? Null/empty filename validation  
- ? Invalid tenant ID validation
- ? **NEW**: Successful file ingestion with text file
- ? **NEW**: Successful file ingestion with image file
- ? **NEW**: File type detection works correctly
- ? **NEW**: Atomizer selection by priority
- ? **NEW**: Embedding job creation after ingestion
- ? **NEW**: URL ingestion downloads and processes
- ? **NEW**: URL validation (HTTP/HTTPS only)
- ? **NEW**: Database ingestion throws NotImplementedException

```csharp
[Fact]
public async Task IngestFileAsync_TextFile_CreatesAtomsAndEmbeddingJobs()
{
    // Arrange
    var context = CreateInMemoryContext();
    var fileTypeDetector = CreateMockFileTypeDetector("text/plain");
    var atomizer = CreateMockAtomizer(atomCount: 5);
    var backgroundJobService = CreateMockBackgroundJobService();
    
    var service = new IngestionService(
        context, 
        fileTypeDetector, 
        new[] { atomizer }, 
        backgroundJobService, 
        logger);
    
    var fileData = Encoding.UTF8.GetBytes("This is a test file");
    
    // Act
    var result = await service.IngestFileAsync(fileData, "test.txt", tenantId: 1);
    
    // Assert
    result.Success.Should().BeTrue();
    result.ItemsProcessed.Should().Be(5);
    
    // Verify atoms were saved
    context.Atoms.Count().Should().Be(5);
    
    // Verify embedding jobs were created
    backgroundJobService.Verify(x => x.CreateJobAsync(
        "GenerateEmbedding",
        It.IsAny<string>(),
        1,
        It.IsAny<CancellationToken>()
    ), Times.Exactly(5));
}
```

#### IFileTypeDetector Tests
**File**: `tests/Hartonomous.UnitTests/Tests/Infrastructure/FileTypeDetectorTests.cs`

Test scenarios:
- ? PNG detection via magic bytes
- ? JPEG detection via magic bytes
- ? PDF detection via magic bytes
- ? ZIP/DOCX detection
- ? GGUF model detection
- ? SafeTensors model detection
- ? Text fallback detection
- ? Extension-based fallback
- ? Unknown binary detection

#### IProvenanceQueryService Tests
**File**: `tests/Hartonomous.UnitTests/Tests/Core/ProvenanceQueryServiceTests.cs`

Test scenarios:
- ? Get atom lineage (mock data)
- ? Find error clusters
- ? Get session paths
- ? Get session errors
- ? Get influences

#### IReasoningService Tests
**File**: `tests/Hartonomous.UnitTests/Tests/Core/ReasoningServiceTests.cs`

Test scenarios:
- ? Chain-of-Thought execution
- ? Tree-of-Thought execution
- ? Session history retrieval
- ? Confidence scoring

#### IBackgroundJobService Tests
**File**: `tests/Hartonomous.UnitTests/Tests/Infrastructure/Services/BackgroundJobServiceTests.cs`

Test scenarios:
- ? Create job returns valid GUID
- ? Get job retrieves correct job
- ? Update job status
- ? List jobs by tenant
- ? List jobs by status
- ? Get pending jobs for worker

### 1.2 Atomizer Tests

#### Base Atomizer Tests
**File**: `tests/Hartonomous.UnitTests/Tests/Infrastructure/Atomizers/BaseAtomizerTests.cs`

Test scenarios:
- ? File metadata atom creation
- ? Content atom creation (< 64 bytes)
- ? Content atom creation (> 64 bytes with overflow)
- ? Composition creation with spatial coordinates
- ? Fingerprint computation
- ? JSON metadata merging

#### Text Atomizers
**Files**: 
- `TextAtomizerTests.cs`
- `MarkdownAtomizerTests.cs`
- `PdfAtomizerTests.cs`

Test scenarios per atomizer:
- ? Simple text chunking
- ? Sentence boundary detection
- ? Token counting
- ? Composition relationships
- ? Modality/subtype assignment

#### Image Atomizers
**Files**:
- `ImageAtomizerTests.cs` 
- `PngAtomizerTests.cs`
- `JpegAtomizerTests.cs`

Test scenarios:
- ? Pixel block atomization
- ? OCR text extraction
- ? Object detection atoms
- ? Scene analysis atoms
- ? Spatial positioning

#### Video Atomizers
**File**: `VideoAtomizerTests.cs`

Test scenarios:
- ? Frame extraction
- ? Shot detection
- ? Audio track extraction
- ? Composition hierarchy

#### Code Atomizers
**File**: `CodeAtomizerTests.cs`

Test scenarios:
- ? Function extraction
- ? Class extraction
- ? Dependency detection
- ? AST parsing

#### Model Atomizers
**Files**:
- `GgufAtomizerTests.cs`
- `SafeTensorsAtomizerTests.cs`
- `OnnxAtomizerTests.cs`

Test scenarios:
- ? Tensor extraction
- ? Weight chunking
- ? Metadata extraction
- ? Layer composition

### 1.3 API Controller Tests

#### DataIngestionController Tests
**File**: `tests/Hartonomous.UnitTests/Tests/Api/Controllers/DataIngestionControllerTests.cs`

Test scenarios:
- ? POST /api/v1/ingestion/file - success
- ? POST /api/v1/ingestion/file - empty file returns 400
- ? POST /api/v1/ingestion/file - null file returns 400
- ? POST /api/v1/ingestion/file - invalid tenant returns 400
- ? POST /api/v1/ingestion/url - success
- ? POST /api/v1/ingestion/url - invalid URL returns 400
- ? POST /api/v1/ingestion/database - returns 501
- ? GET /api/v1/ingestion/jobs/{jobId} - success
- ? GET /api/v1/ingestion/jobs/{jobId} - not found returns 404

---

## 2. Integration Tests (Medium Speed - With Real Infrastructure)

### 2.1 API Integration Tests

**File**: `tests/Hartonomous.IntegrationTests/Tests/Api/DataIngestionIntegrationTests.cs`

Test scenarios:
- ? POST file ingestion end-to-end
- ? File validation middleware
- ? **NEW**: File ingestion creates atoms in database
- ? **NEW**: File ingestion creates embedding jobs
- ? **NEW**: Multi-file concurrent upload
- ? **NEW**: Large file handling (streaming)
- ? **NEW**: Rate limiting enforcement

### 2.2 Service Integration Tests

**File**: `tests/Hartonomous.IntegrationTests/Tests/Services/IngestionServiceIntegrationTests.cs`

Test scenarios:
- ? End-to-end ingestion with real DbContext
- ? Stored procedure execution (sp_IngestAtoms)
- ? Background job creation and retrieval
- ? Atomizer pipeline execution
- ? Transaction rollback on error

### 2.3 Worker Integration Tests

**File**: `tests/Hartonomous.IntegrationTests/Tests/Workers/EmbeddingGeneratorIntegrationTests.cs`

Test scenarios:
- ? Worker polls pending jobs
- ? Worker generates embeddings
- ? Worker updates job status
- ? Worker handles errors gracefully
- ? Worker respects cancellation

---

## 3. Database Tests (SQL Server with Testcontainers)

### 3.1 Stored Procedure Tests

**File**: `tests/Hartonomous.DatabaseTests/Tests/StoredProcedures/SpIngestAtomsTests.cs`

Test scenarios:
- ? sp_IngestAtoms creates atoms
- ? sp_IngestAtoms handles duplicates (deduplication)
- ? sp_IngestAtoms returns batch ID
- ? sp_IngestAtoms triggers Service Broker messages

**File**: `tests/Hartonomous.DatabaseTests/Tests/StoredProcedures/SpProjectTo3DTests.cs`

Test scenarios:
- ? sp_ProjectTo3D projects 1536D ? 3D
- ? sp_ProjectTo3D uses landmark trilateration
- ? sp_ProjectTo3D returns GEOMETRY point
- ? sp_ProjectTo3D handles null embeddings

### 3.2 CLR Function Tests

**File**: `tests/Hartonomous.DatabaseTests/Tests/ClrFunctions/ClrVectorOperationsTests.cs`

Test scenarios:
- ? clr_CosineSimilarity computes correctly
- ? clr_EuclideanDistance computes correctly
- ? clr_DotProduct computes correctly
- ? clr_NormalizeVector normalizes to unit length
- ? clr_ComputeHilbertValue maps coordinates

**File**: `tests/Hartonomous.DatabaseTests/Tests/ClrFunctions/ClrSpatialTests.cs`

Test scenarios:
- ? fn_ProjectTo3D projects embedding
- ? fn_ComputeSpatialBucket computes bucket
- ? fn_ParseFloat16Array parses FP16 data
- ? fn_ParseBFloat16Array parses BF16 data

### 3.3 Service Broker Tests

**File**: `tests/Hartonomous.DatabaseTests/Tests/ServiceBroker/ServiceBrokerTests.cs`

Test scenarios:
- ? IngestionQueue receives messages
- ? EmbeddingQueue receives messages
- ? Neo4jSyncQueue receives messages
- ? Message activation procedures execute

### 3.4 Spatial Index Tests

**File**: `tests/Hartonomous.DatabaseTests/Tests/SpatialIndices/SpatialIndexPerformanceTests.cs`

Test scenarios:
- ? R-tree index used for KNN queries
- ? Spatial bucket partitioning works
- ? Hilbert curve ordering optimization
- ? STDistance performance benchmarks

---

## 4. End-to-End Tests (Playwright)

### 4.1 Full Ingestion Pipeline

**File**: `tests/Hartonomous.EndToEndTests/Tests/IngestionPipelineE2ETests.cs`

Test scenarios:
- ? Upload file ? Detect type ? Atomize ? Save ? Generate embeddings ? Query
- ? Cross-modal search (text query ? find related images)
- ? Spatial reasoning (A* pathfinding between atoms)
- ? Provenance tracking (lineage query)

### 4.2 OODA Loop Tests

**File**: `tests/Hartonomous.EndToEndTests/Tests/OodaLoopE2ETests.cs`

Test scenarios:
- ? Observe slow query
- ? Orient (analyze execution plan)
- ? Decide (create index hypothesis)
- ? Act (execute hypothesis)
- ? Learn (measure improvement)

---

## 5. Test Infrastructure Patterns

### 5.1 Test Fixtures

#### SqlServerTestFixture
**Purpose**: Provides real SQL Server via Testcontainers

```csharp
public class SqlServerTestFixture : IAsyncLifetime
{
    private MsSqlContainer? _container;
    public string ConnectionString { get; private set; }
    
    public async Task InitializeAsync()
    {
        _container = new MsSqlBuilder()
            .WithImage("mcr.microsoft.com/mssql/server:2022-latest")
            .Build();
        await _container.StartAsync();
        ConnectionString = _container.GetConnectionString();
        
        // Deploy schema
        await DeploySchemaAsync();
        
        // Deploy CLR assemblies
        await DeployClrAsync();
    }
    
    public async Task DisposeAsync()
    {
        if (_container != null)
            await _container.DisposeAsync();
    }
}
```

#### InMemoryDbContextFixture
**Purpose**: Fast in-memory database for unit tests

```csharp
public class InMemoryDbContextFixture
{
    public HartonomousDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<HartonomousDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new HartonomousDbContext(options);
    }
}
```

### 5.2 Mock Builders

#### MockAtomizerBuilder
**Purpose**: Fluent builder for test atomizers

```csharp
public class MockAtomizerBuilder
{
    private int _atomCount = 1;
    private string _modality = "text";
    
    public MockAtomizerBuilder WithAtomCount(int count)
    {
        _atomCount = count;
        return this;
    }
    
    public MockAtomizerBuilder WithModality(string modality)
    {
        _modality = modality;
        return this;
    }
    
    public IAtomizer<byte[]> Build()
    {
        var mock = new Mock<IAtomizer<byte[]>>();
        
        mock.Setup(x => x.CanHandle(It.IsAny<string>(), It.IsAny<string>()))
            .Returns(true);
        
        mock.Setup(x => x.AtomizeAsync(
                It.IsAny<byte[]>(), 
                It.IsAny<SourceMetadata>(), 
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AtomizationResult
            {
                Atoms = Enumerable.Range(0, _atomCount)
                    .Select(i => CreateAtomData(i, _modality))
                    .ToList(),
                ProcessingInfo = new ProcessingMetadata
                {
                    TotalAtoms = _atomCount,
                    UniqueAtoms = _atomCount,
                    AtomizerType = "Mock"
                }
            });
        
        return mock.Object;
    }
}
```

### 5.3 Test Data Builders

#### TestFileBuilder
**Purpose**: Creates test file data

```csharp
public class TestFileBuilder
{
    private byte[]? _content;
    private string _fileName = "test.txt";
    
    public TestFileBuilder WithTextContent(string text)
    {
        _content = Encoding.UTF8.GetBytes(text);
        return this;
    }
    
    public TestFileBuilder WithPngHeader()
    {
        _content = new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A };
        _fileName = "test.png";
        return this;
    }
    
    public TestFileBuilder WithFileName(string name)
    {
        _fileName = name;
        return this;
    }
    
    public (byte[] content, string fileName) Build()
    {
        return (_content ?? Array.Empty<byte>(), _fileName);
    }
}
```

---

## 6. Test Execution Strategy

### 6.1 Local Development

```bash
# Fast feedback loop (< 10 seconds)
dotnet test tests/Hartonomous.UnitTests

# Medium feedback loop (< 2 minutes)
dotnet test tests/Hartonomous.IntegrationTests

# Full test suite (< 5 minutes)
dotnet test
```

### 6.2 CI/CD Pipeline

```yaml
# .github/workflows/tests.yml
name: Tests

on: [push, pull_request]

jobs:
  unit-tests:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v3
      - uses: actions/setup-dotnet@v3
        with:
          dotnet-version: '10.0.x'
      - run: dotnet test tests/Hartonomous.UnitTests --logger trx
      - run: dotnet test tests/Hartonomous.IntegrationTests --logger trx
  
  database-tests:
    runs-on: ubuntu-latest
    services:
      sqlserver:
        image: mcr.microsoft.com/mssql/server:2022-latest
        env:
          ACCEPT_EULA: Y
          SA_PASSWORD: YourStrong@Password123
    steps:
      - uses: actions/checkout@v3
      - uses: actions/setup-dotnet@v3
      - run: dotnet test tests/Hartonomous.DatabaseTests --logger trx
  
  e2e-tests:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v3
      - uses: actions/setup-dotnet@v3
      - run: npx playwright install
      - run: dotnet test tests/Hartonomous.EndToEndTests --logger trx
```

---

## 7. Test Coverage Measurement

### 7.1 Code Coverage Tools

```bash
# Generate coverage report
dotnet test /p:CollectCoverage=true /p:CoverletOutputFormat=cobertura

# View coverage in browser
reportgenerator -reports:coverage.cobertura.xml -targetdir:coveragereport
```

### 7.2 Coverage Targets

| Project | Target | Current |
|---------|--------|---------|
| Hartonomous.Core | 90% | TBD |
| Hartonomous.Infrastructure | 85% | TBD |
| Hartonomous.Api | 80% | TBD |
| Atomizers | 95% | TBD |

---

## 8. Test Naming Conventions

### Pattern
```
[UnitOfWork]_[Scenario]_[ExpectedBehavior]
```

### Examples
```csharp
IngestFileAsync_ValidTextFile_ReturnsSuccess
IngestFileAsync_NullFileData_ThrowsArgumentNullException
IngestFileAsync_EmptyFileName_ThrowsArgumentException
AtomizeAsync_LargeImage_CreatesPixelBlocks
ExecuteChainOfThoughtAsync_ValidPrompt_ReturnsReasoningSteps
```

---

## 9. Test Categories

Use `[Trait]` attributes to categorize tests:

```csharp
[Trait("Category", "Unit")]
[Trait("Category", "Fast")]
public class FastUnitTests { }

[Trait("Category", "Integration")]
[Trait("Category", "Slow")]
public class SlowIntegrationTests { }

[Trait("Category", "Database")]
[Trait("Category", "RequiresDocker")]
public class DatabaseTests { }
```

Run specific categories:
```bash
dotnet test --filter "Category=Unit"
dotnet test --filter "Category=Integration"
dotnet test --filter "Category!=RequiresDocker"
```

---

## 10. Implementation Roadmap

### Phase 1: Core Unit Tests (Week 1)
- [ ] IngestionService full coverage
- [ ] FileTypeDetector full coverage
- [ ] BackgroundJobService full coverage
- [ ] BaseAtomizer coverage

### Phase 2: Atomizer Tests (Week 2)
- [ ] Text atomizers
- [ ] Image atomizers
- [ ] Video atomizers
- [ ] Code atomizers
- [ ] Model atomizers

### Phase 3: Integration Tests (Week 3)
- [ ] API integration tests
- [ ] Service integration tests
- [ ] Worker integration tests

### Phase 4: Database Tests (Week 4)
- [ ] Stored procedure tests
- [ ] CLR function tests
- [ ] Service Broker tests
- [ ] Spatial index tests

### Phase 5: E2E Tests (Week 5)
- [ ] Full ingestion pipeline
- [ ] OODA loop tests
- [ ] Cross-modal search
- [ ] Performance benchmarks

---

## 11. Success Metrics

- ? All unit tests pass
- ? All integration tests pass
- ? 95% code coverage on core services
- ? 85% code coverage on infrastructure
- ? 100% coverage on atomizers
- ? 100% coverage on CLR functions
- ? CI/CD pipeline green
- ? Test execution time < 5 minutes

---

## 12. Related Documents

- `tests/README.md` - Current test status
- `docs/implementation/testing.md` - Implementation guide
- `docs/architecture/semantic-first.md` - Architecture overview

---

*This is a living document. Update as test strategy evolves.*
