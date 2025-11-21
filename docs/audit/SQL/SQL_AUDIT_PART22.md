# SQL Audit Part 22: Model and Tensor Tables

## Overview
Part 22 analyzes 5 model/tensor tables: AI model storage, layer decomposition, and tensor coefficient representation.

---

## 1. dbo.Model

**Location:** `src/Hartonomous.Database/Tables/dbo.Model.sql`  
**Type:** Core Entity Table  
**Lines:** ~30  
**Quality Score:** 78/100

### Purpose
Represents AI models ingested into the system. Stores model metadata, configuration, usage statistics, and optional serialized model binary.

### Schema

**Primary Key:** ModelId (INT IDENTITY)  
**No Foreign Keys**

**Core Columns:**
- `ModelId` - INT IDENTITY - Primary key
- `ModelName` - NVARCHAR(200) NOT NULL - Model display name
- `ModelType` - NVARCHAR(100) NOT NULL - Type ('transformer', 'cnn', 'rnn', etc.)
- `ModelVersion` - NVARCHAR(50) NULL - Version string
- `Architecture` - NVARCHAR(100) NULL - Architecture name ('BERT', 'GPT', etc.)
- `Config` - JSON NULL - Native JSON type for structured configuration
- `ParameterCount` - BIGINT NULL - Total parameters
- `MetadataJson` - JSON NULL - Extended metadata
- `SerializedModel` - VARBINARY(MAX) NULL - Serialized model binary

**Usage Tracking:**
- `IngestionDate` - DATETIME2(7) NOT NULL DEFAULT SYSUTCDATETIME()
- `CreatedAt` - DATETIME2(7) NOT NULL DEFAULT SYSUTCDATETIME()
- `LastUsed` - DATETIME2(7) NULL - Last inference timestamp
- `UsageCount` - BIGINT NOT NULL DEFAULT 0 - Total inference count
- `AverageInferenceMs` - FLOAT NULL - Average inference time

**Multi-Tenancy:**
- `TenantId` - INT NOT NULL DEFAULT 0
- `IsActive` - BIT NOT NULL DEFAULT 1 - Soft delete flag

### Indexes

**Clustered:** PK_Model (ModelId)

**Missing Indexes:**
- ❌ No index on ModelName (common lookup)
- ❌ No index on TenantId (multi-tenancy)
- ❌ No index on IsActive (active models query)
- ❌ No index on LastUsed (recent models query)

### Quality Assessment

**Strengths:**
- ✅ **Native JSON columns** - Config, MetadataJson (SQL Server 2025)
- ✅ **Multi-tenancy** - TenantId column
- ✅ **Usage tracking** - LastUsed, UsageCount, AverageInferenceMs
- ✅ **Soft delete** - IsActive flag
- ✅ **UTC timestamps** - SYSUTCDATETIME() defaults
- ✅ **Flexible metadata** - JSON columns for extensibility

**Weaknesses:**
- 🔴 **No indexes** - Only clustered PK, missing common lookup indexes
- 🔴 **SerializedModel VARBINARY(MAX)** - MUST bloat table (MUST use FILESTREAM)
- ⚠️ **ModelType/Architecture NVARCHAR** - No CHECK constraints (enum values)
- ⚠️ **No unique constraint** - MUST have duplicate (ModelName, ModelVersion, TenantId)
- ⚠️ **TenantId DEFAULT 0** - MUST require explicit value
- ⚠️ **UsageCount manual** - No trigger to maintain (trust app logic)
- ⚠️ **No CreatedBy/ModifiedBy** - Audit columns missing

**Performance:**
- 🔴 Missing indexes (ModelName, TenantId, IsActive, LastUsed)
- ⚠️ VARBINARY(MAX) for SerializedModel MUST cause large row issues
- ⚠️ JSON columns (Config, MetadataJson) - No computed column indexes

**Security:**
- ✅ TenantId for multi-tenancy
- ⚠️ No row-level security (RLS)

### Advanced SQL Server Capabilities Analysis

**Memory-Optimized Tables (OLTP/Hekaton):**
- 🔴 **NOT memory-optimized** - Moderate candidate
- **Candidate Score: 60/100** - Moderate benefit
  - Reasons: Moderate read frequency (model lookups), small-ish rows (~500 bytes without SerializedModel)
  - Blocker: VARBINARY(MAX) not supported in memory-optimized tables
  - Blocker: JSON columns supported but limited in memory-optimized
  - **IMPLEMENT:** Keep disk-based, but IMPLEMENT cache table for hot models
  - required implementation: Move SerializedModel to separate table (ModelBinary), make Model memory-optimized

**Columnstore Indexes:**
- ❌ **No columnstore** - Not suitable (OLTP workload, small table)
- ✅ Row-based correct for model metadata lookups

**Table Compression:**
- 🔴 **No compression specified** (defaults to NONE)
- ✅ **Good candidate for PAGE compression** - JSON compresses well
- **IMPLEMENT:** `ALTER TABLE dbo.Model REBUILD WITH (DATA_COMPRESSION = PAGE)`
  - Estimated savings: 40-50% (JSON, NVARCHAR compress well)
  - SerializedModel already compressed (model weights) - won't compress more

**Partitioning:**
- ❌ **Not partitioned** - Low benefit (small table, <10K models expected)
- **Candidate Score: 30/100** - Low benefit
  - Partition by TenantId: Only if multi-tenant with thousands of models per tenant

**FILESTREAM for Large Binary:**
- 🔴 **SerializedModel VARBINARY(MAX)** - MUST use FILESTREAM
- **IMPLEMENT:** Convert to FILESTREAM column
  - `ALTER TABLE dbo.Model ADD SerializedModelStream VARBINARY(MAX) FILESTREAM`
  - Benefits: Store large model binaries outside SQL Server (reduce bloat)
  - Use case: Models >1MB (MUST be vast majority)

**JSON Optimization:**
- ✅ Native JSON columns (Config, MetadataJson)
- ❌ **No JSON indexes** - Common JSON paths MUST be indexed
- **IMPLEMENT:** Add computed columns + indexes
  - `ALTER TABLE dbo.Model ADD ModelFamily AS JSON_VALUE(Config, '$.family') PERSISTED`
  - `CREATE INDEX IX_Model_ModelFamily ON dbo.Model(ModelFamily) WHERE ModelFamily IS NOT NULL`
  - Common paths: $.family, $.task, $.max_length

**Query Performance:**
- 🔴 **Missing critical indexes** - ModelName, TenantId, IsActive, LastUsed
- **IMPLEMENT:** Add nonclustered indexes
  - `CREATE UNIQUE INDEX IX_Model_Name_Version ON dbo.Model(TenantId, ModelName, ModelVersion) WHERE IsActive = 1`
  - `CREATE INDEX IX_Model_TenantId_IsActive ON dbo.Model(TenantId, IsActive) INCLUDE (ModelId, ModelName, ModelType)`
  - `CREATE INDEX IX_Model_LastUsed ON dbo.Model(LastUsed DESC) WHERE IsActive = 1`

**Intelligent Query Processing (IQP):**
- ✅ Compatible with IQP features (SQL Server 2025)
- ✅ Scalar UDF inlining (no UDFs)
- ⚠️ **Small table** - IQP benefits minimal until >100K rows

### REQUIRED FIXES

**CRITICAL - Indexes (Priority 1):**
1. **Add unique constraint** - (TenantId, ModelName, ModelVersion) WHERE IsActive = 1
2. **Add TenantId index** - `CREATE INDEX IX_Model_TenantId_IsActive ON dbo.Model(TenantId, IsActive) INCLUDE (ModelId, ModelName, ModelType)`
3. **Add ModelName index** - `CREATE INDEX IX_Model_Name ON dbo.Model(ModelName) INCLUDE (ModelId, TenantId, IsActive)`
4. **Add LastUsed index** - `CREATE INDEX IX_Model_LastUsed ON dbo.Model(LastUsed DESC) WHERE IsActive = 1`

**FILESTREAM (Priority 1):**
5. **Convert SerializedModel to FILESTREAM** - Move large binaries out of table
   - `ALTER TABLE dbo.Model ADD SerializedModelStream VARBINARY(MAX) FILESTREAM`
   - Migrate data, drop SerializedModel column

**Compression (Priority 1):**
6. **Enable PAGE compression** - `ALTER TABLE dbo.Model REBUILD WITH (DATA_COMPRESSION = PAGE)`
   - 40-50% savings (JSON/NVARCHAR compress well)

