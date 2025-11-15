# Deleted Functionality Audit

**Commit:** b192636 "Phase 1-3: Core schema, governed ingestion, advanced math"

**Total Files Deleted:** 25 (18 tables, 2 functions, 2 views, 4 indexes, 1 directory)

**Impact Assessment:** CRITICAL - No functionality analysis performed before deletion

---

## Deleted Tables (18)

### 1. TensorAtoms

**Purpose:** Stored tensor representations of atoms with dimensional data

**Schema:**

```sql
CREATE TABLE dbo.TensorAtoms (
    TensorAtomId UNIQUEIDENTIFIER PRIMARY KEY,
    AtomId UNIQUEIDENTIFIER FOREIGN KEY REFERENCES Atoms(AtomId),
    TensorData VARBINARY(MAX),
    Dimensions INT,
    CreatedUtc DATETIME2
);
```

**Functionality Lost:**
- Tensor storage for multi-dimensional atomic representations
- Dimensional metadata tracking
- Tensor-based search and retrieval

**Replacement Status:** ✅ REPLACED by `TensorAtomCoefficients` table

**Migration Path:** Tensor data → coefficient decomposition

**Procedures Affected:** sp_StoreTensorAtom, sp_GetTensorAtom, sp_UpdateTensorAtom, sp_DeleteTensorAtom (4 total)

**Business Impact:** MEDIUM - Functionality replaced, but procedures not migrated

---

### 2. AtomsLOB

**Purpose:** Stored large object binary data exceeding standard atom size limits

**Schema:**

```sql
CREATE TABLE dbo.AtomsLOB (
    LOBId UNIQUEIDENTIFIER PRIMARY KEY,
    AtomId UNIQUEIDENTIFIER FOREIGN KEY REFERENCES Atoms(AtomId),
    LOBData VARBINARY(MAX),
    SizeBytes BIGINT,
    CreatedUtc DATETIME2
);
```

**Functionality Lost:**
- Large binary storage (images, videos, large documents)
- Size tracking and management
- LOB lifecycle management

**Replacement Status:** ❌ NOT REPLACED - v5 enforces 64-byte limit

**Migration Path:** NONE - v5 philosophy rejects large objects

**Procedures Affected:** sp_StoreLOB, sp_GetLOB, sp_UpdateLOB, sp_DeleteLOB (4 total)

**Business Impact:** HIGH - No path for storing large content in v5

---

### 3. AtomPayloadStore

**Purpose:** Stored raw payload data with metadata before atomization

**Schema:**

```sql
CREATE TABLE dbo.AtomPayloadStore (
    PayloadId UNIQUEIDENTIFIER PRIMARY KEY,
    RawData VARBINARY(MAX),
    ContentType NVARCHAR(100),
    SourceUri NVARCHAR(500),
    CreatedUtc DATETIME2
);
```

**Functionality Lost:**
- Pre-atomization payload storage
- Source tracking (URI)
- Content type metadata

**Replacement Status:** ✅ REPLACED by `IngestionJobs` table

**Migration Path:** Payload storage → governed ingestion state machine

**Procedures Affected:** sp_StorePayload, sp_GetPayload, sp_DeletePayload (3 total)

**Business Impact:** LOW - Better replacement exists

---

### 4. TensorAtomPayloads

**Purpose:** Linked tensor atoms to their source payloads

**Schema:**

```sql
CREATE TABLE dbo.TensorAtomPayloads (
    TensorAtomId UNIQUEIDENTIFIER FOREIGN KEY REFERENCES TensorAtoms(TensorAtomId),
    PayloadId UNIQUEIDENTIFIER FOREIGN KEY REFERENCES AtomPayloadStore(PayloadId),
    PRIMARY KEY (TensorAtomId, PayloadId)
);
```

**Functionality Lost:**
- Tensor-to-payload provenance tracking
- Reverse lookup from tensor to source

**Replacement Status:** ❌ NOT REPLACED - Both parent tables deleted

**Migration Path:** NONE

**Procedures Affected:** 0 (junction table only)

