# Initial Commit Analysis - 32e6b65

**Commit:** 32e6b65  
**Date:** October 27, 2024  
**Message:** "Initial commit: Pre-refactor checkpoint - Database working, EF migration applied, VECTOR/JSON types verified"  
**Author:** Anthony Hart  
**Tag:** pre-ef-refactor-stable, backup/pre-ef-refactor-20251027-160319  
**Total Files:** 117

## Summary

This commit represents a stable checkpoint before a major EF Core refactor. The system demonstrates a working architecture with:
- EF Core 10 migrations applied successfully
- SQL Server 2025 VECTOR and JSON types verified functional
- Three .NET 10 worker services (CesConsumer, Neo4jSync, ModelIngestion)
- .NET Framework 4.8.1 SQL CLR functions
- Complete multi-modal SQL schema (text, image, audio, video)
- Neo4j integration for provenance tracking

## File Catalog

### Root Configuration Files (14 files)

**Hartonomous.sln**
- Purpose: Visual Studio solution file
- Projects: 7 total
  - CesConsumer (.NET 10 - Change Event Streaming consumer)
  - ModelIngestion (.NET 10 - Model ingestion utilities)
  - Neo4jSync (.NET 10 - Neo4j synchronization)
  - SqlClrFunctions (.NET Framework 4.8.1 - SQL CLR)
  - Hartonomous.Core (.NET 10 - Domain entities)
  - Hartonomous.Data (.NET 10 - EF Core DbContext)
  - Hartonomous.Infrastructure (.NET 10 - Repositories, DI)
- Status: Builds successfully

**.gitignore**
- Purpose: Standard .NET gitignore plus model artifacts
- Notable: Excludes `.onnx`, `.safetensors`, `.pt`, `.pb` files in tools/
- Notable: Excludes `model_cache/` directories
- Reason: Prevents large binary model files from entering repo

**.github/copilot-instructions.md**
- Purpose: GitHub Copilot agent instructions
- Content: Unknown (not examined in detail)
- Note: Present from initial commit

**.claude/settings.local.json**
- Purpose: Claude AI agent local settings
- Content: Unknown (not examined in detail)
- Note: Present from initial commit

**README.md**
- Purpose: Primary project documentation
- Key Claims:
  - "Revolutionary cognitive database system"
  - "Zero VRAM requirements"
  - "Indexes ARE Models, Queries ARE Inference"
- Architecture: SQL Server 2025 → .NET 10 Services → Neo4j
- Multi-modal: Text, Image, Audio, Video support claimed
- Status: Development checklist shows most features incomplete

### Documentation Files (13 files in docs/ + root)

**docs/ARCHITECTURE.md**
- Purpose: Architectural documentation
- Content: Likely details SQL Server 2025 vector/spatial architecture
- Status: Present from initial commit

**docs/SPATIAL_TYPES_COMPREHENSIVE_GUIDE.md**
- Purpose: Guide for using SQL Server spatial types (GEOMETRY/GEOGRAPHY)
- Content: Likely covers multi-resolution feature pyramids, point clouds
- Reason: Core to vision of "spatial data as neural network features"

**ASSESSMENT.md, DEMO.md, EXECUTION_PLAN.md, PRODUCTION_GUIDE.md, etc.**
- Purpose: Various planning and status documents
- Pattern: Multiple overlapping "status" documents (PROJECT_STATUS.md, STATUS.md, PRODUCTION_REFACTORING_STATUS.md)
- Note: Indicates active development with multiple pivot attempts

**THOUGHT_PROCESS.md**
- Purpose: Development rationale documentation
- Indicates: This was a research/experimental project with evolving design

**VERIFICATION_RESULTS.txt**
- Purpose: Test/verification output capture
- Content: Unknown specifics
- Indicates: Manual verification process in place

### SQL Schema Files (21 files in sql/schemas/)

**sql/schemas/01_CoreTables.sql**
- Purpose: Foundation tables for models, embeddings, inference tracking
- Expected Content:
  - Models (metadata)
  - ModelLayers (structure)
  - Embeddings (VECTOR columns)
  - InferenceRequests, InferenceSteps (tracking)
  - TokenVocabulary (language models)
- Evidence: EF Core entities exist for these tables

