# Provenance API Reference

**Endpoint Prefix**: `/api/provenance`  
**Authentication**: Required (Bearer token)  
**Rate Limit**: 500 requests/minute per tenant  
**Response Time**: 15-50ms (Neo4j graph queries)  

---

## Overview

The Provenance API provides **atom lineage tracking**, **Merkle DAG traversal**, **generation stream analysis**, and **content-addressable lookups**. Built on Neo4j graph database, it enables tracking of atom relationships, transformation chains, and influence patterns across billions of atoms.

### Core Concepts

**Content-Addressable Storage**: Every atom identified by SHA-256 hash  
**Merkle DAG**: Atoms form directed acyclic graph of transformations  
**Lineage Tracking**: Parent-child relationships across generations  
**Generation Streams**: Track atom evolution over time  
**Influence Analysis**: Identify high-impact atoms in reasoning chains  

---

## Endpoints

### 1. Get Atom Lineage

Retrieve complete lineage (ancestors and descendants) for an atom.

**Endpoint**: `GET /api/provenance/atoms/{atomId}/lineage`

#### Request

**Path Parameters**:

| Parameter | Type | Description |
|-----------|------|-------------|
| `atomId` | Integer | Atom ID to query lineage |

**Query Parameters**:

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `depth` | Integer | 5 | Maximum traversal depth |
| `direction` | String | `both` | Direction (`ancestors`, `descendants`, `both`) |
| `includeMetadata` | Boolean | true | Include atom metadata |

#### Response

**Success (200 OK)**:

```json
{
  "atomId": 12345,
  "canonicalHash": "a3f5b8c9d2e1f4a7b6c5d8e9f2a1b4c7d6e5f8a9b2c1d4e7f6a5b8c9d2e1f4a7",
  "lineage": {
    "ancestors": [
      {
        "atomId": 12000,
        "canonicalHash": "b1c2d3e4f5a6b7c8d9e0f1a2b3c4d5e6f7a8b9c0d1e2f3a4b5c6d7e8f9a0b1c2",
        "canonicalText": "Original source document",
        "generation": 0,
        "depth": 3,
        "relationship": "source",
        "transformationType": "ingestion"
      },
      {
        "atomId": 12100,
        "canonicalHash": "c2d3e4f5a6b7c8d9e0f1a2b3c4d5e6f7a8b9c0d1e2f3a4b5c6d7e8f9a0b1c2d3",
        "canonicalText": "Extracted paragraph",
        "generation": 1,
        "depth": 2,
        "relationship": "extracted-from",
        "transformationType": "extraction"
      },
      {
        "atomId": 12200,
        "canonicalHash": "d3e4f5a6b7c8d9e0f1a2b3c4d5e6f7a8b9c0d1e2f3a4b5c6d7e8f9a0b1c2d3e4",
        "canonicalText": "Processed sentence",
        "generation": 2,
        "depth": 1,
        "relationship": "derived-from",
        "transformationType": "sentence-splitting"
      }
    ],
    "descendants": [
      {
        "atomId": 12400,
        "canonicalHash": "e4f5a6b7c8d9e0f1a2b3c4d5e6f7a8b9c0d1e2f3a4b5c6d7e8f9a0b1c2d3e4f5",
        "canonicalText": "Summarized content",
        "generation": 4,
        "depth": 1,
        "relationship": "summarized-to",
        "transformationType": "summarization"
      },
      {
        "atomId": 12500,
        "canonicalHash": "f5a6b7c8d9e0f1a2b3c4d5e6f7a8b9c0d1e2f3a4b5c6d7e8f9a0b1c2d3e4f5a6",
        "canonicalText": "Referenced in reasoning",
        "generation": 4,
        "depth": 1,
        "relationship": "referenced-in",
        "transformationType": "reasoning"
      }
    ]
  },
  "totalAncestors": 3,
  "totalDescendants": 2,
  "generationDepth": 3,
  "queryTimeMs": 22
}
```

#### Example cURL Request

```bash
curl -X GET "https://api.hartonomous.ai/api/provenance/atoms/12345/lineage?depth=5&direction=both" \
  -H "Authorization: Bearer YOUR_TOKEN"
```

---

### 2. Merkle DAG Traversal

Traverse the Merkle DAG from a starting atom.

**Endpoint**: `POST /api/provenance/dag/traverse`

#### Request

**Content-Type**: `application/json`

