# Hartonomous Architecture Patterns

> **Last Updated:** November 1, 2025  
> **Purpose:** Separation of concerns, reusability, and centralized library architecture

## Overview

This document defines architectural patterns for the Hartonomous platform to ensure:

1. **Thin Services** - Business logic delegates to reusable libraries
2. **Separation of Concerns** - Each component has a single, well-defined responsibility
3. **Centralized Libraries** - Common functionality lives in shared utilities
4. **Testability** - Dependencies injected, logic isolated, mocks enabled
5. **Reusability** - Write once, use everywhere

---

## Current Architecture Assessment

### ✅ What's Working Well

| Component | Location | Purpose |
|-----------|----------|---------|
| `VectorUtilities` | `Hartonomous.Core.Utilities` | Centralized vector operations |
| `HashUtility` | `Hartonomous.Core.Utilities` | SHA256 hashing for deduplication |
| `BaseService` | `Hartonomous.Core.Services` | Common service base class |
| `VectorUtility` | `Hartonomous.Core.Utilities` | SQL Server vector interop |
| `GeometryConverter` | `Hartonomous.Core.Utilities` | Spatial geometry helpers |

### ⚠️ Areas for Improvement

| Issue | Location | Impact |
|-------|----------|--------|
| **Helper classes in services** | `EventEnricher.cs` | Not reusable, hard to test |
| **Static helpers in wrong layer** | `ModelCapabilityInference`, `InferenceMetadataHelper` | Business logic in Infrastructure |
| **Duplicate configuration logic** | Multiple services | Repeated Environment.GetEnvironmentVariable calls |
| **Embedded business rules** | Service classes | Mixed with orchestration logic |
| **No base classes for common patterns** | Event processors, publishers | Repeated validation/error handling |

---

## Proposed Architecture

### Layer Responsibilities

```
┌─────────────────────────────────────────────────────────┐
│                   Application Layer                     │
│  (Services, Processors, Consumers, Background Services) │
│  • Orchestration only                                   │
│  • Delegates to Core domain logic                       │
│  • Handles infrastructure concerns (logging, metrics)   │
└─────────────────────────────────────────────────────────┘
                           ↓ depends on
┌─────────────────────────────────────────────────────────┐
│                    Domain Layer (Core)                  │
│  • Business logic & domain rules                        │
│  • Domain services                                      │
│  • Entities & value objects                             │
│  • Domain-specific utilities                            │
└─────────────────────────────────────────────────────────┘
                           ↓ depends on
┌─────────────────────────────────────────────────────────┐
│              Shared Kernel (Core.Utilities)             │
│  • Pure functions (no dependencies)                     │
│  • Data structure operations (vectors, geometry)        │
│  • Common algorithms (hashing, encoding)                │
│  • Validation & conversion helpers                      │
└─────────────────────────────────────────────────────────┘
```

---

## Pattern 1: Extract Helper Classes to Domain Services

### ❌ Current (Helpers Embedded in Infrastructure)

```csharp
// src/Hartonomous.Infrastructure/Services/Enrichment/EventEnricher.cs
public class EventEnricher : IEventEnricher
{
    // Service logic mixed with helper classes...
}

// Helper classes at bottom of same file
public static class ModelCapabilityInference
{
    public static string[] InferCapabilities(string modelName)
    {
        // Business logic for model inference
    }
}

public static class InferenceMetadataHelper
{
    public static string DetermineReasoningMode(string taskType)
    {
        // Business logic for reasoning modes
    }
}
```

**Problems:**
- Business logic trapped in Infrastructure layer
- Can't use these helpers in other services
- Hard to unit test in isolation
- Violates single responsibility principle

### ✅ Proposed (Domain Services in Core)

