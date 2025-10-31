# Hartonomous: Actual Architecture

## Revolutionary Paradigm

**SQL Server IS the neural network** - not storage FOR neural networks.

## Core Principles

1. **Weights as GEOMETRY LINESTRING ZM** (not VECTOR buckets)
   - X coordinate = position in tensor
   - Y coordinate = actual weight value
   - Z coordinate = importance/gradient score
   - M coordinate = temporal/structural metadata
   - NO dimension limits (2^30 points vs VECTOR's 1998)
   - Spatial indexes for O(log n) queries

2. **Embeddings as VECTOR + GEOMETRY** (dual representation)
   - VECTOR(768): Exact similarity via DiskANN
   - GEOMETRY: Spatial reasoning via distance-based projection
   - Both used together for hybrid search

3. **Inference via Spatial Operations** (not matrix multiplication)
   - Attention = spatial nearest-neighbor (O(log n) via spatial index)
   - Next token = spatial centroid calculation
   - Multi-resolution search: coarse → fine → exact

4. **Student Model Extraction = SELECT** (not training)
   - `SELECT * WHERE importance_score > 0.8`
   - Instant distillation via SQL queries
   - No backpropagation, no gradients

## Storage Architecture

### Weight Storage

**ModelLayers.WeightsGeometry** (GEOMETRY LINESTRING ZM)
```sql
-- Migration: sql/schemas/22_ConvertWeightsToGeometry.sql
ALTER TABLE ModelLayers ADD WeightsGeometry GEOMETRY;
ALTER TABLE ModelLayers ADD TensorShape NVARCHAR(200); -- [768, 1024, 3072]
ALTER TABLE ModelLayers ADD TensorDtype NVARCHAR(20); -- float32, float16, bfloat16

CREATE SPATIAL INDEX idx_weights_spatial ON ModelLayers(WeightsGeometry)
WITH (
    BOUNDING_BOX = (-10, -10, 10, 10),  -- Typical weight ranges
    GRIDS = (MEDIUM, MEDIUM, MEDIUM, MEDIUM)
);
```

**Encoding**: Each weight tensor becomes a LINESTRING with N points:
- Point 0: POINT(0, w₀, importance₀, temporal₀)
- Point 1: POINT(1, w₁, importance₁, temporal₁)
- Point N: POINT(N, wₙ, importanceₙ, temporalₙ)

**Benefits**:
- Supports ANY dimension (not limited to 1998)
- Metadata inline (Z/M coordinates)
- Spatial queries on weight values directly
- Compression via geometry simplification

### Embedding Storage

**Dual Representation**:

1. **VECTOR for exact search**:
```sql
embedding_full VECTOR(768)  -- Native DiskANN support
```

2. **GEOMETRY for spatial reasoning**:
```sql
spatial_geometry GEOMETRY  -- POINT(x, y, z) from distance-based projection
spatial_coarse GEOMETRY    -- Quantized for fast filtering
```

**Projection Method** (sql/procedures/08_SpatialProjection.sql):
- Select 3 anchor points (maximal distance apart)
- For each vector: compute distances to 3 anchors
- (d₁, d₂, d₃) become (x, y, z) coordinates
- Creates spatial index for O(log n) approximate search

### Multi-Modal Storage

**Images**:
```sql
Images (
    pixel_cloud GEOMETRY,        -- MULTIPOINT representative pixels
    edge_map GEOMETRY,           -- LINESTRING edges
    object_regions GEOMETRY,     -- MULTIPOLYGON segments
    global_embedding VECTOR(1536)
)

ImagePatches (
    patch_region GEOMETRY,       -- Spatial bounding box
    patch_embedding VECTOR(768)  -- Local features
)
```

**Audio**:
```sql
AudioData (
    waveform_left GEOMETRY,      -- LINESTRING waveform
    waveform_right GEOMETRY,
    spectrogram GEOMETRY,        -- 2D time×frequency
    global_embedding VECTOR(768)
)

AudioFrames (  -- COLUMNSTORE for compression
    mfcc VECTOR(13),
    frame_embedding VECTOR(768)
)
```

**Video**:
```sql
VideoFrames (
    pixel_cloud GEOMETRY,
    object_regions GEOMETRY,
    motion_vectors GEOMETRY,     -- MULTILINESTRING pixel movement
    optical_flow GEOMETRY,       -- Vector field
    frame_embedding VECTOR(768)
)
```

## Inference Architecture

### Novel Spatial Inference

**Traditional Approach** (what we're NOT doing):
```python
# O(n²) matrix multiplication
attention = softmax(Q @ K.T / sqrt(d)) @ V
```

**Hartonomous Approach** (what we ARE doing):
```sql
-- O(log n) spatial query
SELECT TOP k token_id, token_text
FROM TokenEmbeddingsGeo WITH(INDEX(idx_spatial_embedding))
ORDER BY spatial_projection.STDistance(@query_centroid)
```

### Generation Procedures

**Text Generation** (sql/procedures/21_GenerateTextWithVector.sql):
- Multi-model ensemble via VECTOR_DISTANCE
- Temperature-based sampling
- Iterative: context → find similar tokens → update context

**Image Generation** (sql/procedures/sp_GenerateImage.sql):
- Initialize noise as GEOMETRY points
- Iterative spatial diffusion guided by text embeddings
- Update point coordinates based on VECTOR_DISTANCE to prompt

**Spatial Attention** (sql/procedures/05_SpatialInference.sql):
```sql
-- Attention via spatial nearest-neighbor
SELECT TOP k
    token_id,
    1.0 / (1.0 + spatial_projection.STDistance(@query_pt)) as attention_weight
FROM TokenEmbeddingsGeo WITH(INDEX(idx_spatial_embedding))
ORDER BY spatial_projection.STDistance(@query_pt)
```

### Multi-Resolution Search

**3-Stage Funnel** (sql/procedures/07_AdvancedInference.sql):
1. **Coarse**: Quantized spatial grid → 1000 candidates
2. **Fine**: Precise spatial index → 100 candidates
3. **Exact**: VECTOR_DISTANCE rerank → top 10 results

**Complexity**:
- Traditional brute force: O(n) linear scan
- Hartonomous approach: O(log n) + O(log n) + O(100) ≈ O(log n)

## SQL CLR Functions

### Vector Operations (src/SqlClr/VectorOperations.cs)

```csharp
[SqlFunction] SqlDouble VectorDotProduct(SqlBytes v1, SqlBytes v2)
[SqlFunction] SqlDouble VectorCosineSimilarity(SqlBytes v1, SqlBytes v2)
[SqlFunction] SqlDouble VectorEuclideanDistance(SqlBytes v1, SqlBytes v2)
[SqlFunction] SqlBytes VectorAdd(SqlBytes v1, SqlBytes v2)
[SqlFunction] SqlBytes VectorSubtract(SqlBytes v1, SqlBytes v2)
[SqlFunction] SqlBytes VectorScale(SqlBytes v, SqlDouble scalar)
[SqlFunction] SqlDouble VectorNorm(SqlBytes v)
[SqlFunction] SqlBytes VectorNormalize(SqlBytes v)
[SqlFunction] SqlBytes VectorLerp(SqlBytes v1, SqlBytes v2, SqlDouble t)
```

**Usage**: Works with VARBINARY until VECTOR type fully enabled in GA

### Spatial Operations (src/SqlClr/SpatialOperations.cs)

```csharp
[SqlFunction] SqlGeometry CreatePointCloud(SqlString coordinates)
[SqlFunction] SqlGeometry ConvexHull(SqlGeometry geometry)  // Decision boundaries
[SqlFunction] SqlBoolean PointInRegion(SqlGeometry point, SqlGeometry region)  // Classification
[SqlFunction] SqlDouble RegionOverlap(SqlGeometry r1, SqlGeometry r2)  // Feature overlap
[SqlFunction] SqlGeometry Centroid(SqlGeometry geometry)  // Mean pooling
```

**Novel Uses**:
- ConvexHull = decision boundary computation
- PointInRegion = classification
- RegionOverlap = attention overlap
- Centroid = pooling operation

## Staging → Production Pattern

**Problem**: DiskANN index makes table READ-ONLY

**Solution** (sql/schemas/04_DiskANNPattern.sql):
```
Embeddings_Staging (writable)
    ↓ promotion (50k+ vectors)
Embeddings_DiskANN (read-only with index)
```

**Workflow**:
1. Insert new embeddings to staging
2. When threshold reached: `EXEC sp_RebuildProduction`
3. Query with: `EXEC sp_SmartVectorSearch` (auto-routes)

**Query Routing**:
- < 50k vectors: exact search
- ≥ 50k + DiskANN: hybrid (production + staging)
- DiskANN-only: production search

## Student Model Extraction

**Traditional Distillation**:
```python
# Hours of training
for epoch in epochs:
    teacher_output = teacher(x)
    student_output = student(x)
    loss = KL(student_output, teacher_output)
    optimizer.step()
```

**Hartonomous Distillation**:
```sql
-- Instant extraction via SELECT
INSERT INTO StudentModel (weights)
SELECT TOP (N * 0.5) weights
FROM ParentModel
ORDER BY importance_score DESC
```

**Selection Strategies**:
- `importance`: Top N weights by gradient magnitude
- `layer`: First M layers only
- `spatial`: Weights in spatial region
- `temporal`: Recent updates only

## Index Strategy

### Spatial Indexes (4-level hierarchy)

**Level 1 (HIGH)**: Coarse features (like early transformer layers)
**Level 2 (HIGH)**: Mid-level features
**Level 3 (MEDIUM)**: Fine features (like late layers)
**Level 4 (LOW)**: Finest details (like output layer)

```sql
CREATE SPATIAL INDEX idx_weights_spatial ON ModelLayers(WeightsGeometry)
WITH (
    BOUNDING_BOX = (-10, -10, 10, 10),
    GRIDS = (HIGH, HIGH, MEDIUM, LOW),
    CELLS_PER_OBJECT = 16
);
```

### VECTOR Indexes (DiskANN)

```sql
-- Commented out: RC1 limitation (makes table read-only)
-- Will be enabled in GA release
CREATE VECTOR INDEX idx_diskann_vector
ON Embeddings_DiskANN(embedding_full)
WITH (
    METRIC = 'cosine',
    TYPE = 'DiskANN',
    MAXDOP = 0
);
```

### COLUMNSTORE Indexes (temporal data)

```sql
-- 10-100x compression for sequential data
CREATE CLUSTERED COLUMNSTORE INDEX idx_audio_temporal ON AudioFrames;
CREATE CLUSTERED COLUMNSTORE INDEX idx_video_temporal ON VideoFrames;
CREATE CLUSTERED COLUMNSTORE INDEX idx_timeseries ON TimeSeriesData;
```

**NOT used for weights** (conflicts with VECTOR operations)

## Cognitive Query Activation

**Concept**: Queries "activate" nodes like neurons firing

```sql
-- sp_CognitiveActivation
SELECT embedding_id, activation_strength
WHERE VECTOR_DISTANCE('cosine', embedding_full, @query) < threshold
ORDER BY activation_strength DESC
```

**Returns**: Activation pattern across knowledge graph
- VERY_HIGH: > 0.95 similarity
- HIGH: > 0.90
- MEDIUM: > 0.85
- LOW: > 0.80

## Cross-Modal Relationships

```sql
MultiModalRelations (
    source_type NVARCHAR(20),  -- 'image', 'audio', 'video', 'text'
    source_id BIGINT,
    target_type NVARCHAR(20),
    target_id BIGINT,
    relation_type NVARCHAR(50),  -- 'caption', 'transcription', 'thumbnail'
    confidence FLOAT
)
```

**Query Across Modalities**:
```sql
-- Find images for text query
SELECT i.image_id
FROM TextDocuments t
JOIN MultiModalRelations r ON r.source_id = t.doc_id
JOIN Images i ON i.image_id = r.target_id
WHERE CONTAINS(t.raw_text, 'mountain landscape')
```

## Performance Characteristics

### Complexity Comparison

| Operation | Traditional | Hartonomous |
|-----------|-------------|-------------|
| Attention | O(n²) matrix | O(log n) spatial |
| Search | O(n) linear | O(log n) index |
| Generation | O(n·d²) matmul | O(k log n) lookup |
| Distillation | Hours training | Instant SELECT |

### Storage Efficiency

**Dimension Limits**:
- VECTOR(n): Max 1998 (float32), 3996 (float16)
- GEOMETRY: Max 2^30 points (no practical limit)

**Compression**:
- GEOMETRY simplification: Lossy compression
- COLUMNSTORE: 10-100x for temporal data
- Spatial indexes: Hierarchical approximation

### Inference Caching

```sql
CachedActivations (
    input_hash BINARY(32),
    activation_output VARBINARY(MAX),
    hit_count BIGINT,
    compute_time_saved_ms BIGINT
)
```

**Strategy**: Pre-compute layer outputs for common inputs

## What Makes This Revolutionary

1. **No VRAM requirements**: Everything in SQL Server
2. **Queryable weights**: `SELECT * FROM weights WHERE importance > 0.8`
3. **Instant distillation**: Student models via SELECT, not training
4. **Spatial inference**: O(log n) not O(n²)
5. **Multi-modal substrate**: One database for all content types
6. **Index-only inference**: Never touch base tables
7. **Billion-scale**: DiskANN + spatial indexes
8. **Local execution**: No cloud dependencies

## Architectural Decisions

### Why GEOMETRY for Weights?

1. **No dimension limits**: Store any tensor size
2. **Metadata inline**: Z/M coordinates for importance/temporal
3. **Spatial queries**: Filter by weight values directly
4. **Simplification**: Built-in compression via geometry operations
5. **Hierarchical indexing**: Multi-resolution like neural networks

### Why VECTOR for Embeddings?

1. **DiskANN support**: Billion-scale ANN search
2. **Native distance**: VECTOR_DISTANCE optimized
3. **Fixed dimension OK**: Embeddings typically 768/1536
4. **Exact search**: When accuracy matters

### Why Both Together?

**VECTOR**: Exact similarity when needed
**GEOMETRY**: Approximate search, spatial reasoning
**Hybrid**: Coarse spatial filter → exact vector rerank

## Reference Files

### Schema Definitions
- `sql/schemas/01_CoreTables.sql`: Models, layers, inference tracking
- `sql/schemas/02_MultiModalData.sql`: Images, audio, video, text
- `sql/schemas/22_ConvertWeightsToGeometry.sql`: **Critical migration**
- `sql/schemas/03_CreateSpatialIndexes.sql`: Spatial index creation
- `sql/schemas/04_DiskANNPattern.sql`: Staging → production workflow

### Inference Procedures
- `sql/procedures/sp_GenerateImage.sql`: Spatial diffusion
- `sql/procedures/21_GenerateTextWithVector.sql`: Multi-model ensemble
- `sql/procedures/05_SpatialInference.sql`: **Spatial attention mechanism**
- `sql/procedures/08_SpatialProjection.sql`: Distance-based projection
- `sql/procedures/07_AdvancedInference.sql`: Multi-resolution, cognitive activation

### SQL CLR Functions
- `src/SqlClr/VectorOperations.cs`: Vector math (dot, cosine, norm)
- `src/SqlClr/SpatialOperations.cs`: Geometric operations (convex hull, overlap)

## Next: What Needs Refactoring

The dimension bucket architecture (Weights_768, Weights_1536, etc.) was WRONG.

Need to:
1. **Delete dimension bucket entities/configurations**
2. **Create EF Core entities matching ACTUAL schema** (GEOMETRY-based)
3. **Build repositories for spatial weight operations**
4. **Refactor services to use spatial inference**
5. **Implement proper enterprise patterns** (interfaces, DI, generics)
