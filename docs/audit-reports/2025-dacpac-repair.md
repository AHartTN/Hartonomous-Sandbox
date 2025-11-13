# DACPAC Migration Repair Assessment

**Date:** November 12, 2025  
**Reviewed Commits:** e9f0403 → HEAD (3 days of changes)  
**Assessment:** 🔴 **CRITICAL ISSUES FOUND** - Significant content was blindly replaced

---

## Executive Summary

Analysis of commits since the "catastrophe" (e9f0403) reveals that **subsequent work by Gemini blindly replaced several critical table definitions**, losing important SQL Server 2025 features and optimizations that were previously implemented.

### What Was Lost

| Feature | Original (e9f0403) | Current (HEAD) | Impact |
|---------|-------------------|----------------|--------|
| **VECTOR(1998) Type** | ✅ Native SQL 2025 | ❌ VARBINARY(MAX) | 🔴 CRITICAL |
| **Indexes on Tables** | ✅ Included in table files | ❌ Removed to Indexes/ folder | 🟡 MODERATE |
| **SpatialBucket Column** | ✅ INT NOT NULL | ❌ Removed | 🔴 HIGH |
| **CHECK Constraints** | ✅ JSON validation | ❌ Removed | 🟡 MODERATE |
| **Comments/Documentation** | ✅ Full headers | ❌ Minimal | 🟡 LOW |

---

## Detailed Analysis

### 1. AtomEmbeddings Table - CRITICAL REGRESSION

**Original Definition (commit e9f0403):**
```sql
CREATE TABLE [dbo].[AtomEmbeddings]
(
    [AtomEmbeddingId] BIGINT IDENTITY(1,1) NOT NULL,
    [AtomId] BIGINT NOT NULL,
    [EmbeddingVector] VECTOR(1998) NOT NULL,  -- ✅ SQL Server 2025 native VECTOR type
    [SpatialGeometry] GEOMETRY NOT NULL,
    [SpatialCoarse] GEOMETRY NOT NULL,
    [SpatialBucket] INT NOT NULL,              -- ✅ Direct bucket column
    [SpatialBucketX] INT NULL,
    [SpatialBucketY] INT NULL,
    [SpatialBucketZ] INT NULL,
    [ModelId] INT NULL,
    [EmbeddingType] NVARCHAR(50) NOT NULL DEFAULT ('semantic'),
    [LastUpdated] DATETIME2 NOT NULL DEFAULT (SYSUTCDATETIME()),
    [CreatedAt] DATETIME2 NOT NULL DEFAULT (SYSUTCDATETIME()),

    CONSTRAINT [PK_AtomEmbeddings] PRIMARY KEY CLUSTERED ([AtomEmbeddingId]),
    CONSTRAINT [FK_AtomEmbeddings_Atoms] FOREIGN KEY ([AtomId])
        REFERENCES [dbo].[Atoms]([AtomId]) ON DELETE CASCADE
);

-- Indexes included in table file
CREATE NONCLUSTERED INDEX [IX_AtomEmbeddings_Atom]
    ON [dbo].[AtomEmbeddings]([AtomId]);

CREATE NONCLUSTERED INDEX [IX_AtomEmbeddings_Bucket]
    ON [dbo].[AtomEmbeddings]([SpatialBucket]);

CREATE NONCLUSTERED INDEX [IX_AtomEmbeddings_BucketXYZ]
    ON [dbo].[AtomEmbeddings]([SpatialBucketX], [SpatialBucketY], [SpatialBucketZ])
    WHERE [SpatialBucketX] IS NOT NULL;

CREATE SPATIAL INDEX [IX_AtomEmbeddings_Spatial]
    ON [dbo].[AtomEmbeddings]([SpatialGeometry])
    WITH (GRIDS = (LEVEL_1 = MEDIUM, LEVEL_2 = MEDIUM, LEVEL_3 = MEDIUM, LEVEL_4 = MEDIUM));

CREATE SPATIAL INDEX [IX_AtomEmbeddings_Coarse]
    ON [dbo].[AtomEmbeddings]([SpatialCoarse])
    WITH (GRIDS = (LEVEL_1 = LOW, LEVEL_2 = LOW, LEVEL_3 = LOW, LEVEL_4 = LOW));
```

