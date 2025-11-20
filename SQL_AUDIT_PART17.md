# SQL Audit Part 17: Provenance, Neo4j Sync & Semantic Features

## Overview
Part 17 analyzes 5 procedures: provenance auditing, Neo4j synchronization (2), semantic feature computation, and semantic similarity search.

---

## 1. sp_AuditProvenanceChain

**Location:** `src/Hartonomous.Database/Procedures/dbo.sp_AuditProvenanceChain.sql`  
**Type:** Stored Procedure  
**Lines:** ~140  
**Quality Score:** 86/100

### Purpose
Audit provenance chains for data quality and integrity. Analyzes operations in date range, validates provenance streams, detects anomalies.

### Parameters
- `@StartDate DATETIME2 = NULL` - Audit period start (default: 7 days ago)
- `@EndDate DATETIME2 = NULL` - Audit period end (default: now)
- `@Scope NVARCHAR(100) = NULL` - Filter by scope
- `@MinValidationScore FLOAT = 0.8` - Minimum acceptable validation score (unused)
- `@Debug BIT = 0` - Debug output

### Architecture

**Audit Pipeline:**
1. Load operations from `OperationProvenance` with validation results
2. Calculate metrics (total, valid, warnings, failures, averages)
3. Detect anomalies:
   - Missing provenance streams
   - Validation failures
   - Unusual segment counts (>2 std deviations)
4. Store audit results in `ProvenanceAuditResults`
5. Return 3 result sets: summary, anomalies, detailed operations

### Key Operations

**UDT Field Access:**
```sql
SELECT op.OperationId, op.ProvenanceStream.Scope, op.ProvenanceStream.Model, 
       op.ProvenanceStream.SegmentCount, pvr.OverallStatus
FROM dbo.OperationProvenance op
LEFT JOIN dbo.ProvenanceValidationResults pvr ON op.OperationId = pvr.OperationId
```

**Anomaly Detection (Statistical):**
```sql
IF @StdDevSegments IS NOT NULL AND EXISTS (
    SELECT 1 FROM @Operations WHERE ABS(SegmentCount - @AvgSegments) > 2 * @StdDevSegments
)
    INSERT INTO @Anomalies VALUES ('Segment Count Anomalies', '...');
```

### Dependencies
- Tables: `OperationProvenance`, `ProvenanceValidationResults`, `ProvenanceAuditResults`
- UDT: `AtomicStream` (ProvenanceStream field)

### Quality Assessment

**Strengths:**
- ✅ **Comprehensive auditing** - Multiple metrics and anomaly detection
- ✅ **Statistical analysis** - Uses STDEV for outlier detection
- ✅ **UDT field access** - ProvenanceStream.Scope, .Model, .SegmentCount
- ✅ **Multiple outputs** - Summary, anomalies, detailed operations
- ✅ **Persistent results** - Stores in ProvenanceAuditResults
- ✅ **Flexible scope** - Date range and scope filtering
- ✅ **JSON anomaly storage** - FOR JSON PATH

**Weaknesses:**
- ⚠️ **MinValidationScore unused** - Parameter not used in logic
- ⚠️ **No multi-tenancy** - Missing TenantId filtering (cross-tenant audit)
- ⚠️ **Hardcoded validation mapping** - PASS=1.0, WARN=0.7, FAIL=0.0
- ⚠️ **ProvenanceStream.IsNull** - Uses CLR UDT method (dependency)
- ⚠️ **No authorization** - Any user can audit any scope
- ⚠️ **TOP 100 limit** - Detailed operations truncated (should be parameter)

**Performance:**
- LEFT JOIN to validation results (efficient)
- Multiple aggregations on table variable (fast for typical sizes)
- Statistical outlier detection is O(N)

**Security:**
- 🔴 No TenantId filtering (cross-tenant data visibility)
- ⚠️ No authorization check

