# Quick Start: Pipeline Integration Guide

## TL;DR - Start Here

This guide provides **copy-paste ready code** to integrate the MS-validated pipeline architecture into Hartonomous in **one day**.

**Time Estimate:** 4-6 hours for basic integration

---

## Step 1: DI Registration (30 minutes)

### File: `src/Hartonomous.Api/Program.cs`

Add this code after `builder.Services.AddDbContext<HartonomousDbContext>()`:

```csharp
// ============================================================================
// PIPELINE ARCHITECTURE - Add at end of service registration
// ============================================================================

// OpenTelemetry resources
var activitySource = new ActivitySource("Hartonomous.Pipelines", "1.0.0");
var meter = new Meter("Hartonomous.Pipelines", "1.0.0");
builder.Services.AddSingleton(activitySource);
builder.Services.AddSingleton(meter);

// OpenTelemetry configuration
builder.Services.AddOpenTelemetry()
    .ConfigureResource(r => r.AddService("Hartonomous"))
    .WithTracing(t => t
        .AddSource("Hartonomous.Pipelines")
        .AddAspNetCoreInstrumentation()
        .AddHttpClientInstrumentation()
        .AddEntityFrameworkCoreInstrumentation()
        .AddConsoleExporter()) // Change to OTLP/AppInsights in production
    .WithMetrics(m => m
        .AddMeter("Hartonomous.Pipelines")
        .AddAspNetCoreInstrumentation()
        .AddPrometheusExporter());

// Atom Ingestion Channel (bounded queue with backpressure)
builder.Services.AddSingleton(_ => 
{
    var capacity = builder.Configuration.GetValue("AtomIngestion:QueueCapacity", 1000);
    var options = new BoundedChannelOptions(capacity)
    {
        FullMode = BoundedChannelFullMode.Wait, // MS-recommended backpressure
        SingleReader = false,
        SingleWriter = false,
        AllowSynchronousContinuations = false
    };
    return Channel.CreateBounded<AtomIngestionPipelineRequest>(options);
});

builder.Services.AddSingleton(sp => 
    sp.GetRequiredService<Channel<AtomIngestionPipelineRequest>>().Reader);
builder.Services.AddSingleton(sp => 
    sp.GetRequiredService<Channel<AtomIngestionPipelineRequest>>().Writer);

// Pipeline factories
builder.Services.AddScoped<AtomIngestionPipelineFactory>();
builder.Services.AddScoped<EnsembleInferencePipelineFactory>();

// Background worker
builder.Services.AddHostedService<AtomIngestionWorker>();

// ============================================================================
```

**Add these using statements at the top:**

```csharp
using System.Diagnostics;
using System.Diagnostics.Metrics;
using System.Threading.Channels;
using Hartonomous.Core.Pipelines;
using Hartonomous.Core.Pipelines.Implementations;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using OpenTelemetry.Metrics;
```

### File: `src/Hartonomous.Api/appsettings.json`

Add this section:

```json
{
  "AtomIngestion": {
    "QueueCapacity": 1000,
    "MaxConcurrency": 10
  },
  "Pipelines": {
    "EnableResilience": true,
    "MaxRetries": 3,
    "TimeoutSeconds": 30
  },
  "FeatureFlags": {
    "UsePipelineArchitecture": false
  }
}
```

**Verify:** Run `dotnet build src/Hartonomous.Api` - should compile without errors.

---

## Step 2: Create AtomIngestion Adapter (1 hour)

### File: `src/Hartonomous.Core/Services/AtomIngestionServiceAdapter.cs` (NEW)

