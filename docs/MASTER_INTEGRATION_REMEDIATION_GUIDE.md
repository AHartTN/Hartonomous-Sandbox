# Hartonomous Master Integration Remediation Guide

## Executive Summary

This document provides a complete inventory of all disconnects between the CLR, SQL, Database, and Application layers in the Hartonomous system. **Nothing is to be removed** - all existing code must be connected and wired together properly.

**Total Work Items:**
- CLR Functions needing SQL wrappers: **63**
- Stored Procedures needing app layer integration: **77**
- Database Functions needing EF Core mappings: **27**
- Service Interfaces needing proper wiring: **6**
- Controllers needing service layer refactoring: **3**

---

## Part 1: CLR → SQL Server Integration

### 1.1 CLR Functions Missing SQL Wrappers

The following 63 CLR functions are defined in C# but have **NO SQL wrapper functions** to expose them to T-SQL:

#### Vector Operations (11 functions)
| C# File | Function Name | Signature | Priority |
|---------|--------------|-----------|----------|
| `CLR/VectorOperations.cs:19` | `clr_VectorDotProduct` | `SqlDouble (SqlBytes vector1, SqlBytes vector2)` | HIGH |
| `CLR/VectorOperations.cs:35` | `clr_VectorCosineSimilarity` | `SqlDouble (SqlBytes vector1, SqlBytes vector2)` | HIGH |
| `CLR/VectorOperations.cs:54` | `clr_VectorEuclideanDistance` | `SqlDouble (SqlBytes vector1, SqlBytes vector2)` | HIGH |
| `CLR/VectorOperations.cs:73` | `clr_VectorAdd` | `SqlBytes (SqlBytes vector1, SqlBytes vector2)` | MEDIUM |
| `CLR/VectorOperations.cs:97` | `clr_VectorSubtract` | `SqlBytes (SqlBytes vector1, SqlBytes vector2)` | MEDIUM |
| `CLR/VectorOperations.cs:121` | `clr_VectorScale` | `SqlBytes (SqlBytes vector, SqlDouble scalar)` | MEDIUM |
| `CLR/VectorOperations.cs:142` | `clr_VectorNorm` | `SqlDouble (SqlBytes vector)` | MEDIUM |
| `CLR/VectorOperations.cs:157` | `clr_VectorNormalize` | `SqlBytes (SqlBytes vector)` | MEDIUM |
| `CLR/VectorOperations.cs:186` | `clr_VectorLerp` | `SqlBytes (SqlBytes v1, SqlBytes v2, SqlDouble t)` | LOW |
| `CLR/VectorOperations.cs:214` | `clr_VectorSoftmax` | `SqlBytes (SqlBytes vector)` | MEDIUM |
| `CLR/VectorOperations.cs:259` | `clr_VectorArgMax` | `SqlInt32 (SqlBytes vector)` | MEDIUM |

#### Audio Processing (6 functions)
| C# File | Function Name | Signature | Priority |
|---------|--------------|-----------|----------|
| `CLR/AudioProcessing.cs:18` | `AudioToWaveform` | `SqlGeometry (SqlBytes audio, SqlInt32 channels, SqlInt32 sampleRate, SqlInt32 maxPoints)` | MEDIUM |
| `CLR/AudioProcessing.cs:74` | `AudioComputeRms` | `SqlDouble (SqlBytes audio, SqlInt32 channels)` | MEDIUM |
| `CLR/AudioProcessing.cs:106` | `AudioComputePeak` | `SqlDouble (SqlBytes audio, SqlInt32 channels)` | MEDIUM |
| `CLR/AudioProcessing.cs:140` | `AudioDownsample` | `SqlBytes (SqlBytes audio, SqlInt32 channels, SqlInt32 factor)` | LOW |
| `CLR/AudioProcessing.cs:215` | `GenerateHarmonicTone` | `SqlBytes (SqlDouble hz, SqlInt32 ms, SqlInt32 rate, ...)` | LOW |
| `CLR/AudioProcessing.cs:328` | `GenerateAudioFromSpatialSignature` | `SqlBytes (SqlGeometry signature)` | LOW |

#### Autonomous Functions (1 function)
| C# File | Function Name | Signature | Priority |
|---------|--------------|-----------|----------|
| `CLR/AutonomousFunctions.cs:155` | `fn_ParseModelCapabilities` | `IEnumerable (SqlString modelName)` | MEDIUM |

#### Attention Generation (1 function)
| C# File | Function Name | Signature | Priority |
|---------|--------------|-----------|----------|
| `CLR/AttentionGeneration.cs:43` | `fn_GenerateWithAttention` | `SqlInt64 (SqlInt32 modelId, SqlString inputAtomIds, ...)` | HIGH |

#### Code Analysis (1 function)
| C# File | Function Name | Signature | Priority |
|---------|--------------|-----------|----------|
| `CLR/CodeAnalysis.cs:35` | `clr_GenerateCodeAstVector` | `SqlString (SqlString sourceCode)` | HIGH |

#### Concept Discovery (3 TVF functions)
| C# File | Function Name | Signature | Priority |
|---------|--------------|-----------|----------|
| `CLR/ConceptDiscovery.cs:28` | `fn_DiscoverConcepts` | `IEnumerable (SqlInt32 minCluster, SqlDouble threshold, ...)` | HIGH |
| `CLR/ConceptDiscovery.cs:251` | `fn_BindConcepts` | `IEnumerable (SqlInt64 atomId, SqlDouble threshold, ...)` | HIGH |
| `CLR/ConceptDiscovery.cs:354` | `fn_BindAtomsToCentroid` | `IEnumerable (SqlBytes centroid, SqlDouble threshold, ...)` | MEDIUM |

#### Embedding Functions (3 functions)
| C# File | Function Name | Signature | Priority |
|---------|--------------|-----------|----------|
| `CLR/EmbeddingFunctions.cs:26` | `fn_ComputeEmbedding` | `SqlBytes (SqlInt64 atomId, SqlInt32 modelId, SqlInt32 tenantId)` | HIGH |
| `CLR/EmbeddingFunctions.cs:112` | `fn_CompareAtoms` | `SqlDouble (SqlInt64 atom1, SqlInt64 atom2, SqlInt32 tenantId)` | HIGH |
| `CLR/EmbeddingFunctions.cs:162` | `fn_MergeAtoms` | `SqlInt64 (SqlInt64 primary, SqlInt64 duplicate, SqlInt32 tenantId)` | MEDIUM |