**sql/schemas/02_MultiModalData.sql**
- Purpose: Image, Audio, Video, Text domain tables
- Expected Content:
  - Image, ImagePatch (GEOMETRY for pixels)
  - AudioData, AudioFrame (LINESTRING for waveforms)
  - Video, VideoFrame (motion vectors)
  - TextDocument (fulltext + semantic)
- Evidence: EF migration 20251027202323_InitialCreate.cs confirms these

**sql/schemas/02_UnifiedAtomization.sql**
- Purpose: "Atomization" system (breaking multi-modal data into atoms)
- Pattern: Parallel naming with 02_MultiModalData.sql suggests iterative design
- Reason: Multiple "02_" files indicate schema evolution

**sql/schemas/03_CreateSpatialIndexes.sql**
- Purpose: Create spatial indexes on GEOMETRY columns
- Critical: 4-level multi-resolution indexes for "convolutional operations"
- Expected: Indexes on ImagePatch.Geometry, AudioData.Waveform, etc.

**sql/schemas/03_EnableCdc.sql**
- Purpose: Enable Change Data Capture on key tables
- Reason: Feeds Change Event Streaming (CES) to CesConsumer service
- Critical: Core to audit trail architecture

**sql/schemas/04_DiskANNPattern.sql**
- Purpose: DiskANN vector index configuration
- Expected: VECTOR indexes on Embeddings.Vector with specific parameters
- Reason: Core to "zero VRAM" inference vision (pre-built indexes)

**sql/schemas/08-20_*.sql**
- Pattern: Files numbered 08-20 are ALL TokenVocabulary fixes/alterations
- Files: 08_AlterTokenVocabulary, 09_AlterTokenVocabularyVector, 10_FixTokenVocabulary, 11_FixTokenVocabularyPrimaryKey, 12-14_FixTokenVocabularyTake2-4, 17-18_FixAndSeedTokenVocabulary(Take2), 20_CreateTokenVocabularyWithVector
- Evidence: Iterative schema problems with TokenVocabulary table
- Root Cause: Likely switching from non-VECTOR to VECTOR column type
- Sabotage Pattern: Multiple fix attempts suggest AI agent or developer confusion

**sql/schemas/19_Cleanup.sql**
- Purpose: Clean up previous TokenVocabulary mess
- Position: Between 18_FixAndSeedTokenVocabularyTake2 and 20_CreateTokenVocabularyWithVector
- Indicates: Admitted failure, starting fresh

**sql/schemas/21_AddContentHashDeduplication.sql**
- Purpose: Add deduplication via content hash
- Expected: ContentHash column on Embeddings, unique constraint
- Reason: Prevent duplicate embeddings for same content

### SQL Stored Procedures (14 files in sql/procedures/)

**sql/procedures/01_SemanticSearch.sql, 02_TestSemanticSearch.sql**
- Purpose: Core semantic search via VECTOR indexes
- Pattern: Test procedure immediately follows implementation
- Expected: Uses VECTOR_DISTANCE() for similarity search

**sql/procedures/03_MultiModelEnsemble.sql**
- Purpose: Run inference across multiple models, ensemble results
- Key Claim: "Query 10 LLMs at once"
- Expected: Parallel execution, weighted averaging

**sql/procedures/04_ModelIngestion.sql, 04_GenerateText.sql**
- Pattern: Two "04_" files (numbering collision)
- Purpose: Model ingestion pipeline + text generation
- Indicates: Concurrent development or schema evolution

**sql/procedures/05_VectorFunctions.sql, 05_SpatialInference.sql**
- Pattern: Two "05_" files
- Purpose: Vector math utilities + spatial index inference
- Indicates: Continued numbering collision pattern

**sql/procedures/06_ConvertVarbinary4ToReal.sql**
- Purpose: Convert 4-byte VARBINARY to REAL (single-precision float)
- Reason: Model weights stored as binary, need conversion for computation
- Technical: Handles endianness, IEEE 754 format

**sql/procedures/06_ProductionSystem.sql**
- Pattern: Third "06_" file
- Purpose: Production deployment procedures
- Indicates: Multiple concurrent development efforts

**sql/procedures/07_AdvancedInference.sql**
- Purpose: Advanced inference capabilities beyond basic search
- Expected: Multi-modal fusion, attention mechanisms

**sql/procedures/07_SeedTokenVocabulary.sql**
- Pattern: Second "07_" file
- Purpose: Populate TokenVocabulary table
- Related: To schemas/08-20 TokenVocabulary fixes

