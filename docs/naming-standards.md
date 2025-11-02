# Hartonomous Naming Standards

> **Last Updated:** November 1, 2025  
> **Purpose:** Standardized naming conventions across the Hartonomous codebase

## Overview

This document defines the naming conventions used throughout the Hartonomous platform. These standards ensure consistency, readability, and maintainability across all projects.

## Core Principles

1. **Clarity over Brevity**: Names should be self-explanatory
2. **Domain Language**: Use business/domain terms over technical jargon
3. **Consistency**: Similar concepts use similar naming patterns
4. **Standard Compliance**: Follow C# and .NET conventions

---

## Event System Naming

### BaseEvent Model

The **`BaseEvent`** class is the foundation for all events in the platform.

```csharp
// ✅ GOOD - Centralized in Hartonomous.Core.Models
using Hartonomous.Core.Models;

public class BaseEvent
{
    public string Id { get; set; }
    public Uri Source { get; set; }
    public string Type { get; set; }
    // ... follows CloudEvents v1.0 spec
}
```

**Rationale:**
- `BaseEvent` clearly indicates it's a foundation class for inheritance
- Avoids confusion with "Cloud" terminology (sounds infrastructure-specific)
- Follows classic OOP naming conventions (`BaseClass`, `AbstractBase`, etc.)
- Still maintains CloudEvents v1.0 spec compliance internally

**❌ AVOID:**
- `CloudEvent` - Sounds cloud-infrastructure-specific
- `Event` - Too generic, conflicts with System.Event
- `PlatformEvent` - Ties to platform instead of being generic
- `CNCFEvent` - Acronym, less recognizable

### Event Variables and Parameters

```csharp
// ✅ GOOD - Clear, concise variable names
public async Task ProcessAsync(BaseEvent evt)
{
    _logger.LogInformation("Processing event {Id}", evt.Id);
}

// ✅ GOOD - Collections
public async Task ProcessBatchAsync(IEnumerable<BaseEvent> events)
{
    foreach (var evt in events)
    {
        await ProcessAsync(evt);
    }
}

// ❌ AVOID - Verbose
public async Task ProcessAsync(BaseEvent baseEvent) // Redundant
public async Task ProcessAsync(BaseEvent cloudEvent) // Old naming

// ❌ AVOID - Too generic
public async Task ProcessAsync(BaseEvent e) // Too short
public async Task ProcessAsync(BaseEvent item) // Not specific
```

---

## Interface Naming

### Pattern: `I[Domain][Action]`

Interfaces follow the pattern: `I` + Domain Concept + Action/Capability

```csharp
// ✅ GOOD - Event-related interfaces
public interface IEventPublisher
public interface IEventConsumer
public interface IEventEnricher

// ✅ GOOD - CDC-related interfaces
public interface ICdcRepository
public interface ICdcCheckpointManager

// ✅ GOOD - Domain-specific interfaces
public interface IKnowledgeRepository
public interface IModelRepository
public interface IInferenceService

// ❌ AVOID
public interface ICloudEventPublisher // "Cloud" prefix removed
public interface IPublisher // Too generic, what does it publish?
public interface IEventService // "Service" is vague
```

### Interface Method Naming

```csharp
// ✅ GOOD - Async methods with clear intent
Task PublishAsync(BaseEvent evt, CancellationToken ct);
Task EnrichAsync(BaseEvent evt, CancellationToken ct);
Task<string?> GetLastProcessedLsnAsync(CancellationToken ct);

// ✅ GOOD - Batch operations
Task PublishBatchAsync(IEnumerable<BaseEvent> events, CancellationToken ct);
Task EnrichBatchAsync(IEnumerable<BaseEvent> events, CancellationToken ct);

// ❌ AVOID - Inconsistent naming
Task Publish(BaseEvent evt); // Missing Async suffix
Task ProcessEvents(List<BaseEvent> events); // Should be ProcessEventsAsync
```

---

## Implementation Naming

