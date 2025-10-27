# Hartonomous Execution Plan - Revised Strategy

**Date:** October 27, 2025  
**Based on:** User direction - EF-first, unified patterns, real-world tests  
**Estimated Effort:** 10-15 hours total

---

## Strategic Direction

### Core Principles

1. **EF Core is the standard** - Only deviate when performance difference is painfully obvious
2. **Unified DI/interfaces/generics** - Thin clients injecting services, no mixed patterns
3. **Extensible ingestion** - ONNX/Safetensors are starting points, not the end goal
4. **Real-world tests** - Prove actual value, not academic "code matches spec" nonsense
5. **Defer CDC/audit** - CesConsumer is for change control tracking, focus on that last

### What "Painfully Obvious" Means for ADO.NET

Keep ADO.NET **ONLY** for:
- ✅ **SqlVector<float> bulk inserts** - `AddWithValue` pattern is measurably faster for batch operations
- ✅ **Stored procedure params** - When EF `FromSqlRaw` doesn't support the parameter type well

Everything else: **Use EF repositories with proper DI.**

---

## Architecture Vision

### Model Ingestion (Extensible)

```
IModelFormatReader<TMetadata>              [Generic interface]
  ├── OnnxModelReader                      [ONNX Runtime - starting point]
  ├── SafetensorsModelReader               [Safetensors format - starting point]
  ├── PyTorchModelReader                   [Future: .pt files]
  ├── TensorFlowModelReader                [Future: SavedModel format]
  └── HuggingFaceModelReader               [Future: Transformers library integration]

Output: Hartonomous.Core.Entities.Model (NOT DTO)
Persistence: IModelRepository (EF Core)
```

### Embedding Ingestion (Service-based)

```
IEmbeddingIngestionService                 [Interface for DI]
  └── EmbeddingIngestionService            [Implementation using IEmbeddingRepository]
      ├── Deduplication (SHA256 + cosine)
      ├── Spatial projection (3D)
      └── Bulk insert (SqlVector AddWithValue - performance justified)

IAtomicStorageService                      [Interface for DI]
  └── AtomicStorageService                 [Implementation using repositories]
      └── Atomic component storage (pixels, audio samples, tokens)
```

### Repository Layer (EF Core)

```
IModelRepository                           [Models + ModelLayers + weights]
IEmbeddingRepository                       [Embeddings + dedup + search]
IInferenceRepository                       [InferenceRequests + InferenceSteps]
IAtomicComponentRepository                 [Future: Atomic storage if needed]
```

---

## Phase 1: Structure and Cleanup (1-2 hours)

### Goals
- Create test project structure
- Move tool files out of production code
- Clean workspace

### Tasks

**1.1 Create Test Projects**
```
tests/
├── Hartonomous.Core.Tests/              [Unit tests for entities]
├── Hartonomous.Infrastructure.Tests/    [Unit tests for repositories]
├── ModelIngestion.Tests/                [Unit tests for services]
└── Integration.Tests/                   [Real-world integration tests]
```

**1.2 Move Tool Files (NOT a project, just directory)**
```
Move from src/ModelIngestion/ to tools/:
- create_and_save_model.py
- parse_onnx.py
- model.onnx
- model.safetensors
- ssd_mobilenet_v2_coco_2018_03_29/ (entire directory)
- TestSqlVector.cs → tests/ModelIngestion.Tests/

Result: ModelIngestion/ goes from 16 files to ~8 files
```

**1.3 Update .gitignore**
```gitignore
# Tool artifacts
tools/**/*.onnx
tools/**/*.safetensors
tools/**/*.pt
tools/**/*.pb
tools/**/model_cache/
```

### Verification
- [ ] `dotnet sln list` shows 7 production + 4 test projects
- [ ] `dotnet build Hartonomous.sln` succeeds
- [ ] ModelIngestion has ~8 files (down from 16)
- [ ] tools/ directory exists with Python scripts and models

---

## Phase 2: Refactor Model Readers (2-3 hours)

### Goals
- Extensible `IModelFormatReader<TMetadata>` generic interface
- Readers output `Hartonomous.Core.Entities.Model` (NOT DTO)
- Delete legacy `Model.cs` DTO
- Use `IModelRepository` for persistence

