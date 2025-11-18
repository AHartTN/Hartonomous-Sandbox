-- =============================================
-- sp_GetUsageAnalytics: Database-native analytics stored procedure
-- Replaces hard-coded SQL in AnalyticsController.GetUsageAnalytics (lines 64-82)
-- T-SQL aggregations benefit from query optimizer, parallelism, columnstore indexes
-- Returns JSON for easy API consumption
-- =============================================
CREATE PROCEDURE dbo.sp_GetUsageAnalytics
    @StartDate DATETIME2,
    @EndDate DATETIME2,
    @BucketInterval VARCHAR(10) = 'HOUR' -- 'HOUR', 'DAY', 'WEEK'
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @BucketFunction NVARCHAR(MAX);
    
    -- Determine time bucketing function
    SET @BucketFunction = CASE @BucketInterval
        WHEN 'HOUR' THEN 'DATEADD(HOUR, DATEDIFF(HOUR, 0, ir.RequestTimestamp), 0)'
        WHEN 'DAY' THEN 'DATEADD(DAY, DATEDIFF(DAY, 0, ir.RequestTimestamp), 0)'
        WHEN 'WEEK' THEN 'DATEADD(WEEK, DATEDIFF(WEEK, 0, ir.RequestTimestamp), 0)'
        ELSE 'DATEADD(HOUR, DATEDIFF(HOUR, 0, ir.RequestTimestamp), 0)' -- Default to hour
    END;

    -- Build dynamic SQL for time bucketing
    DECLARE @SQL NVARCHAR(MAX) = N'
    SELECT 
        ' + @BucketFunction + ' AS TimeBucket,
        COUNT(*) AS RequestCount,
        AVG(ISNULL(ir.TotalDurationMs, 0)) AS AvgDurationMs,
        SUM(CASE WHEN ir.Status = ''Completed'' THEN 1 ELSE 0 END) AS SuccessCount,
        SUM(CASE WHEN ir.Status = ''Failed'' THEN 1 ELSE 0 END) AS FailureCount
    FROM dbo.InferenceRequest ir
    WHERE ir.RequestTimestamp BETWEEN @StartDate AND @EndDate
    GROUP BY ' + @BucketFunction + '
    ORDER BY TimeBucket
    FOR JSON PATH;
    ';

    -- Execute and return JSON
    EXEC sp_executesql @SQL, 
        N'@StartDate DATETIME2, @EndDate DATETIME2', 
        @StartDate, @EndDate;
END;
GO