```csharp
using Hartonomous.Core.Pipelines;
using Hartonomous.Core.Pipelines.Implementations;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Hartonomous.Core.Services;

/// <summary>
/// Backward-compatible adapter that wraps AtomIngestionPipeline.
/// Maintains IAtomIngestionService interface while using new pipeline architecture.
/// </summary>
public class AtomIngestionServiceAdapter : IAtomIngestionService
{
    private readonly AtomIngestionPipelineFactory _pipelineFactory;
    private readonly IConfiguration _configuration;
    private readonly ILogger<AtomIngestionServiceAdapter> _logger;

    public AtomIngestionServiceAdapter(
        AtomIngestionPipelineFactory pipelineFactory,
        IConfiguration configuration,
        ILogger<AtomIngestionServiceAdapter> logger)
    {
        _pipelineFactory = pipelineFactory;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<AtomIngestionResult> IngestAsync(
        AtomIngestionRequest request,
        CancellationToken cancellationToken = default)
    {
        // Convert to pipeline request
        var pipelineRequest = new AtomIngestionPipelineRequest
        {
            ContentType = request.ContentType,
            RawContent = request.RawContent,
            Metadata = request.Metadata,
            SourceSystem = request.SourceSystem,
            CorrelationId = request.CorrelationId ?? Guid.NewGuid().ToString()
        };

        try
        {
            // Execute pipeline
            var result = await _pipelineFactory.IngestAtomAsync(
                pipelineRequest,
                cancellationToken);

            if (!result.IsSuccess || result.Output?.Atom == null)
            {
                _logger.LogError(
                    "Atom ingestion failed. CorrelationId: {CorrelationId}, Error: {Error}",
                    pipelineRequest.CorrelationId,
                    result.ErrorMessage);

                return new AtomIngestionResult
                {
                    Status = IngestionStatus.Failed,
                    ErrorMessage = result.ErrorMessage,
                    CorrelationId = pipelineRequest.CorrelationId
                };
            }

            // Map to legacy result format
            return new AtomIngestionResult
            {
                Status = IngestionStatus.Success,
                AtomId = result.Output.Atom.AtomId,
                ContentHash = result.Output.Atom.ContentHash,
                EmbeddingId = result.Output.Atom.EmbeddingId,
                CorrelationId = pipelineRequest.CorrelationId
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, 
                "Unexpected error in atom ingestion. CorrelationId: {CorrelationId}",
                pipelineRequest.CorrelationId);

            return new AtomIngestionResult
            {
                Status = IngestionStatus.Failed,
                ErrorMessage = ex.Message,
                CorrelationId = pipelineRequest.CorrelationId
            };
        }
    }
}
```

### File: `src/Hartonomous.Api/Program.cs` (Update DI registration)

**Replace existing `IAtomIngestionService` registration with:**

```csharp
// Replace old AtomIngestionService with pipeline-based adapter
builder.Services.AddScoped<IAtomIngestionService, AtomIngestionServiceAdapter>();
```

**Verify:** Run `dotnet build src/Hartonomous.Api` - should compile without errors.

---

## Step 3: Test the Integration (1 hour)

### File: `tests/Hartonomous.IntegrationTests/Pipelines/AtomIngestionPipelineTests.cs` (NEW)

```csharp
using Hartonomous.Core.Pipelines.Implementations;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Hartonomous.IntegrationTests.Pipelines;

public class AtomIngestionPipelineTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly IServiceProvider _services;

    public AtomIngestionPipelineTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
        _services = factory.Services;
    }

    [Fact]
    public async Task IngestAtomAsync_ValidRequest_ReturnsSuccess()
    {
        // Arrange
        using var scope = _services.CreateScope();
        var pipelineFactory = scope.ServiceProvider.GetRequiredService<AtomIngestionPipelineFactory>();

        var request = new AtomIngestionPipelineRequest
        {
            ContentType = "text/plain",
            RawContent = "Integration test atom content",
            SourceSystem = "IntegrationTest",
            CorrelationId = Guid.NewGuid().ToString()
        };

        // Act
        var result = await pipelineFactory.IngestAtomAsync(request);

        // Assert
        Assert.True(result.IsSuccess, $"Pipeline failed: {result.ErrorMessage}");
        Assert.NotNull(result.Output);
        Assert.NotNull(result.Output.Atom);
        Assert.NotNull(result.Output.Atom.ContentHash);
        Assert.Equal(request.CorrelationId, result.Context.CorrelationId);
    }

    [Fact]
    public async Task IngestAtomAsync_DuplicateContent_ReturnsDuplicateAtom()
    {
        // Arrange
        using var scope = _services.CreateScope();
        var pipelineFactory = scope.ServiceProvider.GetRequiredService<AtomIngestionPipelineFactory>();

        var request1 = new AtomIngestionPipelineRequest
        {
            ContentType = "text/plain",
            RawContent = "Duplicate test content",
            SourceSystem = "IntegrationTest"
        };

        // Act - ingest same content twice
        var result1 = await pipelineFactory.IngestAtomAsync(request1);
        var result2 = await pipelineFactory.IngestAtomAsync(request1);

        // Assert
        Assert.True(result1.IsSuccess);
        Assert.True(result2.IsSuccess);
        Assert.Equal(result1.Output!.Atom.ContentHash, result2.Output!.Atom.ContentHash);
        // Should return same atom (duplicate detected)
        Assert.Equal(result1.Output.Atom.AtomId, result2.Output.Atom.AtomId);
    }

    [Fact]
    public async Task AtomIngestionServiceAdapter_MaintainsBackwardCompatibility()
    {
        // Arrange
        using var scope = _services.CreateScope();
        var service = scope.ServiceProvider.GetRequiredService<IAtomIngestionService>();

        var request = new AtomIngestionRequest
        {
            ContentType = "text/plain",
            RawContent = "Adapter test content",
            SourceSystem = "AdapterTest"
        };

        // Act
        var result = await service.IngestAsync(request);

        // Assert
        Assert.Equal(IngestionStatus.Success, result.Status);
        Assert.NotNull(result.AtomId);
        Assert.NotNull(result.ContentHash);
    }
}
```