### Tasks

**2.1 Create Generic Interface**
```csharp
// src/Hartonomous.Core/Interfaces/IModelFormatReader.cs
namespace Hartonomous.Core.Interfaces;

/// <summary>
/// Generic interface for reading models from various formats.
/// Extensible to support ONNX, Safetensors, PyTorch, TensorFlow, etc.
/// </summary>
/// <typeparam name="TMetadata">Format-specific metadata type</typeparam>
public interface IModelFormatReader<TMetadata> where TMetadata : class
{
    /// <summary>
    /// Read model from file and return Core entity (NOT DTO)
    /// </summary>
    Task<Model> ReadAsync(string modelPath, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Extract format-specific metadata (graph info, tensor shapes, etc.)
    /// </summary>
    Task<TMetadata> GetMetadataAsync(string modelPath, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Supported file extensions for this format
    /// </summary>
    IEnumerable<string> SupportedExtensions { get; }
}
```

**2.2 Rewrite OnnxModelReader**
```csharp
// src/ModelIngestion/Readers/OnnxModelReader.cs
public class OnnxModelReader : IModelFormatReader<OnnxMetadata>
{
    public async Task<Model> ReadAsync(string modelPath, CancellationToken ct = default)
    {
        using var session = new InferenceSession(modelPath);
        
        var model = new Model  // Core entity, NOT DTO
        {
            Name = session.ModelMetadata.GraphName ?? Path.GetFileName(modelPath),
            ModelType = "ONNX",
            Version = session.ModelMetadata.Version.ToString(),
            Config = JsonSerializer.Serialize(new {
                Producer = session.ModelMetadata.ProducerName,
                Domain = session.ModelMetadata.Domain,
                Description = session.ModelMetadata.Description
            }),
            Layers = new List<ModelLayer>()
        };
        
        // Parse layers from ONNX graph
        int layerIndex = 0;
        foreach (var input in session.InputMetadata)
        {
            var layer = new ModelLayer
            {
                LayerIndex = layerIndex++,
                LayerName = input.Key,
                LayerType = "Input",
                Parameters = JsonSerializer.Serialize(new {
                    Shape = input.Value.Dimensions,
                    ElementType = input.Value.ElementType.ToString()
                })
            };
            model.Layers.Add(layer);
        }
        
        // TODO: Parse actual computational layers from ONNX graph
        // This requires deeper ONNX graph traversal
        
        return model;  // Returns Core entity ready for IModelRepository
    }
    
    public IEnumerable<string> SupportedExtensions => new[] { ".onnx" };
}
```

**2.3 Rewrite SafetensorsModelReader**
```csharp
// src/ModelIngestion/Readers/SafetensorsModelReader.cs
public class SafetensorsModelReader : IModelFormatReader<SafetensorsMetadata>
{
    public async Task<Model> ReadAsync(string modelPath, CancellationToken ct = default)
    {
        // Similar pattern - output Core entity with ModelLayers
        // Parse safetensors format, create Model + ModelLayer entities
        // Store weights in ModelLayer.Weights (VECTOR column)
    }
    
    public IEnumerable<string> SupportedExtensions => new[] { ".safetensors" };
}
```

**2.4 Delete Legacy Files**
- Delete `src/ModelIngestion/Model.cs` (DTO)
- Delete `src/ModelIngestion/ModelRepository.cs` (duplicate)
- Delete `src/ModelIngestion/ProductionModelRepository.cs` (duplicate)
- Update `ModelReaderFactory` to use `IModelFormatReader<>`

**2.5 Register in DI**
```csharp
// ModelIngestion/Program.cs
services.AddScoped<IModelFormatReader<OnnxMetadata>, OnnxModelReader>();
services.AddScoped<IModelFormatReader<SafetensorsMetadata>, SafetensorsModelReader>();
// Future: PyTorchModelReader, TensorFlowModelReader, etc.
```

### Verification
- [ ] `dotnet build` succeeds with no references to DTO Model
- [ ] OnnxModelReader outputs `Hartonomous.Core.Entities.Model`
- [ ] SafetensorsModelReader outputs `Hartonomous.Core.Entities.Model`
- [ ] Legacy Model.cs DTO deleted