**Body**:

```json
{
  "startAtomId": 12345,
  "traversalStrategy": "breadth-first",
  "maxDepth": 10,
  "filters": {
    "relationshipTypes": ["derived-from", "summarized-to"],
    "modalities": ["text"],
    "dateRange": {
      "from": "2025-01-01T00:00:00Z",
      "to": "2025-01-31T23:59:59Z"
    }
  },
  "includeEdgeWeights": true
}
```

**Request Fields**:

| Field | Type | Required | Default | Description |
|-------|------|----------|---------|-------------|
| `startAtomId` | Integer | Yes | - | Starting point for traversal |
| `traversalStrategy` | String | No | `breadth-first` | Strategy (`breadth-first`, `depth-first`) |
| `maxDepth` | Integer | No | 10 | Maximum traversal depth |
| `filters` | Object | No | {} | Filter criteria |
| `includeEdgeWeights` | Boolean | No | false | Include relationship weights |

#### Response

```json
{
  "startAtomId": 12345,
  "traversalStrategy": "breadth-first",
  "nodes": [
    {
      "atomId": 12345,
      "level": 0,
      "visited": true
    },
    {
      "atomId": 12400,
      "level": 1,
      "visited": true
    },
    {
      "atomId": 12500,
      "level": 1,
      "visited": true
    },
    {
      "atomId": 12600,
      "level": 2,
      "visited": true
    }
  ],
  "edges": [
    {
      "fromAtomId": 12345,
      "toAtomId": 12400,
      "relationshipType": "summarized-to",
      "weight": 0.92,
      "createdAt": "2025-01-15T10:30:00Z"
    },
    {
      "fromAtomId": 12345,
      "toAtomId": 12500,
      "relationshipType": "referenced-in",
      "weight": 0.87,
      "createdAt": "2025-01-15T11:20:00Z"
    }
  ],
  "totalNodes": 4,
  "totalEdges": 3,
  "maxDepthReached": 2,
  "queryTimeMs": 35
}
```

---

### 3. Generation Stream Tracking

Track atoms across generation streams (evolution over time).

**Endpoint**: `GET /api/provenance/generation-streams/{streamId}`

#### Request

**Path Parameters**:

| Parameter | Type | Description |
|-----------|------|-------------|
| `streamId` | String | Generation stream identifier |

**Query Parameters**:

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `fromGeneration` | Integer | 0 | Starting generation |
| `toGeneration` | Integer | ∞ | Ending generation |
| `includeMetrics` | Boolean | false | Include generation metrics |

#### Response

```json
{
  "streamId": "stream-ml-pipeline-001",
  "streamName": "Machine Learning Pipeline Evolution",
  "generations": [
    {
      "generation": 0,
      "atomCount": 1,
      "atoms": [
        {
          "atomId": 10000,
          "canonicalText": "Raw training data",
          "createdAt": "2025-01-01T00:00:00Z"
        }
      ]
    },
    {
      "generation": 1,
      "atomCount": 5,
      "atoms": [
        {
          "atomId": 10100,
          "canonicalText": "Preprocessed features",
          "createdAt": "2025-01-02T10:00:00Z"
        }
      ],
      "transformations": [
        {
          "type": "feature-engineering",
          "inputAtoms": [10000],
          "outputAtoms": [10100, 10101, 10102, 10103, 10104]
        }
      ]
    },
    {
      "generation": 2,
      "atomCount": 3,
      "atoms": [
        {
          "atomId": 10200,
          "canonicalText": "Trained model weights",
          "createdAt": "2025-01-03T15:30:00Z"
        }
      ],
      "transformations": [
        {
          "type": "model-training",
          "inputAtoms": [10100, 10101, 10102],
          "outputAtoms": [10200, 10201, 10202]
        }
      ]
    }
  ],
  "totalGenerations": 3,
  "totalAtoms": 9,
  "streamCreatedAt": "2025-01-01T00:00:00Z",
  "lastUpdatedAt": "2025-01-03T15:30:00Z"
}
```

---

### 4. Content-Addressable Lookup

Retrieve atoms by canonical hash (SHA-256).

**Endpoint**: `GET /api/provenance/atoms/by-hash/{hash}`

#### Request

**Path Parameters**:

| Parameter | Type | Description |
|-----------|------|-------------|
| `hash` | String | SHA-256 hash (64 hex characters) |

#### Response

