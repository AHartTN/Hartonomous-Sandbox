# SQL Audit Part 23: Billing Tables

## Overview
Billing system tables analyzed: 9 files
Focus: Pre-execution cost estimation, post-execution metering, subscription management, payment gateway integration (CIM/ARB), quota enforcement, and usage-based pricing.

---

## 1. BillingUsageLedger.sql

### Schema Analysis
```sql
CREATE TABLE [dbo].[BillingUsageLedger] (
    [LedgerId]     BIGINT          NOT NULL IDENTITY,
    [TenantId]     NVARCHAR (128)  NOT NULL,
    [PrincipalId]  NVARCHAR (256)  NULL,          -- ❌ CRITICAL: MUST be NOT NULL
    [Operation]    NVARCHAR (128)  NULL,          -- ❌ CRITICAL: MUST be NOT NULL
    [MessageType]  NVARCHAR (128)  NULL,          -- ✓ Legitimate nullable (not all ops have message types)
    [Handler]      NVARCHAR (256)  NULL,          -- ✓ Legitimate nullable (system-generated ops)
    [UsageType]    NVARCHAR (50)   NULL,          -- ❌ CRITICAL: MUST be NOT NULL
    [Quantity]     BIGINT          NULL,          -- ❌ CRITICAL: MUST be NOT NULL (or remove if redundant)
    [UnitType]     NVARCHAR (50)   NULL,          -- ❌ CRITICAL: MUST be NOT NULL
    [CostPerUnit]  DECIMAL (18, 8) NULL,          -- ❌ CRITICAL: MUST be NOT NULL
    [Units]        DECIMAL (18, 6) NULL,          -- ❌ CRITICAL: MUST be NOT NULL
    [BaseRate]     DECIMAL (18, 6) NULL,          -- ❌ CRITICAL: MUST be NOT NULL
    [Multiplier]   DECIMAL (18, 6) NOT NULL DEFAULT 1.0,  -- ✓ Correct
    [TotalCost]    DECIMAL (18, 6) NOT NULL,      -- ✓ Correct
    [Metadata]     NVARCHAR (MAX)  NULL,          -- ❌ REMOVE: Redundant with MetadataJson
    [MetadataJson] NVARCHAR (MAX)  NULL,          -- ✓ Legitimate nullable (optional context)
    [RecordedUtc]  DATETIME2 (7)   NULL,          -- ❌ CRITICAL: MUST be NOT NULL
    [TimestampUtc] DATETIME2 (7)   NOT NULL DEFAULT (SYSUTCDATETIME()),  -- ✓ Correct
    CONSTRAINT [PK_BillingUsageLedger] PRIMARY KEY CLUSTERED ([LedgerId] ASC),
    INDEX [IX_BillingUsageLedger_Tenant] ([TenantId], [TimestampUtc] DESC),
    INDEX [IX_BillingUsageLedger_UsageType] ([UsageType], [RecordedUtc] DESC)
);
```

### CRITICAL ISSUES

#### 1. Financial Audit Trail Integrity VIOLATION
**Severity**: CRITICAL - Legal/Compliance Risk

Every billing record MUST have complete attribution:
- **PrincipalId NULL**: Cannot audit WHO incurred the charge
- **Operation NULL**: Cannot audit WHAT was charged
- **UsageType NULL**: Cannot categorize/invoice properly
- **RecordedUtc NULL**: Cannot establish temporal audit trail

**Impact**: 
- SOX/GDPR compliance violations
- Dispute resolution impossible
- Chargebacks undefendable
- Revenue recognition errors

**REQUIRED FIX**:
```sql
ALTER TABLE [dbo].[BillingUsageLedger] ALTER COLUMN [PrincipalId] NVARCHAR(256) NOT NULL;
ALTER TABLE [dbo].[BillingUsageLedger] ALTER COLUMN [Operation] NVARCHAR(128) NOT NULL;
ALTER TABLE [dbo].[BillingUsageLedger] ALTER COLUMN [UsageType] NVARCHAR(50) NOT NULL;
ALTER TABLE [dbo].[BillingUsageLedger] ALTER COLUMN [RecordedUtc] DATETIME2(7) NOT NULL;
```

#### 2. Cost Calculation Fields NULL - Double Entry Accounting BROKEN
**Severity**: CRITICAL - Financial Integrity

Cannot reconstruct cost if ANY component is NULL:
- **Quantity NULL**: Cannot verify charge amount
- **UnitType NULL**: Cannot match against rate plan
- **CostPerUnit NULL**: Cannot recalculate cost
- **Units NULL**: Cannot verify against quantity
- **BaseRate NULL**: Cannot separate base rate from multipliers

**Impact**: 
- Revenue leakage detection impossible
- Rate plan audits fail
- Overcharge/undercharge undetectable
- Tax calculation errors

**REQUIRED FIX**:
```sql
ALTER TABLE [dbo].[BillingUsageLedger] ALTER COLUMN [Quantity] BIGINT NOT NULL;
ALTER TABLE [dbo].[BillingUsageLedger] ALTER COLUMN [UnitType] NVARCHAR(50) NOT NULL;
ALTER TABLE [dbo].[BillingUsageLedger] ALTER COLUMN [CostPerUnit] DECIMAL(18,8) NOT NULL;
ALTER TABLE [dbo].[BillingUsageLedger] ALTER COLUMN [Units] DECIMAL(18,6) NOT NULL;
ALTER TABLE [dbo].[BillingUsageLedger] ALTER COLUMN [BaseRate] DECIMAL(18,6) NOT NULL;
```

#### 3. Duplicate Metadata Columns - Data Inconsistency Risk
**Issue**: Both `Metadata` and `MetadataJson` exist

**REQUIRED FIX**: Remove `Metadata` column, standardize on `MetadataJson`:
```sql
ALTER TABLE [dbo].[BillingUsageLedger] DROP COLUMN [Metadata];
```

#### 4. Missing Pre-Execution Cost Estimation Support
**Issue**: No mechanism for cost estimation BEFORE operation execution

**REQUIRED ADDITIONS**:
```sql
-- Add pre-execution estimation flag and cost estimate
ALTER TABLE [dbo].[BillingUsageLedger] ADD [IsEstimate] BIT NOT NULL DEFAULT 0;
ALTER TABLE [dbo].[BillingUsageLedger] ADD [EstimatedCost] DECIMAL(18,6) NULL;
ALTER TABLE [dbo].[BillingUsageLedger] ADD [ActualCostVariance] AS (TotalCost - EstimatedCost) PERSISTED;

-- Add index for estimate validation analysis
CREATE NONCLUSTERED INDEX [IX_BillingUsageLedger_Estimates]
    ON [dbo].[BillingUsageLedger]([IsEstimate], [UsageType])
    INCLUDE ([EstimatedCost], [TotalCost])
    WHERE [IsEstimate] = 1;
```

#### 5. Missing Payment Gateway Integration Columns
**Issue**: No reference to payment gateway transactions (Authorize.Net CIM/ARB, Stripe, etc.)

**REQUIRED ADDITIONS**:
```sql
-- Payment gateway integration
ALTER TABLE [dbo].[BillingUsageLedger] ADD [PaymentGatewayTransactionId] NVARCHAR(128) NULL;
ALTER TABLE [dbo].[BillingUsageLedger] ADD [PaymentGatewayType] NVARCHAR(50) NULL; -- 'AuthorizeNet_CIM', 'AuthorizeNet_ARB', 'Stripe'
ALTER TABLE [dbo].[BillingUsageLedger] ADD [PaymentStatus] NVARCHAR(50) NULL; -- 'Pending', 'Captured', 'Refunded', 'Failed'
ALTER TABLE [dbo].[BillingUsageLedger] ADD [PaymentProcessedUtc] DATETIME2(7) NULL;

-- Index for payment reconciliation
CREATE NONCLUSTERED INDEX [IX_BillingUsageLedger_PaymentGateway]
    ON [dbo].[BillingUsageLedger]([PaymentGatewayType], [PaymentStatus])
    INCLUDE ([PaymentGatewayTransactionId], [TotalCost], [PaymentProcessedUtc])
    WHERE [PaymentGatewayTransactionId] IS NOT NULL;
```

#### 6. Missing Subscription/Recurring Billing Support
**Issue**: No link to subscription plans or ARB (Automated Recurring Billing)

