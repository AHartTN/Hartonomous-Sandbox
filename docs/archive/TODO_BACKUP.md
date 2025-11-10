# TODO List Backup - Before Build/Integration Fix

## PHASE 1: Consolidate duplicate repositories
- **Status**: IN-PROGRESS (partially started, reverted)
- Create IAtomicRepository<TEntity, TKey> to replace IAtomicAudioSampleRepository, IAtomicPixelRepository, IAtomicTextTokenRepository
- Update DI registrations
- Delete 6 files (3 interfaces + 3 implementations that are now generic)
- Estimated reduction: 6 files → 1 generic
- **NOTE**: IAtomicRepository.cs already created but NOT integrated

## PHASE 2: Consolidate search services
- **Status**: NOT STARTED
- Create ISearchService<TQuery, TResult> to replace ISemanticSearchService, ISpatialSearchService
- Semantic uses VectorQuery, Spatial uses SpatialQuery
- Both return AtomEmbeddingSearchResult
- Estimated reduction: 4+ files → 1 generic + 2 query types

## PHASE 3: Audit embedder interfaces
- **Status**: NOT STARTED
- Check if ITextEmbedder, IImageEmbedder, IAudioEmbedder, IVideoEmbedder can collapse to IEmbedder<TInput, TOutput> or IEmbedder<TInput> where TOutput is always float[]
- Currently in Interfaces/Embedders/
- **NOTE**: Embedder interfaces already created in Core/Interfaces/Embedders/

## PHASE 4: Consolidate event processing
- **Status**: NOT STARTED
- Review IEventListener, IEventProcessor, ISemanticEnricher in Interfaces/Events/
- Check if they can use generic IEventHandler<TEvent> pattern
- CloudEvent and ChangeEvent are model classes (move to Models/)

## PHASE 5: Analyze model format readers
- **Status**: NOT STARTED
- OnnxModelReader, PyTorchModelReader, SafetensorsModelReader, GGUFModelReader all implement IModelFormatReader
- Check if their metadata classes (OnnxMetadata, PyTorchMetadata, etc.) share common structure for generic IModelMetadata
- Move metadata to Models/ModelFormats/
- **NOTE**: Metadata classes created in Core/Interfaces/ModelFormats/ (WRONG LOCATION)

## PHASE 6: Repository pattern consolidation
- **Status**: NOT STARTED
- Review if ModelRepository, InferenceRepository, AtomRepository, etc. can leverage more from EfRepository<TEntity> base
- Check for duplicate query patterns that could be generic extension methods

## PHASE 7: After consolidation - reorganize structure
- **Status**: NOT STARTED
- ONLY AFTER reducing file count significantly, apply type-first organization (Repositories/, Services/) with feature subfolders
- Will be much simpler with 200 files instead of 763

## PHASE 8: Extract model classes from Interfaces/
- **Status**: PARTIALLY COMPLETE
- Move IngestionStats, ModelIngestionRequest, ModelIngestionResult from Interfaces/Ingestion/ to Core/Models/Ingestion/
- Move OnnxMetadata, PyTorchMetadata, SafetensorsMetadata, GGUFMetadata, TensorFlowMetadata from Interfaces/ModelFormats/ to Core/Models/ModelFormats/
- **NOTE**: Files created in wrong location, need to move

## PHASE 9: Update using statements
- **Status**: NOT STARTED
- After all consolidation and reorganization, update using statements across 13 projects
- Verify build
- **NOTE**: 105 files reference `using Hartonomous.Core.Interfaces`

## PHASE 10: Mark obsolete classes
- **Status**: NOT STARTED
- Add [Obsolete] to EmbeddingService and other superseded classes

## Split Core multi-class interface files
- **Status**: NOT STARTED
- IModelFormatReader.cs (8 classes)
- IGenericInterfaces.cs (7 classes)
- IEventProcessing.cs (6 classes)
- Split into individual interface files in appropriate subdirectories

## Split Infrastructure multi-class files
- **Status**: NOT STARTED
- IConceptDiscoveryRepository.cs (7 classes)
- GGUFParser.cs (6 classes)
- Separate interfaces from model classes into subdirectories

## Phase 2.4: Extract interfaces for testability
- **Status**: NOT STARTED
- Add interfaces for concrete services that lack them (improve IoC and testing)

## Phase 2.5: Remove circular dependencies
- **Status**: NOT STARTED
- Identify and break circular references between projects/namespaces

