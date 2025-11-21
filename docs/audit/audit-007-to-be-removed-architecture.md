# Documentation Audit Segment 007: .to-be-removed Architecture Files

**Generated**: 2025-01-XX  
**Scope**: .to-be-removed/architecture/ (18 architecture design documents)  
**Files Sampled**: 6 files (detailed review)  
**Purpose**: Catalog architecture design documents vs. current docs/architecture/

---

## Overview

The .to-be-removed/architecture/ directory contains 18 architecture design documents created during the "rewrite phase" (November 2025). These documents represent comprehensive architectural planning but have significant overlap with current docs/architecture/ files.

**Directory Contents** (18 files):
1. ADVERSARIAL-MODELING-ARCHITECTURE.md
2. ARCHIVE-HANDLER.md (design phase)
3. CATALOG-MANAGER.md (design phase)
4. COGNITIVE-KERNEL-SEEDING.md (bootstrap)
5. COMPLETE-MODEL-PARSERS.md (design phase)
6. END-TO-END-FLOWS.md (7 flows)
7. ENTROPY-GEOMETRY-ARCHITECTURE.md
8. INFERENCE-AND-GENERATION.md
9. MODEL-ATOMIZATION-AND-INGESTION.md
10. MODEL-COMPRESSION-AND-OPTIMIZATION.md (159:1 validated)
11. MODEL-PROVIDER-LAYER.md (design phase)
12. NOVEL-CAPABILITIES-ARCHITECTURE.md
13. OODA-AUTONOMOUS-LOOP-ARCHITECTURE.md
14. SEMANTIC-FIRST-ARCHITECTURE.md
15. SQL-SERVER-2025-INTEGRATION.md (99% validation)
16. TEMPORAL-CAUSALITY-ARCHITECTURE.md
17. TRAINING-AND-FINE-TUNING.md
18. UNIVERSAL-FILE-FORMAT-REGISTRY.md (design phase)

**Status Summary** (per PROJECT-STATUS.md):
- Production Implementation: 10+ files
- Design Phase: 6 files (Archive Handler, Catalog Manager, Complete Model Parsers, Model Provider Layer, Universal File Format Registry)
- Validated: SQL-SERVER-2025-INTEGRATION.md (99%), MODEL-COMPRESSION (159:1)

---

## Detailed File Analysis

### 1. SEMANTIC-FIRST-ARCHITECTURE.md
- **Length**: 547 lines
- **Date**: November 18, 2025
- **Status**: Production Implementation
- **Quality**: ⭐⭐⭐⭐⭐ Excellent - Core innovation explained

**Purpose**:
Explains the paradigm shift: "Filter by semantics BEFORE geometric math" using R-Tree O(log N) → Vector O(K) pattern.

**Key Content**:

**The Problem - Curse of Dimensionality**:
- In 1536D space, all distances become similar
- Example: Distance range only 0.4 (less than 1% variation)
- Brute-force distance computation useless until ALL N distances computed

**The Solution - Three Steps**:
1. **Landmark Projection** (1536D → 3D): `clr_LandmarkProjection_ProjectTo3D` via trilateration
2. **Spatial Indexing**: R-Tree on 3D GEOMETRY with USING GEOMETRY_GRID
3. **Semantic Pre-Filter**: STIntersects with STBuffer for O(log N) lookup

**Code Examples**:
- Complete CLR function: `clr_LandmarkProjection_ProjectTo3D`
- Spatial index creation with BOUNDING_BOX and GRIDS configuration
- A* pathfinding: `sp_GenerateOptimalPath.sql` using STIntersects
- Time complexity analysis: log₂(3.5B) ≈ 31.7 levels, 15-20 node reads

**Performance Claims**:
- 3,500,000× speedup for 3.5B atoms
- O(log N + K·D) where K << N
- STIntersects: O(log N) = 15-20 node reads
- Candidates: K = 100-1000 atoms (from 3.5B)

**Relationship to Current Docs**:
- **OVERLAPS**: docs/architecture/01-semantic-first-architecture.md (same title)
- **DIFFERENCE**: This version has more CLR code examples, detailed time complexity
- Current version may be condensed or restructured

