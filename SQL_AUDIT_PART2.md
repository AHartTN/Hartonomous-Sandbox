# HARTONOMOUS SQL DATABASE PROJECT - COMPREHENSIVE AUDIT PART 2
**Generated:** 2025-11-19 23:59:30  
**Continuation of:** SQL_AUDIT_PART1.md  

---

## PART 2: MODEL INGESTION & TENSOR STORAGE

### TABLE 4: dbo.Model
**File:** Tables/dbo.Model.sql  
**Lines:** 28  
**Purpose:** AI model metadata registry

**Schema Analysis:**
- **Primary Key:** ModelId (INT IDENTITY)
- **Multi-Tenancy:** TenantId (default 0)
- **Usage Tracking:** LastUsed, UsageCount, AverageInferenceMs

**Columns (16 total):**
1. ModelId - Identity PK
2. ModelName - NVARCHAR(200) - Model identifier
3. ModelType - NVARCHAR(100) - 'transformer', 'diffusion', 'encoder', etc.
4. ModelVersion - NVARCHAR(50) - Semantic versioning
5. Architecture - NVARCHAR(100) - 'GPT-3', 'BERT', 'Stable Diffusion', etc.
6. **Config - JSON** - Native JSON for model configuration (SQL 2025)
7. ParameterCount - BIGINT - Total model parameters
8. IngestionDate - DATETIME2(7)
9. CreatedAt - DATETIME2(7)
10. LastUsed - DATETIME2(7) - Last inference timestamp
11. UsageCount - BIGINT - Total inference count
12. AverageInferenceMs - FLOAT - Performance metric
13. TenantId - INT - Multi-tenant isolation
14. IsActive - BIT - Active version flag
15. **MetadataJson - JSON** - Extensible metadata
16. SerializedModel - VARBINARY(MAX) - Model binary (for small models)

**Indexes:**
- PK_Model: Clustered on ModelId
- (From separate index files):
  - IX_Model_TenantId_LastUsed
  - IX_Model_IsActive_ModelType
  - IX_Models_ModelType
  - IX_Models_ModelName

**Quality Assessment: 88/100** ✅
- Good metadata structure
- Native JSON support
- Usage tracking built-in
- Missing: No FILESTREAM for large models (>2GB VARBINARY limit)

**Dependencies:**
- Referenced by: ModelLayer, TensorAtom, TensorAtomCoefficient, AtomEmbedding
- Referenced by procedures: sp_IngestModel, sp_RunInference

**Issues Found:**
- ⚠️ SerializedModel VARBINARY(MAX) limited to 2GB
- ⚠️ No FILESTREAM column for large models (70B+ parameters)
- Minor: No ModelHash for integrity validation

---

### TABLE 5: dbo.ModelLayer
**File:** Tables/dbo.ModelLayer.sql  
**Lines:** 28  
**Purpose:** Model layer metadata with spatial representation

**Schema Analysis:**
- **Primary Key:** LayerId (BIGINT IDENTITY)
- **Foreign Keys:**
  - FK to Model (CASCADE DELETE)
  - FK to Atom (SET NULL)
- **Spatial:** WeightsGeometry GEOMETRY for layer visualization

**Columns (22 total):**
1. LayerId - Identity PK
2. ModelId - FK to Model (CASCADE)
3. LayerIdx - INT - Layer sequence position
4. LayerName - NVARCHAR(100) - 'attention.0', 'mlp.dense', etc.
5. LayerType - NVARCHAR(50) - 'Linear', 'Conv2D', 'Attention', etc.
6. **WeightsGeometry - GEOMETRY** - Spatial representation of weights
7. TensorShape - NVARCHAR(200) - '[768, 3072]', '[12, 64, 64]', etc.
8. TensorDtype - NVARCHAR(20) - 'float32', 'float16', 'int8', etc.
9. QuantizationType - NVARCHAR(20) - 'none', 'int8', 'int4', etc.
10. QuantizationScale - FLOAT - Quantization scaling factor
11. QuantizationZeroPoint - FLOAT - Quantization offset
12. **Parameters - JSON** - Layer configuration
13. ParameterCount - BIGINT - Layer parameter count
14. ZMin/ZMax - FLOAT - Spatial bounds
15. MMin/MMax - FLOAT - 4D spatial bounds (XYZM)
16. MortonCode - BIGINT - Z-order curve for spatial indexing
17. PreviewPointCount - INT - For visualization
18. CacheHitRate - FLOAT - Performance metric
19. AvgComputeTimeMs - FLOAT - Layer inference time
20. LayerAtomId - BIGINT - FK to Atom (optional)

