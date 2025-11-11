-- Ensures SQL CLR bindings are present before referencing helper functions from stored procedures.

IF OBJECT_ID('dbo.clr_VectorDotProduct', 'FN') IS NOT NULL DROP FUNCTION dbo.clr_VectorDotProduct;
GO
CREATE FUNCTION dbo.clr_VectorDotProduct(@v1 VARBINARY(MAX), @v2 VARBINARY(MAX))
RETURNS FLOAT
AS EXTERNAL NAME SqlClrFunctions.[SqlClrFunctions.VectorOperations].VectorDotProduct;
GO

IF OBJECT_ID('dbo.clr_VectorCosineSimilarity', 'FN') IS NOT NULL DROP FUNCTION dbo.clr_VectorCosineSimilarity;
GO
CREATE FUNCTION dbo.clr_VectorCosineSimilarity(@v1 VARBINARY(MAX), @v2 VARBINARY(MAX))
RETURNS FLOAT
AS EXTERNAL NAME SqlClrFunctions.[SqlClrFunctions.VectorOperations].VectorCosineSimilarity;
GO

IF OBJECT_ID('dbo.clr_VectorEuclideanDistance', 'FN') IS NOT NULL DROP FUNCTION dbo.clr_VectorEuclideanDistance;
GO
CREATE FUNCTION dbo.clr_VectorEuclideanDistance(@v1 VARBINARY(MAX), @v2 VARBINARY(MAX))
RETURNS FLOAT
AS EXTERNAL NAME SqlClrFunctions.[SqlClrFunctions.VectorOperations].VectorEuclideanDistance;
GO

IF OBJECT_ID('dbo.clr_VectorNormalize', 'FN') IS NOT NULL DROP FUNCTION dbo.clr_VectorNormalize;
GO
CREATE FUNCTION dbo.clr_VectorNormalize(@vector VARBINARY(MAX))
RETURNS VARBINARY(MAX)
AS EXTERNAL NAME SqlClrFunctions.[SqlClrFunctions.VectorOperations].VectorNormalize;
GO

IF OBJECT_ID('dbo.clr_VectorSoftmax', 'FN') IS NOT NULL DROP FUNCTION dbo.clr_VectorSoftmax;
GO
CREATE FUNCTION dbo.clr_VectorSoftmax(@vector VARBINARY(MAX))
RETURNS VARBINARY(MAX)
AS EXTERNAL NAME SqlClrFunctions.[SqlClrFunctions.VectorOperations].VectorSoftmax;
GO

IF OBJECT_ID('dbo.clr_VectorArgMax', 'FN') IS NOT NULL DROP FUNCTION dbo.clr_VectorArgMax;
GO
CREATE FUNCTION dbo.clr_VectorArgMax(@vector VARBINARY(MAX))
RETURNS INT
AS EXTERNAL NAME SqlClrFunctions.[SqlClrFunctions.VectorOperations].VectorArgMax;
GO

IF OBJECT_ID('dbo.clr_VectorAdd', 'FN') IS NOT NULL DROP FUNCTION dbo.clr_VectorAdd;
GO
CREATE FUNCTION dbo.clr_VectorAdd(@v1 VARBINARY(MAX), @v2 VARBINARY(MAX))
RETURNS VARBINARY(MAX)
AS EXTERNAL NAME SqlClrFunctions.[SqlClrFunctions.VectorOperations].VectorAdd;
GO

IF OBJECT_ID('dbo.clr_SemanticFeaturesJson', 'FN') IS NOT NULL DROP FUNCTION dbo.clr_SemanticFeaturesJson;
GO
CREATE FUNCTION dbo.clr_SemanticFeaturesJson(@text NVARCHAR(MAX))
RETURNS NVARCHAR(MAX)
AS EXTERNAL NAME SqlClrFunctions.[SqlClrFunctions.SemanticAnalysis].ComputeSemanticFeatures;
GO

IF OBJECT_ID('dbo.clr_VectorSubtract', 'FN') IS NOT NULL DROP FUNCTION dbo.clr_VectorSubtract;
GO
CREATE FUNCTION dbo.clr_VectorSubtract(@v1 VARBINARY(MAX), @v2 VARBINARY(MAX))
RETURNS VARBINARY(MAX)
AS EXTERNAL NAME SqlClrFunctions.[SqlClrFunctions.VectorOperations].VectorSubtract;
GO

IF OBJECT_ID('dbo.clr_VectorScale', 'FN') IS NOT NULL DROP FUNCTION dbo.clr_VectorScale;
GO
CREATE FUNCTION dbo.clr_VectorScale(@vector VARBINARY(MAX), @scalar FLOAT)
RETURNS VARBINARY(MAX)
AS EXTERNAL NAME SqlClrFunctions.[SqlClrFunctions.VectorOperations].VectorScale;
GO

IF OBJECT_ID('dbo.clr_VectorLerp', 'FN') IS NOT NULL DROP FUNCTION dbo.clr_VectorLerp;
GO
CREATE FUNCTION dbo.clr_VectorLerp(@v1 VARBINARY(MAX), @v2 VARBINARY(MAX), @t FLOAT)
RETURNS VARBINARY(MAX)
AS EXTERNAL NAME SqlClrFunctions.[SqlClrFunctions.VectorOperations].VectorLerp;
GO

