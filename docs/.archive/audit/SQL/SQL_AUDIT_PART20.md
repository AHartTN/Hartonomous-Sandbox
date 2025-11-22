# SQL Audit Part 20: Utility & Service Procedures

## Overview
Part 20 analyzes 5 procedures: Neo4j sync, migration utilities, billing, cognitive activation, and image search.

---

## 1. sp_ForwardToNeo4j_Activated

**Location:** `src/Hartonomous.Database/Procedures/dbo.sp_ForwardToNeo4j_Activated.sql`  
**Type:** Service Broker Activation Procedure  
**Lines:** ~165  
**Quality Score:** 76/100

### Purpose
Service Broker activation procedure to forward SQL Server entities (Atoms, GenerationStreams, AtomProvenance) to Neo4j graph database. Processes Neo4jSyncQueue messages.

### Parameters
None (Service Broker activation procedure)

### Architecture

**Message Processing Loop:**
1. WAITFOR RECEIVE message from Neo4jSyncQueue
2. Parse XML message (EntityType, EntityId, SyncType)
3. Build Cypher query based on entity type
4. Execute simulated Neo4j REST call
5. Log sync result to Neo4jSyncLog
6. END CONVERSATION

**Supported Entity Types:**
- **Atom:** MERGE node with metadata
- **GenerationStream:** MERGE stream + GENERATED relationships
- **AtomProvenance:** MERGE atom-to-atom relationship edges

### Key Operations

**Atom Sync:**
```sql
SELECT @CypherQuery = 
    'MERGE (a:Atom {atomId: $atomId}) ' +
    'SET a.contentType = $contentType, ' +
    '    a.contentHash = $contentHash, ' +
    '    a.CreatedAt = $CreatedAt, ' +
    '    a.metadata = $metadata'
FROM dbo.Atom
WHERE AtomId = @EntityId;
```

**Simulated External Call:**
```sql
SET @ResponseJson = '{"status": "simulated_success", "entityType": "' + @EntityType + '", "entityId": ' + CAST(@EntityId AS NVARCHAR(MAX)) + '}';
```

### Dependencies
- Service Broker: Neo4jSyncQueue, Neo4jSyncRequest message type
- Tables: `Atom`, `provenance.GenerationStreams`, `graph.AtomGraphEdges`, `Neo4jSyncLog`
- External: Neo4j REST API (not implemented, uses placeholder)

### Quality Assessment

**Strengths:**
- ✅ **Service Broker pattern** - Proper WAITFOR/RECEIVE loop
- ✅ **Error handling** - TRY/CATCH with error logging
- ✅ **Transaction management** - BEGIN/COMMIT per message
- ✅ **EndDialog handling** - Handles system message types
- ✅ **Logging** - Success and failure logged to Neo4jSyncLog
- ✅ **Multiple entity types** - Atom, GenerationStream, AtomProvenance
- ✅ **Parameterized Cypher** - Uses $atomId, $contentType syntax

**Weaknesses:**
- 🔴 **Simulated implementation** - Returns mock JSON, no actual Neo4j call
- ⚠️ **No TenantId filtering** - Entity queries don't filter by tenant
- ⚠️ **graph.AtomGraphEdges schema** - Uses old schema (MUST be provenance.AtomGraphEdges)
- ⚠️ **ContentHash/ContentType** - Atom schema mismatch (MUST use Modality/Subtype)
- ⚠️ **No batching** - Processes 1 message per transaction (inefficient)
- ⚠️ **TIMEOUT 1000** - 1 second timeout, exits if no messages
- ⚠️ **No retry logic** - Failed syncs not retried
- ⚠️ **sp_invoke_external_rest_endpoint** - Placeholder comment, not implemented
- ⚠️ **AtomRelationId assumption** - Assumes EntityId maps to AtomRelationId (unclear)

**Performance:**
- Single message processing (no batching)
- XML parsing overhead
- WAITFOR 1 second timeout

**Security:**
- ⚠️ No TenantId filtering (MUST sync cross-tenant data)
- ✅ Error messages don't expose sensitive data

