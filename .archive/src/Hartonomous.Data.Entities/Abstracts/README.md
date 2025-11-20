# Entity Abstracts and Scaffolding Strategy

## Overview
This directory contains **hand-written** base interfaces and classes for the application layer. They are **NOT auto-generated** and provide opt-in patterns for lightweight data transfer and domain modeling.

## Scaffolding Philosophy: Schema-First, Not Opinionated

### What Gets Scaffolded (Auto-Generated)
- **Entities/** - Pure POCOs matching database schema exactly
- **Configurations/** - EF Core fluent API configuration (separate files per entity)
- **HartonomousDbContext.cs** - DbContext with `ApplyConfiguration` pattern

### What's NOT Scaffolded (Hand-Written)
- **Abstracts/** - This directory - interfaces and base classes for app layer
- **Partial class extensions** - Add interface implementations to scaffolded entities
- **Domain logic** - Business rules, validations, computed properties

## Why Not Auto-Generate Everything?

1. **Temporal Tables** - Atom, AtomRelation, TensorAtomCoefficient use SQL Server system versioning. Audit fields are redundant.
2. **Schema Diversity** - 65+ tables with different patterns (temporal, standard, multi-tenant). One-size-fits-all inheritance doesn't work.
3. **Flexibility** - You decide which entities need which interfaces based on usage patterns.
4. **Performance** - Avoid forcing patterns (soft delete filters, indexes) on tables that don't need them.

## Lightweight Projection Pattern

### Problem
```csharp
// BAD: Loads entire entity graph with all navigation properties
public void ProcessAtom(Atom atom)
{
    var id = atom.AtomId;
    // EF loads Embeddings, Compositions, Relations, etc. = N+1 queries
}
```

### Solution
```csharp
// GOOD: Only needs ID, no navigation loading
public void ProcessAtom(IIdentifiable<long> atom)
{
    var id = atom.Id;  // Just the ID, no graph
}
```

### Implementation
Add partial class for scaffolded entity:

```csharp
// File: Entities/Atom.Partial.cs
namespace Hartonomous.Data.Entities;

public partial class Atom : IIdentifiable<long>, ITenantScoped<long>, ITimestamped<long>
{
    // Map scaffolded properties to interface contracts
    long IIdentifiable<long>.Id => AtomId;
    int ITenantScoped<long>.TenantId => TenantId;
    DateTime ITimestamped<long>.CreatedAt => CreatedAt;
}
```

Now services can use lightweight projections:

```csharp
public interface IAtomService
{
    Task<bool> ExistsAsync(IIdentifiable<long> atom);  // Just needs ID
    Task<ITimestamped<long>> GetTimestampAsync(long atomId);  // ID + CreatedAt only
    Task<IEnumerable<IIdentifiable<long>>> GetAtomIdsForTenant(int tenantId);  // Minimal projection
}
```

## Available Abstracts

### Marker Interfaces (Generic Constraints)
- **IEntity** - Base marker for all entities
- **IEntity<TKey>** - Entity with primary key (repository pattern)

### Projection Interfaces (Lightweight DTOs)
- **IIdentifiable<TKey>** - Just ID, no graph
- **ITimestamped<TKey>** - ID + CreatedAt
- **ITenantScoped<TKey>** - ID + TenantId

### Audit Interfaces (Optional Patterns)
- **IAuditableEntity** - CreatedAt, CreatedBy, ModifiedAt, ModifiedBy
- **ISoftDeletable** - DeletedAt, DeletedBy, IsDeleted
- **IConcurrencyToken** - RowVersion for optimistic concurrency

### Base Classes (Domain Entities Only)
- **EntityBase<TKey>** - For hand-written domain entities (NOT scaffolded entities)

## When to Use What

### Use IIdentifiable/ITimestamped/ITenantScoped
- ✅ Service method parameters (avoid loading full graphs)
- ✅ Batch operations (process IDs only)
- ✅ API responses (return minimal data)
- ✅ Query projections (`.Select(a => new { Id = a.AtomId })`)

### Use EntityBase<TKey>
- ✅ Custom domain entities you create by hand
- ✅ Aggregate roots with business logic
- ❌ **NEVER** for scaffolded database entities

### Use IAuditableEntity/ISoftDeletable
- ✅ Hand-written entities needing audit trails
- ❌ Temporal tables (SQL Server tracks automatically)
- ❌ Scaffolded entities (schema controls this)

## Scaffolding Workflow

1. **Modify database schema** (migrations, manual DDL, etc.)
2. **Run scaffolding**: `.\scripts\scaffold-entities.ps1`
3. **Entities regenerated** - Pure POCOs from schema
4. **Add partial classes** - Implement projection interfaces as needed
5. **Update services** - Use `IIdentifiable<T>` instead of full entities

## Example: Multi-Layered Approach

```csharp
// Data Layer: Scaffolded entity (pure POCO)
public partial class Atom
{
    public long AtomId { get; set; }
    public int TenantId { get; set; }
    public string Modality { get; set; } = null!;
    public DateTime CreatedAt { get; set; }
    // ... 10 more properties, 15 navigation properties
}

// Data Layer: Partial class adds interfaces
public partial class Atom : IIdentifiable<long>, ITenantScoped<long>
{
    long IIdentifiable<long>.Id => AtomId;
    int ITenantScoped<long>.TenantId => TenantId;
}

// Service Layer: Uses lightweight projection
public class AtomBatchProcessor
{
    public async Task<int> ProcessBatchAsync(IEnumerable<IIdentifiable<long>> atomIds)
    {
        // Only loads IDs, no graph - efficient bulk operation
        foreach (var atom in atomIds)
        {
            await ProcessAsync(atom.Id);
        }
    }
}

// API Layer: Returns minimal data
[HttpGet("atoms/tenant/{tenantId}")]
public async Task<IEnumerable<IIdentifiable<long>>> GetAtomIds(int tenantId)
{
    // Returns just IDs, not full 200KB+ entity graphs per atom
    return await _context.Atoms
        .Where(a => a.TenantId == tenantId)
        .Cast<IIdentifiable<long>>()
        .ToListAsync();
}
```

## Anti-Patterns to Avoid

### ❌ Forcing EntityBase on Scaffolded Entities
```csharp
// WRONG: Scaffolded Atom doesn't have DeletedAt/RowVersion in schema
public partial class Atom : EntityBase<long>  // Compilation error!
```

### ❌ Adding Query Filters During Scaffolding
```csharp
// WRONG: Temporal tables track this automatically, don't need app-level filter
entity.HasQueryFilter(e => e.DeletedAt == null);  // Redundant for temporal tables
```

### ❌ Passing Full Entities to Services
```csharp
// WRONG: Loads entire graph unnecessarily
public async Task ProcessAtoms(IEnumerable<Atom> atoms)
{
    foreach (var atom in atoms)  // Each atom brings 15 navigation properties!
    {
        await Process(atom.AtomId);  // Only needed ID!
    }
}
```

### ✅ Use Projections Instead
```csharp
// CORRECT: Minimal data transfer
public async Task ProcessAtoms(IEnumerable<IIdentifiable<long>> atoms)
{
    foreach (var atom in atoms)  // Just IDs, no graph
    {
        await Process(atom.Id);
    }
}
```

## Summary

- **Scaffolding** = Database schema → Clean POCOs
- **Abstracts** = Application patterns → Opt-in interfaces
- **Partial classes** = Bridge between data layer and app layer
- **Result** = Schema-driven data layer + flexible application layer