```json
{
  "canonicalHash": "a3f5b8c9d2e1f4a7b6c5d8e9f2a1b4c7d6e5f8a9b2c1d4e7f6a5b8c9d2e1f4a7",
  "atoms": [
    {
      "atomId": 12345,
      "canonicalText": "Optimize SQL query performance",
      "modality": "text",
      "subtype": "sentence",
      "tenantId": 1,
      "createdAt": "2025-01-15T10:30:00Z",
      "metadata": {
        "sourceFile": "performance-guide.md",
        "lineNumber": 42
      }
    }
  ],
  "totalInstances": 1,
  "deduplicationApplied": true,
  "queryTimeMs": 8
}
```

**Deduplication**: If multiple atoms have same hash, only one canonical atom stored with references.

#### Example cURL Request

```bash
curl -X GET "https://api.hartonomous.ai/api/provenance/atoms/by-hash/a3f5b8c9d2e1f4a7b6c5d8e9f2a1b4c7d6e5f8a9b2c1d4e7f6a5b8c9d2e1f4a7" \
  -H "Authorization: Bearer YOUR_TOKEN"
```

---

### 5. Parent-Child Relationships

Get direct parent or child atoms.

**Endpoint**: `GET /api/provenance/atoms/{atomId}/relationships`

#### Request

**Path Parameters**:

| Parameter | Type | Description |
|-----------|------|-------------|
| `atomId` | Integer | Atom ID |

**Query Parameters**:

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `type` | String | `all` | Relationship type (`parents`, `children`, `all`) |
| `relationshipName` | String | null | Filter by relationship name |

#### Response

```json
{
  "atomId": 12345,
  "parents": [
    {
      "atomId": 12200,
      "canonicalText": "Source paragraph",
      "relationship": "derived-from",
      "weight": 1.0,
      "createdAt": "2025-01-15T10:00:00Z"
    }
  ],
  "children": [
    {
      "atomId": 12400,
      "canonicalText": "Summary",
      "relationship": "summarized-to",
      "weight": 0.92,
      "createdAt": "2025-01-15T10:45:00Z"
    },
    {
      "atomId": 12500,
      "canonicalText": "Referenced in reasoning",
      "relationship": "referenced-in",
      "weight": 0.87,
      "createdAt": "2025-01-15T11:20:00Z"
    }
  ],
  "totalParents": 1,
  "totalChildren": 2
}
```

---

### 6. Influence Analysis

Identify high-impact atoms in reasoning chains.

**Endpoint**: `POST /api/provenance/influence-analysis`

#### Request

**Content-Type**: `application/json`

**Body**:

```json
{
  "atomIds": [12345, 12400, 12500, 12600],
  "analysisType": "pagerank",
  "minInfluenceScore": 0.5,
  "includeVisualization": true
}
```

**Request Fields**:

| Field | Type | Required | Default | Description |
|-------|------|----------|---------|-------------|
| `atomIds` | Integer[] | Yes* | - | Atoms to analyze |
| `sessionId` | String | Yes* | - | Reasoning session ID (alternative) |
| `analysisType` | String | No | `pagerank` | Analysis algorithm (`pagerank`, `betweenness`, `degree`) |
| `minInfluenceScore` | Float | No | 0.0 | Minimum score threshold |
| `includeVisualization` | Boolean | No | false | Generate graph visualization |

*Either `atomIds` or `sessionId` must be provided.

#### Response

```json
{
  "influenceScores": [
    {
      "atomId": 12345,
      "canonicalText": "Database optimization techniques",
      "influenceScore": 0.92,
      "rank": 1,
      "incomingReferences": 47,
      "outgoingReferences": 12,
      "reasoningChains": 23
    },
    {
      "atomId": 12400,
      "canonicalText": "Index design patterns",
      "influenceScore": 0.85,
      "rank": 2,
      "incomingReferences": 35,
      "outgoingReferences": 8,
      "reasoningChains": 19
    }
  ],
  "totalAnalyzed": 4,
  "averageInfluence": 0.73,
  "visualization": {
    "format": "geojson",
    "type": "FeatureCollection",
    "features": [
      {
        "type": "Feature",
        "geometry": {
          "type": "Point",
          "coordinates": [4.5, 6.2, 3.1]
        },
        "properties": {
          "atomId": 12345,
          "influenceScore": 0.92,
          "label": "Database optimization techniques"
        }
      }
    ]
  },
  "analysisTimeMs": 120
}
```