**Indexes:**
- PK_ModelLayer: Clustered on LayerId
- (From separate files):
  - IX_TensorAtom_ModelId_LayerId

**Quality Assessment: 90/100** ✅
- Excellent metadata structure
- Quantization support (int8, int4)
- Spatial representation for visualization
- Performance tracking

**Dependencies:**
- References: Model, Atom
- Referenced by: TensorAtom, TensorAtomCoefficient

**Issues Found:**
- ⚠️ No unique constraint on (ModelId, LayerIdx) - allows duplicate layers
- Minor: MortonCode calculation not automated (should be computed column)
- Minor: No index on ModelId + LayerIdx (common query pattern)

---

### TABLE 6: dbo.TensorAtom
**File:** Tables/dbo.TensorAtom.sql  
**Lines:** 18  
**Purpose:** Atomic tensor elements with spatial signatures

**Schema Analysis:**
- **Primary Key:** TensorAtomId (BIGINT IDENTITY)
- **Foreign Keys:**
  - FK to Atom (CASCADE DELETE)
  - FK to Model
  - FK to ModelLayer

**Columns (10 total):**
1. TensorAtomId - Identity PK
2. AtomId - FK to Atom (CASCADE)
3. ModelId - FK to Model (nullable)
4. LayerId - FK to ModelLayer (nullable)
5. AtomType - NVARCHAR(128) - 'weight', 'bias', 'activation', etc.
6. **SpatialSignature - GEOMETRY** - Spatial representation
7. **GeometryFootprint - GEOMETRY** - Bounding geometry
8. **Metadata - JSON** - Extensible metadata
9. ImportanceScore - REAL - Attention/saliency score
10. CreatedAt - DATETIME2(7)

**Indexes:**
- PK_TensorAtom: Clustered on TensorAtomId
- (From separate files):
  - IX_TensorAtom_ModelId_LayerId
  - IX_TensorAtom_CreatedAt
  - IX_TensorAtom_AtomType

**Quality Assessment: 85/100** ✅
- Good spatial representation
- Importance scoring for pruning
- JSON metadata

**Dependencies:**
- References: Atom, Model, ModelLayer
- Referenced by: TensorAtomCoefficient

**Issues Found:**
- ⚠️ No spatial index on SpatialSignature or GeometryFootprint
- Minor: ImportanceScore not used in any procedures (dead column?)
- Minor: No unique constraint on (AtomId, ModelId, LayerId)

---

### TABLE 7: dbo.TensorAtomCoefficient
**File:** Tables/dbo.TensorAtomCoefficient.sql  
**Lines:** 53  
**Purpose:** Explicit, OLAP-queryable weight mappings with temporal tracking

**Schema Analysis:**
- **Composite Primary Key:** (TensorAtomId, ModelId, LayerIdx, PositionX, PositionY, PositionZ)
- **Temporal:** System-versioned with TensorAtomCoefficients_History
- **Spatial:** Computed SpatialKey GEOMETRY column
- **OLAP:** Non-clustered columnstore index

**Key Innovation:**
This table represents the STRUCTURAL decomposition of neural network weights:
- Each weight coefficient is an ATOM (FK to dbo.Atom)
- Position in tensor: (PositionX, PositionY, PositionZ)
- Layer position: LayerIdx
- Spatial queries: SpatialKey GEOMETRY