#### File System Functions (8 functions)
| C# File | Function Name | Signature | Priority |
|---------|--------------|-----------|----------|
| `CLR/FileSystemFunctions.cs:25` | `WriteFileBytes` | `SqlInt64 (SqlString path, SqlBytes content)` | MEDIUM |
| `CLR/FileSystemFunctions.cs:63` | `WriteFileText` | `SqlInt64 (SqlString path, SqlString content)` | MEDIUM |
| `CLR/FileSystemFunctions.cs:99` | `ReadFileBytes` | `SqlBytes (SqlString path)` | HIGH |
| `CLR/FileSystemFunctions.cs:127` | `ReadFileText` | `SqlString (SqlString path)` | HIGH |
| `CLR/FileSystemFunctions.cs:168` | `ExecuteShellCommand` | `IEnumerable (SqlString exe, SqlString args, ...)` | LOW |
| `CLR/FileSystemFunctions.cs:323` | `FileExists` | `SqlBoolean (SqlString path)` | MEDIUM |
| `CLR/FileSystemFunctions.cs:342` | `DirectoryExists` | `SqlBoolean (SqlString path)` | MEDIUM |
| `CLR/FileSystemFunctions.cs:361` | `DeleteFile` | `SqlBoolean (SqlString path)` | LOW |

#### Generation Functions (2 TVF functions)
| C# File | Function Name | Signature | Priority |
|---------|--------------|-----------|----------|
| `CLR/GenerationFunctions.cs:37` | `GenerateSequence` | `IEnumerable (SqlBytes seed, SqlString models, ...)` | HIGH |
| `CLR/GenerationFunctions.cs:66` | `GenerateTextSequence` | `IEnumerable (SqlBytes seed, SqlString models, ...)` | HIGH |

#### Hilbert Curve (4 functions)
| C# File | Function Name | Signature | Priority |
|---------|--------------|-----------|----------|
| `CLR/HilbertCurve.cs:67` | `clr_ComputeMortonValue` | `SqlInt64 (SqlGeometry key, SqlInt32 precision)` | MEDIUM |
| `CLR/HilbertCurve.cs:108` | `clr_InverseMorton` | `SqlGeometry (SqlInt64 value, SqlInt32 precision)` | MEDIUM |
| `CLR/HilbertCurve.cs:146` | `clr_InverseHilbert` | `SqlGeometry (SqlInt64 value, SqlInt32 precision)` | MEDIUM |
| `CLR/HilbertCurve.cs:184` | `clr_HilbertRangeStart` | `SqlInt64 (SqlGeometry bbox, SqlInt32 precision)` | MEDIUM |

#### Image Generation (3 functions)
| C# File | Function Name | Signature | Priority |
|---------|--------------|-----------|----------|
| `CLR/ImageGeneration.cs:26` | `GenerateImageFromShapes` | `SqlBytes (SqlGeometry shapes, SqlInt32 w, SqlInt32 h)` | MEDIUM |
| `CLR/ImageGeneration.cs:175` | `GenerateGuidedPatches` | `IEnumerable (SqlInt32 w, SqlInt32 h, ...)` | MEDIUM |
| `CLR/ImageGeneration.cs:241` | `GenerateGuidedGeometry` | `SqlGeometry (SqlInt32 w, SqlInt32 h, ...)` | MEDIUM |

#### Image Processing (6 functions)
| C# File | Function Name | Signature | Priority |
|---------|--------------|-----------|----------|
| `CLR/ImageProcessing.cs:19` | `ImageToPointCloud` | `SqlGeometry (SqlBytes data, SqlInt32 w, SqlInt32 h, SqlInt32 step)` | MEDIUM |
| `CLR/ImageProcessing.cs:74` | `ImageAverageColor` | `SqlString (SqlBytes data, SqlInt32 w, SqlInt32 h)` | LOW |
| `CLR/ImageProcessing.cs:130` | `ImageLuminanceHistogram` | `SqlString (SqlBytes data, SqlInt32 w, SqlInt32 h, SqlInt32 bins)` | LOW |
| `CLR/ImageProcessing.cs:250` | `GenerateImagePatches` | `IEnumerable (SqlInt32 w, SqlInt32 h, ...)` | MEDIUM |
| `CLR/ImageProcessing.cs:339` | `GenerateImageGeometry` | `SqlGeometry (SqlInt32 w, SqlInt32 h, ...)` | MEDIUM |
| `CLR/ImageProcessing.cs:410` | `DeconstructImageToPatches` | `IEnumerable (SqlBytes raw, SqlInt32 w, SqlInt32 h, ...)` | HIGH |

#### Model Ingestion Functions (2 functions)
| C# File | Function Name | Signature | Priority |
|---------|--------------|-----------|----------|
| `CLR/ModelIngestionFunctions.cs:26` | `ParseGGUFTensorCatalog` | `IEnumerable (SqlGuid payloadId)` | HIGH |
| `CLR/ModelIngestionFunctions.cs:92` | `ReadFilestreamChunk` | `SqlBytes (SqlGuid payloadId, SqlInt64 offset, SqlInt64 size)` | HIGH |

#### Model Inference (1 function)
| C# File | Function Name | Signature | Priority |
|---------|--------------|-----------|----------|
| `CLR/ModelInference.cs:24` | `ExecuteModelInference` | `SqlString (SqlInt32 modelId, SqlBytes embedding)` | HIGH |

#### Model Parsing (1 function)
| C# File | Function Name | Signature | Priority |
|---------|--------------|-----------|----------|
| `CLR/ModelParsing.cs:27` | `clr_ParseModelLayer` | `SqlString (SqlBytes blob, SqlString tensor, SqlString format)` | HIGH |

