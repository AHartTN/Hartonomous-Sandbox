# SQL Audit Part 18: Ingestion Queue, Tenant Resolution & Query Utilities

## Overview
Part 18 analyzes 5 procedures: ingestion queueing, tenant GUID resolution, lineage queries, model weight queries, and inference history analysis.

---

## 1. sp_EnqueueIngestion

**Location:** `src/Hartonomous.Database/Procedures/dbo.sp_EnqueueIngestion.sql`  
**Type:** Stored Procedure  
**Lines:** ~35  
**Quality Score:** 76/100

### Purpose
Enqueue file for ingestion processing via Service Broker. Sends file metadata + data to IngestionQueue for async processing.

### Parameters
- `@FileName NVARCHAR(500)` - File name
- `@FileData VARBINARY(MAX)` - File binary content
- `@TenantId INT` - Tenant identifier
- `@SourceUri NVARCHAR(500) = NULL` - Optional source URI

### Architecture

**Enqueue Flow:**
1. BEGIN DIALOG CONVERSATION with IngestionService
2. Convert binary file data to hex string
3. Build XML message (FileName, TenantId, SourceUri, FileDataHex)
4. SEND message to IngestionQueue
5. Return conversation handle for tracking

### Key Operations

**Service Broker SEND:**
```sql
BEGIN DIALOG CONVERSATION @ConversationHandle
    FROM SERVICE [IngestionService] TO SERVICE 'IngestionService'
    ON CONTRACT [IngestionContract] WITH ENCRYPTION = OFF;

SEND ON CONVERSATION @ConversationHandle
    MESSAGE TYPE [IngestionRequest] (@MessageXml);
```

**Binary to Hex Conversion:**
```sql
CONVERT(NVARCHAR(MAX), @FileData, 1) AS FileDataHex
```

### Dependencies
- Service Broker: IngestionService, IngestionContract, IngestionRequest message type
- Activated procedure: sp_ProcessIngestion_Activated (not reviewed yet)

### Quality Assessment

**Strengths:**
- ✅ **Async pattern** - Fire-and-forget via Service Broker
- ✅ **Returns tracking handle** - Conversation handle for status checks
- ✅ **Multi-tenant support** - TenantId included in message
- ✅ **Optional source URI** - Flexibility for tracking origin

**Weaknesses:**
- 🔴 **VARBINARY(MAX) in message** - Hex string doubles size (inefficient)
- ⚠️ **No error handling** - Missing TRY/CATCH
- ⚠️ **No validation** - FileName, TenantId not validated
- ⚠️ **No size limit** - Could queue multi-GB files
- ⚠️ **XML format** - JSON would be more modern
- ⚠️ **Encryption OFF** - WITH ENCRYPTION = OFF (security concern)
- ⚠️ **No authorization check** - Any user can enqueue files for any tenant
- ⚠️ **No conversation end** - Relies on activated procedure to close

**Performance:**
- Binary-to-hex doubles size (2x memory/network)
- VARBINARY(MAX) could cause message queue bloat
- No streaming support for large files

**Security:**
- ⚠️ No encryption
- ⚠️ No authorization (tenant access control)
- ⚠️ No file type validation (could queue executables, malware)

### Improvement Recommendations
1. **Priority 1:** Add file size limit check (reject >100MB, use external storage)
2. **Priority 2:** Add authorization check (user can ingest for tenant)
3. **Priority 3:** Add TRY/CATCH error handling
4. **Priority 4:** Validate FileName, TenantId
5. **Priority 5:** Enable encryption
6. **Priority 6:** Add file type validation (allowed extensions/MIME types)
7. **Priority 7:** Switch to JSON message format
8. **Priority 8:** Consider FileStream or external blob storage for large files
9. **Priority 9:** Add deduplication (check ContentHash before enqueue)

---

## 2. sp_ResolveTenantGuid

**Location:** `src/Hartonomous.Database/Procedures/dbo.sp_ResolveTenantGuid.sql`  
**Type:** Stored Procedure  
**Lines:** ~75  
**Quality Score:** 92/100 ⭐

### Purpose
Resolve Azure Entra tenant GUID to internal integer TenantId. ACID-compliant with auto-registration, prevents race conditions. **GOLD STANDARD for tenant resolution.**

