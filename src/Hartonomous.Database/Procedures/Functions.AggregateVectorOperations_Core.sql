-- =============================================
-- CORE CLR AGGREGATES (CRITICAL PATH)
-- =============================================
-- These aggregates are actively used by OODA loop procedures
-- Must be deployed BEFORE other procedures that depend on them
--
-- Prerequisites:
-- 1. SqlClrFunctions assembly deployed with UNSAFE permission set
-- 2. CLR integration enabled: EXEC sp_configure 'clr enabled', 1; RECONFIGURE;
-- =============================================

-- =============================================
-- REASONING FRAMEWORK AGGREGATES (CRITICAL)
-- =============================================

-- Drop existing aggregates if they exist (for redeployment)
IF EXISTS (SELECT * FROM sys.objects WHERE type = 'AF' AND name = 'TreeOfThought')
    DROP AGGREGATE dbo.TreeOfThought;
IF EXISTS (SELECT * FROM sys.objects WHERE type = 'AF' AND name = 'ReflexionAggregate')
    DROP AGGREGATE dbo.ReflexionAggregate;
IF EXISTS (SELECT * FROM sys.objects WHERE type = 'AF' AND name = 'SelfConsistency')
    DROP AGGREGATE dbo.SelfConsistency;
IF EXISTS (SELECT * FROM sys.objects WHERE type = 'AF' AND name = 'ChainOfThoughtCoherence')
    DROP AGGREGATE dbo.ChainOfThoughtCoherence;

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
-- ANOMALY DETECTION AGGREGATES (CRITICAL)
-- =============================================

IF EXISTS (SELECT * FROM sys.objects WHERE type = 'AF' AND name = 'IsolationForestScore')
    DROP AGGREGATE dbo.IsolationForestScore;
IF EXISTS (SELECT * FROM sys.objects WHERE type = 'AF' AND name = 'LocalOutlierFactor')
    DROP AGGREGATE dbo.LocalOutlierFactor;
IF EXISTS (SELECT * FROM sys.objects WHERE type = 'AF' AND name = 'DBSCANCluster')
    DROP AGGREGATE dbo.DBSCANCluster;
IF EXISTS (SELECT * FROM sys.objects WHERE type = 'AF' AND name = 'MahalanobisDistance')
    DROP AGGREGATE dbo.MahalanobisDistance;

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
-- VERIFICATION
-- =============================================

SELECT
    OBJECT_NAME(object_id) AS AggregateName,
    SCHEMA_NAME(schema_id) AS SchemaName,
    create_date AS CreatedDate
FROM sys.objects
WHERE type = 'AF'
  AND OBJECT_NAME(object_id) IN (
    'TreeOfThought',
    'ReflexionAggregate',
    'SelfConsistency',
    'ChainOfThoughtCoherence',
    'IsolationForestScore',
    'LocalOutlierFactor',
    'DBSCANCluster',
    'MahalanobisDistance'
  )
ORDER BY name;

SELECT COUNT(*) AS CoreAggregatesCreated
FROM sys.objects
WHERE type = 'AF'
  AND OBJECT_NAME(object_id) IN (
    'TreeOfThought',
    'ReflexionAggregate',
    'SelfConsistency',
    'ChainOfThoughtCoherence',
    'IsolationForestScore',
    'LocalOutlierFactor',
    'DBSCANCluster',
    'MahalanobisDistance'
  );