#### Multi-Modal Generation (5 functions)
| C# File | Function Name | Signature | Priority |
|---------|--------------|-----------|----------|
| `CLR/MultiModalGeneration.cs:23` | `fn_GenerateText` | `SqlInt64 (SqlInt32 modelId, SqlString atoms, ...)` | HIGH |
| `CLR/MultiModalGeneration.cs:58` | `fn_GenerateImage` | `SqlInt64 (SqlInt32 modelId, SqlString atoms, ...)` | MEDIUM |
| `CLR/MultiModalGeneration.cs:93` | `fn_GenerateAudio` | `SqlInt64 (SqlInt32 modelId, SqlString atoms, ...)` | MEDIUM |
| `CLR/MultiModalGeneration.cs:128` | `fn_GenerateVideo` | `SqlInt64 (SqlInt32 modelId, SqlString atoms, ...)` | LOW |
| `CLR/MultiModalGeneration.cs:163` | `fn_GenerateMultiModal` | `SqlInt64 (SqlInt32 modelId, SqlString atoms, ...)` | HIGH |

#### Performance Analysis (3 functions)
| C# File | Function Name | Signature | Priority |
|---------|--------------|-----------|----------|
| `CLR/PerformanceAnalysis.cs:24` | `BuildPerformanceVector` | `SqlBytes (SqlInt32 duration, SqlInt32 tokens, ...)` | LOW |
| `CLR/PerformanceAnalysis.cs:90` | `ComputeZScore` | `SqlDouble (SqlDouble value, SqlDouble mean, SqlDouble stdDev)` | LOW |
| `CLR/PerformanceAnalysis.cs:103` | `IsOutlierIQR` | `SqlBoolean (SqlDouble value, SqlDouble q1, SqlDouble q3, SqlDouble mult)` | LOW |

#### Prime Number Search (1 function)
| C# File | Function Name | Signature | Priority |
|---------|--------------|-----------|----------|
| `CLR/PrimeNumberSearch.cs:19` | `clr_FindPrimes` | `SqlString (SqlInt64 start, SqlInt64 end)` | LOW |

#### Semantic Analysis (1 function)
| C# File | Function Name | Signature | Priority |
|---------|--------------|-----------|----------|
| `CLR/SemanticAnalysis.cs:69` | `ComputeSemanticFeatures` | `SqlString (SqlString input)` | HIGH |

#### Spatial Operations (1 function)
| C# File | Function Name | Signature | Priority |
|---------|--------------|-----------|----------|
| `CLR/SpatialOperations.cs:20` | `fn_ProjectTo3D` | `SqlGeometry (SqlBytes vector)` | HIGH |

#### SVD Geometry Functions (4 functions)
| C# File | Function Name | Signature | Priority |
|---------|--------------|-----------|----------|
| `CLR/SVDGeometryFunctions.cs:23` | `clr_SvdDecompose` | `SqlString (SqlString weights, SqlInt32 rows, SqlInt32 cols, SqlInt32 rank)` | HIGH |
| `CLR/SVDGeometryFunctions.cs:114` | `clr_ProjectToPoint` | `SqlString (SqlString vectorJson)` | HIGH |
| `CLR/SVDGeometryFunctions.cs:151` | `clr_CreateGeometryPointWithImportance` | `SqlString (SqlDouble x, y, z, importance)` | MEDIUM |
| `CLR/SVDGeometryFunctions.cs:173` | `clr_ReconstructFromSVD` | `SqlString (SqlString U, S, VT)` | MEDIUM |

#### Stream Orchestrator (3 functions)
| C# File | Function Name | Signature | Priority |
|---------|--------------|-----------|----------|
| `CLR/StreamOrchestrator.cs:238` | `fn_GetComponentCount` | `SqlInt32 (SqlBytes stream)` | MEDIUM |
| `CLR/StreamOrchestrator.cs:255` | `fn_GetTimeWindow` | `SqlString (SqlBytes stream)` | MEDIUM |
| `CLR/StreamOrchestrator.cs:282` | `fn_DecompressComponents` | `IEnumerable (SqlBytes stream)` | MEDIUM |

### 1.2 SQL Wrapper Template

For each missing CLR function, create a SQL wrapper in `src/Hartonomous.Database/Functions/`:

```sql
-- File: dbo.clr_VectorDotProduct.sql
CREATE FUNCTION [dbo].[clr_VectorDotProduct]
(
    @vector1 VARBINARY(MAX),
    @vector2 VARBINARY(MAX)
)
RETURNS FLOAT
AS EXTERNAL NAME [Hartonomous.Clr].[Hartonomous.Clr.VectorOperations].[VectorDotProduct]
GO
```

### 1.3 Stored Procedures Calling Non-Existent CLR Functions

These stored procedures call CLR functions that don't have SQL wrappers - they will **fail at runtime**:

| Stored Procedure | CLR Function Called | Status |
|-----------------|---------------------|--------|
| `sp_FindNearestAtoms` | `dbo.clr_CosineSimilarity` | **BROKEN** - No wrapper |
| `sp_Act` | `dbo.clr_FindPrimes` | **BROKEN** - No wrapper |
| `sp_AtomizeCode` | `dbo.clr_GenerateCodeAstVector` | **BROKEN** - No wrapper |
| `sp_AtomizeCode` | `dbo.clr_ProjectToPoint` | **BROKEN** - No wrapper |
| `sp_MigratePayloadLocatorToFileStream` | `dbo.clr_ReadFileBytes` | **BROKEN** - No wrapper |
| `sp_FuseMultiModalStreams` | `dbo.clr_StreamOrchestrator` | **BROKEN** - No wrapper |
| `sp_OrchestrateSensorStream` | `dbo.clr_StreamOrchestrator` | **BROKEN** - No wrapper |
| `sp_RunInference` | `dbo.clr_VectorAverage` | **BROKEN** - No wrapper |
| `sp_Converse` | `fn_clr_AnalyzeSystemState` | **BROKEN** - Function doesn't exist |

---

## Part 2: SQL Server → Application Layer Integration

### 2.1 Stored Procedures Inventory