## Phase 3.1: Azure Blob Storage for model files
- **Status**: NOT STARTED
- Implement BlobStorageService for storing model files in Azure Storage instead of SQL FILESTREAM

## Phase 3.2: Azure Queue Storage for async jobs
- **Status**: NOT STARTED
- Implement QueueStorageService for async job processing (ingestion jobs, inference requests)

## Phase 3.3: Azure Arc resource management
- **Status**: NOT STARTED
- Add Arc-specific monitoring and resource management for hybrid deployments

## Phase 3.4: Application Insights telemetry
- **Status**: NOT STARTED
- Enhance telemetry with custom metrics, dependency tracking, performance counters for ONNX/Torch operations

## Phase 4.1: SQL CLR ArrayPool + SIMD
- **Status**: NOT STARTED
- Refactor SQL CLR vector operations to use ArrayPool<float> and SIMD intrinsics for 3-5x speedup

## Phase 4.2: Analytics processor struct refactoring
- **Status**: NOT STARTED
- Convert heap-allocated analytics classes to stack-allocated structs for 2-3x speedup in hot paths

---

## CURRENT STATE SUMMARY

### Files Created But Not Integrated (178+):
1. **API DTOs (138+)**:
   - Analytics/ (17 files)
   - Autonomy/ (13 files)
   - Billing/ (11 files)
   - Bulk/ (14 files)
   - Feedback/ (13 files)
   - Generation/ (7 files)
   - Graph/ (34 files in Query/, SqlGraph/, Stats/, Traversal/)
   - Models/ (8 files)
   - Operations/ (21 files)

2. **Core Interfaces**:
   - Generic/ (7 files: IService, IRepository, IFactory, IValidator, IMapper, ICache, IQueue)
   - Events/ (6 files: IEventListener, IEventProcessor, ISemanticEnricher, CloudEvent, ChangeEvent)
   - Embedders/ (5 files: ITextEmbedder, IImageEmbedder, IAudioEmbedder, IVideoEmbedder, IEmbedder)
   - Ingestion/ (3 files - MODEL CLASSES in wrong location)
   - ModelFormats/ (7 files - MODEL CLASSES in wrong location)
   - IAtomicRepository.cs (orphaned generic interface)

3. **Infrastructure**:
   - Extensions: LoggerExtensions, SqlCommandExtensions, SqlDataReaderExtensions, ValidationExtensions
   - Caching: ICacheWarmingStrategy, Strategies/ (4 strategy implementations)
   - Data: HartonomousDbContext, HartonomousDbContextFactory, EfCoreOptimizations, Configurations/ (45 entity configurations)
   - Messaging/Events: 10 event classes
   - Repositories: EfCore/Models/ (21 model files), new repository implementations
   - Services: Embedding/ (4 services), EmbeddingServiceRefactored
   - ModelFormats: 15+ reader/parser/loader implementations
   - Ingestion: 3 ingestion services
   - Prediction: TimeSeriesPredictionService

4. **Tests**:
   - IntegrationTests/Ingestion/: EmbeddingIngestionTests
   - IntegrationTests/Search/: SemanticSearchTests

### Critical Issues:
1. **Build Broken**: Hartonomous.Sql.Bridge.csproj referenced but doesn't exist
2. **Files Not In Projects**: None of the 178+ new files are referenced in any .csproj
3. **Using Statements Outdated**: Code references old namespaces
4. **Misplaced Files**: Model classes in Interfaces/ subdirectories
5. **Multi-Class Files**: Still need splitting (IModelFormatReader, IGenericInterfaces, IEventProcessing, GGUFParser)

### Original Work Status:
- ✅ ALL ORIGINAL WORK INTACT (verified via git status after restore)
- ✅ No deletions, no modifications
- ✅ 178+ new files are additions only

---

## IMMEDIATE PRIORITIES (In Order):

1. **FIX BUILD** - Remove/create Hartonomous.Sql.Bridge project reference
2. **INTEGRATE FILES** - Add all 178+ files to respective .csproj files
3. **COMPLETE SPLITS** - Finish multi-class file splitting
4. **FIX MISPLACEMENTS** - Move model classes from Interfaces/ to Models/
5. **UPDATE USINGS** - Fix namespace references across solution
6. **VERIFY** - Build + test everything works
7. **THEN CONTINUE CONSOLIDATION** - Resume generic pattern work

---

*Backup created: 2025-11-08*
*Commit: 8d90299 - "WIP: Consolidation analysis and new file structure"*