### REQUIRED FIXES
1. **CRITICAL:** Implement actual Neo4j REST call (CLR or external service)
2. **URGENT:** Add TenantId filtering to entity queries
3. **REQUIRED:** Fix schema references (graph.AtomGraphEdges → provenance.AtomGraphEdges)
4. **IMPLEMENT:** Add batching (process multiple messages per transaction)
5. **IMPLEMENT:** Add retry logic for failed syncs
6. **IMPLEMENT:** Fix Atom schema references (ContentHash/ContentType)
7. **IMPLEMENT:** Increase timeout or make configurable
8. **IMPLEMENT:** Add metrics (sync duration, message rate)

---

## 2. sp_MigratePayloadLocatorToFileStream

**Location:** `src/Hartonomous.Database/Procedures/dbo.sp_MigratePayloadLocatorToFileStream.sql`  
**Type:** Stored Procedure (Migration/Deprecated)  
**Lines:** ~65  
**Quality Score:** N/A (Deprecated)

### Purpose
**DEPRECATED:** Migrate PayloadLocator (file paths) to FILESTREAM blobs. No longer needed in Core v5 (atomic decomposition only).

### Parameters
- `@BatchSize INT = 100` - Batch size for progress reporting
- `@Debug BIT = 0` - Debug output

### Architecture

**Migration Flow (Deprecated):**
1. RAISERROR('This procedure is deprecated. PayloadLocator removed in Core v5.', 16, 1)
2. Return -1
3. Legacy code: Read file bytes via CLR, insert into Atom table

### Key Operations

**Immediate Deprecation Error:**
```sql
RAISERROR('This procedure is deprecated. PayloadLocator removed in Core v5.', 16, 1);
RETURN -1;
```

### Dependencies
- Tables: `graph.AtomGraphNodes` (old schema), `Atom`
- CLR: `dbo.clr_ReadFileBytes` (reads file from disk)

### Quality Assessment

**Strengths:**
- ✅ **Properly deprecated** - Immediately throws error
- ✅ **Clear message** - Explains PayloadLocator removed in Core v5
- ✅ **Preserved history** - Legacy code retained as comment

**Weaknesses:**
- N/A (deprecated procedure)

**Notes:**
- PayloadLocator was file path storage mechanism (pre-atomic decomposition)
- Core v5 uses pure atomic values (no blob storage)
- Legacy migration code preserved for historical reference
- Can be removed in future cleanup

### REQUIRED FIXES
1. **REQUIRED:** Remove from database (cleanup deprecated objects)
2. **IMPLEMENT:** Document migration history (if needed for rollback scenarios)

---

## 3. sp_InsertBillingUsageRecord_Native

**Location:** `src/Hartonomous.Database/Procedures/Billing.InsertUsageRecord_Native.sql`  
**Type:** Natively Compiled Stored Procedure  
**Lines:** ~58  
**Quality Score:** 88/100 ⭐

### Purpose
High-performance, natively compiled procedure for inserting billing records into memory-optimized table. Lock-free, latch-free insert performance.

### Parameters
- `@TenantId NVARCHAR(128)` - Tenant identifier
- `@PrincipalId NVARCHAR(256)` - User/service principal
- `@Operation NVARCHAR(128)` - Billable operation
- `@MessageType NVARCHAR(128) = NULL` - Optional message type
- `@Handler NVARCHAR(256) = NULL` - Handler name
- `@Units DECIMAL(18,6)` - Usage units
- `@BaseRate DECIMAL(18,6)` - Base rate per unit
- `@Multiplier DECIMAL(18,6) = 1.0` - Rate multiplier
- `@TotalCost DECIMAL(18,6)` - Calculated total cost
- `@MetadataJson NVARCHAR(MAX) = NULL` - Optional metadata

### Architecture

**Natively Compiled:**
```sql
WITH NATIVE_COMPILATION, SCHEMABINDING, EXECUTE AS OWNER
AS
BEGIN ATOMIC WITH
(
    TRANSACTION ISOLATION LEVEL = SNAPSHOT,
    LANGUAGE = N'us_english'
)
```

**Single INSERT:**
- Direct insert into memory-optimized table
- No validation logic (fast path)
- SYSUTCDATETIME() for timestamp

