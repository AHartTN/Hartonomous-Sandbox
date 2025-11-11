# Hartonomous Database Schema

**Comprehensive schema reference for SQL Server 2025 database.**

## Overview

Hartonomous uses SQL Server 2025 as the primary data substrate with advanced features:

- **Native VECTOR type** (`VECTOR(1998)`) for embeddings
- **GEOMETRY type** for spatial projections and R-tree indexing
- **System-versioned temporal tables** for weight history
- **SQL Graph tables** for provenance relationships
- **FILESTREAM** for large model weights
- **In-Memory OLTP** for billing ledger and inference cache
- **Service Broker** for OODA loop messaging

All tables defined in `sql/tables/`, configured via EF Core in `src/Hartonomous.Data/Configurations/`, and deployed by `scripts/deploy-database-unified.ps1`.

## Core Domain Tables

### dbo.Atoms

**Purpose**: Canonical multimodal data record. Single source of truth for all atomic content.

**Definition** (`sql/tables/dbo.Atoms.sql`):

```sql
CREATE TABLE dbo.Atoms (
    AtomId              BIGINT IDENTITY(1,1) PRIMARY KEY,
    Modality            NVARCHAR(64) NOT NULL,              -- text, image, audio, video, sensor, graph
    Subtype             NVARCHAR(64) NULL,
    SourceType          NVARCHAR(128) NULL,
    SourceUri           NVARCHAR(2048) NULL,
    ContentHash         BINARY(32) NOT NULL,                -- SHA-256 for deduplication
    ReferenceCount      INT NOT NULL DEFAULT 1,
    CanonicalText       NVARCHAR(MAX) NULL,
    Metadata            NVARCHAR(MAX) NULL,                 -- JSON
    Semantics           NVARCHAR(MAX) NULL,                 -- JSON
    TenantId            INT NOT NULL DEFAULT 0,
    IsDeleted           BIT NOT NULL DEFAULT 0,
    CreatedAt           DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
    UpdatedAt           DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
    DeletedAt           DATETIME2 NULL
);

CREATE UNIQUE INDEX UX_Atoms_ContentHash_TenantId ON dbo.Atoms(ContentHash, TenantId) WHERE IsDeleted = 0;
CREATE INDEX IX_Atoms_Modality_Subtype ON dbo.Atoms(Modality, Subtype);
```

**EF Core Configuration** (`src/Hartonomous.Data/Configurations/AtomConfiguration.cs`):

- Maps `Metadata` and `Semantics` as JSON columns
- Configures `ContentHash` as computed from content
- Enforces `SpatialKey` geometry column (optional)

**Key Features**:

- **Content-addressable**: SHA-256 deduplication via `ContentHash`
- **Multimodal**: Supports text, image, audio, video, sensor, graph modalities
- **Tenant isolation**: `TenantId` for multi-tenant deployments
- **Soft delete**: `IsDeleted` flag preserves history

### dbo.AtomEmbeddings

**Purpose**: Vector representations with spatial projection for R-tree indexing.

**Definition** (`sql/tables/dbo.AtomEmbeddings.sql`):