**Value**: CRITICAL - Explains core O(log N) + O(K) innovation with full implementation details

**Recommendation**: COMPARE with current docs/architecture/01-semantic-first-architecture.md
- If current version is condensed: Keep this for implementation details
- If current version is comprehensive: This is historical draft

---

### 2. OODA-AUTONOMOUS-LOOP-ARCHITECTURE.md
- **Length**: 956 lines
- **Date**: November 18, 2025
- **Status**: Production Implementation
- **Quality**: ⭐⭐⭐⭐⭐ Excellent - Complete OODA implementation

**Purpose**:
Complete implementation of OODA loop (Observe-Orient-Decide-Act-Learn) with Service Broker integration and .NET event handlers.

**Key Content**:

**Service Broker Integration**:
- 4 queues: AnalyzeQueue, HypothesizeQueue, ActQueue, LearnQueue
- Activation procedures for automatic message processing
- SQL Server Agent job: Every 15 minutes trigger

**Component Architecture**:
1. **Observe**: System state metrics (DMVs, performance counters)
2. **Orient**: Generate hypotheses (sp_Hypothesize)
3. **Decide**: Priority + risk assessment
4. **Act**: Execute safe improvements automatically, queue dangerous for approval
5. **Learn**: Measure outcomes, update model weights

**Gödel Engine Integration**: "Turing-completeness via AutonomousComputeJobs"

**.NET Event Handlers** (src/Hartonomous.ServiceBroker/OodaEventHandlers.cs):
- ObservationEventHandler: Collect external metrics (CPU, memory, network latency)
- OrientationEventHandler: Generate hypotheses via .NET ML models
- Async patterns for external API calls

**Scheduled Triggers**:
- SQL Server Agent job configuration
- BEGIN DIALOG pattern for Service Broker
- Every 15 minutes execution

**Dual Triggering** (referenced but not detailed):
- Event-driven: Immediate response to critical events
- Time-driven: Periodic 15-minute cycle

**Relationship to Current Docs**:
- **OVERLAPS**: docs/architecture/02-ooda-autonomous-loop.md (same concept)
- **ADDS**: Service Broker implementation details, .NET event handlers, scheduled triggers
- Current version may focus on concepts vs. implementation

**Value**: CRITICAL - Complete OODA implementation with Service Broker and .NET integration

**Recommendation**: MERGE with current docs
- Extract Service Broker patterns for implementation guide
- Extract .NET event handler patterns
- Consolidate OODA concept + implementation

---

### 3. MODEL-ATOMIZATION-AND-INGESTION.md
- **Length**: 743 lines
- **Date**: November 18, 2025
- **Status**: Production Ready
- **Quality**: ⭐⭐⭐⭐⭐ Excellent - Complete three-stage pipeline

**Purpose**:
Transform monolithic neural network files into atomic, deduplicated, spatially-indexed tensors.

**Key Innovations**:
1. **Content-Addressable Storage (CAS)**: SHA2_256 hashing, identical weights share storage
2. **Spatial Projection**: Weights become GEOMETRY points via trilateration
3. **Hilbert Indexing**: Z-order curves for O(log N) queries
4. **SVD Compression**: Rank-k decomposition (75% reduction)
5. **Governed Ingestion**: Chunk-based processing with quota enforcement

**Three-Stage Pipeline**:
```
Stage 1: PARSE → Extract tensors from binary formats
Stage 2: ATOMIZE → Deduplicate weights, create TensorAtoms
Stage 3: SPATIALIZE → Project to 3D, compute Hilbert indices
```

**Supported Formats** (6):
- GGUF (.gguf): Quantized LLMs via GGUFParser.cs
- PyTorch (.pt, .pth): ZIP/pickle format (LIMITED support)
- ONNX (.onnx): Protobuf via ONNXParser.cs (lightweight)
- TensorFlow (.pb, .h5): SavedModel protobuf
- SafeTensors (.safetensors): Hugging Face (RECOMMENDED for PyTorch)
- Stable Diffusion: UNet/VAE/TextEncoder variant detection

