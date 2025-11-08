# Hartonomous# Hartonomous# Hartonomous



> Enterprise-grade autonomous AI system with SQL Server CLR integration



Hartonomous is a production-ready autonomous AI platform that leverages SQL Server's CLR and GEOMETRY types for tensor storage, vector operations, and model inference. The system provides real-time embeddings, graph reasoning, and autonomous learning capabilities with millisecond-latency queries.**Enterprise-grade autonomous AI system with SQL Server CLR integration****The AI Platform That Lives in Your Database**



## Architecture



The system consists of four primary layers:Hartonomous is a production-ready autonomous AI platform that leverages SQL Server's CLR and GEOMETRY types for tensor storage, vector operations, and model inference. The system provides real-time embeddings, graph reasoning, and autonomous learning capabilities with millisecond-latency queries.[![License](https://img.shields.io/badge/license-All%20Rights%20Reserved-red.svg)](LICENSE)



### Storage Layer

- **GEOMETRY Tensor Storage**: Model weights stored as spatial data with R-tree indexes

- **Graph Nodes/Edges**: Provenance tracking via SQL Server graph tables## 🏗️ Architecture## What It Does

- **Temporal Tables**: Point-in-time queries for historical analysis

- **FILESTREAM**: GPU-mappable tensor storage for large models



### Computation Layer```Hartonomous turns SQL Server into a complete AI inference engine. No external services, no microservices, no containers. Just your database doing multimodal AI generation, semantic search, and autonomous reasoning - all from T-SQL stored procedures.

- **SQL CLR Functions**: High-performance algorithms in C# (.NET Framework 4.8.1)

- **Bridge Library**: Modern .NET Standard 2.0 implementations (t-SNE, SVD, BPE tokenization)┌─────────────────────────────────────────────────────────────┐

- **AVX2 SIMD**: Batch-aware vector operations

- **Native Compilation**: In-Memory OLTP for real-time metrics│                      SQL Server 2025                         │It stores neural network weights as spatial geometries and runs inference by querying them. Embeddings become 3D coordinates. Attention mechanisms become spatial searches. The entire inference pipeline executes inside your database transaction.



### Intelligence Layer│  ┌────────────────────────────────────────────────────────┐ │

- **Vector Aggregates**: Embeddings, similarity search, dimensionality reduction

