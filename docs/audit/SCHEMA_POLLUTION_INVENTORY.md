# Schema Pollution Inventory

## Tables with Unauthorized Columns

### dbo.Atoms

**Expected Columns (9):**

1. AtomId - UNIQUEIDENTIFIER NOT NULL DEFAULT NEWSEQUENTIALID()
2. ContentHash - VARBINARY(64) NOT NULL
3. AtomicValue - VARBINARY(64) NOT NULL
4. Modality - NVARCHAR(50) NOT NULL
5. Subtype - NVARCHAR(50) NULL
6. GovernanceHash - VARBINARY(64) NOT NULL
7. CreatedAt - DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME()
8. SysStartTime - DATETIME2 GENERATED ALWAYS AS ROW START NOT NULL
9. SysEndTime - DATETIME2 GENERATED ALWAYS AS ROW END NOT NULL

**Polluting Columns Added (6):**

1. **Content** - NVARCHAR(MAX) NULL
   - Added in: Commit 92fe0e4
   - Reason given: "Backward compatibility"
   - Why wrong: System never deployed, violates 64-byte limit
   - Impact: Procedures expect unlimited text storage
   - Fix: Remove column, update ~30 procedures to use AtomicValue

2. **Metadata** - NVARCHAR(MAX) NULL
   - Added in: Commit 92fe0e4
   - Reason given: "Backward compatibility"
   - Why wrong: System never deployed, no metadata architecture
   - Impact: ~12 procedures expect JSON metadata storage
   - Fix: Remove column, redesign metadata handling

3. **IsActive** - BIT NOT NULL DEFAULT 1
   - Added in: Commit 92fe0e4
   - Reason given: "Backward compatibility"
   - Why wrong: v5 uses temporal tables, no soft deletes
   - Impact: ~17 procedures filter by IsActive
   - Fix: Remove column, remove filter clauses

4. **UpdatedAt** - DATETIME2 NULL
   - Added in: Commit 92fe0e4
   - Reason given: "Backward compatibility"
   - Why wrong: Temporal table tracks updates via SysStartTime/SysEndTime
   - Impact: ~8 procedures reference UpdatedAt
   - Fix: Remove column, map to CreatedAt or remove

5. **ContentType** - NVARCHAR(100) NULL
   - Added in: Commit 92fe0e4
   - Reason given: "Backward compatibility"
   - Why wrong: Replaced by Modality + Subtype
   - Impact: ~15 procedures filter/query by ContentType
   - Fix: Remove column, map to Modality + '/' + Subtype

6. **CreatedUtc** - DATETIME2 NULL
   - Added in: Commit 92fe0e4
   - Reason given: "Backward compatibility"
   - Why wrong: Duplicates CreatedAt column
   - Impact: ~10 procedures use CreatedUtc for temporal queries
   - Fix: Remove column, rename to CreatedAt

**Total Schema Pollution:** 6 columns, all violating v5 design

---

### dbo.AtomEmbeddings

**Expected Columns (5):**

1. EmbeddingId - UNIQUEIDENTIFIER NOT NULL DEFAULT NEWSEQUENTIALID()
2. AtomId - UNIQUEIDENTIFIER NOT NULL (FK to Atoms)
3. SpatialKey - GEOMETRY NOT NULL
4. HilbertValue - BIGINT NULL
5. CreatedAt - DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME()

**Polluting Columns Added (3):**

1. **Dimension** - INT NULL
   - Added in: Commit 92fe0e4
   - Reason given: "Backward compatibility"
   - Why wrong: Can derive from SpatialKey.STDimension()
   - Impact: ~14 procedures filter/validate by Dimension
   - Fix: Remove column, use SpatialKey.STDimension()

2. **LastComputedUtc** - DATETIME2 NULL
   - Added in: Commit 92fe0e4
   - Reason given: "Backward compatibility"
   - Why wrong: Duplicates CreatedAt, v5 doesn't recompute embeddings
   - Impact: ~7 procedures check staleness
   - Fix: Remove column, use CreatedAt

3. **EmbeddingVector** - GEOMETRY NULL
   - Added in: Commit 92fe0e4
   - Reason given: "Backward compatibility"
   - Why wrong: Duplicates SpatialKey column
   - Impact: ~28 procedures use EmbeddingVector for search
   - Fix: Remove column, rename all references to SpatialKey

**Total Schema Pollution:** 3 columns, all violating v5 design

---

## Summary

**Total Tables Polluted:** 2
**Total Columns Added:** 9
**Total Procedures Affected:** 88+
**Architectural Violations:**
- Added NVARCHAR(MAX) columns (violates 64-byte atomic limit)
- Duplicated columns (CreatedUtc/CreatedAt, EmbeddingVector/SpatialKey, LastComputedUtc/CreatedAt)
- Added soft delete column (violates temporal table design)
- Added redundant computed columns (Dimension)

**Commit Responsible:** 92fe0e4 "Phase 5b: Add backward compatibility columns and fix DACPAC syntax errors"

**Justification Given:** "Backward compatibility during migration"

**Why Justification Invalid:** System was never deployed to production, no users to maintain compatibility with, this is pre-release refactoring not post-deployment migration

**Correct Action:** Remove all 9 columns, fix procedures to use correct v5 schema

**Current State:** Columns still present in current HEAD, build still failing
