# Database Schema Reference

**Complete SQL Server Database Structure**

## Schema Organization

Hartonomous uses **4 logical schemas** to organize 93 tables:

| Schema | Purpose | Table Count |
|--------|---------|-------------|
| **dbo** | Core application tables | 75 |
| **provenance** | Audit trail and lineage | 8 |
| **graph** | Graph-based queries | 2 |
| **ref** | Reference/lookup data | 3 |

## Core Tables (dbo Schema)

### Atom System (The Foundation)

#### dbo.Atom
**Purpose**: Universal atomic storage - every piece of data atomizes to this table

**Schema**:
```sql
CREATE TABLE dbo.Atom (
    AtomId BIGINT IDENTITY(1,1) PRIMARY KEY,
    TenantId INT NOT NULL,                    -- Multi-tenancy isolation
    ContentHash BINARY(32) NOT NULL,          -- SHA-256 for CAS deduplication
    AtomicValue VARBINARY(64) NOT NULL,       -- Maximum 64 bytes (hard constraint)
    CanonicalText NVARCHAR(MAX) NULL,         -- Overflow/human-readable representation
    Modality VARCHAR(50) NOT NULL,            -- 'text', 'code', 'image', 'model', 'audio', 'database', 'git'
    Subtype VARCHAR(50) NULL,                 -- 'token', 'pixel', 'float32-weight', 'csharp-class'
    ContentType VARCHAR(255) NULL,            -- MIME type from source file
    Metadata NVARCHAR(MAX) NULL,              -- JSON metadata (SQL Server native JSON)
    ReferenceCount BIGINT NOT NULL DEFAULT 1, -- CAS deduplication counter
    ConceptId BIGINT NULL,                    -- FK to Concept (semantic clustering)
    CreatedAt DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
    LastAccessedUtc DATETIME2 NULL,
    UNIQUE (TenantId, ContentHash),           -- CAS constraint
    FOREIGN KEY (ConceptId) REFERENCES dbo.Concept(ConceptId)
);

-- Temporal table for full audit trail
ALTER TABLE dbo.Atom
ADD PERIOD FOR SYSTEM_TIME (SysStartTime, SysEndTime);

ALTER TABLE dbo.Atom
SET (SYSTEM_VERSIONING = ON (HISTORY_TABLE = dbo.AtomHistory));
```

**Key Indices**:
```sql
-- Primary key (clustered)
CREATE CLUSTERED INDEX PK_Atom ON dbo.Atom(AtomId);

-- CAS deduplication lookup
CREATE UNIQUE INDEX IX_Atom_TenantHash ON dbo.Atom(TenantId, ContentHash);

-- Modality filtering
CREATE INDEX IX_Atom_Modality ON dbo.Atom(Modality, Subtype) INCLUDE (CanonicalText);

-- Concept clustering
CREATE INDEX IX_Atom_Concept ON dbo.Atom(ConceptId) WHERE ConceptId IS NOT NULL;

-- Dead atom detection
CREATE INDEX IX_Atom_DeadAtoms ON dbo.Atom(ReferenceCount, LastAccessedUtc)
WHERE ReferenceCount = 0;
```

**Constraints**:
- AtomicValue: Maximum 64 bytes enforced by schema
- Overflow handling: Content > 64 bytes stored in CanonicalText
- Metadata: JSON validated on insert (SQL Server 2025 JSON support)

#### dbo.AtomComposition
**Purpose**: Structural relationships (parent-child hierarchy)

**Schema**:
```sql
CREATE TABLE dbo.AtomComposition (
    AtomCompositionId BIGINT IDENTITY PRIMARY KEY,
    ParentAtomHash BINARY(32) NOT NULL,       -- FK to Atom.ContentHash
    ComponentAtomHash BINARY(32) NOT NULL,    -- FK to Atom.ContentHash
    SequenceIndex BIGINT NOT NULL,            -- Ordering within parent
    Position GEOMETRY NULL,                   -- Spatial position (X, Y, Z, M)
    CreatedAt DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
    FOREIGN KEY (ParentAtomHash) REFERENCES dbo.Atom(ContentHash) ON DELETE CASCADE,
    FOREIGN KEY (ComponentAtomHash) REFERENCES dbo.Atom(ContentHash)
);
```