**Schema (Priority 2):**
7. **Add CHECK constraints** - ModelType, Architecture enum values
8. **Remove TenantId DEFAULT 0** - Require explicit value
9. **Add CreatedBy/ModifiedBy** - Audit trail columns

**JSON Optimization (Priority 2):**
10. **Add JSON computed columns** - Model family, task, max_length
11. **Add JSON indexes** - Common JSON paths

**Maintenance (Priority 3):**
12. **Add trigger for UsageCount** - Automatic maintenance
13. **IMPLEMENT memory-optimized cache** - Hot models table (duplicate top 100 models)

**Architectural Justification:**

**Why does this table exist instead of atoms?**
- 🤔 **QUESTIONABLE: Model metadata MUST be atoms**
- ❌ ModelName, ModelType, Architecture are TEXT (MUST be atoms)
- ❌ Config JSON MUST be atomized (JSON → atoms with composition)
- ❌ MetadataJson MUST be atomized
- ✅ **BUT: ParameterCount, UsageCount, AverageInferenceMs are AGGREGATE METRICS** (not source content)
- ✅ SerializedModel is LARGE BINARY (>64 bytes, but MUST be decomposed to atoms!)
- ⚠️ **HYBRID APPROACH MISSING** - MUST store metadata AS atoms, keep aggregates in table

**What MUST be atoms?**
- ✅ ModelName (text atom, <64 bytes)
- ✅ ModelType (text atom, <64 bytes)
- ✅ Architecture (text atom, <64 bytes)
- ✅ Config JSON (atomize JSON structure → atoms + compositions)
- ✅ MetadataJson (atomize JSON structure → atoms + compositions)
- ✅ SerializedModel (decompose binary → atoms, or FILESTREAM + atom reference)

**What MUST NOT be atoms?**
- ✅ ModelId (identity, internal key)
- ✅ ParameterCount (aggregate, computed from layers)
- ✅ UsageCount (counter, frequently updated)
- ✅ AverageInferenceMs (computed metric, frequently updated)
- ✅ LastUsed (timestamp, frequently updated)
- ✅ TenantId (multi-tenancy, internal metadata)
- ✅ IsActive (flag, soft delete)
- ✅ IngestionDate, CreatedAt (timestamps, internal metadata)

**IMPLEMENT:**
- 🔄 **REFACTOR: Store model metadata AS atoms**
  - ModelName → Atom (Modality='text', Subtype='model-name')
  - Config → Atomize JSON (atoms + compositions)
  - SerializedModel → Atomize binary (atoms + compositions) OR FILESTREAM + atom reference
- ✅ **KEEP: Aggregate metrics in Model table** (UsageCount, AverageInferenceMs, LastUsed)
- ✅ **KEEP: Internal metadata** (TenantId, IsActive, timestamps)
- 🔄 **ADD: AtomId FK** - Reference to "model metadata atom" (aggregates all model metadata)

**Revised schema proposal:**
```sql
CREATE TABLE dbo.Model (
    ModelId INT IDENTITY PRIMARY KEY,
    ModelAtomId BIGINT NOT NULL FK Atom, -- References atom containing model metadata
    TenantId INT NOT NULL,
    -- Aggregate metrics (NOT atoms)
    ParameterCount BIGINT NULL,
    UsageCount BIGINT NOT NULL DEFAULT 0,
    AverageInferenceMs FLOAT NULL,
    LastUsed DATETIME2(7) NULL,
    -- Internal metadata (NOT atoms)
    IsActive BIT NOT NULL DEFAULT 1,
    IngestionDate DATETIME2(7) NOT NULL DEFAULT SYSUTCDATETIME(),
    CreatedAt DATETIME2(7) NOT NULL DEFAULT SYSUTCDATETIME()
);
```

**Example atomization:**
- Atom 1: ModelName="gpt-4" (text atom)
- Atom 2: Architecture="transformer" (text atom)
- Atom 3: Config='{"layers":48,...}' (JSON atom, or atomize further)
- Atom 4: SerializedModel (binary atoms via FILESTREAM + atom refs)
- AtomComposition: Parent="gpt-4 model metadata", Components=[Atom1, Atom2, Atom3, Atom4]
- Model.ModelAtomId = "gpt-4 model metadata" atom

**Notes:**
- SerializedModel MUST be FILESTREAM (most critical fix)
- Missing indexes will cause performance issues at scale
- IMPLEMENT memory-optimized cache for hot models (not full memory-optimization)
- **ARCHITECTURAL DEBT:** Model table mixes source content (MUST be atoms) with aggregates (correct as table)

---

## 2. dbo.ModelLayer

**Location:** `src/Hartonomous.Database/Tables/dbo.ModelLayer.sql`  
**Type:** Model Decomposition Table  
**Lines:** ~35  
**Quality Score:** 72/100

### Purpose
Decompose AI models into layers. Stores layer metadata, tensor shapes, quantization info, and spatial geometry for unified queries.

### Schema

**Primary Key:** LayerId (BIGINT IDENTITY)  
**Foreign Keys:** ModelId → Model (CASCADE DELETE), LayerAtomId → Atom (SET NULL)

**Core Columns:**
- `LayerId` - BIGINT IDENTITY - Primary key
- `ModelId` - INT NOT NULL - FK to Model
- `LayerIdx` - INT NOT NULL - Layer index (0-based)
- `LayerName` - NVARCHAR(100) NULL - Layer name ('encoder.0', 'attention', etc.)
- `LayerType` - NVARCHAR(50) NULL - Type ('linear', 'conv2d', 'attention', etc.)
- `LayerAtomId` - BIGINT NULL - FK to Atom (layer as atom)

**Tensor Metadata:**
- `TensorShape` - NVARCHAR(200) NULL - Shape string ('[768, 768]')
- `TensorDtype` - NVARCHAR(20) NULL DEFAULT 'float32' - Data type
- `ParameterCount` - BIGINT NULL - Layer parameter count
- `Parameters` - JSON NULL - Layer-specific parameters

**Quantization:**
- `QuantizationType` - NVARCHAR(20) NULL - Type ('int8', 'int4', etc.)
- `QuantizationScale` - FLOAT NULL - Quantization scale
- `QuantizationZeroPoint` - FLOAT NULL - Quantization zero point

**Spatial Representation:**
- `WeightsGeometry` - GEOMETRY NULL - Spatial representation of weights
- `ZMin/ZMax` - FLOAT NULL - Depth bounds
- `MMin/MMax` - FLOAT NULL - Measure bounds
- `MortonCode` - BIGINT NULL - Z-order curve value

**Performance Tracking:**
- `PreviewPointCount` - INT NULL - Preview point count
- `CacheHitRate` - FLOAT DEFAULT 0.0 - Cache effectiveness
- `AvgComputeTimeMs` - FLOAT NULL - Average compute time

### Indexes

**Clustered:** PK_ModelLayer (LayerId)

**Missing Indexes:**
- ❌ No index on ModelId (foreign key lookups)
- ❌ No index on (ModelId, LayerIdx) - Natural composite key
- ❌ No spatial index on WeightsGeometry
- ❌ No index on MortonCode (Z-order queries)

### Quality Assessment

**Strengths:**
- ✅ **Rich tensor metadata** - Shape, dtype, quantization
- ✅ **Spatial representation** - WeightsGeometry for unified queries
- ✅ **Z-order curve** - MortonCode for 1D indexing
- ✅ **Performance tracking** - CacheHitRate, AvgComputeTimeMs
- ✅ **CASCADE DELETE** - ModelId FK cascades
- ✅ **SET NULL** - LayerAtomId FK on Atom delete

**Weaknesses:**
- 🔴 **No indexes** - Only clustered PK, missing FK and spatial indexes
- 🔴 **No unique constraint** - MUST have duplicate (ModelId, LayerIdx)
- 🔴 **No TenantId** - Multi-tenancy missing (SECURITY GAP)
- ⚠️ **TensorShape NVARCHAR(200)** - MUST be structured (JSON or computed columns)
- ⚠️ **WeightsGeometry NULL** - Spatial representation optional (inconsistent)
- ⚠️ **No CreatedAt timestamp** - Audit trail missing
- ⚠️ **LayerType NVARCHAR(50)** - No CHECK constraint (enum values)

**Performance:**
- 🔴 Missing FK index on ModelId (slow joins)
- 🔴 Missing spatial index on WeightsGeometry
- 🔴 Missing index on MortonCode
- ⚠️ GEOMETRY columns can be large (spatial overhead)

**Security:**
- 🔴 **NO TenantId** - Cross-tenant data leakage possible (CRITICAL)

