# 12 - Neo4j Provenance Graph: Schema and Implementation

The Neo4j provenance graph is not just an audit trail - it is a fundamental component of the "Periodic Table of Knowledge" architecture. While SQL Server handles the geometric/spatial representation of atoms, Neo4j provides the relationship network that enables complex reasoning about how knowledge connects, transforms, and propagates through the system.

## 1. The Dual Nature of the Graph

The Neo4j graph serves two critical purposes:

1. **Provenance Ledger:** An immutable, cryptographically verifiable history of every data transformation.
2. **Knowledge Network:** A queryable map of how concepts relate, enabling "pathfinding" queries that traverse relationships to discover insights.

This dual nature means the graph schema must be designed to support both deep historical queries (provenance) and fast traversal queries (knowledge discovery).

## 2. Core Node Types

The graph is built from six primary node types, each representing a different aspect of the system's operation.

### `Atom` Node
Represents a unique, content-addressed piece of data.

```cypher
(:Atom {
  atomId: Integer,           // Primary key in SQL Server
  atomHash: String,          // SHA-256 hash (the true identifier)
  contentType: String,       // MIME type or custom type
  createdAt: DateTime,       // When this atom was first ingested
  ingestedBy: String         // User or service that created it
})
```

**Purpose:** This is the bridge between the SQL Server engine and the Neo4j graph. Every atom in SQL has a corresponding node in Neo4j.

### `Source` Node
Represents the origin of raw data.

```cypher
(:Source {
  sourceId: Integer,
  sourceType: String,        // e.g., "file", "url", "stream", "user-input"
  identifier: String,        // File path, URL, or unique identifier
  ingestedAt: DateTime,
  checksum: String          // Hash of the original source for verification
})
```

**Purpose:** Tracks where data came from, enabling impact analysis and data lineage queries.

### `IngestionJob` Node
Represents a specific execution of the ingestion pipeline.

```cypher
(:IngestionJob {
  jobId: Integer,
  jobType: String,           // e.g., "atomize-text", "atomize-model"
  startedAt: DateTime,
  completedAt: DateTime,
  status: String,            // "success", "failed", "partial"
  algorithmVersion: String   // e.g., "v1.2.0" of the atomization algorithm
})
```

**Purpose:** Allows for versioned, reproducible ingestion. You can track which algorithm version created which atoms.

### `User` Node
Represents a human user or service principal.

```cypher
(:User {
  userId: Integer,
  username: String,
  email: String,
  role: String               // e.g., "admin", "data-scientist", "service"
})
```

**Purpose:** Enables user-level auditing and permission tracking.

### `Pipeline` Node
Represents a specific version of an AI pipeline or stored procedure.

```cypher
(:Pipeline {
  pipelineId: Integer,
  name: String,              // e.g., "sp_TransformerStyleInference"
  version: String,           // Semantic version
  deployedAt: DateTime,
  codeHash: String          // Hash of the T-SQL/CLR code for verification
})
```

**Purpose:** Tracks the evolution of AI logic over time. Critical for reproducibility.

### `Inference` Node
Represents a single execution of a pipeline.

```cypher
(:Inference {
  inferenceId: Integer,
  executedAt: DateTime,
  executedBy: String,        // User or service
  durationMs: Integer,
  k_value: Integer,          // The K parameter used
  contextHash: String       // Hash of the input context for caching
})
```

**Purpose:** The core of explainability. Each inference is a node that links inputs to outputs.

## 3. Core Relationship Types

The power of the graph is in the relationships between nodes. These relationships form the Merkle DAG that provides verifiable provenance.

### `INGESTED_FROM`
Links an `Atom` to its `Source`.

```cypher
(:Atom)-[:INGESTED_FROM]->(:Source)
```

**Meaning:** This atom was extracted from this source.

### `CREATED_BY_JOB`
Links an `Atom` to the `IngestionJob` that created it.

```cypher
(:Atom)-[:CREATED_BY_JOB]->(:IngestionJob)
```

**Meaning:** This specific job execution created this atom.

### `INGESTED_BY_USER`
Links a `Source` to the `User` who initiated the ingestion.

```cypher
(:Source)-[:INGESTED_BY_USER]->(:User)
```

**Meaning:** This user is responsible for bringing this data into the system.

### `EXECUTED_BY_PIPELINE`
Links an `Inference` to the `Pipeline` version that was used.

```cypher
(:Inference)-[:EXECUTED_BY_PIPELINE]->(:Pipeline)
```

**Meaning:** This inference used this specific version of the AI logic.

### `HAD_INPUT`
Links an `Inference` to the `Atom` nodes that were its inputs.

```cypher
(:Inference)-[:HAD_INPUT]->(:Atom)
```

**Properties:** Can include metadata like `relevanceScore` or `position` to track why this atom was selected.

**Meaning:** This atom influenced this inference result.

### `GENERATED`
Links an `Inference` to the `Atom` nodes it produced.

```cypher
(:Inference)-[:GENERATED]->(:Atom)
```

**Meaning:** This inference created this atom as its output.

### `RELATED_TO` (Knowledge Network)
Links `Atom` nodes that have a semantic relationship, separate from provenance.

```cypher
(:Atom)-[:RELATED_TO {
  relationshipType: String,  // e.g., "is-synonym", "is-part-of", "contradicts"
  strength: Float,           // 0.0 to 1.0
  discoveredAt: DateTime
}]->(:Atom)
```

