CREATE PROCEDURE dbo.sp_InferenceHistory
    @time_window_hours INT = 24,
    @TaskType NVARCHAR(50) = NULL
AS
BEGIN
    SET NOCOUNT ON;

    PRINT 'INFERENCE HISTORY ANALYSIS';
    PRINT '  Time window: Last ' + CAST(@time_window_hours AS VARCHAR(10)) + ' hours';

    DECLARE @cutoff_time DATETIME2 = DATEADD(HOUR, -@time_window_hours, SYSUTCDATETIME());

    SELECT
        TaskType,
        COUNT(*) AS request_count,
        AVG(TotalDurationMs) AS avg_duration_ms,
        MIN(TotalDurationMs) AS min_duration_ms,
        MAX(TotalDurationMs) AS max_duration_ms,
        SUM(CASE WHEN OutputData IS NOT NULL THEN 1 ELSE 0 END) AS successful_count,
        SUM(CASE WHEN OutputData IS NULL THEN 1 ELSE 0 END) AS failed_count,
        SUM(CASE WHEN CacheHit = 1 THEN 1 ELSE 0 END) AS cache_hits
    FROM dbo.InferenceRequest
    WHERE RequestTimestamp >= @cutoff_time
        AND (@TaskType IS NULL OR TaskType = @TaskType)
    GROUP BY TaskType
    ORDER BY request_count DESC;

    PRINT '✓ Inference history analysis complete';
END