IF OBJECT_ID('dbo.clr_VectorNorm', 'FN') IS NOT NULL DROP FUNCTION dbo.clr_VectorNorm;
GO
CREATE FUNCTION dbo.clr_VectorNorm(@vector VARBINARY(MAX))
RETURNS FLOAT
AS EXTERNAL NAME SqlClrFunctions.[SqlClrFunctions.VectorOperations].VectorNorm;
GO

IF OBJECT_ID('dbo.clr_ImageToPointCloud', 'FN') IS NOT NULL DROP FUNCTION dbo.clr_ImageToPointCloud;
GO
CREATE FUNCTION dbo.clr_ImageToPointCloud(@image VARBINARY(MAX), @width INT, @height INT, @sampleStep INT)
RETURNS GEOMETRY
AS EXTERNAL NAME SqlClrFunctions.[SqlClrFunctions.ImageProcessing].ImageToPointCloud;
GO

IF OBJECT_ID('dbo.clr_ImageAverageColor', 'FN') IS NOT NULL DROP FUNCTION dbo.clr_ImageAverageColor;
GO
CREATE FUNCTION dbo.clr_ImageAverageColor(@image VARBINARY(MAX), @width INT, @height INT)
RETURNS NVARCHAR(MAX)
AS EXTERNAL NAME SqlClrFunctions.[SqlClrFunctions.ImageProcessing].ImageAverageColor;
GO

IF OBJECT_ID('dbo.clr_ImageLuminanceHistogram', 'FN') IS NOT NULL DROP FUNCTION dbo.clr_ImageLuminanceHistogram;
GO
CREATE FUNCTION dbo.clr_ImageLuminanceHistogram(@image VARBINARY(MAX), @width INT, @height INT, @binCount INT)
RETURNS NVARCHAR(MAX)
AS EXTERNAL NAME SqlClrFunctions.[SqlClrFunctions.ImageProcessing].ImageLuminanceHistogram;
GO

IF OBJECT_ID('dbo.clr_GenerateImagePatches', 'TF') IS NOT NULL DROP FUNCTION dbo.clr_GenerateImagePatches;
GO
CREATE FUNCTION dbo.clr_GenerateImagePatches(
    @width INT,
    @height INT,
    @patchSize INT,
    @steps INT,
    @guidanceScale FLOAT,
    @guideX FLOAT,
    @guideY FLOAT,
    @guideZ FLOAT,
    @seed INT
)
RETURNS TABLE (
    patch_x INT,
    patch_y INT,
    spatial_x FLOAT,
    spatial_y FLOAT,
    spatial_z FLOAT,
    patch GEOMETRY
)
AS EXTERNAL NAME SqlClrFunctions.[SqlClrFunctions.ImageGeneration].GenerateGuidedPatches;
GO

IF OBJECT_ID('dbo.clr_GenerateImageGeometry', 'FN') IS NOT NULL DROP FUNCTION dbo.clr_GenerateImageGeometry;
GO
CREATE FUNCTION dbo.clr_GenerateImageGeometry(
    @width INT,
    @height INT,
    @patchSize INT,
    @steps INT,
    @guidanceScale FLOAT,
    @guideX FLOAT,
    @guideY FLOAT,
    @guideZ FLOAT,
    @seed INT
)
RETURNS GEOMETRY
AS EXTERNAL NAME SqlClrFunctions.[SqlClrFunctions.ImageGeneration].GenerateGuidedGeometry;
GO

IF OBJECT_ID('dbo.clr_AudioToWaveform', 'FN') IS NOT NULL DROP FUNCTION dbo.clr_AudioToWaveform;
GO
CREATE FUNCTION dbo.clr_AudioToWaveform(@audio VARBINARY(MAX), @channels INT, @sampleRate INT, @maxPoints INT)
RETURNS GEOMETRY
AS EXTERNAL NAME SqlClrFunctions.[SqlClrFunctions.AudioProcessing].AudioToWaveform;
GO

IF OBJECT_ID('dbo.clr_AudioComputeRms', 'FN') IS NOT NULL DROP FUNCTION dbo.clr_AudioComputeRms;
GO
CREATE FUNCTION dbo.clr_AudioComputeRms(@audio VARBINARY(MAX), @channels INT)
RETURNS FLOAT
AS EXTERNAL NAME SqlClrFunctions.[SqlClrFunctions.AudioProcessing].AudioComputeRms;
GO

IF OBJECT_ID('dbo.clr_AudioComputePeak', 'FN') IS NOT NULL DROP FUNCTION dbo.clr_AudioComputePeak;
GO
CREATE FUNCTION dbo.clr_AudioComputePeak(@audio VARBINARY(MAX), @channels INT)
RETURNS FLOAT
AS EXTERNAL NAME SqlClrFunctions.[SqlClrFunctions.AudioProcessing].AudioComputePeak;
GO