**sql/procedures/08_SpatialProjection.sql**
- Purpose: Project embeddings into spatial coordinates
- Reason: Enables spatial index queries on semantic embeddings

**sql/procedures/09_SemanticFeatures.sql**
- Purpose: Extract semantic features from content
- Expected: TF-IDF, entity extraction, keyword analysis

**sql/procedures/15_GenerateTextWithVector.sql, 16_SeedTokenVocabularyWithVector.sql**
- Pattern: Jumped from 09 to 15-16
- Purpose: Regenerate text generation and vocabulary with VECTOR columns
- Indicates: Mid-development schema change (adding VECTOR type support)

**sql/procedures/21_GenerateTextWithVector.sql**
- Pattern: Duplicate of 15_GenerateTextWithVector.sql concept
- Purpose: Another iteration of VECTOR-based text generation
- Indicates: Continued evolution/fixes

### SQL Verification (1 file)

**sql/verification/SystemVerification.sql**
- Purpose: Comprehensive system health check
- Expected: Query all tables, verify indexes exist, check CDC status
- Use Case: Post-deployment validation

### Neo4j Schema (1 file)

**neo4j/schemas/CoreSchema.cypher**
- Purpose: Neo4j graph schema constraints and indexes
- Expected Content:
  - Node types: Model, Inference, Embedding, User
  - Relationship types: USED_MODEL, GENERATED_BY, RATED_BY
  - Constraints: Unique model names, inference IDs
- Reason: Enables provenance tracking and explainability queries

### PowerShell Scripts (1 file)

**scripts/deploy.ps1**
- Purpose: Deployment automation
- Expected: Runs all schema scripts in order, deploys CLR, verifies
- Critical: Must handle numbered file ordering (01, 02, etc.)

### C# Source Code - SqlClr Project (5 files)

**src/SqlClr/SqlClrFunctions.csproj**
- Target Framework: .NET Framework 4.8.1 (SQL Server 2025 requirement)
- Purpose: Compiles SQL CLR assembly
- Critical: Must produce deterministic, SQL Server-compatible DLL
- Expected References: System.Data.SqlTypes, Microsoft.SqlServer.Types

**src/SqlClr/Properties/AssemblyInfo.cs**
- Purpose: Assembly metadata for SQL CLR
- Expected Attributes: AssemblyTitle, AssemblyVersion, SqlAssembly
- Critical: SqlAssembly attribute controls CLR integration settings

**src/SqlClr/VectorOperations.cs**
- Purpose: Vector math functions exposed to T-SQL
- Expected: Dot product, L2 distance, normalization, etc.
- Reason: Core to VECTOR column operations

**src/SqlClr/SpatialOperations.cs**
- Purpose: Spatial geometry functions
- Expected: Point cloud operations, polygon intersection, distance calculations
- Reason: Extends built-in geometry functions with ML-specific operations

**src/SqlClr/ImageProcessing.cs**
- Purpose: Image manipulation in SQL CLR
- Expected: Pixel extraction, edge detection, histogram analysis
- Reason: Database-native image embeddings (no external models)

**src/SqlClr/AudioProcessing.cs**
- Purpose: Audio signal processing in SQL CLR
- Expected: FFT, MFCC, windowing functions
- Reason: Database-native audio embeddings

### C# Source Code - CesConsumer Project (3 files)

**src/CesConsumer/CesConsumer.csproj**
- Target Framework: .NET 10
- Purpose: .NET 10 worker service project file
- Expected NuGet: Microsoft.Extensions.Hosting, System.Data.SqlClient

**src/CesConsumer/Program.cs**
- Purpose: Service entry point, host configuration
- Expected: IHost builder, CDC connection setup, message loop

**src/CesConsumer/CdcListener.cs**
- Purpose: Change Data Capture listener implementation
- Expected: Polls CDC tables, converts to CloudEvents, enriches with metadata
- Critical: Feeds Neo4j provenance graph

### C# Source Code - Neo4jSync Project (2 files)

**src/Neo4jSync/Neo4jSync.csproj**
- Target Framework: .NET 10
- Expected NuGet: Neo4j.Driver, Microsoft.Extensions.Hosting

**src/Neo4jSync/Program.cs**
- Purpose: Service that consumes CDC events and writes to Neo4j
- Expected: Bolt protocol client, Cypher query generation
- Critical: Maintains provenance graph synchronization

### C# Source Code - ModelIngestion Project (15 files)

