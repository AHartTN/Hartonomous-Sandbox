# Documentation Audit Segment 005: docs_old Examples and Ingestion

**Generated**: 2025-01-XX  
**Scope**: docs_old/examples/ (4 files) + docs_old/ingestion/ (1 file) + docs_old/operations/troubleshooting.md (1 file)  
**Total Files**: 6  
**Purpose**: Catalog example use cases, ingestion system documentation, and troubleshooting guide

---

## Files Audited

### 1. docs_old/examples/behavioral-analysis.md
- **Type**: Technical example documentation
- **Length**: ~630 lines
- **Status**: Production Ready (Nov 19, 2025)
- **Content Quality**: ⭐⭐⭐⭐⭐ Excellent - Complete implementation with SQL examples

**Purpose**:
Demonstrates treating user behavior as GEOMETRY with action sequences becoming LINESTRING paths in semantic space for UX analysis.

**Key Technical Content**:
- Three-phase architecture: User Actions (events) → Action Embedding + 3D Projection → LINESTRING Path (SessionPath) → Geometric Analysis
- SQL schema for `UserActions`, `SessionPaths`, `ErrorRegions` tables with spatial indexes
- Procedures: `sp_TrackUserAction`, `sp_BuildSessionPath`, `sp_ComputeErrorRegions`
- Geometric queries: Error clustering (STBuffer + STCentroid), session path similarity (STIntersects), complexity analysis (STLength)
- OODA integration: Error prediction via hypothesis generation (`sp_Hypothesize_ErrorPrediction`)
- CLR integration: DBSCAN-like clustering for error region detection
- Session path metrics: PathLength, ActionCount, ErrorCount with complexity classification
- Visualization: 3D path heatmaps with color-coding by error count

**Example Queries**:
- Find error clusters: DBSCAN via spatial joins with 10-unit radius, minimum 5 errors
- Detect failure patterns: Session paths intersecting known error regions
- Measure session complexity: PathLength / ActionCount ratio (>10 = high complexity)
- User journey similarity: STDistance between session paths with 20-unit buffer

**Performance**: 80-180ms queries on 500K-2M session datasets with R-Tree spatial indexes

**Integration Points**:
- OODA loop triggers hypothesis generation on error actions
- Neo4j sync for session path provenance tracking
- Grafana dashboards for session health metrics (avg errors, path length, error-free sessions)

**Recommendations**:
- PROMOTE: Core feature demonstrating geometric UX analysis
- Use as reference for spatial query patterns
- Integrate with current docs/features/ directory
- Add to quickstart for behavioral analytics use case

---

### 2. docs_old/examples/cross-modal-queries.md
- **Type**: Technical example documentation
- **Length**: ~680 lines
- **Status**: Production Ready (Nov 19, 2025)
- **Content Quality**: ⭐⭐⭐⭐⭐ Excellent - Novel cross-modal capabilities with complete examples

**Purpose**:
Demonstrates cross-modal semantic queries (text→audio, image→code, audio→text) enabled by unified 3D geometric space.

**Key Innovation**:
ALL modalities (text, audio, image, code, video) project to same 3D semantic space, enabling queries impossible with conventional AI systems.

**Example Implementations**:
1. **Text → Audio** (`sp_CrossModalQuery_TextToAudio`): Find audio clips matching text descriptions
   - Example: "peaceful ocean waves" → ocean_waves_beach.wav (score 0.94)
   - Performance: 18-25ms for 200M audio atoms

2. **Image → Code** (`sp_CrossModalQuery_ImageToCode`): Find code implementing UI mockups
   - Example: Login form mockup → Python tkinter implementation (score 0.89)
   - CLIP embeddings for image-to-semantic projection

3. **Audio → Text** (`sp_CrossModalQuery_AudioToText`): Transcription-free semantic matching
   - Example: Thunderstorm audio → "sound of rain on roof" (score 0.93)
   - NO speech-to-text, direct acoustic feature matching

4. **Multi-Hop Chains**: Image → Code → Documentation (3-hop query ~50-80ms)

**Advanced Features**:
- **Cross-Modal Synthesis** (`sp_GenerateAudioFromImage`): Generate audio "sounding like" an image via spatial proximity guidance
- **Barycentric Interpolation** (`sp_BarycentricBlend`): Blend multiple concepts across modalities
  - Example: "beach" (40%) + "sunset" (30%) + "relaxing music" (30%) → audio atoms

