# Hartonomous Database Deployment Plan

## Overview

The database deployment is split into **three components** for enterprise compatibility:

1. **DACPAC** - Schema-only declarative deployment (tables, types, schemas, indexes)
2. **CLR Deployment** - SQL CLR assemblies, functions, aggregates, UDTs
3. **Runtime Scripts** - Procedures, post-deployment ALTER statements, migration scripts

## Component 1: DACPAC Deployment

**File:** `bin/Release/Hartonomous.Database.dacpac` (22KB)

**Contents:**
- ✅ 56 Tables (dbo, graph, provenance schemas)
- ✅ 2 Schemas (provenance, graph)
- ✅ Indexes (clustered, nonclustered, spatial, graph)
- ✅ Foreign keys and constraints
- ✅ Pre-deployment script (schema creation)

**Deploy with:**
```powershell
SqlPackage /Action:Publish `
  /SourceFile:"src\Hartonomous.Database\bin\Release\Hartonomous.Database.dacpac" `
  /TargetConnectionString:"Server=localhost;Database=Hartonomous;Trusted_Connection=True;TrustServerCertificate=True;"
```

**Or via Visual Studio:**
- Right-click project → Publish
- Select target database
- Review changes → Publish

---

## Component 2: CLR Deployment

**Status:** ✅ Already deployed via `scripts/deploy-database-unified.ps1`

**Deploy manually if needed:**
```powershell
.\scripts\deploy-clr-secure.ps1 -Server "localhost" -Database "Hartonomous"
```

**CLR Components Deployed:**
- SqlClrFunctions.dll (main assembly)
- Vector operations (clr_VectorAdd, clr_VectorDotProduct, etc.)
- Aggregates (clr_VectorAverage, clr_DBSCANCluster, etc.)
- UDTs (provenance.AtomicStream, provenance.ComponentStream)
- Spatial functions (clr_ProjectTo3D, clr_CreateSpatialHash)
- Stream orchestrators (clr_StreamOrchestrator)

**Excluded from DACPAC:**
- `Types/provenance.AtomicStream.sql` → Deployed via CLR script
- `Types/provenance.ComponentStream.sql` → Deployed via CLR script
- `Tables/provenance.GenerationStreams.sql` → Uses CLR UDT (deploy after CLR)

---

## Component 3: Runtime Scripts

### A. Stored Procedures (63 files) - EXCLUDED from DACPAC

**Reason:** Multi-procedure files with `CREATE OR ALTER` syntax incompatible with DACPAC one-object-per-file requirement

**Location:** `src/Hartonomous.Database/Procedures/`

**Deploy with:**
```powershell
# Deploy all procedures via unified script
.\scripts\deploy-database-unified.ps1 -Server "localhost" -Database "Hartonomous" -SkipFilestream -SkipCLR

# Or manually execute each .sql file in SSMS
```

**Key Procedures:**
- **Agent Framework:** `dbo.sp_Act`, `dbo.sp_Analyze`, `dbo.sp_Learn`, `dbo.sp_Hypothesize`
- **Atom Ingestion:** `dbo.sp_AtomIngestion`, `dbo.sp_AtomizeImage`, `dbo.sp_AtomizeAudio`, `dbo.sp_AtomizeCode`
- **Inference:** `Inference.ServiceBrokerActivation`, `Inference.VectorSearchSuite`, `Inference.SpatialGenerationSuite`
- **Provenance:** `Provenance.Neo4jSyncActivation`, `Provenance.ProvenanceTracking`
- **Search:** `Search.SemanticSearch`, `dbo.VectorSearch`, `dbo.FullTextSearch`
- **Generation:** `Generation.TextFromVector`, `Generation.ImageFromPrompt`, `Generation.AudioFromPrompt`
- **Billing:** `Billing.InsertUsageRecord_Native`, `dbo.BillingFunctions`

### B. Post-Deployment Scripts (3 files) - EXCLUDED from DACPAC

**Reason:** ALTER TABLE statements incompatible with declarative schema model

**Location:** `src/Hartonomous.Database/Scripts/Post-Deployment/`

**Files:**
1. `TensorAtomCoefficients_Temporal.sql` - Add temporal columns (ValidFrom, ValidTo)
2. `Temporal_Tables_Add_Retention_and_Columnstore.sql` - Add retention policy + columnstore
3. `graph.AtomGraphEdges_Add_PseudoColumn_Indexes.sql` - Add computed column indexes for graph pseudo-columns

**Deploy manually:**
```sql
-- Execute in order after DACPAC deployment
:r Scripts\Post-Deployment\TensorAtomCoefficients_Temporal.sql
:r Scripts\Post-Deployment\Temporal_Tables_Add_Retention_and_Columnstore.sql
:r Scripts\Post-Deployment\graph.AtomGraphEdges_Add_PseudoColumn_Indexes.sql
```

### C. Migration Scripts (3 files) - EXCLUDED from DACPAC

**Reason:** ALTER DATABASE and complex migration logic incompatible with DACPAC

**Location:** `src/Hartonomous.Database/Tables/`

**Files:**
1. `dbo.BillingUsageLedger_InMemory.sql` - In-Memory OLTP migration (requires memory-optimized filegroup)
2. `dbo.BillingUsageLedger_Migrate_to_Ledger.sql` - Ledger table migration
3. `dbo.TestResults.sql` - Complex test results table with dynamic structure

