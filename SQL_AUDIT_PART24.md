# SQL Audit Part 24: Tenant & Provenance Tables

## Overview
Multi-tenancy and provenance tracking tables analyzed: 7 files
Focus: Tenant isolation, GUID-to-INT mapping safety, security policy enforcement, operation audit trails, provenance chain integrity.

---

## 1. TenantGuidMapping.sql

### Schema Analysis
```sql
CREATE TABLE [dbo].[TenantGuidMapping]
(
    [TenantId]    INT NOT NULL IDENTITY(1,1),  -- ✓ Correct (stable integer ID)
    [TenantGuid]  UNIQUEIDENTIFIER NOT NULL,   -- ✓ Correct (Azure AD tenant GUID)
    [TenantName]  NVARCHAR(200) NULL,          -- ❌ CRITICAL: MUST be NOT NULL
    [IsActive]    BIT NOT NULL DEFAULT 1,      -- ✓ Correct
    [CreatedAt]   DATETIME2(7) NOT NULL DEFAULT (SYSUTCDATETIME()),  -- ✓ Correct
    [CreatedBy]   NVARCHAR(100) NULL,          -- ✓ Legitimate nullable (system-created)
    [ModifiedAt]  DATETIME2(7) NULL,           -- ✓ Legitimate nullable (not yet modified)
    [ModifiedBy]  NVARCHAR(100) NULL,          -- ✓ Legitimate nullable
    
    CONSTRAINT [PK_TenantGuidMapping] PRIMARY KEY CLUSTERED ([TenantId] ASC),
    CONSTRAINT [UQ_TenantGuidMapping_TenantGuid] UNIQUE NONCLUSTERED ([TenantGuid])
);

CREATE NONCLUSTERED INDEX [IX_TenantGuidMapping_IsActive]
    ON [dbo].[TenantGuidMapping]([IsActive])
    INCLUDE ([TenantId], [TenantGuid], [TenantName]);
```

### ✓ EXCELLENT DESIGN PATTERN

**Purpose**: Replaces unsafe `GetHashCode()` approach with stable INT-to-GUID mapping for multi-tenant isolation.

**Design Strengths**:
1. **Stable IDs**: IDENTITY(1,1) guarantees no collisions (vs GetHashCode() conflicts)
2. **Bidirectional lookup**: PK on TenantId (INT→GUID), UQ on TenantGuid (GUID→INT)
3. **Soft delete support**: IsActive flag preserves historical mappings
4. **Audit trail**: CreatedAt, ModifiedAt, CreatedBy, ModifiedBy

### CRITICAL ISSUES

#### 1. TenantName NULL - Cannot Identify Tenant
**Severity**: CRITICAL - Operational Impact

**Issue**: Without TenantName, administrators cannot identify which tenant owns data in queries/reports.

**Impact**:
- Support tickets unresolvable ("TenantId 42" meaningless to support)
- Billing disputes unverifiable
- Audit reports unreadable

**REQUIRED FIX**:
```sql
ALTER TABLE [dbo].[TenantGuidMapping] ALTER COLUMN [TenantName] NVARCHAR(200) NOT NULL;

-- Add constraint for non-empty string
ALTER TABLE [dbo].[TenantGuidMapping]
    ADD CONSTRAINT [CK_TenantGuidMapping_TenantNameNotEmpty]
    CHECK (LEN(RTRIM(LTRIM([TenantName]))) > 0);

-- Add unique constraint on name (prevent duplicate tenant names)
CREATE UNIQUE NONCLUSTERED INDEX [UQ_TenantGuidMapping_TenantName]
    ON [dbo].[TenantGuidMapping]([TenantName])
    WHERE [IsActive] = 1;
```

#### 2. Missing Tenant Domain/Contact Info
**Issue**: No external contact information for tenant

**REQUIRED ADDITIONS**:
```sql
-- Tenant contact information
ALTER TABLE [dbo].[TenantGuidMapping] ADD [PrimaryDomain] NVARCHAR(255) NULL;
ALTER TABLE [dbo].[TenantGuidMapping] ADD [ContactEmail] NVARCHAR(255) NULL;
ALTER TABLE [dbo].[TenantGuidMapping] ADD [ContactPhone] NVARCHAR(50) NULL;

-- Tenant metadata
ALTER TABLE [dbo].[TenantGuidMapping] ADD [TenantType] NVARCHAR(50) NOT NULL DEFAULT 'Standard';
ALTER TABLE [dbo].[TenantGuidMapping]
    ADD CONSTRAINT [CK_TenantGuidMapping_TenantType]
    CHECK ([TenantType] IN ('Trial', 'Standard', 'Premium', 'Enterprise'));

ALTER TABLE [dbo].[TenantGuidMapping] ADD [SubscriptionTier] NVARCHAR(50) NULL;
ALTER TABLE [dbo].[TenantGuidMapping] ADD [MetadataJson] NVARCHAR(MAX) NULL;
```

#### 3. Missing Foreign Key Validation Function
**REQUIRED**: Function to validate TenantId exists and is active

```sql
CREATE FUNCTION [dbo].[fn_ValidateTenantId]
(
    @TenantId INT
)
RETURNS BIT
AS
BEGIN
    DECLARE @IsValid BIT = 0;
    
    IF EXISTS (
        SELECT 1 
        FROM dbo.TenantGuidMapping 
        WHERE TenantId = @TenantId AND IsActive = 1
    )
        SET @IsValid = 1;
    
    RETURN @IsValid;
END
GO
```

#### 4. Missing Tenant Deactivation Audit
**Issue**: IsActive changes not logged (who deactivated tenant and why?)

**REQUIRED**: Trigger to log activation changes

