# Phase 5: Consolidation (DRY/SOLID Refactoring)

**Priority**: MEDIUM
**Estimated Time**: 8-12 hours
**Dependencies**: Phase 4 complete (all files integrated, build working)

## Overview

Apply generic patterns to reduce code duplication. From TODO_BACKUP.md Phases 1-6.

---

## Task 5.1: Generic Repository Pattern

**Status**: ❌ NOT STARTED
**Source**: TODO_BACKUP.md Phase 1

### Problem
6 duplicate atomic repositories:
- IAtomicAudioSampleRepository → AtomicAudioSampleRepository
- IAtomicPixelRepository → AtomicPixelRepository
- IAtomicTextTokenRepository → AtomicTextTokenRepository
- 3 more variants

### Solution

**Step 1**: IAtomicRepository already created but not integrated
- File exists: `src/Hartonomous.Core/Interfaces/IAtomicRepository.cs`
- Generic interface: `IAtomicRepository<TEntity, TKey>`

**Step 2**: Create generic implementation
```csharp
public class AtomicRepository<TEntity, TKey> : IAtomicRepository<TEntity, TKey>
    where TEntity : class, IAtomicEntity<TKey>
{
    // Implement once, reuse for all atomic types
}
```

**Step 3**: Update DI registration
```csharp
services.AddScoped<IAtomicRepository<AudioSample, long>, AtomicRepository<AudioSample, long>>();
services.AddScoped<IAtomicRepository<Pixel, long>, AtomicRepository<Pixel, long>>();
services.AddScoped<IAtomicRepository<TextToken, long>, AtomicRepository<TextToken, long>>();
```

**Step 4**: Delete old interfaces + implementations (6 files)

### Estimated Reduction
6 files → 2 files (interface + implementation)

---

## Task 5.2: Generic Search Services

**Status**: ❌ NOT STARTED
**Source**: TODO_BACKUP.md Phase 2

### Problem
Duplicate search service implementations:
- ISemanticSearchService / SemanticSearchService
- ISpatialSearchService / SpatialSearchService

### Solution

Create `ISearchService<TQuery, TResult>`:
```csharp
public interface ISearchService<TQuery, TResult>
{
    Task<IEnumerable<TResult>> SearchAsync(TQuery query, CancellationToken ct);
}

// Concrete types:
public class VectorQuery { public float[] Vector { get; set; } }
public class SpatialQuery { public SqlGeometry Region { get; set; } }
public class AtomEmbeddingSearchResult { /* ... */ }

// Implementations:
public class SemanticSearchService : ISearchService<VectorQuery, AtomEmbeddingSearchResult> { }
public class SpatialSearchService : ISearchService<SpatialQuery, AtomEmbeddingSearchResult> { }
```

### Estimated Reduction
4+ files → 2 files + generic interface

---

## Task 5.3: Generic Embedder Pattern

**Status**: ❌ NOT STARTED
**Source**: TODO_BACKUP.md Phase 3

### Problem
4 modality-specific embedders with identical structure:
- ITextEmbedder / TextEmbedder
- IImageEmbedder / ImageEmbedder
- IAudioEmbedder / AudioEmbedder
- IVideoEmbedder / VideoEmbedder

### Solution

Check if can use:
```csharp
public interface IEmbedder<TInput>
{
    Task<float[]> EmbedAsync(TInput input, CancellationToken ct);
}

// OR with output type:
public interface IEmbedder<TInput, TOutput>
{
    Task<TOutput> EmbedAsync(TInput input, CancellationToken ct);
}
```

### Analysis Required
- Do all embedders return `float[]`? If yes, use first pattern
- Do inputs share common structure? If yes, can use base class

### Files in Question
Already created in `Core/Interfaces/Embedders/`:
- ITextEmbedder.cs
- IImageEmbedder.cs  
- IAudioEmbedder.cs
- IVideoEmbedder.cs
- IEmbedder.cs

Need to verify if generic IEmbedder can replace specific interfaces.

---

## Task 5.4: Generic Event Handler Pattern

**Status**: ❌ NOT STARTED
**Source**: TODO_BACKUP.md Phase 4

### Problem
Multiple event processors with similar structure:
- IEventListener
- IEventProcessor
- ISemanticEnricher

### Solution

Use generic event handler pattern:
```csharp
public interface IEventHandler<TEvent>
{
    Task HandleAsync(TEvent evt, CancellationToken ct);
}

public abstract class EventHandlerBase<TEvent> : IEventHandler<TEvent>
{
    // Common logging, error handling, retries
    public abstract Task ProcessEventAsync(TEvent evt, CancellationToken ct);
    
    public async Task HandleAsync(TEvent evt, CancellationToken ct)
    {
        // Wrap with logging, metrics, error handling
        try
        {
            await ProcessEventAsync(evt, ct);
        }
        catch (Exception ex)
        {
            // Log, retry, dead-letter, etc.
        }
    }
}

// Concrete handlers:
public class SemanticEnrichmentHandler : EventHandlerBase<CloudEvent> { }
public class ChangeEventHandler : EventHandlerBase<ChangeEvent> { }
```

