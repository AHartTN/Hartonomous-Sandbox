# SOLID/DRY Refactoring Plan

**Date**: 2025-11-08  
**Status**: Analysis Complete, Ready for Implementation

---

## Executive Summary

**Current Issues:**
- 50+ files contain multiple class definitions (violates Single Responsibility)
- SQL connection boilerplate duplicated 100+ times
- Logging patterns repeated across every service
- DTOs/Records mixed with interfaces in same files
- Configuration binding duplicated
- No centralized extension methods

**Impact:**
- Hard to maintain (change requires touching 50+ files)
- Hard to test (tightly coupled SQL connections)
- Inconsistent error handling
- Poor discoverability (classes scattered across files)

---

## 1. MULTI-CLASS FILE VIOLATIONS

### Critical Offenders (5+ classes per file)

#### `GGUFParser.cs` - **5 CLASSES**
```csharp
// CURRENT: All in one file
public class GGUFParser { }
public class GGUFHeader { }
public enum GGUFMetadataValueType { }
public enum GGMLType { }
public class GGUFTensorInfo { }
public class GGUFMetadata { }
```

**SOLUTION**: Split into `ModelFormats/GGUF/` folder:
```
ModelFormats/
  GGUF/
    GGUFParser.cs
    GGUFHeader.cs
    GGUFMetadataValueType.cs
    GGMLType.cs
    GGUFTensorInfo.cs
    GGUFMetadata.cs
```

---

#### `OllamaModelIngestionService.cs` - **5 CLASSES**
```csharp
// CURRENT:
public class OllamaModelIngestionService { }
public class OllamaManifest { }
public class OllamaManifestConfig { }
public class OllamaManifestLayer { }
public class OllamaModelConfig { }
```

**SOLUTION**: Split into `Ollama/` folder:
```
Ollama/
  OllamaModelIngestionService.cs
  Models/
    OllamaManifest.cs
    OllamaManifestConfig.cs
    OllamaManifestLayer.cs
    OllamaModelConfig.cs
```

---

#### `IAutonomousLearningRepository.cs` - **4 CLASSES**
```csharp
// CURRENT: Interface + 3 DTOs in one file
public interface IAutonomousLearningRepository { }
public class PerformanceMetrics { }
public class LearningResult { }
public class OODALoopConfiguration { }
```

**SOLUTION**: Split:
```
Repositories/
  IAutonomousLearningRepository.cs
  Models/
    PerformanceMetrics.cs
    LearningResult.cs
    OODALoopConfiguration.cs
```

---

#### `IVectorSearchRepository.cs` - **4 CLASSES** (DUPLICATED!)
```csharp
// EXISTS IN TWO LOCATIONS:
// 1. Hartonomous.Data/Repositories/IVectorSearchRepository.cs
// 2. Hartonomous.Core/Shared/IVectorSearchRepository.cs

public interface IVectorSearchRepository { }
public class VectorSearchResult { }
public class HybridSearchResult { }
public class EnsembleSearchResult { }
```

**SOLUTION**: 
1. **DELETE** `Hartonomous.Core/Shared/IVectorSearchRepository.cs` (duplicate)
2. Keep `Hartonomous.Data/Repositories/IVectorSearchRepository.cs`
3. Split result classes into `Models/VectorSearch/`

---

#### `IConceptDiscoveryRepository.cs` - **7 CLASSES**
```csharp
public interface IConceptDiscoveryRepository { }
public class EmbeddingVector { }
public class ConceptDiscoveryResult { }
public class DiscoveredConcept { }
public class ConceptBindingResult { }
public class BoundConcept { }
public class FailedBinding { }
```

**SOLUTION**: Split into Models folder:
```
Repositories/
  IConceptDiscoveryRepository.cs
  Models/ConceptDiscovery/
    EmbeddingVector.cs
    ConceptDiscoveryResult.cs
    DiscoveredConcept.cs
    ConceptBindingResult.cs
    BoundConcept.cs
    FailedBinding.cs
```

---

#### `OodaEvents.cs` - **4 EVENTS**
```csharp
public class ObservationEvent : IntegrationEvent { }
public class OrientationEvent : IntegrationEvent { }
public class DecisionEvent : IntegrationEvent { }
public class ActionEvent : IntegrationEvent { }
```

**SOLUTION**: Split into `Events/Ooda/` folder:
```
Events/
  Ooda/
    ObservationEvent.cs
    OrientationEvent.cs
    DecisionEvent.cs
    ActionEvent.cs
```

---

