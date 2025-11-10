-- =============================================
-- CLR User-Defined Aggregates for Vector Operations
-- =============================================
-- Microsoft SQL Server CLR aggregates for ML/AI workloads
--
-- IMPORTANT: These MUST use CREATE AGGREGATE syntax, not CREATE FUNCTION
-- Reference: https://learn.microsoft.com/en-us/sql/t-sql/statements/create-aggregate-transact-sql
--
-- Prerequisites:
-- 1. SqlClrFunctions assembly deployed with UNSAFE permission set
-- 2. CLR integration enabled: EXEC sp_configure 'clr enabled', 1; RECONFIGURE;
-- =============================================

-- Drop existing aggregates if they exist (for redeployment)
IF EXISTS (SELECT * FROM sys.objects WHERE type = 'AF' AND name = 'VectorMeanVariance')
    DROP AGGREGATE dbo.VectorMeanVariance;
IF EXISTS (SELECT * FROM sys.objects WHERE type = 'AF' AND name = 'GeometricMedian')
    DROP AGGREGATE dbo.GeometricMedian;
IF EXISTS (SELECT * FROM sys.objects WHERE type = 'AF' AND name = 'StreamingSoftmax')
    DROP AGGREGATE dbo.StreamingSoftmax;
IF EXISTS (SELECT * FROM sys.objects WHERE type = 'AF' AND name = 'VectorAttentionAggregate')
    DROP AGGREGATE dbo.VectorAttentionAggregate;
IF EXISTS (SELECT * FROM sys.objects WHERE type = 'AF' AND name = 'AutoencoderCompression')
    DROP AGGREGATE dbo.AutoencoderCompression;
IF EXISTS (SELECT * FROM sys.objects WHERE type = 'AF' AND name = 'GradientStatistics')
    DROP AGGREGATE dbo.GradientStatistics;
IF EXISTS (SELECT * FROM sys.objects WHERE type = 'AF' AND name = 'CosineAnnealingSchedule')
    DROP AGGREGATE dbo.CosineAnnealingSchedule;
IF EXISTS (SELECT * FROM sys.objects WHERE type = 'AF' AND name = 'TreeOfThought')
    DROP AGGREGATE dbo.TreeOfThought;
IF EXISTS (SELECT * FROM sys.objects WHERE type = 'AF' AND name = 'ReflexionAggregate')
    DROP AGGREGATE dbo.ReflexionAggregate;
IF EXISTS (SELECT * FROM sys.objects WHERE type = 'AF' AND name = 'SelfConsistency')
    DROP AGGREGATE dbo.SelfConsistency;
IF EXISTS (SELECT * FROM sys.objects WHERE type = 'AF' AND name = 'ChainOfThoughtCoherence')
    DROP AGGREGATE dbo.ChainOfThoughtCoherence;
IF EXISTS (SELECT * FROM sys.objects WHERE type = 'AF' AND name = 'GraphPathVectorSummary')
    DROP AGGREGATE dbo.GraphPathVectorSummary;
IF EXISTS (SELECT * FROM sys.objects WHERE type = 'AF' AND name = 'EdgeWeightedByVectorSimilarity')
    DROP AGGREGATE dbo.EdgeWeightedByVectorSimilarity;
IF EXISTS (SELECT * FROM sys.objects WHERE type = 'AF' AND name = 'VectorDriftOverTime')
    DROP AGGREGATE dbo.VectorDriftOverTime;
IF EXISTS (SELECT * FROM sys.objects WHERE type = 'AF' AND name = 'VectorSequencePatterns')
    DROP AGGREGATE dbo.VectorSequencePatterns;
IF EXISTS (SELECT * FROM sys.objects WHERE type = 'AF' AND name = 'VectorARForecast')
    DROP AGGREGATE dbo.VectorARForecast;
IF EXISTS (SELECT * FROM sys.objects WHERE type = 'AF' AND name = 'DTWDistance')
    DROP AGGREGATE dbo.DTWDistance;
IF EXISTS (SELECT * FROM sys.objects WHERE type = 'AF' AND name = 'ChangePointDetection')
    DROP AGGREGATE dbo.ChangePointDetection;