### Move Model Classes
CloudEvent and ChangeEvent are model classes, NOT interfaces.
Move from `Interfaces/Events/` to `Models/Events/`.

---

## Task 5.5: Split Multi-Class Files

**Status**: ❌ NOT STARTED
**Source**: TODO_BACKUP.md Phase 10

### Files to Split

**Core**:
- `IModelFormatReader.cs` (8 classes) → 8 individual files
- `IGenericInterfaces.cs` (7 classes) → 7 individual files
- `IEventProcessing.cs` (6 classes) → 6 individual files

**Infrastructure**:
- `IConceptDiscoveryRepository.cs` (7 classes) → split DTOs to Models/
- `GGUFParser.cs` (6 classes) → split to `ModelFormats/GGUF/` folder

### Splitting Pattern

For `IModelFormatReader.cs`:
```
Before:
src/Hartonomous.Core/Interfaces/IModelFormatReader.cs
  - IOnnxModelReader
  - IPyTorchModelReader
  - ISafetensorsModelReader
  - IGGUFModelReader
  - etc. (8 total)

After:
src/Hartonomous.Core/Interfaces/ModelFormats/
  - IOnnxModelReader.cs
  - IPyTorchModelReader.cs
  - ISafetensorsModelReader.cs
  - IGGUFModelReader.cs
  - etc. (8 files)
```

### Verification
After split:
```powershell
# Count class definitions per file
Get-ChildItem -Recurse -Include *.cs | ForEach-Object {
    $classes = (Select-String -Path $_.FullName -Pattern "^\s*(public|internal)\s+(class|interface)" -AllMatches).Matches.Count
    if ($classes -gt 1) {
        "$($_.FullName): $classes classes/interfaces"
    }
}
# Should show minimal results (only acceptable multi-class files)
```

---

## Task 5.6: Move Misplaced Model Classes

**Status**: ❌ NOT STARTED
**Source**: TODO_BACKUP.md Phase 8

### Problem
Model classes in Interfaces/ subdirectories:

**From Interfaces/Ingestion/**:
- IngestionStats.cs
- ModelIngestionRequest.cs
- ModelIngestionResult.cs

**From Interfaces/ModelFormats/**:
- OnnxMetadata.cs
- PyTorchMetadata.cs
- SafetensorsMetadata.cs
- GGUFMetadata.cs
- TensorFlowMetadata.cs

### Solution

Move to Models/:
```
src/Hartonomous.Core/Models/Ingestion/
  - IngestionStats.cs
  - ModelIngestionRequest.cs
  - ModelIngestionResult.cs

src/Hartonomous.Core/Models/ModelFormats/
  - OnnxMetadata.cs
  - PyTorchMetadata.cs
  - SafetensorsMetadata.cs
  - GGUFMetadata.cs
  - TensorFlowMetadata.cs
```

Update namespaces:
```csharp
// Before:
namespace Hartonomous.Core.Interfaces.Ingestion;

// After:
namespace Hartonomous.Core.Models.Ingestion;
```

---

## Task 5.7: Update Using Statements

**Status**: ❌ NOT STARTED
**Source**: TODO_BACKUP.md Phase 9

### Problem
105 files reference `using Hartonomous.Core.Interfaces`

After consolidation and reorganization, many namespaces will change.

### Solution

**Step 1**: Find all affected files
```powershell
Get-ChildItem -Recurse -Include *.cs | Select-String "using Hartonomous.Core.Interfaces" | Select-Object -Unique Path
```

**Step 2**: For each namespace change, update references
Use IDE refactoring or bulk find/replace.

**Step 3**: Verify build after each batch
```powershell
dotnet build
```

### Common Namespace Changes

```csharp
// Model classes moved:
using Hartonomous.Core.Interfaces.Ingestion; → using Hartonomous.Core.Models.Ingestion;
using Hartonomous.Core.Interfaces.ModelFormats; → using Hartonomous.Core.Models.ModelFormats;

// Events moved:
using Hartonomous.Core.Interfaces.Events; → using Hartonomous.Core.Models.Events;

// Split files:
using Hartonomous.Core.Interfaces; → using Hartonomous.Core.Interfaces.ModelFormats;
```

---

## Success Criteria

Phase 5 complete when:
- ✅ Generic repository pattern implemented
- ✅ Generic search service pattern implemented
- ✅ Generic embedder analysis complete (decide keep/consolidate)
- ✅ Generic event handler pattern implemented
- ✅ All multi-class files split
- ✅ All model classes moved to Models/
- ✅ All using statements updated
- ✅ Full solution builds with 0 errors
- ✅ All tests pass
- ✅ Changes committed to git

## Estimated File Count Reduction

Before: ~763 files  
After: ~650 files (~113 file reduction)

Breakdown:
- Generic repositories: -4 files
- Generic search: -2 files
- Split multi-class: +15 files (from splitting)
- Model class moves: 0 files (just relocate)
- Consolidation savings: -122 files net

## Next Phase

After Phase 5 complete → `06-ARCHITECTURE.md`
