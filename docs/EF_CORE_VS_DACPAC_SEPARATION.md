# EF Core vs DACPAC Separation of Concerns - Audit & Migration Plan

**Date:** November 12, 2025  
**Status:** Analysis Complete - Ready for Execution  
**Related:** [CONSOLIDATED_EXECUTION_PLAN.md](CONSOLIDATED_EXECUTION_PLAN.md)

---

## Executive Summary

The current architecture has **82 EF Core `IEntityTypeConfiguration` files** that are mixing **database schema definition** with **ORM mapping**. According to Microsoft best practices, these responsibilities should be separated:

- **DACPAC (Database Project)**: Schema source of truth - tables, indexes, constraints, defaults
- **EF Core Configurations**: ORM mapping - navigation properties, change tracking behavior

### Key Findings

✅ **Configurations should exist** - They're architecturally sound  
❌ **Schema elements should migrate to DACPAC** - ~200+ indexes, constraints belong in database project  
✅ **Navigation properties should stay** - EF Core-specific ORM mapping  

---

## Current State Analysis

### What's ALREADY in DACPAC ✅

**Location:** `src/Hartonomous.Database/Tables/*.sql`

```sql
-- Example: dbo.Atoms.sql
CREATE TABLE [dbo].[Atoms] (
    [AtomId]          BIGINT           NOT NULL IDENTITY,
    [ContentHash]     BINARY (32)      NOT NULL,
    [CreatedAt]       DATETIME2 (7)    NOT NULL DEFAULT (SYSUTCDATETIME()),
    [TenantId]        INT              NOT NULL DEFAULT 0,
    [IsActive]        BIT              NOT NULL DEFAULT CAST(1 AS BIT),
    CONSTRAINT [PK_Atoms] PRIMARY KEY CLUSTERED ([AtomId] ASC),
    CONSTRAINT [FK_...] FOREIGN KEY ([...]) REFERENCES [...] ON DELETE CASCADE
);
```

**Present:**
- ✅ Table definitions with column types
- ✅ DEFAULT constraints
- ✅ Foreign Keys with CASCADE behavior
- ✅ Primary Keys

### What's MISSING from DACPAC ❌

**Estimated ~200+ schema objects only in EF Core:**

1. **Indexes** (~200 files needed)
   - Composite indexes
   - Unique indexes with filters
   - Spatial bucket indexes
   - Descending indexes

2. **Check Constraints** (~5 constraints)
   - `CK_AutonomousImprovement_SuccessScore CHECK (SuccessScore BETWEEN 0 AND 1)`

3. **Metadata Corrections**
   - `NVARCHAR(MAX)` → `JSON` for SQL Server 2025

### What's in EF Core (Current State)

**Location:** `src/Hartonomous.Data/Configurations/*.cs` (82 files)

**Mixing Two Concerns:**

```csharp
// AtomEmbeddingConfiguration.cs (CURRENT - 90+ lines)
public void Configure(EntityTypeBuilder<AtomEmbedding> builder)
{
    // ❌ SCHEMA - Should be in DACPAC
    builder.Property(e => e.Dimension).HasDefaultValue(0);
    builder.Property(e => e.EmbeddingVector).HasColumnType("VECTOR(1998)");
    builder.HasIndex(e => new { e.AtomId, e.EmbeddingType, e.ModelId });
    
    // ✅ ORM MAPPING - Should stay in EF Core
    builder.HasOne(e => e.Atom)
        .WithMany(a => a.Embeddings)
        .HasForeignKey(e => e.AtomId)
        .OnDelete(DeleteBehavior.Cascade);  // EF Core behavior
}
```

---

## Microsoft Documentation Research

### IEntityTypeConfiguration Purpose

