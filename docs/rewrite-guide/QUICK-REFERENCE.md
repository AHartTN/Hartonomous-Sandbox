# Quick Reference: Hartonomous Core Concepts

This is a rapid-loading reference for anyone (human or AI) who needs to understand the Hartonomous architecture quickly.

## The One-Sentence Summary

**Hartonomous is an autonomous geometric reasoning system with self-improvement (OODA loop), cross-modal synthesis, reasoning frameworks (Chain/Tree of Thought, Reflexion), behavioral analysis, and cryptographic provenance - all running on SQL Server spatial indexes instead of traditional neural networks.**

## Five Core Truths

### 1. Spatial Indexes ARE the ANN Algorithm
- **NOT using**: SQL Server 2025 VECTOR indexes (they make tables read-only)
- **USING**: SQL Server spatial R-Tree indexes on GEOMETRY columns
- **Why**: R-Tree provides O(log N) search without locking tables
- **Code**: `IX_AtomEmbeddings_SpatialGeometry` in `Common.CreateSpatialIndexes.sql`

### 2. O(log N) + O(K) Replaces Everything
- **Stage 1 (O(log N))**: Spatial R-Tree index returns K×10 candidates via `STIntersects()`
- **Stage 2 (O(K))**: Exact vector similarity on small candidate set
- **Replaces**: Traditional ANN, attention mechanisms, forward passes
- **Code**: `AttentionGeneration.QueryCandidatesWithAttention()` lines 598-700

### 3. Embeddings → 3D Geometry (Deterministic)
- **Input**: 1998-dimensional vectors
- **Output**: 3D GEOMETRY points (X, Y, Z coordinates)
- **Method**: Gram-Schmidt orthonormalization with fixed landmark vectors
- **Critical**: Same vector ALWAYS produces same 3D point (reproducibility)
- **Code**: `LandmarkProjection.ProjectTo3D()`, `SpatialOperations.fn_ProjectTo3D()`

### 4. Model Weights Are GEOMETRY
- **Storage**: TensorAtoms.WeightsGeometry column (GEOMETRY type)
- **Access**: T-SQL queries using `STPointN()` - Y coordinate = weight value
- **Enables**: Queryable AI - inspect model internals with SQL
- **No**: Loading entire models into memory
- **Code**: `AttentionGeneration.LoadTensorWeightsFromGeometry()` lines 444-499

### 5. All Modalities in One Geometric Space
- Text, images, audio, video, code → all project to same 3D space
- Enables cross-modal search (text query returns image atoms)
- Semantic similarity = spatial proximity, **regardless of modality**
- **Code**: Spatial indexes on `AtomEmbeddings`, `AudioData.Spectrogram`, `VideoFrame.MotionVectors`, `CodeAtom.Embedding`

### 6. Reasoning Frameworks Built-In
- **Chain of Thought**: `sp_ChainOfThoughtReasoning` - linear step-by-step reasoning
- **Tree of Thought**: `sp_MultiPathReasoning` - explores N parallel reasoning paths
- **Reflexion**: `sp_SelfConsistencyReasoning` - generates N samples, finds consensus
- **Stored**: Complete reasoning chains in `ReasoningChains`, `MultiPathReasoning`, `SelfConsistencyResults` tables

### 7. Agent Tools Framework
- **AgentTools table**: Registry of available procedures/functions
- Agent dynamically selects tools based on task
- Tools include: generation, reasoning, diagnostics, synthesis
- **Code**: `Seed_AgentTools.sql`, `AgentTools` table

### 8. Behavioral Analysis as Geometry
- **SessionPaths table**: User journey stored as GEOMETRY (LINESTRING)
- X,Y,Z = semantic position, M = timestamp
- OODA detects failing paths, generates "FixUX" hypotheses
- **Code**: `sp_Hypothesize.sql:239-258` - UX issue detection via geometry

### 9. Both Retrieval AND Synthesis
- **Retrieval**: Spatial queries return existing atoms
- **Synthesis**: Generate NEW bytes (audio, images, video)
- **Hybrid**: Retrieve guidance → synthesize new content
- **Code**: `clr_GenerateHarmonicTone` (audio), `GenerateGuidedPatches` (image)