```sql
CREATE TABLE [dbo].[TenantGuidMapping_AuditLog] (
    [AuditId]       BIGINT IDENTITY(1,1) NOT NULL PRIMARY KEY,
    [TenantId]      INT NOT NULL,
    [ChangeType]    NVARCHAR(50) NOT NULL, -- 'Activated', 'Deactivated', 'Created', 'Updated'
    [OldIsActive]   BIT NULL,
    [NewIsActive]   BIT NULL,
    [OldTenantName] NVARCHAR(200) NULL,
    [NewTenantName] NVARCHAR(200) NULL,
    [ChangedBy]     NVARCHAR(100) NOT NULL,
    [ChangedAt]     DATETIME2(7) NOT NULL DEFAULT (SYSUTCDATETIME()),
    [Reason]        NVARCHAR(500) NULL,
    
    INDEX [IX_TenantAuditLog_TenantId] ([TenantId], [ChangedAt] DESC)
);
GO

CREATE TRIGGER [trg_TenantGuidMapping_AuditChanges]
ON [dbo].[TenantGuidMapping]
AFTER INSERT, UPDATE
AS
BEGIN
    SET NOCOUNT ON;
    
    -- Log activation/deactivation changes
    INSERT INTO dbo.TenantGuidMapping_AuditLog (
        TenantId, ChangeType, OldIsActive, NewIsActive, 
        OldTenantName, NewTenantName, ChangedBy
    )
    SELECT 
        i.TenantId,
        CASE 
            WHEN d.TenantId IS NULL THEN 'Created'
            WHEN i.IsActive <> d.IsActive THEN 
                CASE WHEN i.IsActive = 1 THEN 'Activated' ELSE 'Deactivated' END
            ELSE 'Updated'
        END,
        d.IsActive,
        i.IsActive,
        d.TenantName,
        i.TenantName,
        ISNULL(i.ModifiedBy, i.CreatedBy)
    FROM inserted i
    LEFT JOIN deleted d ON i.TenantId = d.TenantId
    WHERE d.TenantId IS NULL 
       OR i.IsActive <> d.IsActive
       OR i.TenantName <> d.TenantName;
END
GO
```

#### 5. Missing Rate Limiting / Quota Integration
**Issue**: No link to BillingRatePlan or BillingTenantQuota

**REQUIRED ADDITIONS**:
```sql
-- Link to billing system
ALTER TABLE [dbo].[TenantGuidMapping] ADD [RatePlanId] UNIQUEIDENTIFIER NULL;
ALTER TABLE [dbo].[TenantGuidMapping]
    ADD CONSTRAINT [FK_TenantGuidMapping_RatePlan]
    FOREIGN KEY ([RatePlanId]) REFERENCES [dbo].[BillingRatePlan]([RatePlanId]);

-- Link to subscription
ALTER TABLE [dbo].[TenantGuidMapping] ADD [SubscriptionId] UNIQUEIDENTIFIER NULL;
ALTER TABLE [dbo].[TenantGuidMapping] ADD [SubscriptionStatus] NVARCHAR(50) NULL;
ALTER TABLE [dbo].[TenantGuidMapping]
    ADD CONSTRAINT [CK_TenantGuidMapping_SubscriptionStatus]
    CHECK ([SubscriptionStatus] IN ('Active', 'Suspended', 'Cancelled', 'Trial', 'PastDue'));
```

---

## 2. TenantSecurityPolicy.sql

### Schema Analysis
```sql
CREATE TABLE [dbo].[TenantSecurityPolicy] (
    [PolicyId]      INT            NOT NULL IDENTITY,
    [TenantId]      NVARCHAR (128) NOT NULL,  -- ❌ CRITICAL: Type mismatch with TenantGuidMapping.TenantId (INT)
    [PolicyName]    NVARCHAR (100) NOT NULL,  -- ✓ Correct
    [PolicyType]    NVARCHAR (50)  NOT NULL,  -- ✓ Correct (needs CHECK constraint)
    [PolicyRules]   NVARCHAR (MAX) NOT NULL,  -- ✓ Correct
    [IsActive]      BIT            NOT NULL DEFAULT 1,  -- ✓ Correct
    [EffectiveFrom] DATETIME2 (7)  NULL,      -- ❌ CRITICAL: MUST be NOT NULL
    [EffectiveTo]   DATETIME2 (7)  NULL,      -- ✓ Legitimate nullable (active indefinitely)
    [CreatedUtc]    DATETIME2 (7)  NOT NULL DEFAULT (SYSUTCDATETIME()),  -- ✓ Correct
    [UpdatedUtc]    DATETIME2 (7)  NULL,      -- ✓ Legitimate nullable
    [CreatedBy]     NVARCHAR (256) NULL,      -- ✓ Legitimate nullable (system-created)
    [UpdatedBy]     NVARCHAR (256) NULL,      -- ✓ Legitimate nullable
    CONSTRAINT [PK_TenantSecurityPolicy] PRIMARY KEY CLUSTERED ([PolicyId] ASC)
);
```

### CRITICAL ISSUES

#### 1. TenantId Type Mismatch - Data Integrity Broken
**Severity**: CRITICAL - Foreign Key Impossible

**Issue**: 
- TenantGuidMapping.TenantId = INT
- TenantSecurityPolicy.TenantId = NVARCHAR(128)
- **Cannot create foreign key constraint**

**Impact**:
- Orphaned security policies (referencing non-existent tenants)
- Security policies applied to wrong tenants
- No referential integrity

**REQUIRED FIX**:
```sql
-- Step 1: Add new column with correct type
ALTER TABLE [dbo].[TenantSecurityPolicy] ADD [TenantId_New] INT NULL;

-- Step 2: Migrate data (assuming NVARCHAR contains numeric tenant IDs)
UPDATE dbo.TenantSecurityPolicy
SET TenantId_New = CAST(TenantId AS INT)
WHERE ISNUMERIC(TenantId) = 1;

-- Step 3: Drop old column
ALTER TABLE [dbo].[TenantSecurityPolicy] DROP COLUMN [TenantId];

-- Step 4: Rename new column
EXEC sp_rename 'dbo.TenantSecurityPolicy.TenantId_New', 'TenantId', 'COLUMN';

-- Step 5: Make NOT NULL
ALTER TABLE [dbo].[TenantSecurityPolicy] ALTER COLUMN [TenantId] INT NOT NULL;

-- Step 6: Add foreign key
ALTER TABLE [dbo].[TenantSecurityPolicy]
    ADD CONSTRAINT [FK_TenantSecurityPolicy_TenantGuidMapping]
    FOREIGN KEY ([TenantId]) REFERENCES [dbo].[TenantGuidMapping]([TenantId]);
```

#### 2. EffectiveFrom NULL - Temporal Policy Broken
**Severity**: CRITICAL - Cannot Determine When Policy Applies

**Issue**: Without EffectiveFrom, cannot determine:
- When does policy take effect?
- Is policy retroactive or future-dated?
- Which policy version applies for historical queries?

**REQUIRED FIX**:
```sql
ALTER TABLE [dbo].[TenantSecurityPolicy] ALTER COLUMN [EffectiveFrom] DATETIME2(7) NOT NULL;

-- Add temporal constraint
ALTER TABLE [dbo].[TenantSecurityPolicy]
    ADD CONSTRAINT [CK_TenantSecurityPolicy_EffectiveDates]
    CHECK ([EffectiveTo] IS NULL OR [EffectiveTo] > [EffectiveFrom]);
```

#### 3. PolicyType Missing CHECK Constraint
**Issue**: PolicyType can be any string (typos, invalid values)