**Run tests:**

```bash
cd tests/Hartonomous.IntegrationTests
dotnet test --filter "FullyQualifiedName~AtomIngestionPipelineTests"
```

---

## Step 4: Enable Metrics Endpoint (30 minutes)

### File: `src/Hartonomous.Api/Program.cs`

**Add before `app.Run()`:**

```csharp
// Prometheus metrics endpoint
app.MapPrometheusScrapingEndpoint(); // Available at /metrics
```

### File: `src/Hartonomous.Api/Hartonomous.Api.csproj`

**Add NuGet package:**

```xml
<ItemGroup>
  <PackageReference Include="OpenTelemetry.Exporter.Prometheus.AspNetCore" Version="1.7.0" />
</ItemGroup>
```

**Restore packages:**

```bash
dotnet restore src/Hartonomous.Api
```

**Test metrics endpoint:**

```bash
# Start application
dotnet run --project src/Hartonomous.Api

# Check metrics (in another terminal)
curl http://localhost:5000/metrics
```

**Expected output:**

```
# TYPE atom_ingestion_requests_processed_total counter
atom_ingestion_requests_processed_total 0

# TYPE atom_ingestion_queue_depth gauge
atom_ingestion_queue_depth 0

# TYPE atom_ingestion_processing_duration histogram
# ... histogram buckets
```

---

## Step 5: Manual Testing (1 hour)

### Test 1: Ingest Atom via API

```bash
# Start application
dotnet run --project src/Hartonomous.Api

# Ingest atom
curl -X POST http://localhost:5000/api/atoms/ingest \
  -H "Content-Type: application/json" \
  -d '{
    "contentType": "text/plain",
    "rawContent": "Test atom from pipeline",
    "sourceSystem": "ManualTest"
  }'
```

**Expected response:**

```json
{
  "status": "Success",
  "atomId": 12345,
  "contentHash": "abc123...",
  "correlationId": "guid-here"
}
```

### Test 2: Verify Observability

**Check logs for source-generated messages:**

```
[Pipeline] Pipeline 'AtomIngestionPipeline' started. CorrelationId: ...
[Step] Pipeline step 'ComputeContentHashStep' started. CorrelationId: ...
[Step] Pipeline step 'ComputeContentHashStep' completed successfully. Duration: 5ms
[Step] Pipeline step 'CheckDuplicateAtomStep' started. CorrelationId: ...
[Step] Pipeline step 'CheckDuplicateAtomStep' completed successfully. Duration: 12ms
[Pipeline] Pipeline 'AtomIngestionPipeline' completed successfully. Duration: 150ms
```

**Check metrics:**

```bash
curl http://localhost:5000/metrics | grep atom_ingestion

# Should show:
# atom_ingestion_requests_processed_total 1
# atom_ingestion_processing_duration_sum 150
```

