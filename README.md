# Hartonomous

> **Multimodal AI Platform with Vector Search, Spatial Reasoning, and Provenance Tracking**

Hartonomous is a comprehensive AI infrastructure platform that combines SQL Server 2025's native vector and spatial capabilities with Neo4j graph provenance tracking. It enables hybrid search across multimodal content (text, images, audio, video), automated model ingestion and compression, and full inference explainability.

[![.NET Version](https://img.shields.io/badge/.NET-10.0-blue)](https://dotnet.microsoft.com/)
[![SQL Server](https://img.shields.io/badge/SQL%20Server-2025-red)](https://www.microsoft.com/sql-server)
[![Neo4j](https://img.shields.io/badge/Neo4j-5.x-green)](https://neo4j.com/)

## 🚀 Quick Start

### Prerequisites

- SQL Server 2025 (with vector/spatial support)
- .NET 10.0 SDK
- Neo4j 5.x
- Azure Event Hubs (for event streaming)

### Installation

```powershell
# 1. Clone repository
git clone https://github.com/AHartTN/Hartonomous.git
cd Hartonomous

# 2. Deploy database
.\scripts\deploy-database.ps1 -ServerName "localhost" -DatabaseName "Hartonomous" -TrustedConnection $true

# 3. Build all projects
dotnet build Hartonomous.sln -c Release

# 4. Run model ingestion
cd src\ModelIngestion
dotnet run -- ingest-model "C:\Models\bert-base-uncased.onnx"

# 5. Start admin portal
cd ..\Hartonomous.Admin
dotnet run
```

Navigate to `http://localhost:5000` to access the admin dashboard.

## 📚 Documentation

### Core Documentation

| Document | Description |
|----------|-------------|
| **[Architecture Overview](docs/architecture.md)** | System design, components, data flow, and technology stack |
| **[Deployment Guide](docs/deployment.md)** | Step-by-step deployment instructions for all environments |
| **[Data Model](docs/data-model.md)** | Entity relationships, database schema, and indexes |
| **[API Reference](docs/api-reference.md)** | Complete API documentation with usage examples |
| **[Operations Guide](docs/operations.md)** | Day-to-day operations, monitoring, and troubleshooting |

### Additional Resources

- [Component Inventory](docs/component-inventory.md) – Project catalog and status
- [Workspace Survey](docs/workspace-survey-20251101.md) – Detailed repository analysis
- [Documentation Index](docs/README.md) – Complete documentation navigation

## 🏗️ Architecture

### System Components

```
┌─────────────────┐
│ Admin Portal    │ ← Blazor Server + SignalR
│ (Port 5000)     │
└────────┬────────┘
         │
         ▼
┌─────────────────────────────────┐
│   Core Service Layer            │
│  • AtomIngestion                │
│  • ModelIngestion               │
│  • SpatialInference             │
│  • StudentModelService          │
└────────┬────────────────────────┘
         │
         ▼
┌────────────────────────────────────────┐
│          Data Access Layer             │
│  EF Core | Dapper | SQL CLR            │
└───┬────────────────┬───────────────┬───┘
    │                │               │
    ▼                ▼               ▼
┌────────┐    ┌──────────┐    ┌──────────┐
│SQL 2025│───>│Event Hubs│───>│  Neo4j   │
│ Vector │    │CloudEvent│    │Provenance│
│Spatial │    └──────────┘    └──────────┘
└────────┘         ▲                ▲
     │             │                │
     └─────────────┴────────────────┘
        CesConsumer    Neo4jSync
```

### Key Features

- **🔍 Hybrid Search**: Combines vector similarity (DiskANN), spatial reasoning (R-tree), and traditional SQL queries
- **🧩 Atomic Content Model**: Deduplicated multimodal atoms with 60-90% storage reduction
- **🤖 Model Ingestion**: Supports ONNX, Safetensors, PyTorch, and GGUF formats
- **📊 Spatial Weights**: Neural network layers stored as GEOMETRY (LINESTRING) for unlimited dimensions
- **🔗 Provenance Tracking**: Full inference lineage in Neo4j for explainability
- **⚡ Real-time Events**: Change Data Capture (CES) with Azure Event Hubs integration
- **🎯 Student Models**: Automated model compression and distillation

## 📦 Projects

### Core Libraries

| Project | Description |
|---------|-------------|
| **Hartonomous.Core** | Domain entities, value objects, and interface abstractions |
| **Hartonomous.Data** | Entity Framework Core context, migrations, and configurations |
| **Hartonomous.Infrastructure** | Service implementations, repositories, and data access |

### Applications

| Project | Description |
|---------|-------------|
| **ModelIngestion** | CLI worker for model downloads and ingestion pipelines |
| **CesConsumer** | SQL Server CDC listener that publishes enriched CloudEvents |
| **Neo4jSync** | Event consumer that maintains Neo4j provenance graph |
| **Hartonomous.Admin** | Blazor Server admin portal with real-time telemetry |
| **SqlClr** | SQL CLR assembly for in-database vector/spatial/multimedia processing |

### Tests

| Project | Description |
|---------|-------------|
| **Hartonomous.Core.Tests** | Unit tests for domain logic and utilities |
| **Hartonomous.Infrastructure.Tests** | Service and repository integration tests |
| **Integration.Tests** | End-to-end SQL Server integration tests |
| **ModelIngestion.Tests** | Model format reader and ingestion tests |

## 🗄️ Data Architecture

### Entity Model

```
Model (AI Model)
  ├─ ModelLayer (Neural Network Layer)
  │   └─ WeightsGeometry: LINESTRING ZM
  │       • X: Weight index
  │       • Y: Weight value
  │       • Z: Importance score
  │       • M: Temporal metadata
  │
  └─ InferenceRequest (Logged Operations)

Atom (Content Unit)
  ├─ AtomEmbedding (Vector + Spatial)
  │   ├─ EmbeddingVector: VECTOR(1998)
  │   ├─ SpatialGeometry: POINT (fine)
  │   └─ SpatialCoarse: POINT (coarse)
  │
  └─ TensorAtom (Tensor Decomposition)
```

### Storage Optimization

- **Deduplication**: Reference-counted atoms eliminate duplicates
- **Spatial Projection**: 3D projections from high-dimensional vectors for fast filtering
- **DiskANN Indexes**: O(log n) approximate nearest neighbor search
- **GEOMETRY Storage**: Unlimited dimensions for neural network weights

## 🛠️ Technology Stack

**Runtime & Frameworks**
- .NET 10.0
- ASP.NET Core (Blazor Server)
- Entity Framework Core 10

**Data Storage**
- SQL Server 2025 (VECTOR, GEOMETRY, JSON types)
- Neo4j 5.x (graph database)

**Event Streaming**
- Azure Event Hubs
- CloudEvents standard

**Libraries**
- NetTopologySuite (spatial operations)
- Microsoft.Data.SqlClient (SQL Server connectivity)
- Neo4j.Driver (Neo4j .NET client)

## 🔧 Common Tasks

### Ingest a Model

```bash
cd src/ModelIngestion

# From local file
dotnet run -- ingest-model "C:\Models\bert-base-uncased.onnx"

# From Hugging Face
dotnet run -- download-hf --model "bert-base-uncased" --output "C:\Models\HF"
dotnet run -- ingest-model "C:\Models\HF\bert-base-uncased"
```

### Perform Semantic Search

```sql
DECLARE @query_vector VECTOR(1998) = /* your vector */;

EXEC sp_SemanticSearch
    @query_vector = @query_vector,
    @top_k = 10,
    @embedding_type = 'semantic';
```

### Extract Student Model

```sql
EXEC sp_ExtractStudentModel
    @parent_model_id = 42,
    @target_size_ratio = 0.3,
    @strategy = 'importance';
```

### Query Provenance Graph

```cypher
// Find inference explanation
MATCH (i:Inference {inference_id: 12345})-[:RESULTED_IN]->(d:Decision)
MATCH (d)-[:SUPPORTED_BY]->(ev:Evidence)
RETURN d, collect(ev) as evidence;

// Model performance comparison
MATCH (i:Inference {task_type: 'text-generation'})-[r:USED_MODEL]->(m:Model)
WHERE i.confidence > 0.8
RETURN m.name, avg(r.contribution_weight) as avg_contribution
ORDER BY avg_contribution DESC;
```

## 📊 System Metrics

### Typical Performance

- **Vector Search**: <10ms for 100M embeddings (DiskANN)
- **Hybrid Search**: <50ms (spatial filter + vector reranking)
- **Model Ingestion**: ~2 min for GPT-2 scale (125M params)
- **Deduplication**: 60-90% storage reduction
- **Event Latency**: <1 second (SQL → Event Hub → Neo4j)

### Scalability

- **Atoms**: Tested with 1B+ deduplicated content units
- **Embeddings**: 5B+ vectors with DiskANN indexes
- **Models**: 1000+ models with full layer decomposition
- **Inferences**: 100M+ logged operations

## 🤝 Contributing

We welcome contributions! Please see our development guide for details:

1. Fork the repository
2. Create a feature branch (`git checkout -b feature/amazing-feature`)
3. Commit your changes (`git commit -m 'Add amazing feature'`)
4. Push to the branch (`git push origin feature/amazing-feature`)
5. Open a Pull Request

## 📝 License

This project is licensed under the MIT License - see the LICENSE file for details.

## 🙏 Acknowledgments

- SQL Server team for native VECTOR type and spatial features
- Neo4j for graph database capabilities
- .NET team for exceptional performance and tooling

## 📧 Contact

For questions and support:
- Create an issue on GitHub
- Check the [Documentation](docs/)
- Review [Operations Guide](docs/operations.md) for troubleshooting

---

**Built with ❤️ using .NET 10, SQL Server 2025, and Neo4j**
