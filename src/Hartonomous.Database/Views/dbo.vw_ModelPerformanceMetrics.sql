-- =============================================
-- vw_ModelPerformanceMetrics: Materialized view for AnalyticsController performance queries
-- Replaces hard-coded SQL in AnalyticsController.GetModelPerformance (lines 171-193)
-- WITH SCHEMABINDING enables indexed views - query optimizer materialization
-- INNER JOIN required for indexed views (no OUTER joins allowed)
-- Uses SUM/COUNT_BIG instead of AVG for indexed view compatibility
-- =============================================
CREATE VIEW [dbo].[vw_ModelPerformanceMetrics]
WITH SCHEMABINDING
AS
SELECT 
    m.ModelId,
    m.ModelName,
    m.UsageCount AS TotalInferences,
    m.LastUsed,
    SUM(ISNULL(ml.AvgComputeTimeMs, 0.0)) AS SumInferenceTimeMs,
    COUNT_BIG(ml.AvgComputeTimeMs) AS CountInferenceTimeMs,
    SUM(ISNULL(ml.CacheHitRate, 0.0)) AS SumCacheHitRate,
    COUNT_BIG(*) AS CountLayers
FROM dbo.Model m
INNER JOIN dbo.ModelLayer ml ON ml.ModelId = m.ModelId
GROUP BY m.ModelId, m.ModelName, m.UsageCount, m.LastUsed;
GO

-- Create indexed view for automatic materialization (MASSIVE perf boost)
-- Note: Indexed view requires COUNT_BIG(*) internally but we don't expose it
CREATE UNIQUE CLUSTERED INDEX IX_vw_ModelPerformanceMetrics_ModelId 
ON dbo.vw_ModelPerformanceMetrics(ModelId) 
WITH (STATISTICS_NORECOMPUTE = OFF);
GO