---

## Phase 3: Migrate Ingestion Services to EF (3-4 hours)

### Goals
- `IEmbeddingIngestionService` interface for DI
- `IAtomicStorageService` interface for DI
- Use `IEmbeddingRepository` methods
- Keep SqlVector AddWithValue ONLY for bulk inserts (performance justified)

### Tasks

**3.1 Create Service Interfaces**
```csharp
// src/Hartonomous.Core/Interfaces/IEmbeddingIngestionService.cs
public interface IEmbeddingIngestionService
{
    Task<EmbeddingIngestionResult> IngestEmbeddingAsync(
        string sourceText,
        string sourceType,
        float[] embeddingFull,
        float[]? spatial3D = null,
        CancellationToken cancellationToken = default);
    
    Task<IEnumerable<EmbeddingIngestionResult>> IngestBatchAsync(
        IEnumerable<(string sourceText, string sourceType, float[] embedding)> batch,
        CancellationToken cancellationToken = default);
}

// src/Hartonomous.Core/Interfaces/IAtomicStorageService.cs
public interface IAtomicStorageService
{
    Task<long> StoreAtomicPixelsAsync(byte r, byte g, byte b, byte a, CancellationToken ct = default);
    Task<long> StoreAtomicAudioSampleAsync(float amplitude, CancellationToken ct = default);
    Task<long> StoreAtomicTextTokenAsync(string tokenText, int? vocabId = null, CancellationToken ct = default);
}
```

**3.2 Refactor EmbeddingIngestionService**
```csharp
// src/ModelIngestion/Services/EmbeddingIngestionService.cs
public class EmbeddingIngestionService : IEmbeddingIngestionService
{
    private readonly IEmbeddingRepository _embeddingRepository;
    private readonly ILogger<EmbeddingIngestionService> _logger;
    private readonly double _deduplicationThreshold;

    public EmbeddingIngestionService(
        IEmbeddingRepository embeddingRepository,
        ILogger<EmbeddingIngestionService> logger,
        IConfiguration configuration)
    {
        _embeddingRepository = embeddingRepository;
        _logger = logger;
        _deduplicationThreshold = configuration.GetValue<double>("Ingestion:DeduplicationThreshold", 0.95);
    }

    public async Task<EmbeddingIngestionResult> IngestEmbeddingAsync(...)
    {
        // Step 1: Check dedup via repository methods
        var contentHash = ComputeSHA256Hash(sourceText);
        var existingByHash = await _embeddingRepository.CheckDuplicateByHashAsync(contentHash);
        
        if (existingByHash != null)
        {
            await _embeddingRepository.IncrementAccessCountAsync(existingByHash.EmbeddingId);
            return new EmbeddingIngestionResult { WasDuplicate = true };
        }
        
        // Step 2: Check semantic similarity via repository
        var similar = await _embeddingRepository.CheckDuplicateBySimilarityAsync(embeddingFull, _deduplicationThreshold);
        
        if (similar != null)
        {
            await _embeddingRepository.IncrementAccessCountAsync(similar.EmbeddingId);
            return new EmbeddingIngestionResult { WasDuplicate = true };
        }
        
        // Step 3: Compute spatial projection via repository stored proc
        var spatial3D = await _embeddingRepository.ComputeSpatialProjectionAsync(embeddingFull);
        
        // Step 4: Insert via repository
        var embedding = new Embedding
        {
            SourceText = sourceText,
            SourceType = sourceType,
            EmbeddingFull = new SqlVector<float>(embeddingFull),
            SpatialProjection = CreateGeometryPoint(spatial3D),
            ContentHash = contentHash
        };
        
        await _embeddingRepository.AddAsync(embedding);
        return new EmbeddingIngestionResult { EmbeddingId = embedding.EmbeddingId };
    }
    
    public async Task<IEnumerable<EmbeddingIngestionResult>> IngestBatchAsync(...)
    {
        // Use AddRangeAsync for bulk - this is where SqlVector AddWithValue shines
        // EF bulk operations with SqlVector are still efficient
        var embeddings = batch.Select(b => new Embedding { ... }).ToList();
        await _embeddingRepository.AddRangeAsync(embeddings);
    }
}
```