IF EXISTS (SELECT * FROM sys.objects WHERE type = 'AF' AND name = 'IsolationForestScore')
    DROP AGGREGATE dbo.IsolationForestScore;
IF EXISTS (SELECT * FROM sys.objects WHERE type = 'AF' AND name = 'LocalOutlierFactor')
    DROP AGGREGATE dbo.LocalOutlierFactor;
IF EXISTS (SELECT * FROM sys.objects WHERE type = 'AF' AND name = 'DBSCANCluster')
    DROP AGGREGATE dbo.DBSCANCluster;
IF EXISTS (SELECT * FROM sys.objects WHERE type = 'AF' AND name = 'MahalanobisDistance')
    DROP AGGREGATE dbo.MahalanobisDistance;
IF EXISTS (SELECT * FROM sys.objects WHERE type = 'AF' AND name = 'CollaborativeFilter')
    DROP AGGREGATE dbo.CollaborativeFilter;
IF EXISTS (SELECT * FROM sys.objects WHERE type = 'AF' AND name = 'ContentBasedFilter')
    DROP AGGREGATE dbo.ContentBasedFilter;
IF EXISTS (SELECT * FROM sys.objects WHERE type = 'AF' AND name = 'MatrixFactorization')
    DROP AGGREGATE dbo.MatrixFactorization;
IF EXISTS (SELECT * FROM sys.objects WHERE type = 'AF' AND name = 'DiversityRecommendation')
    DROP AGGREGATE dbo.DiversityRecommendation;
IF EXISTS (SELECT * FROM sys.objects WHERE type = 'AF' AND name = 'PrincipalComponentAnalysis')
    DROP AGGREGATE dbo.PrincipalComponentAnalysis;
IF EXISTS (SELECT * FROM sys.objects WHERE type = 'AF' AND name = 'TSNEProjection')
    DROP AGGREGATE dbo.TSNEProjection;
IF EXISTS (SELECT * FROM sys.objects WHERE type = 'AF' AND name = 'RandomProjection')
    DROP AGGREGATE dbo.RandomProjection;
IF EXISTS (SELECT * FROM sys.objects WHERE type = 'AF' AND name = 'ResearchWorkflow')
    DROP AGGREGATE dbo.ResearchWorkflow;
IF EXISTS (SELECT * FROM sys.objects WHERE type = 'AF' AND name = 'ToolExecutionChain')
    DROP AGGREGATE dbo.ToolExecutionChain;
IF EXISTS (SELECT * FROM sys.objects WHERE type = 'AF' AND name = 'UserJourney')
    DROP AGGREGATE dbo.UserJourney;
IF EXISTS (SELECT * FROM sys.objects WHERE type = 'AF' AND name = 'ABTestAnalysis')
    DROP AGGREGATE dbo.ABTestAnalysis;
IF EXISTS (SELECT * FROM sys.objects WHERE type = 'AF' AND name = 'ChurnPrediction')
    DROP AGGREGATE dbo.ChurnPrediction;
IF EXISTS (SELECT * FROM sys.objects WHERE type = 'AF' AND name = 'VectorCentroid')
    DROP AGGREGATE dbo.VectorCentroid;
IF EXISTS (SELECT * FROM sys.objects WHERE type = 'AF' AND name = 'VectorCovariance')
    DROP AGGREGATE dbo.VectorCovariance;
IF EXISTS (SELECT * FROM sys.objects WHERE type = 'AF' AND name = 'VectorKMeansCluster')
    DROP AGGREGATE dbo.VectorKMeansCluster;
IF EXISTS (SELECT * FROM sys.objects WHERE type = 'AF' AND name = 'SpatialConvexHull')
    DROP AGGREGATE dbo.SpatialConvexHull;
IF EXISTS (SELECT * FROM sys.objects WHERE type = 'AF' AND name = 'SpatialDensityGrid')
    DROP AGGREGATE dbo.SpatialDensityGrid;

-- =============================================
-- VECTOR AGGREGATES (Basic Statistics)
-- =============================================