---

### 7. Error Cluster Detection

Identify clusters of error-producing atoms.

**Endpoint**: `POST /api/provenance/error-clusters`

#### Request

```json
{
  "dateRange": {
    "from": "2025-01-01T00:00:00Z",
    "to": "2025-01-31T23:59:59Z"
  },
  "errorTypes": ["reasoning-error", "transformation-error"],
  "minClusterSize": 3,
  "spatialRadius": 2.0
}
```

**Request Fields**:

| Field | Type | Required | Default | Description |
|-------|------|----------|---------|-------------|
| `dateRange` | Object | No | Last 30 days | Time range |
| `errorTypes` | String[] | No | all | Filter error types |
| `minClusterSize` | Integer | No | 3 | Minimum atoms in cluster |
| `spatialRadius` | Float | No | 5.0 | Spatial clustering radius |

#### Response

```json
{
  "clusters": [
    {
      "clusterId": "cluster-001",
      "centerPoint": {
        "x": 4.5,
        "y": 6.2,
        "z": 3.1
      },
      "radius": 1.8,
      "atomCount": 12,
      "errorType": "reasoning-error",
      "commonPattern": "Circular reasoning in hypothesis generation",
      "affectedSessions": [
        "cot-abc123",
        "tot-xyz789"
      ],
      "suggestedFix": "Add cycle detection in reasoning graph traversal"
    }
  ],
  "totalClusters": 1,
  "totalErrors": 12,
  "analysisTimeMs": 95
}
```

---

### 8. Session Provenance Path

Get complete provenance path for a reasoning session.

**Endpoint**: `GET /api/provenance/sessions/{sessionId}/path`

#### Request

**Path Parameters**:

| Parameter | Type | Description |
|-----------|------|-------------|
| `sessionId` | String | Reasoning session ID |

#### Response

```json
{
  "sessionId": "cot-abc123",
  "sessionType": "chain-of-thought",
  "path": [
    {
      "stepNumber": 1,
      "atomsAccessed": [
        {
          "atomId": 12345,
          "canonicalText": "Database optimization techniques",
          "accessType": "read",
          "accessedAt": "2025-01-20T10:30:12Z"
        }
      ]
    },
    {
      "stepNumber": 2,
      "atomsAccessed": [
        {
          "atomId": 12378,
          "canonicalText": "Index design patterns",
          "accessType": "read",
          "accessedAt": "2025-01-20T10:30:18Z"
        }
      ]
    },
    {
      "stepNumber": 3,
      "atomsGenerated": [
        {
          "atomId": 15000,
          "canonicalText": "Solution: Create composite indexes",
          "generationType": "reasoning-output",
          "generatedAt": "2025-01-20T10:30:25Z"
        }
      ]
    }
  ],
  "totalAtomsAccessed": 8,
  "totalAtomsGenerated": 1,
  "sessionDurationMs": 510
}
```

---

## Advanced Features

### GeoJSON Visualization

Provenance data exported as GeoJSON for spatial visualization:

**Endpoint**: `GET /api/provenance/atoms/{atomId}/lineage?format=geojson`

#### Response

```json
{
  "type": "FeatureCollection",
  "features": [
    {
      "type": "Feature",
      "geometry": {
        "type": "Point",
        "coordinates": [4.5, 6.2, 3.1]
      },
      "properties": {
        "atomId": 12345,
        "canonicalText": "Database optimization techniques",
        "generation": 3,
        "modality": "text"
      }
    },
    {
      "type": "Feature",
      "geometry": {
        "type": "LineString",
        "coordinates": [
          [4.5, 6.2, 3.1],
          [4.7, 6.0, 3.3]
        ]
      },
      "properties": {
        "fromAtomId": 12345,
        "toAtomId": 12400,
        "relationshipType": "summarized-to",
        "weight": 0.92
      }
    }
  ]
}
```

**Visualization Tools**: Compatible with Kepler.gl, Mapbox, Deck.gl

---

### Cypher Query Execution

Execute custom Cypher queries against Neo4j graph database.

**Endpoint**: `POST /api/provenance/cypher`

**Request**:

```json
{
  "query": "MATCH (a:Atom)-[r:DERIVED_FROM]->(b:Atom) WHERE a.atomId = $atomId RETURN a, r, b LIMIT 10",
  "parameters": {
    "atomId": 12345
  }
}
```