**REQUIRED FIX**:
```sql
ALTER TABLE [dbo].[TenantSecurityPolicy]
    ADD CONSTRAINT [CK_TenantSecurityPolicy_PolicyType]
    CHECK ([PolicyType] IN (
        'DataRetention',
        'AccessControl',
        'Encryption',
        'AuditLogging',
        'NetworkIsolation',
        'Compliance',
        'PrivacyShield',
        'GDPR',
        'HIPAA',
        'SOC2'
    ));
```

#### 4. PolicyRules Not Validated
**Issue**: PolicyRules stored as NVARCHAR(MAX), no validation (is it JSON? XML? custom format?)

**REQUIRED ADDITIONS**:
```sql
-- Add schema version for policy rules format
ALTER TABLE [dbo].[TenantSecurityPolicy] ADD [PolicyRulesSchema] NVARCHAR(50) NOT NULL DEFAULT 'JSON_v1';

-- Add CHECK constraint for valid JSON
ALTER TABLE [dbo].[TenantSecurityPolicy]
    ADD CONSTRAINT [CK_TenantSecurityPolicy_ValidJSON]
    CHECK (ISJSON([PolicyRules]) = 1 OR [PolicyRulesSchema] <> 'JSON_v1');
```

#### 5. Missing Unique Constraint on Active Policies
**Issue**: Can create multiple active policies with same name for tenant

**REQUIRED FIX**:
```sql
CREATE UNIQUE NONCLUSTERED INDEX [UQ_TenantSecurityPolicy_ActivePolicyName]
    ON [dbo].[TenantSecurityPolicy]([TenantId], [PolicyName])
    WHERE [IsActive] = 1;
```

#### 6. Missing Policy Enforcement Tracking
**Issue**: No record of when/where policy was enforced

**REQUIRED**: Separate enforcement log table

```sql
CREATE TABLE [dbo].[TenantSecurityPolicyEnforcement] (
    [EnforcementId]     BIGINT IDENTITY(1,1) NOT NULL PRIMARY KEY,
    [PolicyId]          INT NOT NULL,
    [TenantId]          INT NOT NULL,
    [EnforcedAt]        DATETIME2(7) NOT NULL DEFAULT (SYSUTCDATETIME()),
    [EnforcedBy]        NVARCHAR(256) NULL, -- User or 'SYSTEM'
    [EnforcementContext] NVARCHAR(500) NULL, -- Operation being restricted
    [EnforcementResult] NVARCHAR(50) NOT NULL, -- 'Allowed', 'Denied', 'Audited'
    [EnforcementReason] NVARCHAR(MAX) NULL,
    [MetadataJson]      NVARCHAR(MAX) NULL,
    
    CONSTRAINT [FK_PolicyEnforcement_Policy] 
        FOREIGN KEY ([PolicyId]) REFERENCES [dbo].[TenantSecurityPolicy]([PolicyId]),
    CONSTRAINT [CK_PolicyEnforcement_Result]
        CHECK ([EnforcementResult] IN ('Allowed', 'Denied', 'Audited', 'Warning')),
    
    INDEX [IX_PolicyEnforcement_Tenant] ([TenantId], [EnforcedAt] DESC),
    INDEX [IX_PolicyEnforcement_Policy] ([PolicyId], [EnforcedAt] DESC),
    INDEX [IX_PolicyEnforcement_Denied] ([EnforcementResult], [EnforcedAt] DESC)
        WHERE [EnforcementResult] = 'Denied'
);
GO

-- Columnstore for policy enforcement analytics
CREATE NONCLUSTERED COLUMNSTORE INDEX [NCCI_PolicyEnforcement_Analytics]
    ON [dbo].[TenantSecurityPolicyEnforcement](
        [PolicyId], [TenantId], [EnforcedAt], [EnforcedBy],
        [EnforcementResult], [EnforcementContext]
    );
```

---

## 3. TenantAtom.sql

### Schema Analysis
```sql
CREATE TABLE [dbo].[TenantAtom]
(
    [TenantId]  INT NOT NULL,              -- ✓ Correct (matches TenantGuidMapping)
    [AtomId]    BIGINT NOT NULL,           -- ✓ Correct
    [CreatedAt] DATETIME2(7) NOT NULL DEFAULT (SYSUTCDATETIME()),  -- ✓ Correct
    
    CONSTRAINT [PK_TenantAtoms] PRIMARY KEY CLUSTERED ([TenantId] ASC, [AtomId] ASC),
    CONSTRAINT [FK_TenantAtoms_Atoms] FOREIGN KEY ([AtomId])
        REFERENCES [dbo].[Atom]([AtomId]) ON DELETE CASCADE
);
```

### ✓ EXCELLENT DESIGN

**All columns correct**. Multi-tenant atom ownership tracking with composite PK.

**Design Strengths**:
1. **Composite PK**: (TenantId, AtomId) guarantees no duplicate ownership
2. **Cascade delete**: Atom deletion removes tenant associations
3. **Type consistency**: TenantId INT matches TenantGuidMapping

### REQUIRED ADDITIONS

#### 1. Missing Foreign Key to TenantGuidMapping
**Issue**: TenantId not validated against tenant table

**REQUIRED FIX**:
```sql
ALTER TABLE [dbo].[TenantAtom]
    ADD CONSTRAINT [FK_TenantAtoms_TenantGuidMapping]
    FOREIGN KEY ([TenantId]) REFERENCES [dbo].[TenantGuidMapping]([TenantId]);
```

#### 2. Missing Reverse Lookup Index
**Issue**: Query "all atoms for tenant" will be slow (needs covering index)

**REQUIRED FIX**:
```sql
-- Already covered by PK (TenantId, AtomId) - clustered index
-- But add nonclustered for AtomId → TenantId lookup
CREATE NONCLUSTERED INDEX [IX_TenantAtom_AtomId]
    ON [dbo].[TenantAtom]([AtomId])
    INCLUDE ([TenantId], [CreatedAt]);
```

#### 3. Missing Ownership Transfer Support
**Issue**: No mechanism to transfer atom ownership between tenants