**3.3 Update DI Registration**
```csharp
// ModelIngestion/Program.cs
services.AddScoped<IEmbeddingIngestionService, EmbeddingIngestionService>();
services.AddScoped<IAtomicStorageService, AtomicStorageService>();
```

### Verification
- [ ] Services use `IEmbeddingRepository`, not direct SqlConnection
- [ ] Services registered in DI with interfaces
- [ ] Deduplication uses repository methods
- [ ] Bulk insert still efficient (EF AddRangeAsync with SqlVector)

---

## Phase 4: Extend Repository Methods (2-3 hours)

### Goals
- Add deduplication methods to `IEmbeddingRepository`
- Add layer/weight methods to `IModelRepository`
- Use EF where possible, `FromSqlRaw` for stored procs

### Tasks

**4.1 Extend IEmbeddingRepository**
```csharp
// src/Hartonomous.Infrastructure/Repositories/IEmbeddingRepository.cs
public interface IEmbeddingRepository
{
    // Existing methods...
    Task<Embedding?> GetByIdAsync(long embeddingId, CancellationToken ct = default);
    Task<Embedding> AddAsync(Embedding embedding, CancellationToken ct = default);
    Task<IEnumerable<Embedding>> AddRangeAsync(IEnumerable<Embedding> embeddings, CancellationToken ct = default);
    
    // NEW: Deduplication methods
    Task<Embedding?> CheckDuplicateByHashAsync(string contentHash, CancellationToken ct = default);
    Task<Embedding?> CheckDuplicateBySimilarityAsync(float[] queryVector, double threshold, CancellationToken ct = default);
    Task IncrementAccessCountAsync(long embeddingId, CancellationToken ct = default);
    
    // NEW: Spatial projection via stored proc
    Task<float[]> ComputeSpatialProjectionAsync(float[] fullVector, CancellationToken ct = default);
    
    // Existing search methods...
    Task<IEnumerable<Embedding>> ExactSearchAsync(string queryVector, int topK, CancellationToken ct = default);
    Task<IEnumerable<Embedding>> HybridSearchAsync(...);
}
```

**4.2 Implement in EmbeddingRepository**
```csharp
// src/Hartonomous.Infrastructure/Repositories/EmbeddingRepository.cs
public async Task<Embedding?> CheckDuplicateByHashAsync(string contentHash, CancellationToken ct = default)
{
    return await _context.Embeddings
        .Where(e => e.ContentHash == contentHash)
        .FirstOrDefaultAsync(ct);
}

public async Task<Embedding?> CheckDuplicateBySimilarityAsync(float[] queryVector, double threshold, CancellationToken ct = default)
{
    // Use stored proc via FromSqlRaw
    var results = await _context.Embeddings
        .FromSqlRaw(
            "EXEC dbo.sp_CheckSimilarityAboveThreshold @query_vector = {0}, @threshold = {1}",
            new SqlVector<float>(queryVector), threshold)
        .FirstOrDefaultAsync(ct);
    
    return results;
}

public async Task IncrementAccessCountAsync(long embeddingId, CancellationToken ct = default)
{
    var embedding = await _context.Embeddings.FindAsync(new object[] { embeddingId }, ct);
    if (embedding != null)
    {
        embedding.AccessCount++;
        embedding.LastAccessed = DateTime.UtcNow;
        await _context.SaveChangesAsync(ct);
    }
}

public async Task<float[]> ComputeSpatialProjectionAsync(float[] fullVector, CancellationToken ct = default)
{
    // Call stored proc sp_ComputeSpatialProjection
    using var command = _context.Database.GetDbConnection().CreateCommand();
    command.CommandText = "EXEC dbo.sp_ComputeSpatialProjection @input_vector";
    command.Parameters.Add(new SqlParameter("@input_vector", new SqlVector<float>(fullVector)));
    
    await _context.Database.OpenConnectionAsync(ct);
    using var result = await command.ExecuteReaderAsync(ct);
    
    if (await result.ReadAsync(ct))
    {
        return new float[] { result.GetFloat(0), result.GetFloat(1), result.GetFloat(2) };
    }
    
    throw new InvalidOperationException("Failed to compute spatial projection");
}
```

