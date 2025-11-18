-- =============================================
-- fn_GetModelPerformanceFiltered: Inline TVF for filtered performance metrics
-- Replaces hard-coded SQL in AnalyticsController with date/model filtering
-- INLINE TVFs get full query optimizer benefits
-- Query optimizer can leverage indexed view on vw_ModelPerformanceMetrics
-- =============================================
CREATE FUNCTION dbo.fn_GetModelPerformanceFiltered(
    @ModelId INT = NULL,
    @StartDate DATETIME2 = NULL,
    @EndDate DATETIME2 = NULL
)
RETURNS TABLE
AS
RETURN
(
    SELECT 
        ModelId,
        ModelName,
        TotalInferences,
        AvgInferenceTimeMs,
        AvgConfidenceScore,
        CacheHitRate,
        TotalTokensGenerated,
        LastUsed
    FROM dbo.vw_ModelPerformanceMetrics
    WHERE (@ModelId IS NULL OR ModelId = @ModelId)
      AND (@StartDate IS NULL OR LastUsed >= @StartDate)
      AND (@EndDate IS NULL OR LastUsed <= @EndDate)
);
GO
