CREATE PROCEDURE dbo.sp_GenerateUsageReport
    @TenantId INT,
    @ReportType NVARCHAR(50) = 'Summary', -- 'Summary', 'Detailed', 'Forecast'
    @TimeRange NVARCHAR(20) = 'Day' -- 'Day', 'Week', 'Month', 'Year'
AS
BEGIN
    SET NOCOUNT ON;
    
    BEGIN TRY
        DECLARE @StartDate DATETIME2;
        
        IF @TimeRange = 'Day'
            SET @StartDate = DATEADD(DAY, -1, SYSUTCDATETIME());
        ELSE IF @TimeRange = 'Week'
            SET @StartDate = DATEADD(WEEK, -1, SYSUTCDATETIME());
        ELSE IF @TimeRange = 'Month'
            SET @StartDate = DATEADD(MONTH, -1, SYSUTCDATETIME());
        ELSE IF @TimeRange = 'Year'
            SET @StartDate = DATEADD(YEAR, -1, SYSUTCDATETIME());
        
        IF @ReportType = 'Summary'
        BEGIN
            SELECT 
                UsageType,
                SUM(Quantity) AS TotalQuantity,
                SUM(TotalCost) AS TotalCost,
                AVG(Quantity) AS AvgQuantity,
                COUNT(*) AS RecordCount
            FROM billing.UsageLedger
            WHERE TenantId = @TenantId
                  AND RecordedUtc >= @StartDate
            GROUP BY UsageType
            ORDER BY TotalCost DESC
            FOR JSON PATH;
        END
        ELSE IF @ReportType = 'Detailed'
        BEGIN
            SELECT 
                CAST(RecordedUtc AS DATE) AS UsageDate,
                UsageType,
                SUM(Quantity) AS DailyQuantity,
                SUM(TotalCost) AS DailyCost
            FROM billing.UsageLedger
            WHERE TenantId = @TenantId
                  AND RecordedUtc >= @StartDate
            GROUP BY CAST(RecordedUtc AS DATE), UsageType
            ORDER BY UsageDate, UsageType
            FOR JSON PATH;
        END
        ELSE IF @ReportType = 'Forecast'
        BEGIN
            WITH DailyUsage AS (
                SELECT 
                    CAST(RecordedUtc AS DATE) AS UsageDate,
                    UsageType,
                    SUM(Quantity) AS DailyQuantity
                FROM billing.UsageLedger
                WHERE TenantId = @TenantId
                      AND RecordedUtc >= DATEADD(DAY, -30, SYSUTCDATETIME())
                GROUP BY CAST(RecordedUtc AS DATE), UsageType
            )
            SELECT 
                UsageType,
                AVG(DailyQuantity) AS AvgDailyUsage,
                AVG(DailyQuantity) * 30 AS ForecastMonthly
            FROM DailyUsage
            WHERE UsageDate >= DATEADD(DAY, -7, SYSUTCDATETIME())
            GROUP BY UsageType
            FOR JSON PATH;
        END
        
        RETURN 0;
    END TRY
    BEGIN CATCH
        DECLARE @ErrorMessage NVARCHAR(4000) = ERROR_MESSAGE();
        RAISERROR(@ErrorMessage, 16, 1);
        RETURN -1;
    END CATCH
END;
GO