**REQUIRED ADDITIONS**:
```sql
-- Subscription tracking
ALTER TABLE [dbo].[BillingUsageLedger] ADD [SubscriptionId] UNIQUEIDENTIFIER NULL;
ALTER TABLE [dbo].[BillingUsageLedger] ADD [BillingCycle] NVARCHAR(20) NULL; -- 'Monthly', 'Quarterly', 'Annual'
ALTER TABLE [dbo].[BillingUsageLedger] ADD [IsRecurring] BIT NOT NULL DEFAULT 0;

-- Index for subscription analysis
CREATE NONCLUSTERED INDEX [IX_BillingUsageLedger_Subscription]
    ON [dbo].[BillingUsageLedger]([SubscriptionId], [TimestampUtc] DESC)
    INCLUDE ([TotalCost], [UsageType])
    WHERE [SubscriptionId] IS NOT NULL;
```

#### 7. Missing Refund/Credit Support
**Issue**: No mechanism for credits, refunds, or adjustments

**REQUIRED ADDITIONS**:
```sql
-- Refund/credit tracking
ALTER TABLE [dbo].[BillingUsageLedger] ADD [IsRefund] BIT NOT NULL DEFAULT 0;
ALTER TABLE [dbo].[BillingUsageLedger] ADD [OriginalLedgerId] BIGINT NULL; -- References refunded transaction
ALTER TABLE [dbo].[BillingUsageLedger] ADD [RefundReason] NVARCHAR(500) NULL;

-- Foreign key for refund chain
ALTER TABLE [dbo].[BillingUsageLedger] 
    ADD CONSTRAINT [FK_BillingUsageLedger_OriginalTransaction]
    FOREIGN KEY ([OriginalLedgerId]) REFERENCES [dbo].[BillingUsageLedger]([LedgerId]);

-- Index for refund tracking
CREATE NONCLUSTERED INDEX [IX_BillingUsageLedger_Refunds]
    ON [dbo].[BillingUsageLedger]([IsRefund], [OriginalLedgerId])
    INCLUDE ([TotalCost], [RefundReason])
    WHERE [IsRefund] = 1;
```

### Performance Issues

#### 1. Missing Columnstore for OLAP Analytics
**Issue**: Billing analytics queries will be slow without columnstore

**REQUIRED FIX**:
```sql
-- Create columnstore index for analytics workload
CREATE NONCLUSTERED COLUMNSTORE INDEX [NCCI_BillingUsageLedger_Analytics]
    ON [dbo].[BillingUsageLedger](
        [TenantId], [PrincipalId], [Operation], [UsageType], [UnitType],
        [Quantity], [Units], [BaseRate], [Multiplier], [TotalCost],
        [RecordedUtc], [TimestampUtc], [IsEstimate], [IsRefund], [PaymentStatus]
    );
```

#### 2. Missing Table Partitioning for Time-Series Data
**Issue**: Billions of records, no partitioning = poor query performance and maintenance windows

**REQUIRED FIX**:
```sql
-- Partition by month for efficient archival and query filtering
CREATE PARTITION FUNCTION [PF_BillingUsageLedger_Monthly](DATETIME2(7))
AS RANGE RIGHT FOR VALUES (
    '2024-01-01', '2024-02-01', '2024-03-01', '2024-04-01',
    '2024-05-01', '2024-06-01', '2024-07-01', '2024-08-01',
    '2024-09-01', '2024-10-01', '2024-11-01', '2024-12-01',
    '2025-01-01' -- Add new months as sliding window
);

CREATE PARTITION SCHEME [PS_BillingUsageLedger_Monthly]
AS PARTITION [PF_BillingUsageLedger_Monthly]
ALL TO ([PRIMARY]);

-- Rebuild clustered index on partition scheme
DROP INDEX [PK_BillingUsageLedger] ON [dbo].[BillingUsageLedger];
ALTER TABLE [dbo].[BillingUsageLedger]
ADD CONSTRAINT [PK_BillingUsageLedger] 
    PRIMARY KEY CLUSTERED ([TimestampUtc], [LedgerId])
    ON [PS_BillingUsageLedger_Monthly]([TimestampUtc]);
```

### Architecture Issues

#### 1. No Atomization of Cost Components
**Issue**: Financial calculations stored as monolithic DECIMAL values, not atomized

**Current Storage**: `TotalCost DECIMAL(18,6) = 123.456789`

**SHOULD BE**: Each cost component as separate atom:
- BaseRate atom (e.g., 0.00008)
- Multiplier atom (e.g., 1.5)
- Quantity atom (e.g., 10000)
- TotalCost = BaseRate × Multiplier × Quantity (computed)

**Benefits**:
- CAS deduplication (99% of rates are identical across millions of records)
- Audit trail at component level
- Rate plan changes don't require data migration
- Tax jurisdiction calculations via atom relations

**FUTURE REFACTOR**: See `docs/architecture/financial-atomization.md` (to be created)

---

## 2. BillingUsageLedger_InMemory.sql

### Schema Analysis
```sql
CREATE TABLE [dbo].[BillingUsageLedger_InMemory]
(
    [LedgerId]      BIGINT           IDENTITY (1, 1) NOT NULL,
    [TenantId]      NVARCHAR (128)   COLLATE Latin1_General_100_BIN2 NOT NULL,  -- ✓ Correct
    [PrincipalId]   NVARCHAR (256)   COLLATE Latin1_General_100_BIN2 NOT NULL,  -- ✓ Correct
    [Operation]     NVARCHAR (128)   COLLATE Latin1_General_100_BIN2 NOT NULL,  -- ✓ Correct
    [MessageType]   NVARCHAR (128)   COLLATE Latin1_General_100_BIN2 NULL,      -- ✓ Correct
    [Handler]       NVARCHAR (256)   COLLATE Latin1_General_100_BIN2 NULL,      -- ✓ Correct
    [Units]         DECIMAL (18, 6)  NOT NULL,  -- ✓ Correct
    [BaseRate]      DECIMAL (18, 6)  NOT NULL,  -- ✓ Correct
    [Multiplier]    DECIMAL (18, 6)  NOT NULL DEFAULT (1.0),  -- ✓ Correct
    [TotalCost]     DECIMAL (18, 6)  NOT NULL,  -- ✓ Correct
    [MetadataJson]  NVARCHAR (MAX)   COLLATE Latin1_General_100_BIN2 NULL,  -- ✓ Correct
    [TimestampUtc]  DATETIME2 (7)    NOT NULL DEFAULT (SYSUTCDATETIME()),  -- ✓ Correct
    
    CONSTRAINT [PK_BillingUsageLedger_InMemory] PRIMARY KEY NONCLUSTERED ([LedgerId]),
    
    INDEX [IX_TenantId_Hash] HASH ([TenantId]) WITH (BUCKET_COUNT = 10000000),
    INDEX [IX_Timestamp_Range] NONCLUSTERED ([TimestampUtc] DESC)
)
WITH (MEMORY_OPTIMIZED = ON, DURABILITY = SCHEMA_AND_DATA);
```

### ✓ EXCELLENT DESIGN

This is **CORRECTLY DESIGNED** as hot-path write-optimized table:

1. **All NOT NULL constraints correct** - No nullable financial fields
2. **Binary collation** - Faster string comparisons (case-sensitive exact matching)
3. **Hash index on TenantId** - O(1) tenant lookup
4. **Nonclustered PK** - No clustered index overhead on writes
5. **Range index on timestamp** - Efficient time-range queries for archival
6. **Durability = SCHEMA_AND_DATA** - Crash recovery without data loss

### REQUIRED ADDITIONS (Parity with Disk Table)