**Spatial Position Encoding**:
- **X**: Sequence/horizontal position (column index, pixel X)
- **Y**: Value/vertical position (row index, pixel Y, magnitude)
- **Z**: Layer/depth (namespace=1, class=2, method=3)
- **M**: Hilbert curve index (cache locality, Columnstore compression)

**Indices**:
```sql
-- Parent lookup
CREATE INDEX IX_AtomComposition_Parent ON dbo.AtomComposition(ParentAtomHash)
INCLUDE (ComponentAtomHash, SequenceIndex);

-- Spatial index on Position
CREATE SPATIAL INDEX SIX_AtomComposition_Position ON dbo.AtomComposition(Position)
WITH (BOUNDING_BOX = (0, 0, 10000, 10000));

-- Hilbert-ordered Columnstore (for analytics)
CREATE COLUMNSTORE INDEX CIX_AtomComposition ON dbo.AtomComposition
ORDER (Position.M);  -- Pre-sorted by Hilbert value
```

#### dbo.AtomRelation
**Purpose**: Semantic relationships between atoms (not parent-child)

**Schema**:
```sql
CREATE TABLE dbo.AtomRelation (
    AtomRelationId BIGINT IDENTITY PRIMARY KEY,
    SourceAtomId BIGINT NOT NULL,
    TargetAtomId BIGINT NOT NULL,
    RelationType NVARCHAR(128) NOT NULL,      -- 'derives_from', 'similar_to', 'contradicts', 'influences'
    Importance DECIMAL(5,4) NULL,             -- 0.0-1.0
    Confidence DECIMAL(5,4) NULL,             -- 0.0-1.0
    Weight REAL NULL,                         -- Relationship strength
    SpatialExpression GEOMETRY NULL,          -- Geometric relationship representation
    CoordX FLOAT NULL,                        -- 5D coordinates
    CoordY FLOAT NULL,
    CoordZ FLOAT NULL,
    CoordT FLOAT NULL,                        -- Temporal dimension
    CoordW FLOAT NULL,                        -- Weight/measure dimension
    CreatedAt DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
    FOREIGN KEY (SourceAtomId) REFERENCES dbo.Atom(AtomId) ON DELETE CASCADE,
    FOREIGN KEY (TargetAtomId) REFERENCES dbo.Atom(AtomId)
);

-- Temporal versioning
ALTER TABLE dbo.AtomRelation
SET (SYSTEM_VERSIONING = ON (HISTORY_TABLE = dbo.AtomRelations_History));
```

**Indices**:
```sql
CREATE INDEX IX_AtomRelation_Source ON dbo.AtomRelation(SourceAtomId, RelationType);
CREATE INDEX IX_AtomRelation_Target ON dbo.AtomRelation(TargetAtomId, RelationType);
CREATE SPATIAL INDEX SIX_AtomRelation_Spatial ON dbo.AtomRelation(SpatialExpression);
```

### Embedding System

#### dbo.AtomEmbedding
**Purpose**: Dual spatial index for semantic search

**Schema**:
```sql
CREATE TABLE dbo.AtomEmbedding (
    AtomEmbeddingId BIGINT IDENTITY PRIMARY KEY,
    SourceAtomId BIGINT NOT NULL,
    ModelId INT NOT NULL,
    SpatialKey GEOMETRY NOT NULL,             -- 3D projection + Hilbert in M dimension
    EmbeddingVector VECTOR(1998) NULL,        -- SQL Server 2025 native VECTOR type
    SpatialBucketX AS CAST(FLOOR((SpatialKey.STX + 100) / 10) AS INT) PERSISTED,
    SpatialBucketY AS CAST(FLOOR((SpatialKey.STY + 100) / 10) AS INT) PERSISTED,
    SpatialBucketZ AS CAST(FLOOR((SpatialKey.STZ + 100) / 10) AS INT) PERSISTED,
    HilbertValue AS SpatialKey.M PERSISTED,
    CreatedAt DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
    UNIQUE (SourceAtomId, ModelId),
    FOREIGN KEY (SourceAtomId) REFERENCES dbo.Atom(AtomId) ON DELETE CASCADE,
    FOREIGN KEY (ModelId) REFERENCES dbo.Model(ModelId)
);
```