### Pattern: `[Technology][Domain][Action]`

Implementations follow: Technology/Infrastructure + Domain + Action

```csharp
// ✅ GOOD - Technology-prefixed implementations
public class EventHubPublisher : IEventPublisher
public class EventHubConsumer : IEventConsumer
public class EventEnricher : IEventEnricher

public class SqlCdcRepository : ICdcRepository
public class SqlCdcCheckpointManager : ICdcCheckpointManager
public class FileCdcCheckpointManager : ICdcCheckpointManager

// ✅ GOOD - Neo4j implementations
public class Neo4jKnowledgeRepository : IKnowledgeRepository
public class Neo4jProvenanceGraphBuilder

// ❌ AVOID
public class CloudEventPublisher // Old naming
public class AzureEventHubPublisher // Too specific (EventHub implies Azure)
public class Publisher // Missing domain context
```

---

## Configuration Classes

### Pattern: `[Technology]Options`

Configuration classes use the Options pattern:

```csharp
// ✅ GOOD - Options classes
public class EventHubOptions
{
    public string ConnectionString { get; set; }
    public string Name { get; set; }
    public string ConsumerGroup { get; set; }
}

public class Neo4jOptions
{
    public string Uri { get; set; }
    public string Username { get; set; }
    public string Password { get; set; }
}

public class SqlServerOptions
{
    public string ConnectionString { get; set; }
    public int MaxRetryAttempts { get; set; }
}

// ❌ AVOID
public class EventHubConfig // Use "Options" suffix
public class EventHubConfiguration // Too long
public class EventHub // Missing context
```

### Configuration Sections

Configuration section names should match the Options class without "Options":

```json
{
  "EventHub": {
    "ConnectionString": "...",
    "Name": "sqlserver-ces-events"
  },
  "Neo4j": {
    "Uri": "bolt://localhost:7687",
    "Username": "neo4j"
  }
}
```

```csharp
// ✅ GOOD - Binding configuration
builder.Services.Configure<EventHubOptions>(
    builder.Configuration.GetSection("EventHub"));
```

---

## Service Class Naming

### Background Services

```csharp
// ✅ GOOD - Clear purpose
public class EventProcessor : BackgroundService
public class CdcListener : BackgroundService
public class CdcEventProcessor : BackgroundService

// ❌ AVOID
public class CloudEventProcessor // Old naming
public class RefactoredCdcListener // Sounds like temporary refactoring artifact
public class Worker // Too generic
public class Service // Too generic
```

### Hosted Services

```csharp
// ✅ GOOD - Domain-specific service names
public class CesConsumerService : IHostedService
public class ModelIngestionService : IHostedService

// ❌ AVOID
public class ConsumerService // What does it consume?
public class IngestionService // What does it ingest?
```

---

## Extension Method Classes

### Pattern: `[Domain]ServiceExtensions` or `[Domain]Extensions`

```csharp
// ✅ GOOD - Dependency injection extensions
public static class MessagingServiceExtensions
{
    public static IServiceCollection AddEventHubPublisher(...)
    public static IServiceCollection AddEventHubConsumer(...)
}

public static class Neo4jServiceExtensions
{
    public static IServiceCollection AddNeo4j(...)
}

public static class ConfigurationExtensions
{
    public static string GetConfigurationOrEnvironment(...)
}

// ❌ AVOID
public static class Extensions // Too generic
public static class ServiceExtensions // What services?
public static class Helpers // Vague
```

---

## Repository and Data Access

### Pattern: `[Technology][Domain]Repository`

```csharp
// ✅ GOOD - Clear technology and domain
public class SqlCdcRepository : ICdcRepository
public class Neo4jKnowledgeRepository : IKnowledgeRepository
public class SqlModelRepository : IModelRepository

// ❌ AVOID
public class CdcRepository // Missing technology context
public class DatabaseRepository // Too generic
public class DataAccess // Not a repository pattern
```

---

## Test Class Naming

### Pattern: `[ClassUnderTest]Tests`