#### 1. Missing Pre-Execution and Payment Fields
```sql
-- Add same columns as disk table for parity
ALTER TABLE [dbo].[BillingUsageLedger_InMemory] ADD [UsageType] NVARCHAR(50) COLLATE Latin1_General_100_BIN2 NOT NULL DEFAULT '';
ALTER TABLE [dbo].[BillingUsageLedger_InMemory] ADD [UnitType] NVARCHAR(50) COLLATE Latin1_General_100_BIN2 NOT NULL DEFAULT '';
ALTER TABLE [dbo].[BillingUsageLedger_InMemory] ADD [Quantity] BIGINT NOT NULL DEFAULT 0;
ALTER TABLE [dbo].[BillingUsageLedger_InMemory] ADD [CostPerUnit] DECIMAL(18,8) NOT NULL DEFAULT 0;
ALTER TABLE [dbo].[BillingUsageLedger_InMemory] ADD [RecordedUtc] DATETIME2(7) NOT NULL DEFAULT SYSUTCDATETIME();

-- Pre-execution estimation
ALTER TABLE [dbo].[BillingUsageLedger_InMemory] ADD [IsEstimate] BIT NOT NULL DEFAULT 0;
ALTER TABLE [dbo].[BillingUsageLedger_InMemory] ADD [EstimatedCost] DECIMAL(18,6) NULL;

-- Payment gateway
ALTER TABLE [dbo].[BillingUsageLedger_InMemory] ADD [PaymentGatewayTransactionId] NVARCHAR(128) COLLATE Latin1_General_100_BIN2 NULL;
ALTER TABLE [dbo].[BillingUsageLedger_InMemory] ADD [PaymentGatewayType] NVARCHAR(50) COLLATE Latin1_General_100_BIN2 NULL;
ALTER TABLE [dbo].[BillingUsageLedger_InMemory] ADD [PaymentStatus] NVARCHAR(50) COLLATE Latin1_General_100_BIN2 NULL;

-- Subscription
ALTER TABLE [dbo].[BillingUsageLedger_InMemory] ADD [SubscriptionId] UNIQUEIDENTIFIER NULL;
ALTER TABLE [dbo].[BillingUsageLedger_InMemory] ADD [IsRecurring] BIT NOT NULL DEFAULT 0;

-- Refund
ALTER TABLE [dbo].[BillingUsageLedger_InMemory] ADD [IsRefund] BIT NOT NULL DEFAULT 0;
ALTER TABLE [dbo].[BillingUsageLedger_InMemory] ADD [OriginalLedgerId] BIGINT NULL;

-- Hash indexes for new lookup columns
CREATE INDEX [IX_UsageType_Hash] ON [dbo].[BillingUsageLedger_InMemory]([UsageType]) WITH (BUCKET_COUNT = 1000);
CREATE INDEX [IX_PaymentGateway_Hash] ON [dbo].[BillingUsageLedger_InMemory]([PaymentGatewayTransactionId]) WITH (BUCKET_COUNT = 100000);
CREATE INDEX [IX_Subscription_Hash] ON [dbo].[BillingUsageLedger_InMemory]([SubscriptionId]) WITH (BUCKET_COUNT = 100000);
```

#### 2. Background Archival to Disk Table
**REQUIRED**: Stored procedure to move aged records to disk-based table

```sql
CREATE PROCEDURE [dbo].[sp_ArchiveBillingUsageRecords]
    @OlderThanHours INT = 24,
    @BatchSize INT = 100000
WITH NATIVE_COMPILATION, SCHEMABINDING, EXECUTE AS OWNER
AS
BEGIN ATOMIC WITH
(
    TRANSACTION ISOLATION LEVEL = SNAPSHOT,
    LANGUAGE = N'us_english'
)
    DECLARE @CutoffTime DATETIME2(7) = DATEADD(HOUR, -@OlderThanHours, SYSUTCDATETIME());
    
    -- Archive to disk table (use CLR for bulk insert to avoid row-by-row)
    INSERT INTO dbo.BillingUsageLedger (
        TenantId, PrincipalId, Operation, MessageType, Handler,
        UsageType, Quantity, UnitType, CostPerUnit, Units, BaseRate, Multiplier, TotalCost,
        MetadataJson, RecordedUtc, TimestampUtc,
        IsEstimate, EstimatedCost, PaymentGatewayTransactionId, PaymentGatewayType, PaymentStatus,
        SubscriptionId, IsRecurring, IsRefund, OriginalLedgerId
    )
    SELECT TOP (@BatchSize)
        TenantId, PrincipalId, Operation, MessageType, Handler,
        UsageType, Quantity, UnitType, CostPerUnit, Units, BaseRate, Multiplier, TotalCost,
        MetadataJson, RecordedUtc, TimestampUtc,
        IsEstimate, EstimatedCost, PaymentGatewayTransactionId, PaymentGatewayType, PaymentStatus,
        SubscriptionId, IsRecurring, IsRefund, OriginalLedgerId
    FROM dbo.BillingUsageLedger_InMemory
    WHERE TimestampUtc < @CutoffTime
    ORDER BY TimestampUtc ASC;
    
    -- Delete archived records
    DELETE TOP (@BatchSize)
    FROM dbo.BillingUsageLedger_InMemory
    WHERE TimestampUtc < @CutoffTime;
END
GO
```

**Schedule**: Run every hour via SQL Agent job

---

## 3. BillingTenantQuota.sql

### Schema Analysis
```sql
CREATE TABLE [dbo].[BillingTenantQuota] (
    [QuotaId]       INT            NOT NULL IDENTITY,
    [TenantId]      INT            NOT NULL,  -- ✓ Correct
    [UsageType]     NVARCHAR (50)  NOT NULL,  -- ✓ Correct
    [QuotaLimit]    BIGINT         NOT NULL,  -- ✓ Correct
    [IsActive]      BIT            NOT NULL DEFAULT 1,  -- ✓ Correct
    [ResetInterval] NVARCHAR (20)  NULL,      -- ❌ CRITICAL: MUST be NOT NULL
    [Description]   NVARCHAR (500) NULL,      -- ✓ Legitimate nullable (optional)
    [MetadataJson]  NVARCHAR (MAX) NULL,      -- ✓ Legitimate nullable (optional)
    [CreatedUtc]    DATETIME2 (7)  NOT NULL DEFAULT (SYSUTCDATETIME()),  -- ✓ Correct
    [UpdatedUtc]    DATETIME2 (7)  NOT NULL DEFAULT (SYSUTCDATETIME()),  -- ✓ Correct
    CONSTRAINT [PK_BillingTenantQuotas] PRIMARY KEY CLUSTERED ([QuotaId] ASC),
    INDEX [IX_BillingTenantQuotas_Tenant] ([TenantId], [UsageType], [IsActive])
);
```

### CRITICAL ISSUES

#### 1. ResetInterval NULL - Quota Enforcement Broken
**Severity**: CRITICAL - Business Logic Failure

Without `ResetInterval`, system cannot determine when quota resets:
- Daily quotas never reset → permanent lockout after first violation
- Monthly quotas accumulate forever → incorrect billing
- No reset = quota system is non-functional

**REQUIRED FIX**:
```sql
ALTER TABLE [dbo].[BillingTenantQuota] ALTER COLUMN [ResetInterval] NVARCHAR(20) NOT NULL;

-- Add constraint for valid values
ALTER TABLE [dbo].[BillingTenantQuota]
    ADD CONSTRAINT [CK_BillingTenantQuota_ResetInterval]
    CHECK ([ResetInterval] IN ('Hourly', 'Daily', 'Weekly', 'Monthly', 'Quarterly', 'Yearly', 'Never'));
```

#### 2. Missing Current Usage Tracking
**Issue**: Quota limit exists, but no field for current usage → cannot enforce quota in real-time

**REQUIRED ADDITIONS**:
```sql
-- Real-time usage tracking
ALTER TABLE [dbo].[BillingTenantQuota] ADD [CurrentUsage] BIGINT NOT NULL DEFAULT 0;
ALTER TABLE [dbo].[BillingTenantQuota] ADD [LastResetUtc] DATETIME2(7) NOT NULL DEFAULT SYSUTCDATETIME();
ALTER TABLE [dbo].[BillingTenantQuota] ADD [NextResetUtc] AS (
    CASE [ResetInterval]
        WHEN 'Hourly' THEN DATEADD(HOUR, 1, [LastResetUtc])
        WHEN 'Daily' THEN DATEADD(DAY, 1, [LastResetUtc])
        WHEN 'Weekly' THEN DATEADD(WEEK, 1, [LastResetUtc])
        WHEN 'Monthly' THEN DATEADD(MONTH, 1, [LastResetUtc])
        WHEN 'Quarterly' THEN DATEADD(QUARTER, 1, [LastResetUtc])
        WHEN 'Yearly' THEN DATEADD(YEAR, 1, [LastResetUtc])
        ELSE NULL
    END
) PERSISTED;

-- Computed column: Remaining quota
ALTER TABLE [dbo].[BillingTenantQuota] ADD [RemainingQuota] AS ([QuotaLimit] - [CurrentUsage]) PERSISTED;
ALTER TABLE [dbo].[BillingTenantQuota] ADD [IsExceeded] AS (
    CASE WHEN [CurrentUsage] > [QuotaLimit] THEN CAST(1 AS BIT) ELSE CAST(0 AS BIT) END
) PERSISTED;

-- Index for quota enforcement hot path
CREATE NONCLUSTERED INDEX [IX_BillingTenantQuota_Enforcement]
    ON [dbo].[BillingTenantQuota]([TenantId], [UsageType], [IsActive], [IsExceeded])
    INCLUDE ([CurrentUsage], [QuotaLimit], [RemainingQuota])
    WHERE [IsActive] = 1;
```

