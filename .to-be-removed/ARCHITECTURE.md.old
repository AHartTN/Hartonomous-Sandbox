# Hartonomous Architecture

**High-Level System Architecture**

> For complete technical details, see **[docs/rewrite-guide/](./docs/rewrite-guide/)**

---

## System Overview

Hartonomous is an autonomous geometric reasoning system built on five core layers:

```
┌─────────────────────────────────────────────────────────┐
│                   User Applications                     │
│             (Web, Mobile, Desktop, APIs)                │
└───────────────────────┬─────────────────────────────────┘
                        │
          ┌─────────────▼─────────────┐
          │   Application Layer       │ ← .NET 8 Workers + APIs
          │   (Thin orchestration)    │
          └─────────────┬─────────────┘
                        │
          ┌─────────────▼─────────────┐
          │   Reasoning Layer         │ ← CoT, ToT, Reflexion
          │   (Stored procedures)     │    Agent Tools
          └─────────────┬─────────────┘
                        │
          ┌─────────────▼─────────────┐
          │   Database Layer          │ ← SQL Server 2019+
          │   (Spatial R-Tree)        │    CLR (.NET Fmwk 4.8.1)
          └─────────────┬─────────────┘
                        │
          ┌─────────────▼─────────────┐
          │   Provenance Layer        │ ← Neo4j Merkle DAG
          │   (Graph tracking)        │
          └───────────────────────────┘
```

---

## Layer 1: Database Layer (SQL Server)

**Purpose**: Core intelligence engine and data storage

### Components

**Atoms Table**:
- Content-addressable storage (SHA-256 deduplication)
- VARBINARY(64) maximum atom size
- Everything atomized: images → pixels, models → weights, text → tokens

**AtomEmbeddings Table**:
- High-dimensional vectors (VARBINARY or VECTOR type)
- 3D GEOMETRY projections via deterministic Gram-Schmidt
- Dual indexing: Spatial R-Tree + Hilbert B-Tree

**TensorAtoms Table**:
- Model weights stored as queryable GEOMETRY
- WeightsGeometry column: STPointN().STY = weight value
- Enables: `SELECT * FROM TensorAtoms WHERE WeightsGeometry.STPointN(100).STY > 0.9`

**Spatial Indexes**:
```sql
CREATE SPATIAL INDEX IX_AtomEmbeddings_SpatialGeometry
ON dbo.AtomEmbeddings (SpatialGeometry)
WITH (BOUNDING_BOX = (-1000, -1000, 1000, 1000), GRIDS = (...));
```

**CLR Functions (.NET Framework 4.8.1)**:
- `LandmarkProjection`: 1998D → 3D deterministic projection (SIMD-accelerated)
- `VectorMath`: Dot products, normalization
- `AttentionGeneration`: O(K) inference on spatial candidates
- `HilbertCurve`: Space-filling curves for linearization
- `clr_GenerateHarmonicTone`: Audio synthesis
- `GenerateGuidedPatches`: Image synthesis

### The O(log N) + O(K) Query Pattern

**Stage 1 (O(log N))**: Spatial R-Tree returns K×10 candidates
```sql
SELECT TOP 500 *
FROM dbo.AtomEmbeddings WITH (INDEX(IX_AtomEmbeddings_SpatialGeometry))
WHERE SpatialGeometry.STIntersects(@query.STBuffer(10)) = 1
ORDER BY SpatialGeometry.STDistance(@query);
```

**Stage 2 (O(K))**: Exact vector similarity on candidates
```sql
SELECT TOP 50 *
FROM SpatialCandidates
ORDER BY VECTOR_DISTANCE('cosine', EmbeddingVector, @embedding);
```

**Result**: O(log N) + O(K) instead of O(N) - logarithmic scaling

---

## Layer 2: Reasoning Layer

**Purpose**: Autonomous reasoning frameworks as stored procedures

### Components

**Chain of Thought** (`sp_ChainOfThoughtReasoning`):
- Linear step-by-step reasoning
- Each step stored in ReasoningSteps table
- CLR aggregate ChainOfThoughtCoherence for quality scoring

**Tree of Thought** (`sp_MultiPathReasoning`):
- Explores N parallel reasoning paths
- Different temperatures for diversity
- Best path selected via quality scoring
- Full tree stored in MultiPathReasoning table