IF OBJECT_ID('dbo.clr_AudioDownsample', 'FN') IS NOT NULL DROP FUNCTION dbo.clr_AudioDownsample;
GO
CREATE FUNCTION dbo.clr_AudioDownsample(@audio VARBINARY(MAX), @channels INT, @factor INT)
RETURNS VARBINARY(MAX)
AS EXTERNAL NAME SqlClrFunctions.[SqlClrFunctions.AudioProcessing].AudioDownsample;
GO

IF OBJECT_ID('dbo.clr_GenerateHarmonicTone', 'FN') IS NOT NULL DROP FUNCTION dbo.clr_GenerateHarmonicTone;
GO
CREATE FUNCTION dbo.clr_GenerateHarmonicTone(
    @fundamentalHz FLOAT,
    @durationMs INT,
    @sampleRate INT,
    @channelCount INT,
    @amplitude FLOAT,
    @secondHarmonic FLOAT = NULL,
    @thirdHarmonic FLOAT = NULL
)
RETURNS VARBINARY(MAX)
AS EXTERNAL NAME SqlClrFunctions.[SqlClrFunctions.AudioProcessing].GenerateHarmonicTone;
GO

IF OBJECT_ID('dbo.clr_GenerateSequence', 'TF') IS NOT NULL DROP FUNCTION dbo.clr_GenerateSequence;
GO
CREATE FUNCTION dbo.clr_GenerateSequence(
    @seedEmbedding VARBINARY(MAX),
    @modelsJson NVARCHAR(MAX),
    @maxTokens INT,
    @temperature FLOAT,
    @topK INT,
    @requiredModality NVARCHAR(64)
)
RETURNS TABLE (
    step_number INT,
    atom_id BIGINT,
    token NVARCHAR(400),
    score FLOAT,
    distance FLOAT,
    model_count INT,
    duration_ms INT
)
AS EXTERNAL NAME SqlClrFunctions.[SqlClrFunctions.GenerationFunctions].GenerateSequence;
GO

IF OBJECT_ID('dbo.clr_GenerateTextSequence', 'TF') IS NOT NULL DROP FUNCTION dbo.clr_GenerateTextSequence;
GO
CREATE FUNCTION dbo.clr_GenerateTextSequence(
    @seedEmbedding VARBINARY(MAX),
    @modelsJson NVARCHAR(MAX),
    @maxTokens INT,
    @temperature FLOAT,
    @topK INT
)
RETURNS TABLE (
    AtomId BIGINT,
    Token NVARCHAR(400),
    Score FLOAT,
    Distance FLOAT,
    ModelCount INT,
    DurationMs INT
)
AS EXTERNAL NAME SqlClrFunctions.[SqlClrFunctions.GenerationFunctions].GenerateTextSequence;
GO

IF OBJECT_ID('dbo.fn_DiscoverConcepts', 'TF') IS NOT NULL DROP FUNCTION dbo.fn_DiscoverConcepts;
GO
CREATE FUNCTION dbo.fn_DiscoverConcepts(
    @MinClusterSize INT,
    @CoherenceThreshold FLOAT,
    @MaxConcepts INT,
    @TenantId INT
)
RETURNS TABLE (
    ConceptId UNIQUEIDENTIFIER,
    Centroid VARBINARY(MAX),
    AtomCount INT,
    Coherence FLOAT,
    SpatialBucket INT
)
AS EXTERNAL NAME SqlClrFunctions.[SqlClrFunctions.ConceptDiscovery].fn_DiscoverConcepts;
GO

IF OBJECT_ID('dbo.fn_BindConcepts', 'TF') IS NOT NULL DROP FUNCTION dbo.fn_BindConcepts;
GO
CREATE FUNCTION dbo.fn_BindConcepts(
    @AtomId BIGINT,
    @SimilarityThreshold FLOAT,
    @MaxConceptsPerAtom INT,
    @TenantId INT
)
RETURNS TABLE (
    AtomId BIGINT,
    ConceptId UNIQUEIDENTIFIER,
    Similarity FLOAT,
    IsPrimary BIT
)
AS EXTERNAL NAME SqlClrFunctions.[SqlClrFunctions.ConceptBinding].fn_BindConcepts;
GO

IF OBJECT_ID('dbo.clr_DeconstructImageToPatches', 'TF') IS NOT NULL DROP FUNCTION dbo.clr_DeconstructImageToPatches;
GO
CREATE FUNCTION dbo.clr_DeconstructImageToPatches(
    @rawImage VARBINARY(MAX),
    @imageWidth INT,
    @imageHeight INT,
    @patchSize INT,
    @strideSize INT
)
RETURNS TABLE (
    PatchIndex INT,
    RowIndex INT,
    ColIndex INT,
    PatchX INT,
    PatchY INT,
    PatchWidth INT,
    PatchHeight INT,
    PatchGeometry GEOMETRY,
    MeanR FLOAT,
    MeanG FLOAT,
    MeanB FLOAT,
    Variance FLOAT
)
AS EXTERNAL NAME SqlClrFunctions.[SqlClrFunctions.ImageProcessing].DeconstructImageToPatches;
GO

-- ========================================
-- Code Analysis Functions (AST-as-GEOMETRY)
-- ========================================