### Test 3: Load Test (Optional)

```bash
# Install hey (HTTP load generator)
go install github.com/rakyll/hey@latest

# Run load test (1000 requests, 100 concurrent)
hey -n 1000 -c 100 -m POST \
  -H "Content-Type: application/json" \
  -d '{"contentType":"text/plain","rawContent":"Load test","sourceSystem":"LoadTest"}' \
  http://localhost:5000/api/atoms/ingest
```

**Monitor metrics during load test:**

```bash
watch -n 1 "curl -s http://localhost:5000/metrics | grep atom_ingestion"
```

---

## Step 6: Deploy to Staging (1 hour)

### Build and Publish

```bash
# Build release
dotnet publish src/Hartonomous.Api -c Release -o ./publish

# Test published app locally
cd publish
dotnet Hartonomous.Api.dll

# Verify /metrics endpoint works
curl http://localhost:5000/metrics
```

### Deploy to Azure (if applicable)

```bash
# Deploy to staging slot
az webapp deploy \
  --resource-group Hartonomous-Staging \
  --name hartonomous-api \
  --src-path ./publish.zip

# Verify health
curl https://hartonomous-staging.azurewebsites.net/health

# Verify metrics
curl https://hartonomous-staging.azurewebsites.net/metrics
```

---

## Troubleshooting

### Issue: Compilation errors

**Solution:** Ensure all NuGet packages installed:

```bash
dotnet add src/Hartonomous.Core package System.Threading.Channels
dotnet add src/Hartonomous.Api package OpenTelemetry.Extensions.Hosting
dotnet add src/Hartonomous.Api package OpenTelemetry.Exporter.Prometheus.AspNetCore
dotnet add src/Hartonomous.Api package OpenTelemetry.Instrumentation.AspNetCore
dotnet add src/Hartonomous.Api package OpenTelemetry.Instrumentation.Http
dotnet add src/Hartonomous.Api package OpenTelemetry.Instrumentation.EntityFrameworkCore
```

### Issue: AtomIngestionWorker not processing

**Check:**

```csharp
// Verify channel registration
var channel = app.Services.GetRequiredService<Channel<AtomIngestionPipelineRequest>>();
Console.WriteLine($"Channel capacity: {channel.Reader.CanCount}");

// Verify worker started
var hostedServices = app.Services.GetServices<IHostedService>();
Console.WriteLine($"Hosted services: {hostedServices.Count()}");
```

### Issue: Metrics not showing

**Check:**

```csharp
// Verify meter registration
var meter = app.Services.GetRequiredService<Meter>();
Console.WriteLine($"Meter name: {meter.Name}");

// Enable verbose logging
builder.Logging.AddFilter("OpenTelemetry", LogLevel.Debug);
```

---

## Next Steps After Integration

1. **Enable Feature Flag Rollout**
   - Start at 0%, monitor deployment
   - Increase to 10% → 50% → 100%

2. **Performance Benchmarking**
   - Run BenchmarkDotNet tests
   - Compare old vs. new performance

3. **Production Monitoring**
   - Setup Application Insights
   - Create Grafana dashboards
   - Configure alerts

4. **Refactor InferenceOrchestrator**
   - Similar pattern to AtomIngestionService
   - Use EnsembleInferencePipelineFactory

---

## Success Checklist

After completing these steps, you should have:

- [x] Pipeline services registered in DI
- [x] OpenTelemetry exporting traces and metrics
- [x] AtomIngestionServiceAdapter working
- [x] Integration tests passing
- [x] Metrics endpoint returning pipeline metrics
- [x] Application running with new architecture
- [x] Backward compatibility maintained

**Total time:** 4-6 hours for basic integration

**You're now ready to proceed with gradual rollout!** 🚀

---

## Quick Reference: Key Files

| File | Purpose | Lines |
|------|---------|-------|
| `Program.cs` | DI registration, OpenTelemetry config | +50 |
| `AtomIngestionServiceAdapter.cs` | Backward compatibility adapter | 85 |
| `appsettings.json` | Configuration | +15 |
| `AtomIngestionPipelineTests.cs` | Integration tests | 80 |

**Total new code:** ~230 lines to integrate entire pipeline architecture!