**Reflexion** (`sp_SelfConsistencyReasoning`):
- Generates N independent samples
- CLR aggregate SelfConsistency finds consensus
- Agreement ratio indicates confidence
- Results stored in SelfConsistencyResults table

**Agent Tools Framework**:
- AgentTools table: Registry of available procedures/functions
- `sp_SelectAgentTool`: Semantic tool selection via spatial proximity
- `sp_ExecuteAgentTool`: Dynamic invocation with JSON parameter binding

**Behavioral Analysis**:
- SessionPaths table: User journeys as GEOMETRY (LINESTRING)
- Geometric path analysis: sessions ending in error regions, long failing paths
- Automatic UX issue detection via OODA loop

---

## Layer 3: Application Layer (.NET 8)

**Purpose**: Thin orchestration and background processing

### Worker Services

**Hartonomous.Workers.Ingestion**:
- Atomizes raw content (images, models, text, audio, video, code)
- Calls `sp_AtomizeXxx_Governed` stored procedures
- No business logic - delegates to database

**Hartonomous.Workers.EmbeddingGenerator**:
- Generates high-dimensional embeddings for atoms
- External embedding services or local models
- Stores in AtomEmbeddings.EmbeddingVector

**Hartonomous.Workers.SpatialProjector**:
- Projects embeddings to 3D GEOMETRY via CLR
- Computes Hilbert values for range scans
- Updates AtomEmbeddings.SpatialGeometry and HilbertValue

**Hartonomous.Workers.Neo4jSync**:
- Syncs atoms, inferences, reasoning chains to Neo4j
- Creates Merkle DAG relationships
- Ensures provenance completeness

**Hartonomous.Workers.Gpu** (Optional):
- Out-of-process GPU acceleration via IPC
- Named Pipes + Memory-Mapped Files
- Only for specialized workloads (not required for core operations)

### API Layer

**Hartonomous.Api** (Minimal APIs):
- Thin HTTP layer over stored procedures
- `/api/inference`, `/api/query`, `/api/reasoning`
- No business logic duplication

---

## Layer 4: OODA Loop (Autonomous Self-Improvement)

**Purpose**: Continuous self-optimization via Service Broker

### The Loop

```
┌──────────────┐
│  sp_Analyze  │ ← Observe & Orient
│ (Every 15min)│    - Query performance, index usage
└──────┬───────┘    - Reasoning quality, tool performance
       │ Service Broker message
       ▼
┌──────────────┐
│sp_Hypothesize│ ← Decide
│              │    - 7 hypothesis types
└──────┬───────┘    - IndexOptimization, QueryRegression,
       │              CacheWarming, ConceptDiscovery,
       ▼              PruneModel, RefactorCode, FixUX
┌──────────────┐
│   sp_Act     │ ← Act
│              │    - Execute safe improvements
└──────┬───────┘    - Queue dangerous ones for approval
       │
       ▼
┌──────────────┐
│  sp_Learn    │ ← Measure & Adapt
│              │    - Performance delta
└──────┬───────┘    - UPDATE model weights
       │            - Loop back to sp_Analyze
       └─────────────────┘
```

### Hypothesis Types

1. **IndexOptimization**: Create/rebuild spatial indexes, update statistics
2. **QueryRegression**: Force good plans via Query Store
3. **CacheWarming**: Preload frequently accessed atoms
4. **ConceptDiscovery**: Identify new semantic clusters via Hilbert buckets
5. **PruneModel**: DELETE low-importance weights (`DELETE FROM TensorAtoms WHERE Coefficient < @threshold`)
6. **RefactorCode**: Detect duplicate AST via spatial clustering
7. **FixUX**: Analyze SessionPaths geometrically, generate UI improvement recommendations

### Model Weight Updates

```sql
-- sp_Learn.sql:186-236
EXEC dbo.sp_UpdateModelWeightsFromFeedback
    @ModelName = 'Qwen3-Coder-32B',
    @TrainingSample = @generatedCode,
    @RewardSignal = @successScore,
    @learningRate = 0.0001;

-- Actual gradient descent on GEOMETRY
UPDATE ta
SET WeightsGeometry = dbo.fn_UpdateWeightsWithGradient(
        ta.WeightsGeometry, @gradient, @learningRate
    )
FROM dbo.TensorAtoms ta
WHERE ta.ModelId = @modelId;
```