```csharp
// src/Hartonomous.Core/Services/ModelCapabilityService.cs
namespace Hartonomous.Core.Services;

/// <summary>
/// Domain service for inferring model capabilities from names and metadata.
/// Encapsulates business rules for model classification and capability detection.
/// </summary>
public interface IModelCapabilityService
{
    string[] InferCapabilities(string modelName);
    string ClassifyModelType(string modelName);
    double EstimatePerformance(string modelName);
    string[] GetComplianceRequirements(string modelName);
}

public class ModelCapabilityService : IModelCapabilityService
{
    // Testable, reusable, injectable implementation
    public string[] InferCapabilities(string modelName)
    {
        var capabilities = new List<string>();
        var lowerName = modelName.ToLowerInvariant();

        if (lowerName.Contains("llama") || lowerName.Contains("gpt"))
        {
            capabilities.AddRange(new[] { "text_generation", "question_answering", "summarization" });
        }

        if (lowerName.Contains("clip") || lowerName.Contains("vision"))
        {
            capabilities.AddRange(new[] { "image_classification", "image_captioning" });
        }

        return capabilities.ToArray();
    }

    public string ClassifyModelType(string modelName)
    {
        var lowerName = modelName.ToLowerInvariant();
        
        if (lowerName.Contains("llama") || lowerName.Contains("gpt")) return "text";
        if (lowerName.Contains("clip") || lowerName.Contains("vit")) return "vision";
        if (lowerName.Contains("wav2vec") || lowerName.Contains("whisper")) return "audio";

        return "multimodal";
    }

    // ... other methods
}
```

```csharp
// src/Hartonomous.Core/Services/InferenceMetadataService.cs
namespace Hartonomous.Core.Services;

/// <summary>
/// Domain service for inference metadata generation and reasoning mode determination.
/// </summary>
public interface IInferenceMetadataService
{
    string DetermineReasoningMode(string taskType);
    string EstimateComplexity(string taskType);
    bool RequiresAuditTrail(string taskType);
    TimeSpan GetPerformanceSLA(string taskType);
}

public class InferenceMetadataService : IInferenceMetadataService
{
    public string DetermineReasoningMode(string taskType) => taskType.ToLowerInvariant() switch
    {
        "text_generation" => "generative",
        "question_answering" => "analytical",
        "classification" => "categorical",
        "summarization" => "synthetic",
        "translation" => "transformational",
        _ => "general"
    };

    // ... other methods
}
```

```csharp
// src/Hartonomous.Infrastructure/Services/Enrichment/EventEnricher.cs
// NOW THIN - just orchestrates domain services
public class EventEnricher : IEventEnricher
{
    private readonly IModelCapabilityService _capabilityService;
    private readonly IInferenceMetadataService _metadataService;
    private readonly ILogger<EventEnricher> _logger;

    public EventEnricher(
        IModelCapabilityService capabilityService,
        IInferenceMetadataService metadataService,
        ILogger<EventEnricher> logger)
    {
        _capabilityService = capabilityService;
        _metadataService = metadataService;
        _logger = logger;
    }

    private Task EnrichModelEventAsync(BaseEvent evt, CancellationToken ct)
    {
        if (evt.Data is Dictionary<string, object> data &&
            data.TryGetValue("model_name", out var modelNameObj) &&
            modelNameObj is string modelName)
        {
            // Delegate to domain services
            evt.Extensions["semantic"] = new Dictionary<string, object>
            {
                ["inferred_capabilities"] = _capabilityService.InferCapabilities(modelName),
                ["content_type"] = _capabilityService.ClassifyModelType(modelName),
                ["expected_performance"] = _capabilityService.EstimatePerformance(modelName),
                ["compliance_requirements"] = _capabilityService.GetComplianceRequirements(modelName)
            };
        }

        return Task.CompletedTask;
    }

    private Task EnrichInferenceEventAsync(BaseEvent evt, CancellationToken ct)
    {
        if (evt.Data is Dictionary<string, object> data &&
            data.TryGetValue("task_type", out var taskTypeObj) &&
            taskTypeObj is string taskType)
        {
            // Delegate to domain services
            evt.Extensions["reasoning"] = new Dictionary<string, object>
            {
                ["reasoning_mode"] = _metadataService.DetermineReasoningMode(taskType),
                ["expected_complexity"] = _metadataService.EstimateComplexity(taskType),
                ["audit_trail_required"] = _metadataService.RequiresAuditTrail(taskType),
                ["performance_sla"] = _metadataService.GetPerformanceSLA(taskType).TotalMilliseconds
            };
        }

        return Task.CompletedTask;
    }
}
```