IF OBJECT_ID('dbo.clr_GenerateCodeAstVector', 'FN') IS NOT NULL DROP FUNCTION dbo.clr_GenerateCodeAstVector;
GO
CREATE FUNCTION dbo.clr_GenerateCodeAstVector(@sourceCode NVARCHAR(MAX))
RETURNS NVARCHAR(MAX)
AS EXTERNAL NAME SqlClrFunctions.[SqlClrFunctions.CodeAnalysis].clr_GenerateCodeAstVector;
GO

-- ========================================
-- SVD-as-GEOMETRY Pipeline Functions
-- ========================================

IF OBJECT_ID('dbo.clr_ParseModelLayer', 'FN') IS NOT NULL DROP FUNCTION dbo.clr_ParseModelLayer;
GO
CREATE FUNCTION dbo.clr_ParseModelLayer(
    @modelBlob VARBINARY(MAX),
    @tensorName NVARCHAR(256),
    @modelFormatHint NVARCHAR(50)
)
RETURNS NVARCHAR(MAX)
AS EXTERNAL NAME SqlClrFunctions.[SqlClrFunctions.ModelParsing].clr_ParseModelLayer;
GO

IF OBJECT_ID('dbo.clr_SvdDecompose', 'FN') IS NOT NULL DROP FUNCTION dbo.clr_SvdDecompose;
GO
CREATE FUNCTION dbo.clr_SvdDecompose(
    @weightArrayJson NVARCHAR(MAX),
    @rows INT,
    @cols INT,
    @maxRank INT
)
RETURNS NVARCHAR(MAX)
AS EXTERNAL NAME SqlClrFunctions.[SqlClrFunctions.SVDGeometryFunctions].clr_SvdDecompose;
GO

IF OBJECT_ID('dbo.clr_ProjectToPoint', 'FN') IS NOT NULL DROP FUNCTION dbo.clr_ProjectToPoint;
GO
CREATE FUNCTION dbo.clr_ProjectToPoint(@vectorJson NVARCHAR(MAX))
RETURNS NVARCHAR(MAX)
AS EXTERNAL NAME SqlClrFunctions.[SqlClrFunctions.SVDGeometryFunctions].clr_ProjectToPoint;
GO

IF OBJECT_ID('dbo.clr_CreateGeometryPointWithImportance', 'FN') IS NOT NULL DROP FUNCTION dbo.clr_CreateGeometryPointWithImportance;
GO
CREATE FUNCTION dbo.clr_CreateGeometryPointWithImportance(
    @x FLOAT,
    @y FLOAT,
    @z FLOAT,
    @importance FLOAT
)
RETURNS NVARCHAR(MAX)
AS EXTERNAL NAME SqlClrFunctions.[SqlClrFunctions.SVDGeometryFunctions].clr_CreateGeometryPointWithImportance;
GO

IF OBJECT_ID('dbo.clr_ReconstructFromSVD', 'FN') IS NOT NULL DROP FUNCTION dbo.clr_ReconstructFromSVD;
GO
CREATE FUNCTION dbo.clr_ReconstructFromSVD(
    @UJson NVARCHAR(MAX),
    @SJson NVARCHAR(MAX),
    @VTJson NVARCHAR(MAX)
)
RETURNS NVARCHAR(MAX)
AS EXTERNAL NAME SqlClrFunctions.[SqlClrFunctions.SVDGeometryFunctions].clr_ReconstructFromSVD;
GO

-- ========================================
-- Tensor Data I/O Functions
-- ========================================

IF OBJECT_ID('dbo.clr_StoreTensorAtomPayload', 'P') IS NOT NULL DROP PROCEDURE dbo.clr_StoreTensorAtomPayload;
GO
CREATE PROCEDURE dbo.clr_StoreTensorAtomPayload
    @tensorAtomId BIGINT,
    @payload VARBINARY(MAX)
AS EXTERNAL NAME SqlClrFunctions.[SqlClrFunctions.TensorDataIO].clr_StoreTensorAtomPayload;
GO

IF OBJECT_ID('dbo.clr_JsonFloatArrayToBytes', 'FN') IS NOT NULL DROP FUNCTION dbo.clr_JsonFloatArrayToBytes;
GO
CREATE FUNCTION dbo.clr_JsonFloatArrayToBytes(@jsonFloatArray NVARCHAR(MAX))
RETURNS VARBINARY(MAX)
AS EXTERNAL NAME SqlClrFunctions.[SqlClrFunctions.TensorDataIO].clr_JsonFloatArrayToBytes;
GO

IF OBJECT_ID('dbo.clr_GetTensorAtomPayload', 'FN') IS NOT NULL DROP FUNCTION dbo.clr_GetTensorAtomPayload;
GO
CREATE FUNCTION dbo.clr_GetTensorAtomPayload(@tensorAtomId BIGINT)
RETURNS VARBINARY(MAX)
AS EXTERNAL NAME SqlClrFunctions.[SqlClrFunctions.TensorDataIO].clr_GetTensorAtomPayload;
GO

-- ========================================
-- Trajectory & Path Analysis Aggregates
-- ========================================