**CLR Implementation**:
- TensorInfo.cs (3,521 lines): Unified tensor metadata
- ModelMetadata.cs: Format-agnostic structure
- clr_ExtractModelWeights: SQL UDF wrapper

**Data Flow**:
GGUF/PyTorch/ONNX File → clr_ExtractModelWeights() → clr_StreamAtomicWeights_Chunked() → 
sp_AtomizeModel_Governed() → clr_ProjectTo3D() → clr_ComputeHilbertValue() → dbo.AtomEmbedding

**Relationship to Current Docs**:
- **OVERLAPS**: docs_old/examples/model-ingestion.md (different focus)
- **DIFFERENT FROM**: docs_old/ingestion/README.md (64-byte atoms vs. tensor atomization)
- **NO MATCH**: Current docs/implementation/ has 01-sql-schema.md, 02-t-sql-pipelines.md, 03-sql-clr-functions.md

**Value**: CRITICAL - Complete model ingestion architecture with format parsers

**Recommendation**: EVALUATE against current docs
- If current docs/implementation/ doesn't cover model ingestion: PROMOTE
- If covered elsewhere: ARCHIVE as design document
- **POTENTIAL CONFLICT**: 64-byte atom limit (ingestion/README.md) vs. tensor atomization

---

### 4. ENTROPY-GEOMETRY-ARCHITECTURE.md
- **Length**: 545 lines
- **Date**: November 18, 2025
- **Status**: Production Implementation
- **Quality**: ⭐⭐⭐⭐⭐ Excellent - SVD and manifold clustering

**Purpose**:
Entropy reduction via SVD (Singular Value Decomposition) creating "Strange Attractors" in manifold space.

**Key Concepts**:

**Entropy Reduction via SVD**:
- High-dimensional embedding space (1536D/1998D) has high entropy
- SVD decomposition: X = U · Σ · V^T
- Rank k << D: Compress to 64-256 dimensions
- Explained variance: Rank 64 = 85-90%, Rank 128 = 92-95%, Rank 256 = 96-98%

**Applications**:
1. **Manifold attacks** on cryptographic operations (semantic_key_mining.sql)
2. **Model compression** (64:1 ratio via rank-64 SVD)
3. **Anomaly detection** via manifold distance metrics
4. **Concept discovery** via cluster analysis

**CLR Implementation** (SVDGeometryFunctions.cs):
- clr_SvdDecompose: Decompose tensor weights
- clr_ReconstructFromSVD: Reconstruct from compressed components
- Compression ratio example: 67.1 MB → 2.1 MB = 31.9:1

**Mathematical Foundation**:
- Entropy formula: H(X) = sum(-p_i * log(p_i))
- Entropy reduction: ΔH = H(X) - H(X_k)
- Variance explained: (Σ_k²) / (Σ_total²)

**Usage Pattern**:
```sql
DECLARE @svdResult = dbo.clr_SvdDecompose(@layerWeights, 4096, 4096, 64);
INSERT INTO dbo.SVDCompressedLayers (U_Matrix, S_Vector, VT_Matrix, ExplainedVariance);
```

**Relationship to Current Docs**:
- **UNIQUE**: No equivalent in current docs/architecture/
- **RELATED**: docs/architecture/03-entropy-geometry.md exists (same title)
- Need to compare content

**Value**: CRITICAL - SVD compression and manifold clustering implementation

**Recommendation**: COMPARE with docs/architecture/03-entropy-geometry.md
- If current version lacks CLR implementation: MERGE implementation details
- If equivalent: ARCHIVE as design document

---

### 5. SQL-SERVER-2025-INTEGRATION.md
- **Length**: 859 lines
- **Date**: November 18, 2025
- **Status**: Design Phase (99% validation per DOCUMENTATION-AUDIT)
- **Quality**: ⭐⭐⭐⭐⭐ Excellent - Selective vector type usage

**Purpose**:
Integrate SQL Server 2025 native features WHILE preserving existing geometric AI architecture.