**Benefits:**
- ✅ Domain logic in Core layer (reusable across services)
- ✅ Infrastructure services are thin orchestrators
- ✅ Easy to unit test domain services in isolation
- ✅ Easy to mock in integration tests
- ✅ Single Responsibility Principle applied

---

## Pattern 2: Base Classes for Common Patterns

### ❌ Current (Repeated Patterns)

Every event processor/consumer repeats:
- Null checking
- Exception handling
- Logging setup
- Health checks
- Disposal

### ✅ Proposed (Base Classes)

```csharp
// src/Hartonomous.Core/Services/BaseEventProcessor.cs
namespace Hartonomous.Core.Services;

/// <summary>
/// Base class for event processors with common error handling, logging, and lifecycle management.
/// </summary>
public abstract class BaseEventProcessor : BackgroundService
{
    protected ILogger Logger { get; }
    protected CancellationToken StoppingToken { get; private set; }

    protected BaseEventProcessor(ILogger logger)
    {
        Logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        StoppingToken = stoppingToken;

        Logger.LogInformation("{ProcessorName} starting...", GetType().Name);

        try
        {
            await OnStartingAsync(stoppingToken);

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await ProcessBatchAsync(stoppingToken);
                }
                catch (Exception ex) when (!IsCriticalException(ex))
                {
                    await HandleNonCriticalExceptionAsync(ex, stoppingToken);
                }
            }
        }
        catch (Exception ex)
        {
            Logger.LogCritical(ex, "{ProcessorName} encountered critical error", GetType().Name);
            throw;
        }
        finally
        {
            await OnStoppingAsync(CancellationToken.None);
            Logger.LogInformation("{ProcessorName} stopped", GetType().Name);
        }
    }

    /// <summary>
    /// Process a batch of events. Override this in derived classes.
    /// </summary>
    protected abstract Task ProcessBatchAsync(CancellationToken ct);

    /// <summary>
    /// Called when processor is starting. Override for initialization.
    /// </summary>
    protected virtual Task OnStartingAsync(CancellationToken ct) => Task.CompletedTask;

    /// <summary>
    /// Called when processor is stopping. Override for cleanup.
    /// </summary>
    protected virtual Task OnStoppingAsync(CancellationToken ct) => Task.CompletedTask;

    /// <summary>
    /// Handle non-critical exceptions (default: log and delay).
    /// </summary>
    protected virtual async Task HandleNonCriticalExceptionAsync(Exception ex, CancellationToken ct)
    {
        Logger.LogError(ex, "Non-critical error in {ProcessorName}", GetType().Name);
        await Task.Delay(TimeSpan.FromSeconds(5), ct);
    }

    /// <summary>
    /// Determine if exception is critical (should stop processor).
    /// </summary>
    protected virtual bool IsCriticalException(Exception ex)
    {
        return ex is OutOfMemoryException or StackOverflowException;
    }

    protected void LogDebug(string message, params object[] args) => Logger.LogDebug(message, args);
    protected void LogInformation(string message, params object[] args) => Logger.LogInformation(message, args);
    protected void LogWarning(string message, params object[] args) => Logger.LogWarning(message, args);
    protected void LogError(Exception ex, string message, params object[] args) => Logger.LogError(ex, message, args);
}
```