### Advanced SQL Server Capabilities Analysis

**Memory-Optimized Tables (OLTP/Hekaton):**
- 🔴 **NOT memory-optimized** - Moderate candidate
- **Candidate Score: 65/100** - Moderate benefit
  - Reasons: Frequent layer lookups during inference, moderate row size (~300 bytes)
  - Blocker: GEOMETRY not supported in memory-optimized tables
  - Blocker: JSON columns (limited support in memory-optimized)
  - **IMPLEMENT:** Keep disk-based, but add hash indexes if converted
  - required implementation: Move WeightsGeometry to separate table (ModelLayerSpatial)

**Columnstore Indexes:**
- ❌ **No columnstore** - Not suitable (OLTP workload)
- ✅ Row-based correct for layer metadata lookups

**Table Compression:**
- 🔴 **No compression specified** (defaults to NONE)
- ✅ **Good candidate for ROW compression** - GEOMETRY doesn't compress well
- **IMPLEMENT:** `ALTER TABLE dbo.ModelLayer REBUILD WITH (DATA_COMPRESSION = ROW)`
  - Estimated savings: 20-30% (NVARCHAR compresses moderately, GEOMETRY doesn't)
  - ROW compression (not PAGE) for GEOMETRY workloads

**Partitioning:**
- ❌ **Not partitioned** - MUST partition by ModelId
- **Candidate Score: 70/100** - Moderate benefit
  - Partition by ModelId: Isolate layers per model, parallel queries
  - Use case: Large models (1000+ layers)
  - **IMPLEMENT:** Only if table exceeds 1M rows

**Spatial Index Optimization:**
- 🔴 **No spatial index** on WeightsGeometry - CRITICAL missing index
- **IMPLEMENT:** `CREATE SPATIAL INDEX SIX_ModelLayer_WeightsGeometry ON dbo.ModelLayer(WeightsGeometry) WITH (CELLS_PER_OBJECT = 64, BOUNDING_BOX = (-1000,-1000,1000,1000))`

**Z-Order Curve (MortonCode):**
- ✅ MortonCode column exists
- 🔴 **No index on MortonCode** - Missing critical index for 1D range queries
- **IMPLEMENT:** `CREATE INDEX IX_ModelLayer_MortonCode ON dbo.ModelLayer(MortonCode) INCLUDE (LayerId, ModelId, LayerIdx) WHERE MortonCode IS NOT NULL`

**JSON Optimization:**
- ✅ Native JSON column (Parameters)
- ❌ **No JSON indexes** - Common paths MUST be indexed
- **IMPLEMENT:** Add computed columns for common Parameters JSON paths

**Query Performance:**
- 🔴 **Missing FK index** - ModelId (CRITICAL for joins)
- 🔴 **Missing composite unique** - (ModelId, LayerIdx)
- **IMPLEMENT:** 
  - `CREATE UNIQUE INDEX IX_ModelLayer_Model_Idx ON dbo.ModelLayer(ModelId, LayerIdx) INCLUDE (LayerName, LayerType, ParameterCount)`
  - `CREATE INDEX IX_ModelLayer_LayerType ON dbo.ModelLayer(LayerType) WHERE LayerType IS NOT NULL`

**Intelligent Query Processing (IQP):**
- ✅ Compatible with IQP features (SQL Server 2025)
- ⚠️ **Small-moderate table** - IQP benefits at >100K rows

### REQUIRED FIXES

**CRITICAL - Security (Priority 1):**
1. **Add TenantId column** - `ALTER TABLE dbo.ModelLayer ADD TenantId INT NOT NULL DEFAULT 0` (CRITICAL)

**CRITICAL - Indexes (Priority 1):**
2. **Add unique constraint** - (ModelId, LayerIdx) - Natural key
3. **Create FK index** - `CREATE INDEX IX_ModelLayer_ModelId ON dbo.ModelLayer(ModelId) INCLUDE (LayerIdx, LayerName, LayerType)`
4. **Create spatial index** - WeightsGeometry (CRITICAL for spatial queries)
5. **Create MortonCode index** - `CREATE INDEX IX_ModelLayer_MortonCode ON dbo.ModelLayer(MortonCode) INCLUDE (LayerId, ModelId) WHERE MortonCode IS NOT NULL`

**Compression (Priority 1):**
6. **Enable ROW compression** - `ALTER TABLE dbo.ModelLayer REBUILD WITH (DATA_COMPRESSION = ROW)`
   - 20-30% savings

**Schema (Priority 2):**
7. **Add CreatedAt timestamp** - Audit trail
8. **Add CHECK constraint** - LayerType enum values
9. **Change TensorShape to JSON** - Structured shape representation
   - `ALTER TABLE dbo.ModelLayer ADD TensorShapeJson AS CONVERT(JSON, TensorShape) PERSISTED`

**Partitioning (Priority 3):**
10. **IMPLEMENT partitioning by ModelId** - Only if >1M rows (large model workloads)

**Memory-Optimized required implementation (Priority 3):**
11. **Move WeightsGeometry to separate table** - ModelLayerSpatial (if memory-optimization desired)

**Architectural Justification:**

**Why does this table exist instead of atoms?**
- 🤔 **QUESTIONABLE: Layer metadata MUST be atoms**
- ❌ LayerName, LayerType are TEXT (MUST be atoms)
- ❌ TensorShape, TensorDtype are TEXT (MUST be atoms)
- ❌ Parameters JSON MUST be atomized
- ✅ **BUT: WeightsGeometry is COMPUTED/DERIVED** (spatial representation, not source content)
- ✅ **BUT: ParameterCount is AGGREGATE** (computed from weights)
- ✅ **BUT: CacheHitRate, AvgComputeTimeMs are RUNTIME METRICS** (not source content)
- ⚠️ **HYBRID APPROACH MISSING** - MUST store layer metadata AS atoms, keep metrics in table

**What MUST be atoms?**
- ✅ LayerName (text atom, <64 bytes, e.g., "encoder.0")
- ✅ LayerType (text atom, <64 bytes, e.g., "attention")
- ✅ TensorShape (text atom, <64 bytes, e.g., "[768,768]")
- ✅ TensorDtype (text atom, <64 bytes, e.g., "float32")
- ✅ Parameters JSON (atomize JSON → atoms + compositions)
- ⚠️ QuantizationType, QuantizationScale, QuantizationZeroPoint (small, MUST be atoms)

**What MUST NOT be atoms?**
- ✅ LayerId (identity, internal key)
- ✅ ModelId (FK, internal relationship)
- ✅ LayerIdx (ordering, internal metadata)
- ✅ LayerAtomId (FK to atom, internal relationship)
- ✅ WeightsGeometry (COMPUTED/DERIVED from weights, not source content)
- ✅ ZMin/ZMax, MMin/MMax (COMPUTED bounds, not source content)
- ✅ MortonCode (COMPUTED Z-order curve, not source content)
- ✅ ParameterCount (AGGREGATE, computed from weights)
- ✅ PreviewPointCount (COMPUTED, not source content)
- ✅ CacheHitRate, AvgComputeTimeMs (RUNTIME METRICS, frequently updated)

**IMPLEMENT:**
- 🔄 **REFACTOR: Store layer metadata AS atoms**
  - LayerName → Atom (Modality='text', Subtype='layer-name')
  - LayerType → Atom (Modality='text', Subtype='layer-type')
  - TensorShape → Atom (Modality='text', Subtype='tensor-shape')
  - Parameters JSON → Atomize (atoms + compositions)
- ✅ **KEEP: Computed/derived data in ModelLayer** (WeightsGeometry, MortonCode, bounds)
- ✅ **KEEP: Aggregate metrics** (ParameterCount, CacheHitRate, AvgComputeTimeMs)
- ✅ **KEEP: Internal metadata** (LayerIdx, relationships)
- 🔄 **ADD: LayerMetadataAtomId FK** - Reference to "layer metadata atom"

**Revised schema proposal:**
```sql
CREATE TABLE dbo.ModelLayer (
    LayerId BIGINT IDENTITY PRIMARY KEY,
    ModelId INT NOT NULL FK Model,
    LayerIdx INT NOT NULL,
    LayerAtomId BIGINT NULL FK Atom, -- Layer as atom (weights)
    LayerMetadataAtomId BIGINT NULL FK Atom, -- NEW: Layer metadata as atom
    -- Computed/derived spatial data (NOT atoms)
    WeightsGeometry GEOMETRY NULL,
    ZMin/ZMax FLOAT NULL,
    MMin/MMax FLOAT NULL,
    MortonCode BIGINT NULL,
    -- Aggregate metrics (NOT atoms)
    ParameterCount BIGINT NULL,
    PreviewPointCount INT NULL,
    CacheHitRate FLOAT DEFAULT 0.0,
    AvgComputeTimeMs FLOAT NULL
);
```

**Critical Issues:**
- Missing TenantId is **SECURITY VULNERABILITY** (cross-tenant leakage)
- Missing indexes will cause slow joins/queries
- No spatial index prevents spatial queries from working efficiently
- **ARCHITECTURAL DEBT:** ModelLayer mixes source content (MUST be atoms) with computed/aggregate data (correct as table)

---

## 3. dbo.TensorAtom

**Location:** `src/Hartonomous.Database/Tables/dbo.TensorAtom.sql`  
**Type:** Tensor Decomposition Table  
**Lines:** ~25  
**Quality Score:** 68/100

### Purpose
Map atoms to tensor positions. Represents individual tensor elements (weights) as atoms with spatial signatures.

### Schema

**Primary Key:** TensorAtomId (BIGINT IDENTITY)  
**Foreign Keys:** AtomId → Atom (CASCADE DELETE), ModelId → Model, LayerId → ModelLayer

**Core Columns:**
- `TensorAtomId` - BIGINT IDENTITY - Primary key
- `AtomId` - BIGINT NOT NULL - FK to Atom (the actual weight value)
- `ModelId` - INT NULL - FK to Model (optional)
- `LayerId` - BIGINT NULL - FK to ModelLayer (optional)
- `AtomType` - NVARCHAR(128) NOT NULL - Type ('weight', 'bias', 'embedding', etc.)
- `Metadata` - JSON NULL - Extensible metadata
- `ImportanceScore` - REAL NULL - Importance/saliency score
- `CreatedAt` - DATETIME2(7) NOT NULL DEFAULT SYSUTCDATETIME()

**Spatial Representation:**
- `SpatialSignature` - GEOMETRY NULL - Spatial signature
- `GeometryFootprint` - GEOMETRY NULL - Geometry footprint

### Indexes

**Clustered:** PK_TensorAtom (TensorAtomId)

**Missing Indexes:**
- ❌ No index on AtomId (foreign key lookups)
- ❌ No index on (ModelId, LayerId) - Common filter
- ❌ No index on AtomType
- ❌ No spatial indexes on SpatialSignature/GeometryFootprint

### Quality Assessment

**Strengths:**
- ✅ **Atom-based decomposition** - Weights as atoms (architectural consistency)
- ✅ **Dual spatial representation** - SpatialSignature + GeometryFootprint
- ✅ **Importance scoring** - ImportanceScore for saliency
- ✅ **CASCADE DELETE** - AtomId FK cascades
- ✅ **Extensible metadata** - JSON column
- ✅ **UTC timestamp** - CreatedAt default

**Weaknesses:**
- 🔴 **No indexes** - Only clustered PK, missing FK and spatial indexes
- 🔴 **No TenantId** - Multi-tenancy missing (SECURITY GAP)
- 🔴 **ModelId NULL, LayerId NULL** - MUST be NOT NULL (orphaned tensor atoms possible)
- ⚠️ **AtomType NVARCHAR(128)** - No CHECK constraint (enum values)
- ⚠️ **Two GEOMETRY columns** - Duplication? (SpatialSignature vs GeometryFootprint)
- ⚠️ **No unique constraint** - MUST have duplicate (AtomId, ModelId, LayerId)
- ⚠️ **ImportanceScore REAL** - Low precision (MUST be FLOAT?)

**Performance:**
- 🔴 Missing FK indexes (AtomId, ModelId, LayerId)
- 🔴 Missing spatial indexes (2 GEOMETRY columns)
- ⚠️ Dual GEOMETRY columns double storage overhead

**Security:**
- 🔴 **NO TenantId** - Cross-tenant data leakage possible (CRITICAL)

### Advanced SQL Server Capabilities Analysis

**Memory-Optimized Tables (OLTP/Hekaton):**
- 🔴 **NOT memory-optimized** - Low candidate (large table, spatial columns)
- **Candidate Score: 40/100** - Low benefit
  - Reasons: Large table (millions+ rows), GEOMETRY blocker
  - Blocker: GEOMETRY not supported in memory-optimized tables
  - Blocker: JSON columns (limited support)
  - **IMPLEMENT:** Keep disk-based
  - required implementation: Separate spatial table, memory-optimize core TensorAtom

**Columnstore Indexes:**
- ❌ **No columnstore** - MUST add nonclustered columnstore for analytics
- ✅ Row-based correct for tensor lookups
- **IMPLEMENT:** Add nonclustered columnstore for aggregations
  - `CREATE NONCLUSTERED COLUMNSTORE INDEX NCCI_TensorAtom_Analytics ON dbo.TensorAtom(ModelId, LayerId, AtomType, ImportanceScore, CreatedAt)`
  - Use case: Analyze importance distributions, count atoms per layer

**Table Compression:**
- 🔴 **No compression specified** (defaults to NONE)
- ✅ **Good candidate for ROW compression** - GEOMETRY doesn't compress well
- **IMPLEMENT:** `ALTER TABLE dbo.TensorAtom REBUILD WITH (DATA_COMPRESSION = ROW)`
  - Estimated savings: 20-30% (NVARCHAR compresses, GEOMETRY doesn't)

**Partitioning:**
- ❌ **Not partitioned** - Excellent candidate for partitioning by ModelId or LayerId
- **Candidate Score: 85/100** - High benefit (large table)
  - Partition by ModelId: Isolate tensors per model, parallel queries
  - Partition by LayerId: Isolate tensors per layer (better granularity)
  - Benefits: Partition elimination, maintenance per partition
  - **IMPLEMENT:** Partition by LayerId (most selective filter)
  - Expected rows: Millions+ (100M+ parameters → 100M+ tensor atoms)

**Spatial Index Optimization:**
- 🔴 **No spatial indexes** - CRITICAL for spatial queries
- **IMPLEMENT:** 
  - `CREATE SPATIAL INDEX SIX_TensorAtom_SpatialSignature ON dbo.TensorAtom(SpatialSignature) WITH (CELLS_PER_OBJECT = 64)`
  - `CREATE SPATIAL INDEX SIX_TensorAtom_GeometryFootprint ON dbo.TensorAtom(GeometryFootprint) WITH (CELLS_PER_OBJECT = 64)`

**JSON Optimization:**
- ✅ Native JSON column (Metadata)
- ❌ **No JSON indexes** - Common paths MUST be indexed

**Query Performance:**
- 🔴 **Missing FK indexes** - AtomId, ModelId, LayerId (CRITICAL)
- **IMPLEMENT:**
  - `CREATE INDEX IX_TensorAtom_AtomId ON dbo.TensorAtom(AtomId) INCLUDE (TensorAtomId, ModelId, LayerId, AtomType)`
  - `CREATE INDEX IX_TensorAtom_Model_Layer ON dbo.TensorAtom(ModelId, LayerId, AtomType) INCLUDE (AtomId, ImportanceScore)`
  - `CREATE INDEX IX_TensorAtom_ImportanceScore ON dbo.TensorAtom(ImportanceScore DESC) WHERE ImportanceScore IS NOT NULL`

**Intelligent Query Processing (IQP):**
- ✅ Compatible with IQP features (SQL Server 2025)
- ✅ Large table - IQP benefits significant at millions of rows

### REQUIRED FIXES

**CRITICAL - Security (Priority 1):**
1. **Add TenantId column** - `ALTER TABLE dbo.TensorAtom ADD TenantId INT NOT NULL DEFAULT 0` (CRITICAL)

**CRITICAL - Schema (Priority 1):**
2. **Make ModelId NOT NULL** - Prevent orphaned tensor atoms
3. **Make LayerId NOT NULL** - Enforce layer association
4. **Add unique constraint** - (AtomId, ModelId, LayerId) OR (TensorAtomId already unique?)

**CRITICAL - Indexes (Priority 1):**
5. **Create FK indexes** - AtomId, (ModelId, LayerId), AtomType
6. **Create spatial indexes** - SpatialSignature, GeometryFootprint (2 indexes)

**Partitioning (Priority 1):**
7. **Partition by LayerId** (85/100 candidate) - Isolate tensors per layer
   - Large table (millions+ rows) benefits significantly from partitioning

**Compression (Priority 1):**
8. **Enable ROW compression** - `ALTER TABLE dbo.TensorAtom REBUILD WITH (DATA_COMPRESSION = ROW)`
   - 20-30% savings

**Columnstore (Priority 2):**
9. **Add nonclustered columnstore** - For analytics (importance distributions)

**Schema (Priority 2):**
10. **Add CHECK constraint** - AtomType enum values
11. **Consolidate GEOMETRY columns** - Do we need both SpatialSignature and GeometryFootprint? Document difference or remove one
12. **IMPLEMENT FLOAT for ImportanceScore** - Higher precision (8 bytes vs 4 bytes)

**Architectural Justification:**

**Why does this table exist instead of atoms?**
- ✅ **CORRECT: TensorAtom maps atoms to tensor POSITIONS**
- ✅ AtomId FK references actual weight VALUE (stored as atom)
- ✅ TensorAtom is METADATA about atom positioning in tensor space
- ❌ **BUT: AtomType MUST be an atom** (text, <64 bytes)
- ❌ **BUT: Metadata JSON MUST be atomized**
- ✅ **BUT: SpatialSignature, GeometryFootprint are COMPUTED/DERIVED** (not source content)
- ✅ **BUT: ImportanceScore is COMPUTED METRIC** (saliency, not source content)

**What IS an atom in this context?**
- ✅ The actual weight VALUE (float32, stored in Atom table)
- ✅ TensorAtom.AtomId references this weight atom

**What MUST be atoms?**
- ✅ AtomType (text atom, e.g., "weight", "bias", "embedding")
- ✅ Metadata JSON (atomize JSON → atoms + compositions)

**What MUST NOT be atoms?**
- ✅ TensorAtomId (identity, internal key)
- ✅ AtomId (FK, references weight atom - CORRECT)
- ✅ ModelId, LayerId (FKs, internal relationships)
- ✅ SpatialSignature (COMPUTED/DERIVED from position, not source content)
- ✅ GeometryFootprint (COMPUTED/DERIVED from position, not source content)
- ✅ ImportanceScore (COMPUTED saliency metric, not source content)
- ✅ CreatedAt (timestamp, internal metadata)

**Current architecture:**
- ✅ **MOSTLY CORRECT** - Weight values ARE atoms (TensorAtom.AtomId → Atom)
- ⚠️ **MINOR ISSUE** - AtomType MUST be atom reference (not NVARCHAR column)

**IMPLEMENT:**
- 🔄 **MINOR REFACTOR: Store AtomType AS atom**
  - AtomType → Atom (Modality='text', Subtype='tensor-type')
  - Change: `AtomType NVARCHAR(128)` → `AtomTypeAtomId BIGINT FK Atom`
- ✅ **KEEP: Everything else** (spatial data is computed, not source content)
- ✅ **ARCHITECTURE IS SOUND** - Weight values already stored as atoms

**Revised schema proposal:**
```sql
CREATE TABLE dbo.TensorAtom (
    TensorAtomId BIGINT IDENTITY PRIMARY KEY,
    AtomId BIGINT NOT NULL FK Atom, -- Weight value (CORRECT)
    AtomTypeAtomId BIGINT NOT NULL FK Atom, -- NEW: Type as atom ("weight", "bias", etc.)
    ModelId INT NULL FK Model,
    LayerId BIGINT NULL FK ModelLayer,
    -- Computed/derived spatial data (NOT atoms)
    SpatialSignature GEOMETRY NULL,
    GeometryFootprint GEOMETRY NULL,
    ImportanceScore REAL NULL,
    -- Metadata (IMPLEMENT atomizing)
    Metadata JSON NULL,
    CreatedAt DATETIME2(7) NOT NULL DEFAULT SYSUTCDATETIME()
);
```

**Critical Issues:**
- Missing TenantId is **SECURITY VULNERABILITY**
- Missing indexes will cause catastrophic performance at scale (millions+ rows)
- Missing spatial indexes prevent spatial queries
- ModelId/LayerId NULL allows orphaned tensor atoms
- **ARCHITECTURAL DEBT (minor):** AtomType MUST be atom reference (not NVARCHAR column)

---

## 4. dbo.TensorAtomCoefficient

**Location:** `src/Hartonomous.Database/Tables/dbo.TensorAtomCoefficient.sql`  
**Type:** Tensor Position Mapping Table (System-Versioned)  
**Lines:** ~70  
**Quality Score:** 88/100 ⭐

### Purpose
Structural representation for tensor models. Stores explicit OLAP-queryable weight mappings with spatial indexing. Maps atoms (weights) to 3D tensor positions with temporal versioning.

### Schema

**Primary Key:** (TensorAtomId, ModelId, LayerIdx, PositionX, PositionY, PositionZ)  
**Foreign Keys:** TensorAtomId → Atom, ModelId → Model (CASCADE DELETE)  
**Temporal:** System-versioned with TensorAtomCoefficients_History

**Core Columns:**
- `TensorAtomId` - BIGINT NOT NULL - FK to Atom (the float value)
- `ModelId` - INT NOT NULL - FK to Model
- `LayerIdx` - INT NOT NULL - Layer index
- `PositionX` - INT NOT NULL - Row
- `PositionY` - INT NOT NULL - Column
- `PositionZ` - INT NOT NULL DEFAULT 0 - Depth/batch dimension

**Spatial:**
- `SpatialKey` - GEOMETRY PERSISTED - Computed: Point(PositionX, PositionY, 0)

**Deprecated (v5 migration):**
- `TensorAtomCoefficientId` - BIGINT NULL - DEPRECATED: No identity in v5
- `ParentLayerId` - BIGINT NULL - DEPRECATED: Use ModelId + LayerIdx
- `TensorRole` - NVARCHAR(128) NULL - DEPRECATED: Use positional indexing
- `Coefficient` - REAL NULL - DEPRECATED: The coefficient IS the atom (TensorAtomId)

**Temporal:**
- `ValidFrom` - DATETIME2(7) GENERATED ALWAYS AS ROW START
- `ValidTo` - DATETIME2(7) GENERATED ALWAYS AS ROW END

### Indexes

**Clustered:** PK_TensorAtomCoefficients (TensorAtomId, ModelId, LayerIdx, PositionX, PositionY, PositionZ)

**Nonclustered Columnstore:**
- `NCCI_TensorAtomCoefficients` - (TensorAtomId, ModelId, LayerIdx, PositionX, PositionY, PositionZ)

**Spatial:**
- `SIX_TensorAtomCoefficients_SpatialKey` - BOUNDING_BOX (0, 0, 10000, 10000)

### Quality Assessment

**Strengths:**
- ✅ **GOLD STANDARD: Nonclustered columnstore on temporal table** - Rare pattern, excellent for OLAP
- ✅ **Computed SpatialKey** - PERSISTED computed column (no storage duplication)
- ✅ **Composite PK** - Natural key (TensorAtomId, ModelId, LayerIdx, PositionX, Y, Z)
- ✅ **System versioning** - Full temporal history
- ✅ **CASCADE DELETE** - ModelId FK cascades
- ✅ **Explicit deprecation** - Old columns marked DEPRECATED (migration strategy)
- ✅ **Spatial index** - BOUNDING_BOX (0, 0, 10000, 10000)
- ✅ **OLAP-optimized** - Nonclustered columnstore for aggregations

**Weaknesses:**
- ⚠️ **Deprecated columns still present** - MUST be removed after migration complete
- ⚠️ **No TenantId** - Multi-tenancy missing (but inherited from Model FK)
- ⚠️ **Composite PK (6 columns)** - Large key size (~30 bytes)
- ⚠️ **PositionZ DEFAULT 0** - Assumes 2D tensors (3D tensors must explicitly set Z)
- ⚠️ **BOUNDING_BOX (0, 0, 10000, 10000)** - Hardcoded, assumes max tensor size 10000x10000

**Performance:**
- ✅ Excellent: Nonclustered columnstore for OLAP
- ✅ Spatial index for geometric queries
- ✅ Composite PK for positional lookups
- ⚠️ Large composite PK (30 bytes) - MUST impact index size
- ⚠️ History table (no columnstore on history?) - MUST verify

**Security:**
- ⚠️ No TenantId (inherited from Model FK, but not explicit)

### Advanced SQL Server Capabilities Analysis

**Memory-Optimized Tables (OLTP/Hekaton):**
- ❌ **NOT memory-optimized** - Not applicable
- **Candidate Score: 10/100** - Not suitable
  - Reasons: System-versioned temporal not supported in memory-optimized
  - Blocker: GEOMETRY not supported in memory-optimized
  - Blocker: Columnstore on temporal table (already optimized for OLAP)
  - **IMPLEMENT:** Keep disk-based (already optimized)

**Columnstore Indexes:**
- ✅ **GOLD STANDARD: Nonclustered columnstore on temporal table** - EXCELLENT
- ✅ Columnstore on (TensorAtomId, ModelId, LayerIdx, PositionX, PositionY, PositionZ)
- ✅ Enables fast aggregations (SUM, AVG, COUNT by layer/position)
- ⚠️ **History table missing columnstore?** - MUST verify and add
- **IMPLEMENT:** Ensure history table has clustered columnstore
  - `CREATE CLUSTERED COLUMNSTORE INDEX CCI_TensorAtomCoefficients_History ON dbo.TensorAtomCoefficients_History`

**Table Compression:**
- ✅ **Columnstore provides compression** - No additional compression needed
- ✅ Columnstore typically 70-90% compression for numeric data
- **No action needed** - Columnstore already provides compression

**Partitioning:**
- ❌ **Not partitioned** - Excellent candidate for partitioning by ModelId or LayerIdx
- **Candidate Score: 95/100** - VERY high benefit (largest table)
  - Partition by ModelId: Isolate coefficients per model
  - Partition by LayerIdx: Even finer granularity (partition per layer)
  - Expected rows: Billions (100M parameter model = 100M rows)
  - Benefits: Partition elimination (MASSIVE perf gain), maintenance per partition
  - **IMPLEMENT:** Partition by ModelId first, IMPLEMENT LayerIdx if models >1B parameters
  - Combine with columnstore: Partitioned columnstore (BEST practice)

**Spatial Index Optimization:**
- ✅ Spatial index exists (SIX_TensorAtomCoefficients_SpatialKey)
- ⚠️ **BOUNDING_BOX hardcoded** (0, 0, 10000, 10000) - Document assumption
- ⚠️ **CELLS_PER_OBJECT not specified** - Defaults to 16 (will be too low - CRITICAL FIX)
- **IMPLEMENT:** Rebuild spatial index with tuning
  - `CREATE SPATIAL INDEX SIX_TensorAtomCoefficients_SpatialKey ON dbo.TensorAtomCoefficient(SpatialKey) WITH (CELLS_PER_OBJECT = 64, BOUNDING_BOX = (0,0,10000,10000))`

**Computed Column Optimization:**
- ✅ SpatialKey is PERSISTED computed column - Excellent
- ✅ No runtime overhead (computed at insert/update)
- ✅ Spatial index on computed column (supported)

**Temporal Table Optimization:**
- ✅ System-versioned temporal (excellent for model versioning)
- 🔴 **History table likely missing columnstore** - CRITICAL missing optimization
- **IMPLEMENT:** Add clustered columnstore to history table
  - `CREATE CLUSTERED COLUMNSTORE INDEX CCI_TensorAtomCoefficients_History ON dbo.TensorAtomCoefficients_History`
  - 70-90% compression, 10-100x faster analytical queries on history

**Query Performance:**
- ✅ Composite PK supports positional lookups
- ✅ Columnstore for OLAP queries
- ✅ Spatial index for geometric queries
- ⚠️ **Large composite PK** - 30-byte key (6 INT/BIGINT columns)
- ⚠️ **No covering indexes** - All queries hit PK or columnstore

**Intelligent Query Processing (IQP):**
- ✅ **Batch mode on columnstore** - Huge IQP benefit
- ✅ Aggregate pushdown (columnstore)
- ✅ Segment elimination (columnstore + partitioning)
- ✅ Compatible with SQL Server 2025 IQP features

**Migration Strategy (Deprecated Columns):**
- ✅ Deprecated columns marked in comments
- ⚠️ **Still present in schema** - Cleanup pending
- **IMPLEMENT:** Drop deprecated columns after migration complete
  - `ALTER TABLE dbo.TensorAtomCoefficient DROP COLUMN TensorAtomCoefficientId, ParentLayerId, TensorRole, Coefficient`

### REQUIRED FIXES

**Partitioning (Priority 1):**
1. **Partition by ModelId** (95/100 candidate) - CRITICAL for billion-row tables
   - `CREATE PARTITION FUNCTION PF_TensorAtomCoefficient_ModelId (INT) AS RANGE RIGHT FOR VALUES (1, 2, 3, ...)`
   - Expected: Billions of rows (100M parameter model = 100M coefficients)
   - Combine with columnstore: Partitioned nonclustered columnstore (BEST practice)

**Columnstore (Priority 1):**
2. **Add clustered columnstore to history table** - TensorAtomCoefficients_History
   - `CREATE CLUSTERED COLUMNSTORE INDEX CCI_TensorAtomCoefficients_History ON dbo.TensorAtomCoefficients_History`
   - 70-90% compression, 10-100x faster analytical queries

**Spatial Index (Priority 2):**
3. **Rebuild spatial index with CELLS_PER_OBJECT = 64** - Improve spatial query performance
4. **Document BOUNDING_BOX assumptions** - Max tensor size 10000x10000

**Schema Cleanup (Priority 2):**
5. **Drop deprecated columns** - TensorAtomCoefficientId, ParentLayerId, TensorRole, Coefficient
   - Wait until migration complete

**Retention Policy (Priority 3):**
6. **Add retention policy** - `HISTORY_RETENTION_PERIOD = 1 YEAR` (model versions)
7. **IMPLEMENT TenantId** - Add explicit TenantId column (currently inherited from Model FK)

**Architectural Justification:**

**Why does this table exist instead of atoms?**
- ✅ **CORRECT: TensorAtomCoefficient maps atoms to POSITIONS in tensors**
- ✅ TensorAtomId FK references actual weight VALUE (stored as atom via TensorAtom → Atom)
- ✅ Positions (X, Y, Z) are STRUCTURAL METADATA (not source content)
- ✅ SpatialKey is COMPUTED from positions (PERSISTED computed column)
- ✅ This is INDEXING STRUCTURE for tensor traversal (not source content)

**What IS an atom in this context?**
- ✅ The actual weight VALUE (float32, stored in Atom table)
- ✅ TensorAtomCoefficient.TensorAtomId → TensorAtom.AtomId → Atom.AtomicValue (weight)

**What MUST NOT be atoms?**
- ✅ TensorAtomId (FK to TensorAtom, which references Atom - CORRECT)
- ✅ ModelId, LayerIdx (internal relationships/metadata)
- ✅ PositionX, PositionY, PositionZ (STRUCTURAL METADATA, tensor coordinates)
- ✅ SpatialKey (COMPUTED from positions, derived data)
- ✅ ValidFrom, ValidTo (temporal versioning, internal metadata)

**Architecture is CORRECT:**
1. Weight value (e.g., 0.732) → Stored in **Atom table** (Modality='weight', AtomicValue=0.732)
2. TensorAtom → Maps Atom to tensor context (ModelId, LayerId, spatial signature)
3. TensorAtomCoefficient → Maps TensorAtom to EXACT POSITION in tensor (X, Y, Z coordinates)

**Data flow:**
```
Source: model.safetensors (100M weights)
↓
Atomization: Each weight → Atom (100M atoms)
↓
TensorAtom: Each atom → tensor context (100M TensorAtoms)
↓
TensorAtomCoefficient: Each TensorAtom → position (100M coefficients)
```

**Why separate tables?**
- ✅ Atom: Content-addressable storage (deduplication via ContentHash)
- ✅ TensorAtom: Tensor-specific metadata (ModelId, LayerId, spatial signature)
- ✅ TensorAtomCoefficient: Positional indexing (X, Y, Z coordinates) + temporal versioning

**required implementation (WRONG):**
❌ Store (X, Y, Z, weight) as single atom
- Breaks deduplication (same weight at different positions = different atoms)
- Violates content-addressable principle (position is not content)
- Loses atomicity (position+value is composite, not atomic)

**IMPLEMENT:**
- ✅ **KEEP CURRENT ARCHITECTURE** - Separation of concerns is correct
- ✅ **ADD MISSING OPTIMIZATIONS** (partitioning, history columnstore)
- ✅ **CLEAN UP DEPRECATED COLUMNS** (after migration)

**Notes:**
- This is **ARCHITECTURAL EXCELLENCE** - Rare nonclustered columnstore on temporal table
- Partitioning is CRITICAL (expected billions of rows)
- History table MUST have clustered columnstore (verify and add)
- Deprecated columns cleanup pending (migration strategy)
- **ARCHITECTURE IS CORRECT** - Weight values stored as atoms, positions stored as structure

---

## 5. dbo.ModelMetadata

**Location:** `src/Hartonomous.Database/Tables/dbo.ModelMetadata.sql`  
**Type:** Model Metadata Extension Table  
**Lines:** ~20  
**Quality Score:** 70/100

### Purpose
Extended metadata for AI models. Stores supported tasks, modalities, performance metrics, and training information.

### Schema

**Primary Key:** MetadataId (INT IDENTITY)  
**Foreign Key:** ModelId → Model (CASCADE DELETE)

**Core Columns:**
- `MetadataId` - INT IDENTITY - Primary key
- `ModelId` - INT NOT NULL - FK to Model (1:1 relationship)
- `SupportedTasks` - JSON NULL - Supported tasks (['text-generation', 'qa', ...])
- `SupportedModalities` - JSON NULL - Supported modalities (['text', 'image', ...])
- `MaxInputLength` - INT NULL - Max input tokens/size
- `MaxOutputLength` - INT NULL - Max output tokens/size
- `EmbeddingDimension` - INT NULL - Embedding dimension (768, 1536, etc.)
- `PerformanceMetrics` - JSON NULL - Benchmark metrics
- `TrainingDataset` - NVARCHAR(500) NULL - Training dataset name
- `TrainingDate` - DATE NULL - Training date
- `License` - NVARCHAR(100) NULL - License type
- `SourceUrl` - NVARCHAR(500) NULL - Source URL

### Indexes

**Clustered:** PK_ModelMetadata (MetadataId)

**Missing Indexes:**
- ❌ No unique constraint on ModelId (MUST be 1:1)
- ❌ No index on ModelId (foreign key lookups)
- ❌ No index on EmbeddingDimension (common filter)
- ❌ No index on License (license-based queries)

### Quality Assessment

**Strengths:**
- ✅ **Native JSON columns** - SupportedTasks, SupportedModalities, PerformanceMetrics
- ✅ **CASCADE DELETE** - ModelId FK cascades
- ✅ **Clean schema** - Focused on metadata
- ✅ **Flexible structure** - JSON for extensibility

**Weaknesses:**
- 🔴 **No unique constraint on ModelId** - MUST be 1:1 (one metadata per model)
- 🔴 **No indexes** - Only clustered PK, missing FK index
- 🔴 **No TenantId** - Multi-tenancy missing (inherited from Model FK but not explicit)
- ⚠️ **Separate table** - MUST be merged into Model table (avoid joins)
- ⚠️ **TrainingDataset NVARCHAR(500)** - MUST be large (MUST be separate table?)
- ⚠️ **SourceUrl NVARCHAR(500)** - MUST be longer (URLs can exceed 500 chars)
- ⚠️ **No CreatedAt/ModifiedAt** - Audit trail missing

**Performance:**
- 🔴 Missing FK index on ModelId (slow joins)
- ⚠️ Small table - Performance not critical (<10K rows expected)

**Security:**
- ⚠️ No TenantId (inherited from Model FK)

### Advanced SQL Server Capabilities Analysis

**Memory-Optimized Tables (OLTP/Hekaton):**
- 🔴 **NOT memory-optimized** - Low priority candidate
- **Candidate Score: 30/100** - Low benefit
  - Reasons: Small table, infrequent updates, JSON columns (limited support)
  - **IMPLEMENT:** Keep disk-based (low priority)

**Columnstore Indexes:**
- ❌ **No columnstore** - Not suitable (OLTP, small table)
- ✅ Row-based correct

**Table Compression:**
- 🔴 **No compression specified** (defaults to NONE)
- ✅ **Good candidate for PAGE compression** - JSON compresses well
- **IMPLEMENT:** `ALTER TABLE dbo.ModelMetadata REBUILD WITH (DATA_COMPRESSION = PAGE)`
  - Estimated savings: 40-50% (JSON, NVARCHAR compress well)

**Partitioning:**
- ❌ **Not partitioned** - Not needed (small table)
- **Candidate Score: 10/100** - Not applicable

**JSON Optimization:**
- ✅ Native JSON columns (SupportedTasks, SupportedModalities, PerformanceMetrics)
- ❌ **No JSON indexes** - Common paths MUST be indexed
- **IMPLEMENT:** Add computed columns for common JSON paths
  - `ALTER TABLE dbo.ModelMetadata ADD PrimaryTask AS JSON_VALUE(SupportedTasks, '$[0]') PERSISTED`
  - `CREATE INDEX IX_ModelMetadata_PrimaryTask ON dbo.ModelMetadata(PrimaryTask) WHERE PrimaryTask IS NOT NULL`

**Query Performance:**
- 🔴 **Missing unique constraint on ModelId** - MUST enforce 1:1
- 🔴 **Missing FK index** - ModelId
- **IMPLEMENT:**
  - `CREATE UNIQUE INDEX IX_ModelMetadata_ModelId ON dbo.ModelMetadata(ModelId)`
  - `CREATE INDEX IX_ModelMetadata_EmbeddingDimension ON dbo.ModelMetadata(EmbeddingDimension) WHERE EmbeddingDimension IS NOT NULL`

**Intelligent Query Processing (IQP):**
- ✅ Compatible with IQP features (SQL Server 2025)
- ⚠️ **Small table** - IQP benefits minimal

**Schema Consolidation:**
- ⚠️ **IMPLEMENT merging into Model table** - Avoid joins
  - Pros: Eliminate join, simpler queries
  - Cons: Wider Model table, more NULL columns
  - **IMPLEMENT:** Keep separate (cleaner design, metadata is optional)

### REQUIRED FIXES

**CRITICAL - Constraints (Priority 1):**
1. **Add unique constraint on ModelId** - Enforce 1:1 relationship
   - `CREATE UNIQUE INDEX IX_ModelMetadata_ModelId ON dbo.ModelMetadata(ModelId)`

**Indexes (Priority 1):**
2. **Add FK index** - ModelId (created by unique constraint above)
3. **Add EmbeddingDimension index** - Common filter

**Compression (Priority 1):**
4. **Enable PAGE compression** - `ALTER TABLE dbo.ModelMetadata REBUILD WITH (DATA_COMPRESSION = PAGE)`
   - 40-50% savings (JSON compresses well)

**Schema (Priority 2):**
5. **Add CreatedAt/ModifiedAt** - Audit trail
6. **Add TenantId** - Explicit multi-tenancy (or document inherited from Model)
7. **Increase SourceUrl length** - NVARCHAR(1000) or NVARCHAR(MAX)

**JSON Optimization (Priority 2):**
8. **Add JSON computed columns** - PrimaryTask, PrimaryModality
9. **Add JSON indexes** - Common JSON paths

**Schema Consolidation (Priority 3):**
10. **IMPLEMENT merging into Model** - Eliminate join (design decision)

**Architectural Justification:**

**Why does this table exist instead of atoms?**
- 🤔 **QUESTIONABLE: Model metadata MUST be atoms**
- ❌ SupportedTasks JSON (MUST be atomized)
- ❌ SupportedModalities JSON (MUST be atomized)
- ❌ MaxInputLength, MaxOutputLength (small integers, MUST be atoms)
- ❌ EmbeddingDimension (small integer, MUST be atom)
- ❌ PerformanceMetrics JSON (MUST be atomized)
- ❌ TrainingDataset (text, MUST be atom)
- ❌ TrainingDate (date, MUST be atom)
- ❌ License (text, MUST be atom)
- ❌ SourceUrl (text, MUST be atom)

**What MUST be atoms?**
- ✅ ALL of the above (TrainingDataset, License, SourceUrl are text)
- ✅ SupportedTasks/Modalities JSON (atomize → atoms + compositions)
- ✅ PerformanceMetrics JSON (atomize → atoms + compositions)
- ✅ Scalar values (MaxInputLength, EmbeddingDimension) MUST be atoms

**What MUST NOT be atoms?**
- ✅ MetadataId (identity, internal key)
- ✅ ModelId (FK, internal relationship)

**IMPLEMENT:**
- 🔄 **REFACTOR: Store ALL metadata AS atoms**
  - This table MUST NOT exist (merge into Model as atoms)
  - Model.ModelAtomId → Atom containing ALL model metadata (including this table's data)
- ✅ **OR: Use AtomComposition to link Model → metadata atoms**
  - Model → Parent atom
  - TrainingDataset → Component atom
  - License → Component atom
  - PerformanceMetrics → Component atoms (atomized JSON)

**Revised architecture proposal:**
```sql
-- ELIMINATE dbo.ModelMetadata table entirely
-- Store all metadata as atoms, linked via AtomComposition

-- Example:
-- Atom 1: ModelName="gpt-4"
-- Atom 2: TrainingDataset="web-crawl-2023"
-- Atom 3: License="MIT"
-- Atom 4: SupportedTasks='["text-generation","qa"]' (or atomize further)
-- AtomComposition:
--   Parent="gpt-4 metadata" atom
--   Components=[Atom1, Atom2, Atom3, Atom4, ...]
-- Model.ModelAtomId = "gpt-4 metadata" atom
```

**Why this table exists (likely reasons):**
1. **Legacy/transition** - Easier to query during migration
2. **Performance** - Avoid joins to Atom table (but this is anti-pattern)
3. **Convenience** - Typed columns easier to query than atoms
4. **NOT JUSTIFIED** - All data is source content, MUST be atoms

**CRITICAL ISSUE:**
- 🔴 **ARCHITECTURAL VIOLATION** - This table stores source content (text, JSON) that MUST be atoms
- 🔴 **DUPLICATE STORAGE** - Metadata likely duplicated (not deduplicated like atoms)
- 🔴 **NO CONTENT-ADDRESSABLE STORAGE** - Can't deduplicate identical metadata across models

**Notes:**
- Unique constraint on ModelId is CRITICAL (enforce 1:1)
- Small table - Performance not critical but indexes still important
- JSON compression benefits significant (40-50%)
- **ARCHITECTURAL DEBT:** This table MUST be eliminated, data stored as atoms

---

## Summary Statistics

**Files Analyzed:** 5  
**Total Lines:** ~180  
**Average Quality:** 75.2/100

**Quality Distribution:**
- Excellent (85-100): 1 file (TensorAtomCoefficient 88⭐)
- Good (70-84): 3 files (Model 78, ModelLayer 72, ModelMetadata 70)
- Fair (60-69): 1 file (TensorAtom 68)

**Key Patterns:**
- **Model decomposition** - Model → ModelLayer → TensorAtom → TensorAtomCoefficient
- **Spatial representation** - GEOMETRY columns for unified queries
- **Temporal versioning** - TensorAtomCoefficient (system-versioned)
- **JSON metadata** - Config, Parameters, Metadata (SQL Server 2025 native)
- **Performance tracking** - UsageCount, CacheHitRate, AvgComputeTimeMs

**Security Issues:**
- 🔴 **ModelLayer missing TenantId** - CRITICAL (cross-tenant layer leakage)
- 🔴 **TensorAtom missing TenantId** - CRITICAL (cross-tenant tensor leakage)
- ⚠️ ModelMetadata, TensorAtomCoefficient missing explicit TenantId (inherited from Model FK)

**Performance Issues:**
- 🔴 **Model: Missing indexes** - ModelName, TenantId, IsActive, LastUsed
- 🔴 **ModelLayer: Missing indexes** - ModelId FK, (ModelId, LayerIdx), spatial, MortonCode
- 🔴 **TensorAtom: Missing indexes** - AtomId FK, (ModelId, LayerId), spatial (2 indexes)
- 🔴 **ModelMetadata: Missing unique constraint** - ModelId (1:1 relationship)
- 🔴 **TensorAtomCoefficient history: Missing columnstore** - MUST have clustered columnstore

**Schema Issues:**
- 🔴 **ModelLayer, TensorAtom missing TenantId** (security + correctness)
- 🔴 **Model: SerializedModel VARBINARY(MAX)** - MUST use FILESTREAM
- 🔴 **TensorAtom: ModelId NULL, LayerId NULL** - MUST be NOT NULL (orphaned atoms)
- ⚠️ No unique constraints on natural keys (ModelLayer, TensorAtom)
- ⚠️ TensorAtomCoefficient deprecated columns (cleanup pending)

**Missing Features:**
- FILESTREAM for Model.SerializedModel (large binaries)
- Columnstore on TensorAtomCoefficients_History (compression + analytics)
- JSON computed column indexes (common paths)
- Spatial indexes (ModelLayer, TensorAtom - 3 total)
- MortonCode index (ModelLayer)

**Critical Issues:**
1. ModelLayer, TensorAtom missing TenantId (SECURITY - cross-tenant leakage)
2. Model missing indexes (ModelName, TenantId, IsActive, LastUsed)
3. Model.SerializedModel MUST use FILESTREAM (large binary bloat)
4. TensorAtomCoefficients_History missing columnstore (compression + analytics)
5. TensorAtom.ModelId/LayerId MUST be NOT NULL (orphaned atoms)
6. Missing spatial indexes (3 tables, 4 GEOMETRY columns total)

**Recommendations:**
1. **CRITICAL:** Add TenantId to ModelLayer, TensorAtom (Priority 1 - Security)
2. **CRITICAL:** Add missing indexes (Model, ModelLayer, TensorAtom, ModelMetadata)
3. **CRITICAL:** Convert Model.SerializedModel to FILESTREAM (Priority 1 - Performance)
4. **CRITICAL:** Add clustered columnstore to TensorAtomCoefficients_History
5. **HIGH:** Partition TensorAtomCoefficient by ModelId (expected billions of rows)
6. **HIGH:** Add spatial indexes (ModelLayer, TensorAtom - 4 total)
7. **MEDIUM:** Enable PAGE/ROW compression on all tables
8. **MEDIUM:** Add JSON computed column indexes (common paths)

**Memory-Optimized (Hekaton) Candidates:**
1. **dbo.Model** (60/100) - Moderate candidate (move SerializedModel to separate table first)
2. **dbo.ModelLayer** (65/100) - Moderate candidate (move WeightsGeometry to separate table)
3. **dbo.TensorAtom** (40/100) - Low candidate (large table, GEOMETRY blocker)
4. **dbo.ModelMetadata** (30/100) - Low priority (small table)
5. **dbo.TensorAtomCoefficient** (10/100) - Not applicable (temporal + columnstore)

**Columnstore Candidates:**
1. 🥇 **TensorAtomCoefficients_History** (100/100) - CRITICAL - Missing clustered columnstore
2. 🥈 **dbo.TensorAtomCoefficient** (95/100) - Already has nonclustered columnstore ✅
3. **dbo.TensorAtom** (70/100) - Add nonclustered columnstore for analytics

**Partitioning Candidates:**
1. 🥇 **dbo.TensorAtomCoefficient** (95/100) - CRITICAL - Billion-row table, partition by ModelId
2. 🥈 **dbo.TensorAtom** (85/100) - Million-row table, partition by LayerId
3. **dbo.ModelLayer** (70/100) - Partition by ModelId (only if >1M rows)

**Compression Priorities:**
1. 🥇 **dbo.Model** - PAGE compression (40-50% savings, JSON compresses well)
2. 🥇 **dbo.ModelMetadata** - PAGE compression (40-50% savings, JSON compresses well)
3. **dbo.ModelLayer** - ROW compression (20-30% savings, GEOMETRY doesn't compress)
4. **dbo.TensorAtom** - ROW compression (20-30% savings, GEOMETRY doesn't compress)
5. **dbo.TensorAtomCoefficient** - Already compressed (columnstore) ✅

**Gold Standards:**
- ✅ **dbo.TensorAtomCoefficient (88/100)** - Excellent: Nonclustered columnstore on temporal table (rare pattern)

---

## Part 22 Completion

**Progress Update:**
- Total SQL files: 325
- Analyzed through Part 22: 159 files (48.9%)
- Remaining: 166 files (51.1%)

**Table Audit Status:**
- ✅ Core atom tables (Part 21, 5 files)
- ✅ Model/tensor tables (Part 22, 5 files)
- ⏳ Remaining tables: ~77 files

**Next Steps:**
- Part 23: Billing tables (5 files)
- Part 24: Provenance/Graph tables (5 files)
- Part 25-35: Remaining tables, functions, Service Broker, indexes, scripts

**Major Findings Summary (Parts 1-22):**
1. 🔴 **AtomComposition missing TenantId** - CRITICAL security (Part 21)
2. 🔴 **ModelLayer, TensorAtom missing TenantId** - CRITICAL security (Part 22)
3. 🔴 **AtomHistory comment/code mismatch** - Claims columnstore but isn't (Part 21)
4. 🔴 **TensorAtomCoefficients_History missing columnstore** - CRITICAL missing optimization (Part 22)
5. 🔴 **Model.SerializedModel MUST use FILESTREAM** - Large binary bloat (Part 22)
6. ⚠️ Multiple tables missing critical indexes (spatial, FK, natural keys)
7. ⚠️ Multiple tables missing compression (PAGE/ROW)
8. ⚠️ Multiple tables missing partitioning (billion-row candidates)