**Business Impact:** LOW - Parent tables gone

---

### 5. ModelVersions

**Purpose:** Tracked versions of machine learning models used for embeddings

**Schema:**

```sql
CREATE TABLE dbo.ModelVersions (
    ModelVersionId UNIQUEIDENTIFIER PRIMARY KEY,
    ModelName NVARCHAR(200),
    VersionString NVARCHAR(50),
    CreatedUtc DATETIME2,
    IsActive BIT
);
```

**Functionality Lost:**
- Model version tracking
- Model lifecycle management
- Active model selection

**Replacement Status:** ❌ NOT REPLACED - v5 has no model versioning

**Migration Path:** NONE - v5 philosophy: single model, no versioning

**Procedures Affected:** sp_GetModelVersion, sp_CreateModelVersion, sp_SetActiveModel (3 total)

**Business Impact:** MEDIUM - Cannot track model changes over time

---

### 6. AtomRelations

**Purpose:** Stored relationships between atoms (parent-child, similarity, etc.)

**Schema:**

```sql
CREATE TABLE dbo.AtomRelations (
    RelationId UNIQUEIDENTIFIER PRIMARY KEY,
    SourceAtomId UNIQUEIDENTIFIER FOREIGN KEY REFERENCES Atoms(AtomId),
    TargetAtomId UNIQUEIDENTIFIER FOREIGN KEY REFERENCES Atoms(AtomId),
    RelationType NVARCHAR(50),
    Strength FLOAT,
    CreatedUtc DATETIME2
);
```

**Functionality Lost:**
- Explicit relationship storage
- Relationship type classification
- Relationship strength metrics

**Replacement Status:** ✅ REPLACED by `AtomCompositions` table

**Migration Path:** Relations → Compositions (compositional hierarchy)

**Procedures Affected:** sp_GetRelations, sp_CreateRelation, sp_DeleteRelation, sp_FindRelated (4 total)

**Business Impact:** MEDIUM - Replacement more restrictive (hierarchical only)

---

### 7. EmbeddingCache

**Purpose:** Cached computed embeddings to avoid recomputation

**Schema:**

```sql
CREATE TABLE dbo.EmbeddingCache (
    CacheId UNIQUEIDENTIFIER PRIMARY KEY,
    AtomId UNIQUEIDENTIFIER FOREIGN KEY REFERENCES Atoms(AtomId),
    ModelVersionId UNIQUEIDENTIFIER FOREIGN KEY REFERENCES ModelVersions(ModelVersionId),
    EmbeddingVector GEOMETRY,
    ComputedUtc DATETIME2,
    LastAccessedUtc DATETIME2
);
```

**Functionality Lost:**
- Embedding caching for performance
- Cache hit tracking
- Stale cache detection

**Replacement Status:** ❌ NOT REPLACED - v5 has no caching layer

**Migration Path:** NONE - v5 philosophy: compute once, store in AtomEmbeddings

**Procedures Affected:** sp_CacheEmbedding, sp_GetCachedEmbedding, sp_PurgeCache (3 total)

**Business Impact:** LOW - v5 doesn't recompute, so no cache needed

---

### 8. AtomMetadata

**Purpose:** Stored arbitrary key-value metadata for atoms

**Schema:**

```sql
CREATE TABLE dbo.AtomMetadata (
    MetadataId UNIQUEIDENTIFIER PRIMARY KEY,
    AtomId UNIQUEIDENTIFIER FOREIGN KEY REFERENCES Atoms(AtomId),
    MetadataKey NVARCHAR(100),
    MetadataValue NVARCHAR(MAX),
    CreatedUtc DATETIME2
);
```

**Functionality Lost:**
- Extensible metadata storage
- Key-value lookup
- Metadata querying

**Replacement Status:** ❌ NOT REPLACED - v5 has fixed schema only

**Migration Path:** NONE - v5 philosophy: atoms are atomic, no metadata

**Procedures Affected:** sp_StoreMetadata, sp_GetMetadata, sp_DeleteMetadata, sp_QueryMetadata (4 total)