```csharp
// src/CesConsumer/Services/CdcEventProcessor.cs
// NOW - Thin, focused on CDC-specific logic
public class CdcEventProcessor : BaseEventProcessor
{
    private readonly ICdcRepository _cdcRepository;
    private readonly IEventPublisher _eventPublisher;
    private readonly IEventEnricher _enricher;
    private readonly ICdcCheckpointManager _checkpointManager;
    private string? _lastLsn;

    public CdcEventProcessor(
        ICdcRepository cdcRepository,
        IEventPublisher eventPublisher,
        IEventEnricher enricher,
        ICdcCheckpointManager checkpointManager,
        ILogger<CdcEventProcessor> logger)
        : base(logger)
    {
        _cdcRepository = cdcRepository ?? throw new ArgumentNullException(nameof(cdcRepository));
        _eventPublisher = eventPublisher ?? throw new ArgumentNullException(nameof(eventPublisher));
        _enricher = enricher ?? throw new ArgumentNullException(nameof(enricher));
        _checkpointManager = checkpointManager ?? throw new ArgumentNullException(nameof(checkpointManager));
    }

    protected override async Task OnStartingAsync(CancellationToken ct)
    {
        _lastLsn = await _checkpointManager.GetLastProcessedLsnAsync(ct);
        LogInformation("Starting from LSN: {LastLsn}", _lastLsn ?? "Beginning");
    }

    protected override async Task ProcessBatchAsync(CancellationToken ct)
    {
        var changeEvents = await _cdcRepository.GetChangeEventsSinceAsync(_lastLsn, ct);
        
        if (!changeEvents.Any())
        {
            await Task.Delay(TimeSpan.FromSeconds(1), ct);
            return;
        }

        // Convert, enrich, publish (orchestration only)
        var events = changeEvents.Select(ConvertToBaseEvent).ToList();
        await _enricher.EnrichBatchAsync(events, ct);
        await _eventPublisher.PublishBatchAsync(events, ct);

        // Update checkpoint
        var maxLsn = changeEvents.Max(e => e.Lsn);
        if (maxLsn != null)
        {
            await _checkpointManager.UpdateLastProcessedLsnAsync(maxLsn, ct);
            _lastLsn = maxLsn;
            LogInformation("Processed {Count} events, new LSN: {MaxLsn}", events.Count, maxLsn);
        }
    }

    private BaseEvent ConvertToBaseEvent(dynamic changeEvent)
    {
        // Conversion logic (could be extracted to a mapper)
        return new BaseEvent
        {
            Id = Guid.NewGuid().ToString(),
            Source = new Uri($"/sqlserver/{Environment.MachineName}/Hartonomous"),
            Type = GetEventType(changeEvent.Operation),
            Time = DateTimeOffset.UtcNow,
            Subject = $"{changeEvent.TableName}/lsn:{changeEvent.Lsn}",
            Data = changeEvent.Data,
            Extensions = new Dictionary<string, object>
            {
                ["sqlserver"] = new Dictionary<string, object>
                {
                    ["operation"] = GetOperationName(changeEvent.Operation),
                    ["table"] = changeEvent.TableName,
                    ["lsn"] = changeEvent.Lsn,
                    ["database"] = "Hartonomous"
                }
            }
        };
    }

    private static string GetEventType(int operation) => operation switch
    {
        1 => "com.microsoft.sqlserver.cdc.delete",
        2 => "com.microsoft.sqlserver.cdc.insert",
        3 => "com.microsoft.sqlserver.cdc.update.before",
        4 => "com.microsoft.sqlserver.cdc.update.after",
        _ => "com.microsoft.sqlserver.cdc.unknown"
    };

    private static string GetOperationName(int operation) => operation switch
    {
        1 => "delete",
        2 => "insert",
        3 => "update_before",
        4 => "update_after",
        _ => "unknown"
    };
}
```

**Benefits:**
- ✅ 50% less code in derived processors
- ✅ Consistent error handling across all processors
- ✅ Standardized logging patterns
- ✅ Easier to add metrics/telemetry in one place
- ✅ Testability through protected virtual methods

---

## Pattern 3: Centralized Configuration Helpers

### ❌ Current (Repeated Configuration Logic)

```csharp
// Repeated in many services:
var connectionString = Environment.GetEnvironmentVariable("EVENTHUB_CONNECTION_STRING")
    ?? "Endpoint=sb://localhost;...";

var eventHubName = Environment.GetEnvironmentVariable("EVENTHUB_NAME") ?? "default";
```

### ✅ Proposed (Centralized Configuration Service)