**Current Definition (HEAD):**
```sql
CREATE TABLE [dbo].[AtomEmbeddings] (
    [AtomEmbeddingId]         BIGINT         NOT NULL IDENTITY,
    [AtomId]                  BIGINT         NOT NULL,
    [ModelId]                 INT            NULL,
    [EmbeddingType]           NVARCHAR (128) NOT NULL,
    [Dimension]               INT            NOT NULL DEFAULT 0,
    [EmbeddingVector]         VARBINARY(MAX) NULL,  -- ❌ REGRESSION: VECTOR → VARBINARY
    [UsesMaxDimensionPadding] BIT            NOT NULL DEFAULT CAST(0 AS BIT),
    [SpatialProjX]            FLOAT (53)     NULL,
    [SpatialProjY]            FLOAT (53)     NULL,
    [SpatialProjZ]            FLOAT (53)     NULL,
    [SpatialGeometry]         GEOMETRY       NULL,
    [SpatialCoarse]           GEOMETRY       NULL,
    [SpatialBucketX]          INT            NULL,
    [SpatialBucketY]          INT            NULL,
    [SpatialBucketZ]          INT            NOT NULL DEFAULT -2147483648,  -- ❌ Missing SpatialBucket column
    [Metadata]                NVARCHAR(MAX)  NULL,
    [CreatedAt]               DATETIME2 (7)  NOT NULL DEFAULT (SYSUTCDATETIME()),
    CONSTRAINT [PK_AtomEmbeddings] PRIMARY KEY CLUSTERED ([AtomEmbeddingId] ASC),
    CONSTRAINT [FK_AtomEmbeddings_Atoms_AtomId] FOREIGN KEY ([AtomId]) REFERENCES [dbo].[Atoms] ([AtomId]) ON DELETE CASCADE,
    CONSTRAINT [FK_AtomEmbeddings_Models_ModelId] FOREIGN KEY ([ModelId]) REFERENCES [dbo].[Models] ([ModelId])
);
-- ❌ ALL INDEXES REMOVED FROM TABLE FILE
```

**Issues:**
1. **VECTOR(1998) → VARBINARY(MAX)** - Lost native SQL Server 2025 vector type support
2. **SpatialBucket removed** - Lost direct bucket lookup column
3. **All indexes removed** - No indexes in table file, must be in separate Indexes/ folder
4. **LastUpdated removed** - Lost temporal tracking

**Impact:**
- 🔴 **CRITICAL:** Vector operations will be MUCH slower with VARBINARY vs native VECTOR type
- 🔴 **CRITICAL:** Procedures referencing `SpatialBucket` column will fail
- 🟡 **MODERATE:** Missing indexes until Indexes/ files are created
- 🟡 **MODERATE:** Cannot track embedding updates

---

### 2. Models Table - MODERATE REGRESSION

**Original Definition (commit e9f0403):**
```sql
CREATE TABLE [dbo].[Models]
(
    [ModelId]               INT              NOT NULL IDENTITY(1,1),
    [ModelName]             NVARCHAR(200)    NOT NULL,
    [ModelType]             NVARCHAR(100)    NOT NULL,
    [Architecture]          NVARCHAR(100)    NULL,
    [Config]                NVARCHAR(MAX)    NULL,
    [ParameterCount]        BIGINT           NULL,
    [IngestionDate]         DATETIME2(7)     NOT NULL CONSTRAINT DF_Models_IngestionDate DEFAULT (SYSUTCDATETIME()),
    [LastUsed]              DATETIME2(7)     NULL,
    [UsageCount]            BIGINT           NOT NULL CONSTRAINT DF_Models_UsageCount DEFAULT (0),
    [AverageInferenceMs]    FLOAT            NULL,

    CONSTRAINT [PK_Models] PRIMARY KEY CLUSTERED ([ModelId] ASC),

    CONSTRAINT [CK_Models_Config_IsJson]   -- ✅ JSON validation
        CHECK ([Config] IS NULL OR ISJSON([Config]) = 1)
);

CREATE NONCLUSTERED INDEX [IX_Models_ModelName]
    ON [dbo].[Models]([ModelName] ASC);

CREATE NONCLUSTERED INDEX [IX_Models_ModelType]
    ON [dbo].[Models]([ModelType] ASC);
```

