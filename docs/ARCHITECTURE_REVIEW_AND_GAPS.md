# Architecture Review & Critical Gaps Analysis

**Date:** November 12, 2025  
**Session Summary:** Comprehensive architectural review identifying user flows, billing gaps, and Azure Entra ID integration strategy

---

## Table of Contents

1. [Executive Summary](#executive-summary)
2. [Billing Enforcement Gap](#billing-enforcement-gap)
3. [User Flow Analysis](#user-flow-analysis)
4. [Azure Entra ID Integration](#azure-entra-id-integration)
5. [Missing Schema Components](#missing-schema-components)
6. [Implementation Priorities](#implementation-priorities)
7. [Testing Strategy](#testing-strategy)

---

## Executive Summary

### Project Status: ~65-70% to Deployable

**Core Strengths:**
- ✅ Innovative database-native AI architecture
- ✅ Sophisticated multimodal ingestion pipeline
- ✅ Comprehensive provenance tracking foundation
- ✅ Advanced CLR optimizations (66 functions)
- ✅ Multi-tenant isolation design

**Critical Blockers:**
1. **Billing schema missing** - `billing.*` tables referenced but don't exist
2. **Post-execution billing** - Usage recorded AFTER resources consumed
3. **User flows incomplete** - Most features designed but not wired end-to-end
4. **Authentication deferred** - Waiting for Azure Entra ID setup
5. **Neo4j integration stubbed** - Graph capabilities not operational

**Recommendation:** Focus on billing enforcement + Entra integration before expanding features.

---

## Billing Enforcement Gap

### Current State: Reactive Tracking (POST-EXECUTION)

**What Exists:**
```csharp
// BillingController.RecordUsageAsync()
POST /api/billing/usage/record
{
    "tenantId": 1,
    "usageType": "EmbeddingGeneration",
    "quantity": 1000,
    "unitType": "Tokens"
}
↓
sp_RecordUsage() → Records usage → Checks quota → Throws if exceeded
```

**The Problem:**
- ✅ Records what happened
- ✅ Throws error if quota exceeded
- ❌ **Resources already consumed** (money spent!)
- ❌ **No pre-flight authorization**
- ❌ **No reservation/reconciliation pattern**

### What's Missing: Proactive Enforcement (PRE-EXECUTION)

**Flow Needed:**
```sql
-- 1. Estimate cost BEFORE doing work
DECLARE @EstimatedDCUs DECIMAL(18,6);
EXEC @EstimatedDCUs = dbo.fn_EstimateCost 
    @OperationType = 'EmbeddingGeneration',
    @Quantity = 1000;

-- 2. Check if tenant can afford it
DECLARE @Authorized BIT;
EXEC dbo.sp_CheckTenantQuota 
    @TenantId = @TenantId,
    @EstimatedDCUs = @EstimatedDCUs,
    @Authorized = @Authorized OUTPUT;

IF @Authorized = 0
BEGIN
    THROW 50001, 'Insufficient quota. Please upgrade.', 1;
END;

-- 3. Reserve the quota (pessimistic lock)
EXEC dbo.sp_ReserveDCUs 
    @TenantId = @TenantId,
    @DCUs = @EstimatedDCUs;

-- 4. DO THE ACTUAL WORK
DECLARE @EmbeddingVector VECTOR(1998);
SELECT @EmbeddingVector = dbo.fn_ComputeEmbedding(...);

-- 5. Calculate actual cost
DECLARE @ActualDCUs DECIMAL(18,6);
SELECT @ActualDCUs = dbo.fn_CalculateActualCost(...);

-- 6. Record usage and reconcile reservation
EXEC dbo.sp_RecordUsageAndReconcile
    @TenantId = @TenantId,
    @ReservedDCUs = @EstimatedDCUs,
    @ActualDCUs = @ActualDCUs;
```

### Critical Discovery: Schema Tables Don't Exist

**Referenced but Missing:**
- `billing.UsageLedger` - ❌ Not in DACPAC
- `billing.TenantQuotas` - ❌ Not in DACPAC
- `billing.QuotaViolations` - ❌ Not in DACPAC
- `billing.PricingTiers` - ❌ Not in DACPAC
- `billing.TenantCreditBalance` - ❌ Not in DACPAC (needed for prepaid)

**Impact:** API will fail with "Invalid object name 'billing.UsageLedger'" on first usage record.

---

## User Flow Analysis

### Flow 1: Text Embedding & Search (80% Complete)

**User Journey:**
```
POST /api/embeddings/text → Generate embedding → Auto-ingest as atom → Return AtomId
POST /api/search/semantic → Query with text/embedding → Hybrid search → Return results
```

**What Works:**
- ✅ `EmbeddingsController.CreateTextEmbeddingAsync()` - generates embeddings
- ✅ `SearchController.SemanticSearchAsync()` - vector similarity search
- ✅ `sp_SemanticSearch` - hybrid spatial/vector ranking
- ✅ Auto-ingestion via `IAtomIngestionService`

**What's Broken:**
- ❌ No pre-flight billing check
- ❌ THREE different implementations of `IAtomIngestionService` (which one runs?)
- ❌ `billing.*` tables don't exist
- ⚠️ Embedding model selection unclear

**Priority:** **HIGH** - Core revenue-generating flow

---

### Flow 2: Multimodal Ingestion (60% Complete)

**User Journey (CLI):**
```bash
hartonomous ingest --path ./data --modality text --strategy sentence
→ MultimodalIngestionOrchestrator
→ ContentReader (detect type)
→ Atomizer (strategy-specific)
→ Quality validation
→ Batch ingestion
```

**What Works:**
- ✅ `MultimodalIngestionOrchestrator` - sophisticated pipeline
- ✅ Strategy pattern for atomizers (sentence, paragraph, scene, keyframe)
- ✅ Quality validation with configurable thresholds
- ✅ Batch processing with progress tracking
- ✅ CLI command (`IngestCommand`)

**What's Missing:**
- ❌ No API endpoint (CLI only)
- ❌ `sp_AtomizeVideo`, `sp_AtomizeText`, `sp_AtomizeAudio` - "not yet implemented"
- ❌ Neo4j sync integration not wired
- ⚠️ Model atomization only (`sp_AtomizeModel` exists, others stubbed)

**Priority:** **MEDIUM** - Advanced feature, not core flow

---

### Flow 3: Cross-Modal Search (40% Complete)

**User Journey:**
```
POST /api/search/cross-modal
→ Query text, search images/audio/video
→ Generate embedding from text
→ Search across modality types
```

**What Works:**
- ✅ API endpoint exists (`SearchController.CrossModalSearchAsync`)
- ✅ Embedding service integrated
- ✅ SQL query loops over target modalities

**What's Broken:**
- ❌ No cross-modal embedding projection (text → image different vector spaces!)
- ❌ No modality-specific ranking
- ❌ No fusion strategy (just concatenates results)
- ⚠️ Requires pre-ingested multimodal content (but ingestion incomplete)

**Priority:** **LOW** - Requires Flow 2 completion first

---

### Flow 4: Autonomous Improvement (OODA Loop) - 10% Complete

**Intended Journey:**
```
Service Broker: Observe → Analyze → Hypothesize → Act → Learn
→ Message triggers analysis
→ System proposes improvement
→ Validates via tests
→ Auto-deploys if successful
```

**What Exists:**
- ✅ Service Broker setup script
- ✅ Message types, contracts, queues defined
- ✅ `AutonomousImprovementHistory` table
- ✅ `sp_AnalyzeSystemState`, `sp_ProposeImprovement` procedures

**What's Missing:**
- ❌ Activation procedures (queues don't trigger anything)
- ❌ LLM integration (hypothesis generation stubbed)
- ❌ Test execution (validation logic not implemented)
- ❌ Deployment automation (no actual code modification)
- ❌ Feedback loop (learn phase doesn't update models)

**Status:** **Fully scaffolded, 0% operational**

**Priority:** **LOW** - Innovative but not essential for MVP

---

### Flow 5: Code Ingestion & Analysis (50% Complete)

**User Journey:**
```
Ingest codebase
→ sp_AtomizeCode (AST → GEOMETRY)
→ Store code atoms
→ Analyze complexity/dependencies
```

**What Works:**
- ✅ `sp_AtomizeCode` exists
- ✅ Code atom tables (`CodeAtoms`, `CodeDependencies`)
- ✅ CLR function `clr_GenerateCodeAstVector`

**What's Missing:**
- ❌ No API endpoint (can't ingest via REST)
- ❌ No visualization (GEOMETRY stored but not queryable)
- ❌ No semantic code search (embeddings not generated for code)
- ⚠️ Language support unclear (C#/Python/JS detected but AST parsing?)

**Priority:** **MEDIUM** - Developer tooling, niche market

---

### Flow 6: Model Layer Caching & Inference (30% Complete)

**Intended Journey:**
```
Ingest GGUF/SafeTensors model
→ sp_AtomizeModel (decompose layers)
→ Cache tensors in TensorAtomPayloads
→ Query for inference
→ Reconstruct partial model on-demand
```

**What Exists:**
- ✅ `sp_AtomizeModel` procedure
- ✅ `TensorAtomPayloads`, `TensorAtomCoefficients` tables
- ✅ CLR functions: `clr_ParseGGUFTensorCatalog`, `clr_StoreTensorAtomPayload`
- ✅ `ModelsController` with layer inspection endpoints

**What's Missing:**
- ❌ Inference execution (no `sp_RunInference` implementation)
- ❌ Model reconstruction (can decompose, can't reassemble)
- ❌ Caching strategy (which layers to keep in memory?)
- ❌ Performance validation (is this faster than loading full model?)

**Priority:** **HIGH** - **Your unique value proposition!**

---

### Flow 7: Provenance Tracking (70% Complete)

**User Journey:**
```
POST /api/provenance/lineage/{atomId}
→ Trace atom ancestry
→ Return full derivation chain
→ Include operation metadata
```

**What Works:**
- ✅ `ProvenanceController.GetLineageAsync()`
- ✅ `OperationProvenance` table with temporal tracking
- ✅ Neo4j sync architecture (though not active)

**What's Missing:**
- ❌ Graph traversal via Neo4j (queries SQL only, doesn't use graph DB)
- ❌ Regulatory compliance reports (no GDPR/audit export)
- ❌ Provenance visualization (just JSON, no UI)
- ⚠️ Dual-ledger not proven (SQL temporal + Neo4j both store, but no reconciliation)

**Priority:** **MEDIUM** - Regulatory compliance story, enterprise sales

---

### Flow 0: Onboarding & Account Setup (MISSING - BLOCKER)

**What's Missing:**
- ❌ No tenant registration endpoint
- ❌ No credit purchase flow
- ❌ No API key generation
- ❌ No plan selection (Free/Pro/Enterprise)
- ❌ No usage dashboard

**This is why you can't deploy.** Users can't even get started.

**Priority:** **CRITICAL** - Must exist before launch

---

## Azure Entra ID Integration

### Strategic Decision: Entra External ID for Auth

**Rationale:**
- Waiting for Entra setup before deployment/tests
- Internal operations use Managed Identity
- External clients use Entra External ID (B2B/B2C)

### What Entra Provides for Free

1. **✅ Authentication** - OAuth2/OIDC, no password management
2. **✅ Multi-tenant isolation** - JWT `tid` claim = tenant boundary
3. **✅ Authorization** - RBAC via app roles in JWT `roles` claim
4. **✅ B2B collaboration** - External users via guest accounts
5. **✅ B2C self-service** - User registration/login flows
6. **✅ API protection** - JWT token validation built-in

### What You Still Build

1. **❌ Usage tracking** - Entra doesn't meter API calls
2. **❌ Quota enforcement** - Entra doesn't block over-limit users
3. **❌ Credit/billing** - Entra doesn't handle payments
4. **❌ DCU calculation** - Your custom unit of measure
5. **❌ Provenance tracking** - Entra doesn't track atom lineage

### Updated Flow: Entra-Powered Onboarding

```
1. User visits portal → "Sign up"
   ↓
2. Redirect to Entra External ID
   ↓
3. User authenticates (email/Google/GitHub)
   ↓
4. Entra returns JWT with claims:
   - sub: user ID (GUID)
   - tid: Entra tenant ID (GUID)
   - roles: ["Developer"]
   - email: user@example.com
   - scp: "Embeddings.Generate Search.Query"
   ↓
5. First authenticated request to your API
   ↓
6. Middleware extracts JWT claims
   ↓
7. Check: Does mapping exist?
   SELECT InternalTenantId 
   FROM dbo.EntraTenantMappings 
   WHERE EntraTenantId = @tid
   
   IF NOT EXISTS:
       → EXEC sp_RegisterTenant(@tid, @sub, @email)
       → Create billing record
       → Assign default quota (Free tier)
       → Return InternalTenantId
   ↓
8. Proceed with InternalTenantId in request context
```

### Internal vs External Operations

**Internal Operation (Your Backend Services):**
```
Service → Managed Identity → JWT with app-only token
JWT claims:
  - tid = YOUR Entra tenant
  - oid = service principal object ID
  - roles = ["Service.ModelIngestion", "Service.OODA"]

API recognizes: IsInternal = TRUE
  → Skip billing checks
  → Full admin access
  → Usage tracked for observability only
```

**External Client (Paying Customer):**
```
User → Entra External ID → JWT with user claims
JWT claims:
  - tid = THEIR Entra tenant
  - sub = user ID
  - roles = ["Developer"]
  - scp = "Embeddings.Generate"

API recognizes: IsInternal = FALSE
  → ENFORCE billing
  → Pre-flight quota check
  → Record usage per user
  → Return DCU metadata
```

---

## Missing Schema Components

### Priority 1: Billing Schema (CRITICAL)

**Must create `billing` schema:**

```sql
CREATE SCHEMA billing;
GO

-- 1. Tenant credit balance (for prepaid/pay-as-you-go)
CREATE TABLE billing.TenantCreditBalance (
    TenantId INT NOT NULL PRIMARY KEY,
    AvailableCredits DECIMAL(18,6) NOT NULL DEFAULT 0,
    ReservedCredits DECIMAL(18,6) NOT NULL DEFAULT 0,  -- In-flight operations
    LifetimeSpent DECIMAL(18,6) NOT NULL DEFAULT 0,
    LastUpdated DATETIME2(7) NOT NULL DEFAULT SYSUTCDATETIME(),
    CONSTRAINT CHK_CreditBalance_NonNegative 
        CHECK (AvailableCredits >= 0 AND ReservedCredits >= 0),
    CONSTRAINT FK_CreditBalance_Tenants 
        FOREIGN KEY (TenantId) REFERENCES dbo.Tenants(TenantId)
);

-- 2. Quota tracking (for subscription tiers)
CREATE TABLE billing.TenantQuotas (
    TenantId INT NOT NULL,
    UsageType NVARCHAR(50) NOT NULL,
    QuotaLimit DECIMAL(18,6) NOT NULL,
    QuotaPeriod NVARCHAR(20) NOT NULL DEFAULT 'monthly',
    PeriodStartDate DATETIME2(7) NOT NULL DEFAULT SYSUTCDATETIME(),
    IsActive BIT NOT NULL DEFAULT 1,
    CreatedUtc DATETIME2(7) NOT NULL DEFAULT SYSUTCDATETIME(),
    UpdatedUtc DATETIME2(7) NOT NULL DEFAULT SYSUTCDATETIME(),
    CONSTRAINT PK_TenantQuotas PRIMARY KEY (TenantId, UsageType),
    CONSTRAINT FK_TenantQuotas_Tenants 
        FOREIGN KEY (TenantId) REFERENCES dbo.Tenants(TenantId)
);

-- 3. Real-time quota usage tracking
CREATE TABLE billing.TenantQuotaUsage (
    TenantId INT NOT NULL,
    UsageType NVARCHAR(50) NOT NULL,
    QuotaPeriodStart DATETIME2(7) NOT NULL,
    QuotaPeriodEnd DATETIME2(7) NOT NULL,
    UsedDCUs DECIMAL(18,6) NOT NULL DEFAULT 0,
    ReservedDCUs DECIMAL(18,6) NOT NULL DEFAULT 0,
    QuotaLimit DECIMAL(18,6) NOT NULL,
    PRIMARY KEY (TenantId, UsageType, QuotaPeriodStart),
    CONSTRAINT CHK_QuotaUsage_NotExceeded 
        CHECK (UsedDCUs + ReservedDCUs <= QuotaLimit * 1.1)  -- 10% burst
);

-- 4. Usage ledger (detailed transaction log)
CREATE TABLE billing.UsageLedger (
    UsageId BIGINT IDENTITY(1,1) PRIMARY KEY,
    TenantId INT NOT NULL,
    EntraUserId UNIQUEIDENTIFIER NULL,  -- NEW: per-user tracking
    UsageType NVARCHAR(50) NOT NULL,
    Quantity BIGINT NOT NULL,
    UnitType NVARCHAR(50) NOT NULL,
    CostPerUnit DECIMAL(18,8) NOT NULL,
    TotalCost DECIMAL(18,8) NOT NULL,
    DCUsConsumed DECIMAL(18,6) NOT NULL,
    Metadata NVARCHAR(MAX) NULL,
    RequestId UNIQUEIDENTIFIER NULL,  -- Correlation ID
    RecordedUtc DATETIME2(7) NOT NULL DEFAULT SYSUTCDATETIME(),
    CONSTRAINT FK_UsageLedger_Tenants 
        FOREIGN KEY (TenantId) REFERENCES dbo.Tenants(TenantId)
);

-- 5. Quota violations audit
CREATE TABLE billing.QuotaViolations (
    ViolationId BIGINT IDENTITY(1,1) PRIMARY KEY,
    TenantId INT NOT NULL,
    UsageType NVARCHAR(50) NOT NULL,
    QuotaLimit DECIMAL(18,6) NOT NULL,
    CurrentUsage DECIMAL(18,6) NOT NULL,
    ViolatedUtc DATETIME2(7) NOT NULL DEFAULT SYSUTCDATETIME(),
    CONSTRAINT FK_QuotaViolations_Tenants 
        FOREIGN KEY (TenantId) REFERENCES dbo.Tenants(TenantId)
);

-- 6. Pricing tiers (for cost calculation)
CREATE TABLE billing.PricingTiers (
    PricingTierId INT IDENTITY(1,1) PRIMARY KEY,
    UsageType NVARCHAR(50) NOT NULL,
    UnitType NVARCHAR(50) NOT NULL,
    UnitPrice DECIMAL(18,8) NOT NULL,
    TierName NVARCHAR(50) NOT NULL,
    EffectiveFrom DATETIME2(7) NOT NULL DEFAULT SYSUTCDATETIME(),
    EffectiveTo DATETIME2(7) NULL
);

-- 7. Denial audit (for rejected requests)
CREATE TABLE billing.UsageDenials (
    DenialId BIGINT IDENTITY(1,1) PRIMARY KEY,
    TenantId INT NOT NULL,
    OperationType NVARCHAR(100) NOT NULL,
    RequestedDCUs DECIMAL(18,6) NOT NULL,
    Reason NVARCHAR(500) NOT NULL,
    DeniedAt DATETIME2(7) NOT NULL DEFAULT SYSUTCDATETIME(),
    INDEX IX_Denials_Tenant_Time (TenantId, DeniedAt DESC)
);
```

### Priority 2: Entra Mapping Tables (CRITICAL)

```sql
-- 1. Map Entra tenants to internal tenant IDs
CREATE TABLE dbo.EntraTenantMappings (
    EntraTenantId UNIQUEIDENTIFIER NOT NULL PRIMARY KEY,
    InternalTenantId INT NOT NULL,
    EntraAppId UNIQUEIDENTIFIER NOT NULL,  -- Your app registration ID
    PrincipalId UNIQUEIDENTIFIER NULL,     -- Service principal (for internal ops)
    SubscriptionTier NVARCHAR(50) NOT NULL DEFAULT 'Free',
    MappedAt DATETIME2(7) NOT NULL DEFAULT SYSUTCDATETIME(),
    CONSTRAINT FK_EntraTenantMappings_Tenants 
        FOREIGN KEY (InternalTenantId) REFERENCES dbo.Tenants(TenantId),
    CONSTRAINT UQ_EntraTenantMappings_Internal 
        UNIQUE (InternalTenantId)
);

-- 2. Map Entra users to internal tenants
CREATE TABLE dbo.EntraUserMappings (
    EntraUserId UNIQUEIDENTIFIER NOT NULL,
    InternalTenantId INT NOT NULL,
    UserPrincipalName NVARCHAR(256) NOT NULL,
    DisplayName NVARCHAR(256) NULL,
    Roles NVARCHAR(MAX) NULL,  -- JSON: cached roles from JWT
    LastLoginAt DATETIME2(7) NOT NULL DEFAULT SYSUTCDATETIME(),
    CONSTRAINT PK_EntraUserMappings PRIMARY KEY (EntraUserId, InternalTenantId),
    CONSTRAINT FK_EntraUserMappings_Tenants 
        FOREIGN KEY (InternalTenantId) REFERENCES dbo.Tenants(TenantId)
);

-- 3. Update Tenants table
ALTER TABLE dbo.Tenants
ADD EntraTenantId UNIQUEIDENTIFIER NULL,
    IsInternal BIT NOT NULL DEFAULT 0;
```

### Priority 3: Billing Enforcement Procedures (CRITICAL)

```sql
-- 1. Check if tenant can afford operation
CREATE PROCEDURE billing.sp_CheckTenantQuota
    @TenantId INT,
    @EstimatedDCUs DECIMAL(18,6),
    @Authorized BIT OUTPUT
AS
BEGIN
    SET NOCOUNT ON;
    
    DECLARE @AvailableCredits DECIMAL(18,6);
    DECLARE @CurrentUsage DECIMAL(18,6);
    DECLARE @QuotaLimit DECIMAL(18,6);
    
    -- Check credit balance (for prepaid)
    SELECT @AvailableCredits = AvailableCredits
    FROM billing.TenantCreditBalance
    WHERE TenantId = @TenantId;
    
    -- Check quota (for subscription)
    SELECT @CurrentUsage = UsedDCUs + ReservedDCUs,
           @QuotaLimit = QuotaLimit
    FROM billing.TenantQuotaUsage
    WHERE TenantId = @TenantId
      AND QuotaPeriodEnd > SYSUTCDATETIME();
    
    -- Authorize if either:
    -- 1. Has available credits, OR
    -- 2. Within quota limits
    IF (@AvailableCredits >= @EstimatedDCUs) 
        OR (@CurrentUsage + @EstimatedDCUs <= @QuotaLimit)
    BEGIN
        SET @Authorized = 1;
    END
    ELSE
    BEGIN
        SET @Authorized = 0;
        
        -- Log denial
        INSERT INTO billing.UsageDenials (TenantId, OperationType, RequestedDCUs, Reason)
        VALUES (@TenantId, 'Unknown', @EstimatedDCUs, 'Insufficient quota or credits');
    END;
END;
GO

-- 2. Reserve DCUs (pessimistic lock)
CREATE PROCEDURE billing.sp_ReserveDCUs
    @TenantId INT,
    @DCUs DECIMAL(18,6)
AS
BEGIN
    SET NOCOUNT ON;
    
    -- Reserve from credit balance
    UPDATE billing.TenantCreditBalance WITH (UPDLOCK, ROWLOCK)
    SET ReservedCredits = ReservedCredits + @DCUs,
        LastUpdated = SYSUTCDATETIME()
    WHERE TenantId = @TenantId;
    
    -- Reserve from quota
    UPDATE billing.TenantQuotaUsage WITH (UPDLOCK, ROWLOCK)
    SET ReservedDCUs = ReservedDCUs + @DCUs
    WHERE TenantId = @TenantId
      AND QuotaPeriodEnd > SYSUTCDATETIME();
END;
GO

-- 3. Record actual usage and reconcile reservation
CREATE PROCEDURE billing.sp_RecordUsageAndReconcile
    @TenantId INT,
    @EntraUserId UNIQUEIDENTIFIER = NULL,
    @ReservedDCUs DECIMAL(18,6),
    @ActualDCUs DECIMAL(18,6),
    @OperationType NVARCHAR(100),
    @Quantity BIGINT,
    @UnitType NVARCHAR(50),
    @Metadata NVARCHAR(MAX) = NULL
AS
BEGIN
    SET NOCOUNT ON;
    BEGIN TRANSACTION;
    
    -- Move from reserved to actual in credit balance
    UPDATE billing.TenantCreditBalance
    SET ReservedCredits = ReservedCredits - @ReservedDCUs,
        AvailableCredits = AvailableCredits - @ActualDCUs,
        LifetimeSpent = LifetimeSpent + @ActualDCUs,
        LastUpdated = SYSUTCDATETIME()
    WHERE TenantId = @TenantId;
    
    -- Move from reserved to actual in quota
    UPDATE billing.TenantQuotaUsage
    SET ReservedDCUs = ReservedDCUs - @ReservedDCUs,
        UsedDCUs = UsedDCUs + @ActualDCUs
    WHERE TenantId = @TenantId
      AND QuotaPeriodEnd > SYSUTCDATETIME();
    
    -- Record in usage ledger
    INSERT INTO billing.UsageLedger (
        TenantId, 
        EntraUserId,
        UsageType, 
        Quantity,
        UnitType,
        DCUsConsumed, 
        Metadata
    )
    VALUES (
        @TenantId, 
        @EntraUserId,
        @OperationType, 
        @Quantity,
        @UnitType,
        @ActualDCUs, 
        @Metadata
    );
    
    COMMIT TRANSACTION;
END;
GO
```

---

## Implementation Priorities

### Phase 0: Schema Foundation (THIS WEEK)

**Estimated:** 4-6 hours

1. **Create `billing` schema and tables**
   - TenantCreditBalance
   - TenantQuotas
   - TenantQuotaUsage
   - UsageLedger
   - QuotaViolations
   - PricingTiers
   - UsageDenials

2. **Create Entra mapping tables**
   - EntraTenantMappings
   - EntraUserMappings
   - Update Tenants table

3. **Create billing enforcement procedures**
   - sp_CheckTenantQuota
   - sp_ReserveDCUs
   - sp_RecordUsageAndReconcile
   - sp_RefundReservation (rollback on error)

4. **Update existing sp_RecordUsage**
   - Add pre-flight check
   - Add reservation pattern
   - Add reconciliation

**Output:** Database can enforce billing, ready for testing

---

### Phase 1: Local Development Testing (THIS WEEK)

**Estimated:** 6-8 hours

1. **Create mock JWT middleware**
   ```csharp
   // For Development environment only
   services.AddAuthentication("MockJWT")
       .AddScheme<MockJwtAuthOptions, MockJwtAuthHandler>("MockJWT", opts => 
       {
           opts.DefaultTenantId = Guid.Parse("...");
           opts.DefaultUserId = Guid.Parse("...");
           opts.DefaultRoles = new[] { "Developer" };
       });
   ```

2. **Seed test tenant data**
   ```sql
   INSERT INTO dbo.Tenants (TenantName, EntraTenantId, IsInternal)
   VALUES ('Dev Test Tenant', '00000000-0000-0000-0000-000000000001', 0);
   
   INSERT INTO billing.TenantQuotas (TenantId, UsageType, QuotaLimit)
   VALUES (1, 'EmbeddingGeneration', 1000);
   ```

3. **Update API middleware to extract JWT claims**
   - Map `tid` to InternalTenantId
   - Track `sub` for per-user usage
   - Validate `roles` for authorization

4. **Wrap expensive operations with billing checks**
   - EmbeddingsController
   - SearchController
   - GenerationController

5. **Test billing enforcement locally**
   - Mock JWT → maps to test tenant
   - Pre-flight checks execute
   - Usage recorded
   - Quota enforced
   - Denials logged

**Output:** Can test billing logic without real Entra

---

### Phase 2: Entra Dev Tenant Setup (NEXT WEEK)

**Estimated:** 2-3 hours

1. **Create free Entra External ID tenant**
   - portal.azure.com → Entra → Create tenant
   - Choose "External ID (CIAM)"

2. **Register API app**
   - Expose API scope: `api://{app-id}/Embeddings.Generate`
   - Add app roles: `Developer`, `Premium`, `Enterprise`

3. **Register test client app**
   - Grant API permissions
   - Enable public client flow (for testing)

4. **Generate real JWT locally**
   ```bash
   # Using MSAL
   curl -X POST \
     https://login.microsoftonline.com/{tenant}/oauth2/v2.0/token \
     -d "grant_type=password" \
     -d "username=test@yourdomain.onmicrosoft.com" \
     -d "password=..." \
     -d "client_id=..." \
     -d "scope=api://{your-api-id}/Embeddings.Generate"
   ```

5. **Replace mock JWT with real Entra validation**
   ```csharp
   services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
       .AddMicrosoftIdentityWebApi(configuration, "AzureAd");
   ```

6. **Test with real Entra tokens**

**Output:** Entra integration working locally

---

### Phase 3: Core Flow Completion (2-3 WEEKS)

**Estimated:** 20-30 hours

1. **Text Embed + Search** (complete to 100%)
   - Add pre-flight billing to all endpoints
   - Fix IAtomIngestionService confusion (pick ONE)
   - Add usage metadata to responses

2. **Model Atomization + Inference** (complete to 80%)
   - Implement sp_RunInference
   - Add model reconstruction logic
   - Test caching performance
   - **This is your unique value prop!**

3. **Provenance Tracking** (complete to 90%)
   - Integrate Neo4j for graph queries
   - Add regulatory compliance exports
   - Build provenance visualization

4. **Onboarding Flow** (create from scratch)
   - POST /api/tenants/register
   - POST /api/tenants/credits (Stripe webhook)
   - GET /api/tenants/usage-dashboard

**Output:** MVP-ready system

---

### Phase 4: Azure Deployment (LATER)

**Estimated:** 1-2 weeks

1. Deploy API to Azure App Service
2. Enable Managed Identity for internal operations
3. Configure Entra app registration (production)
4. Set up billing webhook (Stripe/Azure Marketplace)
5. Deploy Neo4j (Cosmos DB or Azure Managed Instance)
6. Enable monitoring (Application Insights)
7. Set up CI/CD (GitHub Actions)

**Output:** Production deployment

---

## Testing Strategy

### Unit Tests (Before Entra Integration)

**Focus:** Billing enforcement logic

```csharp
[Fact]
public async Task CheckQuota_WithSufficientCredits_ReturnsAuthorized()
{
    // Arrange
    var tenantId = 1;
    var estimatedDCUs = 10.0m;
    
    // Mock: Tenant has 100 credits available
    await SeedTenantCredits(tenantId, 100.0m);
    
    // Act
    var authorized = await _billingService.CheckQuotaAsync(tenantId, estimatedDCUs);
    
    // Assert
    Assert.True(authorized);
}

[Fact]
public async Task CheckQuota_ExceedingLimit_ReturnsUnauthorized()
{
    // Arrange
    var tenantId = 1;
    var estimatedDCUs = 200.0m;
    
    // Mock: Tenant has 100 credit limit
    await SeedTenantQuota(tenantId, "EmbeddingGeneration", 100.0m);
    
    // Act
    var authorized = await _billingService.CheckQuotaAsync(tenantId, estimatedDCUs);
    
    // Assert
    Assert.False(authorized);
}

[Fact]
public async Task RecordUsage_ReconcilesDCUReservation()
{
    // Arrange
    var tenantId = 1;
    var reservedDCUs = 10.0m;
    var actualDCUs = 8.5m;
    
    // Act
    await _billingService.ReserveDCUsAsync(tenantId, reservedDCUs);
    await _billingService.ReconcileAsync(tenantId, reservedDCUs, actualDCUs);
    
    // Assert
    var balance = await GetTenantCreditBalance(tenantId);
    Assert.Equal(8.5m, balance.LifetimeSpent);
    Assert.Equal(0m, balance.ReservedCredits);
}
```

### Integration Tests (With Entra Dev Tenant)

**Focus:** End-to-end user flows

```csharp
[Fact]
public async Task TextEmbedding_WithValidJWT_RecordsUsageAndReturnsResult()
{
    // Arrange
    var jwt = await GetEntraJWT("test@example.com", ["Developer"]);
    var request = new EmbeddingRequest { Text = "sample query" };
    
    // Act
    var response = await _client.PostAsync(
        "/api/embeddings/text", 
        JsonContent.Create(request),
        headers: new { Authorization = $"Bearer {jwt}" });
    
    // Assert
    Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    
    var result = await response.Content.ReadFromJsonAsync<EmbeddingResponse>();
    Assert.NotNull(result.AtomId);
    Assert.True(result.DcuConsumed > 0);
    
    // Verify usage recorded in database
    var usage = await GetLatestUsage(tenantId);
    Assert.Equal("EmbeddingGeneration", usage.UsageType);
    Assert.Equal(result.DcuConsumed, usage.DCUsConsumed);
}

[Fact]
public async Task Search_ExceedingQuota_Returns402PaymentRequired()
{
    // Arrange
    var jwt = await GetEntraJWT("test@example.com", ["Developer"]);
    await SetTenantQuota(tenantId, "SemanticSearch", 0); // Zero quota
    
    var request = new SemanticSearchRequest { QueryText = "test" };
    
    // Act
    var response = await _client.PostAsync(
        "/api/search/semantic", 
        JsonContent.Create(request),
        headers: new { Authorization = $"Bearer {jwt}" });
    
    // Assert
    Assert.Equal(HttpStatusCode.PaymentRequired, response.StatusCode);
    
    var error = await response.Content.ReadFromJsonAsync<ErrorResponse>();
    Assert.Contains("quota", error.Message.ToLower());
}
```

---

## Key Architectural Insights

### Why Database-Native AI?

**Thesis:** SQL Server 2025 + CLR + VECTOR types + temporal tables = **deterministic, auditable, performant** AI runtime

**Advantages over Python/PyTorch/Microservices:**
1. **Provenance built-in** - Temporal tables track every change
2. **Compliance-ready** - Full audit trail for regulatory requirements
3. **Multi-tenant isolation** - Row-level security at database level
4. **Cost optimization** - Model layer caching reduces inference costs
5. **Deterministic execution** - No black-box model behavior

**Challenges:**
1. **Industry resistance** - "AI should be in Python" mindset
2. **Complexity** - CLR + SQL + GEOMETRY + VECTOR requires deep expertise
3. **Performance validation** - Need to prove it's faster/cheaper than alternatives
4. **Ecosystem** - Limited tooling compared to Hugging Face/PyTorch

### The Contrarian Bet

**Everyone's doing:** Stateless microservices + separate vector DB + Python inference

**You're doing:** Stateful SQL Server + integrated vector ops + CLR inference

**If you're right:** You leapfrog on cost, compliance, and auditability

**If you're wrong:** You've built unmaintainable complexity

**Key to success:** **PROVE IT WORKS** with model atomization → inference flow (Flow 6)

---

## Next Actions

1. **Immediate (This Week):**
   - ✅ Create `billing.*` schema tables
   - ✅ Create Entra mapping tables
   - ✅ Create billing enforcement procedures
   - ✅ Build mock JWT middleware for local testing

2. **Short-term (Next Week):**
   - Set up Entra dev tenant
   - Test with real JWTs locally
   - Complete Flow 1 (text embed + search) to 100%

3. **Medium-term (2-3 Weeks):**
   - Complete Flow 6 (model atomization + inference) - **YOUR UNIQUE VALUE**
   - Build onboarding flow
   - Integrate Neo4j for provenance

4. **Long-term (1-2 Months):**
   - Azure deployment
   - Production Entra setup
   - Monitoring and observability
   - Customer pilots

---

## Questions to Answer

1. **Which flow will YOU use first?** (Dogfooding reveals gaps)
2. **What's the killer demo?** (Model atomization? Provenance? Multi-modal?)
3. **Who's the first customer?** (Enterprise compliance? Developers? Researchers?)
4. **What's the pricing model?** (Per-DCU? Subscription? Hybrid?)
5. **When do you need revenue?** (Bootstrapped or funded? Timeline matters)

---

**Document Status:** Living document - update as architecture evolves  
**Last Updated:** November 12, 2025  
**Next Review:** After Phase 0 completion
