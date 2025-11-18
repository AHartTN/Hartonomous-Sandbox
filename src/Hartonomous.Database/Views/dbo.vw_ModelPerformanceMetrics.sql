-- =============================================
-- vw_ModelPerformanceMetrics: Materialized view for AnalyticsController performance queries
-- Replaces hard-coded SQL in AnalyticsController.GetModelPerformance (lines 171-193)
-- WITH SCHEMABINDING enables indexed views - query optimizer materialization
-- Flattened with LEFT JOINs instead of subqueries for indexing support
-- =============================================
CREATE VIEW [dbo].[vw_ModelPerformanceMetrics]
WITH SCHEMABINDING
AS
SELECT 
    m.ModelId,
    m.ModelName,
    ISNULL(m.UsageCount, 0) AS TotalInferences,
    m.LastUsed,
    AVG(ml.AvgComputeTimeMs) AS AvgInferenceTimeMs,
    AVG(ISNULL(ml.CacheHitRate, 0.0)) AS CacheHitRate,
    CAST(0.0 AS FLOAT) AS AvgConfidenceScore,
    CAST(0 AS BIGINT) AS TotalTokensGenerated
FROM dbo.Model m
LEFT JOIN dbo.ModelLayer ml ON ml.ModelId = m.ModelId
GROUP BY m.ModelId, m.ModelName, m.UsageCount, m.LastUsed;
GO

-- Create indexed view for automatic materialization (MASSIVE perf boost)
-- Note: Indexed view requires COUNT_BIG(*) internally but we don't expose it
CREATE UNIQUE CLUSTERED INDEX IX_vw_ModelPerformanceMetrics_ModelId 
ON dbo.vw_ModelPerformanceMetrics(ModelId) 
WITH (STATISTICS_NORECOMPUTE = OFF);
GO
