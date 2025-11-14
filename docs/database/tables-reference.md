# Tables Reference

**Status**: Published
**Last Updated**: 2025-11-13
**Total Tables**: 99

Comprehensive reference for all database tables in Hartonomous.

## Table Categories

- [Core Atomic Storage](#core-atomic-storage) (5 tables)
- [Model Management](#model-management) (6 tables)
- [Provenance & Graph](#provenance--graph) (8 tables)
- [Billing](#billing) (5 tables)
- [OODA Loop & Autonomy](#ooda-loop--autonomy) (4 tables)
- [Inference](#inference) (6 tables)
- [Reference Tables](#reference-tables) (10 tables)
- [Ingestion](#ingestion) (5 tables)
- [Service Broker](#service-broker) (8 queues)
- [Supporting Tables](#supporting-tables) (42 tables)

---

## Core Atomic Storage

### dbo.Atoms

**Purpose**: Universal atomic content storage - the foundation of the "Periodic Table of Knowledge"

**Schema**:
```sql
CREATE TABLE dbo.Atoms (
    AtomId BIGINT IDENTITY PRIMARY KEY,
    ContentHash BINARY(32) NOT NULL,  -- SHA-256 for deduplication
    Modality NVARCHAR(64) NOT NULL,   -- text, image, audio, model, etc.
    Subtype NVARCHAR(128) NULL,        -- rgb-pixel, pcm-sample, float32-weight

    -- Atomic payload (SMALL - typically 1-64 bytes)
    AtomicValue VARBINARY(64) NULL,     -- Raw bytes
    CanonicalText NVARCHAR(256) NULL,   -- Text representation

    -- Legacy fields (backward compatibility during migration)
    Content NVARCHAR(MAX) NULL,
    ContentType NVARCHAR(128) NULL,
    PayloadLocator NVARCHAR(1024) NULL,
    ComponentStream VARBINARY(MAX) NULL,  -- Deprecated

    -- Metadata
    Metadata JSON NULL,
    CreatedAt DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
    UpdatedAt DATETIME2 NULL,

    -- Management
    IsActive BIT NOT NULL DEFAULT 1,
    IsDeleted BIT NOT NULL DEFAULT 0,
    TenantId INT NOT NULL DEFAULT 0,
    ReferenceCount BIGINT NOT NULL DEFAULT 0,  -- CRITICAL for garbage collection

    -- Spatial indexing
    SpatialKey GEOMETRY NULL,           -- Multi-dimensional coordinates
    SpatialGeography GEOGRAPHY NULL,    -- True geospatial (lat/lon)

    CONSTRAINT UQ_Atoms_ContentHash UNIQUE (ContentHash),  -- DEDUPLICATION ENFORCEMENT
    INDEX IX_Atoms_Modality_Subtype (Modality, Subtype),
    INDEX IX_Atoms_References (ReferenceCount DESC),
    INDEX IX_Atoms_TenantActive (TenantId, IsActive, IsDeleted)
);
```

**Critical Constraints**:
- **UQ_Atoms_ContentHash**: Enforces deduplication - same content = single atom
- **ReferenceCount**: Tracks how many AtomRelations reference this atom (garbage collection)

**Current Data**: 99 rows (as of 2025-11-13)

**Example Queries**:
```sql
-- Find most-reused atoms
SELECT TOP 10 AtomId, Modality, Subtype, CanonicalText, ReferenceCount
FROM dbo.Atoms
ORDER BY ReferenceCount DESC;

-- Find orphaned atoms (candidates for cleanup)
SELECT COUNT(*) FROM dbo.Atoms
WHERE ReferenceCount = 0 AND CreatedAt < DATEADD(DAY, -7, GETUTCDATE());

-- Deduplication stats by modality
SELECT Modality, COUNT(*) AS TotalAtoms, AVG(ReferenceCount) AS AvgReuse
FROM dbo.Atoms
GROUP BY Modality;
```

---

### dbo.AtomRelations

**Purpose**: Relationships between atoms with rich metadata (spatial coords, importance, confidence)

**Schema**:
```sql
CREATE TABLE dbo.AtomRelations (
    AtomRelationId BIGINT IDENTITY PRIMARY KEY,
    SourceAtomId BIGINT NOT NULL,        -- Parent atom
    TargetAtomId BIGINT NOT NULL,        -- Child/component atom
    RelationType NVARCHAR(128) NOT NULL, -- embedding_dimension, pixel, sample, weight
    SequenceIndex INT NULL,              -- Ordered position (0-1997 for embeddings)

    -- Semantic weights
    Weight REAL NULL,                    -- Relationship strength
    Importance REAL NULL,                -- Saliency/significance
    Confidence REAL NULL,                -- Certainty/quality

    -- Spatial coordinates (5D)
    SpatialBucket BIGINT NULL,           -- O(1) coarse filtering
    SpatialBucketX INT NULL,
    SpatialBucketY INT NULL,
    SpatialBucketZ INT NULL,
    CoordX FLOAT NULL,
    CoordY FLOAT NULL,
    CoordZ FLOAT NULL,
    CoordT FLOAT NULL,                   -- Time dimension
    CoordW FLOAT NULL,                   -- 5th dimension
    SpatialExpression GEOMETRY NULL,     -- Full spatial representation

    Metadata JSON NULL,
    TenantId INT NOT NULL DEFAULT 0,
    CreatedAt DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),

    -- Temporal versioning
    ValidFrom DATETIME2 GENERATED ALWAYS AS ROW START NOT NULL,
    ValidTo DATETIME2 GENERATED ALWAYS AS ROW END NOT NULL,
    PERIOD FOR SYSTEM_TIME (ValidFrom, ValidTo),

    FOREIGN KEY (SourceAtomId) REFERENCES dbo.Atoms(AtomId),
    FOREIGN KEY (TargetAtomId) REFERENCES dbo.Atoms(AtomId),

    INDEX IX_AtomRelations_SourceTarget (SourceAtomId, TargetAtomId),
    INDEX IX_AtomRelations_TargetSource (TargetAtomId, SourceAtomId),
    INDEX IX_AtomRelations_RelationType (RelationType),
    INDEX IX_AtomRelations_SequenceIndex (SourceAtomId, SequenceIndex),
    INDEX IX_AtomRelations_SpatialBucket (SpatialBucket) INCLUDE (SourceAtomId, TargetAtomId, CoordX, CoordY, CoordZ),
    INDEX IX_AtomRelations_Tenant (TenantId, RelationType)
)
WITH (SYSTEM_VERSIONING = ON (HISTORY_TABLE = dbo.AtomRelations_History));
```

**Key Features**:
- **System-Versioned**: Full temporal history, point-in-time queries with `FOR SYSTEM_TIME AS OF`
- **Spatial Coordinates**: 5D coordinates (CoordX/Y/Z/T/W) for trilateration
- **Semantic Weights**: Weight (relationship strength), Importance (saliency), Confidence (certainty)

**Performance Indexes**:
- 9 total indexes optimized for reconstruction, spatial search, temporal queries

**Example Queries**:
```sql
-- Reconstruct vector from atomic dimensions
SELECT ar.SequenceIndex, dbo.clr_BinaryToFloat(a.AtomicValue) AS Value
FROM dbo.AtomRelations ar
INNER JOIN dbo.Atoms a ON a.AtomId = ar.TargetAtomId
WHERE ar.SourceAtomId = @embeddingId
  AND ar.RelationType = 'embedding_dimension'
ORDER BY ar.SequenceIndex;

-- Point-in-time audit query
SELECT * FROM dbo.AtomRelations
FOR SYSTEM_TIME AS OF '2025-01-01 00:00:00'
WHERE SourceAtomId = 42;

-- Spatial range query
SELECT * FROM dbo.AtomRelations
WHERE SpatialBucket = @bucket
  AND CoordX BETWEEN @xMin AND @xMax
  AND CoordY BETWEEN @yMin AND @yMax;
```

---

### dbo.AtomEmbeddings

**Purpose**: Vector embeddings with spatial projection for O(log n) search

**Schema**:
```sql
CREATE TABLE dbo.AtomEmbeddings (
    AtomEmbeddingId BIGINT IDENTITY PRIMARY KEY,
    AtomId BIGINT NOT NULL,               -- References parent atom
    EmbeddingVector VECTOR(1998) NULL,    -- SQL Server 2025 native VECTOR type
    Dimension INT NOT NULL DEFAULT 1998,

    -- Spatial indexing
    SpatialGeometry GEOMETRY NULL,        -- 3D projection via PCA/t-SNE
    SpatialBucket BIGINT NULL,            -- LSH bucket for O(1) filtering

    Model NVARCHAR(256) NULL,
    Metadata JSON NULL,
    CreatedAt DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
    TenantId INT NOT NULL DEFAULT 0,

    FOREIGN KEY (AtomId) REFERENCES dbo.Atoms(AtomId),

    INDEX IX_AtomEmbeddings_Atom (AtomId),
    INDEX IX_AtomEmbeddings_Model (Model),
    INDEX IX_AtomEmbeddings_SpatialBucket (SpatialBucket)
);

-- Spatial indexes for approximate KNN
CREATE SPATIAL INDEX SIX_AtomEmbeddings_Spatial
ON dbo.AtomEmbeddings (SpatialGeometry)
WITH (GRIDS = (MEDIUM, MEDIUM, MEDIUM, MEDIUM));

CREATE SPATIAL INDEX SIX_AtomEmbeddings_SpatialCoarse
ON dbo.AtomEmbeddings (SpatialGeometry)
WITH (GRIDS = (LOW, LOW, LOW, LOW));
```

**Migration Status**:
- Phase 2 READY: Will decompose EmbeddingVector → 1998 atomic dimensions in AtomRelations
- After migration: 95% storage reduction, 99.9975% deduplication

**Performance**:
- Approximate KNN (1M embeddings): 10-50ms with spatial index vs 500-1000ms brute force

**Example Queries**:
```sql
-- Approximate KNN search
DECLARE @queryVector GEOMETRY = dbo.clr_TrilaterationProjection(@vectorJson);
SELECT TOP 10
    AtomEmbeddingId,
    SpatialGeometry.STDistance(@queryVector) AS Distance
FROM dbo.AtomEmbeddings WITH (INDEX(SIX_AtomEmbeddings_Spatial))
WHERE SpatialGeometry.STDistance(@queryVector) < @radius
ORDER BY Distance;
```

---

### dbo.AtomsLOB

**Purpose**: Large object separation (Phase 3 migration - Content/Metadata/ComponentStream moved here)

**Schema**:
```sql
CREATE TABLE dbo.AtomsLOB (
    AtomId BIGINT PRIMARY KEY,           -- 1:1 with Atoms
    Content NVARCHAR(MAX) NULL,
    Metadata NVARCHAR(MAX) NULL,
    ComponentStream VARBINARY(MAX) NULL,
    PayloadLocator NVARCHAR(1024) NULL,
    FOREIGN KEY (AtomId) REFERENCES dbo.Atoms(AtomId)
);
```

**Purpose**: Separate LOB columns to optimize Atoms table row size (8KB page limit)

**Usage**: After Phase 3 migration, always join AtomsLOB for Content/Metadata:
```sql
SELECT a.AtomId, a.CanonicalText, lob.Content, lob.Metadata
FROM dbo.Atoms a
LEFT JOIN dbo.AtomsLOB lob ON lob.AtomId = a.AtomId
WHERE a.AtomId = @id;
```

---

## Model Management

### dbo.Models

**Purpose**: AI model metadata registry

**Schema**:
```sql
CREATE TABLE dbo.Models (
    ModelId INT IDENTITY PRIMARY KEY,
    ModelName NVARCHAR(256) NOT NULL UNIQUE,
    ModelType NVARCHAR(128) NOT NULL,    -- llama4, qwen3, mistral, gpt-4
    Architecture NVARCHAR(128) NULL,     -- transformer, diffusion, etc.
    ParameterCount BIGINT NULL,
    QuantizationType NVARCHAR(50) NULL,  -- int8, int4, fp16, fp32
    Metadata JSON NULL,
    CreatedAt DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
    IsActive BIT NOT NULL DEFAULT 1
);
```

---

### dbo.ModelLayers

**Purpose**: Layer-level model architecture

**Schema**:
```sql
CREATE TABLE dbo.ModelLayers (
    LayerId BIGINT IDENTITY PRIMARY KEY,
    ModelId INT NOT NULL,
    LayerIdx INT NOT NULL,
    LayerType NVARCHAR(128) NOT NULL,    -- attention, feedforward, embedding
    TensorShape NVARCHAR(256) NULL,      -- [4096, 4096]
    ParameterCount BIGINT NULL,
    SpatialWeightDistributionHash BINARY(32) NULL,  -- For deduplication detection
    Metadata JSON NULL,
    FOREIGN KEY (ModelId) REFERENCES dbo.Models(ModelId)
);
```

---

### dbo.TensorAtoms

**Purpose**: Reusable tensor slices with spatial signatures

**Schema**:
```sql
CREATE TABLE dbo.TensorAtoms (
    TensorAtomId BIGINT IDENTITY PRIMARY KEY,
    AtomId BIGINT NOT NULL,
    ModelId INT NULL,
    LayerId BIGINT NULL,
    AtomType NVARCHAR(128) NOT NULL,     -- weight, bias, activation
    SpatialSignature GEOMETRY NULL,      -- (layer, row, col) position
    Metadata JSON NULL,
    FOREIGN KEY (AtomId) REFERENCES dbo.Atoms(AtomId),
    FOREIGN KEY (ModelId) REFERENCES dbo.Models(ModelId),
    FOREIGN KEY (LayerId) REFERENCES dbo.ModelLayers(LayerId)
);
```

---

## Provenance & Graph

### provenance.GenerationStreams

**Purpose**: Track lineage of generated content

**Schema**:
```sql
CREATE TABLE provenance.GenerationStreams (
    StreamId UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    ParentStreamId UNIQUEIDENTIFIER NULL,
    StreamType NVARCHAR(128) NOT NULL,   -- inference, ingestion, generation
    AtomicStreamValue dbo.AtomicStream NULL,  -- UDT for provenance segments
    CreatedAt DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
    FOREIGN KEY (ParentStreamId) REFERENCES provenance.GenerationStreams(StreamId)
);
```

---

### graph.AtomGraphNodes

**Purpose**: SQL Graph nodes for atom relationships

**Schema**:
```sql
CREATE TABLE graph.AtomGraphNodes (
    $node_id NVARCHAR(1000) PRIMARY KEY,
    AtomId BIGINT NOT NULL,
    NodeType NVARCHAR(128) NOT NULL,
    Metadata JSON NULL,
    FOREIGN KEY (AtomId) REFERENCES dbo.Atoms(AtomId)
) AS NODE;
```

---

### graph.AtomGraphEdges

**Purpose**: SQL Graph edges for provenance

**Schema**:
```sql
CREATE TABLE graph.AtomGraphEdges (
    $edge_id NVARCHAR(1000) PRIMARY KEY,
    RelationType NVARCHAR(128) NOT NULL,  -- DerivedFrom, ComponentOf, SimilarTo
    Weight REAL NULL,
    Metadata JSON NULL
) AS EDGE;
```

**Query Example**:
```sql
-- Query provenance chain using MATCH
SELECT atom.AtomId, edge.RelationType, derived.AtomId AS DerivedFromAtomId
FROM graph.AtomGraphNodes AS atom,
     graph.AtomGraphEdges FOR PATH AS edge,
     graph.AtomGraphNodes FOR PATH AS derived
WHERE MATCH(atom-(edge)->derived)
  AND atom.AtomId = @startAtomId;
```

---

## Billing

### dbo.BillingUsageLedger_InMemory

**Purpose**: High-throughput usage tracking with Hekaton In-Memory OLTP

**Schema**:
```sql
CREATE TABLE dbo.BillingUsageLedger_InMemory (
    UsageId BIGINT IDENTITY PRIMARY KEY NONCLUSTERED,
    TenantId INT NOT NULL,
    OperationType NVARCHAR(50) NOT NULL,
    Quantity INT NOT NULL,
    Timestamp DATETIME2 NOT NULL,
    INDEX IX_Tenant_Time NONCLUSTERED HASH (TenantId) WITH (BUCKET_COUNT = 1000000)
)
WITH (MEMORY_OPTIMIZED = ON, DURABILITY = SCHEMA_AND_DATA);
```

**Performance**: <1ms inserts (lock-free, in-memory)

**Example**:
```sql
-- High-frequency usage tracking
EXEC Billing.InsertUsageRecord_Native
    @TenantId = 123,
    @OperationType = 'embedding_generation',
    @Quantity = 1000,
    @Timestamp = SYSUTCDATETIME();
```

---

## OODA Loop & Autonomy

### dbo.AutonomousImprovementHistory

**Purpose**: Log of OODA loop actions and outcomes

**Schema**:
```sql
CREATE TABLE dbo.AutonomousImprovementHistory (
    ImprovementId BIGINT IDENTITY PRIMARY KEY,
    AnalysisId UNIQUEIDENTIFIER NOT NULL,
    HypothesisType NVARCHAR(128) NOT NULL,
    ActionTaken NVARCHAR(MAX) NULL,
    OutcomeClassification NVARCHAR(50) NULL,  -- HighSuccess, Success, Regressed, Failed
    LatencyImprovementPercent FLOAT NULL,
    ThroughputChangePercent FLOAT NULL,
    ExecutedAt DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
    MeasuredAt DATETIME2 NULL,
    SuccessScore FLOAT NULL,                  -- For model fine-tuning feedback
    Metadata JSON NULL
);
```

**Use Case**: Track autonomous improvements, feed results to sp_UpdateModelWeightsFromFeedback

**Example Query**:
```sql
-- Recent successful improvements
SELECT TOP 10 * FROM dbo.AutonomousImprovementHistory
WHERE OutcomeClassification IN ('HighSuccess', 'Success')
ORDER BY ExecutedAt DESC;
```

---

## Inference

### dbo.InferenceRequests

**Purpose**: Track inference operations for OODA loop analysis

**Schema**:
```sql
CREATE TABLE dbo.InferenceRequests (
    InferenceId BIGINT IDENTITY PRIMARY KEY,
    RequestTimestamp DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
    CompletedTimestamp DATETIME2 NULL,
    TotalDurationMs INT NULL,
    Status NVARCHAR(50) NOT NULL,
    ModelsUsed NVARCHAR(MAX) NULL,  -- JSON array
    InputData NVARCHAR(MAX) NULL,
    OutputData NVARCHAR(MAX) NULL,
    TenantId INT NOT NULL
);
```

**Use Case**: sp_Analyze queries recent inferences, detects anomalies

---

## Reference Tables

### ref.Status

**Purpose**: Enum values for Status columns (avoids magic strings)

**Schema**:
```sql
CREATE TABLE ref.Status (
    StatusId INT IDENTITY PRIMARY KEY,
    StatusCode NVARCHAR(50) NOT NULL UNIQUE,
    Description NVARCHAR(256) NULL,
    IsActive BIT NOT NULL DEFAULT 1
);
```

**Seeded Values**: PENDING, RUNNING, COMPLETED, FAILED, CANCELLED, PAUSED, RETRYING, TIMEOUT, ABORTED, UNKNOWN

**Benefits**: Referential integrity, consistent status codes

---

## Service Broker Queues

### dbo.AnalyzeQueue
### dbo.HypothesizeQueue
### dbo.ActQueue
### dbo.LearnQueue

**Purpose**: Service Broker queues for OODA loop message passing

**Schema**:
```sql
CREATE QUEUE dbo.AnalyzeQueue
WITH STATUS = ON,
     ACTIVATION (
         STATUS = ON,
         PROCEDURE_NAME = dbo.sp_Analyze,
         MAX_QUEUE_READERS = 1,
         EXECUTE AS SELF
     );
```

**Monitoring**:
```sql
-- Check queue depth
SELECT qi.conversation_handle, qi.message_type_name, qi.queuing_order
FROM dbo.AnalyzeQueue qi WITH (NOLOCK)
ORDER BY qi.queuing_order;
```

---

## Table Statistics

**Total Tables**: 99
- Core atomic: 5
- Model management: 6
- Provenance: 8
- Billing: 5
- OODA Loop: 4
- Inference: 6
- Reference: 10
- Ingestion: 5
- Service Broker: 8 queues
- Supporting: 42 others

**System-Versioned Tables**: 1 (AtomRelations)
**In-Memory OLTP Tables**: 1 (BillingUsageLedger_InMemory)
**SQL Graph Tables**: 2 (AS NODE, AS EDGE)
**Temporal Retention**: 30 days (default)

---

## Related Documentation

- [Procedures Reference](procedures-reference.md) - Procedures operating on these tables
- [CLR Reference](clr-reference.md) - CLR functions for atomic operations
- [Database Schema Guide](../development/database-schema.md) - Complete schema documentation
- [Architecture Philosophy](../architecture/PHILOSOPHY.md) - WHY behind table design

---

## Version History

| Version | Date | Changes |
|---------|------|---------|
| 1.0 | 2025-11-13 | Initial publication - documented 99 tables |
