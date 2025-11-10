-- sp_RecordUsage: Generic usage tracking for all billable operations
-- Centralizes usage recording with operation-specific metadata

CREATE OR ALTER PROCEDURE dbo.sp_RecordUsage
    @TenantId INT,
    @UsageType NVARCHAR(50), -- 'TokenUsage', 'StorageUsage', 'VectorSearch', 'ComputeUsage'
    @Quantity BIGINT,
    @UnitType NVARCHAR(50), -- 'Tokens', 'Bytes', 'Queries', 'MilliCoreSeconds'
    @CostPerUnit DECIMAL(18, 8) = NULL,
    @Metadata NVARCHAR(MAX) = NULL
AS
BEGIN
    SET NOCOUNT ON;
    
    BEGIN TRY
        -- Get current pricing if not provided
        IF @CostPerUnit IS NULL
        BEGIN
            SELECT TOP 1 @CostPerUnit = UnitPrice
            FROM billing.PricingTiers
            WHERE UsageType = @UsageType 
                  AND UnitType = @UnitType
                  AND EffectiveFrom <= SYSUTCDATETIME()
                  AND (EffectiveTo IS NULL OR EffectiveTo > SYSUTCDATETIME())
            ORDER BY EffectiveFrom DESC;
            
            IF @CostPerUnit IS NULL
                SET @CostPerUnit = 0.0; -- Default to free if no pricing found
        END
        
        DECLARE @TotalCost DECIMAL(18, 8) = @Quantity * @CostPerUnit;
        
        -- Insert usage record
        INSERT INTO billing.UsageLedger (
            TenantId,
            UsageType,
            Quantity,
            UnitType,
            CostPerUnit,
            TotalCost,
            Metadata,
            RecordedUtc
        )
        VALUES (
            @TenantId,
            @UsageType,
            @Quantity,
            @UnitType,
            @CostPerUnit,
            @TotalCost,
            @Metadata,
            SYSUTCDATETIME()
        );
        
        -- Check quota limits
        DECLARE @QuotaLimit BIGINT;
        DECLARE @CurrentUsage BIGINT;
        
        SELECT @QuotaLimit = QuotaLimit
        FROM billing.TenantQuotas
        WHERE TenantId = @TenantId 
              AND UsageType = @UsageType
              AND IsActive = 1;
        
        IF @QuotaLimit IS NOT NULL
        BEGIN
            SELECT @CurrentUsage = SUM(Quantity)
            FROM billing.UsageLedger
            WHERE TenantId = @TenantId
                  AND UsageType = @UsageType
                  AND RecordedUtc >= DATEADD(MONTH, -1, SYSUTCDATETIME());
            
            IF @CurrentUsage > @QuotaLimit
            BEGIN
                -- Log quota violation
                INSERT INTO billing.QuotaViolations (
                    TenantId,
                    UsageType,
                    QuotaLimit,
                    CurrentUsage,
                    ViolatedUtc
                )
                VALUES (
                    @TenantId,
                    @UsageType,
                    @QuotaLimit,
                    @CurrentUsage,
                    SYSUTCDATETIME()
                );
                
                RAISERROR('Quota exceeded for usage type: %s', 16, 1, @UsageType);
            END
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

-- sp_CalculateBill: Aggregate usage and generate invoice
-- Applies pricing tiers and discounts

CREATE OR ALTER PROCEDURE dbo.sp_CalculateBill
    @TenantId INT,
    @BillingPeriodStart DATETIME2 = NULL,
    @BillingPeriodEnd DATETIME2 = NULL,
    @GenerateInvoice BIT = 0