```sql
CREATE TABLE dbo.AtomEmbeddings (
    AtomEmbeddingId     BIGINT IDENTITY(1,1) PRIMARY KEY,
    AtomId              BIGINT NOT NULL,
    EmbeddingVector     VECTOR(1998) NOT NULL,              -- Native SQL Server 2025 vector type
    SpatialGeometry     GEOMETRY NOT NULL,                  -- 3D point cloud projection
    SpatialCoarse       GEOMETRY NOT NULL,                  -- Coarse-grained spatial index
    SpatialBucket       INT NOT NULL,
    SpatialBucketX      INT NULL,
    SpatialBucketY      INT NULL,
    SpatialBucketZ      INT NULL,
    ModelId             INT NULL,
    EmbeddingType       NVARCHAR(50) NOT NULL DEFAULT 'semantic',
    LastUpdated         DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
    CreatedAt           DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),

    CONSTRAINT FK_AtomEmbeddings_Atoms FOREIGN KEY (AtomId) REFERENCES dbo.Atoms(AtomId) ON DELETE CASCADE
);

CREATE INDEX IX_AtomEmbeddings_Atom ON dbo.AtomEmbeddings(AtomId);
CREATE INDEX IX_AtomEmbeddings_Bucket ON dbo.AtomEmbeddings(SpatialBucket);
CREATE SPATIAL INDEX IX_AtomEmbeddings_Spatial ON dbo.AtomEmbeddings(SpatialGeometry)
    WITH (GRIDS = (LEVEL_1 = MEDIUM, LEVEL_2 = MEDIUM, LEVEL_3 = MEDIUM, LEVEL_4 = MEDIUM));
CREATE SPATIAL INDEX IX_AtomEmbeddings_Coarse ON dbo.AtomEmbeddings(SpatialCoarse)
    WITH (GRIDS = (LEVEL_1 = LOW, LEVEL_2 = LOW, LEVEL_3 = LOW, LEVEL_4 = LOW));
```

**Spatial Indexes**:

- **R-tree spatial index** enables O(log n) nearest-neighbor search via bounding box queries
- **Dual granularity**: Fine-grained (`SpatialGeometry`) and coarse-grained (`SpatialCoarse`) for multi-stage filtering

**EF Core Configuration** (`AtomEmbeddingConfiguration.cs`):

- Maps `SpatialGeometry` as NetTopologySuite `Point` (3D)
- Configures `EmbeddingVector` as `VECTOR(1998)` via SQL type
- Relationship: `Atom` → many `AtomEmbeddings`

**Generation Paths**:

1. **C# EmbeddingService**: HTTP ingestion → Azure OpenAI → persist via EF Core
2. **CLR fn_ComputeEmbedding**: T-SQL embedding → CLR function → `INSERT` directly

### dbo.Models and dbo.ModelLayers

**Purpose**: Model metadata and layer structure for tensor decomposition.

**Definition** (`sql/tables/dbo.ModelStructure.sql`):

```sql
CREATE TABLE dbo.Models (
    ModelId             INT IDENTITY(1,1) PRIMARY KEY,
    ModelName           NVARCHAR(256) NOT NULL UNIQUE,
    ModelType           NVARCHAR(64) NOT NULL,              -- transformer, cnn, rnn, diffusion
    Framework           NVARCHAR(64) NOT NULL,              -- pytorch, tensorflow, onnx
    Version             NVARCHAR(64) NULL,
    ParameterCount      BIGINT NULL,
    Metadata            NVARCHAR(MAX) NULL,                 -- JSON
    CreatedAt           DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
    UpdatedAt           DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME()
);

CREATE TABLE dbo.ModelLayers (
    LayerId             BIGINT IDENTITY(1,1) PRIMARY KEY,
    ModelId             INT NOT NULL,
    LayerName           NVARCHAR(256) NOT NULL,
    LayerType           NVARCHAR(64) NOT NULL,              -- attention, feedforward, embedding, etc.
    LayerIndex          INT NOT NULL,
    InputShape          NVARCHAR(256) NULL,                 -- JSON array [batch, seq, dim]
    OutputShape         NVARCHAR(256) NULL,
    ParameterCount      BIGINT NULL,
    Configuration       NVARCHAR(MAX) NULL,                 -- JSON
    CreatedAt           DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),

    CONSTRAINT FK_ModelLayers_Models FOREIGN KEY (ModelId) REFERENCES dbo.Models(ModelId) ON DELETE CASCADE
);

CREATE INDEX IX_ModelLayers_Model ON dbo.ModelLayers(ModelId, LayerIndex);
```

**Relationships**:

- `Model` → many `ModelLayers` → many `TensorAtoms` → many `TensorAtomCoefficients`

### dbo.TensorAtomCoefficients (Temporal)

**Purpose**: Reusable tensor slices with geometry footprints and temporal history.

**Definition** (`sql/tables/TensorAtomCoefficients_Temporal.sql`):