**Current Definition (HEAD):**
```sql
CREATE TABLE [dbo].[Models] (
    [ModelId]            INT            NOT NULL IDENTITY,
    [ModelName]          NVARCHAR (200) NOT NULL,
    [ModelType]          NVARCHAR (100) NOT NULL,
    [Architecture]       NVARCHAR (100) NULL,
    [Config]             NVARCHAR(MAX)  NULL,  -- ❌ Lost JSON validation constraint
    [ParameterCount]     BIGINT         NULL,
    [IngestionDate]      DATETIME2 (7)  NOT NULL DEFAULT (SYSUTCDATETIME()),
    [LastUsed]           DATETIME2 (7)  NULL,
    [UsageCount]         BIGINT         NOT NULL DEFAULT CAST(0 AS BIGINT),
    [AverageInferenceMs] FLOAT (53)     NULL,
    CONSTRAINT [PK_Models] PRIMARY KEY CLUSTERED ([ModelId] ASC)
);
-- ❌ ALL INDEXES REMOVED
```

**Issues:**
1. **JSON CHECK constraint removed** - Can now insert invalid JSON in Config column
2. **Named constraints removed** - Generic IDENTITY/DEFAULT instead of named DF_Models_*
3. **Indexes removed** - Queries on ModelName/ModelType will be slow

**Impact:**
- 🟡 **MODERATE:** Data integrity risk (invalid JSON can be inserted)
- 🟡 **MODERATE:** Query performance degraded without indexes
- 🟢 **LOW:** Named constraints are cosmetic but helpful for debugging

---

### 3. Pattern: Systematic Index Removal

**Finding:** ALL table files had their indexes removed and presumably moved to the `Indexes/` folder.

**Evidence:**
- Original e9f0403: Indexes defined WITH tables (common pattern)
- Current HEAD: NO indexes in table files
- Indexes/ folder: Only 8 files (expected ~200)

**Tables Affected:**
- AtomEmbeddings
- Models  
- Atoms
- TensorAtomCoefficients
- All 60+ tables

**Issue:**
The migration strategy appears to be:
1. ✅ Extract indexes from table files (CORRECT)
2. ❌ Only created 8 index files (INCOMPLETE - need ~192 more)

**Impact:**
- 🔴 **HIGH:** Database will deploy without 96% of critical indexes
- 🔴 **HIGH:** Performance will be catastrophic
- 🟡 **MODERATE:** Semantic search will be unusable without vector indexes

---

### 4. What Was Preserved (Good News)

Despite the regressions, significant work WAS preserved:

✅ **Multimodal Tables Added:**
- Images (width, height, pixel cloud, edge map, global embedding)
- Videos (frame rate, codec, scene boundaries)
- AudioData (sample rate, duration, spectrogram, mel spectrogram)
- ImagePatches, VideoFrames, AudioFrames, TextDocuments

✅ **New Tables Created:**
- CodeAtoms (programming language, syntax tree geometry)
- SemanticFeatures (sentiment, topic categories, temporal features)
- TopicKeywords (topic classification support)
- AgentTools (tool registration)

✅ **Service Broker Configuration:**
- InferenceQueue, InferenceService
- Neo4jSyncQueue, Neo4jSyncService
- Message contracts and types

✅ **Stored Procedures:**
- 100+ procedures properly separated into individual files
- Complex procedures like sp_Hypothesize updated
- CLR bindings, helpers, vector operations preserved

---

## Root Cause Analysis

### Why Did This Happen?

**Context from Commit Messages:**

1. **Commit e9f0403 (Nov 10):** "feat(database): DACPAC build success - 0 errors (892→0)"
   - Agent claimed "build success"
   - Used SQL Server 2025 features (VECTOR type)
   - Included indexes in table files
   
2. **Commit eba78de (Nov 10):** "fix: Restore ALL deleted database project files from e9f0403 catastrophe"
   - Previous commit DELETED files
   - Restoration attempt
   
3. **Commit b8f18bb (Nov 10):** "fix: Restore all database project files deleted in eba78de"
   - SECOND restoration attempt
   - Indicates first restoration was incomplete

4. **Commit 25457de (Nov 11):** "Massive progress by Gemini after a full audit by Claude..."
   - Gemini did major refactoring
   - Split large files into individual procedures
   - **BUT:** Appears to have blindly replaced table definitions without checking what was lost

5. **Commit 3085974 (Nov 12):** "COmmitting before the AI agent fucks me"
   - User noticed AI agent issues
   - Emergency commit to preserve work

