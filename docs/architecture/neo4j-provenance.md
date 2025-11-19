# Neo4j Provenance Graph

**Cryptographically Verifiable Audit Trail & Knowledge Network**

## Overview

The Neo4j provenance graph provides **explainability** and **auditability** for the Hartonomous platform. While SQL Server stores atomic data and high-dimensional vectors, Neo4j stores the **relationships** and **transformations** that created that data. This dual-database strategy creates a complete, verifiable history of every AI inference and data transformation.

## Core Innovation: Dual-Database System of Record (SoR)

**SQL Server (Engine & SoR for Atomic Data)**:
- Master repository for raw `Content` and `VECTOR` embeddings
- Transactional source of truth for "what an atom is"
- Optimized for O(log N) spatial queries and O(K) CLR refinement

**Neo4j (Ledger & SoR for Provenance)**:
- Master repository for relationships *between* atoms and creation processes
- Source of truth for "how an atom came to be" and "what it's connected to"
- Optimized for graph traversal, pathfinding, and multi-hop relationship analysis

## Data Flow & Synchronization

**Asynchronous Event-Driven Model**:

```
SQL Server Atom Creation
    ↓
SQL Service Broker Event Published
    ↓
Hartonomous.Workers.Neo4jSync Consumer
    ↓
Neo4j Graph Update (Atom node + relationships)
```

**Eventual Consistency**:
- Short delay (typically <500ms) between SQL atom creation and Neo4j node appearance
- Acceptable trade-off: Provenance graph is analytical/auditing, not real-time transactional
- Slight delay does not impact core AI engine functionality

**Synchronization Mechanism**:
- Worker service polls SQL Service Broker queue
- Batches events for efficiency (100 events per transaction)
- Retries with exponential backoff on failures
- Dead-letter queue for unprocessable events

## Node Schema

### 1. Atom Node

Represents a unique, content-addressed piece of data. **Bridge between SQL and Neo4j**.

**Cypher Definition**:
```cypher
(:Atom {
  atomId: Integer,           // Primary key from SQL Server dbo.Atoms
  atomHash: String,          // SHA-256 hash (true immutable identifier)
  contentType: String,       // MIME type: "text/plain", "application/gguf", "image/png"
  createdAt: DateTime,       // First ingestion timestamp
  ingestedBy: String,        // User or service that created atom
  sizeBytes: Integer,        // Original content size
  tenantId: Integer          // Multi-tenant isolation
})
```

**Index**:
```cypher
CREATE INDEX atom_hash_idx FOR (a:Atom) ON (a.atomHash);
CREATE INDEX atom_tenant_idx FOR (a:Atom) ON (a.tenantId, a.createdAt);
```

**Example**:
```cypher
// Find atom by SQL primary key
MATCH (a:Atom {atomId: 1234567890})
RETURN a.atomHash, a.contentType, a.createdAt;
```

### 2. Source Node

Represents the origin of raw data (file, URL, user input, API).

**Cypher Definition**:
```cypher
(:Source {
  sourceId: Integer,
  sourceType: String,        // "file", "url", "stream", "user-input", "api"
  identifier: String,        // File path, URL, or unique identifier
  ingestedAt: DateTime,
  checksum: String,         // SHA-256 of original source for verification
  metadata: Map             // Format-specific: {"model_format": "GGUF", "quantization": "Q8_0"}
})
```

**Index**:
```cypher
CREATE INDEX source_identifier_idx FOR (s:Source) ON (s.identifier);
```

**Example**:
```cypher
// Find all atoms from a specific model file
MATCH (s:Source {identifier: "C:\\Models\\Qwen3-Coder-7B.gguf"})<-[:INGESTED_FROM]-(a:Atom)
RETURN COUNT(a) AS total_atoms;
```

### 3. IngestionJob Node

Represents a specific execution of the ingestion pipeline. Enables **versioned, reproducible ingestion**.

**Cypher Definition**:
```cypher
(:IngestionJob {
  jobId: Integer,
  jobType: String,           // "atomize-text", "atomize-model", "atomize-image"
  startedAt: DateTime,
  completedAt: DateTime,
  status: String,            // "success", "failed", "partial"
  algorithmVersion: String,  // "v1.2.0" of atomization algorithm
  atomsCreated: Integer,     // Total atoms produced
  errorMessage: String       // If status="failed"
})
```