**Purpose:** This is the "Periodic Table" aspect. These relationships can be discovered algorithmically (e.g., atoms that are consistently spatially close) or asserted by users.

## 4. Example Provenance Chain

Here's how a complete provenance chain looks in the graph:

```
(:User {username: "alice"})
  <-[:INGESTED_BY_USER]-
(:Source {identifier: "/data/research-paper.pdf"})
  <-[:INGESTED_FROM]-
(:Atom {atomHash: "abc123...", contentType: "text/sentence"})
  <-[:HAD_INPUT]-
(:Inference {inferenceId: 42})
  -[:EXECUTED_BY_PIPELINE]->
(:Pipeline {name: "sp_SemanticExtraction", version: "v2.0"})

(:Inference {inferenceId: 42})
  -[:GENERATED]->
(:Atom {atomHash: "def456...", contentType: "application/semantic-triple"})
```

This chain shows:
1. User "alice" ingested a PDF
2. A sentence atom was extracted from it
3. An inference (ID 42) used that atom as input
4. The inference ran pipeline version v2.0
5. The inference generated a new "semantic triple" atom

## 5. Critical Cypher Queries

### Root Cause Analysis: "Where did this atom come from?"

```cypher
MATCH path = (a:Atom {atomHash: $target_hash})
             -[:CREATED_BY_JOB|INGESTED_FROM*]->
             (s:Source)
RETURN path;
```

This traces an atom back through all transformations to its original source.

### Impact Analysis: "What was influenced by this source?"

```cypher
MATCH path = (s:Source {identifier: $source_path})
             <-[:INGESTED_FROM]-
             (a:Atom)
             <-[:HAD_INPUT]-
             (i:Inference)
             -[:GENERATED]->
             (derived:Atom)
RETURN DISTINCT derived.atomHash, COUNT(path) AS influence_count
ORDER BY influence_count DESC;
```

This finds all atoms that were ultimately derived from a specific source.

### Explainability: "Why did the AI generate this result?"

```cypher
MATCH (result:Atom {atomHash: $result_hash})
      <-[:GENERATED]-
      (inference:Inference)
      -[:HAD_INPUT]->
      (input:Atom)
MATCH (inference)-[:EXECUTED_BY_PIPELINE]->(pipeline:Pipeline)
RETURN input.atomHash, pipeline.name, pipeline.version, inference.executedAt;
```

This shows all inputs and the pipeline version that generated a specific result.

### Knowledge Discovery: "What is semantically related to this concept?"

```cypher
MATCH (start:Atom {atomHash: $concept_hash})
      -[:RELATED_TO*1..3]->
      (related:Atom)
WHERE related.atomHash <> start.atomHash
RETURN DISTINCT related.atomHash, related.contentType
LIMIT 100;
```

This performs a multi-hop traversal to find concepts that are transitively related.

## 6. Indexing Strategy for Performance

Neo4j queries are only fast if the graph is properly indexed.

```cypher
-- Create constraints (these automatically create indexes)
CREATE CONSTRAINT atom_hash_unique IF NOT EXISTS
FOR (a:Atom) REQUIRE a.atomHash IS UNIQUE;

CREATE CONSTRAINT source_id_unique IF NOT EXISTS
FOR (s:Source) REQUIRE s.sourceId IS UNIQUE;

CREATE CONSTRAINT inference_id_unique IF NOT EXISTS
FOR (i:Inference) REQUIRE i.inferenceId IS UNIQUE;

-- Create additional indexes for common queries
CREATE INDEX atom_content_type IF NOT EXISTS
FOR (a:Atom) ON (a.contentType);

CREATE INDEX inference_executed_at IF NOT EXISTS
FOR (i:Inference) ON (i.executedAt);

CREATE INDEX source_identifier IF NOT EXISTS
FOR (s:Source) ON (s.identifier);
```

## 7. Data Synchronization from SQL Server

The graph must be kept in sync with the SQL Server database. This is handled by the `Hartonomous.Workers.Neo4jSync` service, which listens to SQL Service Broker events.

### Event Flow

1. SQL Server emits an event when new atoms are created (via Service Broker).
2. The `Neo4jSync` worker receives the event.
3. It queries SQL Server for the full atom metadata.
4. It creates the corresponding nodes and relationships in Neo4j.

### Ensuring Idempotency

The sync worker must handle duplicate events gracefully. Use `MERGE` instead of `CREATE`:

```cypher
MERGE (a:Atom {atomHash: $hash})
ON CREATE SET
  a.atomId = $id,
  a.contentType = $contentType,
  a.createdAt = datetime($createdAt)
ON MATCH SET
  a.lastSyncedAt = datetime();
```

This ensures that processing the same event multiple times doesn't create duplicate nodes.

## 8. The "Periodic Table" View

For the knowledge network aspect, consider creating a specialized view that clusters atoms by their semantic domain:

```cypher
// Find clusters of highly interconnected atoms
CALL gds.louvain.stream({
  nodeProjection: 'Atom',
  relationshipProjection: 'RELATED_TO'
})
YIELD nodeId, communityId
RETURN gds.util.asNode(nodeId).atomHash AS atomHash, communityId
ORDER BY communityId;
```

This uses Neo4j's Graph Data Science library to detect "elements" (clusters) in the periodic table.

The Neo4j provenance graph transforms the Hartonomous platform from a black box into a fully transparent, queryable, and verifiable knowledge system.
