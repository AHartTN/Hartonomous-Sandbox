# Script Categorization for DACPAC Migration

**Goal**: Categorize ALL scripts in `/sql` and `/scripts` folders to determine what goes where in the DACPAC-based deployment.

## DACPAC Constraints (from Microsoft Docs)

### Post-Deployment Scripts CAN:
- Contain T-SQL only
- Be idempotent (safe to run multiple times)
- Run AFTER schema deployment completes
- Typical uses: data seeding, configuration that depends on schema, index optimization

### Post-Deployment Scripts CANNOT:
- Use `USE <database>` statements
- Use `ALTER DATABASE` statements
- Execute PowerShell or batch commands
- Modify database-level settings (Query Store, CDC enablement, FILESTREAM config)

### Schema Build Items (Standard DACPAC Content):
- CREATE TABLE, VIEW, PROCEDURE, FUNCTION, TYPE
- Service Broker: MESSAGE TYPE, CONTRACT, QUEUE, SERVICE (these ARE schema objects!)
- Must be validated against database model

### External Deployment (Cannot go in DACPAC):
- ALTER DATABASE statements
- sp_configure calls
- Database-level feature enablement (CDC, FILESTREAM instance config)
- PowerShell automation scripts

## PowerShell Scripts in `/scripts` - KEEP OUTSIDE DACPAC ✅

**Decision**: These are deployment automation helpers, NOT database objects. Stay in `/scripts`.

| File | Purpose | Action |
|------|---------|--------|
| `add-go-after-create-table.ps1` | Script transformation | Keep in `/scripts` |
| `analyze-all-dependencies.ps1` | Dependency analysis | Keep in `/scripts` |
| `analyze-dependencies.ps1` | Dependency analysis | Keep in `/scripts` |
| `clean-sql-dacpac-deep.ps1` | DACPAC cleanup | Keep in `/scripts` |
| `clean-sql-for-dacpac.ps1` | SQL cleanup | Keep in `/scripts` |
| `convert-tables-to-dacpac-format.ps1` | Table conversion | Keep in `/scripts` |
| `copy-dependencies.ps1` | Dependency management | Keep in `/scripts` |
| `dacpac-sanity-check.ps1` | DACPAC validation | Keep in `/scripts` |
| `deploy-clr-secure.ps1` | CLR deployment | Keep in `/scripts` |
| `deploy-database-unified.ps1` | Database deployment | Keep in `/scripts` |
| `deploy-local.ps1` | Local deployment | Keep in `/scripts` |
| `deployment-functions.ps1` | Deployment helpers | Keep in `/scripts` |
| `extract-tables-from-migration.ps1` | Migration helper | Keep in `/scripts` |
| `generate-table-scripts-from-efcore.ps1` | EF Core integration | Keep in `/scripts` |
| `map-all-dependencies.ps1` | Dependency mapping | Keep in `/scripts` |
| `sanitize-dacpac-tables.ps1` | Table sanitization | Keep in `/scripts` |
| `split-procedures-for-dacpac.ps1` | Procedure splitting | Keep in `/scripts` |
| `validate-package-versions.ps1` | Package validation | Keep in `/scripts` |

**Total**: 18 PowerShell scripts ✅ ALREADY IN CORRECT LOCATION

---

## SQL Scripts in `/scripts` - NEEDS CATEGORIZATION

### 🔴 DATABASE-LEVEL (Cannot go in DACPAC)

| File | Content | Category | Action |
|------|---------|----------|--------|
| `enable-cdc.sql` | `EXEC sys.sp_cdc_enable_db; EXEC sys.sp_cdc_enable_table` | Database-level feature | **Document as external deployment** |

**Reason**: CDC enablement is a database-level operation that must be done OUTSIDE DACPAC deployment.

**Recommended approach**: 
- Create deployment guide documenting manual CDC enablement steps
- Or create separate PowerShell script that runs AFTER DACPAC deployment
- Cannot be in post-deployment script (uses system procedures for database config)