```sql
CREATE TABLE dbo.TensorAtomCoefficients (
    TensorAtomCoefficientId BIGINT IDENTITY(1,1) PRIMARY KEY,
    TensorAtomId            BIGINT NOT NULL,
    ParentLayerId           BIGINT NOT NULL,
    TensorRole              NVARCHAR(128) NULL,             -- query, key, value, weight, bias
    Coefficient             REAL NOT NULL,
    ValidFrom               DATETIME2(7) GENERATED ALWAYS AS ROW START NOT NULL,
    ValidTo                 DATETIME2(7) GENERATED ALWAYS AS ROW END NOT NULL,
    PERIOD FOR SYSTEM_TIME (ValidFrom, ValidTo),

    CONSTRAINT FK_TensorAtomCoefficients_TensorAtom FOREIGN KEY (TensorAtomId) REFERENCES dbo.TensorAtoms(TensorAtomId),
    CONSTRAINT FK_TensorAtomCoefficients_Layer FOREIGN KEY (ParentLayerId) REFERENCES dbo.ModelLayers(LayerId)
)
WITH (SYSTEM_VERSIONING = ON (HISTORY_TABLE = dbo.TensorAtomCoefficients_History));

CREATE TABLE dbo.TensorAtomCoefficients_History (
    TensorAtomCoefficientId BIGINT NOT NULL,
    TensorAtomId            BIGINT NOT NULL,
    ParentLayerId           BIGINT NOT NULL,
    TensorRole              NVARCHAR(128) NULL,
    Coefficient             REAL NOT NULL,
    ValidFrom               DATETIME2(7) NOT NULL,
    ValidTo                 DATETIME2(7) NOT NULL,
    INDEX IX_TensorAtomCoefficients_History_Period NONCLUSTERED (ValidTo, ValidFrom)
);
```

**Temporal Queries**:

```sql
-- Point-in-time query
SELECT * FROM dbo.TensorAtomCoefficients
FOR SYSTEM_TIME AS OF '2025-11-01T10:00:00';

-- Time range query
SELECT * FROM dbo.TensorAtomCoefficients
FOR SYSTEM_TIME BETWEEN '2025-11-01' AND '2025-11-10';

-- Full history
SELECT * FROM dbo.TensorAtomCoefficients
FOR SYSTEM_TIME ALL;
```

**EF Core Configuration** (`TensorAtomCoefficientConfiguration.cs`):

- Maps temporal columns (`ValidFrom`, `ValidTo`)
- Configures history table relationship

**Use Cases**:

- Autonomous learning rollback (undo bad weight updates)
- Compliance auditing (who changed what when)
- Time-travel debugging (reproduce historical model states)

## Graph Tables

### graph.AtomGraphNodes

**Purpose**: SQL Server graph node table for atom provenance.

**Definition** (`sql/tables/graph.AtomGraphNodes.sql`):

```sql
CREATE TABLE graph.AtomGraphNodes (
    $node_id                NVARCHAR(1000) PRIMARY KEY,
    AtomId                  BIGINT NOT NULL,
    NodeType                NVARCHAR(128) NOT NULL,     -- atom, embedding, model, layer
    Metadata                NVARCHAR(MAX) NULL,
    CreatedAt               DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),

    CONSTRAINT FK_AtomGraphNodes_Atoms FOREIGN KEY (AtomId) REFERENCES dbo.Atoms(AtomId)
) AS NODE;
```

### graph.AtomGraphEdges

**Purpose**: SQL Server graph edge table for relationships.

**Definition** (`sql/tables/graph.AtomGraphEdges.sql`):

```sql
CREATE TABLE graph.AtomGraphEdges (
    $edge_id                NVARCHAR(1000) PRIMARY KEY,
    RelationshipType        NVARCHAR(128) NOT NULL,     -- DerivedFrom, ComponentOf, SimilarTo, GeneratedBy
    Weight                  REAL NULL,
    Metadata                NVARCHAR(MAX) NULL,
    CreatedAt               DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME()
) AS EDGE;
```