**Total: 79 stored procedures**
**Currently called from app: 2**
**Need app layer integration: 77**

#### OODA Loop Procedures (Autonomous)
| Procedure | Purpose | Service Method Needed |
|-----------|---------|----------------------|
| `sp_Analyze` | OODA Phase 1: Anomaly detection | `IOodaService.AnalyzeAsync()` |
| `sp_Hypothesize` | OODA Phase 2: Generate hypotheses | `IOodaService.HypothesizeAsync()` |
| `sp_Act` | OODA Phase 3: Execute actions | `IOodaService.ActAsync()` |
| `sp_StartPrimeSearch` | Start autonomous compute | `IOodaService.StartPrimeSearchAsync()` |

#### Reasoning Procedures
| Procedure | Purpose | Service Method Needed |
|-----------|---------|----------------------|
| `sp_ChainOfThoughtReasoning` | Multi-step reasoning | `IReasoningService.ChainOfThoughtAsync()` |
| `sp_SelfConsistencyReasoning` | Consensus via sampling | `IReasoningService.SelfConsistencyAsync()` |
| `sp_MultiPathReasoning` | Multiple reasoning paths | `IReasoningService.MultiPathAsync()` |
| `sp_AttentionInference` | Attention-based reasoning | `IReasoningService.AttentionInferenceAsync()` |
| `sp_TransformerStyleInference` | Transformer inference | `IReasoningService.TransformerInferenceAsync()` |
| `sp_Converse` | Agent conversation | `IReasoningService.ConverseAsync()` |

#### Search Procedures
| Procedure | Purpose | Service Method Needed |
|-----------|---------|----------------------|
| `sp_SemanticSearch` | Text similarity search | `ISearchService.SemanticSearchAsync()` |
| `sp_HybridSearch` | BM25 + vector search | `ISearchService.HybridSearchAsync()` |
| `sp_FusionSearch` | Vector + keyword + spatial | `ISearchService.FusionSearchAsync()` |
| `sp_ExactVectorSearch` | Exact vector similarity | `ISearchService.ExactVectorSearchAsync()` |
| `sp_SemanticFilteredSearch` | Filtered vector search | `ISearchService.FilteredSearchAsync()` |
| `sp_TemporalVectorSearch` | Time-bounded search | `ISearchService.TemporalSearchAsync()` |
| `sp_CrossModalQuery` | Cross-modal search | `ISearchService.CrossModalSearchAsync()` |
| `sp_FindKNearestAtoms` | K-NN search | Already implemented |
| `sp_FindNearestAtoms` | Radius search | Already implemented |

#### Generation Procedures
| Procedure | Purpose | Service Method Needed |
|-----------|---------|----------------------|
| `sp_GenerateText` | Text generation | `IGenerationService.GenerateTextAsync()` |
| `sp_GenerateTextSpatial` | Spatial text generation | `IGenerationService.GenerateTextSpatialAsync()` |
| `sp_GenerateWithAttention` | Attention generation | `IGenerationService.GenerateWithAttentionAsync()` |
| `sp_GenerateOptimalPath` | A* pathfinding | `IGenerationService.GenerateOptimalPathAsync()` |
| `sp_SpatialNextToken` | Next token prediction | `IGenerationService.PredictNextTokenAsync()` |

#### Ingestion Procedures
| Procedure | Purpose | Service Method Needed |
|-----------|---------|----------------------|
| `sp_IngestAtoms` | Bulk atom ingestion | `IIngestionService.IngestAtomsAsync()` |
| `sp_IngestModel` | Model registration | `IIngestionService.IngestModelAsync()` |
| `sp_EnqueueIngestion` | Queue ingestion job | `IIngestionService.EnqueueAsync()` |
| `sp_AtomizeCode` | Code atomization | `IAtomizationService.AtomizeCodeAsync()` |
| `sp_AtomizeText_Governed` | Text atomization | `IAtomizationService.AtomizeTextAsync()` |
| `sp_AtomizeImage_Governed` | Image atomization | `IAtomizationService.AtomizeImageAsync()` |
| `sp_AtomizeModel_Governed` | Model atomization | `IAtomizationService.AtomizeModelAsync()` |

#### Inference Procedures
| Procedure | Purpose | Service Method Needed |
|-----------|---------|----------------------|
| `sp_SubmitInferenceJob` | Submit async job | `IInferenceService.SubmitJobAsync()` |
| `sp_GetInferenceJobStatus` | Get job status | `IInferenceService.GetJobStatusAsync()` |
| `sp_UpdateInferenceJobStatus` | Update job status | `IInferenceService.UpdateJobStatusAsync()` |
| `sp_RunInference` | Execute inference | `IInferenceService.RunAsync()` |
| `sp_ScoreWithModel` | Score with model | `IInferenceService.ScoreAsync()` |
| `sp_MultiModelEnsemble` | Ensemble inference | `IInferenceService.EnsembleAsync()` |
| `sp_CompareModelKnowledge` | Compare models | `IInferenceService.CompareModelsAsync()` |

#### Provenance Procedures
| Procedure | Purpose | Service Method Needed |
|-----------|---------|----------------------|
| `sp_LinkProvenance` | Link atom lineage | `IProvenanceService.LinkProvenanceAsync()` |
| `sp_QueryLineage` | Query lineage | `IProvenanceService.QueryLineageAsync()` |
| `sp_ExportProvenance` | Export provenance | `IProvenanceService.ExportAsync()` |
| `sp_ValidateOperationProvenance` | Validate provenance | `IProvenanceService.ValidateAsync()` |
| `sp_AuditProvenanceChain` | Audit provenance | `IProvenanceService.AuditAsync()` |
| `sp_FindImpactedAtoms` | Find downstream atoms | `IProvenanceService.FindImpactedAsync()` |
| `sp_FindRelatedDocuments` | Find related docs | `IProvenanceService.FindRelatedAsync()` |

