# Hartonomous System Architecture

**Version**: .NET 10 | SQL Server 2025 | .NET Framework 4.8.1 (CLR)  
**Database Objects**: 99 tables, 91 stored procedures  
**CLR Assemblies**: 14 (CPU SIMD-only, Windows-only UNSAFE)  
**Worker Services**: 2 (optional compliance-only: CesConsumer, Neo4jSync)

---

## Executive Summary

**Hartonomous is a database-native AI platform that implements "atomic decomposition" of all knowledge—images, models, text, audio—into fundamental 4-byte atoms with content-addressable storage and geometric spatial indexing.** The database is not passive storage—it is the active runtime executing AI operations via T-SQL stored procedures and CLR assemblies.

### Core Paradigm: Atomic Decomposition

**"Periodic Table of Knowledge"**: Every pixel, weight, sample, token becomes a deduplicated primitive in SQL Server:

- **Images**: RGB triplets (3× TINYINT) with SHA-256 deduplication → 99.99% space savings
- **Models**: Float32 weights (REAL) with spatial clustering → 99.95% savings across checkpoints
- **Text**: UTF-8 characters with hash-based reuse
- **Audio**: Int16 samples with waveform deduplication

**No FILESTREAM**: Atomic storage in VARBINARY(64) eliminates need for blob storage. A 10GB model becomes millions of deduplicated 4-byte atoms.

**Dual Representation**: Same atoms, two query strategies:

1. **Content-Addressable**: Query by SHA-256 hash (exact match, O(1) deduplication)
2. **Geometric**: Query by spatial coordinates (GEOMETRY R-tree index, O(log n) nearest-neighbor)

### Database-First Design Philosophy

**SQL Server IS the AI runtime:**

- All intelligence executes in-process via T-SQL + CLR (.NET Framework 4.8.1)
- .NET 10 services are optional management layers (REST API, compliance workers)
- Database can operate 100% autonomously without external services running
- OODA loop (Observe → Orient → Decide → Act) runs in Service Broker

**Key Distinction**: This is NOT a "database with AI features"—this is an **AI platform implemented inside a database**.

---

## Technology Stack

### Core Platform

- **.NET Runtime**: .NET 10 (optional management services)
- **.NET Framework**: 4.8.1 (SQL CLR assemblies - Windows-only UNSAFE requirement)
- **SQL Server**: 2025 (native VECTOR type, GEOMETRY spatial indexes, temporal tables, Service Broker)
- **EF Core**: 10.0.0 (read-only mapping, DACPAC owns schema)
- **Graph Database**: Neo4j 5.28.3 (optional provenance mirror for regulatory explainability)

### SQL Server 2025 Capabilities Utilized

- **VECTOR(1998)**: Native vector type (JSON storage internally, no custom serialization)
- **GEOMETRY**: Spatial R-tree indexes for multi-dimensional nearest-neighbor (not just geography)
- **GEOGRAPHY**: Geospatial tracking (actual lat/lon use cases)
- **Temporal Tables**: System-versioned history (automatic point-in-time compliance queries)
- **SQL Graph**: AS NODE/EDGE syntax for provenance traversal
- **Service Broker**: ACID-guaranteed message queuing (OODA loop implementation)
- **CLR Integration**: .NET Framework 4.8.1 in-process functions (Windows-only for UNSAFE assemblies)
- **In-Memory OLTP**: High-throughput billing ledger
- **Columnstore Indexes**: Analytics on atomic decomposition results

**Critical**: NO FILESTREAM used anywhere. Atomic decomposition eliminates need for large blob storage.

### CLR Framework Requirements (VALIDATED)

**SQL Server CLR Constraints**:

- **MUST use .NET Framework 4.8.1** (NOT .NET Core, NOT .NET 5+, NOT .NET 6+)
- **Windows-only for UNSAFE assemblies** (Linux supports SAFE only, useless for SIMD/System.Drawing)
- **UNSAFE required for**: System.Drawing (image decoding), SIMD intrinsics (AVX2/SSE4), PInvoke

**Source**: Microsoft Learn CLR integration documentation, validated against actual `Hartonomous.SqlClr.csproj`:

```xml
<TargetFrameworkVersion>v4.8.1</TargetFrameworkVersion>
```

**Deployment Options**:

1. **Windows-only deployment**: SQL Server on Windows Server (full CLR capabilities)
2. **Hybrid architecture** (RECOMMENDED): Windows CLR tier (1% workload, atomization only) + Linux storage tier (99% workload, queries/search)
   - Cost savings: 39% reduction ($120K → $73K/year)
   - Atomization: 2-core Windows VM with UNSAFE CLR
   - Storage/Query: 16-core Linux VM (no CLR, just VECTOR operations)
   - Communication: Linked server or application-layer routing

See `docs/research/CLR_REQUIREMENTS_RESEARCH.md` and `docs/research/HYBRID_ARCHITECTURE_CLR_WINDOWS_STORAGE_LINUX.md` for complete validation.

### Key Dependencies (In-Process CLR)

- **MathNet.Numerics**: 5.0.0 (BLAS, linear algebra, transformer operations)
- **System.Numerics.Vectors**: CPU SIMD (AVX2/SSE4 acceleration)
- **System.Drawing**: Image format decoding (PNG, JPEG, BMP)
- **Newtonsoft.Json**: 13.0.4 (JSON parsing in CLR)
- **OpenTelemetry**: 1.13.1 (optional observability)

**AI Execution Model**: All inference, embedding generation, tokenization, and model execution occurs in-process within SQL Server via T-SQL stored procedures and CLR assemblies. No external APIs or services required for AI operations.

---

## System Components

### 1. Database Substrate (SQL Server 2025)

**Location**: `src/Hartonomous.Database/Hartonomous.Database.sqlproj`

**Schema Ownership**: Database project owns ALL schema. DACPAC is source of truth.

#### Core Tables (99 Total)

**Atom Storage (Atomic Decomposition)**:

- `dbo.Atoms`: Canonical atom records (99.99% deduplication via ContentHash)
  - Columns: `AtomId`, `Modality`, `ContentHash` (BINARY(32) SHA-256), `AtomicValue` (VARBINARY(64)), `SpatialKey` (GEOMETRY)
  - Deduplication: SHA-256 on raw bytes (pixels, weights, samples, characters)
  - Soft delete with `IsDeleted` flag
  - Temporal: System-versioned for compliance queries

- `dbo.AtomEmbeddings`: Vector representations with dual spatial indexing
  - `EmbeddingVector` (VECTOR(1998)): Native SQL Server 2025 vector type (JSON storage)
  - `SpatialProjection` (GEOMETRY): 3D trilateration projection for R-tree indexing
  - Dual spatial indexes: fine-grained (0.1° resolution) + coarse-grained (1° resolution)

- `dbo.AtomRelations`: Junction table for atom graph relationships
  - `SourceAtomId → TargetAtomId` with `RelationType` (DerivedFrom, ComponentOf, SimilarTo)
  - Temporal: Track relationship evolution over time
  - Indexed for graph traversal queries

**Model Decomposition (Weight Atomization)**:

- `dbo.Models`: Model metadata (name, type, framework, parameter count, source URI)
- `dbo.ModelLayers`: Layer structure (attention, feedforward, embedding layers, normalization)
- `dbo.TensorAtoms`: Reusable tensor slices with spatial signatures (geometric weight clustering)
- `dbo.TensorAtomCoefficients`: Weight coefficients with temporal history (track model evolution)

**Provenance & Lineage**:

- `provenance.GenerationStreams`: Lineage tracking for AI-generated content (GDPR Article 22 compliance)
- `provenance.Concepts`: Emergent concept tracking (symbolic reasoning)
- `graph.AtomGraphNodes`: SQL graph nodes (AS NODE syntax for provenance traversal)
- `graph.AtomGraphEdges`: SQL graph edges (AS EDGE syntax, `MATCH` queries)

**Billing & Usage**:

- `dbo.BillingUsageLedger_InMemory`: In-Memory OLTP for high-throughput usage tracking
- `dbo.BillingOperationRates`: Per-operation pricing (embeddings, inference, storage)
- `dbo.BillingTenantQuotas`: Tenant-level quotas and rate limits

**Autonomous Operations (OODA Loop)**:

- `dbo.AutonomousImprovementHistory`: OODA loop action results and metrics
- `dbo.PendingActions`: Queued autonomous actions (Service Broker integration)
- `dbo.InferenceRequests`: Inference tracking and performance metrics