**Columns (14 total):**
1. TensorAtomId - FK to Atom (the weight VALUE as an atom)
2. ModelId - FK to Model
3. LayerIdx - INT - Layer position
4. PositionX - INT - Row position in tensor
5. PositionY - INT - Column position in tensor
6. PositionZ - INT - Depth position (for 3D tensors)
7. **SpatialKey - GEOMETRY (COMPUTED)** - geometry::Point([PositionX], [PositionY], 0)
8. ValidFrom/ValidTo - DATETIME2(7) - Temporal tracking

**DEPRECATED Columns (backward compatibility):**
9. TensorAtomCoefficientId - BIGINT (no longer identity)
10. ParentLayerId - BIGINT (use ModelId + LayerIdx instead)
11. TensorRole - NVARCHAR(128) (use positional indexing)
12. Coefficient - REAL (the coefficient IS the atom)

**Indexes (3):**
1. PK_TensorAtomCoefficients: Clustered on composite key
2. **NCCI_TensorAtomCoefficients**: Non-clustered columnstore (OLAP)
3. **SIX_TensorAtomCoefficients_SpatialKey**: Spatial index (BOUNDING_BOX: 0,0 to 10000,10000)

**Temporal:**
- System-versioned with TensorAtomCoefficients_History table
- Enables temporal queries: FOR SYSTEM_TIME AS OF
- Used by: sp_RollbackWeightsToTimestamp

**Quality Assessment: 95/100** ✅
- **EXCEPTIONAL**: Columnar + spatial + temporal
- Perfect for OLAP weight analysis queries
- Temporal tracking for weight rollback
- Proper FK cascades

**Core Algorithm Use:**
`sql
-- Query weights at specific position
SELECT ta.AtomId, ta.AtomicValue
FROM TensorAtomCoefficient tac
JOIN Atom ta ON tac.TensorAtomId = ta.AtomId
WHERE tac.ModelId = @modelId
  AND tac.LayerIdx = @layerIdx
  AND tac.SpatialKey.STIntersects(@searchArea) = 1
`

**Dependencies:**
- References: Atom, Model
- Referenced by: sp_RollbackWeightsToTimestamp, sp_QueryModelWeights

**Issues Found:**
- ⚠️ **SCHEMA MIGRATION**: Deprecated columns should be removed (breaking change)
- ⚠️ TensorAtomCoefficients_History table not found in schema (temporal dependency)
- Minor: BOUNDING_BOX (0,0,10000,10000) may be too small for large models
- Minor: No index on (ModelId, LayerIdx) alone (common filter)

---

### STORED PROCEDURE 4: dbo.sp_IngestModel
**File:** Procedures/dbo.sp_IngestModel.sql  
**Lines:** 120  
**Purpose:** Ingest AI model metadata and binary into database

**Algorithm Breakdown:**

**Phase 1: Validation**
`sql
IF @ModelName IS NULL OR @ModelType IS NULL
    RAISERROR('ModelName and ModelType are required', 16, 1);

IF @ModelBytes IS NULL AND @FileStreamPath IS NULL
    RAISERROR('Either ModelBytes or FileStreamPath must be provided', 16, 1);
`

**Phase 2: Create Model Record**
`sql
INSERT INTO dbo.Model (
    ModelName, ModelType, Architecture,
    Config, ParameterCount, TenantId, IsActive
)
VALUES (@ModelName, @ModelType, @Architecture,
    @ConfigJson, @ParameterCount, @TenantId, 1);

SET @ModelId = SCOPE_IDENTITY();
`