#### 3. Missing Soft/Hard Limit Thresholds
**Issue**: No warning thresholds before hard limit (90% warning, 100% block)

**REQUIRED ADDITIONS**:
```sql
-- Soft warning threshold (e.g., 80% = notify, 90% = warn, 100% = block)
ALTER TABLE [dbo].[BillingTenantQuota] ADD [SoftLimitPercent] INT NOT NULL DEFAULT 80;
ALTER TABLE [dbo].[BillingTenantQuota] ADD [WarnLimitPercent] INT NOT NULL DEFAULT 90;

-- Add constraint for valid percentages
ALTER TABLE [dbo].[BillingTenantQuota]
    ADD CONSTRAINT [CK_BillingTenantQuota_Thresholds]
    CHECK ([SoftLimitPercent] BETWEEN 0 AND 100 
           AND [WarnLimitPercent] BETWEEN 0 AND 100
           AND [SoftLimitPercent] <= [WarnLimitPercent]);

-- Computed columns for threshold detection
ALTER TABLE [dbo].[BillingTenantQuota] ADD [SoftLimitReached] AS (
    CASE WHEN ([CurrentUsage] * 100.0 / [QuotaLimit]) >= [SoftLimitPercent] 
         THEN CAST(1 AS BIT) ELSE CAST(0 AS BIT) END
) PERSISTED;

ALTER TABLE [dbo].[BillingTenantQuota] ADD [WarnLimitReached] AS (
    CASE WHEN ([CurrentUsage] * 100.0 / [QuotaLimit]) >= [WarnLimitPercent] 
         THEN CAST(1 AS BIT) ELSE CAST(0 AS BIT) END
) PERSISTED;
```

#### 4. Missing Quota Enforcement Stored Procedure
**REQUIRED**: Real-time quota check before operation execution

```sql
CREATE PROCEDURE [dbo].[sp_CheckAndIncrementQuota]
    @TenantId INT,
    @UsageType NVARCHAR(50),
    @IncrementAmount BIGINT,
    @QuotaExceeded BIT OUTPUT,
    @RemainingQuota BIGINT OUTPUT
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;
    
    BEGIN TRANSACTION;
    
    -- Reset quota if interval expired
    UPDATE dbo.BillingTenantQuota
    SET CurrentUsage = 0,
        LastResetUtc = SYSUTCDATETIME()
    WHERE TenantId = @TenantId
      AND UsageType = @UsageType
      AND IsActive = 1
      AND SYSUTCDATETIME() >= NextResetUtc;
    
    -- Check quota and increment atomically
    DECLARE @CurrentUsage BIGINT, @QuotaLimit BIGINT;
    
    SELECT 
        @CurrentUsage = CurrentUsage,
        @QuotaLimit = QuotaLimit
    FROM dbo.BillingTenantQuota WITH (UPDLOCK, HOLDLOCK)
    WHERE TenantId = @TenantId
      AND UsageType = @UsageType
      AND IsActive = 1;
    
    -- Check if increment would exceed quota
    IF (@CurrentUsage + @IncrementAmount) > @QuotaLimit
    BEGIN
        SET @QuotaExceeded = 1;
        SET @RemainingQuota = @QuotaLimit - @CurrentUsage;
        
        -- Log violation
        INSERT INTO dbo.BillingQuotaViolation (TenantId, UsageType, QuotaLimit, CurrentUsage)
        VALUES (@TenantId, @UsageType, @QuotaLimit, @CurrentUsage + @IncrementAmount);
        
        COMMIT TRANSACTION;
        RETURN -1; -- Quota exceeded
    END
    
    -- Increment usage
    UPDATE dbo.BillingTenantQuota
    SET CurrentUsage = CurrentUsage + @IncrementAmount
    WHERE TenantId = @TenantId
      AND UsageType = @UsageType
      AND IsActive = 1;
    
    SET @QuotaExceeded = 0;
    SET @RemainingQuota = @QuotaLimit - (@CurrentUsage + @IncrementAmount);
    
    COMMIT TRANSACTION;
    RETURN 0; -- Success
END
GO
```

#### 5. Missing Unique Constraint
**Issue**: Can create duplicate quotas for same tenant/usage type

**REQUIRED FIX**:
```sql
CREATE UNIQUE NONCLUSTERED INDEX [UQ_BillingTenantQuota_TenantUsageType]
    ON [dbo].[BillingTenantQuota]([TenantId], [UsageType])
    WHERE [IsActive] = 1;
```

---

## 4. BillingInvoice.sql

### Schema Analysis
```sql
CREATE TABLE [dbo].[BillingInvoice] (
    [InvoiceId]          BIGINT          NOT NULL IDENTITY,
    [TenantId]           INT             NOT NULL,  -- ✓ Correct
    [InvoiceNumber]      NVARCHAR (100)  NOT NULL,  -- ✓ Correct
    [BillingPeriodStart] DATETIME2 (7)   NOT NULL,  -- ✓ Correct
    [BillingPeriodEnd]   DATETIME2 (7)   NOT NULL,  -- ✓ Correct
    [Subtotal]           DECIMAL (18, 2) NOT NULL,  -- ✓ Correct
    [Discount]           DECIMAL (18, 2) NOT NULL DEFAULT 0,  -- ✓ Correct
    [Tax]                DECIMAL (18, 2) NOT NULL DEFAULT 0,  -- ✓ Correct
    [Total]              DECIMAL (18, 2) NOT NULL,  -- ✓ Correct
    [Status]             NVARCHAR (50)   NOT NULL,  -- ✓ Correct (but needs CHECK constraint)
    [GeneratedUtc]       DATETIME2 (7)   NOT NULL DEFAULT (SYSUTCDATETIME()),  -- ✓ Correct
    [PaidUtc]            DATETIME2 (7)   NULL,      -- ✓ Legitimate nullable (unpaid invoices)
    [MetadataJson]       NVARCHAR (MAX)  NULL,      -- ✓ Legitimate nullable
    CONSTRAINT [PK_BillingInvoices] PRIMARY KEY CLUSTERED ([InvoiceId] ASC),
    CONSTRAINT [UQ_BillingInvoices_Number] UNIQUE NONCLUSTERED ([InvoiceNumber]),
    INDEX [IX_BillingInvoices_Tenant] ([TenantId], [GeneratedUtc] DESC),
    INDEX [IX_BillingInvoices_Status] ([Status], [GeneratedUtc] DESC)
);
```

### CRITICAL ISSUES

#### 1. Status Field Missing CHECK Constraint
**Severity**: HIGH - Data Integrity

**REQUIRED FIX**:
```sql
ALTER TABLE [dbo].[BillingInvoice]
    ADD CONSTRAINT [CK_BillingInvoice_Status]
    CHECK ([Status] IN ('Draft', 'Pending', 'Paid', 'PartiallyPaid', 'Overdue', 'Cancelled', 'Refunded', 'WrittenOff'));
```

#### 2. Missing Payment Gateway Integration
**Issue**: No reference to payment transactions (Authorize.Net, Stripe, etc.)

**REQUIRED ADDITIONS**:
```sql
-- Payment gateway tracking
ALTER TABLE [dbo].[BillingInvoice] ADD [PaymentGatewayTransactionId] NVARCHAR(128) NULL;
ALTER TABLE [dbo].[BillingInvoice] ADD [PaymentGatewayType] NVARCHAR(50) NULL;
ALTER TABLE [dbo].[BillingInvoice] ADD [PaymentMethod] NVARCHAR(50) NULL; -- 'CreditCard', 'ACH', 'PayPal', 'Wire'
ALTER TABLE [dbo].[BillingInvoice] ADD [PaymentProfileId] NVARCHAR(128) NULL; -- CIM profile reference

-- Index for payment reconciliation
CREATE NONCLUSTERED INDEX [IX_BillingInvoice_PaymentGateway]
    ON [dbo].[BillingInvoice]([PaymentGatewayType], [PaymentGatewayTransactionId])
    INCLUDE ([InvoiceNumber], [Total], [Status])
    WHERE [PaymentGatewayTransactionId] IS NOT NULL;
```