**Indices**:
```sql
-- Spatial index for KNN queries
CREATE SPATIAL INDEX SIX_AtomEmbedding_Semantic ON dbo.AtomEmbedding(SpatialKey)
WITH (BOUNDING_BOX = (-100, -100, 100, 100), GRIDS = (HIGH, HIGH, HIGH, HIGH));

-- Vector index (SQL Server 2025 DiskANN)
CREATE VECTOR INDEX VIX_AtomEmbedding_Vector ON dbo.AtomEmbedding(EmbeddingVector)
WITH (METRIC = 'cosine', LISTS = 1000);

-- Spatial bucket grid
CREATE INDEX IX_AtomEmbedding_Buckets ON dbo.AtomEmbedding(SpatialBucketX, SpatialBucketY, SpatialBucketZ)
INCLUDE (SpatialKey);

-- Hilbert-ordered index
CREATE INDEX IX_AtomEmbedding_Hilbert ON dbo.AtomEmbedding(HilbertValue)
INCLUDE (SourceAtomId, SpatialKey);
```

#### dbo.AtomEmbeddingComponent
**Purpose**: Per-dimension storage for feature analysis

**Schema**:
```sql
CREATE TABLE dbo.AtomEmbeddingComponent (
    ComponentId BIGINT IDENTITY PRIMARY KEY,
    SourceAtomId BIGINT NOT NULL,
    DimensionIndex SMALLINT NOT NULL,         -- 0-1535 for 1536D embeddings
    DimensionAtomId BIGINT NOT NULL,          -- FK to Atom (the float32 value)
    ModelId INT NOT NULL,
    SpatialKey GEOMETRY NOT NULL,             -- Point(value, index, modelId)
    UNIQUE (SourceAtomId, DimensionIndex, ModelId),
    FOREIGN KEY (SourceAtomId) REFERENCES dbo.Atom(AtomId) ON DELETE CASCADE,
    FOREIGN KEY (DimensionAtomId) REFERENCES dbo.Atom(AtomId),
    FOREIGN KEY (ModelId) REFERENCES dbo.Model(ModelId)
);

-- Dimension space spatial index
CREATE SPATIAL INDEX SIX_AtomEmbeddingComponent_Dim ON dbo.AtomEmbeddingComponent(SpatialKey)
WITH (BOUNDING_BOX = (-10, 0, 10, 1536));

-- Non-zero dimension filter (70% sparse typical)
CREATE INDEX IX_Component_NonZero ON dbo.AtomEmbeddingComponent(DimensionIndex, DimensionAtomId)
WHERE ABS(CAST(DimensionAtomId AS REAL)) > 0.001;
```

### Model System

#### dbo.Model
**Purpose**: AI model metadata and configuration

**Schema**:
```sql
CREATE TABLE dbo.Model (
    ModelId INT IDENTITY PRIMARY KEY,
    ModelName NVARCHAR(255) NOT NULL UNIQUE,
    ModelType NVARCHAR(100) NOT NULL,         -- 'transformer', 'cnn', 'rnn', 'diffusion'
    Architecture NVARCHAR(500) NULL,          -- 'llama-3.2-7b', 'gpt-4', 'stable-diffusion-xl'
    ParameterCount BIGINT NULL,               -- 7000000000 for 7B model
    ConfigurationJson NVARCHAR(MAX) NULL,     -- Full model config
    SerializedModel VARBINARY(MAX) NULL,      -- Optional: Full binary model
    UsageCount BIGINT NOT NULL DEFAULT 0,
    AverageInferenceMs INT NULL,
    LastUsed DATETIME2 NULL,
    CreatedAt DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME()
);
```