**4.3 Extend IModelRepository**
```csharp
// src/Hartonomous.Infrastructure/Repositories/IModelRepository.cs
public interface IModelRepository
{
    // Existing...
    Task<Model?> GetByIdAsync(int modelId, CancellationToken ct = default);
    Task<Model> AddAsync(Model model, CancellationToken ct = default);
    
    // NEW: Layer and weight operations
    Task<ModelLayer> AddLayerAsync(int modelId, ModelLayer layer, CancellationToken ct = default);
    Task UpdateLayerWeightsAsync(int layerId, SqlVector<float> weights, CancellationToken ct = default);
    Task<IEnumerable<ModelLayer>> GetLayersByModelIdAsync(int modelId, CancellationToken ct = default);
}
```

### Verification
- [ ] All deduplication operations use repository methods
- [ ] Spatial projection calls stored proc via repository
- [ ] Model layer operations properly abstracted
- [ ] No direct SqlConnection in service layer

---

## Phase 5: Delete Obsolete Files (30 minutes)

### Tasks

**5.1 Delete Obsolete SQL Files**
```powershell
Remove-Item sql/schemas/08_AlterTokenVocabulary.sql
Remove-Item sql/schemas/09_AlterTokenVocabularyVector.sql
Remove-Item sql/schemas/10_FixTokenVocabulary.sql
Remove-Item sql/schemas/11_FixTokenVocabularyPrimaryKey.sql
Remove-Item sql/schemas/12_FixTokenVocabularyTake2.sql
Remove-Item sql/schemas/13_FixTokenVocabularyTake3.sql
Remove-Item sql/schemas/14_FixTokenVocabularyTake4.sql
Remove-Item sql/schemas/17_FixAndSeedTokenVocabulary.sql
Remove-Item sql/schemas/18_FixAndSeedTokenVocabularyTake2.sql
Remove-Item sql/schemas/19_Cleanup.sql
Remove-Item sql/schemas/20_CreateTokenVocabularyWithVector.sql
# Check if 21 applied to entities first!
```

**5.2 Verify No References**
```powershell
# Grep for any remaining references
dotnet build Hartonomous.sln
# Should succeed with 0 errors
```

### Verification
- [ ] 12 obsolete SQL files deleted
- [ ] Legacy Model.cs DTO deleted (from Phase 2)
- [ ] Duplicate repositories deleted (from Phase 2)
- [ ] `dotnet build` succeeds

---

## Phase 6: Write Real-World Tests (3-4 hours)

### Goals
- Integration tests that **prove actual value**
- Use real models, real embeddings, real queries
- Performance benchmarks
- NO "code matches spec" academic tests

### Tasks

**6.1 Integration Test: ONNX Model Ingestion**
```csharp
// tests/Integration.Tests/OnnxIngestionTests.cs
[Fact]
public async Task IngestRealOnnxModel_VerifyDatabaseState()
{
    // Arrange: Real ONNX model from tools/
    var modelPath = Path.Combine(TestContext.ToolsDirectory, "model.onnx");
    var reader = new OnnxModelReader();
    var repository = GetRepository<IModelRepository>();
    
    // Act: Ingest model
    var model = await reader.ReadAsync(modelPath);
    await repository.AddAsync(model);
    
    // Assert: Verify in database
    using var context = GetDbContext();
    var dbModel = await context.Models
        .Include(m => m.Layers)
        .FirstAsync(m => m.Name == model.Name);
    
    Assert.NotNull(dbModel);
    Assert.True(dbModel.Layers.Count > 0);
    
    // Real-world check: Can we retrieve weights?
    var firstLayer = dbModel.Layers.First();
    Assert.NotNull(firstLayer.Weights);
    
    // Real-world check: Are weights the right size?
    // ONNX model has known dimensions - verify actual data
}
```