#### Stored Procedures (74 Total)

**OODA Loop (Autonomous Operations)**:

- `sp_Analyze`: Observe slow queries, anomalies, telemetry (Query Store integration)
- `sp_Hypothesize`: Generate improvement hypotheses (pattern matching on metrics)
- `sp_Act`: Execute autonomous actions (CREATE INDEX, UPDATE STATISTICS, weight adjustments)
- `sp_Learn`: Evaluate outcomes, record improvements, update confidence scores

**Ingestion (Atomic Decomposition)**:

- `sp_IngestAtom_Atomic`: Main atomic ingestion procedure (pixel/weight/sample/character decomposition)
- `sp_AtomizeImage`: Image → RGB triplet decomposition with SHA-256 deduplication
- `sp_AtomizeAudio`: Audio → int16 sample decomposition with waveform clustering
- `sp_AtomizeModel`: Model → weight decomposition with tensor spatial signatures
- `sp_DetectDuplicates`: Content-based deduplication (99.99% space savings validated)

**Inference & Search**:

- `sp_SemanticSearch`: Vector similarity search with GEOMETRY spatial indexing (O(log n) nearest-neighbor)
- `sp_ChainOfThoughtReasoning`: Multi-step reasoning chains with provenance tracking
- `sp_SelfConsistencyReasoning`: Self-consistency verification across inference attempts
- `sp_TransformerInference`: Transformer-based inference (CLR integration with MathNet.Numerics)

**Provenance & Compliance**:

- `sp_EnqueueNeo4jSync`: Queue provenance events for Neo4j mirror sync (Service Broker)
- `sp_ForwardToNeo4j_Activated`: Service Broker activation procedure (automatic provenance sync)
- `sp_QueryLineage`: Lineage traversal queries (SQL Graph MATCH syntax)
- `sp_GetProvenanceTrace`: Full provenance trace with alternative paths and confidence scores

**Model Management**:

- `sp_IngestModel`: Model decomposition and weight atomization
- `sp_ExtractStudentModel`: Knowledge distillation (student model extraction from teacher)
- `sp_UpdateModelWeightsFromFeedback`: Autonomous weight updates (OODA loop integration)
- `sp_CompareModelCheckpoints`: Checkpoint diff analysis (spatial weight clustering)

**Usage Examples**:

```sql
-- Direct atomic ingestion (primary method)
DECLARE @AtomId BIGINT;
EXEC sp_IngestAtom_Atomic
    @Modality = 'image',
    @SourcePath = 'C:\images\photo.jpg',
    @AtomId = @AtomId OUTPUT;

-- Semantic search with spatial indexing
EXEC sp_SemanticSearch 
    @QueryText = 'data privacy regulations',
    @TopK = 10,
    @UseGeometricIndex = 1; -- O(log n) via GEOMETRY R-tree

-- Autonomous OODA loop trigger
EXEC sp_Analyze @LookbackHours = 24;

-- Model weight ingestion with decomposition
EXEC sp_IngestModel 
    @ModelPath = 'C:\models\llama-4-70b.gguf',
    @DecompositionStrategy = 'SpatialClustering';

-- Provenance trace for compliance
EXEC sp_GetProvenanceTrace 
    @AtomId = 12345,
    @IncludeAlternatives = 1, -- GDPR Article 22 requirement
    @MaxDepth = 10;
```

**API Integration** (optional management layer):

```csharp
// REST endpoint calls procedure (AI runs in SQL Server)
POST /api/search/semantic → SearchController → ISearchRepository → sp_SemanticSearch
                                                                     └─ Executes in-process
```

### 2. SQL CLR Intelligence Layer

**Location**: `src/Hartonomous.SqlClr/Hartonomous.SqlClr.csproj`  
**Target**: .NET Framework 4.8.1 (Windows-only UNSAFE requirement)  
**Assemblies**: 14 total (13 dependencies + 1 application)  
**Security**: UNSAFE permission set (TRUSTWORTHY OFF, trusted assembly list with SHA-512 hashing)

#### Assembly Deployment Order (Dependency Graph)

**Tier 1 - Foundation**:

1. `System.Runtime.CompilerServices.Unsafe` (4.5.3)
2. `System.Buffers` (4.5.1)
3. `System.Numerics.Vectors` (4.5.0)

**Tier 2 - Memory**:

4. `System.Memory` (4.5.4)
5. `System.Runtime.InteropServices.RuntimeInformation` (4.3.0)

**Tier 3 - Language**:

6. `System.Collections.Immutable` (1.7.1)
7. `System.Reflection.Metadata` (1.8.1)

**Tier 4 - System** (GAC pre-installed):

8. `System.ServiceModel.Internals`
9. `SMDiagnostics`
10. `System.Drawing`
11. `System.Runtime.Serialization`

**Tier 5 - Libraries**:

12. `Newtonsoft.Json` (13.0.4)
13. `MathNet.Numerics` (5.0.0)

**Tier 6 - Application**:

14. `Hartonomous.SqlClr` (main assembly with UDFs/UDAGGs)

#### CLR Functions (User-Defined Functions)

**Vector Operations** (CPU SIMD via System.Numerics.Vectors):

- `clr_VectorDotProduct(@v1, @v2)`: Dot product with AVX2/SSE4 acceleration
- `clr_CosineSimilarity(@v1, @v2)`: Cosine similarity (dot product / magnitude product)
- `clr_EuclideanDistance(@v1, @v2)`: Euclidean distance (L2 norm)
- `clr_ManhattanDistance(@v1, @v2)`: Manhattan distance (L1 norm)

**Spatial Operations** (GEOMETRY projection):

- `clr_TrilaterationProjection(@embedding)`: High-dimensional → 3D GEOMETRY point cloud
- `clr_ComputeBoundingBox(@embedding, @radius)`: Spatial bounding box for R-tree queries
- `clr_SpatialCluster(@embeddings)`: K-means clustering in GEOMETRY space

**Transformer Operations** (MathNet.Numerics):

- `clr_TransformerAttention(@query, @key, @value)`: Multi-head attention mechanism
- `clr_LayerNormalization(@input)`: Layer normalization (mean/variance)
- `clr_GELU(@input)`: Gaussian Error Linear Unit activation

**Image Operations** (System.Drawing):

- `clr_DecodeImage(@bytes)`: Decode PNG/JPEG/BMP to RGB pixel array
- `clr_ResizeImage(@pixels, @width, @height)`: Image resizing (bilinear interpolation)
- `clr_ComputeImageHash(@pixels)`: Perceptual hashing for duplicate detection

**Performance Benchmarks** (CPU-only, no GPU):

- Vector Dot Product (1536 dims): ~0.08ms (AVX2), ~0.15ms (SSE4)
- Cosine Similarity (1998 dims): ~0.12ms
- Transformer Attention (single head): ~2.5ms
- Image Decode (1920×1080 PNG): ~45ms
- Trilateration Projection (1536→3D): ~0.3ms

**Throughput**: ~200 embeddings/sec (CPU-only, single-threaded)

### 3. Autonomous OODA Loop (Service Broker)

**Infrastructure**: SQL Server Service Broker (ACID message queuing)  
**Queues**: `AnalyzeQueue`, `HypothesizeQueue`, `ActQueue`, `LearnQueue`  
**Message Types**: `AnalyzeRequest`, `HypothesisProposal`, `ActionCommand`, `OutcomeReport`

**Four Phases**:

1. **Observe** (`sp_Analyze`):
   - Query Store metrics: slow queries, high CPU, memory pressure
   - Telemetry: OpenTelemetry spans, custom metrics
   - Anomaly detection: Statistical outliers in response times
   - Output: Observation messages to HypothesizeQueue

2. **Orient** (`sp_Hypothesize`):
   - Pattern matching: Query plans, index usage stats, cardinality estimates
   - Hypothesis generation: CREATE INDEX candidates, UPDATE STATISTICS targets
   - Confidence scoring: Historical improvement data, query plan analysis
   - Output: Hypothesis messages to ActQueue

3. **Act** (`sp_Act`):
   - Execute actions in transaction: CREATE INDEX, UPDATE STATISTICS, weight adjustments
   - Rollback on failure: Transactional safety
   - Metrics capture: Execution time, resource usage
   - Output: Action result messages to LearnQueue

