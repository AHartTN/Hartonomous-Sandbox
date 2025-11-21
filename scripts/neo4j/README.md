# Hartonomous Neo4j Provenance & Governance Schema

## Overview

This directory contains the **complete Neo4j schema and query library** for Hartonomous provenance tracking, data lineage, governance, and auditability. The schema has been comprehensively redesigned to support:

- **Complete Atom Provenance** - Full lineage from source to derived atoms
- **Transformation Tracking** - Detailed operation and derivation history
- **Model Inference Audit** - AI explainability and decision tracking
- **Governance & Compliance** - Data classification, access control, retention policies
- **Temporal Tracking** - Version history and change audit
- **Error Tracking** - Error clustering and pattern detection
- **Quality Metrics** - Validation and confidence scoring

## Files

### Schema Files

- **`CoreSchema_NEW.cypher`** - **NEW COMPREHENSIVE SCHEMA**
  - Complete provenance graph schema with 9 major sections
  - 20+ node types with constraints and indexes
  - 30+ relationship types for full traceability
  - Initialization of reference data (reasoning modes, classifications, contexts)
  - **Status**: Ready for deployment (replaces existing CoreSchema.cypher)

- **`CoreSchema.cypher`** - **OLD SCHEMA (DEPRECATED)**
  - Original inference-only schema
  - Missing atom provenance, governance, temporal tracking
  - **Action Required**: Backup and replace with CoreSchema_NEW.cypher

### Query Libraries

- **`ProvenanceQueries.cypher`** - **COMPREHENSIVE QUERY LIBRARY**
  - 80+ production-ready Cypher queries organized into 8 sections:
    1. Atom Lineage Queries (10 queries)
    2. Document & Chunk Tracking (4 queries)
    3. Operation & Transformation Queries (5 queries)
    4. Model Inference & Explainability (10 queries)
    5. Error Tracking & Clustering (5 queries)
    6. Access Audit & Governance (7 queries)
    7. Temporal & Version Tracking (4 queries)
    8. Advanced Analytics & Insights (8 queries)
  - Each query includes purpose, use case, and parameters
  - Ready for integration into API services

## Schema Architecture

### Section 1: Atom Provenance & Data Lineage

**Node Types:**
- `:Atom` - Core atomic unit of data
  - Constraints: `(id, tenantId)` uniqueness
  - Indexes: `id`, `tenantId`, `contentHash`, `createdAt`, `syncStatus`
  - Properties: id, tenantId, contentHash, atomType, sourceType, content, metadata, syncType, syncStatus, lastSynced, createdAt, modifiedAt

- `:Tenant` - Multi-tenant isolation
- `:User` - User identity and access
- `:Document` - Source documents

**Key Relationships:**
- `DERIVED_FROM` - Parent-child provenance
- `MERGED_FROM` - Multiple-source merges
- `BELONGS_TO_DOCUMENT` - Chunk-to-document
- `BELONGS_TO_TENANT` - Tenant isolation
- `CREATED_BY` - User creation audit
- `ACCESSED_BY` - Access tracking

### Section 2: Transformation & Operation Tracking

**Node Types:**
- `:Operation` - Transformation operations
  - Properties: id (GUID), type, status, timestamp, durationMs, tenantId, userId, errorMessage, metadata

**Key Relationships:**
- `INPUT_ATOM` - Operation inputs
- `OUTPUT_ATOM` - Operation outputs
- `EXECUTED_BY` - User execution
- `FAILED_DUE_TO` - Error linkage

### Section 3: Model Inference & Explainability

**Node Types:**
- `:Inference` - AI inference operations
- `:Model` - AI models
- `:ModelVersion` - Model versions
- `:Decision` - Inference outputs
- `:Evidence` - Supporting evidence
- `:ReasoningMode` - Reasoning types (vector, spatial, graph, hybrid, symbolic)
- `:Context` - Inference context
- `:Alternative` - Alternatives considered