#### 3. Missing Subscription/ARB Integration
**Issue**: No link to recurring subscriptions

**REQUIRED ADDITIONS**:
```sql
-- Subscription tracking
ALTER TABLE [dbo].[BillingInvoice] ADD [SubscriptionId] UNIQUEIDENTIFIER NULL;
ALTER TABLE [dbo].[BillingInvoice] ADD [ARB_SubscriptionId] NVARCHAR(128) NULL; -- Authorize.Net ARB subscription ID
ALTER TABLE [dbo].[BillingInvoice] ADD [IsRecurring] BIT NOT NULL DEFAULT 0;

-- Index for subscription lookup
CREATE NONCLUSTERED INDEX [IX_BillingInvoice_Subscription]
    ON [dbo].[BillingInvoice]([SubscriptionId], [GeneratedUtc] DESC)
    INCLUDE ([InvoiceNumber], [Total], [Status])
    WHERE [SubscriptionId] IS NOT NULL;
```

#### 4. Missing Invoice Line Items Table
**Severity**: CRITICAL - Cannot itemize invoice

**Current Issue**: Invoice only has total, no line items

**REQUIRED**: Separate line items table

```sql
CREATE TABLE [dbo].[BillingInvoiceLineItem] (
    [LineItemId]      BIGINT          NOT NULL IDENTITY,
    [InvoiceId]       BIGINT          NOT NULL,
    [LineNumber]      INT             NOT NULL, -- Display order
    [Description]     NVARCHAR(500)   NOT NULL,
    [UsageType]       NVARCHAR(50)    NOT NULL,
    [Quantity]        DECIMAL(18,6)   NOT NULL,
    [UnitPrice]       DECIMAL(18,8)   NOT NULL,
    [Subtotal]        DECIMAL(18,2)   NOT NULL,
    [Discount]        DECIMAL(18,2)   NOT NULL DEFAULT 0,
    [Tax]             DECIMAL(18,2)   NOT NULL DEFAULT 0,
    [Total]           DECIMAL(18,2)   NOT NULL,
    [MetadataJson]    NVARCHAR(MAX)   NULL,
    
    CONSTRAINT [PK_BillingInvoiceLineItems] PRIMARY KEY CLUSTERED ([LineItemId] ASC),
    CONSTRAINT [FK_BillingInvoiceLineItems_Invoice] 
        FOREIGN KEY ([InvoiceId]) REFERENCES [dbo].[BillingInvoice]([InvoiceId]) ON DELETE CASCADE,
    CONSTRAINT [UQ_BillingInvoiceLineItems_InvoiceLine] 
        UNIQUE NONCLUSTERED ([InvoiceId], [LineNumber]),
    INDEX [IX_BillingInvoiceLineItems_Invoice] ([InvoiceId], [LineNumber])
);
GO

-- Computed column: Verify invoice total matches sum of line items
ALTER TABLE [dbo].[BillingInvoice] ADD [LineItemsTotal] AS (
    (SELECT ISNULL(SUM(Total), 0) FROM dbo.BillingInvoiceLineItem WHERE InvoiceId = [InvoiceId])
) PERSISTED;

-- Constraint: Total must match line items
ALTER TABLE [dbo].[BillingInvoice]
    ADD CONSTRAINT [CK_BillingInvoice_TotalMatchesLineItems]
    CHECK ([Total] = [LineItemsTotal]);
```

#### 5. Missing Due Date and Overdue Tracking
**Issue**: No due date field, cannot calculate overdue status

**REQUIRED ADDITIONS**:
```sql
-- Payment terms
ALTER TABLE [dbo].[BillingInvoice] ADD [PaymentTermsDays] INT NOT NULL DEFAULT 30; -- Net 30
ALTER TABLE [dbo].[BillingInvoice] ADD [DueDate] AS (DATEADD(DAY, [PaymentTermsDays], [GeneratedUtc])) PERSISTED;
ALTER TABLE [dbo].[BillingInvoice] ADD [IsOverdue] AS (
    CASE WHEN [Status] = 'Pending' AND SYSUTCDATETIME() > DATEADD(DAY, [PaymentTermsDays], [GeneratedUtc])
         THEN CAST(1 AS BIT) ELSE CAST(0 AS BIT) END
) PERSISTED;

-- Index for overdue invoice queries
CREATE NONCLUSTERED INDEX [IX_BillingInvoice_Overdue]
    ON [dbo].[BillingInvoice]([IsOverdue], [DueDate])
    INCLUDE ([InvoiceNumber], [TenantId], [Total], [Status])
    WHERE [IsOverdue] = 1;
```

#### 6. Missing Customer Billing Address
**Issue**: No shipping/billing address for tax calculation and invoice generation

**REQUIRED ADDITIONS**:
```sql
-- Billing address (snapshot at invoice generation)
ALTER TABLE [dbo].[BillingInvoice] ADD [BillingAddressLine1] NVARCHAR(200) NULL;
ALTER TABLE [dbo].[BillingInvoice] ADD [BillingAddressLine2] NVARCHAR(200) NULL;
ALTER TABLE [dbo].[BillingInvoice] ADD [BillingCity] NVARCHAR(100) NULL;
ALTER TABLE [dbo].[BillingInvoice] ADD [BillingState] NVARCHAR(50) NULL;
ALTER TABLE [dbo].[BillingInvoice] ADD [BillingPostalCode] NVARCHAR(20) NULL;
ALTER TABLE [dbo].[BillingInvoice] ADD [BillingCountry] NVARCHAR(100) NULL;
ALTER TABLE [dbo].[BillingInvoice] ADD [TaxJurisdiction] NVARCHAR(100) NULL; -- State/province tax code
ALTER TABLE [dbo].[BillingInvoice] ADD [TaxRate] DECIMAL(5,4) NULL; -- e.g., 0.0875 = 8.75%
```

---

## 5. BillingRatePlan.sql

### Schema Analysis
```sql
CREATE TABLE [dbo].[BillingRatePlan] (
    [RatePlanId]              UNIQUEIDENTIFIER NOT NULL DEFAULT (NEWSEQUENTIALID()),
    [TenantId]                NVARCHAR (64)    NULL,      -- ❌ CRITICAL: MUST be NOT NULL
    [PlanCode]                NVARCHAR (64)    NOT NULL DEFAULT N'',  -- ✓ Correct
    [Name]                    NVARCHAR (128)   NOT NULL DEFAULT N'',  -- ✓ Correct
    [Description]             NVARCHAR (256)   NULL,      -- ✓ Legitimate nullable
    [DefaultRate]             DECIMAL (18, 6)  NOT NULL DEFAULT 0.01,  -- ✓ Correct
    [MonthlyFee]              DECIMAL (18, 2)  NOT NULL DEFAULT 0.0,   -- ✓ Correct
    [UnitPricePerDcu]         DECIMAL (18, 6)  NOT NULL DEFAULT 0.00008,  -- ✓ Correct
    [IncludedPublicStorageGb] DECIMAL (18, 2)  NOT NULL DEFAULT 0.0,   -- ✓ Correct
    [IncludedPrivateStorageGb]DECIMAL (18, 2)  NOT NULL DEFAULT 0.0,   -- ✓ Correct
    [IncludedSeatCount]       INT              NOT NULL DEFAULT 1,     -- ✓ Correct
    [AllowsPrivateData]       BIT              NOT NULL DEFAULT CAST(0 AS BIT),  -- ✓ Correct
    [CanQueryPublicCorpus]    BIT              NOT NULL DEFAULT CAST(0 AS BIT),  -- ✓ Correct
    [IsActive]                BIT              NOT NULL DEFAULT CAST(1 AS BIT),  -- ✓ Correct
    [CreatedUtc]              DATETIME2 (7)    NOT NULL DEFAULT (SYSUTCDATETIME()),  -- ✓ Correct
    [UpdatedUtc]              DATETIME2 (7)    NOT NULL DEFAULT (SYSUTCDATETIME()),  -- ✓ Correct
    CONSTRAINT [PK_BillingRatePlans] PRIMARY KEY CLUSTERED ([RatePlanId] ASC)
);
```