### 🟢 SCHEMA OBJECTS (Should be Build items)

| File | Content | Category | Action |
|------|---------|----------|--------|
| `setup-service-broker.sql` | CREATE MESSAGE TYPE, CREATE CONTRACT, CREATE QUEUE, CREATE SERVICE | Service Broker schema | **Extract into individual schema files** |

**Microsoft Docs Confirmation**: Service Broker MESSAGE TYPE, CONTRACT, QUEUE, and SERVICE are all **database schema objects** that belong in the database model, NOT post-deployment.

**Action Required**:
1. Extract each MESSAGE TYPE into `Types/dbo.MessageTypeName.sql`
2. Extract each CONTRACT into `Contracts/dbo.ContractName.sql`
3. Extract each QUEUE into `Queues/dbo.QueueName.sql`
4. Extract each SERVICE into `Services/dbo.ServiceName.sql`
5. Add all as `<Build Include="..." />` items in .sqlproj

### 📋 POST-DEPLOYMENT CANDIDATES (Idempotent configuration)

| File | Content | Category | Action |
|------|---------|----------|--------|
| `seed-data.sql` | Data seeding (INSERT statements) | Post-deployment data | **Move to Scripts/Post-Deployment/** |

**Verification needed**: Check if script is idempotent (uses IF NOT EXISTS checks).

### ⚠️ NEEDS EXAMINATION

| File | Size | Category | Action |
|------|------|----------|--------|
| `AutonomousSystemValidation.sql` | 8,896 bytes | Unknown | **Read and categorize** |
| `clr-deploy-generated.sql` | 11 MB (!!) | Generated CLR script | **Determine if needed** |
| `deploy-autonomous-clr-functions-manual.sql` | 14,478 bytes | CLR deployment | **Examine** |
| `deploy-autonomous-clr-functions.sql` | 8,445 bytes | CLR deployment | **Examine** |
| `temp-redeploy-clr-unsafe.sql` | 1,866 bytes | Temporary script | **Likely delete** |
| `update-clr-assembly.sql` | 1,430 bytes | CLR assembly update | **Examine** |
| `verify-temporal-tables.sql` | 1,398 bytes | Validation script | **Examine** |

---

## SQL Scripts in `/sql` Root - NEEDS CATEGORIZATION

### 🔴 DATABASE-LEVEL (Cannot go in DACPAC)

| File | Content (from previous reading) | Category | Action |
|------|-------------------------------|----------|--------|
| `EnableQueryStore.sql` | `USE master; ALTER DATABASE Hartonomous SET QUERY_STORE = ON` | Database-level config | **Document as external deployment** |
| `EnableAutomaticTuning.sql` | Similar to Query Store (assumption) | Database-level config | **Verify and document as external** |

**Reason**: Uses `USE master` and `ALTER DATABASE` - both prohibited in DACPAC schema/post-deployment.

**Recommended approach**:
- Document as manual setup steps OR
- Create post-DACPAC PowerShell script OR
- Use SqlCmd variables in external deployment script

### ⚠️ COMPLEX - NEEDS ANALYSIS

| File | Size | Content (from previous reading) | Category | Preliminary Action |
|------|------|-------------------------------|----------|--------------------|
| `Setup_FILESTREAM.sql` | 7,224 bytes | Manual prerequisites, filegroup creation, cursor migration procedure | Mixed (schema + external) | **Split into schema procedure + external setup** |
| `Optimize_ColumnstoreCompression.sql` | 5,675 bytes | Unknown | Likely optimization procedure | **Read and categorize** |
| `Setup_Vector_Indexes.sql` | 9,898 bytes | Unknown | Likely index creation | **Read and categorize** |
| `Temporal_Tables_Evaluation.sql` | 10,834 bytes | Unknown | Likely evaluation procedure | **Read and categorize** |
| `Ingest_Models.sql` | 4,224 bytes | Unknown | Likely ingestion procedure | **Read and categorize** |
| `Predict_Integration.sql` | 11,342 bytes | Unknown | Likely prediction integration | **Read and categorize** |

**Setup_FILESTREAM.sql Special Case**:
- Contains migration procedure: `sp_MigratePayloadLocatorToFilestream` - THIS IS A SCHEMA OBJECT
- Contains filegroup creation: `ALTER DATABASE ... ADD FILEGROUP` - THIS IS DATABASE-LEVEL
- **Action**: Split into two parts:
  1. Migration procedure → `Procedures/dbo.sp_MigratePayloadLocatorToFilestream.sql` (Build item)
  2. Filegroup setup → Document as manual prerequisite or external deployment script

---

## SQL Scripts in Subdirectories

### `/sql/cdc/`

| File | Size | Category | Action |
|------|------|----------|--------|
| `OptimizeCdcConfiguration.sql` | 4,578 bytes | CDC optimization | **Read to determine if post-deployment or procedure** |

### `/sql/ef-core/`

| File | Size | Category | Action |
|------|------|----------|--------|
| `Hartonomous.Schema.sql` | 60,048 bytes | EF Core generated schema | **Compare with Database project schema** |

**Critical Question**: Is this EF Core schema file redundant with SSDT project? Likely YES - delete after verification.

### `/sql/migrations/`

| File | Size | Category | Action |
|------|------|----------|--------|
| `001_atoms_remove_tenantid.sql` | 3,276 bytes | One-time migration | **Determine if historical or still needed** |

**Decision needed**: Is this a historical migration (already applied) or future migration?
- If historical: Delete (no longer needed in DACPAC)
- If future: Apply manually or integrate into schema design

### `/sql/verification/`

| File | Size | Category | Action |
|------|------|----------|--------|
| `GodelEngine_Validation.sql` | 10,189 bytes | Validation procedures | **Read to determine if schema or test script** |
| `SystemVerification.sql` | 10,860 bytes | Verification procedures | **Read to determine if schema or test script** |

---

## Already Processed Files ✅

### `/sql/procedures/` - ALREADY BEING MIGRATED

**Note**: These are being broken down as part of Phase 1.4. Many contain:
- Individual procedures (should be extracted)
- CLR bindings (already extracted in Phase 1.1.1)
- Aggregate functions (already extracted in Phase 1.1.2)

**Status**: 78 CLR wrappers extracted, ready for Phase 1.4 (extract remaining ~60-70 procedures).

### `/sql/tables/` - PARTIALLY PROCESSED

**Status**: 
- 4 table group files split into 13 individual tables ✅ (Phase 1.2 complete)
- Remaining table files need to be reviewed for similar consolidation

**Files needing attention**:
- `graph.AtomGraphEdges_Add_PseudoColumn_Indexes.sql` - Appears to be index creation (post-deployment?)
- `Temporal_Tables_Add_Retention_and_Columnstore.sql` - Appears to be configuration (post-deployment?)
- `TensorAtomCoefficients_Temporal.sql` - Appears to be configuration (post-deployment?)

### `/sql/types/` - APPEARS COMPLETE

| File | Type |
|------|------|
| `provenance.AtomicStream.sql` | User-defined table type |
| `provenance.ComponentStream.sql` | User-defined table type |

**Status**: These are schema objects, should be Build items. ✅

---

## Decision Matrix

| Script Type | DACPAC Location | Example |
|-------------|-----------------|---------|
| CREATE TABLE/PROC/FUNCTION/TYPE | Build item | `Procedures/dbo.sp_Analyze.sql` |
| Service Broker (MESSAGE TYPE, CONTRACT, QUEUE, SERVICE) | Build item | Extract from `setup-service-broker.sql` |
| Idempotent data seeding | Post-deployment | `seed-data.sql` |
| Index optimization (idempotent) | Post-deployment | Potentially `Optimize_ColumnstoreCompression.sql` |
| ALTER DATABASE, USE master | **External deployment** | `EnableQueryStore.sql`, `EnableAutomaticTuning.sql` |
| sp_cdc_enable_db, sp_configure | **External deployment** | `enable-cdc.sql` |
| PowerShell scripts | **Stay in /scripts** | All .ps1 files |
| One-time migrations (historical) | **Delete** | Potentially `001_atoms_remove_tenantid.sql` |
| EF Core generated schema | **Delete** | `Hartonomous.Schema.sql` (redundant) |

---

## Immediate Action Items for Phase 1.3

### Step 1: Read Unexamined SQL Scripts (Priority Order)

1. **High Priority** (affect DACPAC structure):
   - `setup-service-broker.sql` - Confirm Service Broker objects for extraction
   - `Setup_FILESTREAM.sql` - Identify split points (procedure vs. external)
   - `Setup_Vector_Indexes.sql` - Determine if post-deployment or procedure
   - `Optimize_ColumnstoreCompression.sql` - Determine if post-deployment or procedure

2. **Medium Priority** (configuration/deployment):
   - `EnableAutomaticTuning.sql` - Verify it's database-level like Query Store
   - `seed-data.sql` - Verify idempotency for post-deployment
   - `OptimizeCdcConfiguration.sql` - Categorize as post-deployment or procedure

3. **Low Priority** (validation/cleanup):
   - CLR deployment scripts - Determine if obsolete
   - Verification scripts - Determine if tests or schema
   - Migration scripts - Determine if historical

### Step 2: Extract Service Broker Objects

From `setup-service-broker.sql`, extract to:
- `Types/dbo.MessageTypeName.sql` for each MESSAGE TYPE
- `Contracts/dbo.ContractName.sql` for each CONTRACT  
- `Queues/dbo.QueueName.sql` for each QUEUE
- `Services/dbo.ServiceName.sql` for each SERVICE

### Step 3: Document External Deployment Requirements

Create `docs/EXTERNAL_DEPLOYMENT.md` with:
- Query Store enablement steps
- CDC enablement steps
- FILESTREAM filegroup setup
- Any other database-level configuration

### Step 4: Move Verified Post-Deployment Scripts

Move to `src/Hartonomous.Database/Scripts/Post-Deployment/`:
- `seed-data.sql` (after verifying idempotency)
- Any index optimization scripts (after verification)
- Any configuration scripts that are T-SQL idempotent

---

## Questions to Answer

1. **EF Core Schema**: Is `Hartonomous.Schema.sql` redundant with SSDT project? If yes, delete.
2. **Migrations**: Is `001_atoms_remove_tenantid.sql` already applied? If yes, delete.
3. **CLR Scripts**: Are the `clr-deploy-*.sql` scripts obsolete now that we're using DACPAC? Likely yes.
4. **Verification**: Are `GodelEngine_Validation.sql` and `SystemVerification.sql` test scripts or schema?
5. **Temporal Tables**: Are the temporal table configuration scripts post-deployment or schema?

---

## Summary Counts

| Category | Count | Location |
|----------|-------|----------|
| PowerShell scripts | 18 | `/scripts` ✅ KEEP |
| Database-level SQL (external) | 3 confirmed, ~5 suspected | Various - DOCUMENT |
| Service Broker schema objects | 1 file (multiple objects) | `/scripts` - EXTRACT |
| Post-deployment candidates | ~3-5 files | Various - VERIFY & MOVE |
| Schema objects (procedures/tables) | ~70 files | `/sql` - MIGRATE |
| Needs examination | ~10 files | Various - READ & CATEGORIZE |
| Potentially obsolete | ~5 files | Various - VERIFY & DELETE |

---

**Next Step**: Read the high-priority scripts to complete categorization.