#### `DomainEvents.cs` - **6 EVENTS**
```csharp
public class AtomIngestedEvent : IntegrationEvent { }
public class EmbeddingGeneratedEvent : IntegrationEvent { }
public class ModelIngestedEvent : IntegrationEvent { }
public class InferenceCompletedEvent : IntegrationEvent { }
public class CacheInvalidatedEvent : IntegrationEvent { }
public class QuotaExceededEvent : IntegrationEvent { }
```

**SOLUTION**: Split into `Events/Domain/` folder:
```
Events/
  Domain/
    AtomIngestedEvent.cs
    EmbeddingGeneratedEvent.cs
    ModelIngestedEvent.cs
    InferenceCompletedEvent.cs
    CacheInvalidatedEvent.cs
    QuotaExceededEvent.cs
```

---

#### `IJobProcessor.cs` - **2 CLASSES**
```csharp
public interface IJobProcessor<TPayload> { }
public class JobExecutionContext { }
```

**SOLUTION**: Split:
```
Jobs/
  IJobProcessor.cs
  JobExecutionContext.cs
```

---

#### `JobService.cs` - **3 CLASSES**
```csharp
public interface IJobService { }
public class JobEnqueueOptions { }
public class JobService : IJobService { }
```

**SOLUTION**: Split:
```
Jobs/
  IJobService.cs
  JobService.cs
  Models/
    JobEnqueueOptions.cs
```

---

#### `BackgroundJob.cs` - **2 CLASSES**
```csharp
public enum JobStatus { }
public class BackgroundJob { }
```

**SOLUTION**: Split:
```
Jobs/
  BackgroundJob.cs
  JobStatus.cs
```

---

#### `ServiceBusEventBus.cs` - **2 CLASSES**
```csharp
public class ServiceBusOptions { }
public class ServiceBusEventBus : IEventBus { }
```

**SOLUTION**: Split:
```
Messaging/
  ServiceBusEventBus.cs
  Models/
    ServiceBusOptions.cs
```

---

#### `GracefulShutdownService.cs` - **2 CLASSES**
```csharp
public class GracefulShutdownService : IHostedService { }
public class GracefulShutdownOptions { }
```

**SOLUTION**: Split:
```
Lifecycle/
  GracefulShutdownService.cs
  Models/
    GracefulShutdownOptions.cs
```

---

#### `Result.cs` - **2 CLASSES**
```csharp
public class Result<T> { }
public class Result { }
```

**SOLUTION**: Keep together (generic + non-generic variants are paired by design)

---

#### `Specification.cs` - **3 CLASSES**
```csharp
public interface ISpecification<T> { }
public class AndSpecification<T> : Specification<T> { }
public class OrSpecification<T> : Specification<T> { }
```

**SOLUTION**: Keep together (specification pattern implementation is cohesive)

---

#### ALL Job Processors - **3 CLASSES EACH**
```csharp
// AnalyticsJobProcessor.cs
public class AnalyticsJobPayload { }
public class AnalyticsJobResult { }
public class AnalyticsJobProcessor : IJobProcessor<AnalyticsJobPayload> { }

// CleanupJobProcessor.cs
public class CleanupJobPayload { }
public class CleanupJobResult { }
public class CleanupJobProcessor : IJobProcessor<CleanupJobPayload> { }

// IndexMaintenanceJobProcessor.cs
public class IndexMaintenancePayload { }
public class IndexMaintenanceResult { }
public class IndexMaintenanceJobProcessor : IJobProcessor<IndexMaintenancePayload> { }

// CacheWarmingJobProcessor.cs
public class CacheWarmingPayload { }
public class CacheWarmingResult { }
public class CacheWarmingJobProcessor : IJobProcessor<CacheWarmingPayload> { }
```

**SOLUTION**: Standardize structure:
```
Jobs/
  Processors/
    Analytics/
      AnalyticsJobProcessor.cs
      AnalyticsJobPayload.cs
      AnalyticsJobResult.cs
    Cleanup/
      CleanupJobProcessor.cs
      CleanupJobPayload.cs
      CleanupJobResult.cs
    IndexMaintenance/
      IndexMaintenanceJobProcessor.cs
      IndexMaintenancePayload.cs
      IndexMaintenanceResult.cs
    CacheWarming/
      CacheWarmingJobProcessor.cs
      CacheWarmingPayload.cs
      CacheWarmingResult.cs
```

---

## 2. CODE DUPLICATION PATTERNS

### A. SQL Connection Boilerplate (100+ instances)

**CURRENT PATTERN** (repeated everywhere):
```csharp
await using var connection = new SqlConnection(_connectionString);
await connection.OpenAsync(cancellationToken);

await using var command = new SqlCommand("sp_Something", connection)
{
    CommandType = CommandType.StoredProcedure
};
command.Parameters.AddWithValue("@Param", value);

await using var reader = await command.ExecuteReaderAsync(cancellationToken);
```