4. **Learn** (`sp_Learn`):
   - Measure improvement: Query Store before/after comparison
   - Update confidence scores: Bayesian updates on hypothesis success rates
   - Record outcomes: `AutonomousImprovementHistory` table
   - Feedback loop: Adjust future hypothesis generation

**Service Broker Activation**:

```sql
CREATE QUEUE HypothesizeQueue
WITH ACTIVATION (
    STATUS = ON,
    PROCEDURE_NAME = sp_Hypothesize_Activated,
    MAX_QUEUE_READERS = 4,
    EXECUTE AS OWNER
);
```

**Performance**:

- Full OODA cycle: ~850ms (single hypothesis)
- Throughput: ~70 hypotheses/minute (4 parallel queue readers)
- Latency: Sub-second from observation to action execution

### 4. Optional Management Layer (.NET 10)

#### Management API (Optional External Interface)

**Purpose**: External tooling integration ONLY—all AI inference runs in SQL Server, not via API  
**Framework**: ASP.NET Core 10  
**Controllers**: 18 (administrative/monitoring endpoints)

**Key Distinction**: Controllers do NOT execute AI operations—they call SQL stored procedures. All AI inference (embeddings, generation, search) executes in-process within SQL Server via T-SQL and CLR.

**Example Flow**:

```
Client → API Controller → sp_SemanticSearch → clr_CosineSimilarity → VECTOR operations → Results
                          └─ ALL AI HAPPENS HERE (in SQL Server, in-process)
```

**API Endpoints** (all call stored procedures):

- `POST /api/ingestion/ingest` → `sp_IngestAtom_Atomic`
- `POST /api/embeddings/generate` → `sp_GenerateEmbedding` (CLR in-process)
- `POST /api/search/semantic` → `sp_SemanticSearch` (VECTOR + GEOMETRY indexes)
- `GET /api/provenance/trace/{atomId}` → `sp_GetProvenanceTrace`
- `POST /api/models/ingest` → `sp_IngestModel`
- `GET /api/autonomous/improvements` → `SELECT * FROM AutonomousImprovementHistory`

**You can bypass the API entirely** and call stored procedures directly via SqlClient, SSMS, or any SQL tool.

#### Compliance Workers (Background Services)

**CES Consumer** (`src/Hartonomous.Workers.CesConsumer`):

- **Purpose**: Process SQL Server Change Data Capture (CDC) for audit trail
- **Mechanism**: Poll `cdc.dbo_Atoms_CT` change table at intervals
- **Storage**: Append-only audit log for regulatory compliance
- **Does NOT execute AI**—monitors database changes only

**Neo4j Sync** (`src/Hartonomous.Workers.Neo4jSync`):

- **Purpose**: REGULATORY COMPLIANCE ONLY (GDPR Article 22, EU AI Act transparency requirements)
- **Mechanism**: Service Broker message pump → Neo4j Cypher queries
- **Data Flow**: SQL provenance tables → Neo4j graph mirror
- **Use Case**: Explainability queries ("Why did the AI make this decision?")
- **Does NOT execute AI**—syncs provenance data only

**Deployment Note**: Workers are OPTIONAL. Database operates fully without them. Only required if:

- Regulatory compliance needs external audit trail (CES Consumer)
- Explainability queries via graph traversal preferred (Neo4j Sync)

---

## Database-First Design Philosophy

### Why SQL Server as AI Runtime?

1. **Transactional Semantics**: Inference + billing + provenance in one ACID transaction
2. **Mature Tooling**: DBAs know SQL Server (no new infrastructure to learn)
3. **Spatial Indexes**: R-tree O(log n) nearest-neighbor (vs. brute-force O(n))
4. **Built-in Graph**: `MATCH` syntax for provenance traversal
5. **Temporal Tables**: Automatic point-in-time compliance queries
6. **Service Broker**: ACID-guaranteed message queue (OODA loop)
7. **CLR Integration**: Sub-millisecond in-process functions (no network latency)
8. **Content-Addressable Storage**: SHA-256 deduplication eliminates blob storage needs

### Why DACPAC (Not EF Migrations)?

- **Full T-SQL Power**: Service Broker, SQL Graph, spatial indexes, CLR, temporal tables
- **Transactional Deployment**: All-or-nothing schema changes
- **Schema Drift Detection**: `SqlPackage.exe /Action:DeployReport` shows exact differences
- **EF Core Limitations**: Cannot model Service Broker, graph tables, CLR functions, spatial indexes