CREATE AGGREGATE dbo.VectorMeanVariance(@vector NVARCHAR(MAX))
RETURNS NVARCHAR(MAX)
EXTERNAL NAME [SqlClrFunctions].[SqlClrFunctions.VectorMeanVariance];

CREATE AGGREGATE dbo.GeometricMedian(@vector NVARCHAR(MAX))
RETURNS NVARCHAR(MAX)
EXTERNAL NAME [SqlClrFunctions].[SqlClrFunctions.GeometricMedian];

CREATE AGGREGATE dbo.StreamingSoftmax(@vector NVARCHAR(MAX))
RETURNS NVARCHAR(MAX)
EXTERNAL NAME [SqlClrFunctions].[SqlClrFunctions.StreamingSoftmax];

CREATE AGGREGATE dbo.VectorCentroid(@vector NVARCHAR(MAX))
RETURNS NVARCHAR(MAX)
EXTERNAL NAME [SqlClrFunctions].[SqlClrFunctions.VectorCentroid];

CREATE AGGREGATE dbo.VectorCovariance(@vector NVARCHAR(MAX))
RETURNS NVARCHAR(MAX)
EXTERNAL NAME [SqlClrFunctions].[SqlClrFunctions.VectorCovariance];

CREATE AGGREGATE dbo.VectorKMeansCluster(@vector NVARCHAR(MAX), @k INT)
RETURNS NVARCHAR(MAX)
EXTERNAL NAME [SqlClrFunctions].[SqlClrFunctions.VectorKMeansCluster];

-- =============================================
-- NEURAL NETWORK AGGREGATES
-- =============================================

CREATE AGGREGATE dbo.VectorAttentionAggregate(
    @query NVARCHAR(MAX),
    @key NVARCHAR(MAX),
    @value NVARCHAR(MAX),
    @numHeads INT)
RETURNS NVARCHAR(MAX)
EXTERNAL NAME [SqlClrFunctions].[SqlClrFunctions.VectorAttentionAggregate];

CREATE AGGREGATE dbo.AutoencoderCompression(@input NVARCHAR(MAX), @latentDim INT)
RETURNS NVARCHAR(MAX)
EXTERNAL NAME [SqlClrFunctions].[SqlClrFunctions.AutoencoderCompression];

CREATE AGGREGATE dbo.GradientStatistics(@gradient NVARCHAR(MAX))
RETURNS NVARCHAR(MAX)
EXTERNAL NAME [SqlClrFunctions].[SqlClrFunctions.GradientStatistics];

CREATE AGGREGATE dbo.CosineAnnealingSchedule(@iteration INT, @maxIterations INT)
RETURNS FLOAT
EXTERNAL NAME [SqlClrFunctions].[SqlClrFunctions.CosineAnnealingSchedule];

-- =============================================
-- REASONING FRAMEWORK AGGREGATES
-- =============================================

CREATE AGGREGATE dbo.TreeOfThought(@hypothesis NVARCHAR(MAX), @score FLOAT)
RETURNS NVARCHAR(MAX)
EXTERNAL NAME [SqlClrFunctions].[SqlClrFunctions.TreeOfThought];

CREATE AGGREGATE dbo.ReflexionAggregate(@trajectory NVARCHAR(MAX), @feedback NVARCHAR(MAX))
RETURNS NVARCHAR(MAX)
EXTERNAL NAME [SqlClrFunctions].[SqlClrFunctions.ReflexionAggregate];

CREATE AGGREGATE dbo.SelfConsistency(@reasoning NVARCHAR(MAX))
RETURNS NVARCHAR(MAX)
EXTERNAL NAME [SqlClrFunctions].[SqlClrFunctions.SelfConsistency];

CREATE AGGREGATE dbo.ChainOfThoughtCoherence(@step NVARCHAR(MAX), @stepNumber INT)
RETURNS FLOAT
EXTERNAL NAME [SqlClrFunctions].[SqlClrFunctions.ChainOfThoughtCoherence];

-- =============================================
-- GRAPH VECTOR AGGREGATES
-- =============================================

