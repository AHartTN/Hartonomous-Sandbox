# Phase 7: Performance Optimizations

**Priority**: LOW (Optional Enhancements)
**Estimated Time**: 8-16 hours
**Dependencies**: All previous phases complete

## Overview

Performance improvements and Azure integrations. From TODO_BACKUP.md Phases 4, 3.

---

## Task 7.1: SIMD Optimizations (Where Possible)

**Status**: ❌ NOT STARTED
**Source**: TODO_BACKUP.md Phase 4.1
**Research**: FINDING 4 in SQL_CLR_RESEARCH_FINDINGS.md

### Problem
Vector operations currently use scalar loops.

### Constraints
**CRITICAL**: SIMD NOT allowed in SQL CLR (creates mixed assemblies).
SIMD only for non-SQL CLR code (API, Worker, tests).

### Solution

**API Vector Operations** (allowed SIMD):
```csharp
using System.Numerics;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;

public static float DotProductSIMD(float[] a, float[] b)
{
    if (Avx.IsSupported && a.Length >= 8)
    {
        // Use AVX for 8 floats at a time
        Vector256<float> sum = Vector256<float>.Zero;
        int i = 0;
        for (; i <= a.Length - 8; i += 8)
        {
            var va = Avx.LoadVector256(&a[i]);
            var vb = Avx.LoadVector256(&b[i]);
            sum = Avx.Add(sum, Avx.Multiply(va, vb));
        }
        // Sum vector + handle remainder
    }
    else
    {
        // Fallback to Vector<T> (128-bit)
    }
}
```

**SqlClr Vector Operations** (NO SIMD):
```csharp
// Must stay pure managed - no SIMD
public static SqlDouble DotProduct(SqlGeometry vector1, SqlGeometry vector2)
{
    double result = 0;
    for (int i = 0; i < dim; i++)
    {
        result += v1[i] * v2[i]; // Scalar operations only
    }
    return new SqlDouble(result);
}
```

### Files to Optimize
- `src/Hartonomous.Api/Services/VectorService.cs` (SIMD OK)
- `src/Hartonomous.Infrastructure/Services/EmbeddingService.cs` (SIMD OK)
- `src/SqlClr/**/*.cs` (NO SIMD - keep pure managed)

### Performance Targets
- 3-5x speedup for API vector operations
- SqlClr stays same (no SIMD available)

---

## Task 7.2: ArrayPool for Allocations

**Status**: ❌ NOT STARTED
**Source**: TODO_BACKUP.md Phase 4.1

### Problem
Frequent large array allocations create GC pressure.

### Solution

Use `ArrayPool<T>` to reuse buffers:

```csharp
using System.Buffers;

public async Task<float[]> EmbedAsync(string text)
{
    // Rent buffer instead of allocating
    float[] buffer = ArrayPool<float>.Shared.Rent(1536);
    try
    {
        // Use buffer for computation
        await ComputeEmbedding(text, buffer);
        
        // Return only used portion
        float[] result = new float[1536];
        Array.Copy(buffer, result, 1536);
        return result;
    }
    finally
    {
        // Always return to pool
        ArrayPool<float>.Shared.Return(buffer);
    }
}
```

### Files to Update
- Embedding services (large float arrays)
- Vector aggregation services
- Batch processing operations

### Performance Targets
- Reduce GC pressure by 50-70%
- Reduce allocations in hot paths

---

## Task 7.3: Struct Refactoring for Analytics

**Status**: ❌ NOT STARTED
**Source**: TODO_BACKUP.md Phase 4.2

### Problem
Analytics classes allocated on heap, creating GC pressure.

### Solution

Convert to stack-allocated structs:

**Before** (class = heap):
```csharp
public class AnalyticsSnapshot
{
    public long EntityId { get; set; }
    public double ConfidenceScore { get; set; }
    public DateTime Timestamp { get; set; }
}
```

**After** (struct = stack):
```csharp
public readonly struct AnalyticsSnapshot
{
    public long EntityId { get; }
    public double ConfidenceScore { get; }
    public DateTime Timestamp { get; }
    
    public AnalyticsSnapshot(long entityId, double confidence, DateTime timestamp)
    {
        EntityId = entityId;
        ConfidenceScore = confidence;
        Timestamp = timestamp;
    }
}
```

### Guidelines
- Convert to struct if:
  - Small size (<= 16 bytes ideal, max 64 bytes)
  - Immutable (readonly fields)
  - Value semantics (equality based on fields)
  - Short-lived (local variables, not long-term storage)

- Keep as class if:
  - Large size (>64 bytes)
  - Needs inheritance
  - Mutable state
  - Long-lived (cached, stored in collections)

### Performance Targets
- 2-3x speedup in analytics hot paths
- Reduce heap allocations

---

## Task 7.4: Azure Blob Storage for Model Files

**Status**: ❌ NOT STARTED
**Source**: TODO_BACKUP.md Phase 3.1

### Problem
Model files stored in SQL FILESTREAM.
Large files (GB+) not optimal in database.

### Solution

**Step 1**: Create BlobStorageService
```csharp
public class BlobStorageService : IModelStorageService
{
    private readonly BlobContainerClient _container;
    
    public async Task<string> UploadModelAsync(Stream modelStream, string modelId)
    {
        var blobClient = _container.GetBlobClient($"models/{modelId}.onnx");
        await blobClient.UploadAsync(modelStream);
        return blobClient.Uri.ToString();
    }
    
    public async Task<Stream> DownloadModelAsync(string blobUri)
    {
        var blobClient = new BlobClient(new Uri(blobUri));
        return await blobClient.OpenReadAsync();
    }
}
```