```csharp
// ✅ GOOD - Clear test targets
public class BaseEventTests
public class EventEnricherTests
public class EventHubPublisherTests
public class CdcEventProcessorTests

// ✅ GOOD - Integration tests
public class EventHubPublisherIntegrationTests
public class Neo4jRepositoryIntegrationTests

// ❌ AVOID
public class RefactoredCdcListenerTests // Sounds temporary
public class EventTests // Which event?
public class UnitTests // Which unit?
public class Tests // Too generic
```

---

## Constants and Enums

### Constants

```csharp
// ✅ GOOD - PascalCase for public constants
public const string DefaultConsumerGroup = "$Default";
public const int MaxBatchSize = 100;
public const string CloudEventsSpecVersion = "1.0";

// ❌ AVOID
public const string default_consumer_group // Use PascalCase
public const string CONSUMER_GROUP // Not screaming snake case in C#
```

### Enums

```csharp
// ✅ GOOD - Singular enum names
public enum OperationType
{
    Insert,
    Update,
    Delete
}

public enum ProcessingStatus
{
    NotStarted,
    InProgress,
    Completed,
    Failed
}

// ❌ AVOID - Plural enum names
public enum OperationTypes // Should be singular
```

---

## Project Naming

```csharp
// ✅ GOOD - Current project names
Hartonomous.Core
Hartonomous.Infrastructure  
Hartonomous.Admin
CesConsumer
Neo4jSync
ModelIngestion

// Future considerations for consistency:
Hartonomous.CesConsumer (align with Core/Infrastructure pattern)
Hartonomous.Neo4jSync
Hartonomous.ModelIngestion
```

---

## Comments and Documentation

### XML Documentation

```csharp
/// <summary>
/// Base event model for all events in the Hartonomous platform.
/// Follows the CNCF CloudEvents v1.0 specification for interoperability.
/// </summary>
/// <remarks>
/// Despite the internal adherence to CloudEvents specification, we use "BaseEvent"
/// naming to avoid confusion with cloud-infrastructure terminology.
/// Specification: https://cloudevents.io/
/// </remarks>
public class BaseEvent { }
```

### Inline Comments

```csharp
// ✅ GOOD - Explain why, not what
// Convert CDC events to platform events
var events = changeEvents.Select(ConvertToBaseEvent).ToList();

// Enrich with semantic metadata for downstream processing
await _enricher.EnrichBatchAsync(events, cancellationToken);

// ❌ AVOID - Stating the obvious
// Loop through events
foreach (var evt in events) { }

// Call the method
await ProcessAsync(evt);
```

---

## Logging Messages

```csharp
// ✅ GOOD - Structured logging with context
_logger.LogInformation("Processing event {EventId} of type {EventType}", 
    evt.Id, evt.Type);

_logger.LogInformation("Published {Count} events in {BatchCount} batches", 
    events.Count, batches.Count);

// ❌ AVOID - Unstructured logging
_logger.LogInformation($"Processing event {evt.Id}"); // String interpolation
_logger.LogInformation("Processing event"); // Missing context
```

---

## Quick Reference

| Concept | Pattern | Example |
|---------|---------|---------|
| **Event Model** | `BaseEvent` | `BaseEvent` |
| **Event Variable** | `evt` | `var evt = new BaseEvent()` |
| **Event Collection** | `events` | `List<BaseEvent> events` |
| **Interface** | `I[Domain][Action]` | `IEventPublisher` |
| **Implementation** | `[Tech][Domain][Action]` | `EventHubPublisher` |
| **Options Class** | `[Tech]Options` | `EventHubOptions` |
| **Extension Class** | `[Domain]Extensions` | `MessagingServiceExtensions` |
| **Repository** | `[Tech][Domain]Repository` | `SqlCdcRepository` |
| **Test Class** | `[Class]Tests` | `BaseEventTests` |

---

## Migration from Old Names