#### Concept & Semantic Procedures
| Procedure | Purpose | Service Method Needed |
|-----------|---------|----------------------|
| `sp_DiscoverAndBindConcepts` | Concept discovery | `IConceptService.DiscoverAndBindAsync()` |
| `sp_BuildConceptDomains` | Build Voronoi domains | `IConceptService.BuildDomainsAsync()` |
| `sp_ComputeSemanticFeatures` | Compute features | `ISemanticService.ComputeFeaturesAsync()` |
| `sp_ComputeAllSemanticFeatures` | Compute all features | `ISemanticService.ComputeAllFeaturesAsync()` |
| `sp_DetectDuplicates` | Duplicate detection | `ISemanticService.DetectDuplicatesAsync()` |
| `sp_SemanticSimilarity` | Similarity score | `ISemanticService.ComputeSimilarityAsync()` |

#### Model Weight Procedures
| Procedure | Purpose | Service Method Needed |
|-----------|---------|----------------------|
| `sp_CreateWeightSnapshot` | Create snapshot | `IModelWeightService.CreateSnapshotAsync()` |
| `sp_RestoreWeightSnapshot` | Restore snapshot | `IModelWeightService.RestoreSnapshotAsync()` |
| `sp_ListWeightSnapshots` | List snapshots | `IModelWeightService.ListSnapshotsAsync()` |
| `sp_QueryModelWeights` | Query weights | `IModelWeightService.QueryWeightsAsync()` |
| `sp_RollbackWeightsToTimestamp` | Rollback weights | `IModelWeightService.RollbackAsync()` |
| `sp_ReconstructModelWeights` | Reconstruct weights | `IModelWeightService.ReconstructAsync()` |

#### Billing Procedures
| Procedure | Purpose | Service Method Needed |
|-----------|---------|----------------------|
| `sp_CalculateBill` | Calculate bill | `IBillingService.CalculateBillAsync()` |
| `sp_GenerateUsageReport` | Generate report | `IBillingService.GenerateReportAsync()` |
| `sp_GetUsageAnalytics` | Get analytics | `IBillingService.GetAnalyticsAsync()` |

#### Stream Processing Procedures
| Procedure | Purpose | Service Method Needed |
|-----------|---------|----------------------|
| `sp_GenerateEventsFromStream` | Generate events | `IStreamService.GenerateEventsAsync()` |
| `sp_FuseMultiModalStreams` | Fuse streams | `IStreamService.FuseStreamsAsync()` |
| `sp_OrchestrateSensorStream` | Orchestrate sensors | `IStreamService.OrchestrateSensorAsync()` |

#### Utility Procedures
| Procedure | Purpose | Service Method Needed |
|-----------|---------|----------------------|
| `sp_TokenizeText` | Tokenize text | `ITokenizationService.TokenizeAsync()` |
| `sp_TextToEmbedding` | Text to embedding | `IEmbeddingService.TextToEmbeddingAsync()` |
| `sp_CognitiveActivation` | Cognitive activation | `ICognitiveService.ActivateAsync()` |
| `sp_ComputeSpatialProjection` | Spatial projection | `ISpatialService.ProjectAsync()` |
| `sp_ExtractMetadata` | Extract metadata | `IMetadataService.ExtractAsync()` |
| `sp_ResolveTenantGuid` | Resolve tenant | `ITenantService.ResolveGuidAsync()` |
| `sp_MigratePayloadLocatorToFileStream` | Migrate payloads | `IMigrationService.MigratePayloadsAsync()` |
| `sp_GetModelPerformanceMetrics` | Performance metrics | `IModelMetricsService.GetMetricsAsync()` |
| `sp_InferenceHistory` | Inference history | `IHistoryService.GetInferenceHistoryAsync()` |

#### Neo4j Sync Procedures
| Procedure | Purpose | Service Method Needed |
|-----------|---------|----------------------|
| `sp_EnqueueNeo4jSync` | Enqueue sync | `INeo4jSyncService.EnqueueAsync()` |
| `sp_ForwardToNeo4j_Activated` | Process queue | Internal - Service Broker activated |

#### Reconstruction Procedures
| Procedure | Purpose | Service Method Needed |
|-----------|---------|----------------------|
| `sp_ReconstructText` | Reconstruct text | `IReconstructionService.ReconstructTextAsync()` |
| `sp_ReconstructImage` | Reconstruct image | `IReconstructionService.ReconstructImageAsync()` |
| `sp_FindImagesByColor` | Find by color | `IReconstructionService.FindImagesByColorAsync()` |
| `sp_FindWeightsByValueRange` | Find weights | `IReconstructionService.FindWeightsByRangeAsync()` |

### 2.2 Database Functions Inventory

**Total: 27 functions**
**With EF Core mappings: 0**
**Need mappings: 27**

#### CLR Table-Valued Functions (5)
| Function | Schema | EF Core Method Needed |
|----------|--------|----------------------|
| `clr_ExtractAudioFrames` | dbo | `DbContext.ExtractAudioFrames()` |
| `clr_ExtractImagePixels` | dbo | `DbContext.ExtractImagePixels()` |
| `clr_ExtractModelWeights` | dbo | `DbContext.ExtractModelWeights()` |
| `clr_StreamAtomicWeights_Chunked` | dbo | `DbContext.StreamAtomicWeightsChunked()` |
| `clr_EnumerateAtomicStreamSegments` | provenance | `DbContext.EnumerateAtomicStreamSegments()` |

#### CLR Scalar Functions (2)
| Function | Schema | EF Core Method Needed |
|----------|--------|----------------------|
| `clr_AppendAtomicStreamSegment` | provenance | `EF.Functions.AppendAtomicStreamSegment()` |
| `clr_CreateAtomicStream` | provenance | `EF.Functions.CreateAtomicStream()` |