### Parameters
- `@TenantGuid UNIQUEIDENTIFIER` - Azure Entra tenant GUID
- `@TenantName NVARCHAR(200) = NULL` - Optional tenant display name
- `@AutoRegister BIT = 1` - Auto-register new tenants (default enabled)
- `@TenantId INT OUTPUT` - Resolved tenant ID

### Architecture

**Resolution Flow:**
1. Try SELECT with NOLOCK (fast path for existing tenants)
2. If found and active, return immediately
3. If not found and @AutoRegister=1:
   - BEGIN TRANSACTION
   - SELECT with UPDLOCK/HOLDLOCK (prevent race conditions)
   - Double-check still not exists
   - INSERT new TenantGuidMapping
   - Log registration event
   - COMMIT
4. If not found and @AutoRegister=0:
   - RAISERROR with tenant not found

### Key Operations

**Race-Condition-Safe Insert:**
```sql
SELECT @TenantId = TenantId
FROM dbo.TenantGuidMapping WITH (UPDLOCK, HOLDLOCK)
WHERE TenantGuid = @TenantGuid;

IF @TenantId IS NULL
BEGIN
    INSERT INTO dbo.TenantGuidMapping (...) VALUES (...);
    SET @TenantId = SCOPE_IDENTITY();
END
```

### Dependencies
- Tables: `TenantGuidMapping`
- Functions: `SUSER_SNAME()` for audit trail

### Quality Assessment

**Strengths:**
- ✅ **GOLD STANDARD tenant resolution** - Perfect ACID-compliant GUID→INT mapping
- ✅ **Race condition prevention** - UPDLOCK/HOLDLOCK pattern
- ✅ **Double-check locking** - Reads outside transaction, writes inside
- ✅ **Error handling** - TRY/CATCH with ROLLBACK
- ✅ **Audit trail** - Logs new tenant registration with RAISERROR(...WITH LOG)
- ✅ **Return codes** - 0=success, 1=not found (no auto-register)
- ✅ **SET XACT_ABORT ON** - Ensures transaction consistency
- ✅ **IsActive check** - Prevents disabled tenants
- ✅ **Extended properties** - MS_Description for documentation
- ✅ **THROW in CATCH** - Preserves original error info

**Weaknesses:**
- ⚠️ **No tenant name update** - If tenant exists but name changed, not updated
- ⚠️ **NOLOCK on first read** - Could return stale IsActive=0 (rare edge case)

**Performance:**
- Fast path with NOLOCK (no blocking)
- Transaction only when needed (auto-register)
- SCOPE_IDENTITY() for last inserted ID

**Security:**
- ✅ Tracks user via SUSER_SNAME()
- ✅ Prevents unauthorized tenant creation when @AutoRegister=0

### Improvement Recommendations
1. **Priority 3:** Update TenantName if changed (MERGE instead of INSERT-only)
2. **Priority 4:** Consider READCOMMITTEDLOCK hint instead of NOLOCK (safer)
3. **Priority 5:** Add tenant deactivation check (IsActive=0 after initial read)

**Notes:**
- This is a **GOLD STANDARD** example of ACID-compliant GUID resolution
- Replaces unsafe GetHashCode() approach mentioned in comments
- Pattern should be reused for other GUID→INT mappings

---

## 3. sp_QueryLineage

**Location:** `src/Hartonomous.Database/Procedures/dbo.sp_QueryLineage.sql`  
**Type:** Stored Procedure  
**Lines:** ~80  
**Quality Score:** 84/100

### Purpose
Query atom lineage (ancestry/descendants) using recursive graph traversal. Supports upstream (ancestors), downstream (descendants), or both directions.

### Parameters
- `@AtomId BIGINT` - Starting atom for lineage query
- `@Direction NVARCHAR(20) = 'Upstream'` - Traversal direction (Upstream, Downstream, Both)
- `@MaxDepth INT = 10` - Maximum recursion depth
- `@TenantId INT = 0` - Multi-tenancy

### Architecture

**Recursive CTE Traversal:**
1. **Upstream:** Start at atom, follow FromAtomId←ToAtomId edges (ancestors)
2. **Downstream:** Start at atom, follow FromAtomId→ToAtomId edges (descendants)
3. Track depth and path string for visualization
4. Join to Atom table for metadata
5. Filter by TenantId