### Dependencies
- Table: `BillingUsageLedger_InMemory` (memory-optimized)

### Quality Assessment

**Strengths:**
- ✅ **Natively compiled** - Lock-free, latch-free performance
- ✅ **SNAPSHOT isolation** - Optimistic concurrency
- ✅ **SCHEMABINDING** - Prevents table modifications
- ✅ **Simple INSERT** - No complex logic (fast)
- ✅ **Memory-optimized table** - High throughput
- ✅ **All parameters captured** - Complete billing record
- ✅ **JSON metadata** - Flexible extensibility

**Weaknesses:**
- ⚠️ **No validation** - No parameter checks (trusts caller)
- ⚠️ **No return value** - Doesn't return RecordId or success indicator
- ⚠️ **No error handling** - TRY/CATCH not allowed in native procs (by design)
- ⚠️ **TenantId NVARCHAR(128)** - MUST be INT for consistency
- ⚠️ **No duplicate check** - MUST insert same usage twice
- ⚠️ **TotalCost passed in** - Not computed from Units * BaseRate * Multiplier (MUST be inconsistent)

**Performance:**
- ✅ Natively compiled = microsecond latency
- ✅ Memory-optimized = no disk I/O
- ✅ Lock-free = high concurrency

**Security:**
- ✅ EXECUTE AS OWNER (consistent permissions)
- ⚠️ No TenantId validation (trusts caller)

### REQUIRED FIXES
1. **REQUIRED:** Add validation wrapper (non-native proc calls native proc)
2. **IMPLEMENT:** Return RecordId via OUTPUT parameter
3. **IMPLEMENT:** Change TenantId to INT (match schema)
4. **IMPLEMENT:** Compute TotalCost in procedure (ensure consistency)
5. **IMPLEMENT:** Add idempotency key for duplicate prevention

**Notes:**
- This is a **GOLD STANDARD** for high-throughput insert operations
- Used for billing telemetry (high volume, low latency)
- Validation MUST be in calling code (not in hot path)

---

## 4. sp_CognitiveActivation

**Location:** `src/Hartonomous.Database/Procedures/dbo.sp_CognitiveActivation.sql`  
**Type:** Stored Procedure  
**Lines:** ~78  
**Quality Score:** 84/100

### Purpose
Cognitive activation search: Find atom embeddings that "fire" based on cosine similarity to query embedding. Simulates neural activation patterns.

### Parameters
- `@query_embedding VECTOR(1998)` - Query vector
- `@activation_threshold FLOAT = 0.8` - Minimum activation strength (0-1)
- `@max_activated INT = 50` - Maximum atoms to activate

### Architecture

**Activation Process:**
1. Validate query embedding and threshold
2. Calculate max cosine distance (1.0 - threshold)
3. Find top K atoms within distance threshold
4. Classify activation strength (VERY_HIGH/HIGH/MEDIUM/LOW)
5. Log inference request
6. Return activated atoms with metadata

**Activation Levels:**
- **VERY_HIGH:** ≥ 0.95
- **HIGH:** ≥ 0.90
- **MEDIUM:** ≥ 0.85
- **LOW:** < 0.85

### Key Operations

**Vector Similarity Search:**
```sql
INSERT INTO @activated (AtomEmbeddingId, AtomId, ActivationStrength)
SELECT TOP (@max_activated)
    ae.AtomEmbeddingId,
    ae.AtomId,
    1.0 - VECTOR_DISTANCE('cosine', ae.EmbeddingVector, @query_embedding) AS ActivationStrength
FROM dbo.AtomEmbedding AS ae
WHERE ae.EmbeddingVector IS NOT NULL
  AND VECTOR_DISTANCE('cosine', ae.EmbeddingVector, @query_embedding) <= @max_distance
ORDER BY VECTOR_DISTANCE('cosine', ae.EmbeddingVector, @query_embedding) ASC;
```