```csharp
// src/Hartonomous.Core/Services/ConfigurationService.cs
namespace Hartonomous.Core.Services;

/// <summary>
/// Centralized configuration management with environment variable fallback.
/// </summary>
public interface IConfigurationService
{
    string GetRequired(string key);
    string GetOptional(string key, string defaultValue);
    T GetRequired<T>(string key, Func<string, T> parser);
    T GetOptional<T>(string key, T defaultValue, Func<string, T> parser);
    string GetConnectionString(string name);
}

public class ConfigurationService : IConfigurationService
{
    private readonly IConfiguration _configuration;

    public ConfigurationService(IConfiguration configuration)
    {
        _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
    }

    public string GetRequired(string key)
    {
        // Check appsettings first, then environment
        var value = _configuration[key] ?? Environment.GetEnvironmentVariable(key);
        
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new InvalidOperationException($"Required configuration '{key}' not found");
        }

        return value;
    }

    public string GetOptional(string key, string defaultValue)
    {
        return _configuration[key] 
            ?? Environment.GetEnvironmentVariable(key) 
            ?? defaultValue;
    }

    public T GetRequired<T>(string key, Func<string, T> parser)
    {
        var value = GetRequired(key);
        try
        {
            return parser(value);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException(
                $"Failed to parse configuration '{key}' with value '{value}'", ex);
        }
    }

    public T GetOptional<T>(string key, T defaultValue, Func<string, T> parser)
    {
        var value = _configuration[key] ?? Environment.GetEnvironmentVariable(key);
        
        if (string.IsNullOrWhiteSpace(value))
        {
            return defaultValue;
        }

        try
        {
            return parser(value);
        }
        catch
        {
            return defaultValue;
        }
    }

    public string GetConnectionString(string name)
    {
        // Check ConnectionStrings section first
        var connString = _configuration.GetConnectionString(name);
        
        // Fall back to environment variable
        if (string.IsNullOrWhiteSpace(connString))
        {
            connString = Environment.GetEnvironmentVariable($"{name}_CONNECTION_STRING");
        }

        if (string.IsNullOrWhiteSpace(connString))
        {
            throw new InvalidOperationException($"Connection string '{name}' not found");
        }

        return connString;
    }
}
```

**Usage:**
```csharp
// In EventHubPublisher constructor
var connectionString = _configService.GetConnectionString("EventHub");
var maxRetries = _configService.GetOptional("EventHub:MaxRetries", 3, int.Parse);
```

---

## Pattern 4: Validation Utilities

### ✅ Proposed (Centralized Validation)

```csharp
// src/Hartonomous.Core/Utilities/ValidationUtility.cs
namespace Hartonomous.Core.Utilities;

/// <summary>
/// Common validation helpers to reduce boilerplate.
/// </summary>
public static class ValidationUtility
{
    /// <summary>
    /// Ensures value is not null, throws ArgumentNullException if null.
    /// </summary>
    public static T NotNull<T>(T value, string paramName) where T : class
    {
        return value ?? throw new ArgumentNullException(paramName);
    }

    /// <summary>
    /// Ensures string is not null or whitespace.
    /// </summary>
    public static string NotNullOrWhiteSpace(string value, string paramName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException($"Value cannot be null or whitespace", paramName);
        }
        return value;
    }

    /// <summary>
    /// Ensures collection is not null or empty.
    /// </summary>
    public static IEnumerable<T> NotNullOrEmpty<T>(IEnumerable<T> value, string paramName)
    {
        if (value == null || !value.Any())
        {
            throw new ArgumentException($"Collection cannot be null or empty", paramName);
        }
        return value;
    }

    /// <summary>
    /// Ensures value is within range.
    /// </summary>
    public static T InRange<T>(T value, T min, T max, string paramName) where T : IComparable<T>
    {
        if (value.CompareTo(min) < 0 || value.CompareTo(max) > 0)
        {
            throw new ArgumentOutOfRangeException(paramName, 
                $"Value must be between {min} and {max}");
        }
        return value;
    }

    /// <summary>
    /// Validates vector dimensions.
    /// </summary>
    public static float[] ValidateVectorDimensions(float[] vector, int expectedDimension, string paramName)
    {
        NotNull(vector, paramName);
        
        if (vector.Length != expectedDimension)
        {
            throw new ArgumentException(
                $"Vector dimension mismatch. Expected {expectedDimension}, got {vector.Length}", 
                paramName);
        }

        if (!VectorUtilities.IsValid(vector))
        {
            throw new ArgumentException("Vector contains NaN or Infinity values", paramName);
        }

        return vector;
    }
}
```