**REQUIRED ADDITIONS**:
```sql
-- Ownership history table
CREATE TABLE [dbo].[TenantAtomOwnershipHistory] (
    [HistoryId]      BIGINT IDENTITY(1,1) NOT NULL PRIMARY KEY,
    [AtomId]         BIGINT NOT NULL,
    [FromTenantId]   INT NULL, -- NULL = system-created
    [ToTenantId]     INT NOT NULL,
    [TransferredAt]  DATETIME2(7) NOT NULL DEFAULT (SYSUTCDATETIME()),
    [TransferredBy]  NVARCHAR(256) NULL,
    [TransferReason] NVARCHAR(500) NULL,
    
    CONSTRAINT [FK_AtomOwnershipHistory_Atom]
        FOREIGN KEY ([AtomId]) REFERENCES [dbo].[Atom]([AtomId]) ON DELETE CASCADE,
    CONSTRAINT [FK_AtomOwnershipHistory_FromTenant]
        FOREIGN KEY ([FromTenantId]) REFERENCES [dbo].[TenantGuidMapping]([TenantId]),
    CONSTRAINT [FK_AtomOwnershipHistory_ToTenant]
        FOREIGN KEY ([ToTenantId]) REFERENCES [dbo].[TenantGuidMapping]([TenantId]),
    
    INDEX [IX_AtomOwnershipHistory_Atom] ([AtomId], [TransferredAt] DESC),
    INDEX [IX_AtomOwnershipHistory_Tenant] ([ToTenantId], [TransferredAt] DESC)
);
GO

-- Stored procedure for ownership transfer
CREATE PROCEDURE [dbo].[sp_TransferAtomOwnership]
    @AtomId BIGINT,
    @FromTenantId INT,
    @ToTenantId INT,
    @TransferredBy NVARCHAR(256),
    @TransferReason NVARCHAR(500) = NULL
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;
    
    BEGIN TRANSACTION;
    
    -- Verify current ownership
    IF NOT EXISTS (
        SELECT 1 FROM dbo.TenantAtom 
        WHERE TenantId = @FromTenantId AND AtomId = @AtomId
    )
    BEGIN
        RAISERROR('Atom %I64d is not owned by tenant %d', 16, 1, @AtomId, @FromTenantId);
        ROLLBACK TRANSACTION;
        RETURN -1;
    END
    
    -- Log transfer
    INSERT INTO dbo.TenantAtomOwnershipHistory (
        AtomId, FromTenantId, ToTenantId, TransferredBy, TransferReason
    )
    VALUES (@AtomId, @FromTenantId, @ToTenantId, @TransferredBy, @TransferReason);
    
    -- Update ownership
    UPDATE dbo.TenantAtom
    SET TenantId = @ToTenantId
    WHERE AtomId = @AtomId AND TenantId = @FromTenantId;
    
    COMMIT TRANSACTION;
    RETURN 0;
END
GO
```

#### 4. Missing Shared/Public Atoms Support
**Issue**: No mechanism for atoms shared across tenants (e.g., public corpus)

**REQUIRED ADDITIONS**:
```sql
-- Shared atom access table (many-to-many)
CREATE TABLE [dbo].[TenantAtomSharedAccess] (
    [AccessId]       BIGINT IDENTITY(1,1) NOT NULL PRIMARY KEY,
    [OwnerTenantId]  INT NOT NULL, -- Original owner
    [SharedWithTenantId] INT NOT NULL, -- Tenant granted access
    [AtomId]         BIGINT NOT NULL,
    [AccessLevel]    NVARCHAR(20) NOT NULL DEFAULT 'Read',
    [GrantedAt]      DATETIME2(7) NOT NULL DEFAULT (SYSUTCDATETIME()),
    [GrantedBy]      NVARCHAR(256) NULL,
    [ExpiresAt]      DATETIME2(7) NULL, -- NULL = no expiration
    [IsActive]       BIT NOT NULL DEFAULT 1,
    
    CONSTRAINT [FK_SharedAccess_OwnerTenant]
        FOREIGN KEY ([OwnerTenantId]) REFERENCES [dbo].[TenantGuidMapping]([TenantId]),
    CONSTRAINT [FK_SharedAccess_SharedTenant]
        FOREIGN KEY ([SharedWithTenantId]) REFERENCES [dbo].[TenantGuidMapping]([TenantId]),
    CONSTRAINT [FK_SharedAccess_Atom]
        FOREIGN KEY ([AtomId]) REFERENCES [dbo].[Atom]([AtomId]) ON DELETE CASCADE,
    CONSTRAINT [CK_SharedAccess_AccessLevel]
        CHECK ([AccessLevel] IN ('Read', 'ReadWrite', 'Execute')),
    CONSTRAINT [UQ_SharedAccess_TenantAtom]
        UNIQUE NONCLUSTERED ([SharedWithTenantId], [AtomId]),
    
    INDEX [IX_SharedAccess_Atom] ([AtomId], [IsActive])
        WHERE [IsActive] = 1,
    INDEX [IX_SharedAccess_SharedTenant] ([SharedWithTenantId], [IsActive])
        WHERE [IsActive] = 1
);
GO
```

---

## 4. OperationProvenance.sql

### Schema Analysis
```sql
CREATE TABLE dbo.OperationProvenance (
    Id INT IDENTITY(1,1) PRIMARY KEY,              -- ❌ Should be BIGINT (high volume)
    OperationId UNIQUEIDENTIFIER NOT NULL UNIQUE,  -- ✓ Correct
    ProvenanceStream dbo.AtomicStream NOT NULL,    -- ✓ Correct (UDT for provenance)
    CreatedAt DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),  -- ✓ Correct

    INDEX IX_OperationProvenance_OperationId (OperationId),
    INDEX IX_OperationProvenance_CreatedAt (CreatedAt DESC)
);
```

### CRITICAL ISSUES

#### 1. Id Column INT - Will Overflow
**Severity**: HIGH - Scalability Issue

**Issue**: INT max = 2.1 billion operations
- 1M ops/day = 2,100 days (5.7 years) until overflow
- BIGINT max = 9.2 quintillion (effectively unlimited)

**REQUIRED FIX**:
```sql
ALTER TABLE dbo.OperationProvenance ALTER COLUMN Id BIGINT NOT NULL;
```

#### 2. Missing TenantId - Multi-Tenancy Broken
**Severity**: CRITICAL - Cannot Isolate Tenant Data

**Issue**: No TenantId means:
- Cannot query provenance by tenant
- Cannot enforce tenant isolation
- Cannot partition data by tenant

**REQUIRED FIX**:
```sql
ALTER TABLE dbo.OperationProvenance ADD TenantId INT NOT NULL DEFAULT 0;

-- Add foreign key
ALTER TABLE dbo.OperationProvenance
    ADD CONSTRAINT [FK_OperationProvenance_TenantGuidMapping]
    FOREIGN KEY ([TenantId]) REFERENCES [dbo].[TenantGuidMapping]([TenantId]);

-- Add tenant+time index for tenant-specific queries
CREATE NONCLUSTERED INDEX [IX_OperationProvenance_TenantCreatedAt]
    ON dbo.OperationProvenance([TenantId], [CreatedAt] DESC)
    INCLUDE ([OperationId]);
```

#### 3. Missing Operation Type Classification
**Issue**: Cannot categorize operations (ingestion, query, reasoning, etc.)