IF OBJECT_ID('dbo.agg_BuildPathFromAtoms', 'AF') IS NOT NULL DROP AGGREGATE dbo.agg_BuildPathFromAtoms;
GO
CREATE AGGREGATE dbo.agg_BuildPathFromAtoms(
    @atomId BIGINT,
    @timestamp DATETIME
)
RETURNS GEOMETRY
EXTERNAL NAME SqlClrFunctions.[SqlClrFunctions.BuildPathFromAtoms];
GO

-- ========================================
-- Shape-to-Content Generation Functions (Phase 4)
-- ========================================

IF OBJECT_ID('dbo.clr_GenerateImageFromShapes', 'FN') IS NOT NULL DROP FUNCTION dbo.clr_GenerateImageFromShapes;
GO
CREATE FUNCTION dbo.clr_GenerateImageFromShapes(
    @shapes GEOMETRY,
    @width INT,
    @height INT
)
RETURNS VARBINARY(MAX)
AS EXTERNAL NAME SqlClrFunctions.[SqlClrFunctions.ImageGeneration].GenerateImageFromShapes;
GO

IF OBJECT_ID('dbo.clr_GenerateAudioFromSpatialSignature', 'FN') IS NOT NULL DROP FUNCTION dbo.clr_GenerateAudioFromSpatialSignature;
GO
CREATE FUNCTION dbo.clr_GenerateAudioFromSpatialSignature(@spatialSignature GEOMETRY)
RETURNS VARBINARY(MAX)
AS EXTERNAL NAME SqlClrFunctions.[SqlClrFunctions.AudioProcessing].GenerateAudioFromSpatialSignature;
GO

IF OBJECT_ID('dbo.clr_SynthesizeModelLayer', 'FN') IS NOT NULL DROP FUNCTION dbo.clr_SynthesizeModelLayer;
GO
CREATE FUNCTION dbo.clr_SynthesizeModelLayer(
    @queryShape GEOMETRY,
    @parentLayerId BIGINT
)
RETURNS NVARCHAR(MAX)
AS EXTERNAL NAME SqlClrFunctions.[SqlClrFunctions.TensorOperations.ModelSynthesis].clr_SynthesizeModelLayer;
GO

IF OBJECT_ID('dbo.clr_BytesToFloatArrayJson', 'FN') IS NOT NULL DROP FUNCTION dbo.clr_BytesToFloatArrayJson;
GO
CREATE FUNCTION dbo.clr_BytesToFloatArrayJson(@bytes VARBINARY(MAX))
RETURNS NVARCHAR(MAX)
AS EXTERNAL NAME SqlClrFunctions.[SqlClrFunctions.TensorDataIO].clr_BytesToFloatArrayJson;
GO

-- ========================================
-- Transformer & Model Operations
-- ========================================

IF OBJECT_ID('dbo.clr_RunInference', 'FN') IS NOT NULL DROP FUNCTION dbo.clr_RunInference;
GO
CREATE FUNCTION dbo.clr_RunInference(@modelId INT, @tokenIdsJson NVARCHAR(MAX))
RETURNS NVARCHAR(MAX)
AS EXTERNAL NAME SqlClrFunctions.[SqlClrFunctions.TensorOperations.TransformerInference].clr_RunInference;
GO

-- ========================================
-- Multi-Modal Generation Functions
-- ========================================

IF OBJECT_ID('dbo.fn_GenerateText', 'FN') IS NOT NULL DROP FUNCTION dbo.fn_GenerateText;
GO
CREATE FUNCTION dbo.fn_GenerateText(
    @modelId INT,
    @inputAtomIds NVARCHAR(MAX),
    @contextJson NVARCHAR(MAX),
    @maxTokens INT,
    @temperature FLOAT,
    @topK INT,
    @topP FLOAT,
    @tenantId INT
)
RETURNS BIGINT
AS EXTERNAL NAME SqlClrFunctions.[SqlClrFunctions.MultiModalGeneration].fn_GenerateText;
GO

IF OBJECT_ID('dbo.fn_GenerateImage', 'FN') IS NOT NULL DROP FUNCTION dbo.fn_GenerateImage;
GO
CREATE FUNCTION dbo.fn_GenerateImage(
    @modelId INT,
    @inputAtomIds NVARCHAR(MAX),
    @contextJson NVARCHAR(MAX),
    @maxTokens INT,
    @temperature FLOAT,
    @topK INT,
    @topP FLOAT,
    @tenantId INT
)
RETURNS BIGINT
AS EXTERNAL NAME SqlClrFunctions.[SqlClrFunctions.MultiModalGeneration].fn_GenerateImage;
GO

IF OBJECT_ID('dbo.fn_GenerateAudio', 'FN') IS NOT NULL DROP FUNCTION dbo.fn_GenerateAudio;
GO
CREATE FUNCTION dbo.fn_GenerateAudio(
    @modelId INT,
    @inputAtomIds NVARCHAR(MAX),
    @contextJson NVARCHAR(MAX),
    @maxTokens INT,
    @temperature FLOAT,
    @topK INT,
    @topP FLOAT,
    @tenantId INT
)
RETURNS BIGINT
AS EXTERNAL NAME SqlClrFunctions.[SqlClrFunctions.MultiModalGeneration].fn_GenerateAudio;
GO