**Graph Queries**:

```sql
-- Find atoms derived from source atom
SELECT derived.AtomId, derived.Modality
FROM graph.AtomGraphNodes AS source,
     graph.AtomGraphEdges,
     graph.AtomGraphNodes AS derived
WHERE MATCH(source-(AtomGraphEdges)->derived)
  AND source.AtomId = @sourceAtomId
  AND AtomGraphEdges.RelationshipType = 'DerivedFrom';

-- 3-hop provenance traversal
SELECT a.AtomId, a.Modality, e1.RelationshipType, e2.RelationshipType, e3.RelationshipType
FROM graph.AtomGraphNodes AS a,
     graph.AtomGraphEdges AS e1,
     graph.AtomGraphNodes AS b,
     graph.AtomGraphEdges AS e2,
     graph.AtomGraphNodes AS c,
     graph.AtomGraphEdges AS e3,
     graph.AtomGraphNodes AS d
WHERE MATCH(a-(e1)->b-(e2)->c-(e3)->d)
  AND a.AtomId = @startAtomId;
```

**Neo4j Sync**: `ProvenanceGraphBuilder` (`src/Hartonomous.Workers.Neo4jSync/Services/ProvenanceGraphBuilder.cs`) mirrors SQL graph to Neo4j using `neo4j/schemas/CoreSchema.cypher`.

## Provenance Tables

### provenance.GenerationStreams

**Purpose**: Lineage tracking for generated content.

**Definition** (`sql/tables/provenance.GenerationStreams.sql`):

```sql
CREATE TABLE provenance.GenerationStreams (
    StreamId            BIGINT IDENTITY(1,1) PRIMARY KEY,
    SourceAtomId        BIGINT NOT NULL,
    TargetAtomId        BIGINT NOT NULL,
    GenerationType      NVARCHAR(64) NOT NULL,          -- text, image, audio, multimodal
    ModelUsed           NVARCHAR(256) NULL,
    Parameters          NVARCHAR(MAX) NULL,             -- JSON
    Metadata            NVARCHAR(MAX) NULL,
    CreatedAt           DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),

    CONSTRAINT FK_GenerationStreams_Source FOREIGN KEY (SourceAtomId) REFERENCES dbo.Atoms(AtomId),
    CONSTRAINT FK_GenerationStreams_Target FOREIGN KEY (TargetAtomId) REFERENCES dbo.Atoms(AtomId)
);
```

### provenance.Concepts

**Purpose**: Emergent concept tracking and evolution.

**Definition** (`sql/tables/provenance.Concepts.sql`):

```sql
CREATE TABLE provenance.Concepts (
    ConceptId           BIGINT IDENTITY(1,1) PRIMARY KEY,
    ConceptName         NVARCHAR(256) NOT NULL UNIQUE,
    ConceptType         NVARCHAR(64) NOT NULL,
    CentroidEmbedding   VECTOR(1998) NULL,
    MemberCount         INT NOT NULL DEFAULT 0,
    Metadata            NVARCHAR(MAX) NULL,
    CreatedAt           DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
    UpdatedAt           DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME()
);

CREATE TABLE provenance.AtomConcepts (
    AtomConceptId       BIGINT IDENTITY(1,1) PRIMARY KEY,
    AtomId              BIGINT NOT NULL,
    ConceptId           BIGINT NOT NULL,
    MembershipScore     REAL NOT NULL,
    AssignedAt          DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),

    CONSTRAINT FK_AtomConcepts_Atom FOREIGN KEY (AtomId) REFERENCES dbo.Atoms(AtomId),
    CONSTRAINT FK_AtomConcepts_Concept FOREIGN KEY (ConceptId) REFERENCES provenance.Concepts(ConceptId)
);
```

## Autonomous Operations Tables

### dbo.AutonomousImprovementHistory

**Purpose**: Track OODA loop actions and outcomes.

**Definition** (`sql/tables/dbo.AutonomousImprovementHistory.sql`):

