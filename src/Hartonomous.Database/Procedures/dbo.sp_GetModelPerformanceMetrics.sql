-- =============================================
-- sp_GetModelPerformanceMetrics: Database-native performance analytics
-- Replaces hard-coded SQL in AnalyticsController.GetModelPerformance
-- Query optimizer can use indexed view vw_ModelPerformanceMetrics
-- Returns JSON for API consumption
-- =============================================
CREATE PROCEDURE dbo.sp_GetModelPerformanceMetrics
    @ModelId INT = NULL,
    @StartDate DATETIME2 = NULL,
    @EndDate DATETIME2 = NULL,
    @TopN INT = 10
AS
BEGIN
    SET NOCOUNT ON;

    SELECT TOP (@TopN)
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
    ORDER BY TotalInferences DESC
    FOR JSON PATH;
END;
GO