**Deploy manually if needed:**
- These are **optional** advanced features
- Require manual filegroup configuration
- Execute only if implementing In-Memory OLTP or Ledger features

---

## Deployment Order (Clean Install)

```powershell
# 1. DACPAC (schema baseline)
SqlPackage /Action:Publish /SourceFile:"src\Hartonomous.Database\bin\Release\Hartonomous.Database.dacpac" `
  /TargetConnectionString:"Server=localhost;Database=Hartonomous;Trusted_Connection=True;TrustServerCertificate=True;"

# 2. CLR assemblies (functions, aggregates, UDTs)
.\scripts\deploy-clr-secure.ps1 -Server "localhost" -Database "Hartonomous"

# 3. CLR-dependent table (GenerationStreams uses AtomicStream UDT)
sqlcmd -S localhost -d Hartonomous -i "src\Hartonomous.Database\Tables\provenance.GenerationStreams.sql"

# 4. Post-deployment ALTER scripts
sqlcmd -S localhost -d Hartonomous -i "src\Hartonomous.Database\Scripts\Post-Deployment\TensorAtomCoefficients_Temporal.sql"
sqlcmd -S localhost -d Hartonomous -i "src\Hartonomous.Database\Scripts\Post-Deployment\Temporal_Tables_Add_Retention_and_Columnstore.sql"
sqlcmd -S localhost -d Hartonomous -i "src\Hartonomous.Database\Scripts\Post-Deployment\graph.AtomGraphEdges_Add_PseudoColumn_Indexes.sql"

# 5. Stored procedures (all 63 files)
.\scripts\deploy-database-unified.ps1 -Server "localhost" -Database "Hartonomous" -SkipFilestream -SkipCLR
```

**Or use the unified script for everything:**
```powershell
.\scripts\deploy-database-unified.ps1 -Server "localhost" -Database "Hartonomous"
```

---

## What's NOT in the DACPAC (Sanity Check)

### Files Tracked in Git but Excluded from Build

| Category | Files | Reason | Deploy Method |
|----------|-------|--------|---------------|
| **Procedures** | 63 files | Multi-procedure syntax | Unified script or manual |
| **CLR UDTs** | 2 types | Assembly introspection limitation | CLR deployment script |
| **CLR Tables** | 1 table | Uses CLR UDT | Manual after CLR |
| **Post-Deploy** | 3 scripts | ALTER TABLE statements | Manual after DACPAC |
| **Migrations** | 3 scripts | ALTER DATABASE statements | Optional advanced features |

### Critical Functionality Verification

✅ **Core Tables:** All 56 core tables in DACPAC
- Atoms, AtomEmbeddings, Models, ModelLayers, TensorAtoms, etc.

✅ **Graph Tables:** Both graph tables with AS NODE/AS EDGE syntax
- graph.AtomGraphNodes, graph.AtomGraphEdges

✅ **Provenance Tables:** Core provenance tables
- provenance.Concepts, provenance.AtomConcepts, provenance.ConceptEvolution

❌ **Procedures:** 0/63 in DACPAC (all excluded for splitting later)

❌ **CLR UDTs:** 0/2 in DACPAC (deployed via CLR script)

❌ **GenerationStreams:** Excluded (depends on CLR UDT)

---

## Data Loss Risk Assessment

### ✅ NO DATA LOSS - Schema Only

- DACPAC contains **schema only** (CREATE TABLE, indexes, constraints)
- No seed data, no INSERT statements
- Existing data preserved during deployment
- Foreign key constraints preserved

### ⚠️ BREAKING CHANGES (if procedures deployed)

Procedures were excluded to avoid deployment errors. If procedures were previously deployed:
- Old procedure signatures may be incompatible
- Need to execute all 63 .sql files to update

### 🔄 Migration Strategy

1. **Fresh Install:** Follow deployment order above
2. **Update Existing:** 
   - DACPAC will update schema (add columns, indexes, tables)
   - Re-run procedure scripts to update logic
   - Post-deployment scripts for temporal/graph enhancements

---

## Rebuild DACPAC (if needed)

```powershell
cd src\Hartonomous.Database
dotnet build -c Release

# Output: bin\Release\Hartonomous.Database.dacpac
```

**Current Status:** ✅ 0 errors, production-ready

---

## Next Steps

### Immediate (for clean deployment):
1. ✅ DACPAC built and ready
2. ⏳ Deploy DACPAC to target server
3. ⏳ Verify CLR assemblies deployed
4. ⏳ Execute post-deployment scripts
5. ⏳ Deploy procedures

### Future Improvements:
1. **Split Procedures:** Separate multi-procedure files into one-per-file for DACPAC inclusion
2. **CLR UDT Workaround:** Use VARBINARY(MAX) instead of CLR UDTs for DACPAC compatibility
3. **Post-Deployment Automation:** Create wrapper script for post-deployment execution
4. **CI/CD Integration:** Azure Pipelines task for automated DACPAC deployment

---

## References

- **DACPAC Build:** `src/Hartonomous.Database/Hartonomous.Database.sqlproj`
- **CLR Deployment:** `scripts/deploy-clr-secure.ps1`
- **Unified Deployment:** `scripts/deploy-database-unified.ps1`
- **Pre-Deployment:** `src/Hartonomous.Database/Scripts/Pre-Deployment/Script.PreDeployment.sql`