**6.2 Integration Test: Deduplication with Real Embeddings**
```csharp
// tests/Integration.Tests/DeduplicationTests.cs
[Fact]
public async Task IngestSimilarTexts_VerifyDeduplication()
{
    // Arrange: Real embeddings for semantically similar texts
    var service = GetService<IEmbeddingIngestionService>();
    
    var text1 = "The quick brown fox jumps over the lazy dog";
    var text2 = "A fast brown fox leaps over a lazy dog";  // Paraphrase
    
    var embedding1 = await GenerateEmbedding(text1);  // Real embedding model
    var embedding2 = await GenerateEmbedding(text2);  // Real embedding model
    
    // Act: Ingest both
    var result1 = await service.IngestEmbeddingAsync(text1, "test", embedding1);
    var result2 = await service.IngestEmbeddingAsync(text2, "test", embedding2);
    
    // Assert: Second should be detected as duplicate
    Assert.False(result1.WasDuplicate);
    Assert.True(result2.WasDuplicate);
    
    // Real-world check: Only one row in database
    using var context = GetDbContext();
    var count = await context.Embeddings.CountAsync(e => e.SourceType == "test");
    Assert.Equal(1, count);
    
    // Real-world check: Access count incremented
    var embedding = await context.Embeddings.FirstAsync(e => e.SourceType == "test");
    Assert.Equal(2, embedding.AccessCount);
}
```

**6.3 Integration Test: Hybrid Search Performance**
```csharp
// tests/Integration.Tests/HybridSearchTests.cs
[Fact]
public async Task HybridSearch_MeasurePerformance()
{
    // Arrange: Ingest 10,000 real embeddings
    await SeedDatabase(embeddings: 10_000);
    
    var repository = GetRepository<IEmbeddingRepository>();
    var queryVector = await GenerateEmbedding("test query");
    
    // Act: Measure exact vs hybrid search
    var exactStopwatch = Stopwatch.StartNew();
    var exactResults = await repository.ExactSearchAsync(queryVector, topK: 10);
    exactStopwatch.Stop();
    
    var hybridStopwatch = Stopwatch.StartNew();
    var hybridResults = await repository.HybridSearchAsync(queryVector, ..., topK: 10);
    hybridStopwatch.Stop();
    
    // Assert: Hybrid should be faster
    Assert.True(hybridStopwatch.ElapsedMilliseconds < exactStopwatch.ElapsedMilliseconds);
    
    // Real-world check: Recall should be high (95%+)
    var recall = CalculateRecall(exactResults, hybridResults);
    Assert.True(recall > 0.95, $"Recall too low: {recall:P}");
    
    _output.WriteLine($"Exact search: {exactStopwatch.ElapsedMilliseconds}ms");
    _output.WriteLine($"Hybrid search: {hybridStopwatch.ElapsedMilliseconds}ms");
    _output.WriteLine($"Speedup: {exactStopwatch.ElapsedMilliseconds / (double)hybridStopwatch.ElapsedMilliseconds:F2}x");
    _output.WriteLine($"Recall: {recall:P}");
}
```

**6.4 Unit Tests (Minimal, focused)**
```csharp
// tests/Hartonomous.Core.Tests/ModelTests.cs
[Fact]
public void Model_JsonSerialization_PreservesStructure()
{
    // Only test things that EF doesn't handle automatically
    // This is a real concern: JSON columns must round-trip correctly
}
```

### Verification
- [ ] All integration tests pass
- [ ] Tests use real models/embeddings/queries
- [ ] Performance benchmarks show actual speedup
- [ ] Tests prove system value, not just "code works"

---

## Phase 7: Update Documentation (1-2 hours)

### Tasks