### Key Operations

**Upstream Lineage (Ancestors):**
```sql
WITH UpstreamLineage AS (
    SELECT @AtomId AS AtomId, 0 AS Depth, CAST(@AtomId AS NVARCHAR(MAX)) AS Path
    UNION ALL
    SELECT edge.FromAtomId, ul.Depth + 1,
           CAST(edge.FromAtomId AS NVARCHAR(MAX)) + ' -> ' + ul.Path
    FROM UpstreamLineage ul
    INNER JOIN provenance.AtomGraphEdges edge ON ul.AtomId = edge.ToAtomId
    WHERE ul.Depth < @MaxDepth
)
```

### Dependencies
- Tables: `provenance.AtomGraphEdges`, `Atom`
- Schema: `provenance` schema

### Quality Assessment

**Strengths:**
- ✅ **Bidirectional traversal** - Upstream, downstream, or both
- ✅ **Cycle prevention** - @MaxDepth limit (prevents infinite loops)
- ✅ **Multi-tenant safe** - TenantId filtering on final output
- ✅ **Error handling** - TRY/CATCH with RAISERROR
- ✅ **Path tracking** - String path for visualization
- ✅ **Return codes** - 0=success, -1=error
- ✅ **Depth tracking** - Useful for UI rendering

**Weaknesses:**
- ⚠️ **No cycle detection** - If graph has cycles, @MaxDepth is only protection
- ⚠️ **Schema mismatch** - Uses ContentType (Atom table has Modality/Subtype)
- ⚠️ **Path concatenation inefficient** - NVARCHAR(MAX) grows exponentially
- ⚠️ **No TenantId in CTE** - Filters only at final JOIN (could traverse cross-tenant edges)
- ⚠️ **No authorization** - Any user can query any atom's lineage
- ⚠️ **No visited set** - Could revisit same atom via different paths
- ⚠️ **Both direction returns 2 result sets** - Harder to consume

**Performance:**
- Recursive CTEs can be expensive
- No index hints provided
- Path string concatenation grows O(N²)
- Missing TenantId in CTE means more rows traversed

**Security:**
- ⚠️ TenantId filter only at final JOIN (could leak edge existence)
- ⚠️ No authorization check