**Index**:
```cypher
CREATE INDEX job_status_idx FOR (j:IngestionJob) ON (j.status, j.completedAt);
```

**Example**:
```cypher
// Find all failed jobs in last 7 days
MATCH (j:IngestionJob)
WHERE j.status = "failed" 
  AND j.startedAt > datetime() - duration({days: 7})
RETURN j.jobId, j.jobType, j.errorMessage
ORDER BY j.startedAt DESC;
```

### 4. User Node

Represents a human user or service principal. Enables user-level auditing.

**Cypher Definition**:
```cypher
(:User {
  userId: Integer,
  username: String,
  email: String,
  role: String,              // "admin", "data-scientist", "service", "agent"
  createdAt: DateTime
})
```

**Index**:
```cypher
CREATE UNIQUE INDEX user_username_idx FOR (u:User) ON (u.username);
```

### 5. Pipeline Node

Represents a specific **version** of an AI pipeline or stored procedure. Critical for reproducibility.

**Cypher Definition**:
```cypher
(:Pipeline {
  pipelineId: Integer,
  name: String,              // "sp_TransformerStyleInference", "sp_SemanticSearch"
  version: String,           // Semantic versioning: "v1.2.3"
  deployedAt: DateTime,
  codeHash: String,         // SHA-256 of T-SQL/CLR code for cryptographic verification
  description: String        // Human-readable change notes
})
```

**Index**:
```cypher
CREATE INDEX pipeline_name_version_idx FOR (p:Pipeline) ON (p.name, p.version);
```

**Example**:
```cypher
// Track all versions of inference pipeline
MATCH (p:Pipeline)
WHERE p.name = "sp_TransformerStyleInference"
RETURN p.version, p.deployedAt, p.description
ORDER BY p.deployedAt DESC;
```

### 6. Inference Node

Represents a single execution of a pipeline. **Core of explainability**.

**Cypher Definition**:
```cypher
(:Inference {
  inferenceId: Integer,
  executedAt: DateTime,
  executedBy: String,        // User or service principal
  durationMs: Integer,       // Execution time
  k_value: Integer,          // K parameter used in O(K) refinement
  contextHash: String,       // SHA-256 of input context (for caching/deduplication)
  inputCount: Integer,       // Number of input atoms
  outputCount: Integer,      // Number of generated atoms
  cost: Float               // Computational cost (arbitrary units)
})
```

**Index**:
```cypher
CREATE INDEX inference_executed_idx FOR (i:Inference) ON (i.executedAt);
CREATE INDEX inference_context_idx FOR (i:Inference) ON (i.contextHash);
```

## Relationship Schema

The graph's power comes from relationships forming a **Merkle DAG** (Directed Acyclic Graph) with cryptographic verifiability.

### 1. INGESTED_FROM

Links an `Atom` to its originating `Source`.

**Cypher**:
```cypher
(:Atom)-[:INGESTED_FROM]->(:Source)
```

**Meaning**: This atom was extracted from this source.

**Example Query (Impact Analysis)**:
```cypher
// Find all atoms from a compromised data source
MATCH (s:Source {identifier: "https://compromised-dataset.com/data.json"})
      -[:INGESTED_FROM*1..5]-(affected:Atom)
RETURN COUNT(DISTINCT affected) AS total_affected_atoms;
```

### 2. CREATED_BY_JOB

Links an `Atom` to the `IngestionJob` that created it.

**Cypher**:
```cypher
(:Atom)-[:CREATED_BY_JOB]->(:IngestionJob)
```

**Meaning**: This specific job execution created this atom.

**Example Query (Algorithm Versioning)**:
```cypher
// Find all atoms created by old algorithm version
MATCH (a:Atom)-[:CREATED_BY_JOB]->(j:IngestionJob)
WHERE j.algorithmVersion = "v1.0.0"
RETURN COUNT(a) AS atoms_needing_reingestion;
```

### 3. INGESTED_BY_USER

Links a `Source` to the `User` who initiated ingestion.

**Cypher**:
```cypher
(:Source)-[:INGESTED_BY_USER]->(:User)
```

**Meaning**: This user is responsible for bringing this data into the system.

**Example Query (User Audit)**:
```cypher
// Find all data sources added by a user
MATCH (u:User {username: "alice"})<-[:INGESTED_BY_USER]-(s:Source)
RETURN s.sourceType, s.identifier, s.ingestedAt
ORDER BY s.ingestedAt DESC;
```