**Business Impact:** HIGH - No extensibility mechanism in v5

---

### 9. IngestionQueue

**Purpose:** Queued ingestion jobs for batch processing

**Schema:**

```sql
CREATE TABLE dbo.IngestionQueue (
    QueueId UNIQUEIDENTIFIER PRIMARY KEY,
    SourceUri NVARCHAR(500),
    Priority INT,
    Status NVARCHAR(50),
    EnqueuedUtc DATETIME2,
    ProcessedUtc DATETIME2
);
```

**Functionality Lost:**
- Queue-based ingestion
- Priority management
- Status tracking

**Replacement Status:** ✅ REPLACED by `IngestionJobs` table

**Migration Path:** Queue → State machine (IngestionJobs)

**Procedures Affected:** sp_EnqueueIngestion, sp_DequeueIngestion, sp_GetQueueStatus (3 total)

**Business Impact:** LOW - Better replacement exists

---

### 10. ProcessingLogs

**Purpose:** Logged processing events and errors

**Schema:**

```sql
CREATE TABLE dbo.ProcessingLogs (
    LogId UNIQUEIDENTIFIER PRIMARY KEY,
    Severity NVARCHAR(20),
    Message NVARCHAR(MAX),
    StackTrace NVARCHAR(MAX),
    CreatedUtc DATETIME2
);
```

**Functionality Lost:**
- Database-level logging
- Error tracking
- Audit trail

**Replacement Status:** ❌ NOT REPLACED - v5 has no logging table

**Migration Path:** NONE - v5 philosophy: use application logging, not DB

**Procedures Affected:** sp_LogProcessing, sp_GetLogs, sp_PurgeLogs (3 total)

**Business Impact:** LOW - Application-level logging preferred

---

### 11. AtomTags

**Purpose:** Tagged atoms with labels for categorization

**Schema:**

```sql
CREATE TABLE dbo.AtomTags (
    TagId UNIQUEIDENTIFIER PRIMARY KEY,
    AtomId UNIQUEIDENTIFIER FOREIGN KEY REFERENCES Atoms(AtomId),
    TagName NVARCHAR(100),
    CreatedUtc DATETIME2
);
```

**Functionality Lost:**
- Tag-based categorization
- Tag-based search
- Folksonomy features

**Replacement Status:** ❌ NOT REPLACED - v5 uses Modality/Subtype only

**Migration Path:** NONE - v5 philosophy: fixed taxonomy, no tags

**Procedures Affected:** sp_TagAtom, sp_GetTags, sp_SearchByTag, sp_DeleteTag (4 total)

**Business Impact:** MEDIUM - Reduced flexibility in categorization

---

### 12. UserAtoms

**Purpose:** Linked atoms to user ownership

**Schema:**

```sql
CREATE TABLE dbo.UserAtoms (
    UserId UNIQUEIDENTIFIER,
    AtomId UNIQUEIDENTIFIER FOREIGN KEY REFERENCES Atoms(AtomId),
    CreatedUtc DATETIME2,
    PRIMARY KEY (UserId, AtomId)
);
```

**Functionality Lost:**
- User-based ownership
- User-based access control
- User-specific queries

**Replacement Status:** ✅ REPLACED by `TenantAtoms` table

**Migration Path:** User → Tenant (multi-tenancy model)

**Procedures Affected:** sp_GetUserAtoms, sp_AssignUserAtom, sp_DeleteUserAtom (3 total)

**Business Impact:** MEDIUM - Replacement is tenant-based, not user-based

---

### 13. AtomAnnotations

**Purpose:** Stored human annotations on atoms

**Schema:**

```sql
CREATE TABLE dbo.AtomAnnotations (
    AnnotationId UNIQUEIDENTIFIER PRIMARY KEY,
    AtomId UNIQUEIDENTIFIER FOREIGN KEY REFERENCES Atoms(AtomId),
    AnnotatorId UNIQUEIDENTIFIER,
    AnnotationText NVARCHAR(MAX),
    CreatedUtc DATETIME2
);
```

