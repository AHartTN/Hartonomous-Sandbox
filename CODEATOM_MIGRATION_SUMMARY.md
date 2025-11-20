# CodeAtom Migration Summary

**Date:** November 20, 2025  
**Issue:** CodeAtom table violates atomic decomposition principles  
**Status:** Architecture corrected, migration path documented  
**Priority:** HIGH - Blocks cross-modal queries and multi-tenancy  

---

## Executive Summary

The `dbo.CodeAtom` table was identified as a **critical architectural violation** during SQL audit. Code should be atomized using the **SAME pattern** as AI models and documents - each Roslyn SyntaxNode becomes ONE Atom row, not stored in a separate code-specific table.

**Impact:**
- ❌ Prevents cross-modal queries ("find code similar to this text")
- ❌ No multi-tenancy support (missing TenantId)
- ❌ No temporal versioning (SYSTEM_VERSIONING missing)
- ❌ Breaks normalization (Embedding on table instead of join)
- ❌ Deprecated TEXT data type
- ❌ No AST hierarchy tracking

**Solution:** Migrate to `Atom` with `Modality='code'`

---

## What Changed

### Documentation Created

1. **[docs/architecture/code-atomization.md](docs/architecture/code-atomization.md)**
   - Complete architectural correction guide
   - Roslyn integration (.NET Framework 4.8.1)
   - CLR function implementations
   - Migration path with SQL scripts
   - Cross-language AST support (Tree-sitter)
   - 600+ lines of comprehensive documentation

2. **[SQL_AUDIT_PART6.md](SQL_AUDIT_PART6.md)**
   - Detailed CodeAtom architectural analysis
   - 11 specific violations documented
   - Quality score: 60/100 (downgraded from 72/100)
   - Immediate recommendations

3. **Updated Core Architecture Docs:**
   - [docs/architecture/semantic-first.md](docs/architecture/semantic-first.md) - Added code atomization section
   - [docs/architecture/model-atomization.md](docs/architecture/model-atomization.md) - Added AST decomposition
   - [docs/implementation/database-schema.md](docs/implementation/database-schema.md) - Added code atomization schema
   - [docs/README.md](docs/README.md) - Added reference to code-atomization.md

---

## Key Architectural Changes

### BEFORE (Wrong - CodeAtom table):

```sql
CREATE TABLE dbo.CodeAtom (
    CodeAtomId BIGINT IDENTITY PRIMARY KEY,
    Language NVARCHAR(50),
    Code TEXT,  -- ❌ Deprecated type, unlimited size
    Embedding GEOMETRY,  -- ❌ Breaks normalization
    CodeHash VARBINARY(32),  -- ❌ Code-specific deduplication
    -- ❌ NO TenantId, NO SYSTEM_VERSIONING
);

INSERT INTO CodeAtom (Language, Code, Framework, ...)
VALUES ('C#', 'public void Foo() {...}', '.NET 4.8.1', ...);
```

### AFTER (Correct - Atom with Modality='code'):

```sql
-- Each Roslyn SyntaxNode = ONE Atom
INSERT INTO dbo.Atom (
    Modality,          -- 'code'
    Subtype,           -- 'MethodDeclaration' (Roslyn SyntaxKind)
    ContentHash,       -- SHA-256 (universal deduplication)
    AtomicValue,       -- Serialized SyntaxNode (≤64 bytes)
    CanonicalText,     -- 'public void Foo() { ... }'
    Metadata,          -- JSON: Language, Framework, SyntaxKind, QualityScore, etc.
    TenantId           -- Multi-tenant isolation
)
VALUES (
    'code',
    'MethodDeclaration',
    HASHBYTES('SHA2_256', @serializedNode),
    @serializedNode,
    'public void Foo() { ... }',
    JSON_OBJECT(
        'Language': 'C#',
        'Framework': '.NET Framework 4.8.1',
        'SyntaxKind': 'MethodDeclaration',
        'RoslynType': 'Microsoft.CodeAnalysis.CSharp.Syntax.MethodDeclarationSyntax'
    ),
    @TenantId
);

-- AST hierarchy via AtomRelation
INSERT INTO AtomRelation (FromAtomId, ToAtomId, RelationType)
VALUES (@classAtomId, @methodAtomId, 'AST_CONTAINS');

-- Embeddings via AtomEmbedding (normalized)
INSERT INTO AtomEmbedding (AtomId, ModelId, SpatialKey, EmbeddingVector)
VALUES (@methodAtomId, @CodeModelId, @geometry, @vector);
```