**Architecture**:
- O(log N) spatial pre-filter via R-Tree → O(K) vector refinement via cosine similarity
- Semantic radius tuning: 20-40 units depending on modality (target 1K-10K candidates)
- Modality-agnostic storage: Same spatial indexes for all content types

**Performance Characteristics**:
- Text → Audio: 18-25ms (200M atoms)
- Image → Code: 22-30ms (50M atoms)
- Audio → Text: 25-35ms (3B atoms)
- Multi-hop (3 hops): 50-80ms total

**Monitoring**:
- `CrossModalQueryMetrics` table tracks query type, duration, candidate pool size
- Alert if query time >50ms

**Recommendations**:
- CRITICAL FEATURE: Unique differentiator for Hartonomous
- PROMOTE: Core capability documentation
- Add to docs/features/cross-modal-queries.md
- Reference in quickstart for "wow factor" demonstration
- Expand with video modality examples when implemented

---

### 3. docs_old/examples/model-ingestion.md
- **Type**: Technical implementation guide
- **Length**: ~830 lines
- **Status**: Production Ready (Nov 19, 2025)
- **Content Quality**: ⭐⭐⭐⭐ Very Good - Comprehensive three-phase architecture

**Purpose**:
Complete implementation guide for ingesting GGUF, SafeTensors, ONNX, PyTorch models through CDC → Service Broker → Atomizer architecture.

**Three-Phase Architecture**:

**Phase 1: Capture (CesConsumer Worker)**
- Continuous polling via Change Data Capture (CDC)
- LSN checkpointing (no data loss)
- Standardized event format
- Pure capture layer (no heavy processing)

**Phase 2: Decouple (SQL Service Broker)**
- Durable transactional message queue in SQL Server
- Auto-activation with MAX_QUEUE_READERS = 10
- Reliability: Messages survive server restarts
- Benefits: Transactional integration, scalability, flexibility

**Phase 3: Process (Atomizer Workers)**
- `sp_ProcessAtomizationMessage` consumes queue
- Dual-database insertion: SQL Server + Neo4j
- Content-addressable storage (SHA-256 hashing)
- Idempotent: Duplicate atoms increment reference counts

**EPOCH-Based Bootstrap**:
1. **EPOCH 1 - Axioms**: Tenants, models, spatial landmarks (orthogonal basis vectors)
2. **EPOCH 2 - Primordial Soup**: Create atoms with CAS deduplication
3. **EPOCH 3 - Mapping Space**: Project embeddings to 3D via landmark trilateration
4. **EPOCH 4 - Waking the Mind**: Seed operational history for OODA loop

**Format-Specific Parsers**:
- **GGUF** (`sp_IngestGGUF`): LLaMA, Mistral, GPT-NeoX - magic number validation (0x46554747)
- **SafeTensors** (`sp_IngestSafeTensors`): Hugging Face - JSON header + raw tensor data
- **ONNX** (`sp_IngestONNX`): Protocol Buffers via CLR parser (`clr_ParseONNXModel`)

**Monitoring**:
- `vw_IngestionStatus`: Total atoms, embeddings, references, duration per model
- Service Broker queue depth monitoring (alert if >10K messages)
- Atomization throughput: atoms/second, MB/hour

**Troubleshooting**:
- Ingestion stalled: Re-enable Service Broker, restart queue activation, purge poison messages
- Duplicate atoms: Merge via ContentHash, update foreign keys, delete duplicates
- Spatial projection failed: Regenerate landmarks, reproject embeddings

**Performance Targets**:
- Atomization: >150 atoms/sec
- TinyLlama-1.1B: 500K atoms in 3-5 minutes

**Recommendations**:
- PARTIALLY SUPERSEDED: New data ingestion system in `.archive/docs_old/ingestion/README.md` is more comprehensive
- KEEP for reference: Service Broker architecture patterns still valid
- MERGE with ingestion/README.md: Consolidate into single authoritative guide
- CRITICAL: Contains EPOCH bootstrap sequence not documented elsewhere

---

### 4. docs_old/examples/reasoning-chains.md
- **Type**: Technical implementation guide
- **Length**: ~680 lines
- **Status**: Production Ready (Nov 19, 2025)
- **Content Quality**: ⭐⭐⭐⭐⭐ Excellent - Complete reasoning frameworks with CLR aggregates

