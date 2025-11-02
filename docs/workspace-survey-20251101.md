# Hartonomous Repository Survey — 2025-11-01

## Solution Topology
- **Hartonomous.sln** aggregates core services, ingestion pipelines, admin UI, and integration utilities.
- **Hartonomous.Tests.sln** targets the parallel test tree under `tests/`, mirroring project boundaries (Core, Infrastructure, ModelIngestion, Integration).

## Project Inventory

### src/Hartonomous.Core
- **Purpose:** Domain model and abstraction layer. Supplies entities (Atoms, Models, Embeddings), interfaces (repositories, services), utilities (vector math, hashing), and configuration/value objects.
- **Highlights:**
  - `Entities/Model.cs` et al define the multimodal atom substrate, inference tracking, and model catalog structures.
  - `Utilities/VectorUtility` supports SQL Server 2025 `VECTOR` types (dimension guards, cosine distance, padding).
  - Interfaces such as `IAtomRepository`, `IAtomEmbeddingRepository`, `IStudentModelService` maintain clean separation for infrastructure implementations.

### src/Hartonomous.Data

- **Purpose:** EF Core 10 data access for SQL Server 2025.
- **Key Assets:**
  - `HartonomousDbContext`: Registers all multimodal DbSets, applies `dbo` schema, enforces UTC/date column conventions, ensures NetTopologySuite when options are user-supplied.
  - `Configurations/`: EntityType configurations that enforce column types and indexes beyond conventions (e.g., `AtomConfiguration` sets unique `ContentHash` index and geometry column, cascading deletes for embeddings/tensor atoms; other configs handle atomic storage hashes, inference step relationships, model metadata shapes).
  - Migrations:
    - `20251031210015_InitialMigration`: Creates comprehensive schema (Atoms, AtomEmbeddings, tensor atoms, model catalog, ingestion jobs, multimedia tables, spatial anchors, token vocab, inference tracking) with vector/spatial columns, and emits the baseline stored procedures (`sp_ExactVectorSearch`, `sp_ApproxSpatialSearch`, `sp_HybridSearch`, `sp_MultiResolutionSearch`, `sp_CognitiveActivation`, `sp_SpatialAttention`, `sp_SpatialNextToken`, `sp_GenerateTextSpatial`, `sp_InitializeSpatialAnchors`, `sp_ComputeSpatialProjection`, `sp_RecomputeAllSpatialProjections`, `sp_EnsembleInference`, `sp_QueryModelWeights`, `sp_UpdateModelWeightsFromFeedback`).
  - `20251101143425_AddSpatialAndVectorIndexes`: Adds `IX_AtomEmbeddings_SpatialGeometry`, `IX_AtomEmbeddings_SpatialCoarse`, and DiskANN vector index for `Embeddings_DiskANN` when available, guarding with `IF EXISTS` checks to keep repeatable runs safe.
  - **Schema/SQL interplay:** EF migrations lay down the foundation tables and critical stored procedures, while the surviving scripts under `sql/procedures/` provide experimental/extended flows (e.g., `21_GenerateTextWithVector.sql`, `17_FeedbackLoop.sql`, `22_SemanticDeduplication.sql`) that remain outside the migration history until stabilized.
  - `HartonomousDbContextFactory`: Design-time factory (local SQL, retries, NetTopologySuite).

### src/Hartonomous.Infrastructure

- **Purpose:** Implementations for data access, orchestration, and services.
- **Dependency Registration (`DependencyInjection.cs`):**
  - Configures `HartonomousDbContext` with SQL retries, command timeout, and optional detailed errors.
  - Binds `SqlServerOptions`, sets up `ISqlServerConnectionFactory`/`ISqlCommandExecutor` for raw SQL operations.
  - Registers repository implementations (atom, embedding, tensor, dedupe policy, ingestion jobs, models) and service layer (atom ingestion, ingestion orchestration, discovery, statistics, student model, spatial inference).
- **Repository Characteristics:**
  - `AtomRepository`: EF-based CRUD plus reference count, metadata, and spatial key updates.
  - `AtomEmbeddingRepository`: Combines EF with stored procedures/raw SQL for vector similarity, spatial projection, hybrid search; handles component writes with manual transactions.
  - `CdcRepository`: Consumes SQL Server 2025 CES tables, supporting both CDC and change tracking capture instances, normalising LSN handling.
