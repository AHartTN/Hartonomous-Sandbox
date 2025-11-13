# Neo4j Graph Database Integration

**Status**: Partially Implemented  
**Components**: Neo4j sync worker (PLANNED), SQL temporal tables (ACTIVE), graph query API  
**Purpose**: Graph-based relationship traversal and provenance tracking complementing SQL Server

---

## Overview

Hartonomous uses **SQL Server as primary database** with **Neo4j as graph query accelerator** for:

- Multi-hop relationship traversal (6+ hops)
- Provenance chain queries ("what influenced this inference?")
- Graph algorithms (shortest path, centrality, community detection)
- Counterfactual analysis ("what if X was different?")

SQL Server provides ACID transactions and temporal history; Neo4j provides fast graph traversal.

---

## Architecture

**Dual-Ledger System:**

| Component | SQL Server | Neo4j |
|-----------|------------|-------|
| **Role** | Source of truth | Read-optimized graph view |
| **Data** | Atoms, embeddings, inferences, models | Nodes + relationships only |
| **Writes** | Primary (all writes go here first) | Secondary (async sync) |
| **History** | Temporal tables (`FOR SYSTEM_TIME`) | No built-in history |
| **Queries** | Row lookups, aggregations, joins | Relationship traversal, graph algorithms |

**Current Implementation:**

```
API Request → SQL Server (write)
                ↓ (sync planned)
            Neo4j (eventual consistency for graph queries)
```

---

## Data Sync (PLANNED - NOT YET IMPLEMENTED)

**Expected Worker**: `Hartonomous.Workers.Neo4jSync` (directory does not exist)

**Planned Sync Protocol:**

1. **Change Tracking**: SQL Server temporal tables detect changes
2. **Sync Worker**: Background service polls for changes every 5 seconds
3. **Graph Mapping**:
   - SQL `Atoms` → Neo4j `(:Atom)` nodes
   - SQL `AtomEmbeddings` → Neo4j `(:EMBEDDED_AS)` relationships
   - SQL `InferenceRequests` → Neo4j `(:Inference)` nodes
   - SQL `ModelLayers` → Neo4j `(:Layer)` nodes with `(:CONTAINS)` edges
4. **Conflict Resolution**: SQL Server wins (single source of truth)
5. **Batch Size**: 1000 nodes/relationships per sync batch

**Current Status**: 
- ✅ SQL Server temporal tables active on key tables
- ❌ No sync worker implemented
- ❌ No Neo4j connection configuration
- ❌ No graph schema defined

---

## Graph Schema (Planned)

**Nodes:**

```cypher
// Atom nodes (text, image, code, audio, video, model)
(:Atom {
  atomId: integer,
  modality: string,
  contentHash: string,
  createdAt: datetime
})

// Embedding nodes (spatial coordinates + vector)
(:Embedding {
  embeddingId: integer,
  atomId: integer,
  spatialBucketX: integer,
  spatialBucketY: integer,
  spatialBucketZ: integer
})

// Inference nodes (API requests)
(:Inference {
  inferenceId: integer,
  requestTimestamp: datetime,
  modelUsed: string,
  durationMs: integer
})

// Model nodes
(:Model {
  modelId: integer,
  modelName: string,
  architecture: string
})

// Layer nodes (model architecture components)
(:Layer {
  layerId: integer,
  layerIdx: integer,
  layerType: string,
  parameterCount: integer
})
```

**Relationships:**

```cypher
(:Atom)-[:EMBEDDED_AS]->(:Embedding)
(:Inference)-[:USED_ATOM]->(:Atom)
(:Inference)-[:USED_MODEL]->(:Model)
(:Model)-[:CONTAINS]->(:Layer)
(:Atom)-[:DERIVED_FROM]->(:Atom)  // Provenance chain
(:Embedding)-[:SIMILAR_TO {score: float}]->(:Embedding)  // KNN graph
```

---

## API Integration

**Graph Query Endpoints:**

### `POST /api/graph/query` - Cypher Query Execution

**Status**: NOT IMPLEMENTED (GraphQueryController exists but no Neo4j backend)

**Expected Request:**
```json
{
  "query": "MATCH (a:Atom)-[:EMBEDDED_AS]->(e:Embedding) WHERE a.atomId = $atomId RETURN e",
  "parameters": {
    "atomId": 12345
  }
}
```

**Expected Response:**
```json
{
  "results": [
    {
      "e": {
        "embeddingId": 67890,
        "spatialBucketX": 42,
        "spatialBucketY": 128,
        "spatialBucketZ": 7
      }
    }
  ],
  "executionTimeMs": 15
}
```

### `POST /api/graph/provenance/{inferenceId}` - Provenance Chain

**Status**: NOT IMPLEMENTED

**Purpose**: Trace all atoms that influenced an inference result

**Expected Cypher:**
```cypher
MATCH path = (i:Inference {inferenceId: $id})-[:USED_ATOM*1..10]->(a:Atom)
RETURN path
ORDER BY length(path) DESC
```

**Expected Response:**
```json
{
  "inferenceId": 100,
  "provenanceChain": [
    {
      "depth": 0,
      "atomId": 500,
      "modality": "text",
      "contentHash": "abc123..."
    },
    {
      "depth": 1,
      "atomId": 450,
      "modality": "text",
      "contentHash": "def456..."
    }
  ]
}
```

