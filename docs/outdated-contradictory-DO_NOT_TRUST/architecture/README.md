# Hartonomous Architecture

**SQL Server as Active Intelligence Layer**

---

## Overview

Hartonomous reimagines data storage by decomposing all content into fundamental, deduplicated atoms. SQL Server 2025 becomes the active intelligence layer - not passive storage - enabling perfect provenance, cross-modal queries, and autonomous self-improvement.

**Core Paradigm:** Treat knowledge like the periodic table. Every atom precisely defined, perfectly deduplicated, infinitely combinable.

---

## Architecture Pillars

### 1. Atomic Decomposition

**Every input decomposes to smallest fundamental units:**

- **Text** → Sentences, tokens, characters
- **Images** → Individual RGB pixels (4 bytes each)
- **Audio** → PCM samples, spectral frames
- **Models** → Individual float32 weights (4 bytes each)
- **SCADA** → Sensor readings, time-series points

**Result:** 1 sky blue pixel (#87CEEB) stored once, referenced 10,000,000 times across 10,000 images = 99.99% deduplication

**See:** [atomic-decomposition.md](atomic-decomposition.md)

---

### 2. Dual Representation

Every atom exists in TWO forms simultaneously:

#### Structural Representation (Physical)
- **Purpose:** Perfect reconstruction, sequential queries
- **Storage:** `dbo.AtomRelations` or `dbo.TensorAtomCoefficients`
- **Query:** "Get pixel at (x=100, y=200)" or "Get layer 5 weights"
- **Spatial Key:** `GEOMETRY::Point(sequenceIndex, value, 0, 0)` enables XYZM queries

#### Semantic Representation (Conceptual)  
- **Purpose:** Cross-modal similarity search
- **Storage:** `dbo.AtomEmbeddings`
- **Query:** "Find atoms semantically similar to this"
- **Spatial Key:** `GEOMETRY::Point(x, y, z, 0)` from 1998D→3D projection

**See:** [dual-representation.md](dual-representation.md)

---

### 3. Spatial Intelligence

**R-tree indexes work for ANY multi-dimensional data:**

| Data Type | Spatial Key | Index Performance |
|-----------|-------------|-------------------|
| RGB Colors | `POINT(R, G, B, 0)` | 12ms vs 2.5s (208× faster) |
| Audio Samples | `POINT(sampleIdx, channel, amplitude, 0)` | O(log n) vs O(n) |
| Embeddings (1998D→3D) | `POINT(x, y, z, 0)` | 10-50ms vs 500-1000ms |
| Model Weights | `POINT(layerIdx, row, col, 0)` | Queryable tensors |

**4-Level Indexing Hierarchy:**
1. **Spatial Bucket** (O(1)) - BIGINT hash for coarse filtering
2. **GEOMETRY R-tree** (O(log n)) - Spatial index with 4-level grids
3. **Hilbert Curve** (1D mapping) - 21-bit precision per dimension
4. **5D Trilateration** - CoordX/Y/Z/T/W for hyperspatial navigation

**See:** [spatial-intelligence.md](spatial-intelligence.md), [spatial-weight-architecture.md](spatial-weight-architecture.md)

---

### 4. Content-Addressable Storage

**SHA-256 hash as universal identifier:**

```sql
ContentHash = HASHBYTES('SHA2_256', AtomicValue)
```

**Benefits:**
- **Deduplication:** Identical content → same hash → stored once
- **Integrity:** Hash mismatch = corruption detected
- **Provenance:** Every atom traceable to source
- **Reference Counting:** `ReferenceCount` tracks usage, enables garbage collection

**Storage Savings:**
- **Embeddings:** 99.9975% (1998D vectors decomposed to atomic floats)
- **Images:** 95% (unique RGB values << total pixels)
- **Models:** 80-90% (quantized weights, checkpoint deltas)

**See:** [content-addressable-storage.md](content-addressable-storage.md)

---

### 5. Database-First Development

**DACPAC is source of truth. EF Core is read-only.**

| Approach | Hartonomous | Traditional |
|----------|-------------|-------------|
| Schema Source | `.sqlproj` (DACPAC) | `DbContext` (Code-First) |
| Schema Changes | Edit `.sql` files | Add EF Migration |
| Deployment | `SqlPackage.exe` | `dotnet ef database update` |
| Complex Logic | T-SQL procedures | C# repository methods |
| C# Entities | **Generated** from DB | **Defines** DB schema |

**Why:**
- Service Broker ACID messaging requires T-SQL
- CLR aggregates faster in-process
- Query Store analysis needs elevated privileges
- Set-based T-SQL >> C# loops for bulk operations

**See:** [database-first-paradigm.md](database-first-paradigm.md), [data-access-layer.md](data-access-layer.md)

---

### 6. Autonomous OODA Loop

**Database improves itself:**

```
Observe → Orient → Decide → Act → Learn → Observe (repeat)
```

**Implementation:**
- **sp_Analyze** - Monitor Query Store, DMVs, detect anomalies (latency spikes, missing indexes)
- **sp_Hypothesize** - Generate improvement actions (create index, rebuild stats, archive cold data)
- **sp_Act** - Execute actions transactionally
- **sp_Learn** - Measure performance delta, update model weights via feedback

**Service Broker:** ACID-guaranteed message delivery, zero network latency, queryable state

**Examples:**
- Detects slow query → hypothesizes missing index → creates index → measures 10× speedup → learns to prioritize index creation
- Detects unused embeddings → hypothesizes archival → compresses to generating functions → saves 90% storage
- Detects concept drift → hypothesizes weight update → fine-tunes model → validates accuracy improvement

**See:** [ooda-loop.md](ooda-loop.md), [service-broker-messaging.md](service-broker-messaging.md)

---

### 7. Temporal Architecture

**System-versioned tables track ALL changes:**

- `dbo.Atoms` → `dbo.AtomsHistory` (90-day retention)
- `dbo.TensorAtomCoefficients` → `dbo.TensorAtomCoefficients_History`
- `dbo.AtomRelations` → `dbo.AtomRelations_History`

**Capabilities:**
- **Time-Travel Queries:** "What did this model predict on November 1st?"
- **Weight Rollback:** `sp_RollbackWeightsToTimestamp` restores ANY weight to ANY timestamp
- **Audit Trails:** Every change logged with `ValidFrom`/`ValidTo` timestamps
- **Point-in-Time Recovery:** Snapshot database state at specific moment

**Columnstore History:** Compressed storage for temporal data (10× compression ratio)

**See:** [temporal-architecture.md](temporal-architecture.md)

---

### 8. CLR Integration

**60+ functions for what T-SQL cannot do:**

| Category | Functions | Purpose |
|----------|-----------|---------|
| **Atomizers** | `PixelAtomizer`, `WeightAtomizer`, `TextAtomizer` | Decompose content to atoms |
| **Model Parsers** | `GGUFReader`, `SafeTensorsReader`, `ONNXReader` | Parse binary model formats |
| **Spatial Operations** | `LandmarkProjection`, `HilbertCurve`, `VoronoiGenerator` | High-D → 3D projection, 1D mapping, semantic domains |
| **Neural Aggregates** | `AttentionGeneration`, `VectorAggregates`, `DimensionalityReduction` | In-database AI operations |
| **SIMD Optimization** | `System.Numerics.Vector<float>` | 50× faster dot products |

**Performance:** Dot product 0.1ms (CLR SIMD) vs 5ms (T-SQL) = **50× faster**

**Why UNSAFE:** File I/O, unmanaged memory, external library calls (MIConvexHull, MathNet.Numerics)

**See:** [clr-integration.md](clr-integration.md), [../database/clr/](../database/clr/)

---

### 9. Graph + Spatial Fusion

**Neo4j for provenance, SQL Server for queries:**

| Capability | SQL Server | Neo4j |
|------------|------------|-------|
| **Atomic Storage** | ✅ Atoms, TensorAtoms | ❌ |
| **Spatial Queries** | ✅ R-tree, KNN, trilateration | ❌ |
| **Temporal Queries** | ✅ System-versioned tables | ❌ |
| **Provenance Graph** | ⚠️ Graph tables (limited) | ✅ MATCH traversal |
| **Lineage Analysis** | ⚠️ Recursive CTEs | ✅ Native graph |

**Architecture:** SQL Server = source of truth, Neo4j = synchronized view for graph queries

**See:** [neo4j-provenance.md](neo4j-provenance.md)

---

### 10. Multi-Tenancy

**Row-level security via `TenantId`:**

```sql
CREATE SECURITY POLICY dbo.TenantFilter
ADD FILTER PREDICATE dbo.fn_TenantAccessPredicate(TenantId) ON dbo.Atoms,
ADD FILTER PREDICATE dbo.fn_TenantAccessPredicate(TenantId) ON dbo.AtomEmbeddings;
```

**Isolation:**
- Users see only their tenant's atoms
- Shared atoms (e.g., public models) have `TenantId = 0`
- Cross-tenant queries require elevated permissions

**See:** [multi-tenancy.md](multi-tenancy.md)

---

## Key Concepts

### Atoms

**Fundamental, immutable, content-addressable units.**

- **AtomId** - Surrogate key (BIGINT)
- **ContentHash** - SHA-256 hash (BINARY(32), UNIQUE)
- **AtomicValue** - Raw bytes (VARBINARY(64) max, schema-enforced)
- **Modality** - Content type (text, image, audio, model, scada)
- **Subtype** - Granular type (sentence, rgb-pixel, float32-weight)
- **ReferenceCount** - Usage tracking for garbage collection

**Schema Governance:** `AtomicValue` capped at 64 bytes prevents "Trojan Horse" attacks (e.g., 1GB blob disguised as atom)

---

### AtomRelations

**Structural relationships between atoms:**

- **SourceAtomId** - Parent atom (e.g., document, image)
- **TargetAtomId** - Component atom (e.g., sentence, pixel)
- **SequenceIndex** - Ordering (0-based)
- **RelationType** - Relationship type (Composition, Derivation, Reference)
- **SpatialKey** - `GEOMETRY::Point(sequenceIndex, value, 0, 0)` for XYZM queries

**Enables:** Perfect reconstruction, sequential access, structural queries

---

### AtomEmbeddings

**Semantic projections for similarity search:**

- **AtomId** - Reference to atom
- **ModelId** - Embedding model used
- **SpatialKey** - `GEOMETRY::Point(x, y, z, 0)` from 1998D→3D projection
- **HilbertValue** - 1D Hilbert curve mapping (BIGINT)

**Enables:** Cross-modal search, approximate KNN, semantic clustering

---

### TensorAtomCoefficients

**Model weight storage with queryable structure:**

- **TensorAtomId** - Reference to weight atom
- **ModelId** - Model identifier
- **LayerIdx** - Layer index (0-based)
- **PositionX/Y/Z** - Tensor coordinates (row, col, depth)
- **SpatialKey** - `GEOMETRY::Point(PositionX, PositionY, PositionZ, LayerIdx)`

**Enables:** `SELECT * FROM weights WHERE value > 0.9 AND layerType='attention'`

**Temporal:** System-versioned for weight evolution tracking

---

## Design Principles

1. **Database is Intelligence Layer** - Not passive storage, active compute engine
2. **Atoms Are Universal** - Same storage for text, images, audio, models, SCADA
3. **Deduplication Over Performance** - 0.8ms reconstruction acceptable for 99.9975% savings
4. **Spatial Indexing for Multi-Dimensional Data** - R-trees work for embeddings, RGB, audio, weights
5. **Provenance is First-Class** - Every atom traceable to source via ContentHash and lineage
6. **Autonomous by Default** - OODA loop self-optimizes without human intervention
7. **Type Safety Where Possible** - Reference tables for enums, schema-level constraints
8. **Transactional Deployment** - DACPAC ensures schema consistency
9. **CLR for What T-SQL Cannot Do** - SIMD, complex algorithms, external formats
10. **Documentation Explains WHY** - Every decision justified, trade-offs explicit

---

## Architecture Diagrams

### Data Flow

```
Input (Text/Image/Audio/Model)
    ↓
Atomizer (CLR: PixelAtomizer, WeightAtomizer, TextAtomizer)
    ↓
AtomCandidate (in-memory structure)
    ↓
AtomIngestionService (C#: hash, deduplicate, persist)
    ↓
┌─────────────────────────────────────────┐
│ dbo.Atoms (ContentHash, AtomicValue)    │
│ dbo.AtomRelations (Structural)          │ ← Structural Representation
│ dbo.TensorAtomCoefficients (Weights)    │
└─────────────────────────────────────────┘
    ↓
Embedding Generation (CLR or external model)
    ↓
┌─────────────────────────────────────────┐
│ dbo.AtomEmbeddings (SpatialKey, Hilbert)│ ← Semantic Representation
└─────────────────────────────────────────┘
    ↓
Query (SQL: spatial KNN, trilateration, temporal)
    ↓
Result (reconstructed content or atom IDs)
```

### OODA Loop

```
┌─────────────┐
│ sp_Analyze  │ Monitor Query Store, DMVs
└──────┬──────┘
       ↓ AnalyzeMessage (Service Broker)
┌─────────────────┐
│ sp_Hypothesize  │ Generate improvement actions
└──────┬──────────┘
       ↓ HypothesizeMessage
┌─────────────┐
│   sp_Act    │ Execute actions (CREATE INDEX, UPDATE STATISTICS)
└──────┬──────┘
       ↓ ActMessage
┌─────────────┐
│  sp_Learn   │ Measure delta, update model weights
└──────┬──────┘
       ↓ (loop back to sp_Analyze)
```

---

## Further Reading

- **[Atomic Decomposition](atomic-decomposition.md)** - Deep dive into atom storage
- **[Dual Representation](dual-representation.md)** - Structural vs Semantic storage
- **[Spatial Intelligence](spatial-intelligence.md)** - R-tree indexing for multi-dimensional data
- **[Database-First Paradigm](database-first-paradigm.md)** - DACPAC workflow
- **[OODA Loop](ooda-loop.md)** - Autonomous self-improvement
- **[CLR Integration](clr-integration.md)** - SIMD optimization, UNSAFE permissions
- **[Temporal Architecture](temporal-architecture.md)** - System-versioned tables, time-travel queries
- **[Content-Addressable Storage](content-addressable-storage.md)** - SHA-256 deduplication
- **[Multi-Tenancy](multi-tenancy.md)** - Row-level security, tenant isolation