#### dbo.ModelLayer
**Purpose**: Per-layer metadata for neural networks

**Schema**:
```sql
CREATE TABLE dbo.ModelLayer (
    ModelLayerId INT IDENTITY PRIMARY KEY,
    ModelId INT NOT NULL,
    LayerIndex INT NOT NULL,
    LayerName NVARCHAR(255) NOT NULL,
    LayerType NVARCHAR(100) NOT NULL,         -- 'attention', 'feed-forward', 'embedding', 'normalization'
    TensorShape NVARCHAR(500) NULL,           -- '[4096, 4096]', '[32, 128, 128]'
    WeightsGeometry GEOMETRY NULL,            -- Spatial representation of layer
    QuantizationType NVARCHAR(50) NULL,       -- 'fp32', 'fp16', 'int8', 'int4'
    QuantizationScale REAL NULL,
    CacheHitRate DECIMAL(5,4) NULL,
    AvgComputeTimeMs INT NULL,
    UNIQUE (ModelId, LayerIndex),
    FOREIGN KEY (ModelId) REFERENCES dbo.Model(ModelId) ON DELETE CASCADE
);
```

#### dbo.TensorAtomCoefficient
**Purpose**: Model weight storage (queryable tensor representation)

**Schema**:
```sql
CREATE TABLE dbo.TensorAtomCoefficient (
    TensorCoefficientId BIGINT IDENTITY PRIMARY KEY,
    ModelId INT NOT NULL,
    LayerIndex INT NOT NULL,
    PositionX INT NOT NULL,                   -- Matrix/tensor position
    PositionY INT NOT NULL,
    PositionZ INT NULL,                       -- For 3D+ tensors
    ValueAtomId BIGINT NOT NULL,              -- FK to Atom (float32 value)
    SpatialKey AS geometry::Point(PositionX, PositionY, ISNULL(PositionZ, 0)) PERSISTED,
    CreatedAt DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
    FOREIGN KEY (ModelId) REFERENCES dbo.Model(ModelId),
    FOREIGN KEY (ValueAtomId) REFERENCES dbo.Atom(AtomId)
);

-- Temporal versioning for model evolution
ALTER TABLE dbo.TensorAtomCoefficient
SET (SYSTEM_VERSIONING = ON (HISTORY_TABLE = dbo.TensorAtomCoefficients_History));

-- Columnstore index for analytics
CREATE COLUMNSTORE INDEX CIX_TensorAtomCoefficient ON dbo.TensorAtomCoefficient;

-- Spatial index for geometric queries
CREATE SPATIAL INDEX SIX_TensorAtomCoefficient ON dbo.TensorAtomCoefficient(SpatialKey)
WITH (BOUNDING_BOX = (0, 0, 10000, 10000));
```

### Concept System

#### dbo.Concept
**Purpose**: Semantic clustering and concept discovery

**Schema**:
```sql
CREATE TABLE dbo.Concept (
    ConceptId BIGINT IDENTITY PRIMARY KEY,
    ConceptName NVARCHAR(255) NOT NULL,
    Description NVARCHAR(MAX) NULL,
    CentroidVector VARBINARY(MAX) NULL,       -- Average embedding of concept
    CentroidSpatialKey HIERARCHYID NULL,      -- Hierarchical spatial key
    Domain GEOMETRY NULL,                     -- Voronoi cell boundary
    Radius FLOAT NULL,                        -- Concept domain radius
    ParentConceptId BIGINT NULL,              -- Hierarchical concepts
    AtomCount BIGINT NOT NULL DEFAULT 0,
    Confidence DECIMAL(5,4) NULL,
    CreatedAt DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
    FOREIGN KEY (ParentConceptId) REFERENCES dbo.Concept(ConceptId)
);

-- Spatial indices
CREATE SPATIAL INDEX SIX_Concept_Domain ON dbo.Concept(Domain);
CREATE SPATIAL INDEX SIX_Concept_Centroid ON dbo.Concept(CentroidSpatialKey);
```