---

## SQL Server Temporal Tables

**Active Implementation** (source of truth for provenance):

```sql
-- Temporal table example
CREATE TABLE dbo.Atoms
(
    AtomId BIGINT IDENTITY(1,1) NOT NULL PRIMARY KEY,
    Modality NVARCHAR(50) NOT NULL,
    ContentHash NVARCHAR(128) NOT NULL,
    CreatedAt DATETIME2 GENERATED ALWAYS AS ROW START NOT NULL,
    ModifiedAt DATETIME2 GENERATED ALWAYS AS ROW END NOT NULL,
    PERIOD FOR SYSTEM_TIME (CreatedAt, ModifiedAt)
)
WITH (SYSTEM_VERSIONING = ON (HISTORY_TABLE = dbo.AtomsHistory));
```

**Query Historical State:**
```sql
-- Get atom state as of specific time
SELECT * FROM dbo.Atoms
FOR SYSTEM_TIME AS OF '2025-11-01 12:00:00'
WHERE AtomId = 12345;

-- Get all changes to an atom
SELECT * FROM dbo.Atoms
FOR SYSTEM_TIME ALL
WHERE AtomId = 12345
ORDER BY CreatedAt DESC;
```

**Current Tables with Temporal Tracking:**
- `Atoms` - All atom modifications tracked
- `AtomEmbeddings` - Embedding updates tracked
- `Models` - Model metadata changes tracked
- `ModelLayers` - Layer additions/removals tracked

---

## Performance Characteristics

**SQL Server (Current):**
- ✅ Row lookup: < 1ms
- ✅ Aggregations: 10-50ms
- ✅ 2-3 table joins: 20-100ms
- ❌ 6+ hop traversal: 500-2000ms (slow)

**Neo4j (Expected with sync):**
- Graph traversal (6 hops): 10-50ms
- Shortest path: 5-20ms
- Community detection: 100-500ms
- KNN graph query: 20-100ms

---

## Failure Scenarios

**Sync Lag:**
- **Cause**: Neo4j sync worker crashes or falls behind
- **Impact**: Graph queries return stale data
- **Mitigation**: API responses include `syncedAsOf` timestamp, clients can decide to query SQL directly if freshness required

**Neo4j Unavailable:**
- **Cause**: Neo4j server down or network partition
- **Impact**: Graph queries fail
- **Mitigation**: API falls back to SQL Server queries (slower but functional)

**Conflict Detection:**
- **Cause**: Direct Neo4j writes (NOT SUPPORTED)
- **Impact**: Data inconsistency
- **Mitigation**: Neo4j is READ-ONLY; all writes must go through SQL Server API

---

## GDPR Article 22 Compliance

**Right to Explanation** (automated decision-making):

Neo4j provenance graph provides:

1. **Input Traceability**: Track all atoms used in an inference
2. **Model Transparency**: Show which model + layers produced result
3. **Counterfactual Analysis**: "If atom X was removed, would result change?"
4. **Temporal Audit**: Historical state queries via SQL temporal tables

**Example Compliance Query:**

```cypher
// Show all data sources that influenced inference 12345
MATCH (i:Inference {inferenceId: 12345})-[r:USED_ATOM*1..10]->(a:Atom)
WITH a, length(r) AS depth
ORDER BY depth
RETURN 
  a.atomId AS sourceAtomId,
  a.modality AS dataType,
  a.contentHash AS dataFingerprint,
  depth AS influenceDepth
```

**SQL Fallback (Current):**
```sql
-- Provenance via SQL joins (slower for deep chains)
WITH RECURSIVE ProvenanceChain AS (
    SELECT ir.InferenceId, a.AtomId, a.Modality, 0 AS Depth
    FROM InferenceRequests ir
    CROSS APPLY OPENJSON(ir.InputData, '$.atoms') WITH (AtomId BIGINT) atoms
    INNER JOIN Atoms a ON a.AtomId = atoms.AtomId
    WHERE ir.InferenceId = 12345
    
    UNION ALL
    
    SELECT pc.InferenceId, a2.AtomId, a2.Modality, pc.Depth + 1
    FROM ProvenanceChain pc
    INNER JOIN AtomRelationships ar ON ar.TargetAtomId = pc.AtomId
    INNER JOIN Atoms a2 ON a2.AtomId = ar.SourceAtomId
    WHERE pc.Depth < 10
)
SELECT * FROM ProvenanceChain ORDER BY Depth;
```

---

## Next Steps

1. **Implement Neo4j Sync Worker**:
   - Create `src/Hartonomous.Workers.Neo4jSync` project
   - Poll SQL temporal tables for changes
   - Batch sync to Neo4j every 5 seconds

2. **Define Neo4j Schema**:
   - Create constraints (unique AtomId, InferenceId)
   - Create indexes (spatial buckets, timestamps)
   - Define relationship types

3. **Implement Graph Query API**:
   - Add Neo4j driver to `Hartonomous.Infrastructure`
   - Implement `GraphQueryController` endpoints
   - Add fallback to SQL if Neo4j unavailable

4. **Performance Testing**:
   - Benchmark 6+ hop traversal (SQL vs Neo4j)
   - Measure sync lag under load
   - Test failure scenarios

5. **Security**:
   - Neo4j authentication (secure connection string)
   - Read-only Neo4j credentials for API
   - Audit logging for graph queries
