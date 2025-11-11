CREATE PROCEDURE dbo.sp_RecordUsage
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
                SET @CostPerUnit = 0.0;
        END
        
        DECLARE @TotalCost DECIMAL(18, 8) = @Quantity * @CostPerUnit;
        
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