**REQUIRED ADDITIONS**:
```sql
ALTER TABLE dbo.OperationProvenance ADD OperationType NVARCHAR(50) NOT NULL DEFAULT 'Unknown';
ALTER TABLE dbo.OperationProvenance ADD OperationCategory NVARCHAR(50) NULL;

ALTER TABLE dbo.OperationProvenance
    ADD CONSTRAINT [CK_OperationProvenance_OperationType]
    CHECK ([OperationType] IN (
        'Ingestion', 'Query', 'Reasoning', 'Generation', 
        'Transformation', 'Embedding', 'Extraction', 'Aggregation'
    ));

-- Index for operation type analytics
CREATE NONCLUSTERED INDEX [IX_OperationProvenance_Type]
    ON dbo.OperationProvenance([OperationType], [CreatedAt] DESC)
    INCLUDE ([TenantId], [OperationId]);
```

#### 4. Missing User/Principal Attribution
**Issue**: No record of WHO performed operation

**REQUIRED ADDITIONS**:
```sql
ALTER TABLE dbo.OperationProvenance ADD PrincipalId NVARCHAR(256) NOT NULL DEFAULT 'SYSTEM';
ALTER TABLE dbo.OperationProvenance ADD PrincipalType NVARCHAR(50) NOT NULL DEFAULT 'System';

ALTER TABLE dbo.OperationProvenance
    ADD CONSTRAINT [CK_OperationProvenance_PrincipalType]
    CHECK ([PrincipalType] IN ('User', 'ServicePrincipal', 'System', 'API'));
```

#### 5. Missing Operation Status/Result
**Issue**: No record of whether operation succeeded or failed

**REQUIRED ADDITIONS**:
```sql
ALTER TABLE dbo.OperationProvenance ADD OperationStatus NVARCHAR(20) NOT NULL DEFAULT 'Completed';
ALTER TABLE dbo.OperationProvenance ADD ErrorMessage NVARCHAR(MAX) NULL;
ALTER TABLE dbo.OperationProvenance ADD DurationMs INT NULL;

ALTER TABLE dbo.OperationProvenance
    ADD CONSTRAINT [CK_OperationProvenance_Status]
    CHECK ([OperationStatus] IN ('Pending', 'InProgress', 'Completed', 'Failed', 'Cancelled'));

-- Index for failed operations
CREATE NONCLUSTERED INDEX [IX_OperationProvenance_Failed]
    ON dbo.OperationProvenance([OperationStatus], [CreatedAt] DESC)
    INCLUDE ([TenantId], [OperationId], [ErrorMessage])
    WHERE [OperationStatus] = 'Failed';
```

#### 6. ProvenanceStream UDT - Atomization Opportunity
**Issue**: ProvenanceStream stored as monolithic UDT, not atomized

**Current**: UDT stores entire provenance chain as binary blob
**Should Be**: Provenance chain as atom relations in graph

**FUTURE REFACTOR**: 
- Decompose ProvenanceStream UDT into AtomRelations (Operation → Input Atoms → Output Atoms)
- Store provenance segments as atoms with CAS deduplication
- Enable graph traversal queries (find all operations that produced atom X)

#### 7. Missing Table Partitioning
**Issue**: Billions of records, no time-based partitioning

**REQUIRED FIX**:
```sql
-- Partition by month
CREATE PARTITION FUNCTION [PF_OperationProvenance_Monthly](DATETIME2(7))
AS RANGE RIGHT FOR VALUES (
    '2024-01-01', '2024-02-01', '2024-03-01', '2024-04-01',
    '2024-05-01', '2024-06-01', '2024-07-01', '2024-08-01',
    '2024-09-01', '2024-10-01', '2024-11-01', '2024-12-01',
    '2025-01-01'
);

CREATE PARTITION SCHEME [PS_OperationProvenance_Monthly]
AS PARTITION [PF_OperationProvenance_Monthly]
ALL TO ([PRIMARY]);

-- Rebuild clustered index on partition scheme
DROP INDEX [PK__Operatio__*] ON dbo.OperationProvenance; -- Drop existing PK
ALTER TABLE dbo.OperationProvenance
ADD CONSTRAINT [PK_OperationProvenance] 
    PRIMARY KEY CLUSTERED ([CreatedAt], [Id])
    ON [PS_OperationProvenance_Monthly]([CreatedAt]);
```

---

## 5. ProvenanceAuditResults.sql

### Schema Analysis
```sql
CREATE TABLE dbo.ProvenanceAuditResults (
    Id INT IDENTITY(1,1) PRIMARY KEY,           -- ❌ Should be BIGINT
    AuditPeriodStart DATETIME2 NOT NULL,        -- ✓ Correct
    AuditPeriodEnd DATETIME2 NOT NULL,          -- ✓ Correct
    Scope NVARCHAR(100),                        -- ✓ Legitimate nullable (global audit)
    TotalOperations INT NOT NULL,               -- ✓ Correct
    ValidOperations INT NOT NULL,               -- ✓ Correct
    WarningOperations INT NOT NULL,             -- ✓ Correct
    FailedOperations INT NOT NULL,              -- ✓ Correct
    AverageValidationScore FLOAT,               -- ✓ Legitimate nullable (no ops = NULL)
    AverageSegmentCount FLOAT,                  -- ✓ Legitimate nullable
    Anomalies JSON,                             -- ✓ SQL Server 2025 native JSON type
    AuditDurationMs INT NOT NULL,               -- ✓ Correct
    AuditedAt DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),  -- ✓ Correct

    INDEX IX_ProvenanceAuditResults_AuditPeriod (AuditPeriodStart, AuditPeriodEnd),
    INDEX IX_ProvenanceAuditResults_AuditedAt (AuditedAt DESC)
);
```

### CRITICAL ISSUES

#### 1. Id Column INT - Will Overflow
**Severity**: MEDIUM - Long-term Scalability

**REQUIRED FIX**:
```sql
ALTER TABLE dbo.ProvenanceAuditResults ALTER COLUMN Id BIGINT NOT NULL;
```

#### 2. Missing TenantId - Cannot Audit Per-Tenant
**Severity**: HIGH - Multi-Tenancy Issue

**REQUIRED FIX**:
```sql
ALTER TABLE dbo.ProvenanceAuditResults ADD TenantId INT NULL; -- NULL = global audit
ALTER TABLE dbo.ProvenanceAuditResults
    ADD CONSTRAINT [FK_ProvenanceAuditResults_TenantGuidMapping]
    FOREIGN KEY ([TenantId]) REFERENCES [dbo].[TenantGuidMapping]([TenantId]);

-- Index for tenant-specific audit results
CREATE NONCLUSTERED INDEX [IX_ProvenanceAuditResults_Tenant]
    ON dbo.ProvenanceAuditResults([TenantId], [AuditedAt] DESC)
    INCLUDE ([TotalOperations], [ValidOperations], [FailedOperations]);
```