**Key Relationships:**
- `USED_MODEL` - Model contribution weights
- `USED_REASONING` - Reasoning mode weights
- `RESULTED_IN` - Inference → Decision
- `SUPPORTED_BY` - Decision → Evidence
- `INFLUENCED_BY` - Inference causality chains
- `REQUESTED_BY` - User requests
- `USED_ATOM` - Atoms in inference
- `PRODUCED_ATOM` - Generated atoms

### Section 4: Error Tracking & Quality

**Node Types:**
- `:Error` - Error instances
- `:Validation` - Validation results

**Key Relationships:**
- `FAILED_WITH` - Operation errors
- `FAILED_VALIDATION` - Atom validation failures
- `AFFECTED_ATOM` - Error impacts
- `SIMILAR_TO` - Error clustering (similarity scores)

### Section 5: Temporal Tracking & Versioning

**Node Types:**
- `:AtomVersion` - Atom version history
  - Constraints: `(atomId, version)` uniqueness

**Key Relationships:**
- `HAS_VERSION` - Atom → Versions
- `SUPERSEDED_BY` - Version evolution
- `MODIFIED_BY` - User modifications
- `EVOLVED_TO` - Model version evolution

### Section 6: Data Classification & Governance

**Node Types:**
- `:DataClassification` - PII, PHI, Financial, Public, Internal, Confidential
  - Properties: name, description, retentionDays, accessLevel, complianceFrameworks
- `:ComplianceRule` - GDPR, HIPAA, SOX, CCPA rules
- `:AuditLog` - Access audit trail

**Key Relationships:**
- `CLASSIFIED_AS` - Atom/Document classification
- `REQUIRES_COMPLIANCE` - Classification → Rules
- `LOGGED_ACTION_ON_ATOM` - Audit → Atom
- `LOGGED_ACTION_BY_USER` - Audit → User

### Section 7: Feedback & Improvement

**Node Types:**
- `:Feedback` - User feedback on inferences

**Key Relationships:**
- `RATED_BY` - Decision feedback
- `PROVIDED_BY` - Feedback user
- `ABOUT_MODEL` - Model feedback
- `LED_TO_IMPROVEMENT` - Feedback → Model updates

## Key Constraints

### Uniqueness Constraints
```cypher
// Atom provenance
CREATE CONSTRAINT atom_id_tenant_unique FOR (a:Atom) REQUIRE (a.id, a.tenantId) IS UNIQUE;
CREATE CONSTRAINT tenant_id_unique FOR (t:Tenant) REQUIRE t.id IS UNIQUE;
CREATE CONSTRAINT user_id_unique FOR (u:User) REQUIRE u.id IS UNIQUE;
CREATE CONSTRAINT document_id_tenant_unique FOR (d:Document) REQUIRE (d.id, d.tenantId) IS UNIQUE;

// Operations & transformations
CREATE CONSTRAINT operation_id_unique FOR (o:Operation) REQUIRE o.id IS UNIQUE;

// Model inference
CREATE CONSTRAINT inference_id_unique FOR (i:Inference) REQUIRE i.id IS UNIQUE;
CREATE CONSTRAINT model_id_unique FOR (m:Model) REQUIRE m.id IS UNIQUE;
CREATE CONSTRAINT model_version_unique FOR (mv:ModelVersion) REQUIRE (mv.modelId, mv.version) IS UNIQUE;
CREATE CONSTRAINT decision_id_unique FOR (d:Decision) REQUIRE d.id IS UNIQUE;
CREATE CONSTRAINT evidence_id_unique FOR (e:Evidence) REQUIRE e.id IS UNIQUE;
CREATE CONSTRAINT reasoning_mode_type_unique FOR (rm:ReasoningMode) REQUIRE rm.type IS UNIQUE;

// Error tracking
CREATE CONSTRAINT error_id_unique FOR (e:Error) REQUIRE e.id IS UNIQUE;

// Temporal tracking
CREATE CONSTRAINT atom_version_unique FOR (av:AtomVersion) REQUIRE (av.atomId, av.version) IS UNIQUE;

// Governance
CREATE CONSTRAINT classification_name_unique FOR (dc:DataClassification) REQUIRE dc.name IS UNIQUE;
```