#### T-SQL Scalar Functions (11)
| Function | Schema | EF Core Method Needed |
|----------|--------|----------------------|
| `ConvertVarbinary4ToReal` | dbo | `EF.Functions.ConvertVarbinary4ToReal()` |
| `fn_CalculateComplexity` | dbo | `EF.Functions.CalculateComplexity()` |
| `fn_ComputeGeometryRms` | dbo | `EF.Functions.ComputeGeometryRms()` |
| `fn_ComputeHilbertValue` | dbo | `EF.Functions.ComputeHilbertValue()` |
| `fn_ComputeSpatialBucket` | dbo | `EF.Functions.ComputeSpatialBucket()` |
| `fn_CreateSpatialPoint` | dbo | `EF.Functions.CreateSpatialPoint()` |
| `fn_DetermineSla` | dbo | `EF.Functions.DetermineSla()` |
| `fn_EstimateResponseTime` | dbo | `EF.Functions.EstimateResponseTime()` |
| `fn_NormalizeJSON` | dbo | `EF.Functions.NormalizeJson()` |
| `fn_SoftmaxTemperature` | dbo | `EF.Functions.SoftmaxTemperature()` |
| `fn_VectorCosineSimilarity` | dbo | `EF.Functions.VectorCosineSimilarity()` |
| `GetStatusId` | ref | `EF.Functions.GetStatusId()` |

#### T-SQL Table-Valued Functions (9)
| Function | Schema | EF Core Method Needed |
|----------|--------|----------------------|
| `fn_GetContextCentroid` | dbo | `DbContext.GetContextCentroid()` |
| `fn_GetModelLayers` | dbo | `DbContext.GetModelLayers()` |
| `fn_GetModelPerformanceFiltered` | dbo | `DbContext.GetModelPerformanceFiltered()` |
| `fn_GetModelsPaged` | dbo | `DbContext.GetModelsPaged()` |
| `fn_SpatialKNN` | dbo | `DbContext.SpatialKNN()` |
| `fn_InverseHilbert` | dbo | `EF.Functions.InverseHilbert()` |
| `fn_HilbertRangeStart` | dbo | `EF.Functions.HilbertRangeStart()` |
| `fn_SelectModelsForTask` | dbo | `DbContext.SelectModelsForTask()` |

---

## Part 3: Application Layer Wiring

### 3.1 Service Interfaces Needing Implementation

#### New Interfaces Required

```csharp
// IOodaService.cs - OODA Loop operations
public interface IOodaService
{
    Task<AnalysisResult> AnalyzeAsync(int tenantId, string scope, int lookbackHours, CancellationToken ct);
    Task<HypothesisResult> HypothesizeAsync(Guid analysisId, string observationsJson, CancellationToken ct);
    Task<ActionResult> ActAsync(int tenantId, int autoApproveThreshold, CancellationToken ct);
    Task<Guid> StartPrimeSearchAsync(long rangeStart, long rangeEnd, CancellationToken ct);
}

// ISearchService.cs - All search operations
public interface ISearchService
{
    Task<IEnumerable<SearchResult>> SemanticSearchAsync(string query, int topK, int tenantId, CancellationToken ct);
    Task<IEnumerable<SearchResult>> HybridSearchAsync(string text, byte[] vector, int topK, float textWeight, float vectorWeight, int tenantId, CancellationToken ct);
    Task<IEnumerable<SearchResult>> FusionSearchAsync(byte[] vector, string keywords, Geometry region, int topK, float[] weights, int? tenantId, CancellationToken ct);
    Task<IEnumerable<SearchResult>> ExactVectorSearchAsync(byte[] vector, int topK, int tenantId, string metric, string embeddingType, int? modelId, CancellationToken ct);
    Task<IEnumerable<SearchResult>> FilteredSearchAsync(byte[] vector, string filters, int topK, int tenantId, CancellationToken ct);
    Task<IEnumerable<SearchResult>> TemporalSearchAsync(byte[] vector, DateTime start, DateTime end, int topK, int tenantId, CancellationToken ct);
    Task<IEnumerable<SearchResult>> CrossModalSearchAsync(string textQuery, float? x, float? y, float? z, string modalityFilter, int topK, CancellationToken ct);
}

// IGenerationService.cs - Text/multi-modal generation
public interface IGenerationService
{
    Task<GenerationResult> GenerateTextAsync(string prompt, int maxTokens, float temperature, CancellationToken ct);
    Task<GenerationResult> GenerateTextSpatialAsync(string prompt, int maxTokens, float temperature, CancellationToken ct);
    Task<long> GenerateWithAttentionAsync(int modelId, string inputAtomIds, string context, int maxTokens, float temperature, int topK, float topP, int attentionHeads, int tenantId, CancellationToken ct);
    Task<IEnumerable<PathStep>> GenerateOptimalPathAsync(long startAtomId, int targetConceptId, int maxSteps, float neighborRadius, CancellationToken ct);
    Task<long> PredictNextTokenAsync(long currentAtomId, Geometry direction, int topK, CancellationToken ct);
}

// IInferenceService.cs - Inference operations
public interface IInferenceService
{
    Task<long> SubmitJobAsync(int modelId, string inputData, int priority, int tenantId, Guid? correlationId, CancellationToken ct);
    Task<JobStatus> GetJobStatusAsync(long inferenceId, CancellationToken ct);
    Task UpdateJobStatusAsync(long inferenceId, string status, string outputData, string errorMessage, CancellationToken ct);
    Task<InferenceResult> RunAsync(int modelId, string inputData, int tenantId, Guid? correlationId, CancellationToken ct);
    Task<ScoreResult> ScoreAsync(int modelId, long atomId, int tenantId, CancellationToken ct);
    Task<EnsembleResult> EnsembleAsync(string modelIds, string ensembleType, string inputData, int tenantId, CancellationToken ct);
    Task<ComparisonResult> CompareModelsAsync(int model1Id, int model2Id, int topK, CancellationToken ct);
}

// IAtomizationService.cs - Atomization via stored procedures
public interface IAtomizationService
{
    Task AtomizeCodeAsync(long atomId, int tenantId, string language, CancellationToken ct);
    Task AtomizeTextAsync(long atomId, int tenantId, CancellationToken ct);
    Task AtomizeImageAsync(long atomId, int tenantId, CancellationToken ct);
    Task AtomizeModelAsync(long atomId, int tenantId, CancellationToken ct);
}

// IConceptService.cs - Concept operations
public interface IConceptService
{
    Task<ConceptDiscoveryResult> DiscoverAndBindAsync(int minClusterSize, float coherenceThreshold, int maxConcepts, float similarityThreshold, int maxConceptsPerAtom, int tenantId, bool dryRun, CancellationToken ct);
    Task BuildDomainsAsync(int tenantId, CancellationToken ct);
}

// ISemanticService.cs - Semantic operations
public interface ISemanticService
{
    Task ComputeFeaturesAsync(long atomEmbeddingId, CancellationToken ct);
    Task ComputeAllFeaturesAsync(CancellationToken ct);
    Task<IEnumerable<DuplicateResult>> DetectDuplicatesAsync(float threshold, int batchSize, int tenantId, CancellationToken ct);
    Task<float> ComputeSimilarityAsync(long atom1Id, long atom2Id, int tenantId, CancellationToken ct);
}

// IModelWeightService.cs - Model weight operations
public interface IModelWeightService
{
    Task<int> CreateSnapshotAsync(int modelId, string name, string description, CancellationToken ct);
    Task RestoreSnapshotAsync(int snapshotId, int modelId, CancellationToken ct);
    Task<IEnumerable<WeightSnapshot>> ListSnapshotsAsync(int modelId, CancellationToken ct);
    Task<IEnumerable<ModelWeight>> QueryWeightsAsync(int modelId, string layerName, CancellationToken ct);
    Task RollbackAsync(int modelId, DateTime timestamp, CancellationToken ct);
    Task<IEnumerable<ReconstructedWeight>> ReconstructAsync(int modelId, int? snapshotId, CancellationToken ct);
}

// IBillingService.cs - Billing operations
public interface IBillingService
{
    Task<BillResult> CalculateBillAsync(int tenantId, DateTime periodStart, DateTime periodEnd, CancellationToken ct);
    Task<UsageReport> GenerateReportAsync(int tenantId, string reportType, string timeRange, CancellationToken ct);
    Task<UsageAnalytics> GetAnalyticsAsync(int tenantId, DateTime startDate, DateTime endDate, CancellationToken ct);
}

// IStreamService.cs - Stream operations
public interface IStreamService
{
    Task<IEnumerable<StreamEvent>> GenerateEventsAsync(int streamId, string eventType, float threshold, string clustering, CancellationToken ct);
    Task<FusionResult> FuseStreamsAsync(string streamIds, string fusionType, string weights, CancellationToken ct);
    Task<OrchestrationResult> OrchestrateSensorAsync(string sensorType, DateTime windowStart, DateTime windowEnd, string aggregationLevel, int maxComponents, CancellationToken ct);
}

// ITokenizationService.cs
public interface ITokenizationService
{
    Task<int[]> TokenizeAsync(string text, CancellationToken ct);
}

// IEmbeddingService.cs
public interface IEmbeddingService
{
    Task<(byte[] embedding, int dimension)> TextToEmbeddingAsync(string text, string modelName, CancellationToken ct);
}

// IReconstructionService.cs
public interface IReconstructionService
{
    Task<string> ReconstructTextAsync(long atomId, int tenantId, CancellationToken ct);
    Task<byte[]> ReconstructImageAsync(long atomId, int tenantId, CancellationToken ct);
    Task<IEnumerable<ImageResult>> FindImagesByColorAsync(string colorHex, int tolerance, int topK, CancellationToken ct);
    Task<IEnumerable<WeightResult>> FindWeightsByRangeAsync(float minValue, float maxValue, int modelId, CancellationToken ct);
}
```