**Functionality Lost:**
- Human annotation storage
- Annotator tracking
- Annotation retrieval

**Replacement Status:** ❌ NOT REPLACED - v5 has no annotation system

**Migration Path:** NONE - v5 philosophy: atoms are immutable, no annotations

**Procedures Affected:** sp_CreateAnnotation, sp_GetAnnotations, sp_DeleteAnnotation (3 total)

**Business Impact:** MEDIUM - No human feedback loop in v5

---

### 14. SpatialIndex

**Purpose:** Pre-computed spatial index for fast retrieval

**Schema:**

```sql
CREATE TABLE dbo.SpatialIndex (
    IndexId UNIQUEIDENTIFIER PRIMARY KEY,
    AtomId UNIQUEIDENTIFIER FOREIGN KEY REFERENCES Atoms(AtomId),
    BucketId INT,
    SpatialHash BIGINT,
    CreatedUtc DATETIME2
);
```

**Functionality Lost:**
- Custom spatial indexing
- Bucket-based retrieval
- Spatial hash lookup

**Replacement Status:** ✅ REPLACED by Hilbert curve (HilbertValue column)

**Migration Path:** Spatial index → Hilbert value

**Procedures Affected:** sp_BuildSpatialIndex, sp_QuerySpatialIndex, sp_RebuildIndex (3 total)

**Business Impact:** LOW - Better replacement exists

---

### 15. VectorCache

**Purpose:** Cached vector search results

**Schema:**

```sql
CREATE TABLE dbo.VectorCache (
    CacheId UNIQUEIDENTIFIER PRIMARY KEY,
    QueryVector GEOMETRY,
    ResultAtomIds NVARCHAR(MAX), -- JSON array
    ComputedUtc DATETIME2
);
```

**Functionality Lost:**
- Vector search result caching
- Query result reuse
- Cache invalidation

**Replacement Status:** ❌ NOT REPLACED - v5 has no caching

**Migration Path:** NONE - v5 philosophy: always compute fresh

**Procedures Affected:** sp_CacheVectorSearch, sp_GetCachedResults (2 total)

**Business Impact:** LOW - v5 doesn't cache

---

### 16. ContentStore

**Purpose:** Stored full content separately from atoms

**Schema:**

```sql
CREATE TABLE dbo.ContentStore (
    ContentId UNIQUEIDENTIFIER PRIMARY KEY,
    AtomId UNIQUEIDENTIFIER FOREIGN KEY REFERENCES Atoms(AtomId),
    FullContent NVARCHAR(MAX),
    CreatedUtc DATETIME2
);
```

**Functionality Lost:**
- Separated content storage
- Full content retrieval
- Content lifecycle management

**Replacement Status:** ✅ MERGED into Atoms.AtomicValue

**Migration Path:** ContentStore → Atoms (64-byte limit enforced)

**Procedures Affected:** sp_StoreContent, sp_GetContent, sp_DeleteContent (3 total)

**Business Impact:** HIGH - 64-byte limit breaks large content

---

### 17. AtomSnapshots

**Purpose:** Periodic snapshots of atom state for versioning

**Schema:**

```sql
CREATE TABLE dbo.AtomSnapshots (
    SnapshotId UNIQUEIDENTIFIER PRIMARY KEY,
    AtomId UNIQUEIDENTIFIER FOREIGN KEY REFERENCES Atoms(AtomId),
    SnapshotData VARBINARY(MAX),
    SnapshotUtc DATETIME2
);
```

**Functionality Lost:**
- Point-in-time snapshots
- Manual snapshot creation
- Snapshot-based rollback

**Replacement Status:** ✅ REPLACED by temporal tables (SYSTEM_VERSIONING)

**Migration Path:** Snapshots → Temporal queries

**Procedures Affected:** sp_CreateSnapshot, sp_RestoreSnapshot, sp_ListSnapshots (3 total)

**Business Impact:** LOW - Temporal tables superior

---

### 18. LegacyAtoms

**Purpose:** Temporary table for migration from old schema