**Phase 3: Store Model Data**
`sql
IF @ModelBytes IS NOT NULL
BEGIN
    -- Small model: Store in varbinary column (max 2GB)
    UPDATE dbo.Model
    SET SerializedModel = @ModelBytes
    WHERE ModelId = @ModelId;
END
ELSE IF @FileStreamPath IS NOT NULL
BEGIN
    -- Large model: File-based storage
    PRINT 'Model file path provided: ' + @FileStreamPath;
    PRINT 'NOTE: File-based model storage not yet implemented.';
    -- TODO: Requires CLR Win32 API integration
END
`

**Phase 4: Version Management**
`sql
IF @SetAsCurrent = 1
BEGIN
    -- Deactivate previous versions
    UPDATE dbo.Model
    SET IsActive = 0
    WHERE ModelName = @ModelName
      AND ModelId != @ModelId
      AND TenantId = @TenantId;
END
`

**Phase 5: Provenance Logging**
`sql
INSERT INTO provenance.ModelVersionHistory (
    ModelId, VersionTag, ChangeDescription, TenantId
)
VALUES (@ModelId, '1.0', 'Initial model ingestion via sp_IngestModel', @TenantId);
`

**Parameters (10):**
1. @ModelName NVARCHAR(200)
2. @ModelType NVARCHAR(50)
3. @Architecture NVARCHAR(100)
4. @ConfigJson NVARCHAR(MAX) - JSON configuration
5. @ModelBytes VARBINARY(MAX) - Model binary (for small models)
6. @FileStreamPath NVARCHAR(500) - File path (for large models)
7. @ParameterCount BIGINT
8. @TenantId INT
9. @SetAsCurrent BIT - Version management
10. @ModelId INT OUTPUT

**Quality Assessment: 82/100** ✅
- Good validation and error handling
- Transaction safety
- Version management
- Provenance tracking

**Dependencies:**
- Tables:
  - Model (exists ✅)
  - provenance.ModelVersionHistory (MISSING ❌)
- No CLR functions required

**Issues Found:**
- ❌ **BLOCKING**: provenance.ModelVersionHistory table not found
- ⚠️ FileStreamPath parameter accepted but NOT IMPLEMENTED
- ⚠️ No model binary validation (could be corrupted)
- ⚠️ ConfigJson not validated (should check JSON syntax)
- Minor: No rollback on provenance insert failure
- Minor: VersionTag hardcoded to '1.0' (should be parameter)

---

### DUPLICATE ISSUE 3: Provenance Tables
**Files Found:**
1. Tables/Provenance.ProvenanceTrackingTables.sql (creates 3 tables)
2. Tables/dbo.OperationProvenance.sql (individual table)
3. Tables/dbo.ProvenanceValidationResults.sql (individual table)
4. Tables/dbo.ProvenanceAuditResults.sql (individual table)
5. Tables/provenance.ModelVersionHistory.sql (expected by sp_IngestModel)
6. Tables/provenance.GenerationStreams.sql
7. Tables/provenance.Concepts.sql
8. Tables/provenance.ConceptEvolution.sql
9. Tables/provenance.AtomGraphEdges.sql
10. Tables/provenance.AtomConcepts.sql

**Analysis:**
Provenance.ProvenanceTrackingTables.sql creates:
`sql
CREATE TABLE dbo.OperationProvenance (...);
CREATE TABLE dbo.ProvenanceValidationResults (...);
CREATE TABLE dbo.ProvenanceAuditResults (...);
`

BUT these also exist as individual files in dbo schema.

Additionally, multiple files create objects in 'provenance' schema, but there's also a 'Provenance' (capital P) legacy file.

**Schema Confusion:**
- Is it 'provenance' or 'Provenance'?
- Are provenance tables in dbo or provenance schema?

**Resolution Required:**
- ❌ DELETE: Provenance.ProvenanceTrackingTables.sql (legacy)
- ✅ KEEP: Individual provenance.* tables
- ❌ FIX: Rename/consolidate schema (provenance vs Provenance)

**Impact:**
- sp_IngestModel references provenance.ModelVersionHistory (not found)
- Multiple duplicate definitions
- Schema namespace collision