**Key Principles**:
1. **Preserve Geometric AI**: R-Tree, Gram-Schmidt, Hilbert curves, landmark projection UNCHANGED
2. **Selective Vector Usage**: Use vector type ONLY for external embeddings (OpenAI/Azure)
3. **sp_invoke_external_rest_endpoint**: Primary method for external API calls
4. **SQL Service Broker**: Alternative for async patterns
5. **Row-Level Security**: Multi-tenant isolation
6. **Native JSON**: Use json type for metadata

**Vector Type DO/DON'T**:

**DO Use Vector Type For**:
- External model embeddings (OpenAI, Azure)
- Similarity search on existing embeddings
- Hybrid search (keyword + vector)
- Cross-modal search using external embeddings

**DO NOT Use Vector Type For**:
- Geometric AI (1998D landmark projection)
- Spatial indexing (R-Tree queries)
- Gram-Schmidt orthogonalization
- Hilbert curves

**Schema Coexistence**:
```sql
-- EXISTING: ModelEmbeddings (1998 FLOAT columns, SpatialHash, HilbertCurveIndex)
-- NEW: ExternalEmbeddings (vector(1536), vector(3072), Provider, Model)
```

**Hybrid Query Pattern**:
- Combine geometric AI similarity (CLR function)
- With external embedding similarity (VECTOR_DISTANCE)
- Weighted score: 70% geometric + 30% external

**Vector Type Functions**:
- VECTOR_DISTANCE('cosine', @vec1, @vec2)
- VECTOR_DISTANCE('dot', ...) 
- VECTOR_DISTANCE('euclidean', ...)
- VECTOR_NORMALIZE, VECTOR_ADD, VECTOR_SUBTRACT

**Relationship to Current Docs**:
- **UNIQUE**: No SQL Server 2025 integration in current docs/
- **CRITICAL INSIGHT**: Vector type is ADDITIVE, not replacement
- Preserves existing geometric AI architecture

**Value**: CRITICAL - Design for SQL Server 2025 native vector type integration

**Recommendation**: PROMOTE to docs/architecture/
- Add as SQL-SERVER-2025-INTEGRATION.md
- Reference in architecture principles
- Critical for understanding vector type strategy

---

### 6. NOVEL-CAPABILITIES-ARCHITECTURE.md
- **Length**: 642 lines
- **Date**: November 18, 2025
- **Status**: Production Implementation
- **Quality**: ⭐⭐⭐⭐⭐ Excellent - Unique differentiators

**Purpose**:
Document capabilities "impossible with conventional AI systems" enabled by Hartonomous architecture.

**Six Novel Capabilities**:

1. **Cross-Modal Semantic Queries**:
   - Text → Audio: Find audio clips matching text descriptions
   - Image → Code: Find code implementing UI mockups
   - Audio → Text: Transcription-free semantic matching
   - Multi-Hop: Image → Code → Text documentation

2. **Behavioral Analysis as GEOMETRY**:
   - User sessions as LINESTRING paths through semantic space
   - Error clustering via geometric analysis
   - Session complexity metrics

3. **Synthesis AND Retrieval**:
   - Generate new content while retrieving similar existing content
   - Same operation, not separate steps

4. **Audio Generation from Spatial Coordinates**:
   - clr_GenerateHarmonicTone creates audio from 3D semantic position
   - Novel: Audio synthesis guided by geometric location

5. **Temporal Cross-Modal**:
   - Query historical embeddings across modalities
   - Example: "audio similar to this text in November 2024"
   - System-versioned tables + Neo4j provenance

6. **Manifold Interpolation**:
   - Barycentric coordinates for weighted concept blending
   - Example: 70% concept A + 30% concept B

**Implementation Examples**:
- Complete SQL for Text → Audio (3-step: embed → project → spatial pre-filter)
- Image → Code with CLIP embeddings
- Audio → Text with Wav2Vec2 embeddings
- Performance: 18-25ms for 3.5B atoms

**Why This Works**:
- Semantic-first architecture: O(log N) spatial pre-filter
- Unified embedding space: All modalities in same 1536D → 3D
- Temporal causality: System-versioned tables
- Entropy geometry: SVD preserves cross-modal structure

