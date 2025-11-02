# Hartonomous Architecture Documentation

## Table of Contents
- [Overview](#overview)
- [System Architecture](#system-architecture)
- [Technology Stack](#technology-stack)
- [Core Components](#core-components)
- [Data Architecture](#data-architecture)
- [Event-Driven Architecture](#event-driven-architecture)
- [Design Patterns](#design-patterns)
- [Scalability & Performance](#scalability--performance)

## Overview

Hartonomous is a sophisticated multimodal AI system that combines vector embeddings, spatial reasoning, and graph-based provenance tracking to enable advanced semantic search, inference, and model management capabilities. The system leverages SQL Server 2025's native vector and spatial features alongside Neo4j for explainability and provenance tracking.

### Key Capabilities

- **Multimodal Content Processing**: Unified handling of text, images, audio, and video through atomic decomposition
- **Hybrid Search**: Combines vector similarity, spatial reasoning, and traditional SQL queries
- **Model Ingestion Pipeline**: Supports ONNX, Safetensors, PyTorch, and GGUF model formats
- **Inference Explainability**: Full traceability through Neo4j provenance graphs
- **Real-time Event Streaming**: Change Data Capture (CES) integration with Azure Event Hubs
- **Student Model Extraction**: Automated model distillation and compression

## System Architecture

### High-Level Architecture

```
┌─────────────────────────────────────────────────────────────────┐
│                     Hartonomous.Admin (Blazor)                  │
│                  ┌──────────────────────────────┐               │
│                  │   SignalR Telemetry Hub      │               │
│                  └──────────────────────────────┘               │
└─────────────────────────────────────────────────────────────────┘
                                 │
                                 ▼
┌─────────────────────────────────────────────────────────────────┐
│                    Core Service Layer                           │
│  ┌──────────────┐  ┌──────────────┐  ┌──────────────┐         │
│  │   Atom       │  │   Model      │  │   Spatial    │         │
│  │  Ingestion   │  │  Ingestion   │  │  Inference   │         │
│  └──────────────┘  └──────────────┘  └──────────────┘         │
└─────────────────────────────────────────────────────────────────┘
                                 │
                                 ▼
┌─────────────────────────────────────────────────────────────────┐
│                    Data Access Layer                            │
│  ┌──────────────┐  ┌──────────────┐  ┌──────────────┐         │
│  │ EF Core      │  │   Dapper     │  │    SQL CLR   │         │
│  │ Repositories │  │   Queries    │  │   Functions  │         │
│  └──────────────┘  └──────────────┘  └──────────────┘         │
└─────────────────────────────────────────────────────────────────┘
                                 │
        ┌────────────────────────┼────────────────────────┐
        ▼                        ▼                        ▼
┌───────────────┐    ┌───────────────────┐    ┌──────────────────┐
│  SQL Server   │    │  Azure Event Hubs │    │     Neo4j        │
│  2025         │    │  CloudEvents      │    │  Provenance      │
│  - Vectors    │    └───────────────────┘    │  Graph           │
│  - Spatial    │             │                └──────────────────┘
│  - JSON       │             │                         ▲
└───────────────┘             ▼                         │
        ▲         ┌───────────────────┐                │
        │         │   CesConsumer     │                │
        │         │   (CDC Listener)  │                │
        │         └───────────────────┘                │
        │                     │                         │
        │                     └─────────────────────────┘
        │                     ┌───────────────────┐
        └─────────────────────│   Neo4jSync       │
                              │   (Event Consumer)│
                              └───────────────────┘
```

### Component Interaction Flow

1. **Ingestion**: `ModelIngestion` worker downloads models, detects formats, and persists to SQL Server
2. **Storage**: Atoms (content units) are deduplicated and stored with embeddings and spatial projections
3. **Change Capture**: `CesConsumer` monitors SQL Server changes via CES and enriches with semantic metadata
4. **Event Distribution**: Enriched CloudEvents are published to Azure Event Hubs
5. **Provenance**: `Neo4jSync` consumes events and maintains the explainability graph in Neo4j
6. **Administration**: Blazor portal provides real-time monitoring via SignalR

## Technology Stack

### Runtime & Frameworks
- **.NET 10.0**: Core runtime for all services
- **ASP.NET Core**: Web hosting for Blazor admin portal
- **Entity Framework Core 10**: Primary ORM with SQL Server provider
- **Blazor Server**: Interactive admin UI with SignalR

### Data Storage
- **SQL Server 2025**: Primary data store with native vector and spatial support
  - `VECTOR<float>` type for embeddings (up to 1998 dimensions)
  - `GEOMETRY`/`GEOGRAPHY` for spatial reasoning
  - Native JSON type for flexible metadata
  - DiskANN indexes for vector similarity
  - SQL CLR for custom functions
- **Neo4j**: Graph database for inference provenance and explainability

### Event Streaming
- **Azure Event Hubs**: Distributed event streaming platform
- **CloudEvents**: Standard event format for interoperability

### AI/ML Libraries
- **NetTopologySuite**: Spatial geometry operations
- **TorchSharp**: PyTorch model loading (optional)
- **ML.NET**: Future integration for inference (planned)

### Supporting Libraries
- **Microsoft.Data.SqlClient**: Enhanced SQL Server connectivity
- **Neo4j.Driver**: Official Neo4j .NET driver
- **Azure.Messaging.EventHubs**: Event Hubs client library
- **System.Text.Json**: High-performance JSON serialization

## Core Components

### 1. Hartonomous.Core

**Purpose**: Domain model and abstractions

**Key Entities**:
- `Atom`: Deduplicated content unit (text, image patch, audio frame, etc.)
- `AtomEmbedding`: Vector representation with spatial projection
- `Model`: AI model metadata and layer catalog
- `ModelLayer`: Neural network layer with LINESTRING weight storage
- `InferenceRequest`: Logged inference operation with telemetry
- `TensorAtom`: Atomic tensor decomposition for model compression

**Key Interfaces**:
- `IAtomRepository`: CRUD operations for atoms
- `IAtomEmbeddingRepository`: Vector similarity and spatial searches
- `IModelRepository`: Model catalog management
- `IAtomIngestionService`: Deduplication and persistence orchestration
- `IStudentModelService`: Model distillation and extraction

### 2. Hartonomous.Data

**Purpose**: Entity Framework Core data access

**Highlights**:
- `HartonomousDbContext`: Main EF Core context with spatial configuration
- Fluent entity configurations with native JSON and vector types
- Migrations include both schema and stored procedures
- Design-time factory for tooling support

**Database Features**:
- Unique indexes on content hashes for deduplication
- DiskANN vector indexes for O(log n) similarity search
- Spatial indexes for geometric reasoning
- Cascading deletes for referential integrity

### 3. Hartonomous.Infrastructure

**Purpose**: Service implementations and data access

**Services**:
- `AtomIngestionService`: Content deduplication with configurable policies
- `ModelIngestionProcessor`: Format detection and model persistence
- `SpatialInferenceService`: Geometry-based reasoning operations
- `StudentModelService`: Automated model compression
- `ModelDiscoveryService`: Format detection (ONNX, Safetensors, PyTorch, GGUF)

**Repositories**:
- `AtomRepository`: EF Core-based atom management
- `AtomEmbeddingRepository`: Hybrid EF Core + raw SQL for vector operations
- `ModelRepository`: Model catalog with layer relationships
- `CdcRepository`: Change data capture event consumption

### 4. ModelIngestion

**Purpose**: CLI worker for model and embedding ingestion

**Capabilities**:
- Download models from Hugging Face and Ollama
- Detect and parse ONNX, Safetensors, PyTorch, GGUF formats
- Extract layers with weight geometries
- Batch embedding ingestion with deduplication
- Atomic storage for pixels, audio samples, and text tokens
- Query utilities for hybrid search testing

**Model Format Support**:
- **ONNX**: Via ONNX Runtime or custom loader
- **Safetensors**: Direct binary tensor reading
- **PyTorch**: TorchSharp state dict parsing
- **GGUF**: Llama.cpp format for quantized models

### 5. CesConsumer

**Purpose**: SQL Server 2025 Change Event Streaming listener

**Features**:
- Polls `sys.dm_cdc_*` tables for change events
- Transforms to CloudEvents with SQL Server extensions
- Semantic enrichment (capabilities, compliance, reasoning mode)
- Batched publish to Azure Event Hubs
- LSN-based checkpoint persistence

**Event Enrichment**:
- Model events: Inferred capabilities, compliance flags
- Inference events: Reasoning mode, SLA classification
- Atom events: Deduplication statistics, modality metadata

### 6. Neo4jSync

**Purpose**: CloudEvents consumer for provenance graph maintenance

**Responsibilities**:
- Consumes enriched events from Azure Event Hubs
- Creates/updates Neo4j nodes (Model, Inference, Knowledge, etc.)
- Establishes relationships (USED_MODEL, INFLUENCED_BY, etc.)
- Maintains semantic metadata from CES enrichment
- Checkpoint-based resumable processing

### 7. Hartonomous.Admin

**Purpose**: Blazor Server administrative portal

**Features**:
- Real-time telemetry dashboard via SignalR
- Model catalog browser
- Model ingestion UI (single and bulk)
- Student model extraction with parameter controls
- Operation queue monitoring
- Dark-themed responsive UI

**Background Services**:
- `AdminOperationWorker`: Processes queued ingestion/extraction operations
- `TelemetryBackgroundService`: Aggregates and broadcasts metrics
- `AdminOperationCoordinator`: Manages operation lifecycle

### 8. SqlClr

**Purpose**: SQL CLR assembly for in-database processing

**Functions**:
- **Vector Operations**: Dot product, cosine similarity, normalization (VARBINARY-based)
- **Audio Processing**: PCM to geometry conversion, RMS/peak analysis
- **Image Processing**: Pixel cloud generation, color averaging, histograms
- **Image Generation**: Diffusion-inspired patch synthesis
- **Semantic Analysis**: Heuristic topic/sentiment/formality extraction
- **Spatial Operations**: Geometry helpers (convex hull, point-in-region, etc.)

## Data Architecture

### Atomic Content Model

All content is decomposed into **atoms** - deduplicated, immutable content units:

- **Text**: Sentences, paragraphs, or semantic chunks
- **Images**: Patches (e.g., 16x16 pixel blocks)
- **Audio**: Fixed-duration frames (e.g., 100ms windows)
- **Video**: Individual frames with temporal metadata
- **Tensors**: Atomic tensor components for model compression

### Embedding Strategy

1. **Vector Storage**: SQL Server `VECTOR<float>` (native type, up to 1998 dimensions)
2. **Dimension Padding**: Smaller embeddings padded to 1998 for DiskANN compatibility
3. **Spatial Projection**: 3D `GEOMETRY` (Point) derived from high-dimensional vectors
4. **Component Storage**: Explicit dimension breakdown for interpretability

### Model Storage

Neural network weights are stored as **GEOMETRY (LINESTRING ZM)**:
- **X coordinate**: Index in flattened tensor
- **Y coordinate**: Weight value
- **Z coordinate**: Importance score (gradient, attention weight)
- **M coordinate**: Temporal/structural metadata (iteration, depth)

**Advantages**:
- No dimension limits (supports billion+ parameter models)
- Spatial indexes enable O(log n) queries by weight range
- Rich metadata in Z/M dimensions
- Supports arbitrary tensor shapes via JSON `TensorShape` property

### Deduplication Policies

Configurable deduplication at ingestion time:

- **Hash-based**: Exact SHA256 content match
- **Semantic**: Cosine similarity threshold (e.g., 0.95)
- **Spatial**: Geometric distance threshold
- **Hybrid**: Combined vector + spatial thresholds

Reference counting ensures safe cleanup of orphaned atoms.

## Event-Driven Architecture

### Change Data Capture Flow

```
SQL Server Tables → CES Tables (sys.dm_cdc_*) → CesConsumer
                                                      ↓
                                             CloudEvents (enriched)
                                                      ↓
                                             Azure Event Hubs
                                                      ↓
                                                 Neo4jSync
                                                      ↓
                                              Neo4j Graph
```

### CloudEvents Schema

Standard CloudEvents envelope with extensions:

```json
{
  "specversion": "1.0",
  "type": "com.hartonomous.sqlserver.model.created",
  "source": "sqlserver://hartonomous/models",
  "id": "<uuid>",
  "time": "2025-11-01T12:00:00Z",
  "datacontenttype": "application/json",
  "sqlserver_operation": "INSERT",
  "sqlserver_table": "Models",
  "sqlserver_lsn": "0x...",
  "data": {
    "model_id": 42,
    "model_name": "bert-base-uncased",
    "inferred_capabilities": ["text-embedding", "classification"],
    "compliance_verified": true
  }
}
```

### Provenance Graph Model

Neo4j schema captures full inference lineage:

- **Nodes**: Inference, Model, Decision, Evidence, Context, Alternative
- **Relationships**: USED_MODEL, RESULTED_IN, SUPPORTED_BY, INFLUENCED_BY
- **Queries**: Explainability, counterfactual analysis, performance tracking

## Design Patterns

### Repository Pattern
Clean separation between domain logic and data access. All repositories implement interfaces from `Hartonomous.Core.Interfaces`.

### Service Layer Pattern
Business logic encapsulated in services (`IAtomIngestionService`, `ISpatialInferenceService`, etc.), coordinating multiple repositories.

### Factory Pattern
`ModelReaderFactory` selects appropriate model format reader based on extension and file signatures.

### Strategy Pattern
Configurable deduplication policies (`DeduplicationPolicy` entity) allow runtime strategy selection.

### Observer Pattern
SignalR hubs broadcast telemetry updates to subscribed admin portal clients.

### Orchestrator Pattern
`IngestionOrchestrator` coordinates multi-step workflows (download, parse, validate, persist).

### Command Pattern
Admin portal operations queued and processed asynchronously by background workers.

## Scalability & Performance

### Vector Search Optimization
- **DiskANN indexes**: O(log n) approximate nearest neighbor search
- **Coarse/fine dual indexes**: Multi-resolution spatial search
- **Batched operations**: Bulk insert/update with transaction management

### Spatial Reasoning
- **R-tree indexes**: Efficient geometric queries on spatial projections
- **SQL CLR functions**: In-database geometry operations without data movement
- **Multi-resolution anchors**: Coarse filtering before fine-grained search

### Model Ingestion
- **Streaming parsers**: Process large models without loading entirely into memory
- **Parallel layer ingestion**: Concurrent writes for multi-layer models
- **Format-specific optimizers**: Direct binary tensor reading (Safetensors)

### Caching Strategy
- **Layer activation caching**: Frequently used activations cached with hit rate tracking
- **Atomic storage**: Reference-counted deduplication reduces storage by 60-90%
- **EF Core query caching**: Compiled queries for hot paths

### Horizontal Scaling
- **Stateless workers**: Multiple `ModelIngestion` instances can run in parallel
- **Event Hub partitioning**: Parallel event consumption via consumer groups
- **Read replicas**: SQL Server read replicas for query scaling (future)

### Monitoring & Telemetry
- **Application Insights**: Distributed tracing and performance monitoring
- **SignalR dashboards**: Real-time operation status
- **Neo4j analytics**: Query performance analysis and graph statistics

---

## Next Steps

- Review [Data Model Documentation](data-model.md) for detailed entity schemas
- See [Deployment Guide](deployment.md) for infrastructure setup
- Consult [API Reference](api-reference.md) for service interfaces
- Read [Operations Guide](operations.md) for day-to-day procedures