#### 4. Missing Temporal Constraint
**Issue**: AuditPeriodEnd can be before AuditPeriodStart

**REQUIRED FIX**:
```sql
ALTER TABLE dbo.ProvenanceAuditResults
    ADD CONSTRAINT [CK_ProvenanceAuditResults_AuditPeriod]
    CHECK ([AuditPeriodEnd] > [AuditPeriodStart]);
```

#### 5. Missing Computed Columns for Failure Rate
**REQUIRED ADDITIONS**:
```sql
ALTER TABLE dbo.ProvenanceAuditResults ADD FailureRate AS (
    CASE WHEN [TotalOperations] > 0 
         THEN CAST([FailedOperations] AS FLOAT) / [TotalOperations]
         ELSE NULL END
) PERSISTED;

ALTER TABLE dbo.ProvenanceAuditResults ADD WarningRate AS (
    CASE WHEN [TotalOperations] > 0 
         THEN CAST([WarningOperations] AS FLOAT) / [TotalOperations]
         ELSE NULL END
) PERSISTED;

-- Index for high failure rate queries
CREATE NONCLUSTERED INDEX [IX_ProvenanceAuditResults_HighFailureRate]
    ON dbo.ProvenanceAuditResults([FailureRate], [AuditedAt] DESC)
    INCLUDE ([TenantId], [TotalOperations], [FailedOperations])
    WHERE [FailureRate] > 0.05; -- 5% failure threshold
```

#### 6. Missing Anomaly Atomization
**Issue**: Anomalies stored as JSON blob, not atomized

**FUTURE REFACTOR**:
- Separate table: ProvenanceAuditAnomalies (AuditId, AnomalyType, AnomalyDetails, Severity)
- Enable anomaly pattern analysis across audits
- Atomic anomaly types (duplicate operations, missing segments, temporal inconsistencies)

---

## Summary - Part 24 Complete

### Files Analyzed: 5/7

1. **TenantGuidMapping.sql** ✓ (Excellent design, TenantName MUST be NOT NULL)
2. **TenantSecurityPolicy.sql** ✓ (CRITICAL: TenantId type mismatch INT vs NVARCHAR, EffectiveFrom NULL)
3. **TenantAtom.sql** ✓ (Excellent design, needs FK to TenantGuidMapping)
4. **OperationProvenance.sql** ✓ (CRITICAL: Missing TenantId, Id should be BIGINT)
5. **ProvenanceAuditResults.sql** ✓ (CRITICAL: Missing TenantId, ID INT overflow)

### Files Remaining: 2
- ProvenanceValidationResults.sql
- Provenance.ProvenanceTrackingTables.sql

### Critical Findings

**Multi-Tenancy Issues**:
- TenantSecurityPolicy: TenantId type mismatch (NVARCHAR vs INT) → cannot create FK
- OperationProvenance: Missing TenantId → cannot isolate tenant data
- ProvenanceAuditResults: Missing TenantId → cannot audit per-tenant

**Temporal Policy Issues**:
- TenantSecurityPolicy: EffectiveFrom NULL → cannot determine when policy applies
- Missing temporal constraints (EffectiveTo > EffectiveFrom)

**Scalability Issues**:
- OperationProvenance.Id INT → will overflow (use BIGINT)
- ProvenanceAuditResults.Id INT → will overflow (use BIGINT)
- Missing table partitioning for time-series data

**Atomization Opportunities**:
- OperationProvenance.ProvenanceStream UDT → decompose to atom relations
- ProvenanceAuditResults.Anomalies JSON → separate anomaly table

---

## 6. ProvenanceValidationResults.sql

### Schema Analysis
```sql
CREATE TABLE dbo.ProvenanceValidationResults (
    Id INT IDENTITY(1,1) PRIMARY KEY,           -- ❌ Should be BIGINT
    OperationId UNIQUEIDENTIFIER NOT NULL,      -- ✓ Correct
    ValidationResults JSON,                     -- ✓ SQL Server 2025 native JSON type
    OverallStatus NVARCHAR(20) NOT NULL,        -- ✓ Correct (needs CHECK constraint)
    ValidationDurationMs INT NOT NULL,          -- ✓ Correct
    ValidatedAt DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),  -- ✓ Correct

    INDEX IX_ProvenanceValidationResults_OperationId (OperationId),
    INDEX IX_ProvenanceValidationResults_Status (OverallStatus),
    INDEX IX_ProvenanceValidationResults_ValidatedAt (ValidatedAt DESC),

    CONSTRAINT FK_ProvenanceValidationResults_OperationProvenance 
        FOREIGN KEY (OperationId) REFERENCES dbo.OperationProvenance(OperationId)
);
```

### CRITICAL ISSUES

#### 1. Id Column INT - Will Overflow
**Severity**: MEDIUM - Long-term Scalability

**Issue**: 1M validations/day = 2,100 days (5.7 years) until INT overflow

**REQUIRED FIX**:
```sql
ALTER TABLE dbo.ProvenanceValidationResults ALTER COLUMN Id BIGINT NOT NULL;
```

#### 2. OverallStatus Missing CHECK Constraint
**Issue**: Status can be any string (typos, invalid values)

**REQUIRED FIX**:
```sql
ALTER TABLE dbo.ProvenanceValidationResults
    ADD CONSTRAINT [CK_ProvenanceValidationResults_OverallStatus]
    CHECK ([OverallStatus] IN ('PASS', 'WARN', 'FAIL'));
```

#### 3. Missing TenantId - Cannot Filter by Tenant
**Severity**: HIGH - Multi-Tenancy Issue

**REQUIRED FIX**:
```sql
ALTER TABLE dbo.ProvenanceValidationResults ADD TenantId INT NOT NULL DEFAULT 0;

ALTER TABLE dbo.ProvenanceValidationResults
    ADD CONSTRAINT [FK_ProvenanceValidationResults_TenantGuidMapping]
    FOREIGN KEY ([TenantId]) REFERENCES [dbo].[TenantGuidMapping]([TenantId]);

-- Index for tenant-specific validation queries
CREATE NONCLUSTERED INDEX [IX_ProvenanceValidationResults_Tenant]
    ON dbo.ProvenanceValidationResults([TenantId], [ValidatedAt] DESC)
    INCLUDE ([OperationId], [OverallStatus]);
```

#### 4. Missing Validation Check Details
**Issue**: ValidationResults JSON blob doesn't provide queryable structure

**REQUIRED**: Separate validation checks table