### 3.2 Controller → Service Layer Refactoring

#### DataIngestionController (Currently Broken)

**Current State:** Controller directly uses atomizers and bulk insert service
**Target State:** Controller uses `IIngestionService`

```csharp
// BEFORE (broken)
public class DataIngestionController
{
    private readonly IEnumerable<IAtomizer<byte[]>> _atomizers;
    private readonly IAtomBulkInsertService _bulkInsertService;

    public async Task<IActionResult> IngestFile(IFormFile file)
    {
        // Direct atomizer usage - WRONG
        var atomizer = _atomizers.FirstOrDefault(a => a.CanHandle(contentType));
        var result = await atomizer.AtomizeAsync(data, metadata, ct);
        await _bulkInsertService.BulkInsertAtomsAsync(result.Atoms, tenantId, ct);
    }
}

// AFTER (fixed)
public class DataIngestionController
{
    private readonly IIngestionService _ingestionService;

    public async Task<IActionResult> IngestFile(IFormFile file)
    {
        var result = await _ingestionService.IngestFileAsync(data, fileName, tenantId);
        return Ok(result);
    }
}
```

#### AuditController, MLOpsController, ResearchController (DEMO MODE)

**Current State:** Returns hardcoded fake data
**Target State:** Call stored procedures via services

```csharp
// BEFORE (demo mode)
public async Task<IActionResult> GetAuditLogs()
{
    _logger.LogInformation("Audit: Getting logs (DEMO MODE)");
    return Ok(new { DemoMode = true, Data = new[] { /* hardcoded */ } });
}

// AFTER (real)
public async Task<IActionResult> GetAuditLogs()
{
    var results = await _auditService.GetAuditLogsAsync(eventType, userId, ct);
    return Ok(results);
}
```

### 3.3 Media Services Not Registered

The following services exist but are **NOT registered in DI**:

| Interface | Implementation | Action Needed |
|-----------|---------------|---------------|
| `IMediaExtractionService` | `MediaExtractionService` | Register in DI |
| `IFrameExtractionService` | `MediaExtractionService` | Register via composite |
| `IAudioExtractionService` | `MediaExtractionService` | Register via composite |
| `IAudioAnalysisService` | `MediaExtractionService` | Register via composite |
| `IAudioEffectsService` | `MediaExtractionService` | Register via composite |
| `IVideoEditingService` | `MediaExtractionService` | Register via composite |

Add to `ServiceCollectionExtensions.cs`:
```csharp
services.AddScoped<IMediaExtractionService, MediaExtractionService>();
services.AddScoped<IFrameExtractionService>(sp => sp.GetRequiredService<IMediaExtractionService>().FrameExtraction);
services.AddScoped<IAudioExtractionService>(sp => sp.GetRequiredService<IMediaExtractionService>().AudioExtraction);
// etc.
```

