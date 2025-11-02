# Hartonomous API Reference

## Table of Contents

- [Core Interfaces](#core-interfaces)
- [Repository Interfaces](#repository-interfaces)
- [Service Interfaces](#service-interfaces)
- [Models and Value Objects](#models-and-value-objects)
- [Usage Examples](#usage-examples)

## Core Interfaces

### IAtomRepository

Primary repository for atom (content unit) management.

```csharp
public interface IAtomRepository
{
    // Retrieval
    Task<Atom?> GetByIdAsync(long atomId, CancellationToken cancellationToken = default);
    Task<Atom?> GetByContentHashAsync(byte[] contentHash, CancellationToken cancellationToken = default);
    Task<IEnumerable<Atom>> GetByModalityAsync(string modality, int skip = 0, int take = 100, CancellationToken cancellationToken = default);
    
    // Creation
    Task<Atom> AddAsync(Atom atom, CancellationToken cancellationToken = default);
    Task<IEnumerable<Atom>> AddRangeAsync(IEnumerable<Atom> atoms, CancellationToken cancellationToken = default);
    
    // Updates
    Task UpdateAsync(Atom atom, CancellationToken cancellationToken = default);
    Task IncrementReferenceCountAsync(long atomId, CancellationToken cancellationToken = default);
    Task DecrementReferenceCountAsync(long atomId, CancellationToken cancellationToken = default);
    Task UpdateMetadataAsync(long atomId, string metadata, CancellationToken cancellationToken = default);
    Task UpdateSpatialKeyAsync(long atomId, Point spatialKey, CancellationToken cancellationToken = default);
    
    // Deletion
    Task DeleteAsync(long atomId, CancellationToken cancellationToken = default);
    Task<int> DeleteOrphanedAtomsAsync(CancellationToken cancellationToken = default);
    
    // Queries
    Task<bool> ExistsAsync(long atomId, CancellationToken cancellationToken = default);
    Task<int> GetCountAsync(string? modality = null, CancellationToken cancellationToken = default);
    Task<IEnumerable<Atom>> GetTopReferencedAsync(int count = 100, CancellationToken cancellationToken = default);
}
```

**Example**:

```csharp
// Inject repository
public class MyService
{
    private readonly IAtomRepository _atomRepository;
    
    public MyService(IAtomRepository atomRepository)
    {
        _atomRepository = atomRepository;
    }
    
    public async Task<Atom> CreateAtomAsync(string text, CancellationToken cancellationToken)
    {
        var contentHash = SHA256.HashData(Encoding.UTF8.GetBytes(text));
        
        // Check if atom already exists
        var existing = await _atomRepository.GetByContentHashAsync(contentHash, cancellationToken);
        if (existing != null)
        {
            await _atomRepository.IncrementReferenceCountAsync(existing.AtomId, cancellationToken);
            return existing;
        }
        
        // Create new atom
        var atom = new Atom
        {
            ContentHash = contentHash,
            Modality = "text",
            CanonicalText = text,
            ReferenceCount = 1
        };
        
        return await _atomRepository.AddAsync(atom, cancellationToken);
    }
}
```

### IAtomEmbeddingRepository

Repository for vector embeddings with hybrid search capabilities.

```csharp
public interface IAtomEmbeddingRepository
{
    // Retrieval
    Task<AtomEmbedding?> GetByIdAsync(long embeddingId, CancellationToken cancellationToken = default);
    Task<IEnumerable<AtomEmbedding>> GetByAtomIdAsync(long atomId, CancellationToken cancellationToken = default);
    
    // Creation
    Task<AtomEmbedding> AddAsync(AtomEmbedding embedding, CancellationToken cancellationToken = default);
    Task AddWithComponentsAsync(AtomEmbedding embedding, IEnumerable<AtomEmbeddingComponent> components, CancellationToken cancellationToken = default);
    
    // Vector Search
    Task<IEnumerable<EmbeddingSearchResult>> SearchByVectorAsync(
        SqlVector<float> queryVector,
        int topK = 10,
        string? embeddingType = null,
        CancellationToken cancellationToken = default);
    
    Task<IEnumerable<EmbeddingSearchResult>> SearchByVectorWithThresholdAsync(
        SqlVector<float> queryVector,
        double threshold,
        int maxResults = 100,
        string? embeddingType = null,
        CancellationToken cancellationToken = default);
    
    // Spatial Search
    Task<IEnumerable<EmbeddingSearchResult>> SearchBySpatialRegionAsync(
        Point center,
        double radius,
        int topK = 10,
        CancellationToken cancellationToken = default);
    
    // Hybrid Search (Spatial filter + Vector reranking)
    Task<IEnumerable<EmbeddingSearchResult>> HybridSearchAsync(
        SqlVector<float> queryVector,
        Point? spatialCenter = null,
        double? spatialRadius = null,
        int topK = 10,
        string? embeddingType = null,
        CancellationToken cancellationToken = default);
    
    // Spatial Projection
    Task<Point> ComputeSpatialProjectionAsync(SqlVector<float> vector, CancellationToken cancellationToken = default);
    Task UpdateSpatialProjectionAsync(long embeddingId, Point fine, Point coarse, CancellationToken cancellationToken = default);
    
    // Statistics
    Task<int> GetCountAsync(string? embeddingType = null, CancellationToken cancellationToken = default);
    Task<double> GetAverageDimensionAsync(CancellationToken cancellationToken = default);
}
```

**Example**:

```csharp
public class SemanticSearchService
{
    private readonly IAtomEmbeddingRepository _embeddingRepository;
    
    public async Task<IEnumerable<string>> SearchAsync(
        SqlVector<float> queryVector,
        int topK = 10,
        CancellationToken cancellationToken = default)
    {
        // Perform hybrid search
        var results = await _embeddingRepository.HybridSearchAsync(
            queryVector,
            spatialCenter: null, // No spatial filter
            spatialRadius: null,
            topK,
            embeddingType: "semantic",
            cancellationToken);
        
        // Extract canonical text from atoms
        return results
            .Select(r => r.Atom.CanonicalText ?? "[No text]")
            .ToList();
    }
}
```

### IModelRepository

Repository for AI model metadata and layers.

```csharp
public interface IModelRepository
{
    // Retrieval
    Task<Model?> GetByIdAsync(int modelId, CancellationToken cancellationToken = default);
    Task<Model?> GetByNameAsync(string modelName, CancellationToken cancellationToken = default);
    Task<IEnumerable<Model>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<IEnumerable<Model>> GetByTypeAsync(string modelType, CancellationToken cancellationToken = default);
    
    // Creation
    Task<Model> AddAsync(Model model, CancellationToken cancellationToken = default);
    Task<ModelLayer> AddLayerAsync(int modelId, ModelLayer layer, CancellationToken cancellationToken = default);
    
    // Updates
    Task UpdateAsync(Model model, CancellationToken cancellationToken = default);
    Task UpdateLayerWeightsAsync(int layerId, SqlVector<float> weights, CancellationToken cancellationToken = default);
    
    // Layer Queries
    Task<IEnumerable<ModelLayer>> GetLayersByModelIdAsync(int modelId, CancellationToken cancellationToken = default);
    
    // Deletion
    Task DeleteAsync(int modelId, CancellationToken cancellationToken = default);
    
    // Queries
    Task<bool> ExistsAsync(int modelId, CancellationToken cancellationToken = default);
    Task<int> GetCountAsync(CancellationToken cancellationToken = default);
}
```

## Repository Interfaces

### IModelLayerRepository

Specialized repository for model layer operations.

```csharp
public interface IModelLayerRepository
{
    Task<ModelLayer?> GetByIdAsync(long layerId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ModelLayer>> GetByModelAsync(int modelId, CancellationToken cancellationToken = default);
    Task<ModelLayer> AddAsync(ModelLayer layer, CancellationToken cancellationToken = default);
    Task BulkInsertAsync(IEnumerable<ModelLayer> layers, CancellationToken cancellationToken = default);
    Task UpdateAsync(ModelLayer layer, CancellationToken cancellationToken = default);
    Task DeleteAsync(long layerId, CancellationToken cancellationToken = default);
    
    // Weight Queries
    Task<IReadOnlyList<ModelLayer>> GetLayersByWeightRangeAsync(
        int modelId,
        double minValue,
        double maxValue,
        CancellationToken cancellationToken = default);
    
    Task<IReadOnlyList<ModelLayer>> GetLayersByImportanceAsync(
        int modelId,
        double minImportance,
        CancellationToken cancellationToken = default);
    
    // Geometry Helpers
    float[] ExtractWeightsFromGeometry(LineString geometry);
    LineString CreateGeometryFromWeights(
        float[] weights,
        float[]? importanceScores = null,
        float[]? temporalMetadata = null);
}
```

### IDeduplicationPolicyRepository

Repository for deduplication policies.

```csharp
public interface IDeduplicationPolicyRepository
{
    Task<DeduplicationPolicy?> GetActivePolicyAsync(
        string policyName,
        CancellationToken cancellationToken = default);
    
    Task<IReadOnlyList<DeduplicationPolicy>> GetAllAsync(
        CancellationToken cancellationToken = default);
    
    Task<DeduplicationPolicy> AddAsync(
        DeduplicationPolicy policy,
        CancellationToken cancellationToken = default);
    
    Task UpdateAsync(
        DeduplicationPolicy policy,
        CancellationToken cancellationToken = default);
}
```

## Service Interfaces

### IAtomIngestionService

High-level service for atom ingestion with deduplication.

```csharp
public interface IAtomIngestionService
{
    Task<AtomIngestionResult> IngestAsync(
        AtomIngestionRequest request,
        CancellationToken cancellationToken = default);
    
    Task<IEnumerable<AtomIngestionResult>> IngestBatchAsync(
        IEnumerable<AtomIngestionRequest> requests,
        CancellationToken cancellationToken = default);
}

// Supporting types
public record AtomIngestionRequest(
    string Modality,
    byte[] ContentHash,
    string? CanonicalText = null,
    SqlVector<float>? Embedding = null,
    string? Metadata = null);

public record AtomIngestionResult(
    long AtomId,
    bool IsNewAtom,
    long? DuplicateOfAtomId = null,
    double? SimilarityScore = null,
    Point? SpatialProjection = null);
```

**Example**:

```csharp
public class ContentProcessor
{
    private readonly IAtomIngestionService _ingestionService;
    
    public async Task ProcessTextAsync(string text, CancellationToken cancellationToken)
    {
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(text));
        var embedding = await GenerateEmbeddingAsync(text); // Your embedding logic
        
        var request = new AtomIngestionRequest(
            Modality: "text",
            ContentHash: hash,
            CanonicalText: text,
            Embedding: embedding);
        
        var result = await _ingestionService.IngestAsync(request, cancellationToken);
        
        if (result.IsNewAtom)
        {
            Console.WriteLine($"Created new atom {result.AtomId}");
        }
        else
        {
            Console.WriteLine($"Found duplicate atom {result.DuplicateOfAtomId} " +
                            $"with similarity {result.SimilarityScore:F3}");
        }
    }
}
```

### IStudentModelService

Service for model distillation and compression.

```csharp
public interface IStudentModelService
{
    // Extract by importance
    Task<Model> ExtractByImportanceAsync(
        int parentModelId,
        double targetSizeRatio,
        CancellationToken cancellationToken = default);
    
    // Extract by layer count
    Task<Model> ExtractByLayersAsync(
        int parentModelId,
        int targetLayerCount,
        CancellationToken cancellationToken = default);
    
    // Extract by spatial region
    Task<Model> ExtractBySpatialRegionAsync(
        int parentModelId,
        double minValue,
        double maxValue,
        CancellationToken cancellationToken = default);
    
    // Compare models
    Task<ModelComparisonResult> CompareModelsAsync(
        int modelAId,
        int modelBId,
        CancellationToken cancellationToken = default);
}

public record ModelComparisonResult(
    int ModelAParameters,
    int ModelBParameters,
    double CompressionRatio,
    double AvgImportanceA,
    double AvgImportanceB,
    int SharedLayers,
    double WeightOverlap);
```

**Example**:

```csharp
public class ModelCompressionService
{
    private readonly IStudentModelService _studentModelService;
    
    public async Task<Model> CompressModelAsync(
        int parentModelId,
        double targetRatio = 0.3,
        CancellationToken cancellationToken = default)
    {
        // Extract student model (30% of parent size)
        var studentModel = await _studentModelService.ExtractByImportanceAsync(
            parentModelId,
            targetRatio,
            cancellationToken);
        
        // Compare with parent
        var comparison = await _studentModelService.CompareModelsAsync(
            parentModelId,
            studentModel.ModelId,
            cancellationToken);
        
        Console.WriteLine($"Compression ratio: {comparison.CompressionRatio:F2}x");
        Console.WriteLine($"Weight overlap: {comparison.WeightOverlap:P2}");
        
        return studentModel;
    }
}
```

### ISpatialInferenceService

Service for spatial reasoning operations.

```csharp
public interface ISpatialInferenceService
{
    // Spatial attention
    Task<IEnumerable<AtomEmbedding>> GetSpatialAttentionAsync(
        Point queryPoint,
        double radius,
        int topK = 10,
        CancellationToken cancellationToken = default);
    
    // Next token prediction via spatial proximity
    Task<string?> GetSpatialNextTokenAsync(
        Point currentPosition,
        int vocabularySize,
        CancellationToken cancellationToken = default);
    
    // Multi-resolution search
    Task<IEnumerable<EmbeddingSearchResult>> MultiResolutionSearchAsync(
        SqlVector<float> queryVector,
        int coarseK = 100,
        int fineK = 10,
        CancellationToken cancellationToken = default);
}
```

### IModelIngestionService

Service for model format detection and ingestion.

```csharp
public interface IModelIngestionService
{
    Task<ModelIngestionResult> IngestModelAsync(
        string modelPath,
        CancellationToken cancellationToken = default);
    
    Task<IEnumerable<ModelIngestionResult>> IngestModelsAsync(
        IEnumerable<string> modelPaths,
        CancellationToken cancellationToken = default);
}

public record ModelIngestionResult(
    int ModelId,
    string ModelName,
    string Format,
    int LayerCount,
    long ParameterCount,
    TimeSpan Duration,
    string? ErrorMessage = null);
```

## Models and Value Objects

### EmbeddingSearchResult

```csharp
public record EmbeddingSearchResult(
    long AtomEmbeddingId,
    long AtomId,
    Atom Atom,
    double Distance,
    double SimilarityScore,
    SqlVector<float> EmbeddingVector,
    Point? SpatialGeometry);
```

### SemanticFeatures

```csharp
public class SemanticFeatures
{
    public long AtomId { get; set; }
    public string? TopicLabel { get; set; }
    public double TopicConfidence { get; set; }
    public double SentimentScore { get; set; }
    public string SentimentLabel { get; set; } = "neutral";
    public double FormalityScore { get; set; }
    public string? Keywords { get; set; }
    public string? Entities { get; set; }
    public string? FeatureVector { get; set; } // JSON
    public DateTime ComputedAt { get; set; }
}
```

### GenerationResult

```csharp
public record GenerationResult(
    long InferenceId,
    string GeneratedText,
    double Confidence,
    int TokenCount,
    TimeSpan Duration,
    IReadOnlyList<InferenceStep> Steps);
```

### EnsembleInferenceResult

```csharp
public record EnsembleInferenceResult(
    long InferenceId,
    string CombinedOutput,
    double AverageConfidence,
    IReadOnlyDictionary<int, double> ModelWeights,
    IReadOnlyList<InferenceStep> Steps);
```

## Usage Examples

### Complete Ingestion Pipeline

```csharp
public class CompletePipelineExample
{
    private readonly IAtomIngestionService _atomIngestion;
    private readonly IAtomEmbeddingRepository _embeddingRepo;
    private readonly IDeduplicationPolicyRepository _policyRepo;
    
    public async Task ProcessDocumentAsync(
        string filePath,
        CancellationToken cancellationToken)
    {
        // 1. Read document
        var text = await File.ReadAllTextAsync(filePath, cancellationToken);
        
        // 2. Get active deduplication policy
        var policy = await _policyRepo.GetActivePolicyAsync("semantic", cancellationToken);
        
        // 3. Generate embedding
        var embedding = await GenerateEmbeddingAsync(text);
        
        // 4. Create ingestion request
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(text));
        var request = new AtomIngestionRequest(
            Modality: "text",
            ContentHash: hash,
            CanonicalText: text,
            Embedding: embedding,
            Metadata: $"{{\"source\":\"{filePath}\"}}");
        
        // 5. Ingest with deduplication
        var result = await _atomIngestion.IngestAsync(request, cancellationToken);
        
        // 6. Handle result
        if (result.IsNewAtom)
        {
            Console.WriteLine($"Ingested new atom {result.AtomId}");
        }
        else
        {
            Console.WriteLine($"Found duplicate of {result.DuplicateOfAtomId} " +
                            $"(similarity: {result.SimilarityScore:F3})");
        }
    }
    
    private async Task<SqlVector<float>> GenerateEmbeddingAsync(string text)
    {
        // Your embedding generation logic (e.g., call to embedding model)
        // This is a placeholder
        var values = new float[384]; // Example: sentence-transformers dimension
        return new SqlVector<float>(values);
    }
}
```

### Hybrid Search with Filtering

```csharp
public class HybridSearchExample
{
    private readonly IAtomEmbeddingRepository _embeddingRepo;
    private readonly IAtomRepository _atomRepo;
    
    public async Task<IEnumerable<string>> SearchWithFiltersAsync(
        string query,
        string modality = "text",
        CancellationToken cancellationToken = default)
    {
        // 1. Generate query embedding
        var queryEmbedding = await GenerateEmbeddingAsync(query);
        
        // 2. Perform hybrid search
        var searchResults = await _embeddingRepo.HybridSearchAsync(
            queryEmbedding,
            spatialCenter: null,
            spatialRadius: null,
            topK: 20,
            embeddingType: "semantic",
            cancellationToken);
        
        // 3. Filter by modality
        var filteredResults = searchResults
            .Where(r => r.Atom.Modality == modality)
            .Take(10);
        
        // 4. Extract text
        return filteredResults
            .Select(r => r.Atom.CanonicalText ?? "[No text]")
            .ToList();
    }
}
```

### Model Comparison and Selection

```csharp
public class ModelSelectionExample
{
    private readonly IModelRepository _modelRepo;
    private readonly IStudentModelService _studentModelService;
    
    public async Task<Model> SelectBestModelAsync(
        string taskType,
        CancellationToken cancellationToken)
    {
        // 1. Get all models
        var allModels = await _modelRepo.GetAllAsync(cancellationToken);
        
        // 2. Filter by usage statistics
        var candidates = allModels
            .Where(m => m.UsageCount > 100)
            .OrderBy(m => m.AverageInferenceMs)
            .Take(5)
            .ToList();
        
        // 3. Compare candidates
        var comparisons = new List<(Model Model, ModelComparisonResult Comparison)>();
        var referenceModel = candidates.First();
        
        foreach (var candidate in candidates.Skip(1))
        {
            var comparison = await _studentModelService.CompareModelsAsync(
                referenceModel.ModelId,
                candidate.ModelId,
                cancellationToken);
            
            comparisons.Add((candidate, comparison));
        }
        
        // 4. Select best (highest avg importance, lowest inference time)
        var best = comparisons
            .OrderByDescending(c => c.Comparison.AvgImportanceB)
            .ThenBy(c => c.Model.AverageInferenceMs)
            .First();
        
        return best.Model;
    }
}
```

---

## Next Steps

- Review [Data Model Documentation](data-model.md) for entity schemas
- See [Operations Guide](operations.md) for production usage
- Consult [Architecture Overview](architecture.md) for system design