- **Services:**
  - `AtomIngestionService`: Dedupes via hash and semantic similarity (configurable policy thresholds), computes spatial projections, persists embeddings/components.
  - `ModelDiscoveryService`: Detects ONNX/Safetensors/PyTorch/GGUF formats through extensions, magic numbers, config metadata.
  - `ModelIngestionOrchestrator`: Routes ingestion to appropriate reader via DI, supports bulk directory ingestion and validation.
  - Additional services for ingestion pipeline (`ModelIngestionProcessor`, `ModelDownloader`), spatial inference, and student model operations.

### src/ModelIngestion

- **Purpose:** Worker service coordinating model downloads, format-aware ingestion, embedding tests, and atomic storage validation.
- **Entry Point (`Program.cs`):** Builds host with `AddHartonomousInfrastructure`, registers HTTP client for `ModelDownloader`, binds Application Insights if configured, wires scoped services (`IngestionOrchestrator`, `ModelIngestionService`, `EmbeddingIngestionService`, `AtomicStorageService`, test helpers, model format readers).
- **CLI Orchestrator (`IngestionOrchestrator.cs`):** Command router for download/ingest workflows (`download-hf`, `download-ollama`, `ingest-model`, `ingest-models`, `ingest-embeddings`, `query`, `test-atomic`, etc.); composes `ModelIngestionService`, `EmbeddingIngestionService`, `ModelDownloader`, `QueryService`, `AtomicStorageTestService` to execute tasks and log progress.
- **Ingestion Services:**
  - `ModelIngestionService`: Delegates to `Hartonomous.Infrastructure.Services.ModelIngestionProcessor` to detect format, persist metadata/layers, and verify results via `IModelRepository`; supports batch ingestion and statistics retrieval via `IIngestionStatisticsService`.
  - `EmbeddingIngestionService`: Extends `BaseConfigurableService` to push vectors through `IAtomIngestionService`, applying dedupe policy, padding, and optional spatial projection; returns duplicate diagnostics.
  - `AtomicStorageService`: Deduplicates atomic pixels/audio samples/text tokens by SHA256 hash, updating reference counts across repositories; exposes batch utilities.
  - `EmbeddingTestService` and `AtomicStorageTestService`: Generate synthetic data to validate dedupe, log outcomes, and surface progress metrics.
- **Model Acquisition:** `ModelDownloader` fetches Hugging Face artifacts (prefers `.safetensors`, supports `.onnx/.pt/.bin`) with progress logging and config download, and exports Ollama models by copying cached GGUF blobs.
- **Content Pipeline (`Content/`):** `ContentIngestionService` selects extractor (`TelemetryContentExtractor`, `TextContentExtractor`) based on `ContentExtractionContext`; each extractor produces `AtomIngestionRequest` collections consumed by `IAtomIngestionService`, returning source diagnostics.
- **Query Utilities:** `QueryService` issues hybrid vector/spatial searches via `IAtomEmbeddingRepository`, projecting random embeddings into 3D and logging top results.
- **Model Formats (`ModelFormats/`):**
  - `OnnxModelReader`: Uses `IOnnxModelLoader` or default loader to enumerate initializers, convert tensor data (float32/16/bfloat16/64) to weight geometries, and build `ModelLayer` records.
  - `SafetensorsModelReader`: Reads JSON header + binary tensors, builds layers with geometry via `IModelLayerRepository` helper.
  - `PyTorchModelReader`: Delegates to `IPyTorchModelLoader` (TorchSharp-based) to parse state dict parameters, infer architecture metadata, and generate layers.
  - Shared helpers (`Float16Utilities`, `TensorDataReader`, `ModelReaderFactory`) normalise dtype conversions and DI usage.

### src/CesConsumer

- **Purpose:** Console host for SQL Server 2025 Change Event Streaming (CES).
- **Composition:**
  - `Program.cs`: Builds host, pulls configuration from `appsettings.json`, registers Hartonomous infrastructure, configures `CdcListener`, attaches hosted service `CesConsumerService`.
  - `CdcListener`: Polls `ICdcRepository`, transforms change rows into CloudEvents with SQL Server extensions, adds semantic/reasoning enrichments, batches to Azure Event Hubs, persists last processed LSN in local file; enriches models (`inferred_capabilities`, compliance) and inference requests (`reasoning_mode`, SLA) prior to publish.
  - `CesConsumerService`: Background service managing listener lifecycle with cancellation and graceful shutdown.