---

## Layer 5: Provenance Layer (Neo4j)

**Purpose**: Cryptographic audit trail (Merkle DAG)

### Node Types

1. **Atom**: Content-addressable storage nodes (SHA-256 hashes)
2. **Source**: Original data sources
3. **IngestionJob**: Atomization job instances
4. **User**: Users and sessions
5. **Pipeline**: Stored procedure versions
6. **Inference**: Execution instances

### Relationship Types

1. **INGESTED_FROM**: Atom → Source
2. **CREATED_BY_JOB**: Atom → IngestionJob
3. **HAD_INPUT**: Inference → Atom (inputs)
4. **GENERATED**: Inference → Atom (outputs)
5. **USED_REASONING**: Inference → ReasoningChain
6. **SELECTED_TOOL**: AgentDecision → AgentTool
7. **CONTRIBUTED_TO_ISSUE**: SessionPath → UXIssue

### Provenance Queries

```cypher
// Trace complete reasoning chain
MATCH (inference:Inference {inferenceId: $id})
      -[:USED_REASONING]->(chain:ReasoningChain)
      -[:HAS_STEP*]->(steps:ReasoningStep)
RETURN inference, chain, steps
ORDER BY steps.stepNumber;

// Root cause analysis: What generated this atom?
MATCH path = (source:Source)<-[:INGESTED_FROM*]-(atom:Atom {atomId: $id})
RETURN path;

// Impact analysis: What depends on this atom?
MATCH path = (atom:Atom {atomId: $id})-[:HAD_INPUT*]->(dependent)
RETURN path;
```

---

## Data Flow: Complete Picture

### Ingestion Pipeline

```
Raw File (image, model, text, audio, video, code)
    ↓
[Worker: Ingestion]
    ↓ Atomization strategy
[sp_AtomizeXxx_Governed] ← T-SQL stored procedure
    ↓
[Atoms table] ← SHA-256 deduplication
    ↓ Service Broker event
[Worker: EmbeddingGenerator] → AtomEmbeddings.EmbeddingVector
    ↓
[Worker: SpatialProjector]
    ↓ CLR: LandmarkProjection.ProjectTo3D()
[AtomEmbeddings.SpatialGeometry] ← 3D GEOMETRY
    ↓ CLR: clr_ComputeHilbertValue()
[AtomEmbeddings.HilbertValue] ← BIGINT
    ↓
[Worker: Neo4jSync] → Neo4j Merkle DAG
```

### Inference Pipeline

```
User Query
    ↓
[API or sp_GenerateWithAttention]
    ↓
STAGE 1: Spatial R-Tree (O(log N))
    ↓ STIntersects(@buffer)
[500 candidate atoms]
    ↓
STAGE 2: Vector refinement (O(K))
    ↓ VECTOR_DISTANCE('cosine', ...)
[Top 50 atoms]
    ↓
STAGE 3: CLR processing (optional)
    ↓ AttentionGeneration.QueryCandidatesWithAttention()
[Generation/synthesis]
    ↓
[Result + full provenance in Neo4j]
```

---

## Cross-Modal Synthesis

**Key Insight**: All modalities in SAME 3D geometric space

```
Text embedding → 3D point (X₁, Y₁, Z₁)
Image embedding → 3D point (X₂, Y₂, Z₂)
Audio embedding → 3D point (X₃, Y₃, Z₃)

If STDistance((X₁,Y₁,Z₁), (X₂,Y₂,Z₂)) < threshold:
    → Semantically related across modalities
```

**Examples**:
- Image → Audio: Find audio atoms near image coordinates, synthesize via `clr_GenerateHarmonicTone`
- Video → Text: Compute visual centroid, find text atoms nearby, use Chain of Thought
- Code → Image: AST embedding → find image atoms → synthesize via `GenerateGuidedPatches`

---

## Determinism & Reproducibility

**Deterministic 3D Projection**:
- Gram-Schmidt orthonormalization with **fixed landmark vectors**
- Same input embedding → same 3D GEOMETRY coordinate
- Reproducible across systems and time

**Content-Addressable Storage**:
- SHA-256 hashing ensures same content → same AtomHash
- Deduplication across all data

**Provenance Merkle DAG**:
- Cryptographic verification of all transformations
- Tamper-evident audit trail

---

## Scalability

### Performance Characteristics