**Purpose**:
Implementation of three reasoning frameworks integrated with OODA loop: Chain of Thought (CoT), Tree of Thought (ToT), Reflexion.

**Core Concept**:
Reasoning chains are GEOMETRIC PATHS through semantic space. Coherent reasoning = smooth trajectory. Incoherent reasoning = erratic path.

**Framework Implementations**:

**1. Chain of Thought (CoT)** - Sequential reasoning
- Procedure: `sp_ChainOfThoughtReasoning`
- Process: Problem → Step1 → Step2 → ... → Answer
- Geometric coherence tracking: Sum of angular deviations between consecutive steps
- CLR Aggregate: `ChainOfThoughtCoherence` calculates smoothness score
- Incoherent jump detection: If STDistance >50 units, abort chain
- Example: "5 machines, 5 widgets in 5 minutes" → Answer: 5 minutes (coherence 0.89)

**2. Tree of Thought (ToT)** - Branching exploration
- Procedure: `sp_MultiPathReasoning`
- Process: Breadth-first tree exploration with backtracking and pruning
- Branch scoring via cosine similarity with root embedding
- Pruning: Delete branches with score <0.5
- Best path selection: Highest cumulative score at max depth
- Example: 3 paths × 4 depth for "reduce carbon emissions in urban areas"

**3. Reflexion** - Self-evaluation
- Procedure: `sp_SelfConsistencyReasoning`
- Process: Generate multiple independent CoT chains → Calculate consensus → Refine if needed
- CLR Aggregate: `SelfConsistency` calculates pairwise cosine similarity across answers
- Refinement trigger: If consensus <0.7, regenerate with self-critique
- Mode selection: Most common answer across all paths
- Example: 5 independent chains → "Paris" (consensus 0.95)