CREATE AGGREGATE dbo.GraphPathVectorSummary(@nodeVector NVARCHAR(MAX), @pathDepth INT)
RETURNS NVARCHAR(MAX)
EXTERNAL NAME [SqlClrFunctions].[SqlClrFunctions.GraphPathVectorSummary];

CREATE AGGREGATE dbo.EdgeWeightedByVectorSimilarity(@sourceVector NVARCHAR(MAX), @targetVector NVARCHAR(MAX))
RETURNS FLOAT
EXTERNAL NAME [SqlClrFunctions].[SqlClrFunctions.EdgeWeightedByVectorSimilarity];

CREATE AGGREGATE dbo.VectorDriftOverTime(@vector NVARCHAR(MAX), @timestamp DATETIME2)
RETURNS FLOAT
EXTERNAL NAME [SqlClrFunctions].[SqlClrFunctions.VectorDriftOverTime];

-- =============================================
-- TIME SERIES VECTOR AGGREGATES
-- =============================================

CREATE AGGREGATE dbo.VectorSequencePatterns(@vector NVARCHAR(MAX), @sequenceIndex INT)
RETURNS NVARCHAR(MAX)
EXTERNAL NAME [SqlClrFunctions].[SqlClrFunctions.VectorSequencePatterns];

CREATE AGGREGATE dbo.VectorARForecast(@vector NVARCHAR(MAX), @lag INT)
RETURNS NVARCHAR(MAX)
EXTERNAL NAME [SqlClrFunctions].[SqlClrFunctions.VectorARForecast];

CREATE AGGREGATE dbo.DTWDistance(@series1 NVARCHAR(MAX), @series2 NVARCHAR(MAX))
RETURNS FLOAT
EXTERNAL NAME [SqlClrFunctions].[SqlClrFunctions.DTWDistance];

CREATE AGGREGATE dbo.ChangePointDetection(@value FLOAT, @timestamp DATETIME2)
RETURNS NVARCHAR(MAX)
EXTERNAL NAME [SqlClrFunctions].[SqlClrFunctions.ChangePointDetection];

-- =============================================
-- ANOMALY DETECTION AGGREGATES
-- =============================================

CREATE AGGREGATE dbo.IsolationForestScore(@vector NVARCHAR(MAX), @numTrees INT)
RETURNS FLOAT
EXTERNAL NAME [SqlClrFunctions].[SqlClrFunctions.IsolationForestScore];

CREATE AGGREGATE dbo.LocalOutlierFactor(@vector NVARCHAR(MAX), @k INT)
RETURNS FLOAT
EXTERNAL NAME [SqlClrFunctions].[SqlClrFunctions.LocalOutlierFactor];

CREATE AGGREGATE dbo.DBSCANCluster(@vector NVARCHAR(MAX), @eps FLOAT, @minPts INT)
RETURNS INT
EXTERNAL NAME [SqlClrFunctions].[SqlClrFunctions.DBSCANCluster];

CREATE AGGREGATE dbo.MahalanobisDistance(@vector NVARCHAR(MAX))
RETURNS FLOAT
EXTERNAL NAME [SqlClrFunctions].[SqlClrFunctions.MahalanobisDistance];

-- =============================================
-- RECOMMENDER SYSTEM AGGREGATES
-- =============================================

CREATE AGGREGATE dbo.CollaborativeFilter(@userId INT, @itemVector NVARCHAR(MAX), @rating FLOAT)
RETURNS NVARCHAR(MAX)
EXTERNAL NAME [SqlClrFunctions].[SqlClrFunctions.CollaborativeFilter];

CREATE AGGREGATE dbo.ContentBasedFilter(@itemFeatures NVARCHAR(MAX), @userPreferences NVARCHAR(MAX))
RETURNS FLOAT
EXTERNAL NAME [SqlClrFunctions].[SqlClrFunctions.ContentBasedFilter];

CREATE AGGREGATE dbo.MatrixFactorization(@userVector NVARCHAR(MAX), @itemVector NVARCHAR(MAX), @latentFactors INT)
RETURNS NVARCHAR(MAX)
EXTERNAL NAME [SqlClrFunctions].[SqlClrFunctions.MatrixFactorization];

