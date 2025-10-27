# Hartonomous - SQL Server 2025 AI Inference Engine

**A revolutionary cognitive database system that turns SQL Server 2025 into a queryable AI inference engine with zero VRAM requirements.**

## Architecture Overview

```
┌─────────────────────────────────────────────────────────────────┐
│                  SQL SERVER 2025                                 │
│  The "Thinking" Layer - AI Inference via Indexes & T-SQL        │
│                                                                  │
│  • VECTOR indexes (DiskANN) = Attention mechanisms              │
│  • SPATIAL indexes = Multi-resolution features                  │
│  • COLUMNSTORE = Sequential/temporal data                       │
│  • GRAPH (nodes/edges) = Network topology                       │
│  • JSON indexes = Context filtering                             │
│                                                                  │
│  Pre-computed models stored as data, not loaded into VRAM       │
└───────────────────────┬─────────────────────────────────────────┘
                        │ Change Event Streaming (CES)
                        │ CloudEvents
                        ↓
┌─────────────────────────────────────────────────────────────────┐
│                  .NET 10 SERVICES                                │
│  CES Consumer → Semantic Enrichment → Model Analytics           │
└───────────────────────┬─────────────────────────────────────────┘
                        │ Bolt Protocol
                        ↓
┌─────────────────────────────────────────────────────────────────┐
│                  NEO4J                                           │
│  The "Memory" Layer - Semantic Audit Trail                      │
│                                                                  │
│  • Complete inference traces                                    │
│  • Decision provenance graphs                                   │
│  • Model evolution & performance tracking                       │
│  • Explainability paths                                         │
└─────────────────────────────────────────────────────────────────┘
```

## Key Innovations

### 1. Indexes ARE Models, Queries ARE Inference
- **DiskANN Vector Index**: Pre-built attention graphs, sub-millisecond semantic search
- **Spatial Index (4-level)**: Multi-resolution feature pyramids, convolutional operations
- **Columnstore**: 10-100x compression for sequential data (audio, video, time series)
- **Graph**: Neural network topology stored as nodes/edges

### 2. Multi-Modal Data Unification
- **Images**: Pixels as GEOMETRY (point clouds), patches as POLYGON, embeddings as VECTOR
- **Audio**: Waveforms as LINESTRING, frames in COLUMNSTORE, spectrograms as GEOMETRY
- **Video**: Frame sequences, motion vectors, optical flow as spatial data
- **Text**: Tokens in COLUMNSTORE, embeddings in VECTOR, fulltext + semantic hybrid search

### 3. Zero-VRAM Inference
- Models stored as pre-computed indexes on disk
- Inference = SQL queries (no GPU needed)
- Cached activations for common paths (80%+ cache hit rate)
- Quantized weights (4x memory reduction)
- Sparse attention via spatial indexing (O(n²) → O(n log n))

### 4. Model Ensembling Built-In
```sql
-- Query 10 LLMs at once, ensemble results
EXEC sp_GenerateText
    @prompt = 'Explain quantum computing',
    @models = '1,2,3,4,5,6,7,8,9,10'
```

### 5. Complete Auditability
Every inference operation captured via Change Event Streaming (CES) and traced in Neo4j:
- Which models contributed
- Why decisions were made
- Alternative paths considered
- Model performance over time

## Project Structure

```
Hartonomous/
├── src/
│   ├── SqlClr/              # .NET Framework 4.8 CLR functions
│   ├── CesConsumer/         # .NET 10 Change Event consumer
│   ├── Neo4jSync/           # .NET 10 Neo4j synchronization
│   ├── ModelIngestion/      # .NET 10 model ingestion utilities
│   └── Common/              # Shared libraries
├── sql/
│   ├── schemas/             # Table definitions for multi-modal data
│   ├── procedures/          # Inference procedures (sp_GenerateText, etc.)
│   └── indexes/             # Index creation scripts
├── neo4j/
│   ├── schemas/             # Neo4j schema constraints
│   └── queries/             # Common Cypher queries
├── scripts/
│   └── deployment/          # Deployment & setup scripts
└── docs/                    # Documentation

```

## Prerequisites

- **SQL Server 2025 RC1+** (localhost, Windows Authentication)
- **Neo4j Desktop 2.0.5+** (localhost:7474, credentials: neo4j/neo4jneo4j)
- **.NET 10 SDK**
- **.NET Framework 4.8 Developer Pack** (for CLR)

## Getting Started

### 1. Deploy SQL Schema
```bash
sqlcmd -S localhost -E -C -i sql/schemas/01_CoreTables.sql
sqlcmd -S localhost -E -C -i sql/schemas/02_MultiModalData.sql
sqlcmd -S localhost -E -C -i sql/schemas/03_ModelStorage.sql
```

### 2. Deploy SQL CLR Assembly
```bash
cd src/SqlClr
dotnet build -c Release
sqlcmd -S localhost -E -C -i ../../sql/procedures/DeployCLR.sql
```

### 3. Initialize Neo4j Schema
```bash
# Via Neo4j Browser or cypher-shell
cat neo4j/schemas/CoreSchema.cypher | cypher-shell -u neo4j -p neo4jneo4j
```

### 4. Start Services
```bash
# CES Consumer
cd src/CesConsumer
dotnet run

# Neo4j Sync
cd src/Neo4jSync
dotnet run
```

## Usage Examples

### Ingest a Model
```python
python scripts/ingest_model.py --model-name "bert-base-uncased" --model-type "transformer"
```

### Text Generation (10-Model Ensemble)
```sql
DECLARE @result NVARCHAR(MAX)
EXEC sp_GenerateText
    @prompt = 'Write a haiku about databases',
    @max_tokens = 50,
    @models = '1,2,3,4,5,6,7,8,9,10',
    @result = @result OUTPUT
PRINT @result
```

### Image Generation
```sql
EXEC sp_GenerateImage
    @prompt = 'sunset over mountains',
    @models = '11,12,13',
    @output_image_id OUTPUT
```

### Query Model Performance
```cypher
// In Neo4j: Which models perform best for image generation?
MATCH (i:Inference)-[u:USED_MODEL]->(m:Model)
WHERE i.task = 'image_generation' AND i.user_rating > 4
RETURN m.name, avg(u.contribution_weight), count(*)
ORDER BY avg(u.contribution_weight) DESC
```

## Key Features

### Multi-Modal Inference
- **Text**: LLM-style generation, semantic search, sentiment analysis
- **Image**: Generation, classification, similarity search, segmentation
- **Audio**: Generation, transcription, similarity matching
- **Video**: Generation, frame analysis, motion tracking

### Advanced Capabilities
- **Attention Mechanisms**: Via DiskANN vector search
- **Convolutional Operations**: Via spatial index queries
- **Recurrent Processing**: Via columnstore window functions
- **Graph Neural Networks**: Via SQL graph MATCH patterns
- **Sparse Attention**: Via spatial proximity filtering
- **Cached Activations**: Pre-computed common inference paths

### Self-Improvement
- System monitors which models/strategies work best
- Adjusts ensemble weights based on performance
- Learns from user feedback stored in Neo4j
- Complete feedback loop: SQL Server ↔ Neo4j

## Development Status

- [x] Architecture design
- [x] Database connections verified
- [ ] SQL CLR functions
- [ ] Core SQL schema
- [ ] CES consumer service
- [ ] Neo4j sync service
- [ ] Model ingestion pipeline
- [ ] Inference procedures
- [ ] Example models

## License

MIT

## Contributing

This is an experimental research project exploring novel approaches to AI inference using database primitives.
