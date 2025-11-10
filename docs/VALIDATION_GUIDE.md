# 🔍 RAPID VALIDATION GUIDE

**Purpose:** Quick smoke test to verify all Phase 1-5 implementations are functional  
**Estimated Time:** 15 minutes  
**Prerequisites:** SQL Server 2025 with CLR enabled, database deployed

---

## ✅ PHASE 1: CORE FUNCTIONALITY VALIDATION

### Test 1.1: TransformerInference LayerNorm

```sql
-- Verify CLR function exists
SELECT OBJECT_ID('dbo.clr_RunInference', 'FN') AS FunctionId;
-- Expected: Non-NULL integer

-- Test LayerNorm execution (requires test tensor)
-- Manual verification: Check src/SqlClr/TensorOperations/TransformerInference.cs line 180
```

### Test 1.2: OODA Anomaly Detection

```sql
-- Verify dual anomaly detection in sp_Analyze
EXEC sp_helptext 'dbo.sp_Analyze';
-- Expected: Contains "IsolationForestScore" AND "LocalOutlierFactor"

-- Check for UNION detection logic
SELECT definition 
FROM sys.sql_modules 
WHERE object_id = OBJECT_ID('dbo.sp_Analyze')
  AND definition LIKE '%WHERE IsolationScore > 0.7 OR LOFScore > 1.5%';
-- Expected: 1 row
```

---

## ✅ PHASE 2: PERFORMANCE & SECURITY VALIDATION

### Test 2.1: Columnstore Indexes

```sql
SELECT name, type_desc 
FROM sys.indexes 
WHERE name IN (
    'NCCI_BillingUsageLedger_Analytics',
    'NCCI_TensorAtomCoefficients_SVD',
    'NCCI_AutonomousImprovementHistory_Analytics'
);
-- Expected: 3 rows with type_desc = 'NONCLUSTERED COLUMNSTORE'
```

### Test 2.2: Vector DiskANN Indexes

```sql
SELECT name, type_desc
FROM sys.indexes
WHERE type_desc = 'VECTOR'
  AND name LIKE 'IX_%_VectorIndex%';
-- Expected: 10 rows (all VECTOR columns)
```

### Test 2.3: Spatial Indexes

```sql
SELECT name, type_desc
FROM sys.indexes
WHERE type_desc = 'SPATIAL'
  AND name IN (
      'IX_TensorAtoms_SpatialSignature',
      'IX_CodeAtom_Embedding',
      'IX_AudioData_Spectrogram',
      'IX_VideoFrame_MotionVectors',
      'IX_Image_ContentRegions',
      'IX_SessionPaths_Path'
  );
-- Expected: 6 rows (all critical spatial indexes)
```

### Test 2.4: CLR Security Configuration

```sql
-- Verify CLR strict security
SELECT name, value_in_use
FROM sys.configurations
WHERE name = 'clr strict security';
-- Expected: value_in_use = 1

-- Verify TRUSTWORTHY is OFF
SELECT name, is_trustworthy_on
FROM sys.databases
WHERE name = DB_NAME();
-- Expected: is_trustworthy_on = 0

-- Verify trusted assemblies exist
SELECT COUNT(*) AS TrustedAssemblyCount
FROM sys.trusted_assemblies;
-- Expected: >= 6 (System.Numerics.Vectors, MathNet.Numerics, etc.)
```

---

## ✅ PHASE 3: SVD & SHAPE INGESTION VALIDATION

### Test 3.1: SVD Pipeline CLR Bindings

```sql
SELECT name, type_desc
FROM sys.objects
WHERE name IN (
    'clr_SvdDecompose',
    'clr_ProjectToPoint',
    'clr_CreateGeometryPointWithImportance',
    'clr_ParseModelLayer',
    'clr_ReconstructFromSVD'
);
-- Expected: 5 rows with type_desc = 'SQL_SCALAR_FUNCTION'
```

### Test 3.2: SVD Pipeline Stored Procedure

```sql
SELECT OBJECT_ID('dbo.sp_AtomizeModel', 'P') AS ProcedureId;
-- Expected: Non-NULL integer

-- Test execution (requires model blob)
-- EXEC dbo.sp_AtomizeModel @model_blob = 0x..., @model_format_hint = 'gguf', @layer_name = 'test', @parent_layer_id = 1;
```

### Test 3.3: AST-as-GEOMETRY Pipeline

```sql
-- Verify CLR binding
SELECT OBJECT_ID('dbo.clr_GenerateCodeAstVector', 'FN') AS FunctionId;
-- Expected: Non-NULL integer

-- Verify stored procedure
SELECT OBJECT_ID('dbo.sp_AtomizeCode', 'P') AS ProcedureId;
-- Expected: Non-NULL integer

-- Test AST generation
DECLARE @sourceCode NVARCHAR(MAX) = 'public class Test { public int Add(int a, int b) { return a + b; } }';
SELECT dbo.clr_GenerateCodeAstVector(@sourceCode) AS AstVector;
-- Expected: JSON array of 512 floats
```

### Test 3.4: Trajectory Aggregation

```sql
-- Verify aggregate binding
SELECT name, type_desc
FROM sys.objects
WHERE name = 'agg_BuildPathFromAtoms';
-- Expected: 1 row with type_desc = 'AGGREGATE_FUNCTION'

-- Verify SessionPaths table
SELECT COUNT(*) 
FROM sys.tables 
WHERE name = 'SessionPaths';
-- Expected: 1

-- Test aggregate (requires AtomEmbeddings data)
-- SELECT SessionId, dbo.agg_BuildPathFromAtoms(AtomId, Timestamp) AS Path
-- FROM UserInteractions GROUP BY SessionId;
```