**src/ModelIngestion/ModelIngestion.csproj**
- Target Framework: .NET 10
- Purpose: Model ingestion utility (likely CLI or one-shot service)
- Expected NuGet: Microsoft.ML.OnnxRuntime, TorchSharp, or similar

**src/ModelIngestion/Program.cs**
- Purpose: Entry point for model ingestion operations
- Expected: CLI argument parsing, orchestration

**src/ModelIngestion/ModelIngestionService.cs**
- Purpose: Core ingestion orchestration logic
- Expected: Reads model file, extracts weights, stores in SQL

**src/ModelIngestion/IngestionOrchestrator.cs**
- Purpose: Higher-level orchestration (parallel naming with service?)
- Indicates: Possible refactoring or dual approaches

**src/ModelIngestion/EmbeddingIngestionService.cs**
- Purpose: Specialized service for embedding models
- Expected: Handles embedding-specific logic (dimension detection, normalization)

**src/ModelIngestion/AtomicStorageService.cs**
- Purpose: Storage service for "atomic" multi-modal data
- Expected: Stores pixels, audio samples, tokens as atoms
- Related: To UnifiedAtomization schema

**src/ModelIngestion/IModelReader.cs**
- Purpose: Interface for model format readers
- Pattern: Strategy pattern for extensibility

**src/ModelIngestion/ModelReaderFactory.cs**
- Purpose: Factory for creating appropriate model reader
- Expected: Detects format (ONNX, PyTorch, Safetensors), returns reader

**src/ModelIngestion/OnnxModelReader.cs**
- Purpose: Reads ONNX format models
- Expected: Uses OnnxRuntime or manual parsing

**src/ModelIngestion/SafetensorsModelReader.cs**
- Purpose: Reads Safetensors format models
- Expected: JSON header parsing, tensor extraction

**src/ModelIngestion/Model.cs**
- Purpose: Domain model representing an ML model
- Expected: Metadata properties (name, architecture, parameters)
- Note: Parallel with Hartonomous.Core.Entities.Model?

**src/ModelIngestion/ModelRepository.cs, ProductionModelRepository.cs**
- Purpose: Data access for model ingestion
- Pattern: Two repositories (dual approaches or refactoring?)
- Indicates: Possible transition from "ProductionModelRepository" to "ModelRepository"

**src/ModelIngestion/TestSqlVector.cs**
- Purpose: Test harness for SQL Server 2025 VECTOR type
- Reason: Verifies VECTOR column read/write from C#
- Evidence: "VECTOR/JSON types verified" in commit message

**src/ModelIngestion/appsettings.json**
- Purpose: Configuration for ModelIngestion service
- Expected: SQL connection string, model directories

**src/ModelIngestion/build_refs.txt**
- Purpose: Build reference notes or dependency list
- Type: Development artifact

**src/ModelIngestion/create_and_save_model.py, parse_onnx.py**
- Purpose: Python utilities for model conversion/testing
- Reason: Likely used to generate test models for ingestion