AS
BEGIN
    SET NOCOUNT ON;
    
    BEGIN TRY
        -- Default to current month if not specified
        IF @BillingPeriodStart IS NULL
            SET @BillingPeriodStart = DATEADD(MONTH, DATEDIFF(MONTH, 0, SYSUTCDATETIME()), 0);
        
        IF @BillingPeriodEnd IS NULL
            SET @BillingPeriodEnd = DATEADD(MONTH, 1, @BillingPeriodStart);
        
        -- Aggregate usage by type
        DECLARE @UsageSummary TABLE (
            UsageType NVARCHAR(50),
            TotalQuantity BIGINT,
            TotalCost DECIMAL(18, 2)
        );
        
        INSERT INTO @UsageSummary
        SELECT 
            UsageType,
            SUM(Quantity) AS TotalQuantity,
            SUM(TotalCost) AS TotalCost
        FROM billing.UsageLedger
        WHERE TenantId = @TenantId
              AND RecordedUtc >= @BillingPeriodStart
              AND RecordedUtc < @BillingPeriodEnd
        GROUP BY UsageType;
        
        DECLARE @Subtotal DECIMAL(18, 2) = (SELECT SUM(TotalCost) FROM @UsageSummary);
        DECLARE @DiscountPercent DECIMAL(5, 2) = 0.0;
        DECLARE @Tax DECIMAL(18, 2) = 0.0;
        DECLARE @Total DECIMAL(18, 2);
        
        -- Apply volume discounts
        IF @Subtotal > 10000
            SET @DiscountPercent = 15.0;
        ELSE IF @Subtotal > 5000
            SET @DiscountPercent = 10.0;
        ELSE IF @Subtotal > 1000
            SET @DiscountPercent = 5.0;
        
        DECLARE @Discount DECIMAL(18, 2) = @Subtotal * (@DiscountPercent / 100.0);
        SET @Tax = (@Subtotal - @Discount) * 0.08; -- 8% tax
        SET @Total = @Subtotal - @Discount + @Tax;
        
        -- Generate invoice if requested
        IF @GenerateInvoice = 1
        BEGIN
            INSERT INTO billing.Invoices (
                TenantId,
                InvoiceNumber,
                BillingPeriodStart,
                BillingPeriodEnd,
                Subtotal,
                Discount,
                Tax,
                Total,
                Status,
                GeneratedUtc
            )
            VALUES (
                @TenantId,
                'INV-' + FORMAT(@TenantId, '00000') + '-' + FORMAT(SYSUTCDATETIME(), 'yyyyMMdd'),
                @BillingPeriodStart,
                @BillingPeriodEnd,
                @Subtotal,
                @Discount,
                @Tax,
                @Total,
                'Pending',
                SYSUTCDATETIME()
            );
        END
        
        -- Return summary
        SELECT 
            @TenantId AS TenantId,
            @BillingPeriodStart AS PeriodStart,
            @BillingPeriodEnd AS PeriodEnd,
            @Subtotal AS Subtotal,
            @DiscountPercent AS DiscountPercent,
            @Discount AS Discount,
            @Tax AS Tax,
            @Total AS Total,
            (SELECT * FROM @UsageSummary FOR JSON PATH) AS UsageBreakdown;
        
        RETURN 0;
    END TRY
    BEGIN CATCH
        DECLARE @ErrorMessage NVARCHAR(4000) = ERROR_MESSAGE();
        RAISERROR(@ErrorMessage, 16, 1);
        RETURN -1;
    END CATCH
END;
GO

-- sp_GenerateUsageReport: Tenant dashboard query
-- Real-time usage metrics with trend analysis

CREATE OR ALTER PROCEDURE dbo.sp_GenerateUsageReport
    @TenantId INT,
    @ReportType NVARCHAR(50) = 'Summary', -- 'Summary', 'Detailed', 'Forecast'
    @TimeRange NVARCHAR(20) = 'Month' -- 'Day', 'Week', 'Month', 'Year'
AS
BEGIN
    SET NOCOUNT ON;
    
    BEGIN TRY
        DECLARE @StartDate DATETIME2;
        
        -- Determine time range
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
            -- High-level metrics
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
            -- Time-series data
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
            -- Simple linear forecast (7-day moving average)
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