### src/Hartonomous.Admin

- **Purpose:** Blazor Server admin portal + SignalR telemetry hub.
- **Dependency Graph:**
  - Registers infrastructure services, health checks, admin-specific services (`AdminTelemetryCache`, `AdminOperationCoordinator`, background workers) and hosted workers (`AdminOperationWorker`, `TelemetryBackgroundService`).
  - `AdminOperationService`: Bridges UI actions to ingestion/orchestration services, enqueueing operations for ingestion, student extraction, comparisons.
  - SignalR `TelemetryHub`: Broadcasts metrics and operations; groups clients into `metrics`/`operations` channels, pushes initial snapshot on connect.
  - Razor pages:
    - `Index.razor`: Dashboard showing totals, layer counts, architecture breakdown with live updates from `AdminTelemetryCache`.
    - `ModelBrowser.razor`: Lists ingested models via `AdminOperationService` with ingestion timestamps.
    - `ModelIngestion.razor`: Queues single/bulk ingestion via admin operations pipeline, surfaces feedback per operation id.
    - `ModelExtraction.razor`: Student model extraction by ratio, layer count, spatial window; supports comparison queueing and immediate feedback.
    - `Operations.razor`: Real-time log of queued/running/completed operations sorted by start time.
  - Styling: `wwwroot/css/site.css` implements dark UI.

### src/Neo4jSync

- **Purpose:** Background host listening for CloudEvents and projecting provenance into Neo4j.
- **Workflow:**
  - Consumes Azure Event Hub via `EventProcessorClient` with blob storage checkpoints.
  - Deserialises CloudEvents, inspects SQL extensions (`operation`, `table`), dispatches to `ProvenanceGraphBuilder` to create/merge nodes (`Model`, `Inference`, `Knowledge`, generic fallback) enriched with semantic metadata from CES pipeline.
  - Creates relationships for model usage (e.g., `[:USED_IN]`).

### src/SqlClr

- **Purpose:** SQL CLR assembly exposing vector math, multimedia processing, and spatial helpers to SQL Server.
- **VectorOperations.cs:** Provides deterministic VARBINARY-backed implementations for dot product, cosine similarity, Euclidean distance, normalization, softmax, interpolation, and argmax while awaiting full `VECTOR` adoption.
- **AudioProcessing.cs:** Converts 16-bit PCM buffers to `geometry` waveforms, computes RMS/peak amplitudes, and down-samples via mean aggregation per channel.
- **ImageProcessing.cs:** Generates multipoint clouds from RGB/RGBA pixels, returns average color hex strings, and emits luminance histograms as JSON arrays.
- **ImageGeneration.cs:** Produces diffusion-inspired guidance patches/geometry collections seeded by random walks toward guidance vectors, surfaced as table-valued or `geometry` results.
- **SemanticAnalysis.cs:** Extracts heuristic topic/sentiment/formality metrics from text and returns a feature JSON payload (keyword scoring, word statistics).
- **SpatialOperations.cs:** Builds multipoint clouds from coordinate strings and wraps common geometry operations (convex hull, point-in-region, overlap area, centroid).

### scripts/deploy-database.ps1

- **Purpose:** Orchestrates database deployment (migrations, stored procedures, SQL CLR assemblies, vector/spatial prerequisites).
- **Execution Flow:**
  - Validates SQL Server connectivity, creates database if needed, reports table/proc counts.
  - Runs EF Core migrations from `src\Hartonomous.Data` unless `--SkipMigrations` supplied.
  - Iterates `sql\procedures/*.sql`, rewrites `CREATE` to `CREATE OR ALTER`, and applies via `sqlcmd` unless skipped.
  - Builds `src\SqlClr\SqlClrFunctions.csproj` (Release), enables CLR, drops/recreates dependent functions, redeploys assembly in binary form when not skipped.
  - Summarises table/procedure counts and key dataset sizes; suggests follow-on commands for vocabulary seeding, ingestion smoke tests, and CLR validation.

### sql/