IF OBJECT_ID('dbo.fn_GenerateVideo', 'FN') IS NOT NULL DROP FUNCTION dbo.fn_GenerateVideo;
GO
CREATE FUNCTION dbo.fn_GenerateVideo(
    @modelId INT,
    @inputAtomIds NVARCHAR(MAX),
    @contextJson NVARCHAR(MAX),
    @maxTokens INT,
    @temperature FLOAT,
    @topK INT,
    @topP FLOAT,
    @tenantId INT
)
RETURNS BIGINT
AS EXTERNAL NAME SqlClrFunctions.[SqlClrFunctions.MultiModalGeneration].fn_GenerateVideo;
GO

IF OBJECT_ID('dbo.fn_GenerateEnsemble', 'FN') IS NOT NULL DROP FUNCTION dbo.fn_GenerateEnsemble;
GO
CREATE FUNCTION dbo.fn_GenerateEnsemble(
    @modelIdsJson NVARCHAR(MAX),
    @inputAtomIds NVARCHAR(MAX),
    @contextJson NVARCHAR(MAX),
    @maxTokens INT,
    @temperature FLOAT,
    @topK INT,
    @topP FLOAT,
    @attentionHeads INT,
    @tenantId INT
)
RETURNS BIGINT
AS EXTERNAL NAME SqlClrFunctions.[SqlClrFunctions.MultiModalGeneration].fn_GenerateEnsemble;
GO

IF OBJECT_ID('dbo.fn_GenerateWithAttention', 'FN') IS NOT NULL DROP FUNCTION dbo.fn_GenerateWithAttention;
GO
CREATE FUNCTION dbo.fn_GenerateWithAttention(
    @modelId INT,
    @inputAtomIds NVARCHAR(MAX),
    @contextJson NVARCHAR(MAX),
    @maxTokens INT,
    @temperature FLOAT,
    @topK INT,
    @topP FLOAT,
    @attentionHeads INT,
    @tenantId INT
)
RETURNS BIGINT
AS EXTERNAL NAME SqlClrFunctions.[SqlClrFunctions.AttentionGeneration].fn_GenerateWithAttention;
GO

-- ========================================
-- File System Functions
-- ========================================

IF OBJECT_ID('dbo.clr_FileExists', 'FN') IS NOT NULL DROP FUNCTION dbo.clr_FileExists;
GO
CREATE FUNCTION dbo.clr_FileExists(@filePath NVARCHAR(MAX))
RETURNS BIT
AS EXTERNAL NAME SqlClrFunctions.[SqlClrFunctions.FileSystemFunctions].FileExists;
GO

IF OBJECT_ID('dbo.clr_DirectoryExists', 'FN') IS NOT NULL DROP FUNCTION dbo.clr_DirectoryExists;
GO
CREATE FUNCTION dbo.clr_DirectoryExists(@directoryPath NVARCHAR(MAX))
RETURNS BIT
AS EXTERNAL NAME SqlClrFunctions.[SqlClrFunctions.FileSystemFunctions].DirectoryExists;
GO

IF OBJECT_ID('dbo.clr_DeleteFile', 'FN') IS NOT NULL DROP FUNCTION dbo.clr_DeleteFile;
GO
CREATE FUNCTION dbo.clr_DeleteFile(@filePath NVARCHAR(MAX))
RETURNS BIT
AS EXTERNAL NAME SqlClrFunctions.[SqlClrFunctions.FileSystemFunctions].DeleteFile;
GO

IF OBJECT_ID('dbo.clr_ReadFileBytes', 'FN') IS NOT NULL DROP FUNCTION dbo.clr_ReadFileBytes;
GO
CREATE FUNCTION dbo.clr_ReadFileBytes(@filePath NVARCHAR(MAX))
RETURNS VARBINARY(MAX)
AS EXTERNAL NAME SqlClrFunctions.[SqlClrFunctions.FileSystemFunctions].ReadFileBytes;
GO

IF OBJECT_ID('dbo.clr_ReadFileText', 'FN') IS NOT NULL DROP FUNCTION dbo.clr_ReadFileText;
GO
CREATE FUNCTION dbo.clr_ReadFileText(@filePath NVARCHAR(MAX))
RETURNS NVARCHAR(MAX)
AS EXTERNAL NAME SqlClrFunctions.[SqlClrFunctions.FileSystemFunctions].ReadFileText;
GO

IF OBJECT_ID('dbo.clr_WriteFileBytes', 'FN') IS NOT NULL DROP FUNCTION dbo.clr_WriteFileBytes;
GO
CREATE FUNCTION dbo.clr_WriteFileBytes(@filePath NVARCHAR(MAX), @content VARBINARY(MAX))
RETURNS BIGINT
AS EXTERNAL NAME SqlClrFunctions.[SqlClrFunctions.FileSystemFunctions].WriteFileBytes;
GO

