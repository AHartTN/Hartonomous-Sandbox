-- =============================================
-- vw_ModelPerformance: Consumer-friendly wrapper over indexed view
-- Calculates AVG from SUM/COUNT_BIG stored in materialized view
-- Controllers/APIs use this view, not the indexed base view directly
-- =============================================
CREATE VIEW [dbo].[vw_ModelPerformance]
AS
SELECT 
    ModelId,
    ModelName,
    ISNULL(TotalInferences, 0) AS TotalInferences,
    LastUsed,
    CASE WHEN CountInferenceTimeMs > 0 
        THEN SumInferenceTimeMs / CountInferenceTimeMs 
        ELSE 0.0 
    END AS AvgInferenceTimeMs,
    CASE WHEN CountLayers > 0 
        THEN SumCacheHitRate / CountLayers 
        ELSE 0.0 
    END AS CacheHitRate,
    CAST(0.0 AS FLOAT) AS AvgConfidenceScore,
    CAST(0 AS BIGINT) AS TotalTokensGenerated
FROM dbo.vw_ModelPerformanceMetrics;
GO