| Operation | Complexity | Example (1B atoms) |
|---|---|---|
| Spatial query | O(log N) | ~18ms |
| Exact vector refinement | O(K) | ~2ms (K=500) |
| Hilbert range scan | O(log N) | ~15ms |
| Model weight query | O(1) | ~1ms (single weight) |
| Model weight update | O(W) | ~100ms (W=1000 weights) |

### Scaling Strategies

**Vertical**:
- SQL Server scales to 640 CPU cores, 24TB RAM
- Spatial indexes benefit from more RAM (cache)

**Horizontal**:
- Partitioning by Hilbert value ranges
- Read replicas for query distribution
- Neo4j sharding for provenance graph

**Hybrid**:
- Hot data in SQL Server In-Memory OLTP
- Cold data in standard tables
- Archival to blob storage (Azure Blob, S3)

---

## Key Differentiators

| Aspect | Hartonomous | Traditional AI |
|---|---|---|
| ANN Algorithm | Spatial R-Tree O(log N) | Vector indexes O(N) or ANN O(N^0.5) |
| Model Storage | GEOMETRY (queryable) | Binary blobs (opaque) |
| Inference | Spatial navigation | Matrix multiplication |
| Training | UPDATE/DELETE statements | Days of GPU time |
| Reasoning | Stored procedures (CoT/ToT/Reflexion) | External prompting frameworks |
| Agents | AgentTools table registry | External libraries (LangChain, etc.) |
| Analytics | SessionPaths as GEOMETRY | Separate tools (Google Analytics) |
| Provenance | Cryptographic Merkle DAG | Best-effort logging |
| Self-Improvement | OODA loop (automatic) | Manual MLOps |

---

## Security Considerations

**CLR Security**:
- .NET Framework 4.8.1 only (SQL Server CLR limitation)
- UNSAFE permission set required (geometric operations, SIMD)
- Asymmetric key signing for assembly registration
- No .NET Standard dependencies (incompatible with CLR)

**Database Security**:
- Least privilege: Workers use specific service accounts
- Row-level security for multi-tenancy
- Encrypted connections (TLS 1.2+)
- Temporal tables for audit and rollback

**API Security**:
- Authentication: OAuth 2.0 / API keys
- Rate limiting per tenant/user
- Input validation before calling stored procedures

---

## Operational Considerations

**Monitoring**:
- OODA loop health metrics (sp_Analyze execution frequency)
- Service Broker queue depth
- Spatial index usage statistics
- Reasoning framework success rates
- Agent tool performance tracking

**Backup & Recovery**:
- SQL Server: Full + differential + log backups
- Neo4j: Snapshot backups
- DACPAC versioning for schema rollback
- Temporal tables for data rollback

**High Availability**:
- SQL Server Always On Availability Groups
- Neo4j clustering (Enterprise)
- Worker services: Multiple instances with Service Broker load balancing

---

## For More Details

**Complete Technical Specification**:
- [Rewrite Guide Index](./docs/rewrite-guide/INDEX.md)
- [Quick Reference](./docs/rewrite-guide/QUICK-REFERENCE.md) - 5-minute overview
- [Core Innovation](./docs/rewrite-guide/00.5-The-Core-Innovation.md) - The breakthrough explained
- [Complete Stack](./docs/rewrite-guide/00.6-Advanced-Spatial-Algorithms-and-Complete-Stack.md) - Full technology details

**Specific Topics**:
- [OODA Loop Deep Dive](./docs/rewrite-guide/19-OODA-Loop-and-Godel-Engine-Deep-Dive.md)
- [Reasoning Frameworks](./docs/rewrite-guide/20-Reasoning-Frameworks-Guide.md)
- [Agent Framework](./docs/rewrite-guide/21-Agent-Framework-Guide.md)
- [Cross-Modal Examples](./docs/rewrite-guide/22-Cross-Modal-Generation-Examples.md)
- [Behavioral Analysis](./docs/rewrite-guide/23-Behavioral-Analysis-Guide.md)

**Setup & Operations**:
- [Quickstart Guide](./QUICKSTART.md)
- [Setup Guides](./docs/setup/)
- [Operations Runbooks](./docs/operations/)
- [API Reference](./docs/api/)

---

**Hartonomous: Computational geometry as the foundation of intelligence. Packed in SQL Server.** 🚀