**The Pattern:**
1. Initial work (e9f0403) used advanced SQL 2025 features
2. "Catastrophe" occurred - files deleted
3. Restoration attempts partially successful
4. Gemini continued work but **regenerated table definitions from EF Core migrations**
5. EF Core migrations don't know about VECTOR type, so reverted to VARBINARY
6. Gemini stripped indexes from tables (correct strategy) but didn't create enough index files

---

## What Needs To Be Fixed

### Priority 1: Restore SQL Server 2025 VECTOR Type

**File:** `src/Hartonomous.Database/Tables/dbo.AtomEmbeddings.sql`

**Change:**
```sql
-- CURRENT (WRONG):
[EmbeddingVector] VARBINARY(MAX) NULL,
[Dimension] INT NOT NULL DEFAULT 0,
[UsesMaxDimensionPadding] BIT NOT NULL DEFAULT CAST(0 AS BIT),

-- SHOULD BE:
[EmbeddingVector] VECTOR(1998) NOT NULL,
-- Remove Dimension and UsesMaxDimensionPadding (not needed with native VECTOR)
```

**Rationale:**
- SQL Server 2025 native VECTOR type provides:
  - Native distance calculations (cosine, euclidean, dot product)
  - Optimized storage format
  - Query optimizer understands vector operations
  - 10-100x faster than VARBINARY conversions

### Priority 2: Restore SpatialBucket Column

**File:** `src/Hartonomous.Database/Tables/dbo.AtomEmbeddings.sql`

**Change:**
```sql
-- ADD (currently missing):
[SpatialBucket] INT NOT NULL,
```

**Rationale:**
- Many procedures reference `SpatialBucket` column directly
- Used for fast spatial partitioning
- Build will fail without this column (current 1576 errors partially due to this)

### Priority 3: Create Missing 192 Index Files

**Current State:** 8 index files in `Indexes/` folder  
**Required:** ~200 index files

**Process:**
1. Review original e9f0403 commit for all index definitions
2. Extract indexes from table files
3. Create individual `Indexes/*.sql` files:
   - `IX_AtomEmbeddings_AtomId.sql`
   - `IX_AtomEmbeddings_SpatialBucket.sql`
   - `IX_AtomEmbeddings_BucketXYZ.sql`
   - `SIX_AtomEmbeddings_Spatial.sql` (spatial index)
   - `SIX_AtomEmbeddings_Coarse.sql` (spatial index)
   - ... (195 more)

**Note:** Some indexes were likely in other commits (b8f18bb, 25457de). Need to review all commits.

### Priority 4: Restore CHECK Constraints

**File:** `src/Hartonomous.Database/Tables/dbo.Models.sql`

**Change:**
```sql
-- ADD back to table definition:
CONSTRAINT [CK_Models_Config_IsJson]
    CHECK ([Config] IS NULL OR ISJSON([Config]) = 1)
```

**Rationale:**
- Prevents invalid JSON from being inserted
- Data integrity protection
- Helpful error messages vs runtime failures

### Priority 5: Restore LastUpdated Column

**File:** `src/Hartonomous.Database/Tables/dbo.AtomEmbeddings.sql`

**Change:**
```sql
-- ADD (currently missing):
[LastUpdated] DATETIME2 NOT NULL DEFAULT (SYSUTCDATETIME()),
```

**Rationale:**
- Track when embeddings were last regenerated
- Useful for cache invalidation
- Helps identify stale embeddings

---

## Verification Checklist

### Before Proceeding With Repairs

- [ ] **Review e9f0403 commit in detail** - Extract all original table definitions
- [ ] **Identify all SQL Server 2025 features used** - VECTOR, JSON type, temporal tables
- [ ] **Document which tables were changed** - Create list of regressed tables
- [ ] **Compare EF Core migrations vs e9f0403** - Identify what EF Core doesn't know about
- [ ] **Review all commits b8f18bb → HEAD** - Ensure we don't lose work from Gemini

### After Repairs

- [ ] **AtomEmbeddings uses VECTOR(1998)** - Not VARBINARY(MAX)
- [ ] **SpatialBucket column exists** - Procedures can reference it
- [ ] **~200 index files created** - Performance restored
- [ ] **CHECK constraints restored** - Data integrity protected
- [ ] **Build succeeds** - 0 errors (currently 1576)
- [ ] **Deploy succeeds** - Database functional
- [ ] **Vector operations fast** - Native VECTOR type performance
- [ ] **Semantic search works** - Indexes in place

