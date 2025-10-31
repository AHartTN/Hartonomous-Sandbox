# Hartonomous Atom Substrate Overhaul

## Objectives

- Replace dimension-specific vector buckets with a modality-agnostic atom substrate.
- Ingest text/code/images/audio/video/tensors into native SQL types (VECTOR, geometry, JSON) without VARBINARY hacks.
- Support dynamic tensor dimensionality via atom decomposition and coefficient mapping.
- Preserve SQL-first inference, generation, and student-model extraction using spatial + vector hybrid queries.
- Maintain deterministic deduplication (SHA-256 + semantic thresholds) at the atom level.

## Core Constructs

1. **Atoms** (`Atoms` table)
   - Represents the smallest deduplicated unit (text snippet, code block, pixel patch, audio frame, tensor atom).
   - Stores canonical payload metadata, modality, provenance, content hash.
   - Links to embeddings, geometry projections, semantic tags.

2. **Atom Embeddings** (`AtomEmbeddings`)
   - Multiple embeddings per atom and model.
   - `SqlVector<float>` for dimensions ≤ 3996; fallback to component shards otherwise.
   - Spatial projections stored as `geometry` POINT/MultiPoint/LineString with Z/M metadata.

3. **Tensor Atoms** (`TensorAtoms`, `TensorAtomCoefficients`)
   - Decompose weights/tensors into reusable atoms (basis kernels, heads, filters).
   - Each atom has geometry representation, invariants, quantization info.
   - Coefficients map atoms back to parent tensors/layers.

4. **Cognitive Graph** (`AtomGraphNodes`, `AtomGraphEdges`)
   - Captures relationships (temporal order, attention, composition, inference lineage).
   - Edges include spatial encodings for attention/flow queries.

5. **Metadata & Control** (`AtomMetadata`, `IngestionJobs`, `DedupPolicies`)
   - JSON columns for modality-specific data (language, MIME, licensing, timestamps).
   - Configurable dedup thresholds, batch tracking, audit logs.

## Pipeline Flow

1. **Ingestion**
   - Compute SHA-256 of payload; check `Atoms` for duplicates.
   - If new:
     - Persist atom metadata.
     - Generate embeddings (model-specific) and write to `AtomEmbeddings`.
     - Derive spatial projection (anchor-distance or manifold) → `Geometry` columns.
     - For tensors: run atomization, populate `TensorAtoms` + `TensorAtomCoefficients`.
   - Record ingestion event in `IngestionJobs` / audit tables.

2. **Deduplication**
   - Exact: content hash match → increment reference counts.
   - Semantic: `VECTOR_DISTANCE('cosine', embedding, @candidate)` with threshold.
   - Spatial: optional near-duplicate detection via `STDistance` on geometry.

3. **Inference / Generation**
   - Hybrid search (`sp_HybridSearchAtoms`): spatial prefilter + vector rerank.
   - Attention walk (`sp_AttentionWalk`): traverse `AtomGraphEdges` ordered by weight.
   - Sequence generation (`sp_GenerateSequenceFromAtoms`): iterative spatial neighborhood sampling + vector scoring.
   - Student extraction (`sp_ComposeStudentModel`): select atom subsets by importance (geometry Z) and coefficients.

## Implementation Tasks

1. **Schema / Entities**
   - Add new tables (Atoms, AtomEmbeddings, TensorAtoms, TensorAtomCoefficients, AtomGraphNodes/Edges, AtomMetadata, IngestionJobs, DedupPolicies).
   - Update EF Core entities/configurations; remove dimension buckets.

2. **Repositories / Services**
   - Refactor repositories to use new schema.
   - Implement atom-centric ingestion, geometry/projection handling, tensor atomization abstraction.

3. **Stored Procedures**
   - Create/replace procs for ingestion stages, hybrid search, graph traversal, model composition, spatial projection.

4. **Ingestion Application**
   - Update `ModelIngestion` project to new pipeline flow.
   - Ensure semantic dedup + spatial projection executed per atom.

5. **Testing**
   - Expand tests for dedup, atomization, inference paths.

6. **Documentation**
   - Refresh README/PRODUCTION_GUIDE/docs to explain the atom substrate architecture.

## Milestones

- M1: Schema & EF model aligned; migrations applied.
- M2: Ingestion pipeline producing atoms + embeddings + spatial projections.
- M3: Tensor atomization operational for at least one model type.
- M4: Hybrid search & generation procs working on atom data.
- M5: Documentation and tests updated; old bucket artifacts removed.