**Step 2**: Update model ingestion
Store blob URI in database instead of file bytes.

**Step 3**: Update inference
Download model from blob when needed (or cache locally).

### Benefits
- Reduce database size
- Better scalability
- Cheaper storage ($0.018/GB vs SQL storage)
- CDN integration possible

---

## Task 7.5: Azure Queue Storage for Async Jobs

**Status**: ❌ NOT STARTED
**Source**: TODO_BACKUP.md Phase 3.2

### Problem
Service Broker for async jobs is complex to manage.

### Solution

**Step 1**: Create QueueService
```csharp
public class QueueStorageService : IJobQueueService
{
    private readonly QueueClient _queue;
    
    public async Task EnqueueIngestionJobAsync(ModelIngestionRequest request)
    {
        var message = JsonSerializer.Serialize(request);
        await _queue.SendMessageAsync(message);
    }
    
    public async Task<ModelIngestionRequest> DequeueAsync()
    {
        QueueMessage message = await _queue.ReceiveMessageAsync();
        return JsonSerializer.Deserialize<ModelIngestionRequest>(message.Body);
    }
}
```

**Step 2**: Update Worker to process queue
```csharp
public class IngestionWorker : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            var job = await _queueService.DequeueAsync();
            await ProcessIngestionJob(job);
        }
    }
}
```

### Benefits
- Simpler than Service Broker
- Better monitoring (Azure Portal)
- Auto-scaling support
- Dead-letter queue built-in

---

## Task 7.6: Application Insights Telemetry

**Status**: ❌ NOT STARTED
**Source**: TODO_BACKUP.md Phase 3.4

### Problem
Limited observability into ML operations.

### Solution

**Step 1**: Add custom metrics
```csharp
public class InferenceService
{
    private readonly TelemetryClient _telemetry;
    
    public async Task<InferenceResult> InferAsync(InferenceRequest request)
    {
        using var operation = _telemetry.StartOperation<RequestTelemetry>("Inference");
        var sw = Stopwatch.StartNew();
        
        try
        {
            var result = await ExecuteInference(request);
            
            // Track custom metrics
            _telemetry.TrackMetric("InferenceLatency", sw.ElapsedMilliseconds);
            _telemetry.TrackMetric("ModelSize", result.ModelSizeMB);
            _telemetry.TrackMetric("ConfidenceScore", result.ConfidenceScore);
            
            operation.Telemetry.Success = true;
            return result;
        }
        catch (Exception ex)
        {
            _telemetry.TrackException(ex);
            operation.Telemetry.Success = false;
            throw;
        }
    }
}
```

**Step 2**: Track dependencies
```csharp
// Track ONNX Runtime calls
using var dependency = _telemetry.StartOperation<DependencyTelemetry>("OnnxInference");
dependency.Telemetry.Type = "ONNX";
dependency.Telemetry.Target = modelPath;
```

**Step 3**: Create custom dashboards
- Inference latency percentiles
- Model performance over time
- Error rates by model
- Resource usage

---

## Task 7.7: Azure Arc Integration

**Status**: ❌ NOT STARTED
**Source**: TODO_BACKUP.md Phase 3.3

### Problem
Hybrid deployment (on-prem + cloud) needs unified management.

### Solution

**Step 1**: Register SQL Server with Azure Arc
```powershell
# Install Arc agent
Invoke-WebRequest -Uri https://aka.ms/AzureArcAgent -OutFile AzureArcAgent.msi
msiexec /i AzureArcAgent.msi

# Connect to Azure
azcmagent connect --resource-group hartonomous-rg --location eastus
```

**Step 2**: Enable Arc-enabled services
- Azure Monitor for hybrid SQL
- Azure Security Center
- Automated backups to Azure

**Step 3**: Add Arc monitoring
```csharp
public class ArcMetricsService
{
    public async Task ReportResourceUsage()
    {
        // Report CPU, memory, GPU usage to Arc
        await _arcClient.ReportMetricsAsync(new ResourceMetrics
        {
            CpuPercent = GetCpuUsage(),
            MemoryMB = GetMemoryUsage(),
            GpuPercent = GetGpuUsage()
        });
    }
}
```

---

## Success Criteria

Phase 7 complete when:
- ✅ SIMD optimizations added (API/Infrastructure only)
- ✅ ArrayPool used for buffer allocations
- ✅ Analytics structs refactored (where appropriate)
- ✅ Blob storage integrated for model files
- ✅ Queue storage integrated for async jobs
- ✅ Application Insights tracking added
- ✅ Azure Arc configured (optional)
- ✅ Performance benchmarks run
- ✅ All tests pass
- ✅ Changes committed to git

## Performance Targets

- Vector operations: 3-5x faster (API/Infrastructure)
- GC pressure: 50-70% reduction
- Analytics hot paths: 2-3x faster
- Database size: 30-50% smaller (models in blob)
- Job processing: More reliable (queue vs Service Broker)

## Next Steps

After Phase 7 complete:
- Run performance benchmarks
- Document optimization results
- Create production deployment plan
- **CELEBRATE COMPLETION!** 🎉