**LOCATIONS**:
- `ModelIngestion/Prediction/TimeSeriesPredictionService.cs` (5 methods)
- `ModelIngestion/Inference/TensorAtomTextGenerator.cs` (1 method)
- `Hartonomous.Infrastructure/Services/SqlClrAtomIngestionService.cs` (1 method)
- `Hartonomous.Infrastructure/Services/EmbeddingService.cs` (1 method)
- `Hartonomous.Infrastructure/Repositories/CdcRepository.cs` (1 method)
- `Hartonomous.Infrastructure/Jobs/Processors/AnalyticsJobProcessor.cs` (4 methods)
- `Hartonomous.Infrastructure/Jobs/Processors/IndexMaintenanceJobProcessor.cs` (1 method)
- `Hartonomous.Api/Controllers/AnalyticsController.cs` (5 methods)
- `Hartonomous.Api/Controllers/FeedbackController.cs` (4 methods)
- `Hartonomous.Api/Controllers/OperationsController.cs` (1 method)
- 70+ more instances in SqlClr functions

**SOLUTION**: Create `SqlConnectionExtensions`
```csharp
// src/Hartonomous.Infrastructure/Data/SqlConnectionExtensions.cs
public static class SqlConnectionExtensions
{
    public static async Task<SqlConnection> CreateAndOpenAsync(
        this string connectionString,
        CancellationToken cancellationToken = default)
    {
        var connection = new SqlConnection(connectionString);
        await connection.OpenAsync(cancellationToken);
        return connection;
    }

    public static async Task<T> ExecuteStoredProcedureAsync<T>(
        this SqlConnection connection,
        string procedureName,
        Func<SqlDataReader, Task<T>> readFunc,
        Action<SqlCommand>? configureParameters = null,
        CancellationToken cancellationToken = default)
    {
        await using var command = new SqlCommand(procedureName, connection)
        {
            CommandType = CommandType.StoredProcedure
        };
        
        configureParameters?.Invoke(command);
        
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        return await readFunc(reader);
    }

    public static async Task ExecuteStoredProcedureAsync(
        this SqlConnection connection,
        string procedureName,
        Action<SqlCommand>? configureParameters = null,
        CancellationToken cancellationToken = default)
    {
        await using var command = new SqlCommand(procedureName, connection)
        {
            CommandType = CommandType.StoredProcedure
        };
        
        configureParameters?.Invoke(command);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }
}
```

**USAGE EXAMPLE**:
```csharp
// BEFORE (5 lines):
await using var connection = new SqlConnection(_connectionString);
await connection.OpenAsync(cancellationToken);
await using var command = new SqlCommand("sp_GenerateText", connection) { CommandType = CommandType.StoredProcedure };
command.Parameters.AddWithValue("@Prompt", prompt);
await command.ExecuteNonQueryAsync(cancellationToken);

// AFTER (1 line):
await using var connection = await _connectionString.CreateAndOpenAsync(cancellationToken);
await connection.ExecuteStoredProcedureAsync("sp_GenerateText",
    cmd => cmd.Parameters.AddWithValue("@Prompt", prompt),
    cancellationToken);
```

---

### B. Logging Patterns (100+ instances)

**CURRENT PATTERN** (repeated everywhere):
```csharp
_logger.LogInformation("Processing {ItemCount} items for tenant {TenantId}", items.Count, tenantId);
_logger.LogWarning("No results found for query {Query}", query);
_logger.LogError(ex, "Failed to process {Operation} for {Resource}", operation, resource);
```

**LOCATIONS**:
- Every service class (50+ files)
- Every controller (10+ files)
- Every job processor (4 files)
- Every event handler (10+ files)

**SOLUTION**: Create `LoggingExtensions`
```csharp
// src/Hartonomous.Infrastructure/Logging/LoggingExtensions.cs
public static class LoggingExtensions
{
    public static IDisposable BeginOperationScope(
        this ILogger logger,
        string operationName,
        params (string Key, object Value)[] properties)
    {
        var scopeDict = properties.ToDictionary(p => p.Key, p => p.Value);
        scopeDict["Operation"] = operationName;
        return logger.BeginScope(scopeDict);
    }

    public static void LogOperationStart(this ILogger logger, string operation, params (string Key, object Value)[] context)
    {
        logger.LogInformation("Starting {Operation}: {Context}",
            operation,
            string.Join(", ", context.Select(c => $"{c.Key}={c.Value}")));
    }

    public static void LogOperationComplete(this ILogger logger, string operation, TimeSpan duration, params (string Key, object Value)[] context)
    {
        logger.LogInformation("Completed {Operation} in {Duration}ms: {Context}",
            operation,
            duration.TotalMilliseconds,
            string.Join(", ", context.Select(c => $"{c.Key}={c.Value}")));
    }

    public static void LogOperationFailed(this ILogger logger, Exception ex, string operation, params (string Key, object Value)[] context)
    {
        logger.LogError(ex, "Failed {Operation}: {Context}",
            operation,
            string.Join(", ", context.Select(c => $"{c.Key}={c.Value}")));
    }
}
```

