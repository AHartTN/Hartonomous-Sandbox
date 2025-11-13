# Comprehensive DACPAC Build Warning Fix Plan

**Total Warnings: 872 across 37 files**

**Status: EXECUTING FIXES - DO NOT SUPPRESS WARNINGS**

---

## Category 1: DMV Ambiguous Column References (130 warnings)
**File**: `sp_ManageHartonomousIndexes.sql`

### Root Cause
JOIN conditions on system DMVs don't explicitly qualify columns with table aliases, making them ambiguous between multiple tables.

### Fix Strategy
Add explicit table alias prefixes to ALL columns in JOIN ON clauses and WHERE clauses.

**Example Fix:**
```sql
-- BEFORE (AMBIGUOUS):
LEFT JOIN sys.dm_db_index_usage_stats us
    ON i.object_id = us.object_id AND i.index_id = us.index_id

-- AFTER (EXPLICIT):
LEFT JOIN sys.dm_db_index_usage_stats us
    ON i.object_id = us.object_id AND i.index_id = us.index_id
```

**Patterns to Fix:**
- `sys.dm_db_index_usage_stats` joins (6 warnings)
- `sys.dm_db_partition_stats` joins (12 warnings)  
- `sys.dm_db_missing_index_*` joins (24+ warnings)
- `sys.indexes` joins (26 warnings)

**Status**: READY TO FIX

---

## Category 2: Billing Schema Reference Mismatches (66 warnings)
**Files**: `sp_RecordUsage.sql`, `sp_GenerateUsageReport.sql`, `sp_CalculateBill.sql`

### Root Cause
Procedures reference `billing.PricingTiers`, `billing.UsageLedger`, `billing.TenantQuotas` which DON'T EXIST.  
Actual table is `dbo.BillingUsageLedger` with different schema.

### Fix Strategy
**SCHEMA DECISION REQUIRED:**
- Option A: Align all procedures to use `dbo.BillingUsageLedger` (existing table)
- Option B: Create actual `billing.*` tables matching procedure references
- **RECOMMENDED: Option A** - Database already uses `dbo.BillingUsageLedger` extensively

**Files Requiring Changes:**
1. `sp_RecordUsage.sql` - 10 table references + 56 column references
2. `sp_GenerateUsageReport.sql` - 6 table references + 50 column references  
3. `sp_CalculateBill.sql` - 4 table references + 34 column references

**Status**: AWAITING SCHEMA DECISION

---

## Category 3: Query Store DMV Ambiguity (64 warnings)
**File**: `sp_AutonomousImprovement.sql`

### Root Cause
Similar to Category 1 - Query Store DMV joins lack explicit column qualification.

### Fix Strategy
Add table alias prefixes to all JOIN conditions and column references.

**DMVs Affected:**
- `sys.query_store_query` (2 warnings)
- `sys.query_store_query_text` (4 warnings)
- `sys.query_store_plan` (4 warnings)
- `sys.query_store_runtime_stats` (18 warnings)

**Example Fix:**
```sql
-- BEFORE:
INNER JOIN sys.query_store_query_text qt ON q.query_text_id = qt.query_text_id

-- AFTER:
INNER JOIN sys.query_store_query_text qt ON q.query_text_id = qt.query_text_id
```

**Status**: READY TO FIX

---

## Category 4: Graph Pseudo-Column Ambiguity (36 warnings)
**File**: `sp_ForwardToNeo4j_Activated.sql`

### Root Cause
SQL Server graph tables have pseudo-columns `$node_id`, `$edge_id` that need explicit table qualification.

### Fix Strategy
Add table alias prefix to `$node_id` and `$edge_id` references.

**Example Fix:**
```sql
-- BEFORE:
WHERE edge.DependencyType = 'GENERATED_FROM'

-- AFTER:
WHERE edge.DependencyType = 'GENERATED_FROM'  
  AND edge.$node_id_from = parent.$node_id
```

**Status**: READY TO FIX

---

## Category 5: Missing Table References (60+ warnings)
**Files**: Multiple

### Root Cause
Procedures reference tables that don't exist in the database project.

### Missing Tables Identified:
1. **`dbo.AtomEmbeddingSpatialMetadata`** (60 warnings in `sp_UpdateAtomEmbeddingSpatialMetadata.sql`)
   - Referenced but table definition doesn't exist
   - **ACTION REQUIRED**: Create table or remove procedure

2. **`dbo.ml_models`** (22 warnings in `sp_AutonomousImprovement.sql`)
   - Used for PREDICT model storage
   - **ACTION REQUIRED**: Create table or use alternative