**Inference Logging:**
```sql
INSERT INTO dbo.InferenceRequest (TaskType, InputData, ModelsUsed, EnsembleStrategy, OutputData, OutputMetadata, TotalDurationMs)
VALUES (
    'cognitive_activation',
    CAST(@input_json AS NVARCHAR(MAX)),
    'atom_embeddings',
    'cognitive_activation',
    CAST(@output_json AS NVARCHAR(MAX)),
    CAST(@output_metadata AS NVARCHAR(MAX)),
    @DurationMs
);
```

### Dependencies
- Tables: `AtomEmbedding`, `Atom`, `InferenceRequest`
- Indexes: Vector index on AtomEmbedding.EmbeddingVector

### Quality Assessment

**Strengths:**
- ✅ **Parameter validation** - NULL checks, threshold range validation
- ✅ **THROW for errors** - Proper error handling
- ✅ **Cognitive metaphor** - "Firing" neurons concept
- ✅ **Activation levels** - Clear categorization
- ✅ **Inference logging** - Tracks usage in InferenceRequest
- ✅ **Performance metrics** - Duration tracking
- ✅ **JSON metadata** - Structured input/output
- ✅ **PRINT statements** - User feedback
- ✅ **Vector distance optimization** - Single scan with WHERE clause

**Weaknesses:**
- ⚠️ **No multi-tenancy** - Missing TenantId filtering
- ⚠️ **Double VECTOR_DISTANCE** - Computed in INSERT and ORDER BY
- ⚠️ **Threshold validation flaw** - Allows -1.0 (MUST be > 0 or >= 0)
- ⚠️ **VECTOR(1998)** - Hardcoded dimension (MUST match AtomEmbedding schema)
- ⚠️ **No authorization** - Anyone can query all embeddings
- ⚠️ **No vector index hint** - Depends on optimizer

**Performance:**
- VECTOR_DISTANCE computed twice (inefficiency)
- TOP with ORDER BY is efficient
- Vector index usage depends on query plan

**Security:**
- ⚠️ No TenantId filtering (cross-tenant visibility)
- ⚠️ No authorization check

### REQUIRED FIXES
1. **CRITICAL:** Add TenantId filtering (critical security)
2. **URGENT:** Use CTE to compute VECTOR_DISTANCE once
3. **URGENT:** Add authorization check
4. **REQUIRED:** Fix threshold validation (MUST be > 0)
5. **REQUIRED:** Validate @query_embedding dimension matches schema
6. **IMPLEMENT:** Add vector index hint
7. **IMPLEMENT:** Make vector dimension configurable or schema-driven

---

## 5. sp_FindImagesByColor

**Location:** `src/Hartonomous.Database/StoredProcedures/dbo.sp_FindImagesByColor.sql`  
**Type:** Stored Procedure  
**Lines:** ~52  
**Quality Score:** 78/100

### Purpose
Find images containing pixels in specified RGB color range. Uses spatial query on atomic pixel decomposition.

### Parameters
- `@minR TINYINT` - Minimum red (0-255)
- `@maxR TINYINT` - Maximum red (0-255)
- `@minG TINYINT` - Minimum green (0-255)
- `@maxG TINYINT` - Maximum green (0-255)
- `@minB TINYINT` - Minimum blue (0-255)
- `@maxB TINYINT` - Maximum blue (0-255)
- `@minOccurrences INT = 1` - Minimum pixel count
- `@maxResults INT = 100` - Result limit

### Architecture

**Query Flow:**
1. **MatchingPixels CTE:** Find pixels in RGB range
2. **ImageMatches CTE:** Group by source image, count pixels
3. **Final SELECT:** Join to Atom table, return image metadata

### Key Operations

**RGB Range Filter:**
```sql
SELECT 
    p.PixelHash,
    p.R, p.G, p.B, p.A,
    p.ReferenceCount
FROM dbo.AtomicPixels p
WHERE p.R BETWEEN @minR AND @maxR
  AND p.G BETWEEN @minG AND @maxG
  AND p.B BETWEEN @minB AND @maxB
```

**Image Composition Join:**
```sql
SELECT 
    ac.SourceAtomId AS ImageAtomId,
    COUNT(*) AS PixelCount,
    COUNT(DISTINCT mp.PixelHash) AS UniqueColors
FROM dbo.AtomCompositions ac
INNER JOIN dbo.Atom a ON ac.ComponentAtomId = a.AtomId
INNER JOIN MatchingPixels mp ON mp.PixelHash = a.ContentHash
WHERE ac.ComponentType = 'pixel'
GROUP BY ac.SourceAtomId
HAVING COUNT(*) >= @minOccurrences
```

