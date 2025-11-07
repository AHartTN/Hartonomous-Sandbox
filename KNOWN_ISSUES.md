# Known Issues & Remediation Plan

**Document Status:** Active
**Last Updated:** November 6, 2025
**Target Resolution:** Sprint 2025-Q1

---

## Critical Issues (Blocks Deployment)

### Issue #1: SQL Procedures Not Deployed

**Status:** 🔴 **CRITICAL**
**Impact:** Database deployment creates schema but no stored procedures exist
**Priority:** P0
**Estimated Effort:** 4 hours

**Problem:**
The deployment orchestrator (`scripts/deploy/deploy-database.ps1`) executes 7 steps but does not install SQL procedures from `sql/procedures/` directory. After deployment:
- Database created ✅
- CLR assembly deployed ✅
- Schema migrated via EF ✅
- Stored procedures: **NOT CREATED** ❌

**Files Affected:**
- All 54 procedure files in `sql/procedures/` remain unexecuted
- Core procedures unavailable: `sp_ComputeSpatialProjection`, `sp_HybridSearch`, `sp_GenerateText`, etc.
- CLR bindings not created: `Common.ClrBindings.sql`, `Functions.AggregateVectorOperations.sql`

**Remediation:**
1. Create `scripts/deploy/08-create-procedures.ps1`
2. Execute SQL files in dependency order:
   ```
   Common.ClrBindings.sql → Common.Helpers.sql → dbo.*.sql →
   Spatial.*.sql → Inference.*.sql → Generation.*.sql → Autonomy.*.sql
   ```
3. Add step 8 to `deploy-database.ps1` between EF migrations and Service Broker
4. Verify deployment: `SELECT COUNT(*) FROM sys.procedures WHERE schema_id = SCHEMA_ID('dbo')`

**Workaround:**
Manual execution:
```powershell
Get-ChildItem -Path "sql/procedures" -Filter "*.sql" |
    Sort-Object Name |
    ForEach-Object { sqlcmd -S localhost -d Hartonomous -i $_.FullName }
```

---

### Issue #2: CLR Aggregate Binding Syntax Error

**Status:** 🔴 **CRITICAL**
**Impact:** All 75+ SQL aggregates will fail at runtime
**Priority:** P0
**Estimated Effort:** 2 hours

**Problem:**
`sql/procedures/Functions.AggregateVectorOperations.sql` uses incorrect syntax for CLR aggregates.

**Current (Incorrect):**
```sql
CREATE OR ALTER FUNCTION dbo.VectorAvg (@vector VARBINARY(MAX))
RETURNS VARBINARY(MAX)
AS BEGIN
    RETURN dbo.clr_VectorAvg_Aggregate(@vector);
END;
```

**Required (Per Microsoft Docs):**
```sql
CREATE AGGREGATE dbo.VectorAvg(@vector VARBINARY(MAX))
RETURNS VARBINARY(MAX)
EXTERNAL NAME [SqlClrFunctions].[SqlClrFunctions.VectorAvgAggregate];
```

**Remediation:**
1. Rewrite `Functions.AggregateVectorOperations.sql` using `CREATE AGGREGATE` syntax
2. Map all 75+ aggregates to their CLR implementations:
   - VectorAvgAggregate
   - VectorAttentionAggregate
   - TreeOfThoughtAggregate
   - IsolationForestAggregate
   - etc.
3. Test each aggregate after deployment