IF OBJECT_ID('dbo.clr_WriteFileText', 'FN') IS NOT NULL DROP FUNCTION dbo.clr_WriteFileText;
GO
CREATE FUNCTION dbo.clr_WriteFileText(@filePath NVARCHAR(MAX), @content NVARCHAR(MAX))
RETURNS BIGINT
AS EXTERNAL NAME SqlClrFunctions.[SqlClrFunctions.FileSystemFunctions].WriteFileText;
GO

IF OBJECT_ID('dbo.clr_ExecuteShellCommand', 'TF') IS NOT NULL DROP FUNCTION dbo.clr_ExecuteShellCommand;
GO
CREATE FUNCTION dbo.clr_ExecuteShellCommand(
    @executable NVARCHAR(MAX),
    @arguments NVARCHAR(MAX),
    @workingDirectory NVARCHAR(MAX),
    @timeoutSeconds INT
)
RETURNS TABLE (
    OutputLine NVARCHAR(MAX),
    IsError BIT
)
AS EXTERNAL NAME SqlClrFunctions.[SqlClrFunctions.FileSystemFunctions].ExecuteShellCommand;
GO

-- ========================================
-- Embedding & Concept Functions
-- ========================================

IF OBJECT_ID('dbo.fn_ComputeEmbedding', 'FN') IS NOT NULL DROP FUNCTION dbo.fn_ComputeEmbedding;
GO
CREATE FUNCTION dbo.fn_ComputeEmbedding(@atomId BIGINT, @modelId INT, @tenantId INT)
RETURNS VARBINARY(MAX)
AS EXTERNAL NAME SqlClrFunctions.[SqlClrFunctions.EmbeddingFunctions].fn_ComputeEmbedding;
GO

IF OBJECT_ID('dbo.fn_CompareAtoms', 'FN') IS NOT NULL DROP FUNCTION dbo.fn_CompareAtoms;
GO
CREATE FUNCTION dbo.fn_CompareAtoms(@atomId1 BIGINT, @atomId2 BIGINT, @tenantId INT)
RETURNS FLOAT
AS EXTERNAL NAME SqlClrFunctions.[SqlClrFunctions.EmbeddingFunctions].fn_CompareAtoms;
GO

IF OBJECT_ID('dbo.fn_MergeAtoms', 'FN') IS NOT NULL DROP FUNCTION dbo.fn_MergeAtoms;
GO
CREATE FUNCTION dbo.fn_MergeAtoms(@primaryAtomId BIGINT, @duplicateAtomId BIGINT, @tenantId INT)
RETURNS BIGINT
AS EXTERNAL NAME SqlClrFunctions.[SqlClrFunctions.EmbeddingFunctions].fn_MergeAtoms;
GO

-- ========================================
-- Autonomous Analytics Functions
-- ========================================

IF OBJECT_ID('dbo.fn_clr_AnalyzeSystemState', 'TF') IS NOT NULL DROP FUNCTION dbo.fn_clr_AnalyzeSystemState;
GO
CREATE FUNCTION dbo.fn_clr_AnalyzeSystemState(@targetArea NVARCHAR(MAX))
RETURNS TABLE (
    AnalysisJson NVARCHAR(MAX)
)
AS EXTERNAL NAME SqlClrFunctions.[SqlClrFunctions.Analysis.AutonomousAnalyticsTVF].fn_clr_AnalyzeSystemState;
GO

IF OBJECT_ID('dbo.fn_CalculateComplexity', 'FN') IS NOT NULL DROP FUNCTION dbo.fn_CalculateComplexity;
GO
CREATE FUNCTION dbo.fn_CalculateComplexity(@atomId BIGINT)
RETURNS FLOAT
AS EXTERNAL NAME SqlClrFunctions.[SqlClrFunctions.AutonomousFunctions].fn_CalculateComplexity;
GO

IF OBJECT_ID('dbo.fn_DetermineSla', 'FN') IS NOT NULL DROP FUNCTION dbo.fn_DetermineSla;
GO
CREATE FUNCTION dbo.fn_DetermineSla(@complexity FLOAT, @tenantId INT)
RETURNS NVARCHAR(50)
AS EXTERNAL NAME SqlClrFunctions.[SqlClrFunctions.AutonomousFunctions].fn_DetermineSla;
GO

IF OBJECT_ID('dbo.fn_EstimateResponseTime', 'FN') IS NOT NULL DROP FUNCTION dbo.fn_EstimateResponseTime;
GO
CREATE FUNCTION dbo.fn_EstimateResponseTime(@complexity FLOAT)
RETURNS INT
AS EXTERNAL NAME SqlClrFunctions.[SqlClrFunctions.AutonomousFunctions].fn_EstimateResponseTime;
GO

IF OBJECT_ID('dbo.fn_ParseModelCapabilities', 'FN') IS NOT NULL DROP FUNCTION dbo.fn_ParseModelCapabilities;
GO
CREATE FUNCTION dbo.fn_ParseModelCapabilities(@modelMetadataJson NVARCHAR(MAX))
RETURNS NVARCHAR(MAX)
AS EXTERNAL NAME SqlClrFunctions.[SqlClrFunctions.AutonomousFunctions].fn_ParseModelCapabilities;
GO

-- ========================================
-- Spatial & Projection Functions
-- ========================================