### Ingestion System

#### dbo.IngestionJob
**Purpose**: Governed, resumable atomization jobs

**Schema**:
```sql
CREATE TABLE dbo.IngestionJob (
    JobId UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    TenantId INT NOT NULL,
    FileName NVARCHAR(500) NOT NULL,
    ContentType NVARCHAR(255) NULL,
    JobStatus NVARCHAR(50) NOT NULL DEFAULT 'Pending',  -- 'Pending', 'Processing', 'Completed', 'Failed', 'QuotaExceeded'
    AtomChunkSize INT NOT NULL DEFAULT 10000,
    CurrentAtomOffset BIGINT NOT NULL DEFAULT 0,
    TotalAtomsProcessed BIGINT NOT NULL DEFAULT 0,
    AtomQuota BIGINT NOT NULL DEFAULT 5000000000,  -- 5B atom default quota
    ErrorMessage NVARCHAR(MAX) NULL,
    CreatedAt DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
    StartedAt DATETIME2 NULL,
    CompletedAt DATETIME2 NULL
);

CREATE INDEX IX_IngestionJob_Status ON dbo.IngestionJob(JobStatus, CreatedAt);
CREATE INDEX IX_IngestionJob_Tenant ON dbo.IngestionJob(TenantId, JobStatus);
```

### OODA Loop System

#### dbo.LearningMetrics
**Purpose**: OODA Observe phase - store analysis results

**Schema**:
```sql
CREATE TABLE dbo.LearningMetrics (
    MetricId BIGINT IDENTITY PRIMARY KEY,
    TenantId INT NOT NULL,
    AnalysisType NVARCHAR(100) NOT NULL,      -- 'OODA_Observe', 'ConceptDiscovery', 'DriftDetection'
    ObservationsJson NVARCHAR(MAX) NOT NULL,  -- JSON observations
    AnomaliesDetected INT NOT NULL,
    CreatedAt DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME()
);
```

#### dbo.PendingActions
**Purpose**: OODA Orient phase - hypotheses awaiting execution

**Schema**:
```sql
CREATE TABLE dbo.PendingActions (
    ActionId BIGINT IDENTITY PRIMARY KEY,
    TenantId INT NOT NULL,
    ActionType NVARCHAR(100) NOT NULL,        -- 'CreateIndex', 'PruneDeadAtoms', 'RetrainConcept'
    TargetObject NVARCHAR(500) NOT NULL,
    ActionCommand NVARCHAR(MAX) NOT NULL,     -- T-SQL to execute
    ExpectedImprovement NVARCHAR(500) NULL,
    ApprovalScore DECIMAL(3,2) NOT NULL,      -- 0.00-1.00
    Status NVARCHAR(50) NOT NULL DEFAULT 'Pending',
    ExecutedAt DATETIME2 NULL,
    ErrorMessage NVARCHAR(MAX) NULL,
    CreatedAt DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME()
);
```

#### dbo.AutonomousImprovementHistory
**Purpose**: OODA Act phase - execution audit trail

**Schema**:
```sql
CREATE TABLE dbo.AutonomousImprovementHistory (
    ImprovementId BIGINT IDENTITY PRIMARY KEY,
    TenantId INT NOT NULL,
    ActionType NVARCHAR(100) NOT NULL,
    TargetObject NVARCHAR(500) NOT NULL,
    ActionCommand NVARCHAR(MAX) NOT NULL,
    Status NVARCHAR(50) NOT NULL,             -- 'Success', 'Failed'
    DurationMs INT NULL,
    ResultJson NVARCHAR(MAX) NULL,
    ActualImprovement DECIMAL(5,2) NULL,      -- Measured performance gain
    ErrorMessage NVARCHAR(MAX) NULL,
    CreatedAt DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME()
);
```

## Provenance Schema

### provenance.Concepts
**Purpose**: Extended concept tracking with discovery metadata