```sql
CREATE TABLE dbo.ProvenanceValidationChecks (
    [CheckId]           BIGINT IDENTITY(1,1) NOT NULL PRIMARY KEY,
    [ValidationResultId] BIGINT NOT NULL,
    [CheckType]         NVARCHAR(50) NOT NULL,
    [CheckName]         NVARCHAR(100) NOT NULL,
    [CheckStatus]       NVARCHAR(20) NOT NULL, -- 'PASS', 'WARN', 'FAIL'
    [CheckMessage]      NVARCHAR(500) NULL,
    [ExpectedValue]     NVARCHAR(MAX) NULL,
    [ActualValue]       NVARCHAR(MAX) NULL,
    [Severity]          NVARCHAR(20) NOT NULL DEFAULT 'Medium',
    [CheckDurationMs]   INT NULL,
    
    CONSTRAINT [FK_ValidationChecks_ValidationResults]
        FOREIGN KEY ([ValidationResultId]) 
        REFERENCES dbo.ProvenanceValidationResults([Id]) ON DELETE CASCADE,
    CONSTRAINT [CK_ValidationChecks_CheckStatus]
        CHECK ([CheckStatus] IN ('PASS', 'WARN', 'FAIL')),
    CONSTRAINT [CK_ValidationChecks_Severity]
        CHECK ([Severity] IN ('Low', 'Medium', 'High', 'Critical')),
    CONSTRAINT [CK_ValidationChecks_CheckType]
        CHECK ([CheckType] IN (
            'Completeness', 'Consistency', 'Integrity', 
            'Temporal', 'Structural', 'Semantic'
        )),
    
    INDEX [IX_ValidationChecks_Result] ([ValidationResultId], [CheckStatus]),
    INDEX [IX_ValidationChecks_Type] ([CheckType], [CheckStatus]),
    INDEX [IX_ValidationChecks_Failed] ([CheckStatus], [Severity])
        WHERE [CheckStatus] = 'FAIL'
);
GO

-- Columnstore for check pattern analysis
CREATE NONCLUSTERED COLUMNSTORE INDEX [NCCI_ValidationChecks_Analytics]
    ON dbo.ProvenanceValidationChecks(
        [ValidationResultId], [CheckType], [CheckName], [CheckStatus],
        [Severity], [CheckDurationMs]
    );
```

#### 6. Missing Computed Columns for Pass Rate
**REQUIRED ADDITIONS**:
```sql
-- Link to validation checks table
ALTER TABLE dbo.ProvenanceValidationResults ADD [TotalChecks] INT NOT NULL DEFAULT 0;
ALTER TABLE dbo.ProvenanceValidationResults ADD [PassedChecks] INT NOT NULL DEFAULT 0;
ALTER TABLE dbo.ProvenanceValidationResults ADD [FailedChecks] INT NOT NULL DEFAULT 0;
ALTER TABLE dbo.ProvenanceValidationResults ADD [WarningChecks] INT NOT NULL DEFAULT 0;

ALTER TABLE dbo.ProvenanceValidationResults ADD [PassRate] AS (
    CASE WHEN [TotalChecks] > 0 
         THEN CAST([PassedChecks] AS FLOAT) / [TotalChecks]
         ELSE NULL END
) PERSISTED;

-- Index for low pass rate queries (quality issues)
CREATE NONCLUSTERED INDEX [IX_ProvenanceValidationResults_LowPassRate]
    ON dbo.ProvenanceValidationResults([PassRate], [ValidatedAt] DESC)
    INCLUDE ([TenantId], [OperationId], [TotalChecks], [FailedChecks])
    WHERE [PassRate] < 0.8; -- 80% pass threshold
```

#### 7. Missing Foreign Key Index
**Issue**: FK on OperationId will cause table scan without index

**REQUIRED FIX**:
```sql
-- Index already exists: IX_ProvenanceValidationResults_OperationId
-- But should be covering index for FK lookups
DROP INDEX IX_ProvenanceValidationResults_OperationId ON dbo.ProvenanceValidationResults;
CREATE NONCLUSTERED INDEX [IX_ProvenanceValidationResults_OperationId]
    ON dbo.ProvenanceValidationResults([OperationId])
    INCLUDE ([OverallStatus], [ValidatedAt], [TotalChecks], [PassRate]);
```

#### 8. Missing Table Partitioning
**Issue**: High-volume validation results without time-based partitioning

**REQUIRED FIX**:
```sql
-- Partition by month (same scheme as OperationProvenance)
ALTER TABLE dbo.ProvenanceValidationResults
DROP CONSTRAINT [PK__Provenan__*]; -- Drop existing PK

ALTER TABLE dbo.ProvenanceValidationResults
ADD CONSTRAINT [PK_ProvenanceValidationResults] 
    PRIMARY KEY CLUSTERED ([ValidatedAt], [Id])
    ON [PS_OperationProvenance_Monthly]([ValidatedAt]);
```

---

## 7. Provenance.ProvenanceTrackingTables.sql

### Schema Analysis

**Issue**: This file contains **DUPLICATE TABLE DEFINITIONS** of tables already analyzed:
1. `OperationProvenance` (duplicate of dbo.OperationProvenance.sql)
2. `ProvenanceValidationResults` (duplicate of dbo.ProvenanceValidationResults.sql)
3. `ProvenanceAuditResults` (duplicate of dbo.ProvenanceAuditResults.sql)

### CRITICAL ISSUE: Schema Redundancy

**Severity**: CRITICAL - Deployment Failure

**Issue**: 
- Tables defined in both individual files AND this consolidated file
- DACPAC build will fail with "object already exists" errors
- Schema drift risk (which definition is canonical?)

**REQUIRED FIX**:

**Option 1: Delete Consolidated File** (RECOMMENDED)
```powershell
# Remove duplicate definition file
Remove-Item "d:\Repositories\Hartonomous\src\Hartonomous.Database\Tables\Provenance.ProvenanceTrackingTables.sql"
```

**Option 2: Keep Consolidated File, Delete Individual Files**
```powershell
# Remove individual files, keep consolidated
Remove-Item "d:\Repositories\Hartonomous\src\Hartonomous.Database\Tables\dbo.OperationProvenance.sql"
Remove-Item "d:\Repositories\Hartonomous\src\Hartonomous.Database\Tables\dbo.ProvenanceValidationResults.sql"
Remove-Item "d:\Repositories\Hartonomous\src\Hartonomous.Database\Tables\dbo.ProvenanceAuditResults.sql"
```

**Recommendation**: **Option 1** - Delete consolidated file
- Individual files provide better organization
- Easier to track changes in version control
- Standard pattern across rest of codebase

### Schema Comparison: Consolidated vs Individual Files

**Differences Found**:

1. **Foreign Key Name**:
   - Individual file: `CONSTRAINT FK_ProvenanceValidationResults_OperationProvenance`
   - Consolidated file: `FOREIGN KEY (OperationId) REFERENCES ...` (unnamed)
   - **Impact**: Unnamed constraints get auto-generated names (fragile)