**Usage:**
```csharp
// Before:
public EventEnricher(ILogger<EventEnricher> logger)
{
    _logger = logger ?? throw new ArgumentNullException(nameof(logger));
}

// After:
public EventEnricher(ILogger<EventEnricher> logger)
{
    _logger = ValidationUtility.NotNull(logger, nameof(logger));
}
```

---

## Pattern 5: Event Conversion Mappers

### ❌ Current (Conversion Logic in Services)

```csharp
// Embedded in CdcEventProcessor
private BaseEvent ConvertToBaseEvent(dynamic changeEvent)
{
    return new BaseEvent { /* 20 lines of mapping logic */ };
}
```

### ✅ Proposed (Dedicated Mappers)

```csharp
// src/Hartonomous.Core/Mappers/IEventMapper.cs
namespace Hartonomous.Core.Mappers;

/// <summary>
/// Maps from source events to platform BaseEvent format.
/// </summary>
public interface IEventMapper<TSource>
{
    BaseEvent Map(TSource source);
    IEnumerable<BaseEvent> MapBatch(IEnumerable<TSource> sources);
}

// src/Hartonomous.Infrastructure/Mappers/CdcEventMapper.cs
namespace Hartonomous.Infrastructure.Mappers;

/// <summary>
/// Maps SQL Server CDC change events to BaseEvent format.
/// </summary>
public class CdcEventMapper : IEventMapper<CdcChangeEvent>
{
    private readonly string _machineName;
    private readonly string _database;

    public CdcEventMapper()
    {
        _machineName = Environment.MachineName;
        _database = "Hartonomous";
    }

    public BaseEvent Map(CdcChangeEvent changeEvent)
    {
        ValidationUtility.NotNull(changeEvent, nameof(changeEvent));

        return new BaseEvent
        {
            Id = Guid.NewGuid().ToString(),
            Source = new Uri($"/sqlserver/{_machineName}/{_database}"),
            Type = MapOperationType(changeEvent.Operation),
            Time = DateTimeOffset.UtcNow,
            Subject = $"{changeEvent.TableName}/lsn:{changeEvent.Lsn}",
            DataSchema = new Uri("https://schemas.microsoft.com/sqlserver/2025/ces"),
            Data = changeEvent.Data,
            Extensions = new Dictionary<string, object>
            {
                ["sqlserver"] = new Dictionary<string, object>
                {
                    ["operation"] = MapOperationName(changeEvent.Operation),
                    ["table"] = changeEvent.TableName,
                    ["lsn"] = changeEvent.Lsn,
                    ["database"] = _database,
                    ["server"] = _machineName
                }
            }
        };
    }

    public IEnumerable<BaseEvent> MapBatch(IEnumerable<CdcChangeEvent> sources)
    {
        return sources.Select(Map);
    }

    private static string MapOperationType(int operation) => operation switch
    {
        1 => "com.microsoft.sqlserver.cdc.delete",
        2 => "com.microsoft.sqlserver.cdc.insert",
        3 => "com.microsoft.sqlserver.cdc.update.before",
        4 => "com.microsoft.sqlserver.cdc.update.after",
        _ => "com.microsoft.sqlserver.cdc.unknown"
    };

    private static string MapOperationName(int operation) => operation switch
    {
        1 => "delete",
        2 => "insert",
        3 => "update_before",
        4 => "update_after",
        _ => "unknown"
    };
}
```