---

## Migration Benefits

### Storage Benefits

- **Deduplication**: Shared functions across projects: **30-40% reduction**
- **Multi-tenancy**: Row-level security isolates code by tenant
- **Temporal history**: SYSTEM_VERSIONING tracks every code change
- **Normalization**: Multiple embeddings per code (different models)

### Query Benefits

```sql
-- ✅ NOW POSSIBLE: Cross-modal queries
SELECT TOP 10
    a.CanonicalText AS SimilarCode
FROM Atom a
INNER JOIN AtomEmbedding ae ON a.AtomId = ae.AtomId
WHERE a.Modality = 'code'
  AND ae.SpatialKey.STIntersects(@textQueryPoint.STBuffer(5.0)) = 1
ORDER BY ae.SpatialKey.STDistance(@textQueryPoint);

-- ✅ NOW POSSIBLE: AST hierarchy queries
WITH RECURSIVE MethodAst AS (
    SELECT a.AtomId, a.CanonicalText, 0 AS Depth
    FROM Atom a
    WHERE a.AtomId = @methodAtomId
    
    UNION ALL
    
    SELECT a.AtomId, a.CanonicalText, ma.Depth + 1
    FROM Atom a
    INNER JOIN AtomRelation ar ON a.AtomId = ar.ToAtomId
    INNER JOIN MethodAst ma ON ar.FromAtomId = ma.AtomId
    WHERE ar.RelationType = 'AST_CONTAINS'
)
SELECT * FROM MethodAst;

-- ✅ NOW POSSIBLE: Temporal code queries
SELECT CanonicalText
FROM Atom
FOR SYSTEM_TIME AS OF '2025-11-15 14:30:00'
WHERE AtomId = @methodAtomId;
```

---

## Implementation Status

### ✅ Completed

- [x] Architecture analysis (SQL_AUDIT_PART6.md)
- [x] Comprehensive documentation (code-atomization.md)
- [x] Migration scripts documented
- [x] Roslyn integration design
- [x] Updated all architecture docs
- [x] Cross-language support design (Tree-sitter)

### 🔄 In Progress

- [ ] CLR function implementation (`clr_AtomizeCodeRoslyn`)
- [ ] `sp_AtomizeCode` rewrite
- [ ] Migration execution and verification

### ⏳ Pending

- [ ] Tree-sitter CLR integration (Python, JavaScript, etc.)
- [ ] Performance benchmarks (AST spatial queries)
- [ ] Cross-modal query testing
- [ ] CodeAtom table deprecation (after 30-day verification)

---

## Migration Checklist

### Pre-Migration

- [x] **Backup CodeAtom table**
  ```sql
  SELECT * INTO dbo.CodeAtom_Backup_20251120 FROM dbo.CodeAtom;
  ```

- [x] **Backup embeddings**
  ```sql
  SELECT CodeAtomId, Embedding 
  INTO dbo.CodeAtomEmbedding_Backup_20251120 
  FROM dbo.CodeAtom WHERE Embedding IS NOT NULL;
  ```

- [x] **Document current state**
  - Row count: ~150 code snippets
  - Total size: ~2.5 MB
  - Embedding count: ~120

### Migration

- [ ] **Run migration script** (see code-atomization.md)
  - Migrate CodeAtom → Atom (Modality='code')
  - Migrate embeddings → AtomEmbedding
  - Verify row counts match

- [ ] **Update `sp_AtomizeCode`**
  - Rewrite to use Roslyn SyntaxTree walker
  - Insert Atoms + AtomRelations
  - Generate AtomEmbeddings

- [ ] **Create CLR functions**
  - `clr_AtomizeCodeRoslyn`
  - `clr_GenerateCodeEmbedding`
  - `clr_ReconstructSyntaxTree`

### Post-Migration

- [ ] **Verification** (30 days)
  - Test cross-modal queries
  - Test AST hierarchy queries
  - Test temporal queries
  - Validate deduplication working

- [ ] **Deprecation**
  - Mark CodeAtom as deprecated in schema
  - Update all references
  - DROP CodeAtom table after verification

---

## Code Changes Required

### 1. CLR Function: clr_AtomizeCodeRoslyn

**File:** `src/Hartonomous.Clr/CodeAtomizers/RoslynAtomizer.cs`

**Status:** NEW (needs creation)