### Improvement Recommendations
1. **Priority 1:** Add TenantId filtering throughout (CRITICAL security)
2. **Priority 2:** Use @MinValidationScore parameter or remove it
3. **Priority 3:** Add authorization check (scope access control)
4. **Priority 4:** Make TOP 100 a parameter (@MaxDetailedResults)
5. **Priority 5:** Load validation score mapping from configuration table
6. **Priority 6:** Add anomaly severity levels
7. **Priority 7:** Consider pagination for large audits

---

## 2. sp_ForwardToNeo4j_Activated

**Location:** `src/Hartonomous.Database/Procedures/dbo.sp_ForwardToNeo4j_Activated.sql`  
**Type:** Stored Procedure (Service Broker Activated)  
**Lines:** ~160  
**Quality Score:** 76/100

### Purpose
Service Broker activation procedure. Processes messages from Neo4jSyncQueue, forwards entities to Neo4j graph database.

### Parameters
None (activated by Service Broker)

### Architecture

**Message Processing Loop:**
1. WAITFOR RECEIVE from Neo4jSyncQueue (1 second timeout)
2. Parse XML message (EntityType, EntityId, SyncType)
3. Build Cypher query based on entity type:
   - **Atom:** MERGE node with properties
   - **GenerationStream:** MERGE stream, create GENERATED edges
   - **AtomProvenance:** MERGE atom-to-atom relationship edges
4. Execute external REST call (simulated - returns mock JSON)
5. Log sync result to Neo4jSyncLog
6. END CONVERSATION
7. Handle EndDialog and Error messages

### Key Operations

**Service Broker RECEIVE:**
```sql
WAITFOR (
    RECEIVE TOP(1) @conversation_handle = conversation_handle,
        @message_body = CAST(message_body AS NVARCHAR(MAX)),
        @message_type_name = message_type_name
    FROM Neo4jSyncQueue
), TIMEOUT 1000;
```

**Cypher Query Building:**
```sql
IF @EntityType = 'Atom'
    SET @CypherQuery = 
        'MERGE (a:Atom {atomId: $atomId}) ' +
        'SET a.contentType = $contentType, a.contentHash = $contentHash, ...';
```

### Dependencies
- Tables: `Neo4jSyncQueue`, `Atom`, `provenance.GenerationStreams`, `graph.AtomGraphEdges`, `Neo4jSyncLog`
- Service Broker: Neo4jSyncService, Neo4jSyncContract
- External: Neo4j REST API (simulated)

### Quality Assessment

**Strengths:**
- ✅ **Service Broker pattern** - Proper WAITFOR/RECEIVE loop
- ✅ **Transaction per message** - BEGIN TRAN/COMMIT per iteration
- ✅ **Message type handling** - EndDialog, Error, Request
- ✅ **Error handling** - TRY/CATCH with logging
- ✅ **Entity type routing** - Atom, GenerationStream, AtomProvenance
- ✅ **Cypher query generation** - Builds Neo4j queries
- ✅ **Conversation cleanup** - END CONVERSATION

**Weaknesses:**
- 🔴 **Simulated external call** - Returns mock JSON, doesn't actually call Neo4j
- ⚠️ **No multi-tenancy** - Missing TenantId filtering in queries
- ⚠️ **Hardcoded endpoint** - bolt://localhost:7687 should be config
- ⚠️ **XML parsing** - Uses XML instead of JSON (more verbose)
- ⚠️ **Schema mismatches** - Uses ContentType, ContentHash (Atom table has different schema)
- ⚠️ **No retry logic** - Failed syncs not retried
- ⚠️ **No batching** - Processes 1 message at a time (could batch)
- ⚠️ **Assumes AtomRelationId** - Comment says "Assuming EntityId maps to..."

**Performance:**
- TIMEOUT 1000ms is reasonable
- TOP(1) message at a time (could optimize with batching)
- No external call latency (simulated)

**Security:**
- ⚠️ No TenantId filtering (could sync cross-tenant data)
- ⚠️ Hardcoded endpoint (no authentication shown)

