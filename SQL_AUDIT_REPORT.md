# SQL File Comprehensive Audit Report
**Date:** November 19, 2025  
**Database Project:** Hartonomous.Database.sqlproj

## Executive Summary

- **Total Files Audited:** 161
- **Files Matching Convention:** 151 (93.8%)
- **Files with Naming Mismatches:** 10 (6.2%)

---

## Critical Issues Found

### 1. Table Files with Naming Mismatches (6 files)

| File Name | Expected Table Name | Actual Table Name | Issue Type |
|-----------|---------------------|-------------------|------------|
| `Attention.AttentionGenerationTables.sql` | Attention.AttentionGenerationTables | AttentionGenerationLog | **DUPLICATE** - Real table is `dbo.AttentionGenerationLog.sql` |
| `dbo.BillingUsageLedger_Migrate_to_Ledger.sql` | BillingUsageLedger_Migrate_to_Ledger | BillingUsageLedger_New | Migration script (filename mismatch) |
| `Provenance.ProvenanceTrackingTables.sql` | Provenance.ProvenanceTrackingTables | OperationProvenance | **DUPLICATE** - Real table is `dbo.OperationProvenance.sql` |
| `Reasoning.ReasoningFrameworkTables.sql` | Reasoning.ReasoningFrameworkTables | ReasoningChains | **DUPLICATE** - Real table is `dbo.ReasoningChains.sql` |
| `Stream.StreamOrchestrationTables.sql` | Stream.StreamOrchestrationTables | StreamOrchestrationResults | **DUPLICATE** - Real table is `dbo.StreamOrchestrationResults.sql` |
| `TensorAtomCoefficients_Temporal.sql` | TensorAtomCoefficients_Temporal | TensorAtomCoefficients_History | Filename mismatch |

**Note:** Files like `Attention.AttentionGenerationTables.sql`, `Provenance.ProvenanceTrackingTables.sql`, etc. appear to be OLD schema files that create tables already defined elsewhere. These should be DELETED.

### 2. Procedure Files with Naming Mismatches (3 files)

| File Name | Expected Procedure Name | Actual Procedure Name | Issue |
|-----------|------------------------|----------------------|-------|
| `Admin.WeightRollback.sql` | Admin.WeightRollback | sp_RollbackWeightsToTimestamp | **DUPLICATE** - Real proc is `dbo.sp_RollbackWeightsToTimestamp.sql` |
| `Billing.InsertUsageRecord_Native.sql` | Billing.InsertUsageRecord_Native | sp_InsertBillingUsageRecord_Native | **DUPLICATE** - Real proc is `dbo.sp_InsertBillingUsageRecord_Native.sql` |
| `Embedding.TextToVector.sql` | Embedding.TextToVector | sp_TextToEmbedding | Filename mismatch |

### 3. Function Files with Naming Mismatches (1 file)

| File Name | Expected Function Name | Actual Function Name | Issue |
|-----------|----------------------|---------------------|-------|
| `dbo.fn_HilbertFunctions.sql` | fn_HilbertFunctions | fn_ComputeHilbertValue | Filename mismatch |

---

## Duplicate Table Discovery

### Found Duplicate: TenantAtom vs TenantAtoms

**Status:** Both files exist and create different tables:
- `dbo.TenantAtom.sql` → Creates `[dbo].[TenantAtom]` (better documented, 19 lines)
- `dbo.TenantAtoms.sql` → Creates `[dbo].[TenantAtoms]` (minimal, 7 lines)

**Action Required:** Determine which table name is correct, delete the other file.

---

## Recommended Actions

### CRITICAL - Delete Duplicate/Old Schema Files (5 files)

These appear to be legacy schema files that duplicate existing tables:

```powershell
Remove-Item '.\src\Hartonomous.Database\Tables\Attention.AttentionGenerationTables.sql'
Remove-Item '.\src\Hartonomous.Database\Tables\Provenance.ProvenanceTrackingTables.sql'
Remove-Item '.\src\Hartonomous.Database\Tables\Reasoning.ReasoningFrameworkTables.sql'
Remove-Item '.\src\Hartonomous.Database\Tables\Stream.StreamOrchestrationTables.sql'
Remove-Item '.\src\Hartonomous.Database\Procedures\Admin.WeightRollback.sql'
Remove-Item '.\src\Hartonomous.Database\Procedures\Billing.InsertUsageRecord_Native.sql'
```

### HIGH - Rename Mismatched Files (3 files)

```powershell
# Table files
Move-Item '.\src\Hartonomous.Database\Tables\TensorAtomCoefficients_Temporal.sql' '.\src\Hartonomous.Database\Tables\dbo.TensorAtomCoefficients_History.sql'
Move-Item '.\src\Hartonomous.Database\Tables\dbo.BillingUsageLedger_Migrate_to_Ledger.sql' '.\src\Hartonomous.Database\Tables\dbo.BillingUsageLedger_New.sql'

# Procedure files
Move-Item '.\src\Hartonomous.Database\Procedures\Embedding.TextToVector.sql' '.\src\Hartonomous.Database\Procedures\dbo.sp_TextToEmbedding.sql'

# Function files
Move-Item '.\src\Hartonomous.Database\Functions\dbo.fn_HilbertFunctions.sql' '.\src\Hartonomous.Database\Functions\dbo.fn_ComputeHilbertValue.sql'
```

### MEDIUM - Resolve TenantAtom vs TenantAtoms Conflict

1. Review code references to determine correct table name
2. Delete the unused file
3. Update any FK references if needed

---

## Additional Findings

### Files with Correct Naming (151 files)

All other tables, procedures, functions, and views follow the correct naming convention:
- **Tables:** 65/71 (91.5% correct)
- **Procedures:** 70/73 (95.9% correct)
- **Functions:** 24/25 (96% correct)
- **Views:** 6/6 (100% correct)

---

## Build Impact Analysis

Current DACPAC build failures are caused by:

1. **Duplicate table definitions** from old schema files (e.g., `Attention.AttentionGenerationTables.sql`)
2. **Missing CLR function wrappers** (from previous audit)
3. **Missing T-SQL functions** (fn_DiscoverConcepts, fn_BindAtomsToCentroid - these exist but were duplicated)
4. **Column name mismatches** in sp_ComputeSemanticFeatures
5. **Table name conflicts** (IngestionJobs vs IngestionJob references)

---

## Next Steps Priority

1. ✅ **COMPLETE** - File naming audit (this document)
2. ⏳ **TODO** - Delete duplicate/old schema files (5 files)
3. ⏳ **TODO** - Rename mismatched files (3 files)
4. ⏳ **TODO** - Resolve TenantAtom conflict
5. ⏳ **TODO** - Fix IngestionJobs → IngestionJob references in procedures
6. ⏳ **TODO** - Delete duplicate functions created earlier (fn_DiscoverConcepts, fn_BindAtomsToCentroid, sp_ComputeSemanticFeatures)
7. ⏳ **TODO** - Rebuild DACPAC and verify 0 errors
8. ⏳ **TODO** - Address missing CLR wrappers (from previous audit)
9. ⏳ **TODO** - Sign and deploy CLR assemblies

---

## Full Audit Data

Complete audit results saved to: `SQL_FILE_AUDIT.csv`

**Generated by:** GitHub Copilot  
**Command:** Comprehensive SQL file audit script