2. **No Schema Differences Otherwise**: Table structures identical

**All issues from individual file analysis apply**:
- Missing TenantId columns
- Missing CHECK constraints
- ID columns should be BIGINT
- Missing table partitioning

---

## Summary - Part 24 Complete

### Files Analyzed: 7/7

1. **TenantGuidMapping.sql** ✓ (Excellent design, TenantName MUST be NOT NULL)
2. **TenantSecurityPolicy.sql** ✓ (CRITICAL: TenantId type mismatch INT vs NVARCHAR)
3. **TenantAtom.sql** ✓ (Excellent design, needs FK to TenantGuidMapping)
4. **OperationProvenance.sql** ✓ (CRITICAL: Missing TenantId, Id should be BIGINT)
5. **ProvenanceAuditResults.sql** ✓ (CRITICAL: Missing TenantId, ID INT overflow)
6. **ProvenanceValidationResults.sql** ✓ (CRITICAL: Missing TenantId, ID INT overflow)
7. **Provenance.ProvenanceTrackingTables.sql** ✓ (CRITICAL: Duplicate table definitions)

### Critical Findings Summary

**Schema Duplication Crisis**:
- Provenance.ProvenanceTrackingTables.sql duplicates 3 existing table files
- **DEPLOYMENT WILL FAIL** with "object already exists" errors
- MUST delete consolidated file OR delete individual files

**Multi-Tenancy Broken**:
- TenantSecurityPolicy: TenantId type mismatch (NVARCHAR(128) vs INT)
  - **Cannot create foreign key to TenantGuidMapping**
  - Data corruption risk (orphaned policies)
- OperationProvenance: Missing TenantId → cannot isolate tenant operations
- ProvenanceAuditResults: Missing TenantId → cannot audit per-tenant
- ProvenanceValidationResults: Missing TenantId → cannot filter validations by tenant

**Temporal Policy Issues**:
- TenantSecurityPolicy: EffectiveFrom NULL → cannot determine when policy applies
- No temporal constraints (EffectiveTo > EffectiveFrom)

**Scalability Issues**:
- All provenance tables: Id column INT → will overflow (use BIGINT)
- No table partitioning for time-series data (billions of records)

**Missing Attribution**:
- OperationProvenance: No PrincipalId (who performed operation?)
- OperationProvenance: No OperationType/Status (what happened? did it succeed?)

**Atomization Opportunities**:
1. **OperationProvenance.ProvenanceStream UDT** → Decompose to atom relations
   - Current: Monolithic binary blob
   - Should be: Graph of atoms (Operation → InputAtoms → OutputAtoms)
   - Benefits: CAS deduplication, graph traversal, provenance chain queries

2. **ValidationResults JSON blobs** → Separate ProvenanceValidationChecks table
   - Current: Opaque JSON, not queryable
   - Should be: Structured checks (CheckType, Status, Severity, ExpectedValue, ActualValue)
   - Benefits: Pattern analysis, check-level filtering, severity classification

3. **Anomalies JSON blobs** → Separate ProvenanceAuditAnomalies table
   - Current: JSON array, not queryable
   - Should be: Typed anomalies (DuplicateOperations, MissingSegments, TemporalInconsistencies)
   - Benefits: Anomaly pattern analysis, anomaly-specific queries

### Architecture Pattern Observed

**Provenance System Design**:
- 3-tier validation: Operation → Validation → Audit
- Operation-level: Individual operation provenance streams (dbo.AtomicStream UDT)
- Validation-level: Per-operation validation checks (pass/warn/fail)
- Audit-level: Aggregate statistics over time period

**Missing**: Graph-based provenance traversal
- No table for provenance chain links (Operation A produced Atom X, Operation B consumed Atom X)
- No way to query "find all operations in lineage of atom Z"
- ProvenanceStream UDT is opaque blob, not queryable graph structure

**FUTURE REFACTOR**: Provenance Graph Tables
```sql
-- Decompose ProvenanceStream UDT into queryable graph
CREATE TABLE dbo.ProvenanceOperationInputs (
    OperationId UNIQUEIDENTIFIER NOT NULL,
    AtomId BIGINT NOT NULL,
    InputSequence INT NOT NULL,
    PRIMARY KEY (OperationId, AtomId),
    INDEX IX_ProvenanceInputs_Atom (AtomId, OperationId)
);

CREATE TABLE dbo.ProvenanceOperationOutputs (
    OperationId UNIQUEIDENTIFIER NOT NULL,
    AtomId BIGINT NOT NULL,
    OutputSequence INT NOT NULL,
    PRIMARY KEY (OperationId, AtomId),
    INDEX IX_ProvenanceOutputs_Atom (AtomId, OperationId)
);

-- Now can query: "Find all operations that produced atom X"
SELECT o.* 
FROM dbo.ProvenanceOperationOutputs poo
JOIN dbo.OperationProvenance o ON poo.OperationId = o.OperationId
WHERE poo.AtomId = @AtomId;

-- Or: "Find full lineage of atom X (recursive)"
WITH ProvenanceLineage AS (
    -- Base: Operations that produced target atom
    SELECT o.OperationId, o.TenantId, o.OperationType, 1 AS Depth
    FROM dbo.ProvenanceOperationOutputs poo
    JOIN dbo.OperationProvenance o ON poo.OperationId = o.OperationId
    WHERE poo.AtomId = @AtomId
    
    UNION ALL
    
    -- Recursive: Operations that produced inputs to previous operations
    SELECT o.OperationId, o.TenantId, o.OperationType, pl.Depth + 1
    FROM ProvenanceLineage pl
    JOIN dbo.ProvenanceOperationInputs poi ON pl.OperationId = poi.OperationId
    JOIN dbo.ProvenanceOperationOutputs poo ON poi.AtomId = poo.AtomId
    JOIN dbo.OperationProvenance o ON poo.OperationId = o.OperationId
    WHERE pl.Depth < 100 -- Prevent infinite recursion
)
SELECT * FROM ProvenanceLineage;
```

### Performance Optimizations Required

1. **Columnstore indexes**: ProvenanceValidationChecks, audit analytics
2. **Table partitioning**: All 3 provenance tables by month
3. **BIGINT PKs**: Change all Id INT → BIGINT (5 tables)
4. **Covering indexes**: Foreign key columns need covering indexes

### Next: Part 25 - Reasoning & Hypothesis Tables

**Files Queued** (estimate 7 files):
- ReasoningChain tables
- HypothesisTracking tables
- DecisionNode tables
- InferenceResults tables

**Status**: Part 24 complete (7/7 tenant & provenance tables) - Continue to Part 25