---

## ✅ PHASE 4: GENERATIVE CAPABILITIES VALIDATION

### Test 4.1: Shape-to-Content CLR Bindings

```sql
SELECT name, type_desc
FROM sys.objects
WHERE name IN (
    'clr_GenerateImageFromShapes',
    'clr_GenerateAudioFromSpatialSignature',
    'clr_SynthesizeModelLayer'
);
-- Expected: 3 rows with type_desc = 'SQL_SCALAR_FUNCTION'
```

### Test 4.2: Student Model Synthesis Procedure

```sql
SELECT OBJECT_ID('dbo.sp_ExtractStudentModel', 'P') AS ProcedureId;
-- Expected: Non-NULL integer

-- Test execution (requires TensorAtom data)
-- DECLARE @blob VARBINARY(MAX);
-- EXEC dbo.sp_ExtractStudentModel 
--     @QueryShape = geometry::Point(0,0,0).STBuffer(10),
--     @ParentLayerId = 1,
--     @OutputFormat = 'json',
--     @ModelBlob = @blob OUTPUT;
```

### Test 4.3: Enhanced OODA Hypotheses

```sql
-- Verify sp_Hypothesize contains advanced hypotheses
SELECT definition
FROM sys.sql_modules
WHERE object_id = OBJECT_ID('dbo.sp_Hypothesize')
  AND definition LIKE '%PruneModel%'
  AND definition LIKE '%RefactorCode%'
  AND definition LIKE '%FixUX%';
-- Expected: 1 row (procedure contains all three hypotheses)
```

---

## ✅ PHASE 5: GÖDEL ENGINE VALIDATION

### Test 5.1: AutonomousComputeJobs Table

```sql
SELECT COUNT(*) 
FROM sys.tables 
WHERE name = 'AutonomousComputeJobs';
-- Expected: 1
```

### Test 5.2: OODA Loop Service Broker Integration

```sql
-- Verify queues exist
SELECT name 
FROM sys.service_queues 
WHERE name IN ('AnalyzeQueue', 'HypothesizeQueue', 'ActQueue', 'LearnQueue');
-- Expected: 4 rows

-- Verify services exist
SELECT name 
FROM sys.services 
WHERE name IN ('AnalyzeService', 'HypothesizeService', 'ActService', 'LearnService');
-- Expected: 4 rows
```

---

## 🚀 DEPLOYMENT VALIDATION

### Verify Performance Optimizations Phase

```powershell
# Check deployment script includes optimization phase
Select-String -Path "scripts/deploy-database-unified.ps1" -Pattern "Deploy-PerformanceOptimizations"
# Expected: Match found

# Check optimization scripts are in SqlPaths
Select-String -Path "scripts/deploy-database-unified.ps1" -Pattern "Optimize_ColumnstoreCompression.sql|Setup_Vector_Indexes.sql|CreateSpatialIndexes.sql"
# Expected: 3 matches
```

---

## 📊 SUMMARY VALIDATION

Run this comprehensive check:

```sql
-- Comprehensive system health check
SELECT 
    (SELECT COUNT(*) FROM sys.indexes WHERE type_desc = 'NONCLUSTERED COLUMNSTORE') AS ColumnstoreIndexCount,
    (SELECT COUNT(*) FROM sys.indexes WHERE type_desc = 'VECTOR') AS VectorIndexCount,
    (SELECT COUNT(*) FROM sys.indexes WHERE type_desc = 'SPATIAL') AS SpatialIndexCount,
    (SELECT COUNT(*) FROM sys.assemblies WHERE is_user_defined = 1) AS CLRAssemblyCount,
    (SELECT COUNT(*) FROM sys.trusted_assemblies) AS TrustedAssemblyCount,
    (SELECT COUNT(*) FROM sys.objects WHERE name LIKE 'clr_%' AND type = 'FN') AS CLRFunctionCount,
    (SELECT COUNT(*) FROM sys.objects WHERE name LIKE 'agg_%' AND type = 'AF') AS CLRAggregateCount,
    (SELECT COUNT(*) FROM sys.objects WHERE name LIKE 'sp_%' AND type = 'P') AS StoredProcedureCount,
    (SELECT is_broker_enabled FROM sys.databases WHERE name = DB_NAME()) AS ServiceBrokerEnabled,
    (SELECT value_in_use FROM sys.configurations WHERE name = 'clr strict security') AS CLRStrictSecurity;
```

**Expected Results:**

| Metric | Expected | Actual |
|--------|----------|--------|
| ColumnstoreIndexCount | >= 3 | ___ |
| VectorIndexCount | >= 10 | ___ |
| SpatialIndexCount | >= 15 | ___ |
| CLRAssemblyCount | >= 7 | ___ |
| TrustedAssemblyCount | >= 6 | ___ |
| CLRFunctionCount | >= 30 | ___ |
| CLRAggregateCount | >= 5 | ___ |
| StoredProcedureCount | >= 20 | ___ |
| ServiceBrokerEnabled | 1 | ___ |
| CLRStrictSecurity | 1 | ___ |

---

## ✅ ACCEPTANCE CRITERIA

**All phases validated successfully if:**

1. ✅ All index counts meet or exceed expected values
2. ✅ All CLR bindings registered (functions + aggregates)
3. ✅ All stored procedures deployed and executable
4. ✅ Service Broker enabled and queues operational
5. ✅ CLR strict security enabled, TRUSTWORTHY disabled
6. ✅ No SQL errors when executing validation queries

**Status:** PASS / FAIL  
**Validation Date:** _____________  
**Validated By:** _____________

---

**Document Version:** 1.0  
**Last Updated:** 2025-01-XX  
**Purpose:** Pre-production smoke testing