**Usage:**
```csharp
// CdcEventProcessor becomes even thinner
protected override async Task ProcessBatchAsync(CancellationToken ct)
{
    var changeEvents = await _cdcRepository.GetChangeEventsSinceAsync(_lastLsn, ct);
    if (!changeEvents.Any()) return;

    // Delegate to mapper
    var events = _eventMapper.MapBatch(changeEvents).ToList();
    
    await _enricher.EnrichBatchAsync(events, ct);
    await _eventPublisher.PublishBatchAsync(events, ct);

    // Update checkpoint...
}
```

---

## Recommended Library Structure

```
src/
├── Hartonomous.Core/
│   ├── Abstracts/
│   │   ├── IEventPublisher.cs
│   │   ├── IEventConsumer.cs
│   │   └── IEventEnricher.cs
│   ├── Services/
│   │   ├── BaseService.cs ✨ NEW
│   │   ├── BaseEventProcessor.cs ✨ NEW
│   │   ├── ModelCapabilityService.cs ✨ NEW
│   │   ├── InferenceMetadataService.cs ✨ NEW
│   │   └── ConfigurationService.cs ✨ NEW
│   ├── Mappers/
│   │   └── IEventMapper.cs ✨ NEW
│   ├── Utilities/
│   │   ├── VectorUtilities.cs ✅ EXISTING
│   │   ├── HashUtility.cs ✅ EXISTING
│   │   ├── VectorUtility.cs ✅ EXISTING
│   │   ├── ValidationUtility.cs ✨ NEW
│   │   └── JsonUtility.cs ✨ NEW
│   └── Extensions/
│       └── ServiceCollectionExtensions.cs ✨ NEW
│
└── Hartonomous.Infrastructure/
    ├── Services/
    │   ├── Enrichment/
    │   │   └── EventEnricher.cs (NOW THIN)
    │   └── Messaging/
    │       ├── EventHubPublisher.cs
    │       └── EventHubConsumer.cs
    └── Mappers/
        └── CdcEventMapper.cs ✨ NEW
```

---

## Summary: Benefits of Centralized Architecture

| Before | After | Benefit |
|--------|-------|---------|
| Helper classes in service files | Domain services in Core | Reusable across all services |
| Repeated validation logic | ValidationUtility | DRY principle, consistent errors |
| Embedded configuration code | ConfigurationService | Centralized, testable config |
| Mixed concerns in services | Thin orchestrators + domain logic | Easier to test, maintain |
| No base classes | BaseEventProcessor, BaseService | Less boilerplate, consistent patterns |
| Mapping logic in processors | Dedicated mappers | Single responsibility, testable |

### Metrics Improvement Estimate

| Metric | Current | With Patterns | Improvement |
|--------|---------|---------------|-------------|
| **Lines per Service** | 200-400 | 50-150 | -60% |
| **Code Duplication** | High | Low | -70% |
| **Test Coverage** | 45% | 90%+ | +45% |
| **Reusability** | Low | High | +300% |
| **Maintainability** | Medium | High | +50% |

---

## Implementation Roadmap

### Phase 1: Extract Domain Services (1-2 days)
1. Create `IModelCapabilityService` + implementation
2. Create `IInferenceMetadataService` + implementation
3. Refactor `EventEnricher` to use injected services
4. Write unit tests for domain services

### Phase 2: Add Base Classes (1 day)
1. Create `BaseEventProcessor` 
2. Refactor `CdcEventProcessor` to inherit
3. Refactor `EventProcessor` (Neo4jSync) to inherit
4. Add `ConfigurationService`

### Phase 3: Centralize Utilities (1 day)
1. Create `ValidationUtility`
2. Create `JsonUtility` (for consistent serialization)
3. Update services to use utilities
4. Remove duplicated validation code

### Phase 4: Add Mappers (1 day)
1. Create `IEventMapper<T>` interface
2. Create `CdcEventMapper`
3. Refactor event processors to use mappers
4. Write mapper unit tests

---

**Document History:**
- 2025-11-01: Initial creation - Architecture patterns for separation of concerns
