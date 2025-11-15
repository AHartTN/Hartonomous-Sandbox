# Hartonomous Data Access Layer - Quick Reference

## 🚀 Common Commands

### Entity Scaffolding
```powershell
# Regenerate entities after schema changes
.\scripts\generate-entities.ps1 -Force

# What-If mode (preview changes)
.\scripts\generate-entities.ps1 -WhatIf

# Custom database
.\scripts\generate-entities.ps1 -Server "myserver" -Database "MyDb"
```

### DACPAC Deployment (includes auto-scaffolding)
```powershell
# Full deployment with entity generation
.\scripts\deploy-dacpac.ps1 -Server localhost -Database Hartonomous
```

---

## 📝 Pattern Cheat Sheet

### Repository Pattern

```csharp
// Get repository from UnitOfWork
var modelRepo = _unitOfWork.Repository<Model, long>();

// CRUD operations
var model = await modelRepo.GetByIdAsync(id);
var all = await modelRepo.GetAllAsync();
var filtered = await modelRepo.FindAsync(m => m.Name.Contains("gpt"));

await modelRepo.AddAsync(new Model { /* ... */ });
modelRepo.Update(existingModel);
modelRepo.Remove(model);

await _unitOfWork.SaveChangesAsync();
```

### Specification Pattern

```csharp
// Define reusable specification
public class ActiveModelsSpec : Specification<Model>
{
    public ActiveModelsSpec(string filter)
    {
        AddCriteria(m => !m.IsDeleted && m.Name.Contains(filter));
        AddInclude(m => m.ModelLayers);
        AddOrderByDescending(m => m.CreatedAt);
        ApplyNoTracking();
    }
}

// Use specification
var spec = new ActiveModelsSpec("transformer");
var models = await modelRepo.FindAsync(spec);
```

### Unit of Work Pattern

```csharp
// Automatic transaction management
await _unitOfWork.ExecuteInTransactionAsync(async () =>
{
    var repo1 = _unitOfWork.Repository<Model, long>();
    var repo2 = _unitOfWork.Repository<Atom, long>();
    
    // Multiple operations in single transaction
    await repo1.AddAsync(/* ... */);
    await repo2.AddAsync(/* ... */);
    
    // Auto-commits on success, rolls back on exception
});
```

---

## 🏗️ File Structure

```
Hartonomous.Core/
└── Data/
    ├── IEntity.cs          ← Entity marker interfaces
    ├── IRepository.cs      ← Repository contracts
    ├── ISpecification.cs   ← Specification pattern
    ├── IUnitOfWork.cs      ← Transaction management
    └── IDbContextFactory.cs

Hartonomous.Infrastructure/
└── Data/
    ├── EfRepository.cs     ← EF Core repository impl
    └── EfUnitOfWork.cs     ← EF Core UoW impl

Hartonomous.Data/
├── Entities/               ← GENERATED (do not edit)
│   ├── Model.cs
│   ├── Atom.cs
│   └── ...
├── HartonomousDbContext.cs              ← GENERATED
└── HartonomousDbContext.Partial.cs      ← CUSTOM (preserved)
```

---

## ✅ Best Practices

### DO
- ✅ Use specifications for complex queries
- ✅ Wrap multi-step operations in `ExecuteInTransactionAsync`
- ✅ Add custom logic to `*.Partial.cs` files
- ✅ Use `AsNoTracking()` for read-only queries
- ✅ Regenerate entities after schema changes

### DON'T
- ❌ Edit generated entity files directly
- ❌ Reference EF Core in `Hartonomous.Core`
- ❌ Call `SaveChangesAsync` outside UnitOfWork
- ❌ Skip entity regeneration after DACPAC deployment

---

## 🔧 Troubleshooting

### "Build failed" during scaffolding
```powershell
# Temporarily remove project reference, scaffold, then restore
# Or use temporary project approach (see docs/architecture/data-access-layer.md)
```

### Entities out of sync with database
```powershell
# Regenerate entities
.\scripts\generate-entities.ps1 -Force
```

### Need custom DbContext configuration
```csharp
// Edit: Hartonomous.Data/HartonomousDbContext.Partial.cs
partial void OnModelCreatingPartial(ModelBuilder modelBuilder)
{
    // Add custom configurations here
}
```

---

## 📚 Documentation

- **Full Architecture Guide**: `docs/architecture/data-access-layer.md`
- **Entity Scaffolding**: `scripts/generate-entities.ps1 -?`
- **DACPAC Deployment**: `scripts/deploy-dacpac.ps1 -?`

---

**Quick Start**: Deploy database → Entities auto-generated → Build solution → Ready to code! 🎉