CREATE AGGREGATE dbo.DiversityRecommendation(@itemVector NVARCHAR(MAX), @diversityWeight FLOAT)
RETURNS NVARCHAR(MAX)
EXTERNAL NAME [SqlClrFunctions].[SqlClrFunctions.DiversityRecommendation];

-- =============================================
-- DIMENSIONALITY REDUCTION AGGREGATES
-- =============================================

CREATE AGGREGATE dbo.PrincipalComponentAnalysis(@vector NVARCHAR(MAX), @numComponents INT)
RETURNS NVARCHAR(MAX)
EXTERNAL NAME [SqlClrFunctions].[SqlClrFunctions.PrincipalComponentAnalysis];

CREATE AGGREGATE dbo.TSNEProjection(@vector NVARCHAR(MAX), @perplexity FLOAT)
RETURNS NVARCHAR(MAX)
EXTERNAL NAME [SqlClrFunctions].[SqlClrFunctions.TSNEProjection];

CREATE AGGREGATE dbo.RandomProjection(@vector NVARCHAR(MAX), @targetDim INT)
RETURNS NVARCHAR(MAX)
EXTERNAL NAME [SqlClrFunctions].[SqlClrFunctions.RandomProjection];

-- =============================================
-- RESEARCH TOOL AGGREGATES
-- =============================================

CREATE AGGREGATE dbo.ResearchWorkflow(@actionTaken NVARCHAR(MAX), @resultQuality FLOAT)
RETURNS NVARCHAR(MAX)
EXTERNAL NAME [SqlClrFunctions].[SqlClrFunctions.ResearchWorkflow];

CREATE AGGREGATE dbo.ToolExecutionChain(@toolName NVARCHAR(MAX), @executionTime INT, @success BIT)
RETURNS NVARCHAR(MAX)
EXTERNAL NAME [SqlClrFunctions].[SqlClrFunctions.ToolExecutionChain];

-- =============================================
-- BEHAVIORAL ANALYTICS AGGREGATES
-- =============================================

CREATE AGGREGATE dbo.UserJourney(@eventType NVARCHAR(MAX), @timestamp DATETIME2, @sessionId UNIQUEIDENTIFIER)
RETURNS NVARCHAR(MAX)
EXTERNAL NAME [SqlClrFunctions].[SqlClrFunctions.UserJourney];

CREATE AGGREGATE dbo.ABTestAnalysis(@variant NVARCHAR(MAX), @metric FLOAT)
RETURNS NVARCHAR(MAX)
EXTERNAL NAME [SqlClrFunctions].[SqlClrFunctions.ABTestAnalysis];

CREATE AGGREGATE dbo.ChurnPrediction(@featureVector NVARCHAR(MAX), @daysSinceLastActivity INT)
RETURNS FLOAT
EXTERNAL NAME [SqlClrFunctions].[SqlClrFunctions.ChurnPrediction];

-- =============================================
-- SPATIAL AGGREGATES
-- =============================================

CREATE AGGREGATE dbo.SpatialConvexHull(@point GEOMETRY)
RETURNS GEOMETRY
EXTERNAL NAME [SqlClrFunctions].[SqlClrFunctions.SpatialConvexHull];

CREATE AGGREGATE dbo.SpatialDensityGrid(@point GEOMETRY, @gridSize FLOAT)
RETURNS NVARCHAR(MAX)
EXTERNAL NAME [SqlClrFunctions].[SqlClrFunctions.SpatialDensityGrid];

-- =============================================
-- VERIFICATION
-- =============================================

-- Verify all aggregates were created
SELECT
    OBJECT_NAME(object_id) AS AggregateName,
    SCHEMA_NAME(schema_id) AS SchemaName,
    create_date AS CreatedDate
FROM sys.objects
WHERE type = 'AF'
ORDER BY name;

-- Expected count: 39 aggregates
SELECT COUNT(*) AS TotalAggregates
FROM sys.objects
WHERE type = 'AF';