### Performance Indexes
```cypher
// Atom indexes
CREATE INDEX atom_id FOR (a:Atom) ON (a.id);
CREATE INDEX atom_tenant FOR (a:Atom) ON (a.tenantId);
CREATE INDEX atom_content_hash FOR (a:Atom) ON (a.contentHash);
CREATE INDEX atom_created FOR (a:Atom) ON (a.createdAt);
CREATE INDEX atom_sync_status FOR (a:Atom) ON (a.syncStatus);

// Operation indexes
CREATE INDEX operation_type FOR (o:Operation) ON (o.type);
CREATE INDEX operation_status FOR (o:Operation) ON (o.status);
CREATE INDEX operation_timestamp FOR (o:Operation) ON (o.timestamp);

// Inference indexes
CREATE INDEX inference_timestamp FOR (i:Inference) ON (i.timestamp);
CREATE INDEX inference_task FOR (i:Inference) ON (i.taskType);
CREATE INDEX inference_confidence FOR (i:Inference) ON (i.confidence);

// Error indexes
CREATE INDEX error_type FOR (e:Error) ON (e.type);
CREATE INDEX error_timestamp FOR (e:Error) ON (e.timestamp);

// Audit indexes
CREATE INDEX audit_log_timestamp FOR (al:AuditLog) ON (al.timestamp);
CREATE INDEX audit_log_action FOR (al:AuditLog) ON (al.action);

// Relationship indexes
CREATE INDEX derived_from_type FOR ()-[r:DERIVED_FROM]-() ON (r.derivationType);
CREATE INDEX accessed_timestamp FOR ()-[r:ACCESSED_BY]-() ON (r.timestamp);
```

## Deployment Instructions

### 1. Backup Existing Schema (IMPORTANT)
```bash
# Export existing data
cypher-shell -a bolt://localhost:7687 -u neo4j -p neo4jneo4j \
  "MATCH (n) RETURN n LIMIT 1000" > neo4j_backup_$(date +%Y%m%d).cypher

# Or use Neo4j dump
neo4j-admin database dump neo4j --to-path=./backups/
```

### 2. Deploy New Schema
```bash
# Option A: Direct execution
cypher-shell -a bolt://localhost:7687 -u neo4j -p neo4jneo4j \
  -f scripts/neo4j/schemas/CoreSchema_NEW.cypher

# Option B: Via Neo4j Browser
# 1. Open http://localhost:7474
# 2. Copy contents of CoreSchema_NEW.cypher
# 3. Execute in Browser

# Option C: Via PowerShell
$env:NEO4J_URI = "bolt://localhost:7687"
$env:NEO4J_USER = "neo4j"
$env:NEO4J_PASSWORD = "neo4jneo4j"
Get-Content scripts/neo4j/schemas/CoreSchema_NEW.cypher | cypher-shell
```

### 3. Verify Schema Deployment
```cypher
// Check constraints
SHOW CONSTRAINTS;

// Check indexes
SHOW INDEXES;

// Verify reference data
MATCH (rm:ReasoningMode) RETURN rm.type, rm.description;
MATCH (dc:DataClassification) RETURN dc.name, dc.accessLevel;
MATCH (ctx:Context) RETURN ctx.domain;
```

### 4. Test Worker Synchronization