### 4. EXECUTED_BY_PIPELINE

Links an `Inference` to the `Pipeline` version used.

**Cypher**:
```cypher
(:Inference)-[:EXECUTED_BY_PIPELINE]->(:Pipeline)
```

**Meaning**: This inference used this specific version of the AI logic.

**Example Query (Pipeline Versioning)**:
```cypher
// Compare inference performance across pipeline versions
MATCH (i:Inference)-[:EXECUTED_BY_PIPELINE]->(p:Pipeline)
WHERE p.name = "sp_TransformerStyleInference"
RETURN p.version, AVG(i.durationMs) AS avg_duration_ms, COUNT(i) AS total_inferences
ORDER BY p.deployedAt DESC;
```

### 5. HAD_INPUT

Links an `Inference` to its input `Atom` nodes.

**Cypher**:
```cypher
(:Inference)-[:HAD_INPUT {ordinal: Integer, weight: Float}]->(:Atom)
```

**Properties**:
- `ordinal`: Position in input sequence (for ordered inputs)
- `weight`: Importance/attention weight (0.0-1.0)

**Meaning**: This inference consumed this atom as input.

**Example Query (Root Cause Analysis)**:
```cypher
// Find all inputs that contributed to a specific output
MATCH (output:Atom {atomId: 9876543210})<-[:GENERATED]-(i:Inference)-[:HAD_INPUT]->(input:Atom)
RETURN input.atomHash, input.contentType, input.createdAt
ORDER BY input.createdAt;
```

### 6. GENERATED

Links an `Inference` to its output `Atom` nodes.

**Cypher**:
```cypher
(:Inference)-[:GENERATED {confidence: Float}]->(:Atom)
```

**Properties**:
- `confidence`: Model confidence score (0.0-1.0)

**Meaning**: This inference created this atom as output.

**Example Query (Forward Provenance)**:
```cypher
// Find all outputs generated from a specific input atom
MATCH (input:Atom {atomId: 1234567890})<-[:HAD_INPUT]-(i:Inference)-[:GENERATED]->(output:Atom)
RETURN output.atomHash, output.contentType, i.executedAt
ORDER BY i.executedAt;
```

### 7. USED_REASONING

Links an `Inference` to a `ReasoningChain` node (Tree-of-Thought, Chain-of-Thought).

**Cypher**:
```cypher
(:Inference)-[:USED_REASONING]->(:ReasoningChain)
```

**ReasoningChain Node**:
```cypher
(:ReasoningChain {
  chainId: Integer,
  chainType: String,         // "tree-of-thought", "chain-of-thought", "react"
  depth: Integer,            // Number of reasoning steps
  explored: Integer,         // Number of branches explored (ToT)
  selected: Integer          // Index of selected branch
})
```

### 8. HAS_STEP

Links a `ReasoningChain` to individual reasoning steps.

**Cypher**:
```cypher
(:ReasoningChain)-[:HAS_STEP {ordinal: Integer}]->(:ReasoningStep)
```

**ReasoningStep Node**:
```cypher
(:ReasoningStep {
  stepId: Integer,
  stepType: String,          // "thought", "action", "observation"
  content: String,           // Step description
  score: Float,             // Quality score (for beam search)
  pruned: Boolean           // Was this branch pruned?
})
```

## Explainability Queries

### Root Cause Analysis

**Question**: "Why did the AI generate this specific output?"

```cypher
// Complete provenance chain for an output atom
MATCH path = (output:Atom {atomId: $outputAtomId})
             <-[:GENERATED]-(inference:Inference)
             -[:HAD_INPUT]->(input:Atom)
             -[:INGESTED_FROM]->(source:Source)
RETURN path;
```

**Returns**: Full chain showing inference → inputs → sources.

### Impact Analysis

**Question**: "If I delete this source, what will be affected?"

```cypher
// Find all derived atoms from a source
MATCH (source:Source {identifier: $sourceIdentifier})
      <-[:INGESTED_FROM*1..]-(atom:Atom)
      <-[:HAD_INPUT]-(inference:Inference)
      -[:GENERATED]->(derived:Atom)
RETURN COUNT(DISTINCT derived) AS total_affected_atoms,
       COUNT(DISTINCT inference) AS total_affected_inferences;
```

### Bias Detection

**Question**: "Are certain users disproportionately influencing AI outputs?"