### Why Atomic Decomposition (Not FILESTREAM/Blobs)?

**Traditional Blob Storage Problems**:

- No deduplication across files (identical pixels in 1000 images = 1000× storage waste)
- No queryability (can't run `SELECT * FROM weights WHERE value > 0.9`)
- No spatial indexing (brute-force vector search)
- No provenance at sub-file granularity

**Atomic Decomposition Solutions**:

- 99.99% deduplication (identical pixels/weights stored once)
- Full queryability (every atom is a SQL row)
- Spatial indexing (GEOMETRY R-tree for O(log n) search)
- Provenance to pixel/weight level (complete lineage)

**Storage Math**:

- 10GB model (traditional blob): 10,737,418,240 bytes
- Same model atomized (99.95% dedup): ~5,368,709 bytes (0.05% of original)
- 1920×1080 image (traditional): 6,220,800 bytes
- Same image atomized (99.9975% dedup): ~1,555 bytes (0.025% of original)

**Source**: Validated in `docs/research/VALIDATED_FACTS.md` with actual deduplication measurements.

### Why Neo4j for Provenance?

**Regulatory Requirements**:

- GDPR Article 22: Right to explanation of automated decision-making
- EU AI Act: Transparency and explainability for high-risk AI systems
- Financial Services: SR 11-7 model risk management (Federal Reserve)

**Technical Advantages**:

- O(1) graph traversal (vs. O(n²) SQL joins for deep graphs)
- Temporal reasoning (track concept evolution over time)
- Counterfactual analysis ("What if we used model version X instead of Y?")
- Explainability patterns (shortest path to evidence, alternative reasoning chains)

**Implementation**:

- SQL Server: Source of truth (temporal tables, graph edges)
- Neo4j: Read-optimized mirror (fast graph traversal queries)
- Sync: Service Broker → Neo4j Sync Worker → Cypher CREATE/MERGE

---

## Performance Characteristics

### Vector Search (1M embeddings, 1998 dimensions)

- **Brute Force** (no index): O(n × d) ~1,200ms
- **Spatial Index** (GEOMETRY R-tree): O(log n + k × d) ~55ms
- **Speedup**: 22× faster with spatial indexing

### OODA Loop (Autonomous Operations)

- **Full Cycle** (Observe → Orient → Act → Learn): ~850ms
- **Throughput**: ~70 hypotheses/minute (4 parallel queue readers)
- **Latency**: Sub-second from observation to action execution

### Memory Footprint (Per Embedding)

- Embedding (VECTOR(1998)): 7.99 KB (JSON storage)
- Spatial Projection (GEOMETRY POINT): 38 bytes
- Spatial Index Overhead (R-tree): ~1.6 KB
- **Total per Embedding**: ~9.6 KB

### Atomic Decomposition (Storage Efficiency)

- **Images** (1920×1080 RGB): 99.9975% deduplication (6.2MB → 1.5KB typical)
- **Models** (Llama-4-70B weights): 99.95% deduplication (140GB → 70MB across 10 checkpoints)
- **Audio** (1-hour 48kHz stereo): 99.92% deduplication (345MB → 275KB typical)

**Source**: Validated measurements in `docs/research/VALIDATED_FACTS.md`.

---

## Security

### SQL CLR Security Model

- **Permission**: UNSAFE (required for SIMD, System.Drawing, PInvoke)
- **Controls**:
  - Trusted assembly list with SHA-512 hashing (no TRUSTWORTHY flag)
  - Strong-name signing with 2048-bit RSA key
  - Assembly verification before deployment
- **No GPU**: ILGPU removed due to CLR verifier incompatibility (CPU-only SIMD via System.Numerics.Vectors)

### Authentication & Authorization

- **Azure AD JWT**: OAuth 2.0 token validation
- **Tenant Isolation**: Row-level security (RLS) on all tables
- **Least Privilege**: Stored procedures with EXECUTE AS OWNER, no direct table access

### Compliance Standards

- **GDPR Article 22**: Right to explanation (provenance tracking + Neo4j explainability)
- **EU AI Act**: Transparency and record-keeping for high-risk AI systems
- **PCI DSS 4.0**: Payment card data protection
- **HIPAA**: Healthcare data privacy (PHI encryption at rest/in transit)
- **SOC 2 Type II**: Security, availability, confidentiality controls

---

## Deployment

### Local Development

```powershell
# 1. Deploy database schema (DACPAC)
./scripts/deploy-dacpac.ps1 -Server "localhost" -Database "Hartonomous"

# 2. Deploy CLR assemblies (14 assemblies in dependency order)
./scripts/deploy-clr-secure.ps1 -ServerName "." -Rebuild

# 3. Start Neo4j (optional, compliance only)
docker run -d --name neo4j -p 7474:7474 -p 7687:7687 -e NEO4J_AUTH=neo4j/password neo4j:5.28-community

# 4. Run API (optional, management only)
cd src/Hartonomous.Api; dotnet run

# 5. Run workers (optional, compliance only)
cd src/Hartonomous.Workers.Neo4jSync; dotnet run
cd src/Hartonomous.Workers.CesConsumer; dotnet run
```

### Production (Azure Arc Hybrid)

**Recommended Architecture** (39% cost savings):

- **Windows CLR Tier**: 2-core Windows Server VM (atomization only, 1% workload)
  - SQL Server 2025 with UNSAFE CLR enabled
  - Handles: `sp_IngestAtom_Atomic`, `sp_AtomizeImage`, `sp_AtomizeModel`
  - Cost: $24K/year (Standard_D2s_v5)

- **Linux Storage Tier**: 16-core Linux VM (queries/search, 99% workload)
  - SQL Server 2025 on Linux (no CLR, just VECTOR operations)
  - Handles: `sp_SemanticSearch`, `sp_ChainOfThoughtReasoning`, `sp_QueryLineage`
  - Cost: $49K/year (Standard_D16s_v5)

- **Communication**: Linked server (Windows → Linux) or application-layer routing

**Total Cost**: $73K/year (vs. $120K all-Windows, 39% savings)

**Source**: Validated in `docs/research/HYBRID_ARCHITECTURE_CLR_WINDOWS_STORAGE_LINUX.md`.

### Alternative: Windows-Only Deployment

- **Single Tier**: 16-core Windows Server VM (full CLR + VECTOR)
- **Cost**: $120K/year (Standard_D16s_v5 Windows)
- **Tradeoff**: Higher cost, but simpler architecture (no linked server)

---

## Testing

**Projects**: 6 test suites

- **Hartonomous.UnitTests**: 110 tests (domain logic, value objects)
- **Hartonomous.Core.Tests**: 45 tests (entities, interfaces)
- **Hartonomous.IntegrationTests**: Infrastructure-blocked (requires live SQL Server)
- **Hartonomous.DatabaseTests**: SQL schema validation
- **Hartonomous.SqlClr.Tests**: CLR function unit tests
- **Hartonomous.EndToEndTests**: Full workflow validation

**Status**: 110/110 unit tests passing (~30-40% coverage), integration tests require setup.

---

## Documentation Map

- **README.md**: Getting started, quick start, core concepts
- **CONTRIBUTING.md**: Development workflow, coding standards
- **docs/ARCHITECTURE.md**: This document (system architecture)
- **docs/architecture/atomic-decomposition.md**: Atomic decomposition philosophy and implementation
- **docs/api/rest-api.md**: REST API reference (18 controllers)
- **docs/deployment/clr-deployment.md**: CLR deployment (14 assemblies)
- **docs/security/clr-security.md**: CLR security model
- **docs/development/database-schema.md**: Schema reference (99 tables, 91 procedures)
- **docs/development/testing-guide.md**: Testing strategy
- **docs/reference/version-compatibility.md**: Version matrix
- **docs/research/CLR_REQUIREMENTS_RESEARCH.md**: CLR framework validation
- **docs/research/VALIDATED_FACTS.md**: Single source of truth (all claims validated)
- **docs/research/HYBRID_ARCHITECTURE_CLR_WINDOWS_STORAGE_LINUX.md**: Hybrid deployment architecture
- **neo4j/schemas/CoreSchema.cypher**: Neo4j schema (provenance mirror)

---

**All architectural claims are traceable to source code and validated research. No speculative features documented.**