**Reference:** [Microsoft Learn: CREATE AGGREGATE](https://learn.microsoft.com/en-us/sql/t-sql/statements/create-aggregate-transact-sql)

---

## High Priority Issues (Security/Compliance)

### Issue #3: Hardcoded Credentials in Deployment Files

**Status:** ⚠️ **HIGH**
**Impact:** Production credentials exposed in source control
**Priority:** P1
**Estimated Effort:** 3 hours

**Files Affected:**
- `deploy/hartonomous-api.service:7` - `User=ahart`
- `deploy/hartonomous-api.service:16` - `Environment=AZURE_CLIENT_ID=c25ed11d-...`
- `deploy/deploy-to-hart-server.ps1:15` - `$server = "ahart@192.168.1.2"`

**Remediation:**
1. Update `.service` files to use `EnvironmentFile=/etc/hartonomous/env`
2. Configure Azure Pipeline to populate environment file from Key Vault
3. Remove hardcoded IPs, use pipeline variables for connection strings
4. Implement Azure Managed Identity for Arc-enabled SQL Server authentication

**Example:**
```ini
# /etc/hartonomous/env (not in source control)
AZURE_CLIENT_ID=...
AZURE_TENANT_ID=...
SQL_CONNECTION_STRING=...
```

---

### Issue #4: Redundant Deployment Scripts

**Status:** ⚠️ **MEDIUM**
**Impact:** Maintenance confusion, potential use of outdated scripts
**Priority:** P2
**Estimated Effort:** 30 minutes

**Files to Remove:**
- `scripts/deploy-database.ps1` (old monolithic version, contains duplicated lines 1-50)
- `scripts/deployment-functions.ps1` (legacy, superseded by modular scripts)
- `deploy/deploy-to-hart-server.ps1` (manual deployment, superseded by azure-pipelines.yml)

**Retained:**
- `scripts/deploy/deploy-database.ps1` (current modular orchestrator)
- `scripts/deploy/01-prerequisites.ps1` through `07-verification.ps1`
- `azure-pipelines.yml` (CI/CD pipeline)

**Action:** Archive old scripts to `archive/deprecated-scripts/` with README explaining deprecation

---

## Code Quality Issues (Non-Blocking)

### Issue #5: Controller Exception Handling Duplication

**Status:** 📝 **MEDIUM**
**Impact:** Code maintainability, SoC violation
**Priority:** P3
**Estimated Effort:** 4 hours

**Problem:**
Controllers contain redundant exception handling that duplicates ProblemDetails middleware functionality.

**Examples:**
- `GenerationController.cs`: 4 identical catch blocks (15+ lines each)
- `AutonomyController.cs`: 6 identical catch blocks
- `BillingController.cs`: 5 identical catch blocks

**Remediation:**
Remove controller-level try/catch blocks, rely on centralized exception middleware configured in `Program.cs:107`.

**Rationale:** Microsoft ASP.NET Core best practices recommend centralized exception handling via middleware.

---

### Issue #6: SqlParameter Type Inference (AddWithValue)

**Status:** 📝 **LOW**
**Impact:** Query plan cache bloat, minor performance degradation
**Priority:** P4
**Estimated Effort:** 3 hours

**Problem:**
Some controllers use `SqlParameter.AddWithValue` which causes:
- Implicit type conversions
- Plan cache bloat (separate plans per string length)
- Minor performance overhead

**Files:**
- `GenerationController.cs:54-59`
- `BillingController.cs:76-78, 175-178`
- `AutonomyController.cs:54-56`

**Remediation:**
Use `SqlCommandExecutorExtensions` pattern already implemented in `InferenceOrchestrator.cs:387-394`.

**Example:**
```csharp
// Instead of:
command.Parameters.AddWithValue("@promptText", promptText);

// Use:
command.Parameters.Add(new SqlParameter("@promptText", SqlDbType.NVarChar, 4000) {
    Value = promptText
});
```

---

## Resolved Non-Issues (Architectural Verification)

### ✅ OODA Loop Implementation

**Status:** ✅ **COMPLETE**
All Service Broker procedures exist and functional:
- `sp_Analyze.sql` (4,944 bytes)
- `sp_Hypothesize.sql` (6,948 bytes)
- `sp_Act.sql` (11,238 bytes)
- `sp_Learn.sql` (8,715 bytes)

No action required.

---

### ✅ Spatial Projection Integration

**Status:** ✅ **COMPLETE**
`AtomIngestionService.cs:184-199` calls `ComputeSpatialProjectionAsync` on every embedding ingestion.

Both fine-grained (`SpatialGeometry`) and coarse (`SpatialCoarse`) projections stored.

No action required.

---

### ✅ CLR Vector Utilities Shared Implementation

**Status:** ✅ **COMPLETE**
All aggregate implementations use shared `VectorUtilities.cs` methods. No code duplication exists.

No action required.

---

### ✅ GGUFModelReader Architecture

**Status:** ✅ **COMPLETE**
Already implements dependency injection with `GGUFParser`, `GGUFDequantizer`, `GGUFModelBuilder`, `GGUFGeometryBuilder`.

File size: 146 LOC (strategy pattern already applied).

No action required.

---

## Remediation Timeline

| Issue | Priority | Effort | Target Sprint |
|-------|----------|--------|---------------|
| #1: Procedure Deployment | P0 | 4h | Sprint 2025-Q1 Week 1 |
| #2: CLR Aggregate Syntax | P0 | 2h | Sprint 2025-Q1 Week 1 |
| #3: Hardcoded Credentials | P1 | 3h | Sprint 2025-Q1 Week 2 |
| #4: Redundant Scripts | P2 | 0.5h | Sprint 2025-Q1 Week 2 |
| #5: Controller Exceptions | P3 | 4h | Sprint 2025-Q1 Week 3 |
| #6: AddWithValue Refactor | P4 | 3h | Sprint 2025-Q1 Week 4 |

**Total Critical Path:** 9 hours (Issues #1-3)
**Total Effort:** 16.5 hours

---

## Verification Checklist

After remediation, verify:

```sql
-- 1. Procedures deployed
SELECT COUNT(*) FROM sys.procedures WHERE schema_id = SCHEMA_ID('dbo');
-- Expected: 40+

-- 2. CLR aggregates created
SELECT COUNT(*) FROM sys.objects WHERE type = 'AF';
-- Expected: 75+

-- 3. Spatial indexes exist
SELECT COUNT(*) FROM sys.spatial_indexes;
-- Expected: 2+ (SpatialGeometry, SpatialCoarse)

-- 4. Service Broker enabled
SELECT is_broker_enabled FROM sys.databases WHERE name = 'Hartonomous';
-- Expected: 1

-- 5. CLR assembly loaded
SELECT * FROM sys.assemblies WHERE name = 'SqlClrFunctions';
-- Expected: 1 row, permission_set_desc = 'UNSAFE'
```

---

**Document Owner:** Infrastructure Team
**Review Cadence:** Weekly until all P0/P1 issues resolved
**Next Review:** November 13, 2025