- **Purpose:** Stored procedures and helper scripts for inference, ingestion, and maintenance (e.g., `21_GenerateTextWithVector.sql`).
- **01_SemanticSearch.sql:** Hybrid spatial filter + vector reranker. Logs inference request, projects query into spatial coordinates, invokes `sp_HybridSearch`, and records metrics in `InferenceRequests`.
- **02_TestSemanticSearch.sql:** Smoke harness executing `sp_SemanticSearch` with sample vectors and dumping recent `InferenceRequests` rows.
- **03_MultiModelEnsemble.sql:** Creates toy `AIModels` table and `sp_EnsembleInference` procedure that queries `KnowledgeBase` with three simulated models, aggregates via weighted averaging, and records per-model `InferenceSteps`.
- **05_SpatialInference.sql:** Introduces spatialized token store (`TokenEmbeddingsGeo`), spatial indexes, and three procedures (`sp_SpatialAttention`, `sp_SpatialNextToken`, `sp_GenerateTextSpatial`) that replace matrix math with geometry searches.
- **05_VectorFunctions.sql:** SQL wrappers over CLR vector helpers (dot product, cosine distance, normalization, arithmetic) for legacy binaries stored as `VARBINARY`.
- **06_ConvertVarbinary4ToReal.sql:** Scalar function decoding 4-byte IEEE 754 floats from `VARBINARY` blobs (used by legacy vocab ingestion scripts).
- **06_ProductionSystem.sql:** Comprehensive deck of production procs—exact/spatial/hybrid vector search plus tensor-atom student extraction (`sp_ExtractStudentModel`), weight inspection, with summary prints for go-live usage.
- **07_AdvancedInference.sql:** Adds multi-stage spatial funnel search, cognitive activation (thresholded cosine), dynamic student extraction strategies, cross-modal queries, knowledge comparison, and inference history analytics.
- **08_SpatialProjection.sql:** Manages anchor selection and distance-based 3D projections with batch recomputation/quality analysis helpers.
- **09_SemanticFeatures.sql:** Builds `SemanticFeatures` table, topic dictionaries, and batch routines relying on CLR semantic scoring; supports semantic-filtered vector search.
- **16_SeedTokenVocabularyWithVector.sql:** Seeds `TokenVocabulary` using native `VECTOR` literals instead of binary casts.
- **17_FeedbackLoop.sql:** Stub for reinforcement updates—collects highly rated inference steps, computes intended update magnitudes, and reports candidate layer adjustments (weights not mutated yet).
- **21_GenerateTextWithVector.sql:** Ensemble-aware generator logging inference metadata, supporting temperature tweaks, weighted multi-model predictions, and detailed `InferenceSteps` records.
- **22_SemanticDeduplication.sql:** `sp_CheckSimilarityAboveThreshold` helper for ingestion duplicates, returning nearest atom when cosine similarity exceeds configurable threshold.
- **sp_GenerateImage.sql:** Diffusion-inspired patch synthesis using CLR image functions with optional geometry output; ties inference logging to `InferenceSteps`.

- **TextToEmbedding.sql:** Wraps `sp_invoke_external_rest_endpoint` against Azure OpenAI embeddings, materializing results to `VECTOR(768)` and logging request metadata.
- **Status:** Remaining stored procedures are now catalogued; further review should cross-check duplicated proc names (`sp_GenerateText`) and ensure seeds align with production vocab.
- *Retired 2025-11-01:* `04_GenerateText.sql`, `04_ModelIngestion.sql`, `07_SeedTokenVocabulary.sql`, and `15_GenerateTextWithVector.sql` deleted to remove conflicting legacy entry points.
- **EF-deployed procs:** `20251031210015_InitialMigration` already installs the baseline T-SQL endpoints that infrastructure depends on (`sp_ExactVectorSearch`, `sp_ApproxSpatialSearch`, `sp_HybridSearch`, `sp_MultiResolutionSearch`, `sp_CognitiveActivation`, `sp_SpatialAttention`, `sp_SpatialNextToken`, `sp_GenerateTextSpatial`, `sp_InitializeSpatialAnchors`, `sp_ComputeSpatialProjection`, `sp_RecomputeAllSpatialProjections`, `sp_EnsembleInference`, `sp_QueryModelWeights`, `sp_UpdateModelWeightsFromFeedback`); the remaining scripts in this folder represent extensions not yet rolled into migrations.