```cypher
// Count inferences by user
MATCH (u:User)<-[:INGESTED_BY_USER]-(s:Source)
      <-[:INGESTED_FROM]-(a:Atom)
      <-[:HAD_INPUT]-(i:Inference)
RETURN u.username, COUNT(DISTINCT i) AS inference_count
ORDER BY inference_count DESC
LIMIT 10;
```

### Temporal Causality (Laplace's Demon)

**Question**: "What was the system state at a specific point in time?"

```cypher
// Find all atoms and inferences before timestamp
MATCH (a:Atom)
WHERE a.createdAt <= datetime($timestamp)
WITH a
OPTIONAL MATCH (a)<-[:HAD_INPUT]-(i:Inference)
WHERE i.executedAt <= datetime($timestamp)
RETURN COUNT(DISTINCT a) AS atoms_at_time,
       COUNT(DISTINCT i) AS inferences_at_time;
```

### Cryptographic Verification

**Question**: "Has any data been tampered with?"

```cypher
// Verify atom hash integrity
MATCH (a:Atom)
WHERE a.atomHash <> dbo.fn_ComputeAtomHash(a.atomId)  // Call SQL function
RETURN a.atomId, a.atomHash AS stored_hash, 
       dbo.fn_ComputeAtomHash(a.atomId) AS recomputed_hash;
```

## Merkle DAG Properties

The provenance graph forms a **Merkle DAG** (Directed Acyclic Graph) with these guarantees:

1. **Immutability**: Once created, nodes/relationships never modified (append-only)
2. **Cryptographic Verifiability**: Hash chains ensure data integrity
3. **Bidirectional Traversal**: Walk forwards (impact) or backwards (root cause)
4. **Acyclic**: No circular reasoning chains (guaranteed by timestamp ordering)

**Merkle Tree Structure**:
```
          Inference Node (hash = H(inputs + outputs + pipeline))
                    /        |        \
                   /         |         \
         Input Atom 1   Input Atom 2   Output Atom
         (hash = H(content))           (hash = H(generated content))
              |                              |
              |                              |
        Source Node                    (stored in SQL)
     (hash = H(file))
```

## Performance Characteristics

**Graph Size**:
- 3.5B atoms → 3.5B Atom nodes
- 100M inferences/day → 100M Inference nodes/day
- Storage: ~500 bytes per node (with relationships) = 1.75TB for 3.5B atoms

**Query Performance**:
- Root cause analysis (3-hop query): 5-15ms average
- Impact analysis (5-hop forward): 25-80ms average
- Temporal queries (filtered by timestamp): 10-30ms with indexes
- Bias detection aggregation: 200-500ms (full scan with grouping)

**Scaling Strategy**:
- Sharding by `tenantId` for multi-tenant isolation
- Neo4j Enterprise clustering (5-node cluster recommended for HA)
- Read replicas for analytical queries (don't impact write path)

## Dual-Database Benefits

| Capability | SQL Server | Neo4j |
|------------|-----------|-------|
| **O(log N) Spatial Queries** | ✅ R-Tree + Vector indexes | ❌ Not optimized |
| **O(K) CLR Refinement** | ✅ In-database computation | ❌ External only |
| **Multi-Hop Provenance** | ❌ Recursive CTEs slow | ✅ Native graph traversal |
| **Pathfinding (A*)** | ✅ CLR functions | ✅ Native algorithms |
| **Cryptographic Verification** | ✅ SHA-256 UDFs | ✅ Hash chains |
| **Multi-Tenant Isolation** | ✅ Row-level security | ✅ Sharding |
| **Real-Time Ingestion** | ✅ Transactional | ⚠️ Eventual consistency |

## Summary

The Neo4j provenance graph is **not optional** - it is the foundational solution for:

1. **Explainability**: Answer "why did the AI do that?" with definitive provenance chains
2. **Auditability**: Cryptographic verification of all data transformations (Merkle DAG)
3. **Compliance**: Complete audit trail for GDPR, HIPAA, financial regulations
4. **Bias Detection**: Identify disproportionate influences from specific sources/users
5. **Temporal Analysis**: Reconstruct system state at any point in time (Laplace's Demon)
6. **Impact Analysis**: Determine downstream effects of data changes or deletions

**Key Architectural Decision**: Dual-database strategy leverages strengths of both systems - SQL for geometric queries, Neo4j for relationship traversal. Asynchronous synchronization provides eventual consistency without impacting real-time AI performance.