```sql
-- Send test message to Neo4jSyncQueue
DECLARE @ConversationHandle UNIQUEIDENTIFIER;

BEGIN DIALOG CONVERSATION @ConversationHandle
    FROM SERVICE [HartonomousService]
    TO SERVICE 'HartonomousService'
    ON CONTRACT [HartonomousContract]
    WITH ENCRYPTION = OFF;

DECLARE @MessageBody XML = N'
<Neo4jSync>
    <EntityId>99999</EntityId>
    <TenantId>0</TenantId>
    <EntityType>Atom</EntityType>
    <SyncType>CREATE</SyncType>
</Neo4jSync>';

SEND ON CONVERSATION @ConversationHandle
    MESSAGE TYPE [Neo4jSyncRequest]
    (@MessageBody);

-- Verify message sent
SELECT COUNT(*) as MessageCount FROM Neo4jSyncQueue;
```

```cypher
-- Verify atom created in Neo4j (after worker processes message)
MATCH (a:Atom {id: 99999, tenantId: 0})
RETURN a.id, a.tenantId, a.syncType, a.lastSynced;

-- Check sync status
MATCH (a:Atom {tenantId: 0})
RETURN a.syncStatus, count(*) as count
GROUP BY a.syncStatus;
```

## Common Query Patterns

### Atom Lineage
```cypher
// Get full upstream lineage
MATCH path = (atom:Atom {id: $atomId})-[:DERIVED_FROM*]->(ancestor:Atom)
RETURN path;

// Get downstream impact
MATCH path = (atom:Atom {id: $atomId})<-[:DERIVED_FROM*]-(descendant:Atom)
RETURN path;
```

### Inference Explainability
```cypher
// Why was this decision made?
MATCH (inf:Inference {id: $inferenceId})-[:RESULTED_IN]->(d:Decision)
MATCH (d)-[:SUPPORTED_BY]->(evidence:Evidence)
MATCH (inf)-[r:USED_MODEL]->(model:Model)
RETURN d, model.name, r.weight, collect(evidence) as evidence;
```

### Governance Audit
```cypher
// Find PII data
MATCH (atom:Atom)-[:CLASSIFIED_AS]->(dc:DataClassification {name: 'PII'})
RETURN atom.id, atom.content[0..100];

// Access audit
MATCH (atom:Atom {id: $atomId})<-[r:ACCESSED_BY]-(user:User)
RETURN user.email, r.operation, r.timestamp
ORDER BY r.timestamp DESC;
```

### Error Clustering
```cypher
// Find similar errors
MATCH (error1:Error {id: $errorId})-[r:SIMILAR_TO]-(error2:Error)
WHERE r.similarity > 0.7
RETURN error2, r.similarity
ORDER BY r.similarity DESC;
```

## Integration with Hartonomous

### Neo4jSyncWorker
- Worker polls `Neo4jSyncQueue` for sync messages
- Executes `MERGE` queries to create/update Atom nodes
- Schema now provides proper constraints and indexes for atom sync
- Worker configuration: `src/Hartonomous.Workers.Neo4jSync/appsettings.json`

### Neo4jProvenanceService
- Implements read-only analytical queries
- Uses comprehensive query patterns from `ProvenanceQueries.cypher`
- Location: `src/Hartonomous.Infrastructure/Services/Provenance/Neo4jProvenanceService.cs`

### ProvenanceController API
- Exposes REST endpoints for lineage, audit, explainability
- Location: `src/Hartonomous.Api/Controllers/ProvenanceController.cs`
- Example endpoints:
  - `GET /api/v1/provenance/atoms/{atomId}/lineage`
  - `GET /api/v1/provenance/inferences/{inferenceId}/explainability`
  - `GET /api/v1/provenance/atoms/{atomId}/access-audit`

## Monitoring & Maintenance

### Health Checks
```cypher
// Graph statistics
CALL apoc.meta.stats() YIELD nodeCount, relCount, labels
RETURN nodeCount, relCount, labels;

// Constraint violations (should be empty)
MATCH (a:Atom)
WITH a.id, a.tenantId, count(*) as duplicates
WHERE duplicates > 1
RETURN *;

// Orphaned nodes
MATCH (a:Atom)
WHERE NOT (a)-[]-()
RETURN count(a) as orphanedAtoms;
```