```sql
CREATE TABLE dbo.AutonomousImprovementHistory (
    HistoryId           BIGINT IDENTITY(1,1) PRIMARY KEY,
    ActionType          NVARCHAR(128) NOT NULL,         -- CreateIndex, AdjustWeight, CachePrime
    TargetObject        NVARCHAR(512) NULL,
    Hypothesis          NVARCHAR(MAX) NULL,             -- JSON
    ActionTaken         NVARCHAR(MAX) NULL,
    Improvement         REAL NULL,                      -- Performance improvement %
    Outcome             NVARCHAR(MAX) NULL,
    Timestamp           DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME()
);

CREATE INDEX IX_AutonomousImprovementHistory_ActionType ON dbo.AutonomousImprovementHistory(ActionType);
CREATE INDEX IX_AutonomousImprovementHistory_Timestamp ON dbo.AutonomousImprovementHistory(Timestamp);
```

**Populated By**: `sp_Learn` stored procedure records results of autonomous actions

## Inference and Reasoning Tables

### dbo.InferenceRequests

**Purpose**: Track inference requests and metrics.

**Definition** (`sql/tables/dbo.InferenceTracking.sql`):

```sql
CREATE TABLE dbo.InferenceRequests (
    InferenceRequestId  BIGINT IDENTITY(1,1) PRIMARY KEY,
    RequestType         NVARCHAR(64) NOT NULL,          -- embedding, generation, classification
    ModelUsed           NVARCHAR(256) NULL,
    InputHash           VARBINARY(32) NOT NULL,
    OutputHash          VARBINARY(32) NULL,
    DurationMs          INT NULL,
    TokensProcessed     INT NULL,
    TenantId            INT NOT NULL,
    UserId              NVARCHAR(256) NULL,
    Timestamp           DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME()
);

CREATE INDEX IX_InferenceRequests_Timestamp ON dbo.InferenceRequests(Timestamp);
CREATE INDEX IX_InferenceRequests_TenantId ON dbo.InferenceRequests(TenantId);
```

### dbo.ReasoningChains

**Purpose**: Multi-step reasoning provenance.

**Definition** (`sql/tables/Reasoning.ReasoningFrameworkTables.sql`):

```sql
CREATE TABLE dbo.ReasoningChains (
    ChainId             BIGINT IDENTITY(1,1) PRIMARY KEY,
    InputAtomId         BIGINT NOT NULL,
    ReasoningType       NVARCHAR(64) NOT NULL,          -- chain-of-thought, self-consistency, tree-of-thought
    StepCount           INT NOT NULL,
    FinalAnswer         NVARCHAR(MAX) NULL,
    Confidence          REAL NULL,
    Timestamp           DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),

    CONSTRAINT FK_ReasoningChains_Atom FOREIGN KEY (InputAtomId) REFERENCES dbo.Atoms(AtomId)
);
```

## Billing Tables

### dbo.BillingUsageLedger_InMemory

**Purpose**: High-throughput usage tracking with In-Memory OLTP.

**Definition** (`sql/tables/dbo.BillingUsageLedger_InMemory.sql`):

```sql
CREATE TABLE dbo.BillingUsageLedger_InMemory (
    LedgerEntryId       BIGINT IDENTITY(1,1) PRIMARY KEY NONCLUSTERED,
    TenantId            INT NOT NULL,
    UserId              NVARCHAR(256) NULL,
    EventType           NVARCHAR(64) NOT NULL,          -- inference, embedding, storage
    Quantity            BIGINT NOT NULL,
    UnitCost            DECIMAL(18,6) NOT NULL,
    TotalCost           DECIMAL(18,6) NOT NULL,
    Timestamp           DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),

    INDEX IX_BillingUsageLedger_TenantId NONCLUSTERED HASH (TenantId) WITH (BUCKET_COUNT = 1024),
    INDEX IX_BillingUsageLedger_Timestamp NONCLUSTERED (Timestamp)
) WITH (MEMORY_OPTIMIZED = ON, DURABILITY = SCHEMA_AND_DATA);
```

**Performance**: Lock-free inserts, ~1M records/sec throughput

