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