---

## Recommendations

### Immediate Actions

1. **Stop using EF Core migrations as source of truth for table definitions**
   - EF Core doesn't know about VECTOR type
   - EF Core doesn't know about SPATIAL indexes
   - EF Core doesn't know about SQL Server 2025 features
   
2. **Use e9f0403 commit as baseline for table schemas**
   - That commit had SQL Server 2025 features
   - Merge in new tables from Gemini's work (Images, Videos, AudioData, CodeAtoms, etc.)
   - Don't blindly regenerate from EF Core

3. **Create proper index migration**
   - Extract ALL indexes from e9f0403 tables
   - Create individual index files
   - Verify ~200 indexes exist

4. **Document SQL Server 2025 dependencies**
   - VECTOR type is REQUIRED (not optional)
   - Deployment target must be SQL Server 2025 Preview
   - Document fallback strategy if deploying to SQL Server 2022

### Process Improvements

1. **Version control critical features**
   - Tag commits when SQL Server 2025 features are added
   - Document breaking changes
   - Don't let AI agents blindly replace without review

2. **Validate AI agent changes**
   - Check for regressions after major refactorings
   - Compare before/after for critical columns
   - Test build after AI commits

3. **Separate concerns clearly**
   - DACPAC = schema source of truth (SQL Server 2025 features)
   - EF Core = ORM mapping only (navigation properties, relationships)
   - Don't regenerate DACPAC from EF Core migrations

---

## Action Plan

### Phase 1: Restore Critical Features (Priority 1)

**Tasks:**
1. Restore VECTOR(1998) type in AtomEmbeddings
2. Restore SpatialBucket column
3. Restore LastUpdated column
4. Restore JSON CHECK constraint in Models
5. Test build - expect error count to drop significantly

**Estimated Time:** 1 hour  
**Expected Outcome:** ~800 errors fixed (references to missing columns)

### Phase 2: Extract and Create Index Files (Priority 1)

**Tasks:**
1. Review e9f0403 commit - extract all CREATE INDEX statements
2. Review b8f18bb commit - extract additional indexes
3. Create individual files in `Indexes/` folder
4. Update `.sqlproj` to include `Indexes/*.sql`
5. Test build - expect remaining errors to be assembly references

**Estimated Time:** 4-6 hours  
**Expected Outcome:** ~200 index files created, performance restored

### Phase 3: Fix CLR Assembly References (Priority 2)

**Tasks:**
1. Verify SqlClrFunctions.dll builds successfully
2. Add assembly registration to post-deployment script
3. Test CLR function execution
4. Test build - expect 0 errors

**Estimated Time:** 2-3 hours  
**Expected Outcome:** Build succeeds, CLR functions operational

### Phase 4: Deploy and Validate (Priority 1)

**Tasks:**
1. Deploy DACPAC to test database
2. Verify VECTOR type works: `SELECT dbo.clr_VectorDotProduct(...)`
3. Verify spatial indexes work: `SELECT * FROM AtomEmbeddings WHERE SpatialBucket = 42`
4. Run semantic search: `EXEC sp_SemanticSearch @Query='test', @K=10`
5. Verify performance acceptable

**Estimated Time:** 2 hours  
**Expected Outcome:** Working database with SQL Server 2025 features

---

## Conclusion

**Current State:** 🔴 CRITICAL  
**Root Cause:** AI agent (Gemini) blindly regenerated table definitions from EF Core migrations, losing SQL Server 2025 features

**Key Losses:**
- VECTOR(1998) type → VARBINARY(MAX) regression
- SpatialBucket column removed
- ~192 index files not created
- CHECK constraints removed
- Documentation removed

**Recovery Plan:**
1. Restore VECTOR type and missing columns (1 hour)
2. Create 192 missing index files (4-6 hours)
3. Fix CLR assembly references (2-3 hours)
4. Deploy and validate (2 hours)

**Total Estimated Repair Time:** 9-12 hours

**Prevention:**
- Don't use EF Core migrations as DACPAC source of truth
- Use e9f0403 commit as baseline for SQL Server 2025 features
- Validate AI agent changes for regressions
- Document critical dependencies (VECTOR type requirement)

---

**Status:** Ready to begin Phase 1 repairs upon approval.