## Service Broker (OODA Loop)

**Message Types, Contracts, Queues, Services** defined in `scripts/setup-service-broker.sql`:

```sql
-- Message types
CREATE MESSAGE TYPE [AnalyzeMessage] VALIDATION = WELL_FORMED_XML;
CREATE MESSAGE TYPE [HypothesisMessage] VALIDATION = WELL_FORMED_XML;
CREATE MESSAGE TYPE [ActionMessage] VALIDATION = WELL_FORMED_XML;
CREATE MESSAGE TYPE [LearnMessage] VALIDATION = WELL_FORMED_XML;

-- Contracts
CREATE CONTRACT [OODAContract] (
    [AnalyzeMessage] SENT BY INITIATOR,
    [HypothesisMessage] SENT BY TARGET,
    [ActionMessage] SENT BY INITIATOR,
    [LearnMessage] SENT BY TARGET
);

-- Queues
CREATE QUEUE AnalyzeQueue WITH STATUS = ON;
CREATE QUEUE HypothesizeQueue WITH STATUS = ON;
CREATE QUEUE ActQueue WITH STATUS = ON;
CREATE QUEUE LearnQueue WITH STATUS = ON;

-- Services
CREATE SERVICE AnalyzeService ON QUEUE AnalyzeQueue ([OODAContract]);
CREATE SERVICE HypothesizeService ON QUEUE HypothesizeQueue ([OODAContract]);
CREATE SERVICE ActService ON QUEUE ActQueue ([OODAContract]);
CREATE SERVICE LearnService ON QUEUE LearnQueue ([OODAContract]);
```

**Activation Procedures**: `sql/procedures/dbo.sp_Analyze.sql`, `sp_Hypothesize.sql`, `sp_Act.sql`, `sp_Learn.sql` consume messages and execute OODA loop phases.

## EF Core Mappings

**DbContext**: `src/Hartonomous.Data/HartonomousDbContext.cs`

**Entity Configurations**: `src/Hartonomous.Data/Configurations/*.cs`

**Key Mappings**:

- `AtomConfiguration.cs`: Maps `ContentHash`, `Metadata`/`Semantics` JSON, optional `SpatialKey` geometry
- `AtomEmbeddingConfiguration.cs`: Maps `EmbeddingVector` as `VECTOR(1998)`, `SpatialGeometry` as NetTopologySuite `Point`
- `TensorAtomConfiguration.cs`: Maps `SpatialSignature` geometry, relationships to coefficients
- `TensorAtomCoefficientConfiguration.cs`: Maps temporal columns, history table
- `ModelConfiguration.cs`, `ModelLayerConfiguration.cs`: Model structure relationships

**Migrations**: `src/Hartonomous.Data/Migrations/` for schema evolution

## Verification Queries

```sql
-- Check temporal tables
SELECT t.name AS TableName, t.temporal_type_desc
FROM sys.tables t
WHERE t.temporal_type = 2;

-- Check spatial indexes
SELECT 
    OBJECT_NAME(i.object_id) AS TableName,
    i.name AS IndexName,
    i.type_desc
FROM sys.indexes i
WHERE i.type_desc = 'SPATIAL';

-- Check graph tables
SELECT name FROM sys.tables WHERE is_node = 1 OR is_edge = 1;

-- Check In-Memory OLTP tables
SELECT name, is_memory_optimized
FROM sys.tables
WHERE is_memory_optimized = 1;

-- Check Service Broker queues
SELECT name, is_receive_enabled, is_enqueue_enabled
FROM sys.service_queues;
```

## References

- [README.md](../README.md) - Getting started guide
- [ARCHITECTURE.md](../ARCHITECTURE.md) - System architecture
- [DEPLOYMENT.md](../DEPLOYMENT.md) - Deployment guide
- [CLR_GUIDE.md](CLR_GUIDE.md) - CLR functions and deployment
- `sql/tables/*.sql` - Table definitions
- `src/Hartonomous.Data/Configurations/*.cs` - EF Core mappings
