-- Ensures SQL CLR bindings are present before referencing helper functions from stored procedures.

CREATE FUNCTION dbo.clr_VectorDotProduct(@v1 VARBINARY(MAX), @v2 VARBINARY(MAX))
RETURNS FLOAT
AS EXTERNAL NAME SqlClrFunctions.[SqlClrFunctions.VectorOperations].VectorDotProduct;

CREATE FUNCTION dbo.clr_VectorCosineSimilarity(@v1 VARBINARY(MAX), @v2 VARBINARY(MAX))
RETURNS FLOAT
AS EXTERNAL NAME SqlClrFunctions.[SqlClrFunctions.VectorOperations].VectorCosineSimilarity;

CREATE FUNCTION dbo.clr_VectorEuclideanDistance(@v1 VARBINARY(MAX), @v2 VARBINARY(MAX))
RETURNS FLOAT
AS EXTERNAL NAME SqlClrFunctions.[SqlClrFunctions.VectorOperations].VectorEuclideanDistance;

CREATE FUNCTION dbo.clr_VectorNormalize(@vector VARBINARY(MAX))
RETURNS VARBINARY(MAX)
AS EXTERNAL NAME SqlClrFunctions.[SqlClrFunctions.VectorOperations].VectorNormalize;

CREATE FUNCTION dbo.clr_VectorSoftmax(@vector VARBINARY(MAX))
RETURNS VARBINARY(MAX)
AS EXTERNAL NAME SqlClrFunctions.[SqlClrFunctions.VectorOperations].VectorSoftmax;

CREATE FUNCTION dbo.clr_VectorArgMax(@vector VARBINARY(MAX))
RETURNS INT
AS EXTERNAL NAME SqlClrFunctions.[SqlClrFunctions.VectorOperations].VectorArgMax;

CREATE FUNCTION dbo.clr_VectorAdd(@v1 VARBINARY(MAX), @v2 VARBINARY(MAX))
RETURNS VARBINARY(MAX)
AS EXTERNAL NAME SqlClrFunctions.[SqlClrFunctions.VectorOperations].VectorAdd;

CREATE FUNCTION dbo.clr_SemanticFeaturesJson(@text NVARCHAR(MAX))
RETURNS NVARCHAR(MAX)
AS EXTERNAL NAME SqlClrFunctions.[SqlClrFunctions.SemanticAnalysis].ComputeSemanticFeatures;

CREATE FUNCTION dbo.clr_VectorSubtract(@v1 VARBINARY(MAX), @v2 VARBINARY(MAX))
RETURNS VARBINARY(MAX)
AS EXTERNAL NAME SqlClrFunctions.[SqlClrFunctions.VectorOperations].VectorSubtract;

CREATE FUNCTION dbo.clr_VectorScale(@vector VARBINARY(MAX), @scalar FLOAT)
RETURNS VARBINARY(MAX)
AS EXTERNAL NAME SqlClrFunctions.[SqlClrFunctions.VectorOperations].VectorScale;

CREATE FUNCTION dbo.clr_VectorLerp(@v1 VARBINARY(MAX), @v2 VARBINARY(MAX), @t FLOAT)
RETURNS VARBINARY(MAX)
AS EXTERNAL NAME SqlClrFunctions.[SqlClrFunctions.VectorOperations].VectorLerp;

CREATE FUNCTION dbo.clr_VectorNorm(@vector VARBINARY(MAX))
RETURNS FLOAT
AS EXTERNAL NAME SqlClrFunctions.[SqlClrFunctions.VectorOperations].VectorNorm;

CREATE FUNCTION dbo.clr_ImageToPointCloud(@image VARBINARY(MAX), @width INT, @height INT, @sampleStep INT)
RETURNS GEOMETRY
AS EXTERNAL NAME SqlClrFunctions.[SqlClrFunctions.ImageProcessing].ImageToPointCloud;

CREATE FUNCTION dbo.clr_ImageAverageColor(@image VARBINARY(MAX), @width INT, @height INT)
RETURNS NVARCHAR(MAX)
AS EXTERNAL NAME SqlClrFunctions.[SqlClrFunctions.ImageProcessing].ImageAverageColor;

CREATE FUNCTION dbo.clr_ImageLuminanceHistogram(@image VARBINARY(MAX), @width INT, @height INT, @binCount INT)
RETURNS NVARCHAR(MAX)
AS EXTERNAL NAME SqlClrFunctions.[SqlClrFunctions.ImageProcessing].ImageLuminanceHistogram;

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

CREATE FUNCTION dbo.clr_AudioToWaveform(@audio VARBINARY(MAX), @channels INT, @sampleRate INT, @maxPoints INT)
RETURNS GEOMETRY
AS EXTERNAL NAME SqlClrFunctions.[SqlClrFunctions.AudioProcessing].AudioToWaveform;

CREATE FUNCTION dbo.clr_AudioComputeRms(@audio VARBINARY(MAX), @channels INT)
RETURNS FLOAT
AS EXTERNAL NAME SqlClrFunctions.[SqlClrFunctions.AudioProcessing].AudioComputeRms;

CREATE FUNCTION dbo.clr_AudioComputePeak(@audio VARBINARY(MAX), @channels INT)
RETURNS FLOAT
AS EXTERNAL NAME SqlClrFunctions.[SqlClrFunctions.AudioProcessing].AudioComputePeak;