**Relationship to Current Docs**:
- **OVERLAPS**: docs_old/examples/cross-modal-queries.md (detailed implementation)
- **OVERLAPS**: docs_old/examples/behavioral-analysis.md (user session geometry)
- **UNIQUE**: Synthesis + Retrieval, Audio generation, Temporal cross-modal
- **NO MATCH**: Current docs/ doesn't have "novel capabilities" document

**Value**: CRITICAL - Marketing + technical differentiation

**Recommendation**: PROMOTE to docs/features/
- Create docs/features/novel-capabilities.md
- Reference in architecture overview
- Essential for understanding unique value proposition

---

## Remaining Files (Brief Summary)

**Not Individually Catalogued** (12 files):

**ADVERSARIAL-MODELING-ARCHITECTURE.md**: Adversarial attack modeling  
**ARCHIVE-HANDLER.md**: Archive file processing (design phase)  
**CATALOG-MANAGER.md**: Catalog management system (design phase)  
**COGNITIVE-KERNEL-SEEDING.md**: Bootstrap system initialization  
**COMPLETE-MODEL-PARSERS.md**: Model parser completion (design phase)  
**END-TO-END-FLOWS.md**: 7 operational flows  
**INFERENCE-AND-GENERATION.md**: Spatial generation  
**MODEL-COMPRESSION-AND-OPTIMIZATION.md**: 159:1 compression (validated)  
**MODEL-PROVIDER-LAYER.md**: Model provider abstraction (design phase)  
**TEMPORAL-CAUSALITY-ARCHITECTURE.md**: Temporal queries  
**TRAINING-AND-FINE-TUNING.md**: Geometric gradient descent  
**UNIVERSAL-FILE-FORMAT-REGISTRY.md**: File format registry (design phase)  

---

## Cross-File Analysis

### Overlap with Current docs/architecture/

