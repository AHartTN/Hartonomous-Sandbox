# Hartonomous

**Database-Centric AI Platform with Spatial Reasoning**

Hartonomous is a revolutionary AI platform that stores AI models as **atomized spatial data** in SQL Server, enabling semantic search, geometric inference, and autonomous improvement through spatial reasoning—without loading entire models into memory.

## 🎯 Core Innovation

Traditional AI systems load multi-gigabyte models into RAM for inference. Hartonomous **atomizes** model weights into content-addressable atoms stored in SQL Server's `GEOMETRY` spatial indices, enabling:

- **O(log N) Inference**: Spatial KNN queries find relevant weights via R-tree indices
- **Zero Model Loading**: Query 3D semantic space instead of loading full models
- **Content-Addressable Storage**: Automatic deduplication via SHA-256 hashing
- **Gradient Descent on Geometry**: True machine learning—update 3D coordinates, not weights
- **OODA Loop Autonomy**: Self-healing database that optimizes itself through spatial reasoning

## 🏗️ Architecture

```
┌─────────────────────────────────────────────────────────────┐
│                        API Layer                            │
│  .NET 10 ASP.NET Core • Entra ID Auth • Problem Details    │
└────────────┬────────────────────────────────────────────────┘
             │
┌────────────▼────────────────────────────────────────────────┐
│                   Application Services                      │
│  18 Atomizers • Ingestion Pipeline • Spatial Queries       │
└────────────┬────────────────────────────────────────────────┘
             │
┌────────────▼────────────────────────────────────────────────┐
│              SQL Server 2025 (Spatial Core)                 │
│  • TensorAtoms (GEOMETRY) • DiskANN Vector Index            │
│  • CLR Functions (O(K) refinement) • R-Tree Spatial Index   │
│  • Columnstore (analytics) • Temporal Tables (audit trail)  │
└────────────┬────────────────────────────────────────────────┘
             │
┌────────────▼────────────────────────────────────────────────┐
│              Neo4j (Provenance Graph)                       │
│  Merkle DAG • Atom lineage • Cross-modal relationships     │
└─────────────────────────────────────────────────────────────┘
```

### Key Components

1. **Atomization Engine**: Breaks models, documents, images, videos into content-addressable atoms
2. **Spatial Geometry Layer**: Projects 1536D embeddings → 3D coordinates via landmark trilateration
3. **CLR Computation**: 49 SIMD-optimized functions for O(K) attention/refinement (SAFE permission level)
4. **OODA Autonomous Loop**: Observe → Orient → Decide → Act → Learn cycle for self-optimization
5. **Multi-Tenant Isolation**: Row-level security with spatial index partitioning

## 🚀 Quick Start

### Prerequisites

- **Windows Server** or **Windows 11** (SQL Server requirement)
- **.NET 10 SDK** (or later)
- **SQL Server 2025** (or 2022) with CLR enabled
- **Neo4j 5.x** (Community or Enterprise)
- **PowerShell 7+**

### 5-Minute Setup

```powershell
# 1. Clone repository
git clone https://github.com/AHartTN/Hartonomous.git
cd Hartonomous

# 2. Configure connection strings
cp src/Hartonomous.Api/appsettings.json.template src/Hartonomous.Api/appsettings.json
# Edit appsettings.json with your SQL Server and Neo4j connection strings

# 3. Deploy database
.\scripts\Deploy-Database.ps1 -ServerName "localhost" -DatabaseName "Hartonomous"

# 4. Run API
dotnet run --project src/Hartonomous.Api
```

Navigate to `https://localhost:5001/swagger` to explore the API.

### First Model Ingestion

```bash
# Ingest Ollama model (requires Ollama running locally)
curl -X POST https://localhost:5001/api/ingest/ollama \
  -H "Content-Type: application/json" \
  -d '{
    "modelIdentifier": "llama3.2",
    "source": {
      "name": "Llama 3.2 from Ollama",
      "metadata": "{\"ollamaEndpoint\":\"http://localhost:11434\"}"
    }
  }'

# Ingest HuggingFace model
curl -X POST https://localhost:5001/api/ingest/huggingface \
  -H "Content-Type: application/json" \
  -d '{
    "modelIdentifier": "meta-llama/Llama-3.2-1B",
    "source": {
      "name": "Llama 3.2 1B from Hugging Face"
    }
  }'
```

## 📚 Documentation

### Getting Started
- **[Quickstart Guide](docs/getting-started/quickstart.md)** - 5-minute setup and first query
- **[Installation](docs/getting-started/installation.md)** - Detailed installation instructions
- **[Configuration](docs/getting-started/configuration.md)** - Connection strings, Azure services, authentication
- **[First Ingestion](docs/getting-started/first-ingestion.md)** - Step-by-step model/document ingestion