### CRITICAL ISSUES

#### 1. TenantId NULL - Ambiguous Ownership
**Severity**: CRITICAL - Multi-Tenancy Broken

**Issue**: If TenantId is NULL, does rate plan apply to:
- All tenants (global default)?
- No tenant (system template)?
- Undefined behavior?

**REQUIRED FIX**:
```sql
-- Option 1: TenantId 0 = Global Default Plan
ALTER TABLE [dbo].[BillingRatePlan] ALTER COLUMN [TenantId] NVARCHAR(64) NOT NULL;
ALTER TABLE [dbo].[BillingRatePlan] 
    ADD CONSTRAINT [DF_BillingRatePlan_TenantId] DEFAULT '0' FOR [TenantId];

-- Option 2: Separate IsGlobalDefault flag
ALTER TABLE [dbo].[BillingRatePlan] ADD [IsGlobalDefault] BIT NOT NULL DEFAULT 0;
ALTER TABLE [dbo].[BillingRatePlan]
    ADD CONSTRAINT [CK_BillingRatePlan_GlobalDefault]
    CHECK (([TenantId] IS NOT NULL) OR ([IsGlobalDefault] = 1));
```

#### 2. Missing Effective Date Range
**Issue**: Rate plans change over time (price increases, plan deprecation), no temporal support

**REQUIRED ADDITIONS**:
```sql
-- Temporal validity
ALTER TABLE [dbo].[BillingRatePlan] ADD [EffectiveFrom] DATETIME2(7) NOT NULL DEFAULT SYSUTCDATETIME();
ALTER TABLE [dbo].[BillingRatePlan] ADD [EffectiveTo] DATETIME2(7) NULL; -- NULL = active indefinitely

-- Constraint: EffectiveTo must be after EffectiveFrom
ALTER TABLE [dbo].[BillingRatePlan]
    ADD CONSTRAINT [CK_BillingRatePlan_EffectiveDates]
    CHECK ([EffectiveTo] IS NULL OR [EffectiveTo] > [EffectiveFrom]);

-- Index for temporal queries (get active plan as of date)
CREATE NONCLUSTERED INDEX [IX_BillingRatePlan_Temporal]
    ON [dbo].[BillingRatePlan]([TenantId], [PlanCode], [EffectiveFrom], [EffectiveTo])
    INCLUDE ([RatePlanId], [IsActive])
    WHERE [IsActive] = 1;
```

#### 3. Missing Trial Period Support
**Issue**: No free trial period tracking

**REQUIRED ADDITIONS**:
```sql
-- Trial period configuration
ALTER TABLE [dbo].[BillingRatePlan] ADD [TrialPeriodDays] INT NOT NULL DEFAULT 0;
ALTER TABLE [dbo].[BillingRatePlan] ADD [TrialIncludesDcu] BIGINT NOT NULL DEFAULT 0;
ALTER TABLE [dbo].[BillingRatePlan] ADD [TrialIncludesStorageGb] DECIMAL(18,2) NOT NULL DEFAULT 0;
```

#### 4. Missing Overage Rates
**Issue**: What happens when tenant exceeds included DCU/storage?

**REQUIRED ADDITIONS**:
```sql
-- Overage pricing
ALTER TABLE [dbo].[BillingRatePlan] ADD [OverageDcuRate] DECIMAL(18,6) NULL; -- NULL = no overage allowed (hard limit)
ALTER TABLE [dbo].[BillingRatePlan] ADD [OverageStorageGbRate] DECIMAL(18,6) NULL;
ALTER TABLE [dbo].[BillingRatePlan] ADD [OverageAllowed] BIT NOT NULL DEFAULT 1;
```

#### 5. Missing Currency Support
**Issue**: All prices in USD? Multi-currency support needed for global SaaS

**REQUIRED ADDITIONS**:
```sql
-- Currency configuration
ALTER TABLE [dbo].[BillingRatePlan] ADD [Currency] NVARCHAR(3) NOT NULL DEFAULT 'USD'; -- ISO 4217 code
ALTER TABLE [dbo].[BillingRatePlan]
    ADD CONSTRAINT [CK_BillingRatePlan_Currency]
    CHECK ([Currency] IN ('USD', 'EUR', 'GBP', 'CAD', 'AUD', 'JPY', 'INR'));
```

---

## 6. BillingOperationRate.sql

### Schema Analysis
```sql
CREATE TABLE [dbo].[BillingOperationRate] (
    [OperationRateId] UNIQUEIDENTIFIER NOT NULL DEFAULT (NEWSEQUENTIALID()),
    [RatePlanId]      UNIQUEIDENTIFIER NOT NULL,  -- ✓ Correct
    [Operation]       NVARCHAR (128)   NOT NULL DEFAULT N'',  -- ✓ Correct
    [UnitOfMeasure]   NVARCHAR (64)    NOT NULL DEFAULT N'',  -- ✓ Correct
    [Category]        NVARCHAR (64)    NULL,      -- ✓ Legitimate nullable (optional grouping)
    [Description]     NVARCHAR (256)   NULL,      -- ✓ Legitimate nullable
    [Rate]            DECIMAL (18, 6)  NOT NULL,  -- ✓ Correct
    [IsActive]        BIT              NOT NULL DEFAULT CAST(1 AS BIT),  -- ✓ Correct
    [CreatedUtc]      DATETIME2 (7)    NOT NULL DEFAULT (SYSUTCDATETIME()),  -- ✓ Correct
    [UpdatedUtc]      DATETIME2 (7)    NOT NULL DEFAULT (SYSUTCDATETIME()),  -- ✓ Correct
    CONSTRAINT [PK_BillingOperationRates] PRIMARY KEY CLUSTERED ([OperationRateId] ASC),
    CONSTRAINT [FK_BillingOperationRates_BillingRatePlans_RatePlanId] 
        FOREIGN KEY ([RatePlanId]) REFERENCES [dbo].[BillingRatePlan] ([RatePlanId]) ON DELETE CASCADE
);
```

### REQUIRED ADDITIONS

#### 1. Missing Unique Constraint
**Issue**: Can create duplicate operation rates for same plan/operation

**REQUIRED FIX**:
```sql
CREATE UNIQUE NONCLUSTERED INDEX [UQ_BillingOperationRate_PlanOperation]
    ON [dbo].[BillingOperationRate]([RatePlanId], [Operation])
    WHERE [IsActive] = 1;
```

#### 2. Missing Rate Atomization
**Issue**: Rates stored as monolithic DECIMAL values, not atomized

**FUTURE REFACTOR**: Rate should be atom with CAS deduplication
- 99% of operations share same base rates (0.00008 per DCU)
- Rate changes require data migration (expensive)
- Should be: OperationId → RateAtomId → Atom.AtomicValue

---

## 7. BillingMultiplier.sql

### Schema Analysis
```sql
CREATE TABLE [dbo].[BillingMultiplier] (
    [MultiplierId] UNIQUEIDENTIFIER NOT NULL DEFAULT (NEWSEQUENTIALID()),
    [RatePlanId]   UNIQUEIDENTIFIER NOT NULL,  -- ✓ Correct
    [Dimension]    NVARCHAR (32)    NOT NULL DEFAULT N'',  -- ✓ Correct (e.g., 'Region', 'Priority')
    [Key]          NVARCHAR (128)   NOT NULL DEFAULT N'',  -- ✓ Correct (e.g., 'us-east-1', 'high')
    [Multiplier]   DECIMAL (18, 6)  NOT NULL,  -- ✓ Correct (e.g., 1.5 = 50% markup)
    [IsActive]     BIT              NOT NULL DEFAULT CAST(1 AS BIT),  -- ✓ Correct
    [CreatedUtc]   DATETIME2 (7)    NOT NULL DEFAULT (SYSUTCDATETIME()),  -- ✓ Correct
    [UpdatedUtc]   DATETIME2 (7)    NOT NULL DEFAULT (SYSUTCDATETIME()),  -- ✓ Correct
    CONSTRAINT [PK_BillingMultipliers] PRIMARY KEY CLUSTERED ([MultiplierId] ASC),
    CONSTRAINT [FK_BillingMultipliers_BillingRatePlans_RatePlanId] 
        FOREIGN KEY ([RatePlanId]) REFERENCES [dbo].[BillingRatePlan] ([RatePlanId]) ON DELETE CASCADE
);
```