### Performance Tuning
```cypher
// Identify slow queries
CALL dbms.listQueries() YIELD query, elapsedTimeMillis
WHERE elapsedTimeMillis > 1000
RETURN query, elapsedTimeMillis
ORDER BY elapsedTimeMillis DESC;

// Index usage statistics
CALL db.indexes() YIELD name, type, state, populationPercent
RETURN name, type, state, populationPercent;
```

## Migration from Old Schema

### Data Migration Steps

1. **Export existing inference data**
   ```cypher
   // Export inferences
   MATCH (i:Inference)-[r:USED_MODEL]->(m:Model)
   RETURN i, r, m;
   ```

2. **Apply new schema** (CoreSchema_NEW.cypher)

3. **Migrate existing nodes** (if any)
   ```cypher
   // Add missing properties to existing Inference nodes
   MATCH (i:Inference)
   WHERE NOT EXISTS(i.tenantId)
   SET i.tenantId = 0;
   ```

4. **Verify constraints satisfied**
   ```cypher
   // Check for constraint violations before creating constraints
   MATCH (i:Inference)
   WITH i.id as inferenceId, count(*) as duplicates
   WHERE duplicates > 1
   RETURN inferenceId, duplicates;
   ```

## Troubleshooting

### Issue: Atom nodes not appearing after sync
**Diagnosis:**
```cypher
SHOW CONSTRAINTS;
// Verify atom_id_tenant_unique exists
```

**Solution:**
```cypher
// Recreate constraint if missing
CREATE CONSTRAINT atom_id_tenant_unique IF NOT EXISTS
FOR (a:Atom) REQUIRE (a.id, a.tenantId) IS UNIQUE;
```

### Issue: Worker sync failures
**Check:**
1. Verify Neo4j connection: `bolt://localhost:7687`
2. Check worker logs: `D:\Repositories\Hartonomous\src\Hartonomous.Workers.Neo4jSync\`
3. Verify Service Broker messages consumed:
   ```sql
   SELECT COUNT(*) FROM Neo4jSyncQueue;
   ```

### Issue: Query performance degradation
**Diagnosis:**
```cypher
// Check missing indexes
CALL db.indexes() YIELD name, state
WHERE state <> 'ONLINE'
RETURN name, state;
```

**Solution:**
```cypher
// Recreate failed indexes
CREATE INDEX atom_id IF NOT EXISTS FOR (a:Atom) ON (a.id);
```

## Future Enhancements

1. **Graph Data Science Integration**
   - Community detection for error clustering
   - PageRank for atom importance scoring
   - Link prediction for missing lineage

2. **Real-time Streaming**
   - Neo4j Change Data Capture (CDC)
   - Real-time dashboard updates
   - Anomaly detection on graph mutations

3. **Advanced Analytics**
   - Graph Neural Networks (GNN) for quality prediction
   - Temporal graph analysis for trend detection
   - Multi-hop reasoning optimization

4. **Visualization**
   - Neo4j Bloom integration
   - Custom D3.js lineage visualizations
   - Interactive governance dashboards

## References

- [Neo4j Cypher Manual - Constraints](https://neo4j.com/docs/cypher-manual/current/constraints/)
- [Neo4j Graph Data Modeling](https://neo4j.com/developer/guide-data-modeling/)
- [Microsoft Docs - Data Lineage Best Practices](https://learn.microsoft.com/en-us/azure/databricks/lakehouse-architecture/data-governance/best-practices)
- [Neo4j Best Practices for Graph Semantics](https://neo4j.com/docs/getting-started/data-modeling/)

## Contact

For questions or issues with the Neo4j schema:
- Check worker logs: `D:\Repositories\Hartonomous\src\Hartonomous.Workers.Neo4jSync\`
- Review SQL audit docs: `docs/audit/SQL/`
- See architecture docs: `docs/architecture/`

---

**Last Updated**: November 21, 2025  
**Schema Version**: 2.0 (Comprehensive Provenance & Governance)  
**Status**: Ready for Production Deployment