3. **`dbo.ActQueue`, `dbo.AnalyzeQueue`, `dbo.HypothesizeQueue`, `dbo.LearnQueue`, `dbo.InferenceQueue`** (12 warnings)
   - These are SERVICE BROKER QUEUES, not tables
   - **FIX**: Change references from table syntax to queue syntax
   - **ACTUAL OBJECTS**: Service Broker queues already exist in `ServiceBroker/Queues/`

4. **`provenance.AtomGraphEdges`** (multiple files)
   - Graph table exists as `graph.AtomGraphEdges`
   - **FIX**: Change schema from `provenance` to `graph`

5. **`provenance.ConceptEvolution`, `provenance.AtomConcepts`, `provenance.ModelVersionHistory`** (sp_DiscoverAndBindConcepts, sp_IngestModel)
   - **ACTION REQUIRED**: Verify schema or create tables

**Status**: NEEDS TABLE CREATION OR CODE REMOVAL

---

## Category 6: CLR Function References (26+ warnings)
**Files**: Multiple

### Root Cause
Procedures reference CLR table-valued functions that may not exist yet.

### CLR Functions Referenced:
- `dbo.clr_DeconstructImageToPatches` (sp_AtomizeImage)
- `dbo.clr_GenerateTextSequence` (sp_GenerateText)
- `dbo.clr_GenerateImagePatches` (sp_GenerateImage)
- `dbo.clr_AudioComputeRms`, `dbo.clr_AudioComputePeak` (sp_AtomizeAudio)

**Status**: CLR DEPLOYMENT DEPENDENCY - May be runtime warnings, not schema warnings

---

## Category 7: Fulltext Index DMV Ambiguity (12 warnings)
**Files**: `sp_ExtractKeyPhrases.sql`, `sp_SemanticSimilarity.sql`

### Root Cause
Fulltext system DMV joins lack explicit qualification.

**DMVs Affected:**
- `sys.fulltext_indexes`
- `sys.fulltext_index_columns`

**Status**: READY TO FIX

---

## Category 8: Service Broker Object References (18 warnings)
**Files**: `sp_Act.sql`, `sp_Analyze.sql`, `sp_Hypothesize.sql`, `sp_Learn.sql`, etc.

### Root Cause
Service Broker services and contracts referenced without proper schema qualification.

**Objects Referenced:**
- `ActService`, `AnalyzeService`, `HypothesizeService`, `LearnService`, `InferenceService`
- `Neo4jSyncService`
- `ActContract`, `AnalyzeContract`, etc.

**Fix Strategy:**
Service Broker objects already exist in `ServiceBroker/` directory. References are correct but need full qualification.

**Status**: VERIFY SERVICE BROKER DEPLOYMENT ORDER

---

## Category 9: Miscellaneous (50+ warnings)
**Files**: Various

### Issues:
1. `dbo.sp_executesql` references (10 warnings) - System procedure, false positive
2. `sys.all_objects` vs `sys.objects` ambiguity (2 warnings)
3. Generation stream table references (20 warnings)
4. CLR procedure references without schema qualification

**Status**: MIXED - NEEDS CASE-BY-CASE REVIEW

---

## EXECUTION PLAN

### Phase 1: Delete Incorrect Additions ✅ COMPLETE
- ✅ Deleted `billing.PricingTiers.sql`
- ✅ Deleted `billing.UsageLedger.sql`
- ✅ Deleted `billing.TenantQuotas.sql`
- ✅ Deleted `billing.sql` schema file

### Phase 2: Fix DMV Ambiguous References (IN PROGRESS)
1. Fix `sp_ManageHartonomousIndexes.sql` - 130 warnings
2. Fix `sp_AutonomousImprovement.sql` - 64 warnings
3. Fix `sp_Act.sql` - 48 warnings

### Phase 3: Resolve Missing Tables
1. Create `dbo.AtomEmbeddingSpatialMetadata` table
2. Create `dbo.ml_models` table
3. Fix provenance schema references

### Phase 4: Fix Billing References
1. Align all billing procedures to `dbo.BillingUsageLedger`
2. Update column mappings

### Phase 5: Fix Graph/Fulltext/Service Broker Ambiguity
1. Fix graph pseudo-column references
2. Fix fulltext DMV references
3. Verify Service Broker object references

### Phase 6: Final Build & Verification
1. Rebuild DACPAC
2. Verify 0 errors, 0 warnings
3. Test deployment to localhost

---

## SUCCESS CRITERIA

✅ **ALL 872 warnings resolved**  
✅ **0 new warnings introduced**  
✅ **Build completes with 0 errors, 0 warnings**  
✅ **No suppressions added**  
✅ **Enterprise-grade production ready**