### Improvement Recommendations
1. **Priority 1:** Implement actual Neo4j REST/Bolt API call (remove simulation)
2. **Priority 2:** Add TenantId filtering to all entity queries
3. **Priority 3:** Load Neo4j endpoint from configuration table
4. **Priority 4:** Switch from XML to JSON message format
5. **Priority 5:** Fix schema references (ContentType → Modality, etc.)
6. **Priority 6:** Add retry logic for failed syncs
7. **Priority 7:** Implement batching (RECEIVE TOP(N))
8. **Priority 8:** Add Neo4j authentication/encryption
9. **Priority 9:** Verify AtomRelationId mapping assumption

---

## 3. sp_EnqueueNeo4jSync

**Location:** `src/Hartonomous.Database/Procedures/dbo.sp_EnqueueNeo4jSync.sql`  
**Type:** Stored Procedure  
**Lines:** ~25  
**Quality Score:** 78/100

### Purpose
Enqueue entity synchronization request to Neo4j. Sends message to Service Broker queue for async processing.

### Parameters
- `@EntityType NVARCHAR(50)` - Entity type (Atom, GenerationStream, AtomProvenance)
- `@EntityId BIGINT` - Entity ID
- `@SyncType NVARCHAR(50) = 'CREATE'` - Sync operation type (CREATE, UPDATE, DELETE)

### Architecture

**Simple Enqueue:**
1. BEGIN DIALOG CONVERSATION
2. Build XML message (EntityType, EntityId, SyncType)
3. SEND message on conversation
4. (Conversation handled by activated procedure)

### Key Operations

**Service Broker SEND:**
```sql
BEGIN DIALOG CONVERSATION @ConversationHandle
    FROM SERVICE [Neo4jSyncService] TO SERVICE 'Neo4jSyncService'
    ON CONTRACT [Neo4jSyncContract] WITH ENCRYPTION = OFF;

SEND ON CONVERSATION @ConversationHandle
    MESSAGE TYPE [Neo4jSyncRequest] (@MessageXml);
```

### Dependencies
- Service Broker: Neo4jSyncService, Neo4jSyncContract, Neo4jSyncRequest message type

### Quality Assessment

**Strengths:**
- ✅ **Simple and focused** - Does one thing well
- ✅ **Async pattern** - Fire-and-forget via Service Broker
- ✅ **XML generation** - FOR XML PATH for structured message
- ✅ **Default sync type** - CREATE is sensible default

**Weaknesses:**
- ⚠️ **No error handling** - Missing TRY/CATCH
- ⚠️ **No validation** - EntityType, EntityId not validated
- ⚠️ **XML format** - Could use JSON (more modern)
- ⚠️ **No return value** - Doesn't indicate success/failure
- ⚠️ **Encryption OFF** - WITH ENCRYPTION = OFF (security concern)
- ⚠️ **No multi-tenancy** - Missing TenantId parameter
- ⚠️ **No conversation end** - Relies on activated procedure to close

**Performance:**
- Fast enqueue operation
- No blocking (async)