## What Got Eliminated

| Traditional AI | Hartonomous |
|---|---|
| O(N²) attention matrices | Spatial navigation O(log N) + O(K) |
| Full forward passes | Spatial weight queries via STPointN |
| GPU VRAM | SQL Server memory |
| Vector indexes (read-only) | Spatial R-Tree + Hilbert (read-write) |
| Non-deterministic | Deterministic projections |
| Black box | Full provenance (Neo4j + reasoning chains) |
| Static models | Self-improving via OODA loop |
| Single modality | Unified cross-modal geometric space |
| Retrieval OR synthesis | Both retrieval AND synthesis |

## Key Files Proving It Works

**Core Geometric Engine**:
1. **LandmarkProjection.cs** - 1998D → 3D projection (SIMD-accelerated)
2. **AttentionGeneration.cs** - Complete inference via geometric navigation
3. **sp_SpatialNextToken.sql** - Text generation using spatial R-Tree
4. **Common.CreateSpatialIndexes.sql** - All spatial indexes defined
5. **SpatialOperations.cs** - CLR bridge for projection

**Reasoning Frameworks**:
6. **sp_ChainOfThoughtReasoning.sql** - Linear step-by-step reasoning
7. **sp_MultiPathReasoning.sql** - Tree of Thought exploration
8. **sp_SelfConsistencyReasoning.sql** - Reflexion and consensus finding

**OODA Loop**:
9. **sp_Analyze.sql** - System observation and metrics
10. **sp_Hypothesize.sql** - Hypothesis generation (7 types including UX fixes)
11. **sp_Act.sql** - Hypothesis execution
12. **sp_Learn.sql** - Weight updates and model pruning

**Synthesis Capabilities**:
13. **ImageGeneration.cs** - GenerateGuidedPatches for image synthesis
14. **GenerationFunctions.cs** - clr_GenerateHarmonicTone for audio synthesis

## The "Periodic Table" Metaphor

- Atoms = Elements with fixed 3D coordinates
- Semantic domains = Clusters in 3D space
- Inference = Navigation between nearby points
- Model weights = Geometric topology
- Provenance = Merkle DAG connections

## Critical for Rewrite

**MUST PRESERVE**:
- Two-stage O(log N) + O(K) query pattern
- Deterministic 3D projection
- Spatial indexes (not VECTOR indexes)
- Model weights as queryable GEOMETRY
- Cross-modal geometric space
- Reasoning frameworks (CoT, ToT, Reflexion)
- Agent tools framework and dynamic tool selection
- Behavioral analysis (SessionPaths as GEOMETRY)
- Synthesis capabilities (audio, image, video generation)
- OODA loop self-improvement (weight updates, pruning, UX fixing)

**MUST ELIMINATE**:
- Any dependency on SQL Server 2025 preview VECTOR indexes
- Matrix multiplication as PRIMARY generation path (optional ProjectWithTensor feature can remain)
- Non-deterministic generation
- In-memory model loading
- Traditional ANN indexes (replaced by spatial R-Tree)

## Commit Message Context

Recent commits: "AI agents suck", "More fucking AI stupidity"

**Why**: Traditional AI agents are non-deterministic black boxes that can't be verified or traced.

**Solution**: This architecture makes AI deterministic, queryable, and provably correct:
- Every decision tracked in Neo4j Merkle DAG
- Reasoning chains stored in ReasoningChains/MultiPathReasoning/SelfConsistencyResults tables
- SessionPaths capture complete user journey geometrically
- Model weights queryable via SQL (no black box)
- OODA loop provides autonomous self-improvement with full audit trail

## Migration Philosophy

**NOT** a rewrite from scratch. This is **stabilization and cleanup** of a working innovation.

**Goal**: Clean up dependency issues, formalize the architecture, eliminate instability - but **preserve the core geometric engine**.

---

**Last Updated**: 2025-11-15
**Status**: Validated against existing codebase
**Confidence**: VALIDATED - this is not theory, it's working code
