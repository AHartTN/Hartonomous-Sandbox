-- =============================================
-- Migrate dbo.BillingUsageLedger to SQL Ledger (Append-Only)
-- Provides tamper-evidence with blockchain-style cryptographic verification
-- =============================================

-- STEP 1: Create new ledger table with identical schema
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'BillingUsageLedger_New' AND schema_id = SCHEMA_ID('dbo'))
BEGIN
    CREATE TABLE dbo.BillingUsageLedger_New
    (
        LedgerId BIGINT IDENTITY(1,1) NOT NULL,
        TenantId NVARCHAR(128) NOT NULL,
        PrincipalId NVARCHAR(256) NOT NULL,
        Operation NVARCHAR(128) NOT NULL,
        MessageType NVARCHAR(128) NULL,
        Handler NVARCHAR(256) NULL,
        Units DECIMAL(18,6) NOT NULL,
        BaseRate DECIMAL(18,6) NOT NULL,
        Multiplier DECIMAL(18,6) NOT NULL DEFAULT 1.0,
        TotalCost DECIMAL(18,6) NOT NULL,
        MetadataJson NVARCHAR(MAX) NULL,
        TimestampUtc DATETIME2(7) NOT NULL DEFAULT SYSUTCDATETIME(),
        
        CONSTRAINT PK_BillingUsageLedger_New PRIMARY KEY CLUSTERED (LedgerId)
    )
    WITH (LEDGER = ON (APPEND_ONLY = ON));

    -- Index for tenant queries (most common access pattern)
    CREATE NONCLUSTERED INDEX IX_BillingUsageLedger_New_TenantId_Timestamp
        ON dbo.BillingUsageLedger_New(TenantId, TimestampUtc DESC)
        INCLUDE (Operation, TotalCost);

    -- Index for operation analytics
    CREATE NONCLUSTERED INDEX IX_BillingUsageLedger_New_Operation_Timestamp
        ON dbo.BillingUsageLedger_New(Operation, TimestampUtc DESC)
        INCLUDE (TenantId, Units, TotalCost);

END

-- STEP 2: Migrate existing data (if BillingUsageLedger exists)
IF EXISTS (SELECT 1 FROM sys.tables WHERE name = 'BillingUsageLedger' AND schema_id = SCHEMA_ID('dbo'))
BEGIN
    -- Disable identity insert to preserve LedgerId values
    SET IDENTITY_INSERT dbo.BillingUsageLedger_New ON;

    INSERT INTO dbo.BillingUsageLedger_New 
        (LedgerId, TenantId, PrincipalId, Operation, MessageType, Handler, 
         Units, BaseRate, Multiplier, TotalCost, MetadataJson, TimestampUtc)
    SELECT 
        LedgerId, TenantId, PrincipalId, Operation, MessageType, Handler,
        Units, BaseRate, Multiplier, TotalCost, MetadataJson, TimestampUtc
    FROM dbo.BillingUsageLedger
    ORDER BY LedgerId;

    SET IDENTITY_INSERT dbo.BillingUsageLedger_New OFF;

    PRINT 'Migrated ' + CAST(@@ROWCOUNT AS NVARCHAR(20)) + ' rows to ledger table';
END

-- STEP 3: Rename tables (swap old and new)
-- Run this manually after validating data migration:
-- sp_rename 'dbo.BillingUsageLedger', 'BillingUsageLedger_Old';
-- sp_rename 'dbo.BillingUsageLedger_New', 'BillingUsageLedger';
-- DROP TABLE dbo.BillingUsageLedger_Old;

-- STEP 4: Configure automatic digest storage (run after table swap)
/*
-- Configure Azure Blob Storage for ledger digest storage
-- Replace <storage_account_name>, <container_name>, and <sas_token> with actual values
ALTER DATABASE SCOPED CONFIGURATION 
SET LEDGER_DIGEST_STORAGE_ENDPOINT = 'https://<storage_account_name>.blob.core.windows.net/<container_name>';

-- Generate and upload database ledger digest (monthly schedule recommended)
EXECUTE sp_generate_database_ledger_digest;

-- Verify ledger integrity from stored digests
EXECUTE sp_verify_database_ledger_from_digest_storage;
*/

-- STEP 5: Query ledger history (example)
/*
SELECT 
    LedgerId,
    TenantId,
    Operation,
    TotalCost,
    TimestampUtc,
    ledger_start_transaction_id,
    ledger_start_sequence_number,
    ledger_operation_type_desc
FROM dbo.BillingUsageLedger
WHERE TenantId = 'tenant-123'
ORDER BY ledger_start_sequence_number DESC;
*/