### Improvement Recommendations
1. **Priority 1:** Add TenantId to CTE WHERE clause (traverse only tenant's edges)
2. **Priority 2:** Add cycle detection (track visited AtomIds)
3. **Priority 3:** Fix schema references (ContentType → Modality/Subtype)
4. **Priority 4:** Return single result set for "Both" (UNION ALL)
5. **Priority 5:** Add authorization check (user can access atom)
6. **Priority 6:** Add OPTION (MAXRECURSION) hint for safety
7. **Priority 7:** Consider storing path as JSON array instead of string

---

## 4. sp_QueryModelWeights

**Location:** `src/Hartonomous.Database/Procedures/dbo.sp_QueryModelWeights.sql`  
**Type:** Stored Procedure  
**Lines:** ~30  
**Quality Score:** 80/100

### Purpose
Query tensor atoms and coefficients for a model, with optional layer and atom type filtering. Returns hierarchical weight structure.

### Parameters
- `@ModelId INT` - Model to query weights for
- `@LayerIdx INT = NULL` - Optional layer filter
- `@atom_type NVARCHAR(128) = NULL` - Optional atom type filter

### Architecture

**Query Flow:**
1. JOIN TensorAtom → ModelLayer → Atom → TensorAtomCoefficient
2. LEFT JOIN to parent layer for hierarchy
3. Filter by ModelId, LayerIdx (optional), atom_type (optional)
4. Order by layer index, importance score, tensor role

### Key Operations

**Hierarchical Join:**
```sql
SELECT ta.TensorAtomId, ml.LayerIdx, ml.LayerType, ta.AtomType, 
       ta.ImportanceScore, coeff.ParentLayerId, mlParent.LayerIdx AS ParentLayerIdx,
       coeff.TensorRole, coeff.Coefficient
FROM dbo.TensorAtom AS ta
INNER JOIN dbo.ModelLayer AS ml ON ml.LayerId = ta.LayerId
INNER JOIN dbo.Atom AS a ON a.AtomId = ta.AtomId
LEFT JOIN dbo.TensorAtomCoefficient AS coeff ON coeff.TensorAtomId = ta.TensorAtomId
LEFT JOIN dbo.ModelLayer AS mlParent ON mlParent.LayerId = coeff.ParentLayerId
```

### Dependencies
- Tables: `TensorAtom`, `ModelLayer`, `Atom`, `TensorAtomCoefficient`

### Quality Assessment

**Strengths:**
- ✅ **Hierarchical structure** - Shows layer→parent layer relationships
- ✅ **Flexible filtering** - Optional layer and atom type
- ✅ **Importance ordering** - ORDER BY ImportanceScore DESC
- ✅ **Coefficient details** - TensorRole and numeric coefficient
- ✅ **Metadata included** - TensorMetadata JSON field

**Weaknesses:**
- ⚠️ **No multi-tenancy** - Missing TenantId filtering (cross-tenant weight visibility)
- ⚠️ **No authorization** - Any user can query any model's weights
- ⚠️ **No error handling** - Missing TRY/CATCH
- ⚠️ **No paging** - Could return millions of rows for large models
- ⚠️ **No model validation** - Doesn't check ModelId exists
- ⚠️ **No coefficient count limit** - Multiple coefficients per tensor atom (cartesian explosion risk)

**Performance:**
- Multiple JOINs (5 tables)
- No explicit index hints
- Could benefit from covering index on (ModelId, LayerIdx)

**Security:**
- 🔴 No TenantId filtering (cross-tenant data leak)
- ⚠️ No authorization check

### Improvement Recommendations
1. **Priority 1:** Add TenantId filtering throughout (CRITICAL security)
2. **Priority 2:** Add authorization check (user can access model)
3. **Priority 3:** Add paging (@Offset, @Limit parameters)
4. **Priority 4:** Add TRY/CATCH error handling
5. **Priority 5:** Validate ModelId exists
6. **Priority 6:** Add query hint for expected index usage
7. **Priority 7:** Consider returning coefficient arrays as JSON (reduce rows)

---

## 5. sp_InferenceHistory

**Location:** `src/Hartonomous.Database/Procedures/dbo.sp_InferenceHistory.sql`  
**Type:** Stored Procedure  
**Lines:** ~30  
**Quality Score:** 74/100

### Purpose
Analyze inference request history over time window. Returns statistics grouped by task type (counts, durations, success/failure, cache hits).

### Parameters
- `@time_window_hours INT = 24` - Time window for analysis (default 24 hours)
- `@TaskType NVARCHAR(50) = NULL` - Optional task type filter

### Architecture

**Analysis Flow:**
1. Calculate cutoff timestamp (NOW - @time_window_hours)
2. Aggregate InferenceRequest by TaskType
3. Calculate metrics: count, avg/min/max duration, success/fail, cache hits
4. Order by request count descending

### Key Operations

**Aggregation Query:**
```sql
SELECT TaskType, COUNT(*) AS request_count,
       AVG(TotalDurationMs) AS avg_duration_ms,
       MIN(TotalDurationMs) AS min_duration_ms,
       MAX(TotalDurationMs) AS max_duration_ms,
       SUM(CASE WHEN OutputData IS NOT NULL THEN 1 ELSE 0 END) AS successful_count,
       SUM(CASE WHEN OutputData IS NULL THEN 1 ELSE 0 END) AS failed_count,
       SUM(CASE WHEN CacheHit = 1 THEN 1 ELSE 0 END) AS cache_hits
FROM dbo.InferenceRequest
WHERE RequestTimestamp >= @cutoff_time AND (@TaskType IS NULL OR TaskType = @TaskType)
GROUP BY TaskType
ORDER BY request_count DESC;
```

### Dependencies
- Tables: `InferenceRequest`

### Quality Assessment

**Strengths:**
- ✅ **Useful metrics** - Count, duration stats, success/fail, cache efficiency
- ✅ **Flexible time window** - Configurable hours
- ✅ **Optional task filter** - Analyze specific task types
- ✅ **Clear output** - PRINT statements for user feedback
- ✅ **Simple aggregation** - Easy to understand query

**Weaknesses:**
- ⚠️ **No multi-tenancy** - Missing TenantId filtering (cross-tenant metrics visibility)
- ⚠️ **No error handling** - Missing TRY/CATCH
- ⚠️ **OutputData NULL check flawed** - Assumes NULL=failed (could be valid empty output)
- ⚠️ **No authorization** - Any user can see all inference history
- ⚠️ **No percentiles** - Should include p50/p95/p99 latency
- ⚠️ **PRINT statements** - Better to return metadata as result set

**Performance:**
- Single aggregation query (fast)
- Needs index on (RequestTimestamp, TaskType) for efficiency
- No OPTION hints provided

**Security:**
- 🔴 No TenantId filtering (cross-tenant visibility)
- ⚠️ No authorization check

### Improvement Recommendations
1. **Priority 1:** Add TenantId filtering (CRITICAL security)
2. **Priority 2:** Add authorization check (admin role required)
3. **Priority 3:** Add percentile calculations (p50, p95, p99)
4. **Priority 4:** Fix success/fail detection (check ErrorMessage field instead of OutputData NULL)
5. **Priority 5:** Add TRY/CATCH error handling
6. **Priority 6:** Return metadata as result set instead of PRINT
7. **Priority 7:** Add time-series bucketing option (hourly, daily)
8. **Priority 8:** Include ModelId breakdown (which models used most)

---

## Summary Statistics

**Files Analyzed:** 5  
**Total Lines:** ~250  
**Average Quality:** 81.2/100

**Quality Distribution:**
- Excellent (85-100): 1 file (sp_ResolveTenantGuid 92 ⭐)
- Good (70-84): 4 files (sp_QueryLineage 84, sp_QueryModelWeights 80, sp_EnqueueIngestion 76, sp_InferenceHistory 74)

**Key Patterns:**
- **Service Broker integration** - sp_EnqueueIngestion enqueues files for async processing
- **ACID-compliant GUID resolution** - sp_ResolveTenantGuid is GOLD STANDARD with race condition prevention
- **Graph traversal** - sp_QueryLineage uses recursive CTEs for lineage
- **Analytics queries** - sp_QueryModelWeights (weights), sp_InferenceHistory (metrics)

**Security Issues:**
- 🔴 **sp_EnqueueIngestion** - No authorization, no encryption, no file validation
- 🔴 **sp_QueryLineage** - Missing TenantId in CTE (traverses cross-tenant edges)
- 🔴 **sp_QueryModelWeights** - No TenantId filtering (cross-tenant weight visibility)
- 🔴 **sp_InferenceHistory** - No TenantId filtering (cross-tenant metrics)

**Performance Issues:**
- sp_EnqueueIngestion: Binary-to-hex doubles message size
- sp_QueryLineage: Path string concatenation (O(N²)), no TenantId in CTE
- sp_QueryModelWeights: No paging for large result sets
- sp_InferenceHistory: Missing index hint for time range query

**Schema Mismatches:**
- sp_QueryLineage: Uses ContentType (should be Modality/Subtype)

**Missing Features:**
- sp_EnqueueIngestion: File size limits, deduplication
- sp_QueryLineage: Cycle detection, visited set
- sp_QueryModelWeights: Paging, result set limits
- sp_InferenceHistory: Percentile metrics, time-series bucketing

**Critical Issues:**
1. 4 procedures missing multi-tenancy (security vulnerability)
2. sp_EnqueueIngestion allows unlimited file sizes (DoS risk)
3. No authorization checks in any procedure (access control gap)

**Recommendations:**
1. Add TenantId filtering to all procedures (Priority 1 - Security)
2. Add authorization checks (Priority 1 - Security)
3. Add file size limit to sp_EnqueueIngestion (Priority 1 - DoS prevention)
4. Fix TenantId in sp_QueryLineage CTE (Priority 1 - Security)
5. Add error handling (TRY/CATCH) to all procedures (Priority 2)
6. Fix schema references (ContentType → Modality/Subtype) (Priority 2)
7. Add paging to analytics queries (Priority 3 - Performance)

**Gold Standard:**
- ✅ **sp_ResolveTenantGuid (92/100)** - Perfect ACID-compliant tenant resolution with race condition prevention. Use as reference for other GUID→INT mappings.