**CLR Aggregates** (C# with IBinarySerialize):
- `ChainOfThoughtCoherence`: Geometric smoothness via angular deviation
- `SelfConsistency`: Average pairwise similarity across embeddings
- Serialization for SQL Server: BinaryReader/BinaryWriter with WKB format

**Neo4j Provenance**:
```cypher
MERGE (rc:ReasoningChain {sessionId, framework, coherenceScore})
MERGE (rs:ReasoningStep {stepId, stepNumber, content})
MERGE (rc)-[:HAS_STEP {stepNumber}]->(rs)
MERGE (rs)-[:USED_ATOM]->(a:Atom)
```

**Performance**:
- CoT (5 steps): 800-1200ms (dominated by LLM inference ~150ms/step)
- ToT (3 paths × 4 depth): 2.5-4s
- Reflexion (5 paths + refinement): 4-6s

**Best Practices**:
- CoT: Linear problems (math, logic)
- ToT: Open-ended problems (multiple valid approaches)
- Reflexion: Critical correctness (medical, legal)
- Coherence threshold: Alert if <0.5 (likely hallucination)

**Recommendations**:
- PROMOTE: Core AI reasoning capability
- Move to docs/features/reasoning-chains.md
- Essential for AI agent development documentation
- Reference in Azure AI best practices integration

---

### 5. docs_old/ingestion/README.md
- **Type**: System architecture documentation
- **Length**: ~840 lines
- **Status**: Production Ready (implementation in progress)
- **Content Quality**: ⭐⭐⭐⭐⭐ Excellent - Comprehensive first-principles design

**Purpose**:
Complete data ingestion system for ALL content types with 64-byte atom limit, SHA-256 deduplication, spatial composition tracking.

**Architecture** (First Principles Atomization):
```
ANY CONTENT → Format Detection → Format-Specific Parsing → Semantic Decomposition → 
64-Byte Atoms → SHA-256 Hash → Bulk Insert with MERGE → Spatial Composition
```

**Components Implemented**:

**✅ Core Foundation** (Interfaces + FileTypeDetector):
- `IAtomizer<TInput>`: Generic atomization strategy interface
- `AtomizationResult`: Complete output with atoms, compositions, metadata, child sources
- `AtomData`: Max 64 bytes, SHA-256 content hash
- `AtomComposition`: Parent-child spatial relationships with GEOMETRY positions
- `SpatialPosition`: X,Y,Z,M coordinates with ToWkt() for SQL Server
- File type detection: Magic bytes for 50+ formats, 19 category classification

**✅ Atomizers Implemented**:
1. **TextAtomizer**: Lines → Characters → 4-byte UTF-8 atoms
   - Spatial: X=column, Y=line, Z=0, M=char offset
   - Modality: text/file → text/line → text/utf8-char

2. **ImageAtomizer**: Pixels → 4-byte RGBA atoms
   - Spatial: X=pixelX, Y=pixelY, Z=layer, M=frame
   - Deduplication: 95-99% (limited color palette)
   - SixLabors.ImageSharp for PNG/JPG/GIF/BMP/TIFF/WebP

3. **ArchiveAtomizer**: ZIP/GZ → Recursive extraction
   - Spatial: X=0, Y=entryIndex, Z=0
   - ChildSource enables multi-level recursion (max depth 10)

**✅ Bulk Insert Service**:
- `AtomBulkInsertService`: Table-Valued Parameters with MERGE pattern
- ACID transactions, automatic deduplication via ContentHash
- Reference counting for garbage collection
- Target performance: >1M atoms in <5 seconds

**✅ API Endpoints**:
- `POST /api/ingestion/file`: Upload and atomize (max 1GB)
- `GET /api/ingestion/jobs/{jobId}`: Query job status and progress
- Dynamic atomizer selection by priority
- In-memory job tracking with child job support

**Supported Categories**:
- Text, Code, Markdown, Json, Xml, Yaml
- Images (Raster/Vector)
- Audio, Video (planned: NAudio, FFmpeg)
- Documents (planned: iText7/PdfSharp, Open XML SDK)
- Archives (ZIP, GZ, TAR planned)
- AI Models (planned: GGUF, SafeTensors, ONNX, PyTorch)
- Databases, Executables, Binary

**Database Schema**:
```sql
dbo.Atom (AtomId, ContentHash BINARY(32), AtomicValue VARBINARY(64), Modality, Subtype, 
          ContentType, CanonicalText, Metadata JSON, TenantId, ReferenceCount, CreatedAt)
CONSTRAINT UQ_Atom_ContentHash_TenantId

dbo.AtomComposition (CompositionId, ParentAtomId, ComponentAtomId, SequenceIndex, 
                     SpatialKey GEOMETRY, TenantId, CreatedAt)
```

**Performance Characteristics**:
- Text (100KB): ~100K chars → ~5K unique → <1 second total
- Image (1920×1080): 2.07M pixels → ~12K unique colors → ~4 seconds total
- Archive (100 files): Sum of all file atomizations + overhead

**Remaining Work**:
- Audio/Video atomizers (NAudio, FFmpeg)
- Document atomizers (iText7, Open XML SDK)
- Model atomizers (integrate existing parsers)
- Code atomizers (Roslyn, Tree-sitter)
- Background service (persistent queue, resumability, Neo4j sync)

**Design Principles**:
1. 64-byte atom limit (schema enforced)
2. Content-addressable storage (SHA-256)
3. Spatial structure preservation (GEOMETRY)
4. Recursive decomposition (ChildSource)
5. Modality classification (Modality + Subtype)
6. Bulk operations (MERGE deduplication)
7. ACID compliance
8. Self-contained (no Azure services)

**Recommendations**:
- **SUPERSEDES** docs_old/examples/model-ingestion.md (more comprehensive)
- PROMOTE to docs/implementation/data-ingestion.md
- CRITICAL: 64-byte atom limit is architectural constraint
- Add to architecture documentation (fundamental design decision)
- Reference in quickstart for ingestion examples

---

### 6. docs_old/operations/troubleshooting.md
- **Type**: Operations/support documentation
- **Length**: ~630 lines
- **Status**: Production Ready (Nov 19, 2025)
- **Content Quality**: ⭐⭐⭐⭐⭐ Excellent - Complete diagnostic procedures with SQL solutions

**Purpose**:
Common issues in Hartonomous production deployments with diagnosis and solutions.

**Issues Covered**:

**1. Slow Spatial Queries** (>50ms):
- Diagnosis: Check execution plan, spatial index fragmentation (sys.dm_db_index_physical_stats)
- Solutions: Rebuild spatial index (ALTER INDEX...REBUILD), update statistics, force index hints, reduce semantic radius, columnstore index for large scans
- Target: <30ms for spatial queries

**2. OODA Loop Failures** (>5% error rate):
- Diagnosis: Check OODALogs for failing phases, identify phase-specific errors
- Solutions: 
  - Orient timeout: Increase httpClient timeout, use async/await, cache frequent hypotheses
  - Act CLR errors: See CLR Assembly Errors section
- Example: HypothesisCache table for 24-hour hypothesis caching

**3. CLR Assembly Errors**:
- "Could not load assembly": Deploy dependency assemblies or use ILMerge to bundle
- "Security exception UNSAFE disabled": Enable clr strict security, use sp_add_trusted_assembly with SHA512 hash
- "Invalid serialization data": Drop/recreate CLR objects, clear serialization cache
- PowerShell scripts for assembly hash calculation

**4. Neo4j Sync Lag** (>60 seconds):
- Diagnosis: Check Neo4jSyncQueue depth, identify failed syncs (RetryCount >=5)
- Solutions:
  - Queue depth >10K: Increase worker parallelism (8 workers), batch sync operations (100 items)
  - Failed syncs: Test Neo4j connectivity, inspect malformed Cypher, requeue failed items
  - Optimize Neo4j indexes: CREATE INDEX on atomId, sessionId, relationship types

**5. Service Broker Stalled**:
- Diagnosis: Check queue activation status, query queue directly for poison messages, check sys.transmission_queue
- Solutions:
  - Re-enable queue: ALTER QUEUE...WITH STATUS = ON
  - Activation procedure crashing: Add TRY/CATCH error handling
  - Poison messages: RECEIVE and END CONVERSATION WITH CLEANUP, log to PoisonMessages table

**6. Spatial Projection Failures** (NULL geometries):
- Diagnosis: Check for NULL SpatialGeometry, verify landmarks exist (3 rows, 6144 bytes each)
- Solutions:
  - Regenerate landmarks: sp_BootstrapCognitiveKernel_Epoch1_Axioms
  - CLR function failing: Test clr_LandmarkProjection_ProjectTo3D manually
  - Reproject all atoms: UPDATE with CLR projection function

**7. Performance Degradation**:
- Diagnosis: Check database size (sp_spaceused), memory pressure (sys.dm_os_sys_memory), CPU pressure (sys.dm_os_schedulers)
- Solutions:
  - Database >1TB: Archive old atoms (>6 months), partition large tables by IngestTimestamp
  - Memory pressure: Increase max server memory
  - High CPU: Limit MAXDOP, enable Query Store auto-tuning (FORCE_LAST_GOOD_PLAN)

**Best Practices**:
- Monitor proactively via Grafana dashboards
- Rebuild spatial indexes weekly
- OODA error rate <5%
- Neo4j sync lag <60 seconds
- Service Broker queue depth checks daily
- CLR assembly versioning and hash tracking
- Validate landmarks after bootstrap

**Diagnostic Tools**:
- SQL DMVs: sys.dm_db_index_physical_stats, sys.dm_os_sys_memory, sys.dm_os_schedulers
- SQL views: Custom views for OODA health, sync queue depth
- Error logs: xp_readerrorlog for Service Broker errors
- Neo4j: HTTP API and Bolt protocol connectivity tests

**Recommendations**:
- PROMOTE to docs/operations/troubleshooting.md
- Essential for production support
- Cross-reference with monitoring.md for proactive detection
- Add to operations runbook
- Update with new issues as discovered in production

---

## Cross-File Analysis

### Overlaps and Conflicts

**Examples vs. Features**:
- `behavioral-analysis.md` and `cross-modal-queries.md` are feature demonstrations, not just examples
- Should be promoted to docs/features/ as standalone capabilities
- No conflicts with existing architecture docs

**Ingestion Documentation**:
- `model-ingestion.md` (examples) vs. `README.md` (ingestion) have overlapping scope
- ingestion/README.md is MORE comprehensive (all content types, 64-byte atoms)
- model-ingestion.md has valuable Service Broker architecture details not in ingestion/README.md
- **RESOLUTION**: Merge both into consolidated docs/implementation/data-ingestion.md

**Reasoning Chains**:
- `reasoning-chains.md` is standalone - no conflicts
- Should be promoted to docs/features/ as core AI capability

**Troubleshooting**:
- `troubleshooting.md` is operations documentation - no conflicts
- Complements monitoring.md (proactive vs. reactive)

### Quality Assessment

**Highest Quality** (⭐⭐⭐⭐⭐):
- cross-modal-queries.md: Novel capabilities with complete implementations
- reasoning-chains.md: Three frameworks with CLR aggregates and Neo4j integration
- ingestion/README.md: Comprehensive first-principles design
- troubleshooting.md: Complete diagnostic procedures

**Very Good** (⭐⭐⭐⭐):
- model-ingestion.md: Service Broker architecture valuable but partially superseded
- behavioral-analysis.md: Excellent examples but needs feature-level documentation

**Common Strengths**:
- Complete SQL implementations with stored procedures
- Performance characteristics documented
- Integration with OODA loop, Neo4j, spatial indexes
- Production-ready status (Nov 2025)
- Monitoring and best practices included

### Relationships to Other Documentation

**Architecture References**:
- All files reference 3D spatial projection (landmark trilateration)
- All use O(log N) R-Tree spatial indexes
- All integrate with OODA loop
- All track provenance in Neo4j

**Implementation Dependencies**:
- CLR functions: CosineSimilarity, LandmarkProjection_ProjectTo3D, ChainOfThoughtCoherence
- SQL Server features: Service Broker, spatial indexes, GEOMETRY type, Query Store
- Worker services: CesConsumer, Neo4jSync, Atomizer workers
- Database schema: Atoms, AtomEmbeddings, AtomComposition, Neo4jSyncQueue

**Operations Integration**:
- troubleshooting.md references monitoring.md for proactive alerts
- All docs include performance targets and monitoring queries
- Best practices align across all files

---

## Consolidation Recommendations

### Immediate Actions

**1. Promote to Core Documentation**:
- docs/features/behavioral-analysis.md (from examples)
- docs/features/cross-modal-queries.md (from examples)
- docs/features/reasoning-chains.md (from examples)
- docs/operations/troubleshooting.md (from operations)

**2. Merge and Consolidate**:
- docs/implementation/data-ingestion.md:
  - Merge model-ingestion.md (Service Broker architecture)
  - Merge ingestion/README.md (comprehensive atomization)
  - Add EPOCH bootstrap sequence
  - Include all format parsers

**3. Archive Originals**:
- Keep in .archive/docs_old/ for historical reference
- Update references in other docs to new locations

### Documentation Gaps Identified

**Missing from Current Docs**:
1. **Behavioral Analytics**: No mention of user action tracking in current docs/
2. **Cross-Modal Queries**: Not documented in current docs/features/
3. **Reasoning Frameworks**: CoT/ToT/Reflexion not in current docs/
4. **64-Byte Atom Limit**: Architectural constraint not in architecture docs
5. **Service Broker Architecture**: CDC → Queue → Atomizer pattern not documented
6. **EPOCH Bootstrap**: Axioms → Primordial Soup → Mapping Space → Waking the Mind sequence

**New Documentation Needed**:
- docs/architecture/ingestion-architecture.md (Service Broker + Atomization)
- docs/features/ai-reasoning.md (CoT/ToT/Reflexion frameworks)
- docs/getting-started/feature-showcase.md (Behavioral analytics + Cross-modal queries)

---

## Summary Statistics

**Files Processed**: 6  
**Total Lines**: ~4,290 lines  
**Average Quality**: 4.67 / 5.0 stars  
**Production Ready**: 6 / 6 (100%)  
**Promotion Candidates**: 5 / 6 (83%)  
**Merge Required**: 2 files (model-ingestion + ingestion/README)  

**Key Findings**:
- All files production-ready with Nov 2025 dates
- High-quality implementations with complete SQL examples
- Novel capabilities (cross-modal, behavioral analytics) not in current docs
- Service Broker architecture critical for ingestion system
- CLR aggregates enable advanced reasoning (CoT coherence, self-consistency)
- Troubleshooting guide essential for production operations

**Critical Issues Documented**:
- System.Collections.Immutable.dll CLR dependency (referenced but not detailed here)
- 64-byte atom limit architectural constraint (fundamental design decision)
- Service Broker activation failures (poison messages, queue stalls)
- Spatial index fragmentation (>30% requires rebuild)
- Neo4j sync lag (>60 seconds indicates bottleneck)

**Next Segment**: Continue with .to-be-removed/ directory (largest section with ~110 files)