### Dependencies
- Tables: `AtomicPixels`, `AtomCompositions`, `Atom`
- Indexes: RGB spatial index on AtomicPixels, index on AtomCompositions

### Quality Assessment

**Strengths:**
- ✅ **RGB range query** - Flexible color search
- ✅ **Atomic pixel model** - Uses decomposition architecture
- ✅ **Occurrence threshold** - Filters noise
- ✅ **UniqueColors count** - Useful metric
- ✅ **Reference count** - Tracks pixel reuse
- ✅ **TOP limit** - Prevents runaway results
- ✅ **Ordered output** - By pixel count (most matching first)

**Weaknesses:**
- ⚠️ **No multi-tenancy** - Missing TenantId filtering
- ⚠️ **ContentHash join** - Atom.ContentHash doesn't exist (schema mismatch)
- ⚠️ **No authorization** - Anyone can search all images
- ⚠️ **No error handling** - Missing TRY/CATCH
- ⚠️ **ComponentType hardcoded** - 'pixel' string literal
- ⚠️ **AtomCompositions schema** - will not exist or have different structure
- ⚠️ **No alpha channel filter** - Only filters RGB, not A

**Performance:**
- RGB BETWEEN MUST be slow without index
- Multiple JOINs and GROUP BY
- COUNT DISTINCT can be expensive

**Security:**
- ⚠️ No TenantId filtering
- ⚠️ No authorization check

### REQUIRED FIXES
1. **CRITICAL:** Verify AtomCompositions schema exists
2. **CRITICAL:** Fix ContentHash join (schema mismatch)
3. **URGENT:** Add TenantId filtering
4. **URGENT:** Add authorization check
5. **REQUIRED:** Add TRY/CATCH error handling
6. **IMPLEMENT:** Add alpha channel filter option (@minA, @maxA)
7. **IMPLEMENT:** Add index hints for RGB range query
8. **IMPLEMENT:** IMPLEMENT HSV/HSL color space option

---

## Summary Statistics

**Files Analyzed:** 5  
**Total Lines:** ~418 (excluding deprecated)  
**Average Quality:** 81.5/100

**Quality Distribution:**
- Excellent (85-100): 1 file (sp_InsertBillingUsageRecord_Native 88⭐)
- Good (70-84): 3 files (sp_CognitiveActivation 84, sp_FindImagesByColor 78, sp_ForwardToNeo4j_Activated 76)
- Deprecated: 1 file (sp_MigratePayloadLocatorToFileStream N/A)

**Key Patterns:**
- **Service Broker activation** - sp_ForwardToNeo4j_Activated
- **Natively compiled** - sp_InsertBillingUsageRecord_Native (high-performance billing)
- **Vector similarity** - sp_CognitiveActivation (cognitive metaphor)
- **Atomic decomposition** - sp_FindImagesByColor (pixel-based search)
- **Deprecated migration** - sp_MigratePayloadLocatorToFileStream (Core v5 removed blob storage)

**Security Issues:**
- ⚠️ 3 procedures missing TenantId filtering (sp_ForwardToNeo4j_Activated, sp_CognitiveActivation, sp_FindImagesByColor)
- ⚠️ No authorization checks in any procedure
- ⚠️ sp_InsertBillingUsageRecord_Native trusts caller (no validation)

**Performance Issues:**
- VECTOR_DISTANCE computed twice in sp_CognitiveActivation
- No batching in sp_ForwardToNeo4j_Activated
- RGB range query in sp_FindImagesByColor MUST be slow