**Response**:

```json
{
  "records": [
    {
      "a": {
        "atomId": 12345,
        "canonicalHash": "a3f5b8c9...",
        "canonicalText": "..."
      },
      "r": {
        "type": "DERIVED_FROM",
        "weight": 1.0,
        "createdAt": "2025-01-15T10:30:00Z"
      },
      "b": {
        "atomId": 12200,
        "canonicalHash": "d3e4f5a6...",
        "canonicalText": "..."
      }
    }
  ],
  "totalRecords": 1,
  "queryTimeMs": 18
}
```

**Safety**: Query validation prevents destructive operations (DELETE, DROP, MERGE).

---

### Provenance Metrics

Aggregate metrics across provenance graph.

**Endpoint**: `GET /api/provenance/metrics`

#### Response

```json
{
  "totalAtoms": 3500000000,
  "totalRelationships": 8750000000,
  "averageAncestors": 2.5,
  "averageDescendants": 2.5,
  "maxGenerationDepth": 47,
  "relationshipTypes": [
    {
      "type": "derived-from",
      "count": 2100000000
    },
    {
      "type": "summarized-to",
      "count": 1500000000
    },
    {
      "type": "referenced-in",
      "count": 980000000
    }
  ],
  "topInfluentialAtoms": [
    {
      "atomId": 45678,
      "canonicalText": "Machine learning fundamentals",
      "influenceScore": 0.98,
      "totalReferences": 125000
    }
  ]
}
```

---

## Relationship Types

### Transformation Relationships

| Relationship | Description | Example |
|--------------|-------------|---------|
| `derived-from` | Direct transformation | Sentence extracted from paragraph |
| `summarized-to` | Summarization | Paragraph → Summary |
| `expanded-from` | Expansion | Summary → Detailed explanation |
| `translated-to` | Translation | English → Spanish |
| `referenced-in` | Citation | Atom used in reasoning step |

### Provenance Relationships

| Relationship | Description | Example |
|--------------|-------------|---------|
| `source` | Original ingestion | File upload → Atom |
| `extracted-from` | Extraction | Archive → Individual file |
| `generated-by` | System generation | Reasoning → Hypothesis |
| `merged-from` | Deduplication | Duplicate atoms → Canonical atom |

---

## Neo4j Integration

### Graph Schema

**Nodes**:

```cypher
(:Atom {
  atomId: Integer,
  canonicalHash: String,
  canonicalText: String,
  modality: String,
  generation: Integer,
  createdAt: DateTime
})
```

**Relationships**:

```cypher
(a:Atom)-[:DERIVED_FROM {
  weight: Float,
  transformationType: String,
  createdAt: DateTime
}]->(b:Atom)
```

### Performance Optimization

**Indexes**:

```cypher
CREATE INDEX atom_id_idx FOR (a:Atom) ON (a.atomId);
CREATE INDEX atom_hash_idx FOR (a:Atom) ON (a.canonicalHash);
CREATE INDEX atom_generation_idx FOR (a:Atom) ON (a.generation);
```

**Query Performance**:

- **Lineage Query**: 15-25ms for depth=5
- **DAG Traversal**: 30-50ms for 1000 nodes
- **Influence Analysis**: 80-150ms for PageRank on 10,000 atoms

---

## Error Handling

### Hash Not Found

```json
{
  "error": "Atom not found",
  "hash": "a3f5b8c9d2e1f4a7b6c5d8e9f2a1b4c7d6e5f8a9b2c1d4e7f6a5b8c9d2e1f4a7",
  "message": "No atoms found with canonical hash",
  "suggestion": "Verify hash is correct (64 hex characters)"
}
```

### Circular Dependency

```json
{
  "error": "Circular dependency detected",
  "message": "Atom 12345 has circular lineage",
  "cycle": [12345, 12400, 12500, 12345],
  "suggestion": "Review transformation pipeline for cycles"
}
```

### Neo4j Connection Error

```json
{
  "error": "Graph database unavailable",
  "message": "Cannot connect to Neo4j instance",
  "retryAfter": 30,
  "fallbackMode": "SQL-based provenance (limited features)"
}
```

---

## SDK Examples

### C# SDK

