CREATE OR ALTER PROCEDURE dbo.sp_CalculateBill
    @TenantId INT,
    @BillingPeriodStart DATETIME2 = NULL,
    @BillingPeriodEnd DATETIME2 = NULL,
    @GenerateInvoice BIT = 0
AS
BEGIN
    SET NOCOUNT ON;
    
    BEGIN TRY
        IF @BillingPeriodStart IS NULL
            SET @BillingPeriodStart = DATEADD(MONTH, DATEDIFF(MONTH, 0, SYSUTCDATETIME()), 0);
        
        IF @BillingPeriodEnd IS NULL
            SET @BillingPeriodEnd = DATEADD(MONTH, 1, @BillingPeriodStart);
        
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
        FROM dbo.BillingUsageLedger
        WHERE TenantId = @TenantId
              AND RecordedUtc >= @BillingPeriodStart
              AND RecordedUtc < @BillingPeriodEnd
        GROUP BY UsageType;
        
        DECLARE @Subtotal DECIMAL(18, 2) = (SELECT SUM(TotalCost) FROM @UsageSummary);
        DECLARE @DiscountPercent DECIMAL(5, 2) = 0.0;
        DECLARE @Tax DECIMAL(18, 2) = 0.0;
        DECLARE @Total DECIMAL(18, 2);
        
        IF @Subtotal > 10000
            SET @DiscountPercent = 15.0;
        ELSE IF @Subtotal > 5000
            SET @DiscountPercent = 10.0;
        ELSE IF @Subtotal > 1000
            SET @DiscountPercent = 5.0;
        
        DECLARE @Discount DECIMAL(18, 2) = @Subtotal * (@DiscountPercent / 100.0);
        SET @Tax = (@Subtotal - @Discount) * 0.08; -- 8% tax
        SET @Total = @Subtotal - @Discount + @Tax;
        
        IF @GenerateInvoice = 1
        BEGIN
            INSERT INTO dbo.BillingInvoice (
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