**Schema Issues:**
- 🔴 **sp_ForwardToNeo4j_Activated** - Uses ContentHash/ContentType (Atom schema mismatch)
- 🔴 **sp_ForwardToNeo4j_Activated** - Uses graph.AtomGraphEdges (MUST be provenance.AtomGraphEdges)
- 🔴 **sp_FindImagesByColor** - Joins on Atom.ContentHash (column doesn't exist)
- ⚠️ **AtomCompositions table** - Not verified to exist

**Incomplete Implementations:**
- 🔴 **sp_ForwardToNeo4j_Activated** - Simulated Neo4j API (returns mock JSON)

**Critical Issues:**
1. sp_ForwardToNeo4j_Activated returns mock data (no actual Neo4j sync)
2. Schema mismatches in 2 procedures (ContentHash, ContentType, graph.AtomGraphEdges)
3. Missing TenantId filtering in 3 procedures (security vulnerability)
4. AtomCompositions table existence not verified

**Recommendations:**
1. Implement actual Neo4j REST call in sp_ForwardToNeo4j_Activated (Priority 1 - Functionality)
2. Fix schema references in all procedures (Priority 1 - Correctness)
3. Add TenantId filtering to all procedures (Priority 1 - Security)
4. Verify AtomCompositions table schema (Priority 1 - Correctness)
5. Add authorization checks to all procedures (Priority 2 - Security)
6. Optimize VECTOR_DISTANCE to compute once (Priority 2 - Performance)
7. Add batching to sp_ForwardToNeo4j_Activated (Priority 3 - Performance)

**Gold Standard:**
- ✅ **sp_InsertBillingUsageRecord_Native (88/100)** - Perfect example of natively compiled, high-throughput insert procedure. Use for other high-volume telemetry.

---

## Cross-Reference: Schema Mismatches (Running Total)

**CRITICAL FINDING:** Multiple procedures across Parts 19-20 reference non-existent columns:

**Part 19 (3 procedures):**
- sp_FindRelatedDocuments: ContentHash, ContentType
- sp_FindImpactedAtoms: ContentHash, ContentType
- sp_SemanticSimilarity: ContentHash, ContentType

**Part 20 (2 procedures):**
- sp_ForwardToNeo4j_Activated: ContentHash, ContentType
- sp_FindImagesByColor: ContentHash (join)

**Total Impact: 5 procedures** (will fail at runtime)

**Fix Required:**
```sql
-- OLD (WRONG):
a.ContentHash, a.ContentType

-- NEW (CORRECT):
a.Modality, a.Subtype, a.CanonicalText
```

**Action Required:**
1. Global search for "ContentHash" and "ContentType" across all procedures
2. Replace with correct Atom schema columns
3. Test all affected procedures

---

## Part 20 Completion

**Progress Update:**
- Total SQL files: 325
- Analyzed through Part 20: 149 files (45.8%)
- Remaining: 176 files (54.2%)

**Procedure Audit Status:**
- ✅ COMPLETE: ~75 procedure files analyzed across Parts 1-20
- Remaining categories: Tables (~90 files), Functions (~15 files), Service Broker (~5 files), Indexes (~25 files), Scripts (~15 files)

**Next Steps:**
- Parts 21-30: Tables (~90 files) - LARGEST CATEGORY
- Parts 31-32: Functions (~15 files)
- Part 33: Service Broker (~5 files)
- Part 34: Indexes (~25 files)
- Part 35: Scripts (~15 files)

**Major Findings Summary (Parts 1-20):**
1. 🔴 sp_Learn MISSING (OODA Phase 4 - CRITICAL)
2. 🔴 InferenceRequest missing 4 columns (blocks sp_RunInference)
3. 🔴 Schema mismatches: 5+ procedures use ContentHash/ContentType
4. 🔴 Neo4j sync not implemented (mock data only)
5. ⚠️ Multi-tenancy gaps: 10+ procedures missing TenantId filtering
6. ⚠️ Authorization gaps: No procedures check user permissions
7. ⚠️ 3+ CLR functions undefined (fn_DecompressComponents, fn_GetComponentCount, fn_GetTimeWindow)

**Critical Path Forward:**
1. Fix schema mismatches (5 procedures)
2. Implement sp_Learn (OODA completion)
3. Fix InferenceRequest schema (unblock sp_RunInference)
4. Implement Neo4j sync (sp_ForwardToNeo4j_Activated)
5. Add multi-tenancy to all procedures
6. Analyze tables (Parts 21-30)