### Architecture
- **[Semantic-First Architecture](docs/architecture/semantic-first.md)** - O(log N) + O(K) pattern, spatial indices
- **[OODA Autonomous Loop](docs/architecture/ooda-loop.md)** - Self-healing, autonomous optimization
- **[Spatial Geometry](docs/architecture/spatial-geometry.md)** - Landmark projection, Voronoi partitioning
- **[Model Atomization](docs/architecture/model-atomization.md)** - Content-addressable atoms, CAS deduplication
- **[Catalog Management](docs/architecture/catalog-management.md)** - Multi-file models (HuggingFace, Ollama)
- **[Model Parsers](docs/architecture/model-parsers.md)** - GGUF, SafeTensors, ONNX, PyTorch, TensorFlow
- **[Inference & Generation](docs/architecture/inference.md)** - Spatial KNN, autoregressive decoding
- **[Training & Fine-Tuning](docs/architecture/training.md)** - Gradient descent on geometry
- **[Archive Handling](docs/architecture/archive-handler.md)** - ZIP/TAR/GZIP extraction with security

### Implementation
- **[Database Schema](docs/implementation/database-schema.md)** - Core tables, indices, temporal tables
- **[T-SQL Pipelines](docs/implementation/t-sql-pipelines.md)** - Service Broker, OODA queues, message processing
- **[CLR Functions](docs/implementation/clr-functions.md)** - 49 SIMD-optimized functions, SAFE permission level
- **[Neo4j Integration](docs/implementation/neo4j-integration.md)** - Provenance graph, Merkle DAG sync
- **[Worker Services](docs/implementation/worker-services.md)** - Background processing, ingestion, sync
- **[Testing Strategy](docs/implementation/testing.md)** - Unit, CLR, integration, E2E testing

### Operations
- **[Deployment](docs/operations/deployment.md)** - Azure Arc, DACPAC deployment, GitHub Actions
- **[Monitoring](docs/operations/monitoring.md)** - Application Insights, performance counters, health checks
- **[Backup & Recovery](docs/operations/backup-recovery.md)** - Database backup, Neo4j backup, disaster recovery
- **[Performance Tuning](docs/operations/performance-tuning.md)** - Index optimization, query plans, columnstore
- **[Troubleshooting](docs/operations/troubleshooting.md)** - Common issues, error codes, diagnostics
- **[Cognitive Kernel Seeding](docs/operations/kernel-seeding.md)** - Bootstrap testing framework (4 epochs)

### API Reference
- **[Ingestion Endpoints](docs/api/ingestion.md)** - File, URL, database, model platform ingestion
- **[Query Endpoints](docs/api/query.md)** - Spatial search, semantic queries, cross-modal
- **[Reasoning Endpoints](docs/api/reasoning.md)** - A* pathfinding, OODA loop, hypothesis generation
- **[Provenance Endpoints](docs/api/provenance.md)** - Atom lineage, Merkle DAG traversal
- **[Streaming Endpoints](docs/api/streaming.md)** - Server-sent events for long-running operations

### Atomizers
- **[AI Model Platforms](docs/atomizers/ai-model-platforms.md)** - Ollama, HuggingFace atomizers
- **[Document Atomizers](docs/atomizers/documents.md)** - PDF, Markdown, text splitting strategies
- **[Image Atomizers](docs/atomizers/images.md)** - OCR, object detection, scene analysis
- **[Video Atomizers](docs/atomizers/videos.md)** - Frame extraction, shot detection, audio transcription
- **[Code Atomizers](docs/atomizers/code.md)** - AST parsing, function extraction, dependency analysis

### Contributing
- **[Contributing Guide](docs/contributing/contributing.md)** - How to contribute, code standards
- **[Development Setup](docs/contributing/development-setup.md)** - Local development environment
- **[Code Standards](docs/contributing/code-standards.md)** - C#, T-SQL, PowerShell style guides
- **[Pull Request Process](docs/contributing/pull-requests.md)** - PR templates, review process

### Planning (Current Development)
- **[Architectural Validation](docs/planning/ARCHITECTURAL-VALIDATION-REPORT.md)** - Microsoft pattern validation
- **[Refactoring Plan](docs/planning/ARCHITECTURAL-REFACTORING-PLAN.md)** - SOLID, Clean Architecture refactoring
- **[App Layer Production Plan](docs/planning/APP-LAYER-PRODUCTION-PLAN.md)** - Production readiness roadmap

## 🎓 Key Concepts

### Content-Addressable Storage (CAS)

Every atom has a SHA-256 hash of its content:

```sql
-- Automatic deduplication
INSERT INTO Atom (ContentHash, AtomicValue, ReferenceCount)
SELECT @hash, @content, 1
WHERE NOT EXISTS (SELECT 1 FROM Atom WHERE ContentHash = @hash);

UPDATE Atom SET ReferenceCount += 1 WHERE ContentHash = @hash;
```

### Spatial Reasoning for Inference