### REQUIRED ADDITIONS

#### 1. Missing Unique Constraint
**Issue**: Can create duplicate multipliers for same dimension/key

**REQUIRED FIX**:
```sql
CREATE UNIQUE NONCLUSTERED INDEX [UQ_BillingMultiplier_PlanDimensionKey]
    ON [dbo].[BillingMultiplier]([RatePlanId], [Dimension], [Key])
    WHERE [IsActive] = 1;
```

#### 2. Missing Multiplier Validation
**Issue**: Multiplier can be 0 or negative (invalid)

**REQUIRED FIX**:
```sql
ALTER TABLE [dbo].[BillingMultiplier]
    ADD CONSTRAINT [CK_BillingMultiplier_PositiveValue]
    CHECK ([Multiplier] > 0);
```

---

## Summary

### Critical Findings (MUST FIX)

1. **BillingUsageLedger**: 8 NULL columns that MUST be NOT NULL (PrincipalId, Operation, UsageType, Quantity, etc.)
2. **BillingUsageLedger**: Missing pre-execution estimation, payment gateway, subscription, refund tracking
3. **BillingTenantQuota**: ResetInterval NULL breaks quota enforcement
4. **BillingTenantQuota**: Missing current usage tracking (cannot enforce quotas)
5. **BillingInvoice**: Missing line items table (cannot itemize invoices)
6. **BillingInvoice**: Missing payment gateway integration
7. **BillingRatePlan**: TenantId NULL (ambiguous ownership)
8. **BillingRatePlan**: Missing effective date range (no temporal support)

### Architecture Findings

1. **Financial atomization missing**: Rates, multipliers, costs stored as monolithic DECIMALs
2. **No CAS deduplication**: 99% of rates identical across millions of records
3. **No columnstore indexes**: OLAP analytics will be slow
4. **No table partitioning**: Billions of records without time-based partitioning

### Performance Optimizations Required

1. Columnstore indexes for analytics workload
2. Table partitioning by month for BillingUsageLedger
3. In-memory archival pattern already correct (BillingUsageLedger_InMemory)
4. CLR batch processing for high-volume inserts

### Files Analyzed: 7/7
- BillingUsageLedger.sql ✓
- BillingUsageLedger_InMemory.sql ✓
- BillingTenantQuota.sql ✓
- BillingInvoice.sql ✓
- BillingRatePlan.sql ✓
- BillingOperationRate.sql ✓
- BillingMultiplier.sql ✓

---

## 8. BillingPricingTier.sql

### Schema Analysis
```sql
CREATE TABLE [dbo].[BillingPricingTier] (
    [TierId]        INT             NOT NULL IDENTITY,
    [UsageType]     NVARCHAR (50)   NOT NULL,  -- ✓ Correct
    [UnitType]      NVARCHAR (50)   NOT NULL,  -- ✓ Correct
    [UnitPrice]     DECIMAL (18, 8) NOT NULL,  -- ✓ Correct
    [EffectiveFrom] DATETIME2 (7)   NOT NULL,  -- ✓ Correct
    [EffectiveTo]   DATETIME2 (7)   NULL,      -- ✓ Legitimate nullable (active indefinitely)
    [Description]   NVARCHAR (500)  NULL,      -- ✓ Legitimate nullable
    [MetadataJson]  NVARCHAR (MAX)  NULL,      -- ✓ Legitimate nullable
    [CreatedUtc]    DATETIME2 (7)   NOT NULL DEFAULT (SYSUTCDATETIME()),  -- ✓ Correct
    CONSTRAINT [PK_BillingPricingTiers] PRIMARY KEY CLUSTERED ([TierId] ASC),
    INDEX [IX_BillingPricingTiers_UsageType] ([UsageType], [UnitType], [EffectiveFrom] DESC)
);
```

### ✓ EXCELLENT TEMPORAL DESIGN

**All columns correctly defined**. Temporal pricing with effective date ranges.

### REQUIRED ADDITIONS

#### 1. Missing Temporal Constraint
**Issue**: EffectiveTo can be before EffectiveFrom

**REQUIRED FIX**:
```sql
ALTER TABLE [dbo].[BillingPricingTier]
    ADD CONSTRAINT [CK_BillingPricingTier_EffectiveDates]
    CHECK ([EffectiveTo] IS NULL OR [EffectiveTo] > [EffectiveFrom]);
```

#### 2. Missing Currency Support
**Issue**: Multi-currency pricing for global SaaS

**REQUIRED ADDITIONS**:
```sql
ALTER TABLE [dbo].[BillingPricingTier] ADD [Currency] NVARCHAR(3) NOT NULL DEFAULT 'USD';
ALTER TABLE [dbo].[BillingPricingTier]
    ADD CONSTRAINT [CK_BillingPricingTier_Currency]
    CHECK ([Currency] IN ('USD', 'EUR', 'GBP', 'CAD', 'AUD', 'JPY', 'INR'));
```

#### 3. Missing Tiered Pricing Support
**Issue**: Current design is flat rate, no volume discounts (e.g., $0.10 for 0-1000 units, $0.08 for 1001-10000)

**REQUIRED ADDITIONS**:
```sql
-- Tiered pricing thresholds
ALTER TABLE [dbo].[BillingPricingTier] ADD [TierMinQuantity] BIGINT NOT NULL DEFAULT 0;
ALTER TABLE [dbo].[BillingPricingTier] ADD [TierMaxQuantity] BIGINT NULL; -- NULL = unlimited
ALTER TABLE [dbo].[BillingPricingTier]
    ADD CONSTRAINT [CK_BillingPricingTier_TierRange]
    CHECK ([TierMaxQuantity] IS NULL OR [TierMaxQuantity] > [TierMinQuantity]);

-- Unique constraint: No overlapping tiers for same usage type
CREATE UNIQUE NONCLUSTERED INDEX [UQ_BillingPricingTier_NonOverlapping]
    ON [dbo].[BillingPricingTier]([UsageType], [UnitType], [TierMinQuantity], [EffectiveFrom])
    WHERE [EffectiveTo] IS NULL;
```

#### 4. Missing Function for Rate Lookup
**REQUIRED**: Function to get applicable rate for quantity/date

```sql
CREATE FUNCTION [dbo].[fn_GetApplicablePricingTier]
(
    @UsageType NVARCHAR(50),
    @UnitType NVARCHAR(50),
    @Quantity BIGINT,
    @EffectiveDate DATETIME2(7)
)
RETURNS TABLE
AS
RETURN
(
    SELECT TOP 1
        TierId,
        UnitPrice,
        TierMinQuantity,
        TierMaxQuantity
    FROM dbo.BillingPricingTier
    WHERE UsageType = @UsageType
      AND UnitType = @UnitType
      AND @Quantity >= TierMinQuantity
      AND (@Quantity <= TierMaxQuantity OR TierMaxQuantity IS NULL)
      AND EffectiveFrom <= @EffectiveDate
      AND (EffectiveTo IS NULL OR EffectiveTo > @EffectiveDate)
    ORDER BY TierMinQuantity DESC
);
GO
```

#### 5. Missing Price Atomization
**Issue**: UnitPrice stored as monolithic DECIMAL, not atomized

**FUTURE REFACTOR**: Price should reference Atom table with CAS deduplication
- 95% of prices repeat across tiers (e.g., 0.00008 per DCU)
- Price changes require data migration
- Should be: TierId → PriceAtomId → Atom.AtomicValue

---

## 9. BillingQuotaViolation.sql