| Old Name | New Name | Reason |
|----------|----------|--------|
| `CloudEvent` | `BaseEvent` | Avoid cloud-infrastructure confusion |
| `ICloudEventEnricher` | `IEventEnricher` | Consistency with BaseEvent |
| `CloudEventEnricher` | `EventEnricher` | Consistency with BaseEvent |
| `RefactoredCdcListener` | `CdcEventProcessor` | Remove "refactored" - sounds temporary |
| `IUnifiedEmbeddingService` | `IEmbeddingService` | Remove redundant "unified" prefix |
| `UnifiedEmbeddingService` | `EmbeddingService` | Simpler, more professional |
| `cloudEvent` (variable) | `evt` | Concise, clear |
| `cloudEvents` (collection) | `events` | Standard collection naming |

---

## Avoid Unprofessional Naming

### ❌ Names That Sound Temporary

These names make production code sound like work-in-progress:

| Unprofessional | Professional | Why It's Bad |
|----------------|--------------|--------------|
| `RefactoredX` | `X` or `XProcessor` | Suggests temporary refactoring artifact |
| `NewX` | `X` | What happens when there's a newer "New"? |
| `X2` / `XVersion2` | `X` | Version belongs in git, not class names |
| `ImprovedX` | `X` | All code should be "improved" |
| `FixedX` | `X` | Implies the original was broken |
| `TempX` | `X` | Nothing temporary should be in production |
| `TestX` (non-test) | `X` | Test code should be in test projects only |

### ❌ Overly Generic Names

| Too Generic | More Specific | Why It's Better |
|-------------|---------------|-----------------|
| `Service` | `EmbeddingService` | Tells you what it does |
| `Manager` | `CheckpointManager` | Indicates what it manages |
| `Handler` | `EventHandler` | Specifies what it handles |
| `Helper` | `ConfigurationHelper` | Explains what it helps with |
| `Util` / `Utils` | `StringUtils` | Scopes the utilities |
| `Data` | `ChangeEvent` | Describes the data type |

### ❌ Redundant Prefixes/Suffixes

| Redundant | Clean | Why |
|-----------|-------|-----|
| `UnifiedX` when there's only one X | `X` | "Unified" is redundant if there's no fragmentation |
| `BaseXBase` | `BaseX` | One "Base" is enough |
| `IInterfaceX` | `IX` | The `I` already indicates interface |
| `AbstractXAbstract` | `AbstractX` or `XBase` | Choose one pattern |
| `RepositoryRepository` | `Repository` or `XRepository` | Don't double-suffix |

### ✅ Professional Naming Guidelines

1. **Production-Ready**: Name as if it's the final version
2. **Descriptive**: Name should explain purpose without comments
3. **Consistent**: Follow established patterns in the codebase
4. **Specific**: Avoid generic terms like "Data", "Manager", "Helper"
5. **Clear Domain**: Use business/domain language
6. **No Version Numbers**: Use git for versioning, not class names
7. **No Status Prefixes**: No "New", "Old", "Temp", "Fixed", "Refactored"

---

## Future Considerations

1. **Project Reorganization**: Consider moving `CesConsumer`, `Neo4jSync`, `ModelIngestion` under `Hartonomous.*` namespace
2. **Service Suffixes**: Standardize on `Service` vs `Processor` vs `Manager`
3. **Async Suffix**: Ensure all async methods use `Async` suffix
4. **Generic Types**: Consider conventions for generic type parameters (e.g., `TEvent` vs `T`)

---

## References

- [C# Coding Conventions](https://learn.microsoft.com/en-us/dotnet/csharp/fundamentals/coding-style/coding-conventions)
- [.NET Naming Guidelines](https://learn.microsoft.com/en-us/dotnet/standard/design-guidelines/naming-guidelines)
- [CloudEvents Specification v1.0](https://cloudevents.io/)

---

**Document History:**
- 2025-11-01: Initial creation during naming standardization refactoring
- 2025-11-01: Added "Avoid Unprofessional Naming" section with anti-patterns