```csharp
using Hartonomous.Client;

var client = new HartonomousClient("https://api.hartonomous.ai", "YOUR_TOKEN");

// Get atom lineage
var lineage = await client.Provenance.GetLineageAsync(12345, new LineageOptions
{
    Depth = 5,
    Direction = LineageDirection.Both,
    IncludeMetadata = true
});

Console.WriteLine($"Ancestors: {lineage.TotalAncestors}");
Console.WriteLine($"Descendants: {lineage.TotalDescendants}");

foreach (var ancestor in lineage.Lineage.Ancestors)
{
    Console.WriteLine($"Gen {ancestor.Generation}: {ancestor.CanonicalText}");
}

// Content-addressable lookup
var atom = await client.Provenance.GetByHashAsync(
    "a3f5b8c9d2e1f4a7b6c5d8e9f2a1b4c7d6e5f8a9b2c1d4e7f6a5b8c9d2e1f4a7"
);

Console.WriteLine($"Found atom: {atom.Atoms[0].CanonicalText}");

// Influence analysis
var influence = await client.Provenance.AnalyzeInfluenceAsync(new InfluenceRequest
{
    AtomIds = new[] { 12345, 12400, 12500 },
    AnalysisType = InfluenceAnalysis.PageRank,
    MinInfluenceScore = 0.5
});

foreach (var score in influence.InfluenceScores)
{
    Console.WriteLine($"Rank {score.Rank}: {score.CanonicalText} (Score: {score.InfluenceScore:F2})");
}
```

### Python SDK

```python
from hartonomous import HartonomousClient

client = HartonomousClient(
    base_url="https://api.hartonomous.ai",
    token="YOUR_TOKEN"
)

# Get atom lineage
lineage = client.provenance.get_lineage(
    atom_id=12345,
    depth=5,
    direction="both",
    include_metadata=True
)

print(f"Ancestors: {lineage['totalAncestors']}")
print(f"Descendants: {lineage['totalDescendants']}")

for ancestor in lineage['lineage']['ancestors']:
    print(f"Gen {ancestor['generation']}: {ancestor['canonicalText']}")

# Content-addressable lookup
atom = client.provenance.get_by_hash(
    "a3f5b8c9d2e1f4a7b6c5d8e9f2a1b4c7d6e5f8a9b2c1d4e7f6a5b8c9d2e1f4a7"
)

print(f"Found atom: {atom['atoms'][0]['canonicalText']}")

# Influence analysis
influence = client.provenance.analyze_influence(
    atom_ids=[12345, 12400, 12500],
    analysis_type="pagerank",
    min_influence_score=0.5
)

for score in influence['influenceScores']:
    print(f"Rank {score['rank']}: {score['canonicalText']} (Score: {score['influenceScore']:.2f})")
```

### JavaScript/TypeScript SDK

```typescript
import { HartonomousClient } from '@hartonomous/client';

const client = new HartonomousClient({
  baseUrl: 'https://api.hartonomous.ai',
  token: 'YOUR_TOKEN'
});

// Get atom lineage
const lineage = await client.provenance.getLineage(12345, {
  depth: 5,
  direction: 'both',
  includeMetadata: true
});

console.log(`Ancestors: ${lineage.totalAncestors}`);
console.log(`Descendants: ${lineage.totalDescendants}`);

lineage.lineage.ancestors.forEach(ancestor => {
  console.log(`Gen ${ancestor.generation}: ${ancestor.canonicalText}`);
});

// Content-addressable lookup
const atom = await client.provenance.getByHash(
  'a3f5b8c9d2e1f4a7b6c5d8e9f2a1b4c7d6e5f8a9b2c1d4e7f6a5b8c9d2e1f4a7'
);

console.log(`Found atom: ${atom.atoms[0].canonicalText}`);

// Influence analysis
const influence = await client.provenance.analyzeInfluence({
  atomIds: [12345, 12400, 12500],
  analysisType: 'pagerank',
  minInfluenceScore: 0.5
});

influence.influenceScores.forEach(score => {
  console.log(`Rank ${score.rank}: ${score.canonicalText} (Score: ${score.influenceScore.toFixed(2)})`);
});
```

---

## Related Documentation

- [Ingestion API](ingestion.md) - Data ingestion and atomization
- [Query API](query.md) - Semantic search and spatial queries
- [Reasoning API](reasoning.md) - Chain-of-Thought and Tree-of-Thought
- [Architecture: Model Atomization](../architecture/model-atomization.md) - Content-addressable storage