**Schema**:
```sql
CREATE TABLE provenance.Concepts (
    ConceptId BIGINT IDENTITY PRIMARY KEY,
    ConceptName NVARCHAR(255) NOT NULL,
    ConceptDomain GEOMETRY NULL,              -- Voronoi cell
    CentroidSpatialKey GEOMETRY NULL,         -- 3D centroid position
    DiscoveryMethod NVARCHAR(100) NULL,       -- 'DBSCAN', 'KMeans', 'Manual'
    CoherenceScore DECIMAL(5,4) NULL,         -- Intra-cluster similarity
    SeparationScore DECIMAL(5,4) NULL,        -- Inter-cluster distance
    CreatedAt DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME()
);

CREATE SPATIAL INDEX SIX_ProvenanceConcepts_Domain ON provenance.Concepts(ConceptDomain);
CREATE SPATIAL INDEX SIX_ProvenanceConcepts_Centroid ON provenance.Concepts(CentroidSpatialKey);
```

### provenance.AtomConcepts
**Purpose**: Atom-to-concept membership

**Schema**:
```sql
CREATE TABLE provenance.AtomConcepts (
    AtomId BIGINT NOT NULL,
    ConceptId BIGINT NOT NULL,
    MembershipScore DECIMAL(5,4) NULL,        -- 0.0-1.0
    AssignedAt DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
    PRIMARY KEY (AtomId, ConceptId),
    FOREIGN KEY (AtomId) REFERENCES dbo.Atom(AtomId) ON DELETE CASCADE,
    FOREIGN KEY (ConceptId) REFERENCES provenance.Concepts(ConceptId)
);
```

## In-Memory OLTP Tables

**Purpose**: Low-latency operations (billing, inference caching)

### dbo.BillingUsageLedger_InMemory
```sql
CREATE TABLE dbo.BillingUsageLedger_InMemory (
    LedgerId BIGINT IDENTITY PRIMARY KEY NONCLUSTERED,
    TenantId INT NOT NULL,
    OperationType NVARCHAR(100) NOT NULL,
    Quantity BIGINT NOT NULL,
    Timestamp DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
    INDEX IX_Tenant_Time NONCLUSTERED HASH (TenantId) WITH (BUCKET_COUNT = 1024)
)
WITH (MEMORY_OPTIMIZED = ON, DURABILITY = SCHEMA_AND_DATA);
```

### dbo.InferenceCache_InMemory
```sql
CREATE TABLE dbo.InferenceCache_InMemory (
    CacheKey BINARY(32) NOT NULL PRIMARY KEY NONCLUSTERED,
    OutputAtomId BIGINT NOT NULL,
    CreatedAt DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
    ExpiresAt DATETIME2 NOT NULL
)
WITH (MEMORY_OPTIMIZED = ON, DURABILITY = SCHEMA_ONLY);
```

## Key Stored Procedures

### Core Atomization
- `sp_AtomizeText_Governed` - Governed text tokenization with quota
- `sp_AtomizeImage_Governed` - Image patch/OCR/object atomization
- `sp_AtomizeModel_Governed` - Model weight atomization
- `sp_AtomizeCode` - AST-based code atomization

### OODA Loop
- `sp_Analyze` - Observe phase (anomaly detection)
- `sp_Hypothesize` - Orient phase (hypothesis generation)
- `sp_Act` - Act phase (execute approved actions)

### Search & Inference
- `sp_SemanticSearch` - Spatial KNN search
- `sp_HybridSearch` - BM25 + vector fusion
- `sp_CrossModalQuery` - Query across modalities
- `sp_RunInference` - Model inference execution
- `sp_MultiModelEnsemble` - Multi-model voting

### Concept Management
- `sp_ClusterConcepts` - DBSCAN clustering for concept discovery
- `sp_BuildConceptDomains` - Voronoi domain construction
- `sp_ComputeSemanticFeatures` - Feature extraction

---

**Document Version**: 2.0
**Last Updated**: January 2025
**Total Tables**: 93
**Total Procedures**: 77
**Total Functions**: 93