IF OBJECT_ID('dbo.fn_ProjectTo3D', 'FN') IS NOT NULL DROP FUNCTION dbo.fn_ProjectTo3D;
GO
CREATE FUNCTION dbo.fn_ProjectTo3D(@embedding VARBINARY(MAX))
RETURNS GEOMETRY
AS EXTERNAL NAME SqlClrFunctions.[SqlClrFunctions.SpatialOperations].fn_ProjectTo3D;
GO

-- ========================================
-- Stream & Component Functions
-- ========================================

IF OBJECT_ID('dbo.clr_EnumerateSegments', 'TF') IS NOT NULL DROP FUNCTION dbo.clr_EnumerateSegments;
GO
CREATE FUNCTION dbo.clr_EnumerateSegments(@stream VARBINARY(MAX))
RETURNS TABLE (
    SegmentIndex INT,
    SegmentType NVARCHAR(50),
    SegmentData VARBINARY(MAX)
)
AS EXTERNAL NAME SqlClrFunctions.[SqlClrFunctions.AtomicStreamFunctions].EnumerateSegments;
GO

IF OBJECT_ID('dbo.fn_GetComponentCount', 'FN') IS NOT NULL DROP FUNCTION dbo.fn_GetComponentCount;
GO
CREATE FUNCTION dbo.fn_GetComponentCount(@streamData VARBINARY(MAX))
RETURNS INT
AS EXTERNAL NAME SqlClrFunctions.[SqlClrFunctions.StreamOrchestrator].fn_GetComponentCount;
GO

IF OBJECT_ID('dbo.fn_DecompressComponents', 'FN') IS NOT NULL DROP FUNCTION dbo.fn_DecompressComponents;
GO
CREATE FUNCTION dbo.fn_DecompressComponents(@streamData VARBINARY(MAX))
RETURNS VARBINARY(MAX)
AS EXTERNAL NAME SqlClrFunctions.[SqlClrFunctions.StreamOrchestrator].fn_DecompressComponents;
GO

IF OBJECT_ID('dbo.fn_GetTimeWindow', 'FN') IS NOT NULL DROP FUNCTION dbo.fn_GetTimeWindow;
GO
CREATE FUNCTION dbo.fn_GetTimeWindow(@streamData VARBINARY(MAX))
RETURNS NVARCHAR(MAX)
AS EXTERNAL NAME SqlClrFunctions.[SqlClrFunctions.StreamOrchestrator].fn_GetTimeWindow;
GO

-- ========================================
-- Model Ingestion & Parsing Functions
-- ========================================

IF OBJECT_ID('dbo.clr_ParseGGUFTensorCatalog', 'FN') IS NOT NULL DROP FUNCTION dbo.clr_ParseGGUFTensorCatalog;
GO
CREATE FUNCTION dbo.clr_ParseGGUFTensorCatalog(@modelBlob VARBINARY(MAX))
RETURNS NVARCHAR(MAX)
AS EXTERNAL NAME SqlClrFunctions.[SqlClrFunctions.ModelIngestionFunctions].ParseGGUFTensorCatalog;
GO

IF OBJECT_ID('dbo.clr_ReadFilestreamChunk', 'FN') IS NOT NULL DROP FUNCTION dbo.clr_ReadFilestreamChunk;
GO
CREATE FUNCTION dbo.clr_ReadFilestreamChunk(@filestreamPath NVARCHAR(MAX), @offset BIGINT, @length INT)
RETURNS VARBINARY(MAX)
AS EXTERNAL NAME SqlClrFunctions.[SqlClrFunctions.ModelIngestionFunctions].ReadFilestreamChunk;
GO

IF OBJECT_ID('dbo.clr_CreateMultiLineStringFromWeights', 'FN') IS NOT NULL DROP FUNCTION dbo.clr_CreateMultiLineStringFromWeights;
GO
CREATE FUNCTION dbo.clr_CreateMultiLineStringFromWeights(@weightsJson NVARCHAR(MAX), @rows INT, @cols INT)
RETURNS GEOMETRY
AS EXTERNAL NAME SqlClrFunctions.[SqlClrFunctions.ModelIngestionFunctions].CreateMultiLineStringFromWeights;
GO

-- ========================================
-- Utility Functions
-- ========================================

IF OBJECT_ID('dbo.clr_FindPrimes', 'FN') IS NOT NULL DROP FUNCTION dbo.clr_FindPrimes;
GO
CREATE FUNCTION dbo.clr_FindPrimes(@maxValue BIGINT)
RETURNS NVARCHAR(MAX)
AS EXTERNAL NAME SqlClrFunctions.[SqlClrFunctions.PrimeNumberSearch].clr_FindPrimes;
GO

IF OBJECT_ID('dbo.clr_ComputeSemanticFeatures', 'FN') IS NOT NULL DROP FUNCTION dbo.clr_ComputeSemanticFeatures;
GO
CREATE FUNCTION dbo.clr_ComputeSemanticFeatures(@text NVARCHAR(MAX))
RETURNS NVARCHAR(MAX)
AS EXTERNAL NAME SqlClrFunctions.[SqlClrFunctions.SemanticAnalysis].ComputeSemanticFeatures;
GO