CREATE FUNCTION dbo.clr_AudioDownsample(@audio VARBINARY(MAX), @channels INT, @factor INT)
RETURNS VARBINARY(MAX)
AS EXTERNAL NAME SqlClrFunctions.[SqlClrFunctions.AudioProcessing].AudioDownsample;

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

CREATE FUNCTION dbo.clr_SemanticFeaturesJson(@input NVARCHAR(MAX))
RETURNS NVARCHAR(MAX)
AS EXTERNAL NAME SqlClrFunctions.[SqlClrFunctions.SemanticAnalysis].ComputeSemanticFeatures;

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

-- ========================================
-- Code Analysis Functions (AST-as-GEOMETRY)
-- ========================================

CREATE FUNCTION dbo.clr_GenerateCodeAstVector(@sourceCode NVARCHAR(MAX))
RETURNS NVARCHAR(MAX)
AS EXTERNAL NAME SqlClrFunctions.[SqlClrFunctions.CodeAnalysis].clr_GenerateCodeAstVector;

-- ========================================
-- SVD-as-GEOMETRY Pipeline Functions
-- ========================================

CREATE FUNCTION dbo.clr_ParseModelLayer(
    @modelBlob VARBINARY(MAX),
    @tensorName NVARCHAR(256),
    @modelFormatHint NVARCHAR(50)
)
RETURNS NVARCHAR(MAX)
AS EXTERNAL NAME SqlClrFunctions.[SqlClrFunctions.ModelParsing].clr_ParseModelLayer;

CREATE FUNCTION dbo.clr_SvdDecompose(
    @weightArrayJson NVARCHAR(MAX),
    @rows INT,
    @cols INT,
    @maxRank INT
)
RETURNS NVARCHAR(MAX)
AS EXTERNAL NAME SqlClrFunctions.[SqlClrFunctions.SVDGeometryFunctions].clr_SvdDecompose;

CREATE FUNCTION dbo.clr_ProjectToPoint(@vectorJson NVARCHAR(MAX))
RETURNS NVARCHAR(MAX)
AS EXTERNAL NAME SqlClrFunctions.[SqlClrFunctions.SVDGeometryFunctions].clr_ProjectToPoint;

CREATE FUNCTION dbo.clr_CreateGeometryPointWithImportance(
    @x FLOAT,
    @y FLOAT,
    @z FLOAT,
    @importance FLOAT
)
RETURNS NVARCHAR(MAX)
AS EXTERNAL NAME SqlClrFunctions.[SqlClrFunctions.SVDGeometryFunctions].clr_CreateGeometryPointWithImportance;

CREATE FUNCTION dbo.clr_ReconstructFromSVD(
    @UJson NVARCHAR(MAX),
    @SJson NVARCHAR(MAX),
    @VTJson NVARCHAR(MAX)
)
RETURNS NVARCHAR(MAX)
AS EXTERNAL NAME SqlClrFunctions.[SqlClrFunctions.SVDGeometryFunctions].clr_ReconstructFromSVD;

-- ========================================
-- Tensor Data I/O Functions
-- ========================================

CREATE PROCEDURE dbo.clr_StoreTensorAtomPayload
    @tensorAtomId BIGINT,
    @payload VARBINARY(MAX)
AS EXTERNAL NAME SqlClrFunctions.[SqlClrFunctions.TensorDataIO].clr_StoreTensorAtomPayload;

CREATE FUNCTION dbo.clr_JsonFloatArrayToBytes(@jsonFloatArray NVARCHAR(MAX))
RETURNS VARBINARY(MAX)
AS EXTERNAL NAME SqlClrFunctions.[SqlClrFunctions.TensorDataIO].clr_JsonFloatArrayToBytes;

CREATE FUNCTION dbo.clr_GetTensorAtomPayload(@tensorAtomId BIGINT)
RETURNS VARBINARY(MAX)
AS EXTERNAL NAME SqlClrFunctions.[SqlClrFunctions.TensorDataIO].clr_GetTensorAtomPayload;

-- ========================================
-- Trajectory & Path Analysis Aggregates
-- ========================================

CREATE AGGREGATE dbo.agg_BuildPathFromAtoms(
    @atomId BIGINT,
    @timestamp DATETIME
)
RETURNS GEOMETRY
EXTERNAL NAME SqlClrFunctions.[SqlClrFunctions.BuildPathFromAtoms];

-- ========================================
-- Shape-to-Content Generation Functions (Phase 4)
-- ========================================

CREATE FUNCTION dbo.clr_GenerateImageFromShapes(
    @shapes GEOMETRY,
    @width INT,
    @height INT
)
RETURNS VARBINARY(MAX)
AS EXTERNAL NAME SqlClrFunctions.[SqlClrFunctions.ImageGeneration].GenerateImageFromShapes;

CREATE FUNCTION dbo.clr_GenerateAudioFromSpatialSignature(@spatialSignature GEOMETRY)
RETURNS VARBINARY(MAX)
AS EXTERNAL NAME SqlClrFunctions.[SqlClrFunctions.AudioProcessing].GenerateAudioFromSpatialSignature;

CREATE FUNCTION dbo.clr_SynthesizeModelLayer(
    @queryShape GEOMETRY,
    @parentLayerId BIGINT
)
RETURNS NVARCHAR(MAX)
AS EXTERNAL NAME SqlClrFunctions.[SqlClrFunctions.TensorOperations.ModelSynthesis].clr_SynthesizeModelLayer;