**Purpose:** Walk Roslyn SyntaxTree, create Atom for each SyntaxNode

**Estimated Effort:** 8-12 hours

### 2. Stored Procedure: sp_AtomizeCode

**File:** `src/Hartonomous.Database/Procedures/dbo.sp_AtomizeCode.sql`

**Status:** NEEDS REWRITE (currently inserts into CodeAtom)

**Purpose:** Call CLR function, generate embeddings

**Estimated Effort:** 4-6 hours

### 3. Migration Script: sp_MigrateCodeAtomToAtom

**File:** `src/Hartonomous.Database/Procedures/dbo.sp_MigrateCodeAtomToAtom.sql` (NEW)

**Status:** NEEDS CREATION

**Purpose:** One-time migration from CodeAtom to Atom

**Estimated Effort:** 2-4 hours

---

## Testing Requirements

### Unit Tests

- [ ] RoslynAtomizer parses valid C# source
- [ ] RoslynAtomizer handles syntax errors gracefully
- [ ] AST hierarchy correctly stored in AtomRelation
- [ ] Embeddings generated for all AST nodes

### Integration Tests

- [ ] Migration script succeeds with sample data
- [ ] Row counts match before/after migration
- [ ] Embeddings preserved after migration
- [ ] Cross-modal queries return expected results

### Performance Tests

- [ ] AST spatial query: <25ms for 100K code atoms
- [ ] Cross-modal query: <50ms for 1M atoms (code + text)
- [ ] Temporal query: <10ms for single method history

---

## Timeline

### Week 1 (Nov 20-26)

- **Day 1-2:** Implement `clr_AtomizeCodeRoslyn` CLR function
- **Day 3:** Rewrite `sp_AtomizeCode` stored procedure
- **Day 4:** Create migration script
- **Day 5:** Unit testing

### Week 2 (Nov 27 - Dec 3)

- **Day 1:** Run migration on dev environment
- **Day 2-3:** Integration testing, cross-modal query testing
- **Day 4-5:** Performance benchmarking, documentation updates

### Week 3 (Dec 4-10)

- **Production Cutover** (Dec 7-8)
- 30-day verification period begins

### Week 7 (Jan 5-11, 2026)

- **CodeAtom Deprecation** (after 30-day verification)
- DROP CodeAtom table
- Remove legacy code references

---

## Risk Mitigation

### Risk 1: Migration Data Loss

**Mitigation:**
- Full backup before migration
- Row count verification
- 30-day verification period before DROP

### Risk 2: Performance Degradation

**Mitigation:**
- Spatial index pre-built on AtomEmbedding
- AtomRelation indexed for AST traversal
- Performance benchmarks before/after

### Risk 3: Breaking Changes

**Mitigation:**
- Backward compatibility: Keep CodeAtom read-only for 30 days
- Gradual rollout: Dev → Staging → Production
- Rollback plan: Restore from backup

---

## Success Metrics

### Migration Success

- ✅ Row count match: CodeAtom rows = Atom (Modality='code') rows
- ✅ Zero data loss: All embeddings migrated
- ✅ Query parity: Existing queries work via compatibility layer

### Performance Success

- ✅ Cross-modal queries: <50ms for 1M atoms
- ✅ AST spatial queries: <25ms for 100K code atoms
- ✅ Storage reduction: 30-40% via deduplication

### Functional Success

- ✅ Multi-tenancy: Code isolated by tenant
- ✅ Temporal queries: Access historical code versions
- ✅ AST hierarchy: Parent-child relationships queryable

---

## References

### Documentation

- **[Code Atomization Architecture](docs/architecture/code-atomization.md)** - Complete guide
- **[SQL Audit Part 6](SQL_AUDIT_PART6.md)** - Detailed analysis
- **[Database Schema](docs/implementation/database-schema.md)** - Atom table schema
- **[Semantic-First Architecture](docs/architecture/semantic-first.md)** - Spatial query pattern

### Related Issues

- **GitHub Issue #42:** CodeAtom architectural violation
- **GitHub PR #43:** CLR function implementation (pending)
- **GitHub PR #44:** Migration script (pending)

---

## Contact

**Questions?** Open an issue on GitHub or contact:
- **Architecture:** architecture@hartonomous.ai
- **Technical Support:** support@hartonomous.ai

---

**Last Updated:** November 20, 2025  
**Next Review:** December 7, 2025 (post-migration)
