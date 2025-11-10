-- =============================================
-- Natively Compiled Stored Procedure for Billing Inserts
-- Provides lock-free, latch-free insert performance
-- =============================================

IF OBJECT_ID('dbo.sp_InsertBillingUsageRecord_Native', 'P') IS NOT NULL
    DROP PROCEDURE dbo.sp_InsertBillingUsageRecord_Native;

CREATE PROCEDURE dbo.sp_InsertBillingUsageRecord_Native
    @TenantId NVARCHAR(128),
    @PrincipalId NVARCHAR(256),
    @Operation NVARCHAR(128),
    @MessageType NVARCHAR(128) = NULL,
    @Handler NVARCHAR(256) = NULL,
    @Units DECIMAL(18,6),
    @BaseRate DECIMAL(18,6),
    @Multiplier DECIMAL(18,6) = 1.0,
    @TotalCost DECIMAL(18,6),
    @MetadataJson NVARCHAR(MAX) = NULL
WITH NATIVE_COMPILATION, SCHEMABINDING, EXECUTE AS OWNER
AS
BEGIN ATOMIC WITH
(
    TRANSACTION ISOLATION LEVEL = SNAPSHOT,
    LANGUAGE = N'us_english'
)
    
END