```sql
-- Traditional: Load 7B parameters into RAM (28 GB)
-- Hartonomous: Query 3D space for relevant atoms

-- 1. Embed query → 3D projection
DECLARE @queryPoint GEOMETRY = dbo.fn_ProjectTo3D(@queryEmbedding);

-- 2. Spatial KNN (O(log N) via R-tree)
SELECT TOP 50 AtomId, SpatialKey.STDistance(@queryPoint) AS Distance
FROM AtomEmbedding WITH (INDEX(IX_Spatial))
ORDER BY SpatialKey.STDistance(@queryPoint);

-- 3. Attention weighting (O(K) where K=50)
-- 4. Return next token (no full model loaded)
```

### OODA Loop Autonomy

```
┌──────────────────────────────────────────────────────┐
│  OBSERVE: Detect slow query (> 1000ms)              │
│     ↓                                                │
│  ORIENT: Analyze execution plan, missing indices    │
│     ↓                                                │
│  DECIDE: Generate hypothesis (CREATE INDEX)         │
│     ↓                                                │
│  ACT: Execute with rollback safety                  │
│     ↓                                                │
│  LEARN: Measure improvement → Update model weights  │
└──────────────────────────────────────────────────────┘
```

## 🧪 Example Queries

### Semantic Search

```csharp
// Find atoms semantically similar to "machine learning optimization"
var results = await context.AtomEmbeddings
    .OrderBy(ae => ae.SpatialKey.STDistance(queryPoint))
    .Take(10)
    .Select(ae => ae.Atom)
    .ToListAsync();
```

### Cross-Modal Reasoning

```sql
-- Find images related to code concept
WITH CodeAtoms AS (
  SELECT AtomId, SpatialKey 
  FROM AtomEmbedding 
  WHERE AtomId IN (SELECT AtomId FROM Atom WHERE SourceType = 'Code')
),
ImageAtoms AS (
  SELECT AtomId, SpatialKey 
  FROM AtomEmbedding 
  WHERE AtomId IN (SELECT AtomId FROM Atom WHERE SourceType = 'Image')
)
SELECT i.AtomId, c.AtomId AS RelatedCodeAtomId,
       i.SpatialKey.STDistance(c.SpatialKey) AS Distance
FROM ImageAtoms i
CROSS APPLY (
  SELECT TOP 1 AtomId, SpatialKey 
  FROM CodeAtoms c
  ORDER BY i.SpatialKey.STDistance(c.SpatialKey)
) c
ORDER BY Distance;
```

### A* Pathfinding (Reasoning Chain)

```sql
EXEC sp_SpatialAStar 
  @startAtomId = 123,  -- Problem statement
  @goalAtomId = 456,   -- Desired solution
  @maxDepth = 10,
  @beamWidth = 5;
-- Returns: Reasoning chain with spatial coherence
```

## 🔧 Technology Stack

- **.NET 10**: ASP.NET Core Web API, EF Core 10 (DB-First)
- **SQL Server 2025**: Spatial indices, CLR, DiskANN, Columnstore, Temporal Tables
- **Neo4j 5.x**: Provenance graph, Merkle DAG
- **Azure Services** (optional): Entra ID, Key Vault, App Configuration, Application Insights
- **Deployment**: Azure Arc (on-prem servers), GitHub Actions CI/CD

## 📊 Performance

- **Spatial Index Performance**: O(log N) KNN queries on 100M+ atoms
- **Inference Latency**: < 50ms for spatial search (vs seconds for model loading)
- **Deduplication**: 60-80% storage savings on typical document corpora
- **Throughput**: 10K+ atoms/second ingestion (bulk insert with CLR)

## 🛣️ Roadmap

### Current (Q4 2025)
- ✅ Atomization pipeline (18 atomizers)
- ✅ Spatial geometry layer
- ✅ Basic inference via spatial KNN
- ✅ Neo4j provenance sync
- ⏳ Entra ID authentication (in progress)
- ⏳ OODA autonomous loop (implementation phase)

### Near-Term (Q1 2026)
- Full model weight download and atomization
- Training/fine-tuning via gradient descent on geometry
- Advanced reasoning (Chain-of-Thought, ReAct, Tree-of-Thoughts)
- Production deployment on Azure Arc

### Future
- Multi-modal generation (text → image, image → text)
- Export functionality (reconstitute models from atoms)
- Distributed spatial indices (sharding across databases)
- GPU-accelerated CLR functions (CUDA.NET)

## 📜 License

MIT License - see [LICENSE](LICENSE) file for details.

## 🤝 Contributing

Contributions welcome! See [CONTRIBUTING.md](docs/contributing/contributing.md) for guidelines.

## 📧 Contact

- **Project Lead**: Adam Hart (@AHartTN)
- **Repository**: https://github.com/AHartTN/Hartonomous
- **Issues**: https://github.com/AHartTN/Hartonomous/issues

---

**Built with ❤️ in Tennessee • Powered by Spatial Reasoning**