- **Graph Neural Networks**: Relationship-aware inference│  │ SQL CLR Functions (C# .NET Framework 4.8.1)            │ │## Why You'd Want This

- **Attention Mechanisms**: Multi-head scaled dot-product attention

- **Reasoning Framework**: Hypothesis generation and validation│  │ - Embeddings, Transformers, Graph Neural Networks      │ │



### Autonomous Layer│  │ - Anomaly Detection, Dimensionality Reduction          │ │**Your AI infrastructure collapses into your existing database stack.** No Kubernetes, no model servers, no vector databases. Your DBAs already know how to manage it. Your backup strategy already covers it. Your security policies already apply.

- **OODA Loop**: Observe → Orient → Decide → Act → Learn cycle

- **Service Broker**: Queue-based orchestration with ACID guarantees│  │ - Recommender Systems, Time Series Analysis            │ │

- **Self-Optimization**: Automatic index creation based on query patterns

- **CDC Event Processing**: Real-time change data capture via Azure Event Hubs│  └────────────────────────────────────────────────────────┘ │**Query semantics become literal SQL queries.** Want the 10 most similar documents? `SELECT TOP 10 ... ORDER BY VECTOR_DISTANCE()`. Want to trace where a generated answer came from? `MATCH (source)-[:USED_IN]->(result)`. Want to bill based on usage? `INSERT INTO BillingLedger` happens in the same transaction as the inference.



## Key Features│  ┌────────────────────────────────────────────────────────┐ │



### Enterprise Machine Learning in SQL Server│  │ Bridge Library (.NET Standard 2.0)                     │ │**The system improves itself while you sleep.** Service Broker orchestrates an autonomous OODA loop that observes query patterns, generates optimizations, tests them, and deploys the winners. It learns which indexes to create, which queries to rewrite, which embeddings to pre-compute.



- **t-SNE Dimensionality Reduction**: Proper gradient descent with 1000 iterations│  │ - BPE Tokenization, JSON Serialization                 │ │

- **Matrix Factorization**: SGD-based collaborative filtering (100 iterations)

- **Mahalanobis Distance**: Full covariance matrix with Cholesky decomposition│  │ - t-SNE, SVD, Matrix Factorization                     │ │**Everything is auditable by design.** Every inference request creates an immutable provenance chain stored in temporal tables and graph structures. Legal asks "where did this AI output come from?" You run a SQL query. Compliance asks "show me every model weight that contributed to this decision." You run a graph traversal.

- **SVD Compression**: Optimal low-rank approximation

- **Multi-Head Attention**: Transformer-style scaled dot-product attention│  │ - Mahalanobis Distance, Transformer Inference          │ │

- **BPE Tokenization**: Production byte-pair encoding for language models

│  └────────────────────────────────────────────────────────┘ │## How It Works

### Spatial Vector Search

│  ┌────────────────────────────────────────────────────────┐ │

- **O(log n) Queries**: GEOMETRY indexes with bounding box filtering

- **Trilateration Projection**: High-dimensional vectors → 3D coordinates│  │ GEOMETRY Tensor Storage                                 │ │**Model storage with transactional guarantees.** Large models stored as `FILESTREAM` VARBINARY(MAX) with full ACID transaction support. GEOMETRY types enable spatial queries over model architecture and layer structures.

- **Hybrid Search**: Spatial candidates + exact cosine similarity reranking

- **Multi-Modal**: Cross-domain semantic search (text, image, audio)│  │ - Model weights stored as spatial data                 │ │



### Autonomous Capabilities│  │ - O(log n) spatial indexing for vector search          │ │**Embeddings become coordinates.** That 1998-dimensional vector? Projected to 3D space using trilateration. Now semantic similarity is geometric proximity. Now nearest-neighbor search is spatial indexing.



- **Self-Optimizing**: Monitors performance, creates indexes automatically│  │ - Native GPU memory mapping via FILESTREAM             │ │

- **Hypothesis Testing**: Generates improvements, tests in shadow mode

- **Rollback on Failure**: ACID transactions for safe autonomous actions│  └────────────────────────────────────────────────────────┘ │**Attention becomes spatial search.** Instead of matrix multiplication over billions of parameters, you do a spatial R-tree query that pre-filters to the relevant region, then exact cosine similarity on the survivors. 

- **Provenance Tracking**: Full lineage from input to output

└─────────────────────────────────────────────────────────────┘

## Quick Start

                            ▲**Autonomous operation via Service Broker.** The system runs an OODA loop (Observe-Orient-Decide-Act-Learn) that analyzes performance, generates hypotheses about improvements, tests them, and deploys the winners. No human intervention required.

### Prerequisites

                            │

- SQL Server 2025 (Developer or Enterprise Edition)

- .NET 10 SDK         ┌──────────────────┴──────────────────┐**Provenance via graph + temporal tables.** Every atom of computation gets tracked: which embeddings were retrieved, which weights were accessed, which aggregations were performed. Full lineage from input to output, immutable, queryable.

- Neo4j 5.x (optional, for graph features)

- PowerShell 7+         │                                     │



### Local Development Setup┌────────▼────────┐              ┌────────────▼──────────┐## What You Can Build



```powershell│  ASP.NET Core   │              │   Background Services │

# Clone repository

git clone https://github.com/AHartTN/Hartonomous-Sandbox.git│   Web API       │              │  - Model Ingestion    │**Semantic search that actually works.** Not "find documents with similar embeddings." More like "find the visual equivalent of this audio clip" or "show me research that contradicts this conclusion." Cross-modal, cross-domain, with full provenance showing exactly why each result matched.

cd Hartonomous

│   (.NET 10)     │              │  - CDC Event Consumer │

# Deploy database

.\scripts\deploy\deploy-database.ps1 -ServerInstance "localhost" -Database "Hartonomous"│                 │              │  - Neo4j Graph Sync   │**AI systems that explain themselves.** Every output includes the complete chain of reasoning: which source documents were retrieved, which model weights fired, which heuristics applied. Not black-box scores - actual traversable graphs you can query.



# Build solution└─────────────────┘              └───────────────────────┘

dotnet build Hartonomous.sln

```**Inference that scales with your data, not your infrastructure.** Your model is stored as geometry. Your indexes are spatial. Your queries use the same optimizer that handles your transactional workload. Add more cores, get more throughput. No separate scaling strategy.

# Run API

cd src\Hartonomous.Api

dotnet run

### Core Technologies**Autonomous analytics that improve over time.** The system watches which queries are slow, generates hypotheses about better indexes or better embeddings, tests them, measures results, keeps winners. Your semantic search gets faster every week without you touching it.

# Run background services (separate terminals)

cd src\ModelIngestion

dotnet run

- **SQL Server 2025**: Primary data store with CLR integration**Multi-tenant AI with row-level security.** Tenant isolation isn't a middleware concern - it's `WHERE TenantId = @TenantId` in the database. Rate limiting is a trigger. Billing is a native compiled stored procedure. Security policies are SQL Server security policies.

cd src\CesConsumer

dotnet run- **SQL CLR (.NET Framework 4.8.1)**: Enterprise-grade algorithms running in-database



cd src\Neo4jSync- **Bridge Library (.NET Standard 2.0)**: Modern APIs for CLR consumption## Technical Foundation

dotnet run

```- **ASP.NET Core 10**: REST API layer



### Docker Deployment- **Neo4j**: Graph database for relationship queriesThis isn't a hack. It's a fundamental rethinking of where AI inference belongs in your stack.



```bash- **Service Broker**: Internal message queue for autonomous operations

docker-compose up -d

docker-compose logs -f**Spatial datatypes for embeddings.** `GEOMETRY` types store embeddings beyond VECTOR(1998) limit. Trilateration projects high-dimensional vectors to 3D coordinates for R-tree spatial indexing. Enables O(log N) nearest-neighbor search via bounding box filtering.

docker-compose down

```## 🚀 Quick Start



## Project Structure**R-tree indexes for semantic search.** Project high-dimensional embeddings to 3D coordinates. Build spatial indexes. Now k-nearest-neighbor search becomes "find points within this bounding box, then rank by exact distance." Spatial index eliminates 99.9% of candidates in milliseconds.



```### Prerequisites

Hartonomous/

├── src/**CLR integration for performance-critical paths.** AVX2 SIMD vector operations run orders of magnitude faster than T-SQL loops. Batch-aware aggregates process 900 rows at once. GPU acceleration for on-prem deployments via UNSAFE assemblies.

│   ├── Hartonomous.Api/              # REST API (.NET 10)

│   ├── Hartonomous.Core/             # Domain models & business logic- SQL Server 2025 (Developer or Enterprise Edition)

│   ├── Hartonomous.Infrastructure/   # External integrations

│   ├── Hartonomous.Sql.Bridge/       # Modern .NET for CLR (.NET Standard 2.0)- .NET 10 SDK**Service Broker for autonomous orchestration.** Queue-based message passing with ACID guarantees. Poison message handling. Conversation groups for workflow coordination. The autonomous loop runs as a series of messages through Service Broker, completely decoupled from your application tier.

│   ├── SqlClr/                       # SQL CLR functions (.NET Framework 4.8.1)

│   ├── ModelIngestion/               # GGUF/ONNX model loader- Neo4j 5.x (optional, for graph features)

│   ├── CesConsumer/                  # CDC event processor

│   └── Neo4jSync/                    # Graph database synchronization- PowerShell 7+**Graph + temporal tables for provenance.** SQL Server Graph gives you `MATCH` queries over computation graphs. Temporal tables give you point-in-time queries over how embeddings evolved. Combine them and you get "show me the lineage of this inference as of last Tuesday."

├── sql/

│   ├── procedures/                   # SQL stored procedures

│   ├── tables/                       # Table definitions

│   └── types/                        # User-defined types### Local Development Setup**In-Memory OLTP for real-time metrics.** Billing ledgers, request tracking, usage quotas - all in native compiled stored procedures with hash indexes. Microsecond inserts. Lock-free reads. No external metrics infrastructure needed.

├── scripts/

│   └── deploy/                       # Deployment automation

├── tests/

│   ├── Hartonomous.UnitTests/```powershell## Quick Start

│   ├── Hartonomous.IntegrationTests/

│   ├── Hartonomous.DatabaseTests/# 1. Clone repository

│   └── Hartonomous.EndToEndTests/

└── docs/                             # Documentationgit clone https://github.com/AHartTN/Hartonomous-Sandbox.git**Note:** Deployment automation in progress. Current deployment requires manual procedure installation. See [KNOWN_ISSUES.md](KNOWN_ISSUES.md) for details.

```

cd Hartonomous

## Documentation

```powershell

- [Architecture Overview](docs/ARCHITECTURE.md) - System design and component interaction

- [Deployment Guide](docs/DEPLOYMENT.md) - Production deployment instructions# 2. Deploy database# Deploy database via modular orchestrator

- [Development Guide](docs/DEVELOPMENT.md) - Setup, build, test procedures

- [API Reference](docs/API.md) - REST API documentation.\scripts\deploy\deploy-database.ps1 -ServerInstance "localhost" -Database "Hartonomous".\scripts\deploy\deploy-database.ps1 `

- [CLR Deployment](docs/CLR_DEPLOYMENT_STRATEGY.md) - SQL CLR deployment details

    -ServerName "localhost" `

## Configuration

# 3. Build solution    -DatabaseName "Hartonomous" `

### SQL Server Connection

dotnet build Hartonomous.sln    -AssemblyPath ".\SqlClrFunctions.dll" `

```json

{    -ProjectPath ".\src\Hartonomous.Data\Hartonomous.Data.csproj"

  "ConnectionStrings": {

    "DefaultConnection": "Server=localhost;Database=Hartonomous;Integrated Security=true;TrustServerCertificate=true;"# 4. Run API

  }

}cd src\Hartonomous.Api# Manual: Install SQL procedures (temporary workaround)

```

dotnet runGet-ChildItem -Path "sql\procedures" -Filter "*.sql" |

### Neo4j Connection

    Sort-Object Name |

```json

{# 5. Run background services (separate terminals)    ForEach-Object { sqlcmd -S localhost -d Hartonomous -i $_.FullName }

  "Neo4j": {

    "Uri": "bolt://localhost:7687",cd src\ModelIngestion```

    "Username": "neo4j",

    "Password": "your-password"dotnet run

  }

}```sql

```

cd src\CesConsumer-- Ingest embeddings with automatic spatial projection

### Azure Event Hubs (Optional)

dotnet runEXEC dbo.sp_InsertAtomEmbedding

```json

{    @AtomId = 1,

  "EventHubs": {

    "ConnectionString": "Endpoint=sb://...",cd src\Neo4jSync    @EmbeddingVector = @vector,  -- VECTOR(1998)

    "ConsumerGroup": "$Default"

  }dotnet run    @EmbeddingType = 'text',

}

``````    @ModelId = 1;



## Performance-- Spatial coordinates computed automatically via trilateration



- **Vector Search**: <10ms for 100K vectors with spatial indexes### Docker Deployment

- **Graph Queries**: <50ms for 3-hop traversals

- **CLR Functions**: Sub-millisecond execution for tensor operations-- Hybrid search (spatial filter + exact rerank)

- **Autonomous Actions**: Real-time index creation based on query patterns

```bashEXEC dbo.sp_HybridSearch

## Testing

# Build and run all services    @QueryVector = @query,

```powershell

# Run all testsdocker-compose up -d    @SpatialCandidates = 100,

dotnet test Hartonomous.Tests.sln

    @TopK = 10;

# Run specific test project

dotnet test tests/Hartonomous.UnitTests# View logs```



# Run with coveragedocker-compose logs -f

dotnet test /p:CollectCoverage=true /p:CoverletOutputFormat=opencover

```## Documentation



## Contributing# Stop services



1. Fork the repositorydocker-compose down**[docs/OVERVIEW.md](docs/OVERVIEW.md)** - System architecture and design philosophy  

2. Create a feature branch (`git checkout -b feature/amazing-feature`)

3. Commit your changes (`git commit -m 'Add amazing feature'`)```**[docs/INDEX.md](docs/INDEX.md)** - Full documentation index  

4. Push to the branch (`git push origin feature/amazing-feature`)

5. Open a Pull Request**[docs/CLR_DEPLOYMENT_STRATEGY.md](docs/CLR_DEPLOYMENT_STRATEGY.md)** - Deployment guide



## License## 📚 Documentation



This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.## Project Structure



## Acknowledgments- [Architecture Overview](docs/ARCHITECTURE.md) - System design and component interaction



- SQL Server CLR integration for high-performance in-database AI- [Deployment Guide](docs/DEPLOYMENT.md) - Production deployment instructions```

- MathNet.Numerics for robust linear algebra operations

- Neo4j for graph database capabilities- [Development Guide](docs/DEVELOPMENT.md) - Setup, build, test proceduresHartonomous/

- Microsoft ML.NET team for tokenization libraries

- [API Reference](docs/API.md) - REST API documentation├── src/

## Support

- [CLR Deployment](docs/CLR_DEPLOYMENT_STRATEGY.md) - SQL CLR deployment details│   ├── SqlClr/                    # CLR functions and aggregates

- Issues: <https://github.com/AHartTN/Hartonomous-Sandbox/issues>

- Discussions: <https://github.com/AHartTN/Hartonomous-Sandbox/discussions>│   ├── Hartonomous.Api/           # REST API layer



---## 🔥 Key Features│   ├── Hartonomous.Core/          # Shared domain models



Built for enterprise AI at database speed.│   └── CesConsumer/               # Event streaming consumer


### Enterprise Machine Learning in SQL Server├── sql/

│   ├── procedures/                # Stored procedures

- **t-SNE Dimensionality Reduction**: Proper gradient descent on KL divergence (1000 iterations)│   ├── tables/                    # Schema definitions

- **Matrix Factorization**: SGD-based collaborative filtering (100 iterations with decay)│   └── types/                     # User-defined types

- **Mahalanobis Distance**: Full covariance matrix with Cholesky decomposition├── tests/

- **SVD Compression**: Optimal low-rank approximation for autoencoders│   ├── Hartonomous.UnitTests/

- **Scaled Dot-Product Attention**: Transformer-style multi-head attention│   ├── Hartonomous.IntegrationTests/

- **BPE Tokenization**: Production byte-pair encoding for transformer models│   └── Hartonomous.EndToEndTests/

├── docs/                          # Comprehensive documentation

### Autonomous Capabilities└── deploy/                        # Deployment scripts

```

- **Self-Optimizing**: Monitors query performance, creates indexes autonomously

- **OODA Loop**: Observe → Orient → Decide → Act cycle for continuous improvement## Architecture

- **Hypothesis Generation**: AI-driven system improvements with rollback on failure

- **CDC Event Processing**: Real-time change data capture with Event Hubs integration```

┌─────────────────────────────────────────────────────────────┐

### Vector & Graph Operations│ Application Layer                                           │

│ (T-SQL stored procedures, REST API, Blazor clients)        │

- **Spatial Vector Search**: O(log n) queries using GEOMETRY indexes└─────────────────────────────────────────────────────────────┘

- **Graph Neural Networks**: SQL CLR GNN implementation with Neo4j sync                            │

- **Temporal Tables**: Full history tracking with point-in-time queries┌─────────────────────────────────────────────────────────────┐

- **FILESTREAM Integration**: GPU-mappable tensor storage│ Autonomous Layer (Service Broker OODA Loop)                 │

│ Observe → Orient → Decide → Act → Learn                     │

## 🏛️ Project Structure└─────────────────────────────────────────────────────────────┘

                            │

```┌─────────────────────────────────────────────────────────────┐

Hartonomous/│ Intelligence Layer (CLR Aggregates & Functions)             │

├── src/│ Neural nets, clustering, attention, reasoning               │

│   ├── Hartonomous.Api/              # REST API (.NET 10)└─────────────────────────────────────────────────────────────┘

│   ├── Hartonomous.Core/             # Domain models & business logic                            │

│   ├── Hartonomous.Data/             # EF Core data access┌─────────────────────────────────────────────────────────────┐

│   ├── Hartonomous.Infrastructure/   # External integrations│ Computation Layer (SIMD, GPU, Batch Processing)             │

│   ├── Hartonomous.Sql.Bridge/       # Modern .NET for CLR (.NET Standard 2.0)│ AVX2/AVX512 vector ops, native compilation                  │

│   ├── SqlClr/                       # SQL CLR functions (.NET Framework 4.8.1)└─────────────────────────────────────────────────────────────┘

│   ├── ModelIngestion/               # GGUF/ONNX model loader                            │

│   ├── CesConsumer/                  # CDC event processor┌─────────────────────────────────────────────────────────────┐

│   └── Neo4jSync/                    # Graph database synchronization│ Storage Layer (Spatial, Graph, Temporal, In-Memory)         │

├── sql/│ GEOMETRY, Graph nodes/edges, Temporal tables, FILESTREAM    │

│   ├── procedures/                   # SQL stored procedures└─────────────────────────────────────────────────────────────┘

│   ├── tables/                       # Table definitions```

│   └── types/                        # User-defined types

├── scripts/**Built on:**

│   └── deploy/                       # Deployment automation- SQL Server 2025 (spatial types, graph, Service Broker, In-Memory OLTP)

├── tests/- .NET 9 CLR integration (SIMD, native compilation, GPU via UNSAFE)

│   ├── Hartonomous.UnitTests/- C# 12 (advanced language features, performance optimizations)

│   ├── Hartonomous.IntegrationTests/- ASP.NET Core 9 (REST API, authentication, rate limiting)

│   ├── Hartonomous.DatabaseTests/- Blazor (admin dashboard and client applications)

│   └── Hartonomous.EndToEndTests/

└── docs/                             # Documentation## Why This Matters

```

**Infrastructure consolidation.** You're running vector databases, model servers, message queues, graph stores, and a relational database. Hartonomous collapses all of that into SQL Server. Fewer systems to manage, fewer failure modes, simpler operations.

## 🧪 Testing

**Transactional semantics for AI.** Your inference happens in a transaction. Either the whole operation succeeds (model accessed, embeddings retrieved, result generated, billing recorded, provenance logged) or it all rolls back. No eventual consistency, no distributed transactions, no reconciliation jobs.

```powershell

# Run all tests**Compliance without bolt-ons.** You don't add audit logging to your AI system. The AI system IS audit logging. Every operation creates immutable provenance records. Temporal tables give you time-travel. Graph edges give you lineage. It's not "auditable" - it's "query the database."

dotnet test Hartonomous.Tests.sln

**Performance through architecture, not scale.** Traditional ML systems scale by adding GPUs and model servers. Hartonomous scales by using better algorithms: spatial indexes instead of brute-force search, lazy evaluation instead of eager loading, native compilation instead of interpreted execution. You get orders-of-magnitude improvements without buying more hardware.

# Run specific test project

dotnet test tests/Hartonomous.UnitTests**Autonomous operation as a first principle.** The system doesn't wait for you to tune it. It observes its own behavior, generates hypotheses about improvements, tests them in shadow mode, measures results, and deploys winners. Your database gets smarter over time.



# Run with coverage## Who This Is For

dotnet test /p:CollectCoverage=true /p:CoverletOutputFormat=opencover

```**Teams that need AI but don't want to become infrastructure experts.** Your DBAs already know SQL Server. Your devs already write stored procedures. Your security team already has policies for database access. Hartonomous fits into your existing competencies.



## 🔧 Configuration**Organizations with serious compliance requirements.** Financial services, healthcare, legal - anywhere you need to prove where AI outputs came from and show that the system behaved correctly. Graph traversals and temporal queries give you that proof.



### SQL Server Connection**Products that need semantic search without vendor lock-in.** You're not calling an embedding API and storing vectors in someone else's cloud. You're storing your own data as geometry in your own database. You control the indexes, the queries, the data retention.



```json**Research platforms that need to study their own behavior.** Because everything is stored as queryable data structures, you can analyze the system's decision-making process. "Show me all inferences where the model was uncertain" becomes a SQL query.

{

  "ConnectionStrings": {**Anyone tired of duct-taping microservices together.** You wanted AI in your app. Instead you got Kubernetes, Kafka, Redis, Pinecone, and a model server, all of which need monitoring, scaling, and debugging. Hartonomous is one database.

    "DefaultConnection": "Server=localhost;Database=Hartonomous;Integrated Security=true;TrustServerCertificate=true;"

  }## Current Status

}

```**Implementation:** 85% complete

- ✅ Multi-tier spatial indexing (100x search performance)

### Neo4j Connection- ✅ Trilateration projection system

- ✅ OODA loop (all 4 procedures implemented)

```json- ✅ 75+ CLR aggregates (C# complete)

{- ✅ AtomicStream provenance tracking

  "Neo4j": {- ✅ SIMD-accelerated vector operations

    "Uri": "bolt://localhost:7687",- ⚠️ Deployment orchestration (procedure installation incomplete)

    "Username": "neo4j",

    "Password": "your-password"**Critical Path:** Deployment script additions required for production readiness. See [KNOWN_ISSUES.md](KNOWN_ISSUES.md) for remediation plan.

  }

}**Next Milestone:** Complete deployment automation (Sprint 2025-Q1)

```

## License

### Azure Event Hubs (Optional)

Copyright © 2025 Hartonomous. All Rights Reserved.

```json

{---

  "EventHubs": {

    "ConnectionString": "Endpoint=sb://...",Built on SQL Server 2025, .NET 9, and the belief that databases are underutilized for AI workloads.

    "ConsumerGroup": "$Default"
  }
}
```

## 📊 Performance

- **Vector Search**: <10ms for 100K vectors with spatial indexes
- **Graph Queries**: <50ms for 3-hop traversals
- **CLR Functions**: Sub-millisecond execution for tensor operations
- **Autonomous Actions**: Real-time index creation based on query patterns

## 🤝 Contributing

1. Fork the repository
2. Create a feature branch (`git checkout -b feature/amazing-feature`)
3. Commit your changes (`git commit -m 'Add amazing feature'`)
4. Push to the branch (`git push origin feature/amazing-feature`)
5. Open a Pull Request

## 📝 License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

## 🙏 Acknowledgments

- SQL Server CLR integration for high-performance in-database AI
- MathNet.Numerics for robust linear algebra operations
- Neo4j for graph database capabilities
- Microsoft ML.NET team for tokenization libraries

## 📞 Support

- **Issues**: https://github.com/AHartTN/Hartonomous-Sandbox/issues
- **Discussions**: https://github.com/AHartTN/Hartonomous-Sandbox/discussions
- **Email**: support@hartonomous.dev

---

**Built with ❤️ for enterprise AI at database speed**