### 3.4 IIngestionService Bypassed

**Issue:** `IIngestionService` is registered but `DataIngestionController` doesn't use it

**Fix:** Modify controller to inject and use `IIngestionService`:

```csharp
// In DataIngestionController.cs
public DataIngestionController(
    IIngestionService ingestionService,  // Add this
    ILogger<DataIngestionController> logger)
{
    _ingestionService = ingestionService;
    _logger = logger;
}
```

### 3.5 In-Memory Job Tracking

**Issue:** `DataIngestionController` uses static `ConcurrentDictionary` for job tracking
**Fix:** Use `BackgroundJob` table via `IBackgroundJobService`

```csharp
// Create new service
public interface IBackgroundJobService
{
    Task<Guid> CreateJobAsync(string jobType, string parameters, int tenantId, CancellationToken ct);
    Task<BackgroundJob> GetJobAsync(Guid jobId, CancellationToken ct);
    Task UpdateJobAsync(Guid jobId, string status, string result, CancellationToken ct);
}
```

---

## Part 4: EF Core DbContext Enhancements

### 4.1 Add HasDbFunction Mappings

Add to `HartonomousDbContext.OnModelCreating()`:

```csharp
// Scalar functions
modelBuilder.HasDbFunction(
    typeof(HartonomousDbContext).GetMethod(nameof(CalculateComplexity))!)
    .HasName("fn_CalculateComplexity")
    .HasSchema("dbo");

modelBuilder.HasDbFunction(
    typeof(HartonomousDbContext).GetMethod(nameof(ComputeHilbertValue))!)
    .HasName("fn_ComputeHilbertValue")
    .HasSchema("dbo");

modelBuilder.HasDbFunction(
    typeof(HartonomousDbContext).GetMethod(nameof(VectorCosineSimilarity))!)
    .HasName("fn_VectorCosineSimilarity")
    .HasSchema("dbo");

// Add 24 more...
```

### 4.2 Add DbFunction Method Stubs

Add to `HartonomousDbContext`:

```csharp
// Scalar function stubs
public static int CalculateComplexity(int inputSize, string modelType)
    => throw new NotSupportedException("Direct calls not supported");

public static long ComputeHilbertValue(Geometry spatialKey)
    => throw new NotSupportedException("Direct calls not supported");

public static double VectorCosineSimilarity(byte[] vec1, byte[] vec2)
    => throw new NotSupportedException("Direct calls not supported");

// TVF stubs (return IQueryable)
public IQueryable<ModelLayerResult> GetModelLayers(int modelId)
    => FromExpression(() => GetModelLayers(modelId));

public IQueryable<ModelPerformanceResult> GetModelPerformanceFiltered(int? modelId, DateTime? start, DateTime? end)
    => FromExpression(() => GetModelPerformanceFiltered(modelId, start, end));
```

---

## Part 5: Implementation Checklist

### Phase 1: Fix CLR → SQL (Foundation)
- [ ] Create 63 SQL wrapper functions for CLR
- [ ] Fix 9 broken stored procedures calling non-existent CLR
- [ ] Test all CLR function wrappers

### Phase 2: Create Service Interfaces
- [ ] Create `IOodaService` interface and implementation
- [ ] Create `ISearchService` interface and implementation
- [ ] Create `IGenerationService` interface and implementation
- [ ] Create `IInferenceService` interface and implementation
- [ ] Create `IAtomizationService` interface and implementation
- [ ] Create `IConceptService` interface and implementation
- [ ] Create `ISemanticService` interface and implementation
- [ ] Create `IModelWeightService` interface and implementation
- [ ] Create `IBillingService` interface and implementation
- [ ] Create `IStreamService` interface and implementation
- [ ] Create `ITokenizationService` interface and implementation
- [ ] Create `IEmbeddingService` interface and implementation
- [ ] Create `IReconstructionService` interface and implementation
- [ ] Create `IBackgroundJobService` interface and implementation

### Phase 3: Implement Service Classes
- [ ] Implement all 77 stored procedure calls in appropriate services
- [ ] Add connection string management
- [ ] Add proper error handling and logging
- [ ] Add cancellation token support throughout

### Phase 4: Wire Controllers to Services
- [ ] Refactor `DataIngestionController` to use `IIngestionService`
- [ ] Refactor `AuditController` to use real services (remove DEMO MODE)
- [ ] Refactor `MLOpsController` to use real services (remove DEMO MODE)
- [ ] Refactor `ResearchController` to use real services (remove DEMO MODE)
- [ ] Replace in-memory job tracking with `IBackgroundJobService`

### Phase 5: Add EF Core Mappings
- [ ] Add 27 `HasDbFunction` mappings to DbContext
- [ ] Add method stubs for all database functions
- [ ] Create result types for TVFs

### Phase 6: Register All Services in DI
- [ ] Register all new service interfaces
- [ ] Register `IMediaExtractionService` and related interfaces
- [ ] Verify all registrations with integration tests

### Phase 7: Testing
- [ ] Unit tests for all new services
- [ ] Integration tests for stored procedure calls
- [ ] End-to-end tests for complete workflows

---

## Appendix A: File Locations Summary

### CLR Source Files
`src/Hartonomous.Database/CLR/`

### SQL Functions
`src/Hartonomous.Database/Functions/`

### Stored Procedures
`src/Hartonomous.Database/Procedures/`
`src/Hartonomous.Database/StoredProcedures/`

### Service Interfaces
`src/Hartonomous.Core/Interfaces/`
`src/Hartonomous.Core/Services/`

### Service Implementations
`src/Hartonomous.Infrastructure/Services/`

### DI Registration
`src/Hartonomous.Infrastructure/Configurations/ServiceCollectionExtensions.cs`
`src/Hartonomous.Infrastructure/Configurations/IngestionServiceRegistration.cs`
`src/Hartonomous.Infrastructure/Configurations/BusinessServiceRegistration.cs`

### API Controllers
`src/Hartonomous.Api/Controllers/`

### DbContext
`src/Hartonomous.Data.Entities/HartonomousDbContext.cs`