---

### DUPLICATE ISSUE 4: Reasoning Framework Tables
**Files Found:**
1. Tables/Reasoning.ReasoningFrameworkTables.sql (creates 3 tables)
2. Tables/dbo.ReasoningChains.sql (individual table)
3. Tables/dbo.SelfConsistencyResults.sql (individual table)
4. Tables/dbo.MultiPathReasoning.sql (individual table)

**Analysis:**
Reasoning.ReasoningFrameworkTables.sql creates:
`sql
CREATE TABLE dbo.ReasoningChains (...);
CREATE TABLE dbo.SelfConsistencyResults (...);
CREATE TABLE dbo.MultiPathReasoning (...);
`

All 3 tables ALSO exist as individual files.

**Differences:**
- Reasoning.ReasoningFrameworkTables.sql: Missing indexes
- Individual files: Have proper indexes and constraints

**Resolution Required:**
- ❌ DELETE: Reasoning.ReasoningFrameworkTables.sql
- ✅ KEEP: Individual table files

**Impact:**
- DACPAC build fails with 3 duplicate table definitions

---

### DUPLICATE ISSUE 5: Stream Orchestration Tables
**Files Found:**
1. Tables/Stream.StreamOrchestrationTables.sql (creates 4 tables)
2. Tables/dbo.StreamOrchestrationResults.sql (individual table)
3. Tables/dbo.StreamFusionResults.sql (individual table)
4. Tables/dbo.EventGenerationResults.sql (individual table)
5. Tables/dbo.EventAtoms.sql (individual table)

**Analysis:**
Stream.StreamOrchestrationTables.sql creates:
`sql
CREATE TABLE dbo.StreamOrchestrationResults (...);
CREATE TABLE dbo.StreamFusionResults (...);
CREATE TABLE dbo.EventGenerationResults (...);
CREATE TABLE dbo.EventAtoms (...);
`

All 4 tables ALSO exist as individual files.

**Resolution Required:**
- ❌ DELETE: Stream.StreamOrchestrationTables.sql
- ✅ KEEP: Individual table files

**Impact:**
- DACPAC build fails with 4 duplicate table definitions

---

### MISSING OBJECTS DISCOVERED (Part 2)

**Tables Referenced but Not Found:**
1. provenance.ModelVersionHistory - Referenced by sp_IngestModel
2. TensorAtomCoefficients_History - Temporal history table for TensorAtomCoefficient
3. InferenceTracking - Referenced by sp_RunInference
4. dbo.seq_InferenceId - Sequence for inference IDs
5. CodeAtom - Referenced by sp_AtomizeCode

**CLR Functions Still Missing:**
1. dbo.clr_VectorAverage - sp_RunInference
2. dbo.clr_CosineSimilarity - sp_FindNearestAtoms
3. dbo.clr_ComputeHilbertValue - sp_FindNearestAtoms
4. dbo.fn_ProjectTo3D - sp_FindNearestAtoms, sp_AtomizeCode
5. dbo.clr_GenerateCodeAstVector - sp_AtomizeCode
6. dbo.clr_ProjectToPoint - sp_AtomizeCode

**SQL Functions Missing:**
1. None discovered in Part 2

---

### PART 2 SUMMARY

**Files Analyzed:** 5 (Model, ModelLayer, TensorAtom, TensorAtomCoefficient, sp_IngestModel)  
**Duplicate Issues Found:** 3 (Provenance, Reasoning, Stream legacy schema files)  
**Missing Tables:** 5  
**Missing CLR Functions:** 6 (total)  
**Schema Quality:** 88/100 average  

**Critical Findings:**
- Temporal tables missing history tables
- Legacy schema files cause 14 duplicate object definitions
- CLR functions not implemented (blocking inference)
- Schema namespace confusion (provenance vs Provenance)

**Next:** Part 3 will analyze remaining procedures, views, and Service Broker infrastructure.