**7.1 Update copilot-instructions.md**
```markdown
### Key developer workflows (commands)
- **EF Code First migrations**: `cd src/Hartonomous.Data; dotnet ef migrations add MigrationName --startup-project ../ModelIngestion; dotnet ef database update --startup-project ../ModelIngestion`
- **Build solution**: `dotnet build Hartonomous.sln` (7 production + 4 test projects)
- **Run tests**: `dotnet test tests/Integration.Tests/` (real-world integration tests)
- **Test ingestion**: `dotnet run --project src/ModelIngestion`

### Project-specific conventions and patterns
- **EF Core is the standard**: Use IModelRepository, IEmbeddingRepository, IInferenceRepository via DI
- **ADO.NET only when painfully obvious**: SqlVector bulk inserts, stored proc parameters
- **Extensible model ingestion**: IModelFormatReader<TMetadata> supports ONNX, Safetensors, PyTorch, TensorFlow, etc.
- **Service interfaces for DI**: IEmbeddingIngestionService, IAtomicStorageService, etc.
- **Real-world tests**: Integration tests prove actual value with real models/embeddings

### Don'ts / pitfalls
- **Do NOT use direct SqlConnection in services** - Use repositories (EF Core)
- **Do NOT create academic tests** - Tests must prove real-world value
- **Do NOT hardcode configuration** - Use IConfiguration with appsettings.json
- **Do NOT use DTOs for model parsing** - Readers output Core entities directly
```

**7.2 Update README.md**
```markdown
## Architecture

### Clean Architecture (EF Core)
- **Core**: Domain entities (Model, ModelLayer, Embedding, etc.)
- **Data**: EF DbContext, configurations, migrations
- **Infrastructure**: Repository implementations (IModelRepository, IEmbeddingRepository)
- **Applications**: ModelIngestion (services using repositories via DI)

### Extensible Model Ingestion
- **IModelFormatReader<TMetadata>**: Generic interface for any model format
- **Current**: ONNX, Safetensors (state-of-the-art parsing)
- **Future**: PyTorch (.pt), TensorFlow (SavedModel), HuggingFace (Transformers)

### Real-World Testing
- Integration tests with actual models, embeddings, queries
- Performance benchmarks showing real speedups
- No academic "code matches spec" tests
```

**7.3 Update PRODUCTION_GUIDE.md**
```markdown
## Performance Characteristics

### When ADO.NET is Used
- **SqlVector bulk inserts**: `AddWithValue` pattern measurably faster for batch operations
- **Everything else**: EF Core repositories with proper DI

### Model Ingestion
- Extensible via `IModelFormatReader<TMetadata>`
- Parsers output Core entities directly (no DTO layer)
- State-of-the-art: ONNX Runtime, Safetensors format support

### Testing Philosophy
- Real-world integration tests
- Actual models, embeddings, queries
- Performance benchmarks with meaningful metrics
```

### Verification
- [ ] All docs updated with EF-first approach
- [ ] Extensible architecture documented
- [ ] Real-world test philosophy explained
- [ ] No references to "legacy" or "mixed patterns"

---

## Summary: Before/After

### Before (Assessment identified)
- ❌ 18 `new SqlConnection` instances (mixed patterns)
- ❌ 2 Model classes (DTO confusion)
- ❌ 3 ModelRepository implementations (duplication)
- ❌ Test files in production code
- ❌ No proper test projects
- ❌ 12 obsolete SQL files

### After (This plan achieves)
- ✅ EF Core standard with repository pattern
- ✅ SqlVector ADO.NET only where painfully obvious (bulk inserts)
- ✅ Unified DI/interfaces/generics throughout
- ✅ Extensible `IModelFormatReader<TMetadata>` (ONNX/Safetensors starting points)
- ✅ Service interfaces for DI injection
- ✅ Proper test project structure
- ✅ Real-world integration tests proving value
- ✅ Clean workspace (8 files in ModelIngestion, down from 16)
- ✅ CesConsumer/CDC deferred (focus on ingestion first)

---

## Risk Mitigation

**Backup before starting:**
```powershell
git add -A
git commit -m "Pre-execution checkpoint - EF-first refactor"
git branch backup/pre-ef-refactor-$(Get-Date -Format 'yyyyMMdd-HHmmss')
git tag pre-ef-refactor-stable
```

**Rollback if needed:**
```powershell
git reset --hard pre-ef-refactor-stable
```

---

## Next Steps

1. **Review this plan** - Approve strategy and phases
2. **Create git backup** - Before any changes
3. **Start Phase 1** - Structure and cleanup (safest first step)
4. **Proceed sequentially** - Don't skip phases

**Estimated Total Time:** 10-15 hours (spread over 2-3 days)

**End of Execution Plan**