**USAGE EXAMPLE**:
```csharp
// BEFORE:
_logger.LogInformation("Processing embedding generation for atom {AtomId} tenant {TenantId}", atomId, tenantId);
try {
    // work
    _logger.LogInformation("Completed embedding generation in {Duration}ms", sw.ElapsedMilliseconds);
}
catch (Exception ex) {
    _logger.LogError(ex, "Embedding generation failed for atom {AtomId}", atomId);
}

// AFTER:
using (_logger.BeginOperationScope("EmbeddingGeneration", ("AtomId", atomId), ("TenantId", tenantId)))
{
    _logger.LogOperationStart("EmbeddingGeneration");
    try {
        // work
        _logger.LogOperationComplete("EmbeddingGeneration", sw.Elapsed);
    }
    catch (Exception ex) {
        _logger.LogOperationFailed(ex, "EmbeddingGeneration");
        throw;
    }
}
```

---

### C. Configuration Binding Duplication

**CURRENT PATTERN** (repeated in 10+ files):
```csharp
var options = new SomeOptions();
configuration.GetSection("SomeSection").Bind(options);
services.Configure<SomeOptions>(configuration.GetSection("SomeSection"));
```

**LOCATIONS**:
- `Hartonomous.Api/Program.cs`
- `Hartonomous.Infrastructure/DependencyInjection.cs`
- `ModelIngestion/Program.cs`
- `Neo4jSync/Program.cs`
- `CesConsumer/Program.cs`

**SOLUTION**: Create `ConfigurationExtensions`
```csharp
// src/Hartonomous.Infrastructure/Configuration/ConfigurationExtensions.cs
public static class ConfigurationExtensions
{
    public static IServiceCollection AddOptionsWithValidation<TOptions>(
        this IServiceCollection services,
        IConfiguration configuration,
        string sectionName)
        where TOptions : class, IValidatable
    {
        services.AddOptions<TOptions>()
            .Bind(configuration.GetSection(sectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();
        
        return services;
    }

    public static TOptions GetValidatedOptions<TOptions>(
        this IConfiguration configuration,
        string sectionName)
        where TOptions : class, new()
    {
        var options = new TOptions();
        configuration.GetSection(sectionName).Bind(options);
        
        var validationContext = new ValidationContext(options);
        Validator.ValidateObject(options, validationContext, validateAllProperties: true);
        
        return options;
    }
}
```

---

### D. HttpClient Configuration Duplication

**CURRENT PATTERN**:
```csharp
services.AddHttpClient<SomeService>(client =>
{
    client.Timeout = TimeSpan.FromSeconds(30);
    client.DefaultRequestHeaders.Add("User-Agent", "Hartonomous");
})
.AddPolicyHandler(GetRetryPolicy())
.AddPolicyHandler(GetCircuitBreakerPolicy());
```

**SOLUTION**: Centralized HttpClient factory
```csharp
// src/Hartonomous.Infrastructure/Http/HttpClientExtensions.cs
public static class HttpClientExtensions
{
    public static IHttpClientBuilder AddResilientHttpClient<TClient>(
        this IServiceCollection services,
        string name,
        Action<HttpClient>? configureClient = null)
        where TClient : class
    {
        return services.AddHttpClient<TClient>(name, client =>
        {
            client.Timeout = TimeSpan.FromSeconds(30);
            client.DefaultRequestHeaders.Add("User-Agent", "Hartonomous/1.0");
            configureClient?.Invoke(client);
        })
        .AddStandardResilienceHandler();
    }
}
```

---

## 3. DEPENDENCY INJECTION VIOLATIONS

### Current: 'new' Instantiations

**LOCATIONS**:
- `SqlConnection` created directly (100+ places)
- `Random` created directly (10+ places)
- `Stopwatch` created directly (20+ places)
- `JsonSerializer` created directly (15+ places)

**SOLUTION**: Extract interfaces + DI