### sql/indexes/ (removed 2025-11-01)

- **Status:** Placeholder directory deleted; vector/spatial indexes now managed through EF Core migrations and in-proc scripts.

### sql/verification/

- **SystemVerification.sql:** End-to-end smoke script running inventory reports, vector/hybrid/spatial searches, dynamic student extraction, knowledge comparison, inference history review, cross-modal demos, and health checks across production tables.
- **Status:** Comprehensive and current; treat as baseline validation harness before major deployments.

### neo4j/

- **Purpose:** Cypher schema definitions (`schemas/CoreSchema.cypher`) establishing constraints, labels, and sample queries for inference explainability graph.
- **schemas/CoreSchema.cypher:** Declares unique constraints and indexes for inference/model nodes, documents explainability-oriented node/relationship taxonomy, and ships example trace + query patterns for post-hoc analysis; also seeds canonical `ReasoningMode`/`Context` nodes.
- **queries/**: Placeholder folder deleted 2025-11-01; promote future canned queries through versioned Cypher files when ready.

### tests/

- `Hartonomous.Core.Tests`: `UtilitiesTests` exercises vector padding, cosine distance, hashing, geometry conversions; placeholder `UnitTest1.cs` removed 2025-11-01.
- `Hartonomous.Infrastructure.Tests`: `AtomIngestionServiceTests` runs dedupe flows via stub repositories; still the only suite in project.
- `Integration.Tests`: Uses `SqlServerTestFixture` to migrate, seed, and verify stored procedures/spatial indexes; `DatabaseSmokeTests` assert presence of core procs, embeddings, vector ops.
- `ModelIngestion.Tests`: `ModelReaderTests` validate ONNX/Safetensors/PyTorch readers with custom loaders; `TestSqlVector` confirms `SqlVector<T>` availability; comment-only `UnitTest1.cs` removed 2025-11-01.
- **Coverage gap:** New tests needed where placeholders were deleted to maintain safety net.

### docs/

- **component-inventory.md:** Updated 2025-11-01 to reflect removal of legacy SQL prototypes and to enumerate outstanding documentation gaps (deployment playbook, coverage audit, Neo4j query library, operational runbooks).

## Reconciliation Findings

- **Legacy SQL prototypes:** `sql/procedures/04_ModelIngestion.sql`, `04_GenerateText.sql`, `07_SeedTokenVocabulary.sql`, and `15_GenerateTextWithVector.sql` removed 2025-11-01 to eliminate conflicting entry points and outdated ingestion logic.
- **Duplicate text generators:** `sp_GenerateText` now maps solely to `21_GenerateTextWithVector.sql`; ensure deployment tooling excludes retired variants.
- **Empty scaffolding:** Placeholder directories `sql/indexes/` and `neo4j/queries/` removed 2025-11-01; ensure future artifacts land in version-controlled files.
- **Stub tests:** Placeholder files deleted, but replacement coverage is outstanding.
- **Legacy script summary:** `16_SeedTokenVocabularyWithVector.sql` is the surviving vocabulary loader; `21_GenerateTextWithVector.sql` is the sole text generator after cleanup.

## Unfinished Workflow Notes

- **Feedback loop updates:** `sql/procedures/17_FeedbackLoop.sql` ends at reporting candidate adjustments without persisting weight changes or scheduling follow-up actions.
- **SQL ingestion walkthrough:** Legacy SQL script removed; author a modern walkthrough or documentation based on `src/ModelIngestion` if reference material is still required.
- **Verification harness:** `sql/verification/SystemVerification.sql` provides valuable smoke coverage but lacks automated assertions or pipeline integration; consider wrapping it with tests or CI hooks.

## Immediate Follow-Ups

1. **Backfill Tests:** Add meaningful coverage in `Hartonomous.Core.Tests` and `ModelIngestion.Tests` to replace the deleted placeholders.
2. **Automate Verification Harness:** Integrate `sql/verification/SystemVerification.sql` into repeatable tests or deployment validation so smoketests produce actionable pass/fail signals.

---
*Generated directly from repository inspection on 2025-11-01. Updates required as additional directories are reviewed.*