**src/ModelIngestion/ssd_mobilenet_v2_coco_2018_03_29/** (4 files)
- Purpose: Test model files (TensorFlow checkpoint)
- Files: checkpoint, model.ckpt.index, model.ckpt.meta, pipeline.config
- Reason: Concrete test case for ingestion pipeline
- Note: Large binary files, should be in .gitignore but aren't

### C# Source Code - Hartonomous.Core Project (9 files)

**src/Hartonomous.Core/Hartonomous.Core.csproj**
- Target Framework: .NET 10
- Purpose: Domain entities (DDD approach)
- Expected: No external dependencies (pure domain)

**src/Hartonomous.Core/Entities/Model.cs**
- Purpose: Model aggregate root
- Properties: Id, Name, Architecture, ParameterCount, etc.
- Evidence: EF configuration exists

**src/Hartonomous.Core/Entities/ModelLayer.cs**
- Purpose: Model layer entity
- Properties: LayerId, ModelId, Type, Weights (GEOMETRY LINESTRING?)
- Evidence: EF configuration exists

**src/Hartonomous.Core/Entities/ModelMetadata.cs**
- Purpose: Model metadata entity
- Properties: ModelId, Key, Value (JSON?)
- Evidence: EF configuration exists

**src/Hartonomous.Core/Entities/Embedding.cs**
- Purpose: Embedding entity
- Properties: Id, ModelId, Content, Vector (VECTOR type), CreatedAt
- Evidence: EF configuration exists

**src/Hartonomous.Core/Entities/InferenceRequest.cs**
- Purpose: Inference request entity
- Properties: RequestId, UserId, Prompt, Status, CreatedAt
- Evidence: EF configuration exists

**src/Hartonomous.Core/Entities/InferenceStep.cs**
- Purpose: Individual step in inference process
- Properties: StepId, RequestId, ModelId, Input, Output, Confidence
- Evidence: EF configuration exists

**src/Hartonomous.Core/Entities/CachedActivation.cs**
- Purpose: Cached neural network activations
- Properties: ActivationId, ModelId, Input Hash, Activation (VECTOR?)
- Reason: "80%+ cache hit rate" claim in README
- Evidence: EF configuration exists

**src/Hartonomous.Core/Entities/TokenVocabulary.cs**
- Purpose: Token vocabulary for language models
- Properties: TokenId, Token, Embedding (VECTOR), Frequency
- Related: To 10+ SQL schema fix files for this table
- Evidence: EF configuration exists

### C# Source Code - Hartonomous.Data Project (11 files)

**src/Hartonomous.Data/Hartonomous.Data.csproj**
- Target Framework: .NET 10
- Purpose: EF Core DbContext and configurations
- Expected NuGet: Microsoft.EntityFrameworkCore.SqlServer, NetTopologySuite

**src/Hartonomous.Data/HartonomousDbContext.cs**
- Purpose: Main EF Core DbContext
- Expected DbSets: Models, Embeddings, InferenceRequests, etc.
- Critical: Must configure VECTOR and GEOMETRY column types

**src/Hartonomous.Data/HartonomousDbContextFactory.cs**
- Purpose: Design-time factory for EF tools
- Reason: Enables `dotnet ef migrations add`

**src/Hartonomous.Data/Configurations/ModelConfiguration.cs**
- Purpose: Fluent API configuration for Model entity
- Expected: Table name, primary key, indexes

**src/Hartonomous.Data/Configurations/ModelLayerConfiguration.cs**
- Purpose: Fluent API configuration for ModelLayer
- Expected: GEOMETRY column mapping for Weights

**src/Hartonomous.Data/Configurations/ModelMetadataConfiguration.cs**
- Purpose: Fluent API configuration for ModelMetadata
- Expected: Composite key (ModelId, Key)?

**src/Hartonomous.Data/Configurations/EmbeddingConfiguration.cs**
- Purpose: Fluent API configuration for Embedding
- Critical: VECTOR column type mapping
- Expected: HasConversion or ColumnType("VECTOR(384)")

**src/Hartonomous.Data/Configurations/InferenceRequestConfiguration.cs**
- Purpose: Fluent API configuration for InferenceRequest

**src/Hartonomous.Data/Configurations/InferenceStepConfiguration.cs**
- Purpose: Fluent API configuration for InferenceStep
- Expected: Foreign key to InferenceRequest

**src/Hartonomous.Data/Configurations/CachedActivationConfiguration.cs**
- Purpose: Fluent API configuration for CachedActivation

**src/Hartonomous.Data/Configurations/TokenVocabularyConfiguration.cs**
- Purpose: Fluent API configuration for TokenVocabulary
- Critical: VECTOR column for embedding
- Related: To 10+ schema fix attempts

**src/Hartonomous.Data/Migrations/20251027202323_InitialCreate.Designer.cs**
- Purpose: EF migration designer metadata
- Generated: October 27, 2024 at 20:23 UTC
- Reason: Same day as commit, confirms "EF migration applied"

**src/Hartonomous.Data/Migrations/20251027202323_InitialCreate.cs**
- Purpose: EF migration Up/Down methods
- Expected: CreateTable for all 9 entities
- Critical: Must handle VECTOR and GEOMETRY columns

**src/Hartonomous.Data/Migrations/HartonomousDbContextModelSnapshot.cs**
- Purpose: Current model snapshot for EF
- Reason: Used by EF to detect model changes

### C# Source Code - Hartonomous.Infrastructure Project (8 files)

**src/Hartonomous.Infrastructure/Hartonomous.Infrastructure.csproj**
- Target Framework: .NET 10
- Purpose: Infrastructure services (repositories, external integrations)
- Expected NuGet: Dapper?, Azure SDKs?

**src/Hartonomous.Infrastructure/DependencyInjection.cs**
- Purpose: Service registration extension methods
- Expected: services.AddRepositories(), services.AddInfrastructure()
- Pattern: Clean Architecture DI registration

**src/Hartonomous.Infrastructure/Repositories/IEmbeddingRepository.cs**
- Purpose: Repository interface for Embeddings
- Expected Methods: GetSimilar, FindByContentHash, ComputeSpatialProjection

**src/Hartonomous.Infrastructure/Repositories/EmbeddingRepository.cs**
- Purpose: EF Core implementation of IEmbeddingRepository
- Expected: Uses VECTOR_DISTANCE() for similarity search

**src/Hartonomous.Infrastructure/Repositories/IModelRepository.cs**
- Purpose: Repository interface for Models
- Expected Methods: GetById, GetByName, GetLayers

**src/Hartonomous.Infrastructure/Repositories/ModelRepository.cs**
- Purpose: EF Core implementation of IModelRepository

**src/Hartonomous.Infrastructure/Repositories/IInferenceRepository.cs**
- Purpose: Repository interface for InferenceRequests/Steps
- Expected Methods: CreateRequest, AddStep, GetHistory

**src/Hartonomous.Infrastructure/Repositories/InferenceRepository.cs**
- Purpose: EF Core implementation of IInferenceRepository

**src/Hartonomous.Infrastructure/appsettings.json, appsettings.Development.json**
- Purpose: Configuration for infrastructure services
- Expected: Connection strings, external service endpoints

## Assessment of Initial State

### What Was Working

**Evidence from commit message: "Database working, EF migration applied, VECTOR/JSON types verified"**

1. **SQL Server 2025 Connection:** Verified functional
2. **EF Core 10:** Migration 20251027202323_InitialCreate applied successfully
3. **VECTOR Type:** Read/write from C# confirmed (TestSqlVector.cs)
4. **JSON Type:** Read/write from C# confirmed
5. **Build System:** Solution compiles successfully (7 projects)

### What Was Incomplete

**Evidence from README development checklist:**

- [ ] SQL CLR functions (only scaffolding present)
- [ ] Core SQL schema (schemas exist but not verified deployed)
- [ ] CES consumer service (code exists but not verified running)
- [ ] Neo4j sync service (code exists but not verified running)
- [ ] Model ingestion pipeline (code exists but not verified working)
- [ ] Inference procedures (SQL files exist but not verified functional)
- [ ] Example models (SSD MobileNet present but not confirmed ingested)

### Architectural Concerns from Initial State

**1. File Numbering Collisions (Sabotage Pattern #1)**
- Multiple files numbered 04_, 05_, 06_, 07_
- 10+ files (08-20) all fixing TokenVocabulary
- Indicates: Disorganized development, schema thrashing

**2. Duplicate Concepts**
- ModelRepository.cs vs ProductionModelRepository.cs
- IngestionOrchestrator.cs vs ModelIngestionService.cs
- Model.cs in both ModelIngestion and Hartonomous.Core
- Indicates: Unclear separation of concerns

**3. Missing .gitignore Entries**
- ssd_mobilenet_v2_coco_2018_03_29 directory (4 model files) should be excluded
- Violates stated .gitignore policy for .pb files
- Indicates: .gitignore added after files committed

**4. Documentation Sprawl**
- 13 documentation files (many with "STATUS" in name)
- Indicates: Multiple false starts or pivots

**5. Test Artifacts in Src**
- TestSqlVector.cs in production ModelIngestion project
- Python scripts (create_and_save_model.py) in src/
- Indicates: Lack of test/ directory structure

## Initial State Summary

This commit represents a **partially functional prototype** with:

- ✅ EF Core infrastructure established
- ✅ Multi-project solution structure
- ✅ SQL Server 2025 VECTOR/JSON type integration
- ✅ Comprehensive SQL schema design (on paper)
- ⚠️ Disorganized file numbering and naming
- ⚠️ Duplicate/overlapping concepts
- ⚠️ Incomplete implementation (per README checklist)
- ❌ No tests/ directory
- ❌ No CI/CD
- ❌ Large binary files in repo
- ❌ Schema churn (10+ TokenVocabulary fix attempts)

**Next Commit (66e57ef):** "Phase 1 complete: Structure and cleanup - Added 4 test projects, moved tools to tools/, ModelIngestion from 21 to 15 files"

This indicates the problems identified above were recognized and addressed in the very next commit.