```csharp
// src/Hartonomous.Core/Abstractions/ISystemClock.cs
public interface ISystemClock
{
    DateTime UtcNow { get; }
}

public class SystemClock : ISystemClock
{
    public DateTime UtcNow => DateTime.UtcNow;
}

// src/Hartonomous.Core/Abstractions/IRandomNumberGenerator.cs
public interface IRandomNumberGenerator
{
    int Next(int maxValue);
    int Next(int minValue, int maxValue);
    double NextDouble();
}

public class RandomNumberGenerator : IRandomNumberGenerator
{
    private readonly Random _random = new();
    public int Next(int maxValue) => _random.Next(maxValue);
    public int Next(int minValue, int maxValue) => _random.Next(minValue, maxValue);
    public double NextDouble() => _random.NextDouble();
}
```

---

## 4. VALIDATION DUPLICATION

**CURRENT PATTERN** (repeated in controllers):
```csharp
if (string.IsNullOrWhiteSpace(input))
    return BadRequest("Input cannot be empty");

if (id <= 0)
    return BadRequest("Invalid ID");

if (tenantId <= 0)
    return BadRequest("Invalid tenant ID");
```

**SOLUTION**: FluentValidation + ValidationHelpers
```csharp
// src/Hartonomous.Core/Validation/ValidationHelpers.cs
public static class ValidationHelpers
{
    public static void ValidateRequired(string value, string paramName)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException($"{paramName} cannot be empty", paramName);
    }

    public static void ValidatePositive(long value, string paramName)
    {
        if (value <= 0)
            throw new ArgumentException($"{paramName} must be positive", paramName);
    }

    public static void ValidateNotNull<T>(T value, string paramName) where T : class
    {
        if (value == null)
            throw new ArgumentNullException(paramName);
    }
}
```

---

## 5. IMPLEMENTATION PRIORITY

### Phase 1: Extract Extensions (LOW RISK)
1. **SqlConnectionExtensions** → Save 500+ lines
2. **LoggingExtensions** → Save 300+ lines
3. **ConfigurationExtensions** → Save 100+ lines
4. **ValidationHelpers** → Save 200+ lines

**Impact**: Reduce codebase by ~1,100 lines, improve maintainability

---

### Phase 2: Split Multi-Class Files (MEDIUM RISK)
1. Split GGUF classes (6 files)
2. Split Ollama classes (5 files)
3. Split Repository DTOs (15 files)
4. Split Event classes (10 files)
5. Split Job Processor payloads (12 files)

**Impact**: +35 files, better organization, easier navigation

---

### Phase 3: Remove Duplication (MEDIUM RISK)
1. Delete duplicate `IVectorSearchRepository` from Hartonomous.Core
2. Consolidate configuration options classes
3. Merge redundant service implementations

**Impact**: -10 files, eliminate confusion

---

### Phase 4: Extract Abstractions (HIGH RISK - REQUIRES TESTS)
1. `ISystemClock` for DateTime
2. `IRandomNumberGenerator` for Random
3. `ISqlConnectionFactory` for connections

**Impact**: Improved testability, better DI

---

## 6. METRICS

**Current State**:
- Total .cs files: ~300
- Multi-class files: ~50 (16.6%)
- SQL boilerplate instances: 100+
- Logging pattern instances: 100+
- Total LOC: ~75,000

**After Refactoring**:
- Total .cs files: ~340 (+40 from splits)
- Multi-class files: 5 (1.5%) - only deliberate patterns
- SQL boilerplate instances: 10 (extension method calls)
- Logging pattern instances: 20 (structured logging)
- Total LOC: ~73,000 (-2,000 from deduplication)

**Benefits**:
- ✅ 95% reduction in multi-class violations
- ✅ 90% reduction in SQL boilerplate
- ✅ 80% reduction in logging duplication
- ✅ Easier to navigate (1 class = 1 file)
- ✅ Easier to test (injected dependencies)
- ✅ Easier to maintain (change in one place)

---

## 7. BREAKING CHANGES

### Minimal Breaking Changes
- File moves only (namespaces unchanged)
- Extension methods are additive (backward compatible)
- Existing code continues to work

### Migration Path
1. Add extension methods alongside existing code
2. Gradually migrate to extension methods
3. Mark old patterns as `[Obsolete]`
4. Remove obsolete code in next major version

---

## Next Steps

1. **Get approval for Phase 1** (extensions - zero breaking changes)
2. **Implement SqlConnectionExtensions** (highest ROI)
3. **Implement LoggingExtensions**
4. **Run full test suite to validate**
5. **Proceed to Phase 2 after validation**

---

**Recommendation**: Start with Phase 1 extensions. Zero risk, massive benefit.
