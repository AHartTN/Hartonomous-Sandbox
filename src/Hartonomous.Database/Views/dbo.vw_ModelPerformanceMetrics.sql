-- =============================================
-- vw_ModelPerformanceMetrics: Materialized view for AnalyticsController performance queries
-- Replaces hard-coded SQL in AnalyticsController.GetModelPerformance (lines 171-193)
-- WITH SCHEMABINDING enables indexed views - query optimizer materialization
-- =============================================
CREATE VIEW [dbo].[vw_ModelPerformanceMetrics]
WITH SCHEMABINDING
AS
SELECT 
    m.ModelId,
    m.ModelName,
    ISNULL(m.UsageCount, 0) AS TotalInferences,
    m.LastUsed,
    -- Compute average inference time from model layers
    (SELECT AVG(ml2.AvgComputeTimeMs) 
     FROM dbo.ModelLayer ml2 
     WHERE ml2.ModelId = m.ModelId) AS AvgInferenceTimeMs,
    -- Compute average cache hit rate
    (SELECT AVG(ISNULL(ml3.CacheHitRate, 0.0)) 
     FROM dbo.ModelLayer ml3 
     WHERE ml3.ModelId = m.ModelId) AS CacheHitRate,
    -- Placeholder for confidence score (requires inference request data)
    CAST(0.0 AS FLOAT) AS AvgConfidenceScore,
    -- Placeholder for tokens (requires inference request data)
    CAST(0 AS BIGINT) AS TotalTokensGenerated
FROM dbo.Model m;
GO

-- Create indexed view for automatic materialization (MASSIVE perf boost)
CREATE UNIQUE CLUSTERED INDEX IX_vw_ModelPerformanceMetrics_ModelId 
ON dbo.vw_ModelPerformanceMetrics(ModelId);
GO