From [EF Core 2.0 Release Notes](https://learn.microsoft.com/en-us/ef/core/what-is-new/ef-core-2.0/#modeling):

> "In EF6 it was possible to encapsulate the code first configuration of a specific entity type by deriving from EntityTypeConfiguration. In EF Core 2.0 we are bringing this pattern back... allowing configuration for each entity type to be contained in its own configuration class."

**Key Point:** This is for **organizing configuration code**, not for being the source of truth for schema.

### Database-First with DACPAC

From [What are SQL database projects?](https://learn.microsoft.com/en-us/sql/tools/sql-database-projects/sql-database-projects):

> "SQL database projects are used to track the source of truth for database state, **including development with an object-relational mapper (ORM) such as EF Core**."

**Key Point:** DACPAC is source of truth, EF Core reads from deployed schema.

### OnDelete Behavior Clarification

**CRITICAL:** EF Core `DeleteBehavior` ≠ SQL Server CASCADE

**EF Core (Change Tracking):**
```csharp
.OnDelete(DeleteBehavior.Cascade)  // EF deletes entities in memory before SaveChanges()
.OnDelete(DeleteBehavior.NoAction) // EF doesn't auto-delete related entities
```

**SQL Server (Database Constraint):**
```sql
FOREIGN KEY (AtomId) REFERENCES dbo.Atoms(AtomId)
ON DELETE CASCADE  -- Database deletes rows automatically
```

These are **complementary** - both can exist. EF Core's behavior handles in-memory tracking, SQL enforces at database level.

---

## Detailed Migration Inventory

### Schema Elements to Migrate (200+ items)

#### 1. Indexes by Category

**Composite Indexes** (~80 files):
```csharp
// FROM: AtomEmbeddingConfiguration.cs
builder.HasIndex(e => new { e.AtomId, e.EmbeddingType, e.ModelId })
    .HasDatabaseName("IX_AtomEmbeddings_Atom_Model_Type");

// TO: Indexes/dbo.AtomEmbeddings.IX_AtomEmbeddings_Atom_Model_Type.sql
CREATE NONCLUSTERED INDEX [IX_AtomEmbeddings_Atom_Model_Type]
ON [dbo].[AtomEmbeddings] ([AtomId], [EmbeddingType], [ModelId]);
```

**Unique Indexes with Filters** (~15 files):
```csharp
// FROM: AtomConfiguration.cs
builder.HasIndex(a => new { a.ContentHash, a.TenantId })
    .IsUnique()
    .HasFilter("[IsDeleted] = 0")
    .HasDatabaseName("UX_Atoms_ContentHash_TenantId");

// TO: Indexes/dbo.Atoms.UX_Atoms_ContentHash_TenantId.sql
CREATE UNIQUE NONCLUSTERED INDEX [UX_Atoms_ContentHash_TenantId]
ON [dbo].[Atoms] ([ContentHash], [TenantId])
WHERE [IsDeleted] = 0;
```

**Descending Indexes** (~20 files):
```csharp
// FROM: ImageConfiguration.cs
builder.HasIndex(e => e.IngestionDate)
    .HasDatabaseName("IX_Images_IngestionDate")
    .IsDescending();

// TO: Indexes/dbo.Images.IX_Images_IngestionDate.sql
CREATE NONCLUSTERED INDEX [IX_Images_IngestionDate]
ON [dbo].[Images] ([IngestionDate] DESC);
```

**Spatial/Specialized Indexes** (~30 files):
```csharp
// FROM: AtomEmbeddingConfiguration.cs
builder.HasIndex(e => new { e.SpatialBucketX, e.SpatialBucketY, e.SpatialBucketZ })
    .HasDatabaseName("IX_AtomEmbeddings_SpatialBucket");

// TO: Indexes/dbo.AtomEmbeddings.IX_AtomEmbeddings_SpatialBucket.sql
CREATE NONCLUSTERED INDEX [IX_AtomEmbeddings_SpatialBucket]
ON [dbo].[AtomEmbeddings] ([SpatialBucketX], [SpatialBucketY], [SpatialBucketZ]);
```

#### 2. Check Constraints (~5 constraints)

```csharp
// FROM: AutonomousImprovementHistoryConfiguration.cs
builder.ToTable(t => t.HasCheckConstraint("CK_AutonomousImprovement_SuccessScore",
    "[SuccessScore] >= 0 AND [SuccessScore] <= 1"));

// TO: Add to Tables/dbo.AutonomousImprovementHistory.sql
ALTER TABLE [dbo].[AutonomousImprovementHistory]
ADD CONSTRAINT [CK_AutonomousImprovement_SuccessScore]
CHECK ([SuccessScore] >= 0 AND [SuccessScore] <= 1);
```

#### 3. Column Type Corrections

**JSON Type (SQL Server 2025):**
```sql
-- CURRENT:
[Metadata] NVARCHAR(MAX) NULL,

-- SHOULD BE:
[Metadata] JSON NULL,
```

Affected tables: Atoms, AtomEmbeddings, AtomRelations, Images, AudioData, Videos, TextDocuments, ModelMetadata, BillingRatePlans, etc. (~25 tables)

### Navigation Properties to Keep (150+ relationships)

**Examples that STAY in EF Core:**

```csharp
// Cascade delete (EF Core change tracking)
builder.HasMany(a => a.Embeddings)
    .WithOne(e => e.Atom)
    .HasForeignKey(e => e.AtomId)
    .OnDelete(DeleteBehavior.Cascade);

// No action (prevent circular cascades)
builder.HasMany(a => a.SourceRelations)
    .WithOne(r => r.SourceAtom)
    .HasForeignKey(r => r.SourceAtomId)
    .OnDelete(DeleteBehavior.NoAction);

// SQL Graph special handling
builder.Ignore(e => e.OutgoingEdges);
builder.Ignore(e => e.IncomingEdges);
```

---

## Recommended Migration Plan

### Phase 1: Extract Indexes to DACPAC

**Create:** `src/Hartonomous.Database/Indexes/` folder structure

**Naming Convention:** `{schema}.{table}.{indexName}.sql`

**Example:**
```
Indexes/
├── dbo.Atoms.UX_Atoms_ContentHash_TenantId.sql
├── dbo.Atoms.IX_Atoms_Modality_Subtype.sql
├── dbo.AtomEmbeddings.IX_AtomEmbeddings_Atom_Model_Type.sql
├── dbo.AtomEmbeddings.IX_AtomEmbeddings_SpatialBucket.sql
└── ... (~200 files)
```

**Automation Script:** Generate from EF Core configuration files via parsing

### Phase 2: Add Check Constraints

**Option A:** Add inline to table definitions
**Option B:** Create separate `Constraints/` folder

**Recommended:** Option A - keeps related schema together

### Phase 3: Update Table Metadata

**Update all tables with:**
- `JSON` column type (SQL Server 2025)
- Any missing DEFAULT constraints
- Verify FK CASCADE matches intent

### Phase 4: Simplify EF Core Configurations

**Remove from configurations:**
- ❌ `.HasDefaultValue()` / `.HasDefaultValueSql()`
- ❌ `.HasIndex()`
- ❌ `.HasCheckConstraint()`
- ❌ `.HasColumnType()` (except for special mappings)
- ❌ `.HasMaxLength()` / `.HasPrecision()`
- ❌ `.IsRequired()` (already in NOT NULL)

**Keep in configurations:**
- ✅ `.HasMany()` / `.WithOne()` / `.HasForeignKey()`
- ✅ `.OnDelete(DeleteBehavior.*)` (EF Core change tracking)
- ✅ `.Ignore()` (properties not mapped)
- ✅ `.ToTable()` / `.HasKey()` (entity mapping)

**Result:** Configurations reduce from ~90 lines to ~30 lines (focus on relationships)

### Phase 5: Update .sqlproj

Add all new index files to `Hartonomous.Database.sqlproj`:
```xml
<Build Include="Indexes\dbo.Atoms.UX_Atoms_ContentHash_TenantId.sql" />
<Build Include="Indexes\dbo.Atoms.IX_Atoms_Modality_Subtype.sql" />
<!-- ... ~200 more -->
```

---

## Benefits of Separation

### Single Source of Truth ✅
- DACPAC defines schema
- Deploy → Database → Scaffold EF Core (if needed)
- No duplication between SQL and C#

### Simplified Configurations ✅
- EF Core configs focus on ORM concerns
- 60% reduction in configuration code
- Easier to understand and maintain

### Better Schema Control ✅
- Database admins can modify indexes independently
- Version control for schema is SQL (not C#)
- Performance tuning doesn't require code changes

### Scaffolding Compatibility ✅
- Can regenerate EF Core entities from DACPAC
- Partial classes preserve customizations
- Database-first workflow fully supported

---

## Validation Checklist

After migration, verify:

- [ ] All indexes exist in DACPAC (`Indexes/*.sql`)
- [ ] All check constraints in table definitions
- [ ] Foreign keys have correct CASCADE behavior (both SQL and EF Core)
- [ ] JSON column types updated to `JSON`
- [ ] EF Core configurations compile without errors
- [ ] Navigation properties work correctly
- [ ] DACPAC builds successfully
- [ ] Deployment to test database succeeds
- [ ] EF Core can query all relationships
- [ ] No duplicate schema definitions (SQL vs C#)

---

## Files Affected

### To Create (New)
- `src/Hartonomous.Database/Indexes/*.sql` (~200 files)
- This documentation file

### To Modify (Existing)
- All 82 files in `src/Hartonomous.Data/Configurations/*.cs` (simplify)
- ~25 table files in `src/Hartonomous.Database/Tables/*.sql` (add constraints, update JSON type)
- `src/Hartonomous.Database/Hartonomous.Database.sqlproj` (add index Build items)

### To Delete (None)
- EF Core migrations are already identified for deletion (separate task)
- No other deletions required for this separation

---

## Appendix: Quick Reference

### EF Core DeleteBehavior Options

| Behavior | EF Core Action | SQL Server Equivalent |
|----------|----------------|----------------------|
| `Cascade` | Deletes related entities in memory | `ON DELETE CASCADE` |
| `NoAction` | No automatic deletion | No FK constraint or `NO ACTION` |
| `Restrict` | Throws if related entities exist | `NO ACTION` with validation |
| `SetNull` | Sets FK to null | `ON DELETE SET NULL` |
| `ClientSetNull` | Sets FK to null (client-side only) | N/A |

**Note:** EF Core behavior works with change tracking. SQL Server constraint enforces at database level. Both can coexist.

### Index Naming Conventions

- **Primary Key:** `PK_{TableName}`
- **Unique Constraint:** `UX_{TableName}_{Columns}`
- **Index:** `IX_{TableName}_{Columns}`
- **Foreign Key:** `FK_{TableName}_{ReferencedTable}_{Column}`
- **Check Constraint:** `CK_{TableName}_{Description}`

---

## Next Steps

1. Review and approve this migration plan
2. Generate index extraction script (or manual extraction)
3. Create index files in DACPAC
4. Update table definitions with constraints
5. Simplify EF Core configurations
6. Test build and deployment
7. Commit changes with descriptive message

**Estimated Effort:** 4-6 hours for full migration  
**Risk Level:** Low (non-breaking if done correctly)  
**Rollback:** Git revert if issues discovered