**Current docs/architecture/** (from workspace structure):
- 00-principles.md
- 01-semantic-first-architecture.md
- 02-ooda-autonomous-loop.md
- 03-entropy-geometry.md

**Comparison**:

| .to-be-removed/architecture/ | Current docs/architecture/ | Status |
|------------------------------|----------------------------|--------|
| SEMANTIC-FIRST-ARCHITECTURE.md | 01-semantic-first-architecture.md | OVERLAP - Need comparison |
| OODA-AUTONOMOUS-LOOP-ARCHITECTURE.md | 02-ooda-autonomous-loop.md | OVERLAP - Need comparison |
| ENTROPY-GEOMETRY-ARCHITECTURE.md | 03-entropy-geometry.md | OVERLAP - Need comparison |
| SQL-SERVER-2025-INTEGRATION.md | (none) | UNIQUE - Should promote |
| NOVEL-CAPABILITIES-ARCHITECTURE.md | (none) | UNIQUE - Should promote |
| MODEL-ATOMIZATION-AND-INGESTION.md | (none - implementation/) | Different scope |

**Analysis**:
- **3 files have matching titles** in current docs/architecture/
- **2 files are unique** and should be promoted
- **Need to compare** 3 overlapping files to determine if .to-be-removed/ versions have additional value

### Common Themes

**All Architecture Files Share**:
- Date: November 18, 2025
- O(log N) + O(K) pattern references
- Spatial indexing with R-Tree
- Landmark projection (1536D → 3D)
- CLR function references
- SQL Server implementation focus

**Design Patterns**:
- Service Broker for async messaging
- CLR functions for geometric computation
- System-versioned tables for temporal queries
- Neo4j for provenance tracking
- Multi-tenant isolation via Row-Level Security

### Quality Assessment

**Highest Quality** (⭐⭐⭐⭐⭐) - All 6 sampled files:
- Complete implementations with CLR code
- SQL examples with real queries
- Performance analysis with numbers
- Mathematical foundations explained
- Production-ready status

**Common Strengths**:
- Comprehensive coverage (500-1000 lines each)
- Code examples (SQL + C#)
- Performance metrics
- Implementation details
- Clear architectural principles

### Documentation Gaps Identified

**Current docs/ Missing**:
1. **SQL Server 2025 Integration**: Vector type strategy, sp_invoke_external_rest_endpoint
2. **Novel Capabilities**: Cross-modal queries, behavioral geometry, synthesis+retrieval
3. **Model Ingestion**: Complete three-stage pipeline (parse → atomize → spatialize)
4. **Service Broker Patterns**: OODA loop implementation details
5. **SVD Compression**: CLR implementation with code examples

**Implementation Details Missing from Current docs/**:
- CLR function signatures and implementations
- Service Broker queue configuration
- SQL Server Agent job scheduling
- .NET event handler patterns
- Format parser details (GGUF, ONNX, PyTorch, TensorFlow)

---

## Recommendations

### Immediate Actions

**1. COMPARE with Current docs/architecture/**:
- 01-semantic-first-architecture.md vs SEMANTIC-FIRST-ARCHITECTURE.md
- 02-ooda-autonomous-loop.md vs OODA-AUTONOMOUS-LOOP-ARCHITECTURE.md
- 03-entropy-geometry.md vs ENTROPY-GEOMETRY-ARCHITECTURE.md
- **Determine**: Which version is more comprehensive? Which has implementation details?

**2. PROMOTE Unique Files**:
- SQL-SERVER-2025-INTEGRATION.md → docs/architecture/04-sql-server-2025-integration.md
- NOVEL-CAPABILITIES-ARCHITECTURE.md → docs/features/novel-capabilities.md

**3. EVALUATE for Implementation Guides**:
- MODEL-ATOMIZATION-AND-INGESTION.md → docs/implementation/model-ingestion.md (if not covered)
- Extract Service Broker patterns from OODA-AUTONOMOUS-LOOP-ARCHITECTURE.md
- Extract CLR patterns from multiple files

**4. ARCHIVE Design Phase Files**:
- ARCHIVE-HANDLER.md (design phase)
- CATALOG-MANAGER.md (design phase)
- COMPLETE-MODEL-PARSERS.md (design phase)
- MODEL-PROVIDER-LAYER.md (design phase)
- UNIVERSAL-FILE-FORMAT-REGISTRY.md (design phase)
- **Status**: Design phase means incomplete or planning only

### Consolidation Strategy

**Merge Implementation Details**:
- If current docs/architecture/ files are concept-focused
- Extract CLR code examples from .to-be-removed/ versions
- Extract SQL implementation patterns
- Create comprehensive architecture + implementation docs

**Promote Novel Content**:
- SQL Server 2025 vector type strategy (CRITICAL)
- Novel capabilities documentation (marketing + technical)
- Format parser details (GGUF, ONNX, etc.)
- Service Broker OODA implementation

**Archive Historical Drafts**:
- If current docs/architecture/ is more comprehensive
- Keep .to-be-removed/ as historical reference
- Document what was merged/consolidated

---

## Summary Statistics

**Files Catalogued**: 6 detailed + 12 brief mentions  
**Total Lines Sampled**: ~4,342 lines (detailed files)  
**Average Quality**: 5.0 / 5.0 stars (all sampled files excellent)  
**Status**: 4 Production Implementation, 1 Design Phase (99% validated), 1 Production Ready  
**Overlap with Current**: 3 files (50%)  
**Unique Files**: 3 files (50%)  

**Key Findings**:
- All sampled files are high-quality architecture documents
- 3 files overlap with current docs/architecture/ (need comparison)
- 2 unique files critical for promotion (SQL Server 2025, Novel Capabilities)
- All include CLR code examples and SQL implementations
- Design phase files (6) should be archived
- Implementation details exceed current docs/

**Critical Content to Extract**:
- SQL Server 2025 vector type selective usage strategy
- Cross-modal query implementations
- Service Broker OODA loop patterns
- Model ingestion three-stage pipeline
- SVD compression CLR functions
- Format parser details (6 formats)

**Next Steps**:
1. Compare 3 overlapping files with current docs/architecture/
2. Promote SQL-SERVER-2025-INTEGRATION.md and NOVEL-CAPABILITIES-ARCHITECTURE.md
3. Continue audit of .to-be-removed/rewrite-guide/ (~46 files - LARGEST section)
4. Audit .to-be-removed/operations/ (6 files)
5. Final consolidation recommendations