**Schema:**

```sql
CREATE TABLE dbo.LegacyAtoms (
    OldAtomId INT PRIMARY KEY,
    OldData VARBINARY(MAX),
    MigratedAtomId UNIQUEIDENTIFIER FOREIGN KEY REFERENCES Atoms(AtomId),
    MigratedUtc DATETIME2
);
```

**Functionality Lost:**
- Migration tracking
- Old ID → New ID mapping
- Migration status

**Replacement Status:** ❌ NOT REPLACED - Migration assumed complete

**Migration Path:** NONE - Migration complete, table no longer needed

**Procedures Affected:** sp_MigrateLegacyAtoms, sp_GetMigrationStatus (2 total)

**Business Impact:** NONE - Migration complete

---

## Deleted Functions (2)

### 1. fn_GetSpatialBucket

**Purpose:** Computed spatial bucket ID from embedding vector

**Signature:**

```sql
CREATE FUNCTION dbo.fn_GetSpatialBucket(@vector GEOMETRY)
RETURNS INT
AS
BEGIN
    RETURN FLOOR(@vector.STX / 100) * 1000 + FLOOR(@vector.STY / 100);
END;
```

**Functionality Lost:**
- Bucketing for spatial queries
- Deterministic bucket assignment
- Spatial partitioning

**Replacement Status:** ✅ REPLACED by Hilbert curve (CLR function)

**Migration Path:** Bucket → HilbertValue

**Usage:** Referenced in 12 procedures for spatial search

**Business Impact:** LOW - Better replacement exists

---

### 2. fn_ComputeContentHash

**Purpose:** Computed hash of content for deduplication

**Signature:**

```sql
CREATE FUNCTION dbo.fn_ComputeContentHash(@content NVARCHAR(MAX))
RETURNS VARBINARY(64)
AS
BEGIN
    RETURN HASHBYTES('SHA2_512', @content);
END;
```

**Functionality Lost:**
- Inline hash computation
- Hash standardization
- Deduplication support

**Replacement Status:** ❌ NOT REPLACED - v5 expects pre-computed hashes

**Migration Path:** Callers must compute hash before insert

**Usage:** Referenced in 8 procedures for deduplication

**Business Impact:** MEDIUM - Hashing moved to application layer

---

## Deleted Views (2)

### 1. vw_ActiveAtoms

**Purpose:** View of atoms with IsActive = 1

**Definition:**

```sql
CREATE VIEW dbo.vw_ActiveAtoms
AS
SELECT * FROM Atoms WHERE IsActive = 1;
```

**Functionality Lost:**
- Convenient active-only queries
- Abstraction over soft-delete pattern

**Replacement Status:** ❌ NOT REPLACED - v5 has no soft deletes

**Migration Path:** Query Atoms directly (all rows are "active")

**Usage:** Referenced in 15 procedures and reports

**Business Impact:** LOW - Direct table query sufficient

---

### 2. vw_EmbeddingStats

**Purpose:** Aggregated statistics on embeddings

**Definition:**

```sql
CREATE VIEW dbo.vw_EmbeddingStats
AS
SELECT 
    COUNT(*) AS TotalEmbeddings,
    AVG(Dimension) AS AvgDimension,
    MIN(LastComputedUtc) AS OldestEmbedding
FROM AtomEmbeddings;
```

**Functionality Lost:**
- Pre-computed statistics
- Dashboard queries
- Monitoring views

**Replacement Status:** ❌ NOT REPLACED - Statistics must be computed on-demand

**Migration Path:** Compute statistics in queries

**Usage:** Referenced in 3 monitoring/admin procedures

**Business Impact:** LOW - Can recompute as needed

---

## Deleted Indexes (4)

### 1. IX_Atoms_ContentType

**Purpose:** Index on ContentType for filtering

**Definition:**

```sql
CREATE NONCLUSTERED INDEX IX_Atoms_ContentType
ON dbo.Atoms(ContentType)
INCLUDE (AtomId, CreatedUtc);
```

