# Hartonomous Data Model Documentation

## Table of Contents

- [Overview](#overview)
- [Core Entities](#core-entities)
- [Entity Relationships](#entity-relationships)
- [Database Schema](#database-schema)
- [Indexes and Performance](#indexes-and-performance)
- [Data Types](#data-types)
- [Sample Queries](#sample-queries)

## Overview

The Hartonomous data model is designed around **atomic content decomposition** and **multimodal embeddings**. All content (text, images, audio, video) is broken down into deduplicated atoms, each with vector embeddings and spatial projections for hybrid search.

### Design Principles

1. **Atomic Decomposition**: All content decomposed into smallest meaningful units
2. **Deduplication**: Reference-counted shared atoms reduce storage by 60-90%
3. **Hybrid Indexing**: Vector (DiskANN) + Spatial (R-tree) + Traditional (B-tree) indexes
4. **Geometry-Based Storage**: Neural network weights stored as LINESTRING for unlimited dimensions
5. **Native JSON**: Configuration and metadata using SQL Server 2025 JSON type

## Core Entities

### Atom

**Purpose**: Deduplicated content unit (text chunk, image patch, audio frame, etc.)

**Schema**:

| Column | Type | Description |
|--------|------|-------------|
| AtomId | bigint (PK) | Auto-incrementing unique identifier |
| ContentHash | varbinary(32) | SHA256 hash for deduplication (unique index) |
| Modality | nvarchar(64) | Content type: 'text', 'image', 'audio', 'video', 'tensor' |
| Subtype | nvarchar(64) | Optional subtype: 'paragraph', 'patch', 'frame' |
| SourceUri | nvarchar(max) | Original source location |
| SourceType | nvarchar(128) | Source type: 'file', 'url', 'stream' |
| CanonicalText | nvarchar(max) | Text representation (for text modality) |
| PayloadLocator | nvarchar(1024) | Storage reference (blob URL, file path) |
| Metadata | JSON | Flexible metadata (dimensions, duration, etc.) |
| CreatedAt | datetime2 | Creation timestamp (default: SYSUTCDATETIME()) |
| UpdatedAt | datetime2 | Last modification timestamp |
| IsActive | bit | Soft delete flag (default: 1) |
| ReferenceCount | bigint | Number of references (default: 0) |
| SpatialKey | geometry (Point) | Optional spatial index key |

**Relationships**:

- One-to-many with `AtomEmbedding`
- One-to-many with `TensorAtom`
- Many-to-many with other `Atom` via `AtomRelation`

### AtomEmbedding

**Purpose**: Vector representation of an atom with spatial projection

**Schema**:

| Column | Type | Description |
|--------|------|-------------|
| AtomEmbeddingId | bigint (PK) | Unique identifier |
| AtomId | bigint (FK) | Reference to parent atom |
| ModelId | int (FK, nullable) | Model that generated embedding |
| EmbeddingType | nvarchar(64) | Type: 'semantic', 'visual', 'acoustic' |
| Dimension | int | Original vector dimension |
| EmbeddingVector | vector(1998) | Padded vector (SQL Server 2025 native) |
| UsesMaxDimensionPadding | bit | Indicates if padded to 1998 |
| SpatialGeometry | geometry (Point) | 3D spatial projection (fine resolution) |
| SpatialCoarse | geometry (Point) | 3D coarse projection (for filtering) |
| Metadata | JSON | Additional metadata |
| CreatedAt | datetime2 | Creation timestamp |

**Indexes**:

- DiskANN on `EmbeddingVector`
- Spatial index on `SpatialGeometry` (fine)
- Spatial index on `SpatialCoarse`
- B-tree on `(AtomId, EmbeddingType)`

**Relationships**:

- Many-to-one with `Atom`
- Many-to-one with `Model` (nullable)
- One-to-many with `AtomEmbeddingComponent`

### Model

**Purpose**: AI model metadata and catalog

**Schema**:

| Column | Type | Description |
|--------|------|-------------|
| ModelId | int (PK) | Unique identifier |
| ModelName | nvarchar(200) | Human-readable name |
| ModelType | nvarchar(100) | Type: 'transformer', 'cnn', 'diffusion' |
| Architecture | nvarchar(100) | Variant: 'bert-base', 'gpt2-small' |
| Config | JSON | Model configuration |
| ParameterCount | bigint | Total parameters |
| IngestionDate | datetime2 | When ingested (default: SYSUTCDATETIME()) |
| LastUsed | datetime2 | Last inference timestamp |
| UsageCount | bigint | Inference count (default: 0) |
| AverageInferenceMs | float | Average inference time |

**Indexes**:

- B-tree on `ModelName`
- B-tree on `ModelType`

**Relationships**:

- One-to-many with `ModelLayer`
- One-to-many with `InferenceRequest`
- One-to-one with `ModelMetadata`
- One-to-many with `AtomEmbedding`
- One-to-many with `TensorAtom`

### ModelLayer

**Purpose**: Individual layer in a neural network model

**Schema**:

| Column | Type | Description |
|--------|------|-------------|
| LayerId | bigint (PK) | Unique identifier |
| ModelId | int (FK) | Parent model |
| LayerIdx | int | Zero-based layer index |
| LayerName | nvarchar(200) | Name: 'encoder.layer.0.attention.self.query' |
| LayerType | nvarchar(100) | Type: 'embedding', 'attention', 'feedforward' |
| WeightsGeometry | geometry (LINESTRING ZM) | Weights as spatial geometry |
| TensorShape | nvarchar(max) | JSON array: "[3584, 3584]" |
| TensorDtype | nvarchar(50) | Data type: 'float32', 'float16', 'bfloat16' |
| QuantizationType | nvarchar(50) | Quantization: 'int8', 'int4', 'fp16' |
| QuantizationScale | float | Quantization scale factor |
| QuantizationZeroPoint | float | Quantization zero point |
| Parameters | JSON | Layer parameters |
| ParameterCount | bigint | Number of parameters |
| CacheHitRate | float | Activation cache hit rate (default: 0) |
| AvgComputeTimeMs | float | Average computation time |

**Weight Storage Format** (LINESTRING ZM):

- **X coordinate**: Index in flattened tensor (0, 1, 2, ...)
- **Y coordinate**: Weight value (actual float)
- **Z coordinate**: Importance score (gradient magnitude, attention weight)
- **M coordinate**: Temporal metadata (training iteration, layer depth)

**Indexes**:

- B-tree on `(ModelId, LayerIdx)`
- Spatial index on `WeightsGeometry`

**Relationships**:

- Many-to-one with `Model`
- One-to-many with `CachedActivation`
- One-to-many with `TensorAtom`
- One-to-many with `TensorAtomCoefficient`

### InferenceRequest

**Purpose**: Logged inference operation with telemetry

**Schema**:

| Column | Type | Description |
|--------|------|-------------|
| InferenceId | bigint (PK) | Unique identifier |
| ModelId | int (FK, nullable) | Model used (null for ensemble) |
| TaskType | nvarchar(100) | Task: 'text-generation', 'embedding', 'search' |
| InputText | nvarchar(max) | Input prompt/query |
| InputVector | vector(1998) | Input embedding (if applicable) |
| OutputText | nvarchar(max) | Generated output |
| Confidence | float | Confidence score (0-1) |
| StartTime | datetime2 | Request start time |
| EndTime | datetime2 | Request end time |
| DurationMs | int | Computed duration |
| TokensGenerated | int | Number of tokens generated |
| Temperature | float | Sampling temperature |
| Metadata | JSON | Additional context |

**Indexes**:

- B-tree on `TaskType`
- B-tree on `StartTime` (DESC)
- B-tree on `ModelId` (for usage analytics)

**Relationships**:

- Many-to-one with `Model` (nullable)
- One-to-many with `InferenceStep`

### TensorAtom

**Purpose**: Atomic tensor decomposition for model compression

**Schema**:

| Column | Type | Description |
|--------|------|-------------|
| TensorAtomId | bigint (PK) | Unique identifier |
| AtomId | bigint (FK) | Reference to base atom |
| ModelId | int (FK, nullable) | Source model |
| LayerId | bigint (FK, nullable) | Source layer |
| TensorData | geometry (LINESTRING) | Tensor values as geometry |
| TensorShape | nvarchar(max) | Shape as JSON array |
| TensorDtype | nvarchar(50) | Data type |
| Metadata | JSON | Decomposition metadata |
| CreatedAt | datetime2 | Creation timestamp |

**Purpose**: Enables tensor-level deduplication across models (e.g., shared attention heads).

**Relationships**:

- Many-to-one with `Atom`
- Many-to-one with `Model` (nullable)
- Many-to-one with `ModelLayer` (nullable)
- One-to-many with `TensorAtomCoefficient`

### DeduplicationPolicy

**Purpose**: Configurable deduplication strategy

**Schema**:

| Column | Type | Description |
|--------|------|-------------|
| DeduplicationPolicyId | int (PK) | Unique identifier |
| PolicyName | nvarchar(128) | Name: 'strict', 'semantic', 'spatial' |
| HashEnabled | bit | Enable exact hash matching |
| SemanticThreshold | float | Cosine similarity threshold (e.g., 0.95) |
| SpatialThreshold | float | Spatial distance threshold |
| IsActive | bit | Policy is active (default: 1) |
| Metadata | JSON | Additional configuration |
| CreatedAt | datetime2 | Creation timestamp |

**Unique Index**: `PolicyName`

## Entity Relationships

### Entity Relationship Diagram

```text
┌──────────────┐         ┌──────────────────┐         ┌──────────────┐
│    Model     │────┬───>│   ModelLayer     │────────>│CachedActivation│
└──────────────┘    │    └──────────────────┘         └──────────────┘
       │            │              │
       │            │              │
       │            │              v
       │            │    ┌──────────────────┐
       │            │    │   TensorAtom     │
       │            │    └──────────────────┘
       │            │              │
       │            │              v
       │            │    ┌──────────────────┐
       │            │    │TensorAtomCoeff   │
       │            │    └──────────────────┘
       │            │
       v            v
┌──────────────┐  ┌──────────────────┐
│ InferenceReq │  │ AtomEmbedding    │
└──────────────┘  └──────────────────┘
       │                    │
       │                    │
       v                    v
┌──────────────┐  ┌──────────────────┐
│InferenceStep │  │AtomEmbeddingComp │
└──────────────┘  └──────────────────┘
                           │
                           v
                  ┌──────────────────┐
                  │      Atom        │<───────┐
                  └──────────────────┘        │
                           │                  │
                           v                  │
                  ┌──────────────────┐        │
                  │  AtomRelation    │────────┘
                  └──────────────────┘

┌──────────────────┐
│DeduplicationPolicy│  (Referenced by AtomIngestionService)
└──────────────────┘
```

### Key Relationships

1. **Model → ModelLayer**: One-to-many (cascade delete)
2. **Model → InferenceRequest**: One-to-many (no cascade, preserve history)
3. **Atom → AtomEmbedding**: One-to-many (cascade delete)
4. **Atom → TensorAtom**: One-to-many (cascade delete)
5. **Atom → AtomRelation**: Many-to-many (self-referencing for graph)
6. **ModelLayer → CachedActivation**: One-to-many (cascade delete)
7. **ModelLayer → TensorAtom**: One-to-many (set null on delete)

## Database Schema

### Tables Summary

| Table | Rows (Typical) | Primary Use |
|-------|----------------|-------------|
| Atoms | 10M - 1B | Deduplicated content units |
| AtomEmbeddings | 50M - 5B | Vector embeddings with spatial projections |
| Models | 10 - 1000 | Model catalog |
| ModelLayers | 100 - 100K | Layer definitions |
| InferenceRequests | 1M - 100M | Inference logs |
| TensorAtoms | 1M - 100M | Tensor decomposition |
| DeduplicationPolicies | 5 - 20 | Active deduplication strategies |

### Storage Estimates

**Per Million Atoms**:

- Atoms table: ~500 MB (without PayloadLocator blob references)
- AtomEmbeddings: ~8 GB (1998-dim vectors)
- Spatial indexes: ~2 GB
- Total: ~10.5 GB per million atoms

**Per Model** (GPT-2 scale, 125M params):

- Model record: <1 KB
- ModelLayers: ~500 rows × 100 KB each = 50 MB
- Total: ~50 MB per model

### Partitioning Strategy (Future)

For tables exceeding 100M rows, consider partitioning:

```sql
-- Partition Atoms by Modality
CREATE PARTITION FUNCTION PF_Modality (nvarchar(64))
AS RANGE RIGHT FOR VALUES ('audio', 'image', 'tensor', 'text', 'video');

CREATE PARTITION SCHEME PS_Modality
AS PARTITION PF_Modality ALL TO ([PRIMARY]);

-- Apply to Atoms table (requires rebuild)
CREATE TABLE Atoms (...) ON PS_Modality(Modality);
```

## Indexes and Performance

### DiskANN Vector Indexes

```sql
-- Primary vector index (approximate nearest neighbor)
CREATE INDEX IX_AtomEmbeddings_DiskANN
ON AtomEmbeddings (EmbeddingVector)
WITH (
    DATA_COMPRESSION = ROWSTORE_MEMORY_OPTIMIZED,
    MAX_DEGREE = 64,
    ENTRY_POINT_SIZE = 100,
    METRIC = 'cosine'
);
```

**Performance**: O(log n) for ANN search, typically <10ms for 100M vectors.

### Spatial Indexes

```sql
-- Fine-grained spatial index
CREATE SPATIAL INDEX IX_AtomEmbeddings_SpatialGeometry
ON AtomEmbeddings (SpatialGeometry)
WITH (
    BOUNDING_BOX = (-1000, -1000, 1000, 1000),
    GRIDS = (
        LEVEL_1 = MEDIUM,
        LEVEL_2 = MEDIUM,
        LEVEL_3 = MEDIUM,
        LEVEL_4 = MEDIUM
    ),
    CELLS_PER_OBJECT = 16
);

-- Coarse spatial index for fast filtering
CREATE SPATIAL INDEX IX_AtomEmbeddings_SpatialCoarse
ON AtomEmbeddings (SpatialCoarse)
WITH (
    BOUNDING_BOX = (-100, -100, 100, 100),
    CELLS_PER_OBJECT = 8
);
```

**Performance**: Spatial filter reduces candidate set by 90-99% before vector reranking.

### Composite Indexes

```sql
-- Atom lookup by hash
CREATE UNIQUE INDEX UX_Atoms_ContentHash
ON Atoms (ContentHash)
INCLUDE (AtomId, Modality, ReferenceCount);

-- Embedding lookup by atom and type
CREATE INDEX IX_AtomEmbeddings_AtomId_Type
ON AtomEmbeddings (AtomId, EmbeddingType)
INCLUDE (EmbeddingVector, Dimension);

-- Layer lookup by model
CREATE INDEX IX_ModelLayers_ModelId_Idx
ON ModelLayers (ModelId, LayerIdx)
INCLUDE (LayerName, ParameterCount);
```

### Statistics Maintenance

```sql
-- Update statistics weekly for large tables
UPDATE STATISTICS Atoms WITH FULLSCAN;
UPDATE STATISTICS AtomEmbeddings WITH FULLSCAN;
```

## Data Types

### SQL Server 2025 Native Types

**VECTOR(n)**:

- Native vector type for embeddings
- Max dimensions: 1998 floats (7992 bytes)
- Supports DiskANN indexing
- Cosine/Euclidean/Dot Product metrics built-in

**JSON**:

- Schema-free JSON storage
- Queryable via JSON_VALUE, JSON_QUERY
- Validated on insert
- Better compression than NVARCHAR(MAX)

**GEOMETRY/GEOGRAPHY**:

- NetTopologySuite integration via EF Core
- Supports Points, LineStrings, Polygons, MultiPoint
- Spatial indexes (R-tree)
- Rich function library (STDistance, STIntersects, etc.)

### Custom CLR Types

**SqlVector<T>** (Legacy):

- Used for VARBINARY-based vector storage
- Being phased out in favor of native VECTOR type
- Still used in some CLR functions

## Sample Queries

### Find Similar Atoms by Vector

```sql
DECLARE @query_vector VECTOR(1998) = /* your vector */;

SELECT TOP 10
    a.AtomId,
    a.CanonicalText,
    ae.EmbeddingVector.CosineDistance(@query_vector) AS Distance
FROM Atoms a
JOIN AtomEmbeddings ae ON a.AtomId = ae.AtomId
WHERE ae.EmbeddingType = 'semantic'
ORDER BY ae.EmbeddingVector.CosineDistance(@query_vector) ASC;
```

### Hybrid Search (Spatial + Vector)

```sql
DECLARE @query_vector VECTOR(1998) = /* your vector */;
DECLARE @query_point GEOMETRY = geometry::Point(500, 500, 500, 0);
DECLARE @spatial_radius FLOAT = 100;

-- Stage 1: Spatial filter
WITH SpatialCandidates AS (
    SELECT AtomEmbeddingId, AtomId, EmbeddingVector
    FROM AtomEmbeddings
    WHERE SpatialGeometry.STDistance(@query_point) < @spatial_radius
)
-- Stage 2: Vector reranking
SELECT TOP 10
    a.AtomId,
    a.CanonicalText,
    sc.EmbeddingVector.CosineDistance(@query_vector) AS Distance
FROM SpatialCandidates sc
JOIN Atoms a ON sc.AtomId = a.AtomId
ORDER BY sc.EmbeddingVector.CosineDistance(@query_vector) ASC;
```

### Extract Student Model by Importance

```sql
-- Get top 20% most important layers from a model
DECLARE @model_id INT = 42;
DECLARE @target_ratio FLOAT = 0.2;

WITH LayerImportance AS (
    SELECT
        LayerId,
        LayerName,
        ParameterCount,
        -- Extract importance from Z coordinate (avg across all points)
        AVG(WeightsGeometry.STPointN(n.number).Z) AS AvgImportance,
        ROW_NUMBER() OVER (ORDER BY AVG(WeightsGeometry.STPointN(n.number).Z) DESC) AS Rank
    FROM ModelLayers ml
    CROSS APPLY (
        SELECT TOP (ml.WeightsGeometry.STNumPoints())
            ROW_NUMBER() OVER (ORDER BY (SELECT NULL)) AS number
        FROM sys.objects
    ) n
    WHERE ModelId = @model_id
    GROUP BY LayerId, LayerName, ParameterCount
)
SELECT
    LayerId,
    LayerName,
    ParameterCount,
    AvgImportance
FROM LayerImportance
WHERE Rank <= (SELECT COUNT(*) * @target_ratio FROM ModelLayers WHERE ModelId = @model_id)
ORDER BY Rank;
```

### Inference Analytics

```sql
-- Model performance comparison
SELECT
    m.ModelName,
    COUNT(*) AS InferenceCount,
    AVG(ir.DurationMs) AS AvgDurationMs,
    AVG(ir.Confidence) AS AvgConfidence,
    SUM(ir.TokensGenerated) AS TotalTokens
FROM InferenceRequests ir
JOIN Models m ON ir.ModelId = m.ModelId
WHERE ir.StartTime > DATEADD(day, -7, GETUTCDATE())
GROUP BY m.ModelId, m.ModelName
ORDER BY AvgConfidence DESC;
```

### Deduplication Statistics

```sql
-- Atoms with highest reference counts (most reused)
SELECT TOP 20
    AtomId,
    Modality,
    LEFT(CanonicalText, 100) AS Preview,
    ReferenceCount,
    CreatedAt
FROM Atoms
WHERE ReferenceCount > 1
ORDER BY ReferenceCount DESC;

-- Storage savings from deduplication
SELECT
    Modality,
    COUNT(*) AS UniqueAtoms,
    SUM(ReferenceCount) AS TotalReferences,
    SUM(ReferenceCount) - COUNT(*) AS DuplicatesAvoided,
    CAST(100.0 * (SUM(ReferenceCount) - COUNT(*)) / SUM(ReferenceCount) AS DECIMAL(5,2)) AS SavingsPercent
FROM Atoms
GROUP BY Modality
ORDER BY SavingsPercent DESC;
```

---

## Next Steps

- See [SQL Procedures Reference](sql-procedures.md) for stored procedure documentation
- Review [Architecture Overview](architecture.md) for system design context
- Consult [API Reference](api-reference.md) for programmatic data access