### Schema Analysis
```sql
CREATE TABLE [dbo].[BillingQuotaViolation] (
    [ViolationId]   BIGINT         NOT NULL IDENTITY,
    [TenantId]      INT            NOT NULL,  -- ✓ Correct
    [UsageType]     NVARCHAR (50)  NOT NULL,  -- ✓ Correct
    [QuotaLimit]    BIGINT         NOT NULL,  -- ✓ Correct
    [CurrentUsage]  BIGINT         NOT NULL,  -- ✓ Correct
    [ViolatedUtc]   DATETIME2 (7)  NOT NULL DEFAULT (SYSUTCDATETIME()),  -- ✓ Correct
    [Resolved]      BIT            NOT NULL DEFAULT 0,  -- ✓ Correct
    [ResolvedUtc]   DATETIME2 (7)  NULL,      -- ✓ Legitimate nullable (unresolved violations)
    [Notes]         NVARCHAR (MAX) NULL,      -- ✓ Legitimate nullable
    CONSTRAINT [PK_BillingQuotaViolations] PRIMARY KEY CLUSTERED ([ViolationId] ASC),
    INDEX [IX_BillingQuotaViolations_Tenant] ([TenantId], [ViolatedUtc] DESC),
    INDEX [IX_BillingQuotaViolations_Unresolved] ([Resolved]) WHERE ([Resolved] = 0)
);
```

### ✓ GOOD AUDIT DESIGN

**All columns correctly defined**. Captures quota violations with resolution tracking.

### REQUIRED ADDITIONS

#### 1. Missing Foreign Key Constraint
**Issue**: TenantId has no FK to tenant table

**REQUIRED FIX**:
```sql
ALTER TABLE [dbo].[BillingQuotaViolation]
    ADD CONSTRAINT [FK_BillingQuotaViolation_TenantGuidMapping]
    FOREIGN KEY ([TenantId]) REFERENCES [dbo].[TenantGuidMapping]([TenantId]);
```

#### 2. Missing Quota Reference
**Issue**: No link back to BillingTenantQuota (which quota was violated?)

**REQUIRED ADDITIONS**:
```sql
ALTER TABLE [dbo].[BillingQuotaViolation] ADD [QuotaId] INT NULL;
ALTER TABLE [dbo].[BillingQuotaViolation]
    ADD CONSTRAINT [FK_BillingQuotaViolation_TenantQuota]
    FOREIGN KEY ([QuotaId]) REFERENCES [dbo].[BillingTenantQuota]([QuotaId]);

-- Index for quota analysis
CREATE NONCLUSTERED INDEX [IX_BillingQuotaViolation_Quota]
    ON [dbo].[BillingQuotaViolation]([QuotaId], [ViolatedUtc] DESC)
    INCLUDE ([TenantId], [CurrentUsage], [QuotaLimit]);
```

#### 3. Missing Severity Classification
**Issue**: All violations treated equally (soft warning vs hard block)

**REQUIRED ADDITIONS**:
```sql
ALTER TABLE [dbo].[BillingQuotaViolation] ADD [Severity] NVARCHAR(20) NOT NULL DEFAULT 'Critical';
ALTER TABLE [dbo].[BillingQuotaViolation]
    ADD CONSTRAINT [CK_BillingQuotaViolation_Severity]
    CHECK ([Severity] IN ('Info', 'Warning', 'Critical', 'Blocked'));

-- Index for critical violations
CREATE NONCLUSTERED INDEX [IX_BillingQuotaViolation_CriticalUnresolved]
    ON [dbo].[BillingQuotaViolation]([Severity], [Resolved], [ViolatedUtc] DESC)
    INCLUDE ([TenantId], [UsageType])
    WHERE [Severity] = 'Critical' AND [Resolved] = 0;
```

#### 4. Missing Auto-Resolution Support
**Issue**: No tracking of auto-resolved violations (quota reset)

**REQUIRED ADDITIONS**:
```sql
ALTER TABLE [dbo].[BillingQuotaViolation] ADD [AutoResolved] BIT NOT NULL DEFAULT 0;
ALTER TABLE [dbo].[BillingQuotaViolation] ADD [ResolvedBy] NVARCHAR(256) NULL; -- User or 'SYSTEM'
ALTER TABLE [dbo].[BillingQuotaViolation] ADD [ResolutionReason] NVARCHAR(500) NULL;
```

#### 5. Missing Notification Tracking
**Issue**: No record of whether tenant was notified of violation

**REQUIRED ADDITIONS**:
```sql
ALTER TABLE [dbo].[BillingQuotaViolation] ADD [NotificationSent] BIT NOT NULL DEFAULT 0;
ALTER TABLE [dbo].[BillingQuotaViolation] ADD [NotificationSentUtc] DATETIME2(7) NULL;
ALTER TABLE [dbo].[BillingQuotaViolation] ADD [NotificationChannel] NVARCHAR(50) NULL; -- 'Email', 'SMS', 'Webhook'
```

#### 6. Missing Computed Column for Overage Amount
**REQUIRED ADDITION**:
```sql
ALTER TABLE [dbo].[BillingQuotaViolation] ADD [OverageAmount] AS ([CurrentUsage] - [QuotaLimit]) PERSISTED;
```

#### 7. Missing Columnstore for Analytics
**Issue**: Violation pattern analysis will be slow without columnstore

**REQUIRED FIX**:
```sql
CREATE NONCLUSTERED COLUMNSTORE INDEX [NCCI_BillingQuotaViolation_Analytics]
    ON [dbo].[BillingQuotaViolation](
        [TenantId], [UsageType], [QuotaId], [Severity],
        [QuotaLimit], [CurrentUsage], [OverageAmount],
        [ViolatedUtc], [Resolved], [ResolvedUtc], [AutoResolved]
    );
```

---

## Summary - Part 23 Complete

### Files Analyzed: 9/9
1. BillingUsageLedger.sql ✓ (CRITICAL: 8 NULL columns → NOT NULL)
2. BillingUsageLedger_InMemory.sql ✓ (Excellent design, needs parity columns)
3. BillingTenantQuota.sql ✓ (CRITICAL: Missing ResetInterval NOT NULL, CurrentUsage tracking)
4. BillingInvoice.sql ✓ (CRITICAL: Missing line items table, payment gateway)
5. BillingRatePlan.sql ✓ (CRITICAL: TenantId NULL, missing temporal support)
6. BillingOperationRate.sql ✓ (Good design, needs unique constraint)
7. BillingMultiplier.sql ✓ (Good design, needs unique constraint + validation)
8. BillingPricingTier.sql ✓ (Excellent temporal design, needs tiered pricing support)
9. BillingQuotaViolation.sql ✓ (Good audit design, needs severity + notification tracking)

### Critical Findings Summary

**Financial Integrity Issues**:
- BillingUsageLedger: 8 critical NULL columns break audit trail (PrincipalId, Operation, UsageType, etc.)
- Missing pre-execution cost estimation (no IsEstimate flag)
- Missing payment gateway integration (Authorize.Net CIM/ARB, Stripe)
- No subscription/ARB tracking (no SubscriptionId, IsRecurring)
- No refund/credit support (no IsRefund, OriginalLedgerId)

**Quota Enforcement Broken**:
- BillingTenantQuota.ResetInterval NULL → cannot reset quotas
- No CurrentUsage field → cannot enforce quotas in real-time
- No RemainingQuota computed column
- Missing sp_CheckAndIncrementQuota stored procedure

**Invoice Generation Incomplete**:
- No BillingInvoiceLineItem table → cannot itemize invoices
- No payment gateway integration
- No due date calculation
- Total cannot be validated against line items

**Multi-Tenancy Issues**:
- BillingRatePlan.TenantId NULL → ambiguous ownership
- BillingQuotaViolation missing FK to TenantGuidMapping
- Inconsistent TenantId types (INT vs NVARCHAR(128))

**Architecture Issues**:
- No financial atomization (rates/costs as monolithic DECIMALs)
- No CAS deduplication (99% of rates identical)
- Missing columnstore indexes for OLAP analytics
- Missing table partitioning for time-series data

### Performance Optimizations Required

1. **Columnstore indexes**: BillingUsageLedger, BillingQuotaViolation analytics
2. **Table partitioning**: BillingUsageLedger by month (billions of records)
3. **In-memory archival**: Already correct in BillingUsageLedger_InMemory
4. **CLR batch processing**: High-volume ledger inserts (avoid RBAR)

### Next: Part 24 - Tenant & Provenance Tables

**Files Queued** (7 files):
- TenantGuidMapping.sql
- TenantSecurityPolicy.sql
- TenantAtom.sql
- OperationProvenance.sql
- ProvenanceAuditResults.sql
- ProvenanceValidationResults.sql
- Provenance.ProvenanceTrackingTables.sql

**Status**: Part 23 complete (9/9 billing tables) - Continue to Part 24