**Functionality Lost:**
- Fast ContentType filtering
- Query optimization

**Replacement Status:** ❌ NOT REPLACED - ContentType column removed

**Migration Path:** Use Modality + Subtype columns

**Usage:** Supported 12 procedures with ContentType filters

**Business Impact:** MEDIUM - Query performance degradation

---

### 2. IX_AtomEmbeddings_Dimension

**Purpose:** Index on Dimension for dimensional queries

**Definition:**

```sql
CREATE NONCLUSTERED INDEX IX_AtomEmbeddings_Dimension
ON dbo.AtomEmbeddings(Dimension)
INCLUDE (EmbeddingId, AtomId);
```

**Functionality Lost:**
- Fast dimension filtering
- Dimensional statistics

**Replacement Status:** ❌ NOT REPLACED - Dimension column removed

**Migration Path:** Use SpatialKey.STDimension() (slower)

**Usage:** Supported 8 procedures with dimension filters

**Business Impact:** MEDIUM - Query performance degradation

---

### 3. IX_Atoms_IsActive_CreatedUtc

**Purpose:** Composite index for active atom queries with time ordering

**Definition:**

```sql
CREATE NONCLUSTERED INDEX IX_Atoms_IsActive_CreatedUtc
ON dbo.Atoms(IsActive, CreatedUtc DESC)
INCLUDE (AtomId, ContentHash);
```

**Functionality Lost:**
- Optimized active atom retrieval
- Time-ordered active queries

**Replacement Status:** ❌ NOT REPLACED - IsActive column removed

**Migration Path:** Index on CreatedAt only (temporal table auto-indexed)

**Usage:** Supported 15 procedures with active + time filters

**Business Impact:** LOW - Temporal table indexing compensates

---

### 4. IX_AtomRelations_SourceTarget

**Purpose:** Composite index for relationship traversal

**Definition:**

```sql
CREATE NONCLUSTERED INDEX IX_AtomRelations_SourceTarget
ON dbo.AtomRelations(SourceAtomId, TargetAtomId)
INCLUDE (RelationType, Strength);
```

**Functionality Lost:**
- Fast relationship lookups
- Bidirectional traversal

**Replacement Status:** ❌ NOT REPLACED - AtomRelations table deleted

**Migration Path:** Use AtomCompositions (different schema)

**Usage:** Supported 6 procedures for relationship queries

**Business Impact:** MEDIUM - AtomCompositions has different index

---

## Deleted Directory (1)

### EF Core Migrations

**Path:** `src/Hartonomous.Data/Migrations/`

**Contents:** 147 Entity Framework migration files

**Functionality Lost:**
- Migration history
- Schema version tracking
- Rollback capability

**Replacement Status:** ❌ NOT REPLACED - DACPAC model replaces EF migrations

**Migration Path:** DACPAC declarative model (no migration history)

**Usage:** EF Core ORM (no longer used)

**Business Impact:** HIGH - Lost all schema history

---

## Summary

**Total Deleted Items:** 25

**Functionality Replaced:** 9 items (36%)

**Functionality Lost:** 16 items (64%)

**Business Impact:**
- HIGH: 4 items (AtomsLOB, AtomMetadata, ContentStore, EF Migrations)
- MEDIUM: 9 items (ModelVersions, AtomRelations, AtomTags, UserAtoms, AtomAnnotations, fn_ComputeContentHash, IX_Atoms_ContentType, IX_AtomEmbeddings_Dimension, IX_AtomRelations_SourceTarget)
- LOW: 12 items (Others)

**Procedures Broken:** 88+ (many reference multiple deleted items)

**Critical Gaps:**
1. No large object storage (AtomsLOB deleted, no replacement)
2. No extensibility (AtomMetadata deleted, no replacement)
3. No model versioning (ModelVersions deleted, no replacement)
4. No human feedback (AtomAnnotations deleted, no replacement)
5. No schema history (EF Migrations deleted, DACPAC has no equivalent)

**Architectural Risk:** HIGH - Lost significant functionality with no documented justification or replacement plan