**Security:**
- ⚠️ No encryption
- ⚠️ No TenantId (could sync other tenant's entities)
- ⚠️ No authorization check

### Improvement Recommendations
1. **Priority 1:** Add TenantId parameter
2. **Priority 2:** Add error handling (TRY/CATCH)
3. **Priority 3:** Validate EntityType, EntityId, SyncType
4. **Priority 4:** Enable encryption (remove WITH ENCRYPTION = OFF)
5. **Priority 5:** Switch to JSON message format
6. **Priority 6:** Return success indicator
7. **Priority 7:** Add authorization check (user can sync entity?)

---

## 4. sp_ComputeSemanticFeatures

**Location:** `src/Hartonomous.Database/Procedures/dbo.sp_ComputeSemanticFeatures.sql`  
**Type:** Stored Procedure  
**Lines:** ~70  
**Quality Score:** 70/100

### Purpose
Compute semantic features for a single atom embedding. Placeholder implementation with hardcoded values. Used by batch processing procedure.

### Parameters
- `@atom_embedding_id BIGINT` - AtomEmbedding to compute features for

### Architecture

**Computation Flow:**
1. Retrieve atom embedding and canonical text
2. Check if features already exist
3. If exists: UPDATE with new computed values
4. If not: INSERT new semantic features
5. (Currently returns hardcoded placeholder values)

### Key Operations

**UPSERT Pattern:**
```sql
IF EXISTS (SELECT 1 FROM dbo.SemanticFeatures WHERE AtomEmbeddingId = @atom_embedding_id)
    UPDATE dbo.SemanticFeatures SET ComputedAt = SYSUTCDATETIME(), Sentiment = 0.0, ...
ELSE
    INSERT INTO dbo.SemanticFeatures (...) VALUES (...);
```

### Dependencies
- Tables: `AtomEmbedding`, `Atom`, `SemanticFeatures`
- Functions: Should use NLP/ML functions (not implemented)

### Quality Assessment

**Strengths:**
- ✅ **UPSERT pattern** - UPDATE existing or INSERT new
- ✅ **Error handling** - TRY/CATCH with RAISERROR
- ✅ **Clear structure** - Easy to extend with actual computation
- ✅ **Return codes** - 0 for success, -1/-2 for errors

**Weaknesses:**
- 🔴 **Placeholder implementation** - Returns hardcoded 0.0/0.5 values
- ⚠️ **No actual computation** - Comment says "Feature computation would go here"
- ⚠️ **No multi-tenancy** - Missing TenantId filtering
- ⚠️ **@embedding_vector declared unused** - Variable declared but never populated
- ⚠️ **CanonicalText unused** - Retrieved but not used for computation
- ⚠️ **No validation** - Doesn't check embedding exists
- ⚠️ **MERGE would be cleaner** - Could use MERGE instead of IF EXISTS

**Performance:**
- EXISTS check before UPDATE/INSERT (efficient)
- Single row operation (fast)

**Security:**
- ⚠️ No TenantId filtering (could compute features for other tenant's atoms)

### Improvement Recommendations
1. **Priority 1:** Implement actual semantic feature computation (NLP models)
2. **Priority 2:** Add TenantId filtering
3. **Priority 3:** Use @embedding_vector and @canonical_text for computation
4. **Priority 4:** Use MERGE instead of IF EXISTS + UPDATE/INSERT
5. **Priority 5:** Add validation (check embedding exists)
6. **Priority 6:** Compute sentiment from text (NLTK/transformers)
7. **Priority 7:** Compute topic scores via classification
8. **Priority 8:** Add complexity metrics (readability, vocab diversity)

---

## 5. sp_SemanticSimilarity

**Location:** `src/Hartonomous.Database/Procedures/dbo.sp_SemanticSimilarity.sql`  
**Type:** Stored Procedure  
**Lines:** ~45  
**Quality Score:** 84/100

### Purpose
Find semantically similar atoms using vector cosine similarity. Returns top-K most similar atoms to source.

### Parameters
- `@SourceAtomId BIGINT` - Source atom for similarity search
- `@TopK INT = 10` - Number of similar atoms to return
- `@TenantId INT = 0` - Multi-tenancy

### Architecture

**Similarity Search:**
1. Retrieve source embedding vector
2. Find top-K similar atoms using VECTOR_DISTANCE
3. Return similarity scores (0-100%), content metadata

### Key Operations

**Vector Similarity:**
```sql
SELECT TOP (@TopK)
    ae.AtomId AS SimilarAtomId,
    (1.0 - VECTOR_DISTANCE('cosine', ae.EmbeddingVector, @SourceEmbedding)) * 100.0 AS SimilarityScore,
    a.ContentHash, a.ContentType, a.CreatedAt
FROM dbo.AtomEmbedding ae
INNER JOIN dbo.Atom a ON ae.AtomId = a.AtomId AND ae.TenantId = a.TenantId
WHERE ae.TenantId = @TenantId AND ae.AtomId != @SourceAtomId AND ae.EmbeddingVector IS NOT NULL
ORDER BY VECTOR_DISTANCE('cosine', ae.EmbeddingVector, @SourceEmbedding) ASC;
```

### Dependencies
- Tables: `AtomEmbedding`, `Atom`
- Indexes: Vector index on AtomEmbedding.EmbeddingVector

### Quality Assessment

**Strengths:**
- ✅ **Multi-tenancy** - Proper TenantId filtering
- ✅ **Error handling** - TRY/CATCH with RAISERROR
- ✅ **Native VECTOR_DISTANCE** - Uses SQL Server 2025 vector ops
- ✅ **Excludes source** - AtomId != @SourceAtomId
- ✅ **Percentage score** - 0-100% scale for readability
- ✅ **NULL check** - Source embedding validation
- ✅ **Clean JOIN** - TenantId on both sides

**Weaknesses:**
- ⚠️ **Double VECTOR_DISTANCE** - Computed in SELECT and ORDER BY (inefficient)
- ⚠️ **Schema mismatch** - Uses ContentType (Atom table has Modality/Subtype)
- ⚠️ **No dimension validation** - Doesn't check vector dimensions match
- ⚠️ **No authorization** - Any user can find similar atoms for any atom

**Performance:**
- VECTOR_DISTANCE computed twice per row (unnecessary)
- Should benefit from vector index
- INNER JOIN is efficient

**Security:**
- ✅ Multi-tenant safe (TenantId filtering)
- ⚠️ No authorization (atom-level access control)

### Improvement Recommendations
1. **Priority 1:** Use CTE to compute VECTOR_DISTANCE once
2. **Priority 2:** Fix schema references (ContentType → Modality/Subtype)
3. **Priority 3:** Add vector dimension validation
4. **Priority 4:** Add authorization check (user can access atom)
5. **Priority 5:** Add query hint for vector index

---

## Summary Statistics

**Files Analyzed:** 5  
**Total Lines:** ~440  
**Average Quality:** 78.8/100

**Quality Distribution:**
- Excellent (85-100): 1 file (sp_AuditProvenanceChain 86)
- Good (70-84): 4 files (sp_SemanticSimilarity 84, sp_EnqueueNeo4jSync 78, sp_ForwardToNeo4j_Activated 76, sp_ComputeSemanticFeatures 70)

**Key Patterns:**
- **Service Broker integration** - 2 procedures for Neo4j sync (enqueue + activated processor)
- **Placeholder implementations** - sp_ForwardToNeo4j_Activated (simulated API) and sp_ComputeSemanticFeatures (hardcoded values)
- **Multi-tenancy gaps** - 3 procedures missing TenantId filtering
- **VECTOR_DISTANCE redundancy** - sp_SemanticSimilarity computes twice

**Security Issues:**
- 🔴 **sp_AuditProvenanceChain** - No TenantId filtering (cross-tenant audit)
- ⚠️ **sp_ForwardToNeo4j_Activated** - No TenantId in entity queries
- ⚠️ **sp_EnqueueNeo4jSync** - No encryption, no TenantId
- ⚠️ **sp_ComputeSemanticFeatures** - No TenantId filtering

**Implementation Gaps:**
- 🔴 **sp_ForwardToNeo4j_Activated** - Simulated Neo4j API call (returns mock data)
- 🔴 **sp_ComputeSemanticFeatures** - Returns hardcoded feature values (no actual computation)

**Performance Issues:**
- VECTOR_DISTANCE computed twice in sp_SemanticSimilarity
- No batching in sp_ForwardToNeo4j_Activated (processes 1 message at a time)

**Missing Objects:**
- None (all referenced tables/objects exist or are acknowledged as external)

**Critical Issues:**
1. 2 placeholder implementations block functionality (Neo4j sync, semantic features)
2. 4 procedures missing multi-tenancy (security vulnerability)
3. sp_EnqueueNeo4jSync has encryption disabled
4. sp_AuditProvenanceChain shows cross-tenant data

**Recommendations:**
1. Implement actual Neo4j REST/Bolt API integration (Priority 1 - Functionality)
2. Implement actual semantic feature computation with NLP models (Priority 1 - Functionality)
3. Add TenantId filtering to all procedures (Priority 1 - Security)
4. Enable Service Broker encryption (Priority 2 - Security)
5. Optimize VECTOR_DISTANCE computation in sp_SemanticSimilarity (Priority 2 - Performance)
6. Add authorization checks throughout (Priority 3 - Security)
