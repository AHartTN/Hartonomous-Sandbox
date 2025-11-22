# Query API Reference

**Endpoint Prefix**: `/api/query`  
**Authentication**: Required (Bearer token)  
**Rate Limit**: 1000 requests/minute per tenant  
**Response Time**: <25ms (O(log N) complexity)  

---

## Overview

The Query API provides semantic search, spatial KNN queries, and cross-modal retrieval across billions of atoms. Built on the **Semantic-First Architecture** (O(log N) + O(K·D) pattern), queries execute in milliseconds via R-Tree spatial indexing and 3D landmark projection.

### Query Performance

**3.5 Billion Atoms**: 18-25ms average query time  
**Scaling**: O(log N) - 10× more data → ~1.3× slower  
**Speedup**: 3,500,000× vs brute-force distance computation  

---

## Core Concepts

### Semantic-First Pattern

1. **Spatial Pre-Filter** (O(log N)): R-Tree traversal returns ~1000 candidates
2. **High-Precision Ranking** (O(K·D)): Vector similarity on candidates only
3. **Result**: Query billions in milliseconds instead of hours

### Spatial Indexing

All atoms projected into 3D semantic space:
- **X-Axis**: Abstract ↔ Concrete
- **Y-Axis**: Technical ↔ Creative
- **Z-Axis**: Static ↔ Dynamic

**Hilbert Curve Correlation**: 0.89 (validates spatial coherence)

---

## Endpoints

### 1. Semantic Search

Find atoms semantically similar to a query text or vector.

**Endpoint**: `POST /api/query/semantic`

#### Request

**Content-Type**: `application/json`

**Body**:

```json
{
  "query": "optimize SQL query performance",
  "topK": 10,
  "tenantId": 1,
  "filters": {
    "modality": ["text"],
    "sourceType": ["document", "code"],
    "dateRange": {
      "from": "2025-01-01T00:00:00Z",
      "to": "2025-01-31T23:59:59Z"
    }
  },
  "includeEmbeddings": false
}
```

**Request Fields**:

| Field | Type | Required | Default | Description |
|-------|------|----------|---------|-------------|
| `query` | String | Yes* | - | Text query to search |
| `queryVector` | Float[] | Yes* | - | 1536D embedding vector (alternative to `query`) |
| `topK` | Integer | No | 10 | Number of results to return |
| `tenantId` | Integer | No | 0 | Tenant isolation ID |
| `filters` | Object | No | {} | Filter criteria |
| `includeEmbeddings` | Boolean | No | false | Include embedding vectors in response |

*Either `query` or `queryVector` must be provided.

**Filter Options**:

```json
{
  "modality": ["text", "image", "audio", "video", "tensor"],
  "sourceType": ["document", "code", "web", "database", "model"],
  "contentType": ["application/pdf", "text/plain"],
  "subtype": ["paragraph", "sentence", "tensor-weight"],
  "dateRange": {
    "from": "2025-01-01T00:00:00Z",
    "to": "2025-01-31T23:59:59Z"
  },
  "minConfidence": 0.7,
  "spatialRadius": 5.0
}
```

#### Response

**Success (200 OK)**:

```json
{
  "results": [
    {
      "atomId": 12345,
      "score": 0.94,
      "canonicalText": "CREATE INDEX ON orders(customer_id) WHERE created_at > '2024-01-01'",
      "modality": "text",
      "subtype": "code-snippet",
      "metadata": {
        "sourceFile": "database-optimization.sql",
        "language": "sql",
        "lineNumber": 42
      },
      "spatialCoordinates": {
        "x": 4.5,
        "y": 6.2,
        "z": 3.1
      },
      "createdAt": "2025-01-15T10:30:00Z"
    },
    {
      "atomId": 12350,
      "score": 0.91,
      "canonicalText": "Query performance can be improved by creating appropriate indexes...",
      "modality": "text",
      "subtype": "paragraph",
      "metadata": {
        "sourceFile": "performance-guide.pdf",
        "page": 17
      },
      "spatialCoordinates": {
        "x": 4.7,
        "y": 6.0,
        "z": 3.3
      },
      "createdAt": "2025-01-12T14:20:00Z"
    }
  ],
  "totalResults": 2,
  "queryTimeMs": 18,
  "spatialCandidates": 1023,
  "deduplicationApplied": true
}
```

#### Example cURL Request

```bash
curl -X POST "https://api.hartonomous.ai/api/query/semantic" \
  -H "Authorization: Bearer YOUR_TOKEN" \
  -H "Content-Type: application/json" \
  -d '{
    "query": "optimize SQL query performance",
    "topK": 10,
    "filters": {
      "modality": ["text"],
      "sourceType": ["code", "document"]
    }
  }'
```

---

### 2. Spatial KNN Query

Find K nearest neighbors in 3D semantic space.

**Endpoint**: `POST /api/query/spatial/knn`

#### Request

**Content-Type**: `application/json`

**Body**:

```json
{
  "centerPoint": {
    "x": 4.5,
    "y": 6.2,
    "z": 3.1
  },
  "k": 10,
  "maxRadius": 5.0,
  "tenantId": 1
}
```

**Request Fields**:

| Field | Type | Required | Default | Description |
|-------|------|----------|---------|-------------|
| `centerPoint` | Object | Yes | - | 3D coordinates (X, Y, Z) |
| `k` | Integer | No | 10 | Number of neighbors |
| `maxRadius` | Float | No | ∞ | Maximum distance threshold |
| `tenantId` | Integer | No | 0 | Tenant isolation ID |

#### Response

```json
{
  "neighbors": [
    {
      "atomId": 12345,
      "distance": 0.23,
      "canonicalText": "...",
      "coordinates": {
        "x": 4.7,
        "y": 6.0,
        "z": 3.3
      }
    }
  ],
  "totalNeighbors": 10,
  "queryTimeMs": 12,
  "rTreeNodesVisited": 147
}
```

---

### 3. Cross-Modal Search

Search across modalities: text→image, image→text, audio→text, etc.

**Endpoint**: `POST /api/query/cross-modal`

#### Request

**Content-Type**: `application/json`

**Body**:

```json
{
  "sourceModality": "text",
  "targetModality": "image",
  "query": "sunset over mountains",
  "topK": 10,
  "tenantId": 1
}
```

**Request Fields**:

| Field | Type | Required | Default | Description |
|-------|------|----------|---------|-------------|
| `sourceModality` | String | Yes | - | Input modality (`text`, `image`, `audio`, `video`) |
| `targetModality` | String | Yes | - | Output modality to search |
| `query` | String/Bytes | Yes | - | Query content (text or base64 encoded) |
| `topK` | Integer | No | 10 | Number of results |
| `tenantId` | Integer | No | 0 | Tenant isolation ID |

#### Response

```json
{
  "results": [
    {
      "atomId": 45678,
      "score": 0.87,
      "modality": "image",
      "metadata": {
        "format": "jpeg",
        "width": 1920,
        "height": 1080,
        "sourceFile": "sunset_mountains.jpg"
      },
      "thumbnailUrl": "/api/atoms/45678/thumbnail",
      "downloadUrl": "/api/atoms/45678/download"
    }
  ],
  "totalResults": 1,
  "queryTimeMs": 22
}
```

#### Use Cases

- **Text → Image**: "show me diagrams of neural networks"
- **Image → Text**: Find documentation for visual diagrams
- **Audio → Text**: Search transcriptions from audio samples
- **Video → Image**: Extract key frames matching description

---

### 4. Filtered Search

Search with advanced filtering and faceting.

**Endpoint**: `POST /api/query/filtered`

#### Request

```json
{
  "query": "machine learning",
  "topK": 20,
  "filters": {
    "modality": ["text", "image"],
    "sourceType": ["document", "web"],
    "metadata": {
      "language": "en",
      "author": "John Doe"
    },
    "spatialBounds": {
      "minX": 0.0,
      "maxX": 10.0,
      "minY": 0.0,
      "maxY": 10.0,
      "minZ": 0.0,
      "maxZ": 10.0
    },
    "confidence": {
      "min": 0.8,
      "max": 1.0
    }
  },
  "facets": ["modality", "sourceType", "contentType"],
  "page": 1,
  "pageSize": 20
}
```

#### Response

```json
{
  "results": [...],
  "totalResults": 847,
  "page": 1,
  "pageSize": 20,
  "totalPages": 43,
  "facets": {
    "modality": {
      "text": 623,
      "image": 187,
      "video": 37
    },
    "sourceType": {
      "document": 412,
      "web": 315,
      "code": 120
    },
    "contentType": {
      "application/pdf": 298,
      "text/html": 215,
      "text/plain": 187
    }
  },
  "queryTimeMs": 24
}
```

---

### 5. Pagination

Handle large result sets with cursor-based pagination.

**Endpoint**: `POST /api/query/semantic` (with pagination params)

#### Request

```json
{
  "query": "AI research",
  "topK": 1000,
  "page": 1,
  "pageSize": 50,
  "sortBy": "score",
  "sortOrder": "desc"
}
```

**Pagination Fields**:

| Field | Type | Default | Description |
|-------|------|---------|-------------|
| `page` | Integer | 1 | Page number (1-indexed) |
| `pageSize` | Integer | 50 | Results per page (max 100) |
| `sortBy` | String | `score` | Sort field (`score`, `createdAt`, `distance`) |
| `sortOrder` | String | `desc` | Sort direction (`asc`, `desc`) |
| `cursor` | String | null | Opaque cursor for next page |

#### Response

```json
{
  "results": [...],
  "pagination": {
    "currentPage": 1,
    "pageSize": 50,
    "totalResults": 4752,
    "totalPages": 96,
    "hasNextPage": true,
    "hasPreviousPage": false,
    "nextCursor": "eyJvZmZzZXQiOjUwLCJzb3J0IjowLjk0fQ==",
    "previousCursor": null
  },
  "queryTimeMs": 19
}
```

---

### 6. Similar Atoms

Find atoms similar to a specific atom.

**Endpoint**: `GET /api/query/atoms/{atomId}/similar`

#### Request

**Path Parameters**:

| Parameter | Type | Description |
|-----------|------|-------------|
| `atomId` | Integer | Source atom ID |

**Query Parameters**:

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `topK` | Integer | 20 | Number of similar atoms |
| `maxDistance` | Float | ∞ | Maximum distance threshold |
| `excludeSelf` | Boolean | true | Exclude source atom from results |

#### Response

```json
{
  "sourceAtomId": 12345,
  "similarAtoms": [
    {
      "atomId": 12350,
      "similarity": 0.95,
      "distance": 0.12,
      "canonicalText": "...",
      "relationship": "semantic-neighbor"
    }
  ],
  "totalSimilar": 20,
  "queryTimeMs": 15
}
```

#### Example cURL Request

```bash
curl -X GET "https://api.hartonomous.ai/api/query/atoms/12345/similar?topK=20&maxDistance=2.0" \
  -H "Authorization: Bearer YOUR_TOKEN"
```

---

### 7. Aggregate Queries

Statistical aggregations over atom collections.

**Endpoint**: `POST /api/query/aggregate`

#### Request

```json
{
  "groupBy": ["modality", "sourceType"],
  "aggregations": [
    {
      "field": "score",
      "operation": "avg",
      "alias": "avgScore"
    },
    {
      "field": "atomId",
      "operation": "count",
      "alias": "totalAtoms"
    }
  ],
  "filters": {
    "dateRange": {
      "from": "2025-01-01T00:00:00Z",
      "to": "2025-01-31T23:59:59Z"
    }
  }
}
```

**Aggregation Operations**:

- `count`: Count items
- `sum`: Sum numeric values
- `avg`: Average
- `min`: Minimum
- `max`: Maximum
- `stddev`: Standard deviation

#### Response

```json
{
  "groups": [
    {
      "modality": "text",
      "sourceType": "document",
      "avgScore": 0.87,
      "totalAtoms": 1523
    },
    {
      "modality": "image",
      "sourceType": "web",
      "avgScore": 0.82,
      "totalAtoms": 847
    }
  ],
  "totalGroups": 2,
  "queryTimeMs": 31
}
```

---

### 8. Geospatial Queries

Query atoms by geographic location (for atoms with geo metadata).

**Endpoint**: `POST /api/query/geospatial`

#### Request

```json
{
  "center": {
    "latitude": 37.7749,
    "longitude": -122.4194
  },
  "radiusKm": 50.0,
  "topK": 100,
  "filters": {
    "modality": ["image", "text"]
  }
}
```

#### Response

```json
{
  "results": [
    {
      "atomId": 78901,
      "distanceKm": 12.5,
      "location": {
        "latitude": 37.8044,
        "longitude": -122.2712
      },
      "canonicalText": "...",
      "metadata": {
        "placeName": "Oakland, CA",
        "capturedAt": "2025-01-15T14:30:00Z"
      }
    }
  ],
  "totalResults": 47,
  "queryTimeMs": 22
}
```

---

## Advanced Features

### Vector Index Types

Hartonomous supports multiple vector index strategies:

#### 1. R-Tree Spatial Index (Default)

**Complexity**: O(log N)  
**Best For**: General-purpose semantic search  
**Configuration**:

```sql
CREATE SPATIAL INDEX IX_AtomEmbedding_Spatial
ON dbo.AtomEmbedding(SpatialKey)
USING GEOMETRY_GRID
WITH (
    GRIDS = (LEVEL_1 = HIGH, LEVEL_2 = HIGH, LEVEL_3 = MEDIUM, LEVEL_4 = LOW),
    CELLS_PER_OBJECT = 64
);
```

#### 2. DiskANN Vector Index (SQL Server 2025)

**Complexity**: O(log N)  
**Best For**: High-dimensional embeddings (OpenAI, Azure)  
**Configuration**:

```sql
CREATE INDEX IX_ExternalEmbeddings_Vector
ON dbo.ExternalEmbeddings(EmbeddingVector)
USING DISKANN
WITH (
    DISTANCE_METRIC = 'cosine',
    GRAPH_DEGREE = 64
);
```

#### 3. Hilbert Curve Index

**Complexity**: O(log N)  
**Best For**: Sequential access patterns  
**Correlation**: 0.89 with semantic similarity

---

### Query Optimization

#### Spatial Pre-Filtering

All queries use spatial pre-filter to reduce search space:

```
3.5B atoms → Spatial filter (O(log N)) → ~1000 candidates → Vector similarity (O(K·D)) → Top 10 results
Time: 15-20ms + 3-5ms = 18-25ms total
```

#### Query Plan Analysis

Enable query plan analysis:

```bash
curl -X POST "https://api.hartonomous.ai/api/query/semantic" \
  -H "Authorization: Bearer YOUR_TOKEN" \
  -H "X-Explain-Plan: true" \
  -d '{"query": "machine learning", "topK": 10}'
```

Response includes execution plan:

```json
{
  "results": [...],
  "executionPlan": {
    "stages": [
      {
        "stage": "EmbeddingGeneration",
        "timeMs": 12,
        "details": "OpenAI text-embedding-3-large API"
      },
      {
        "stage": "SpatialProjection",
        "timeMs": 1,
        "details": "1536D → 3D via landmark trilateration"
      },
      {
        "stage": "RTreeTraversal",
        "timeMs": 15,
        "details": "147 nodes visited, 1023 candidates"
      },
      {
        "stage": "VectorSimilarity",
        "timeMs": 4,
        "details": "1023 × 1536D cosine similarity (SIMD)"
      },
      {
        "stage": "Deduplication",
        "timeMs": 1,
        "details": "3 duplicates removed"
      }
    ],
    "totalTimeMs": 33
  }
}
```

---

## Result Scoring

### Similarity Metrics

#### Cosine Similarity (Default)

```
similarity = (A · B) / (||A|| × ||B||)
Range: [-1, 1] (normalized to [0, 1])
```

**Best For**: Text embeddings, semantic similarity

#### Euclidean Distance

```
distance = √(Σ(A[i] - B[i])²)
Range: [0, ∞)
```

**Best For**: Spatial queries, geometric proximity

#### Manhattan Distance

```
distance = Σ|A[i] - B[i]|
Range: [0, ∞)
```

**Best For**: High-dimensional sparse vectors

### Hybrid Scoring

Combine multiple metrics:

```json
{
  "scoringWeights": {
    "semanticSimilarity": 0.7,
    "spatialProximity": 0.2,
    "temporalRecency": 0.1
  }
}
```

**Formula**:

```
finalScore = (0.7 × semantic) + (0.2 × spatial) + (0.1 × temporal)
```

---

## Caching & Performance

### Query Result Caching

Frequently executed queries cached in memory:

**Cache Settings**:

```json
{
  "cacheTTL": 300,
  "maxCacheSize": 10000,
  "evictionPolicy": "LRU"
}
```

**Cache Headers**:

```
X-Cache: HIT
X-Cache-Age: 142
X-Cache-TTL: 300
```

### Embedding Cache

Pre-computed embeddings cached for common queries:

**Hit Rate**: ~85% for production workloads  
**Latency Reduction**: 12ms → 1ms for cached embeddings

---

## Error Handling

### Common Errors

**Invalid Query Vector Dimensions**:

```json
{
  "error": "Invalid vector dimension",
  "expected": 1536,
  "received": 768,
  "message": "Query vector must be 1536-dimensional"
}
```

**Spatial Index Not Found**:

```json
{
  "error": "Spatial index unavailable",
  "message": "R-Tree index rebuild in progress, retry in 60 seconds",
  "retryAfter": 60
}
```

**Quota Exceeded**:

```json
{
  "error": "Query quota exceeded",
  "limit": 1000000,
  "used": 1000234,
  "resetAt": "2025-02-01T00:00:00Z"
}
```

---

## Performance Benchmarks

### Query Latency by Dataset Size

| Dataset Size | Avg Latency | P95 Latency | P99 Latency |
|--------------|-------------|-------------|-------------|
| 1M atoms | 12ms | 18ms | 25ms |
| 10M atoms | 15ms | 22ms | 30ms |
| 100M atoms | 18ms | 25ms | 35ms |
| 1B atoms | 22ms | 30ms | 42ms |
| 3.5B atoms | 25ms | 35ms | 48ms |

### Throughput

**Single Node**: 1,200 queries/second  
**Cluster (3 nodes)**: 3,500 queries/second  
**Auto-scaling**: Up to 10,000 queries/second

---

## SDK Examples

### C# SDK

```csharp
using Hartonomous.Client;

var client = new HartonomousClient("https://api.hartonomous.ai", "YOUR_TOKEN");

// Semantic search
var results = await client.Query.SemanticSearchAsync(new SemanticSearchRequest
{
    Query = "optimize SQL performance",
    TopK = 10,
    Filters = new QueryFilters
    {
        Modality = new[] { "text" },
        SourceType = new[] { "code", "document" }
    }
});

foreach (var result in results.Results)
{
    Console.WriteLine($"Score: {result.Score:F2} - {result.CanonicalText}");
}
```

### Python SDK

```python
from hartonomous import HartonomousClient

client = HartonomousClient(
    base_url="https://api.hartonomous.ai",
    token="YOUR_TOKEN"
)

# Semantic search
results = client.query.semantic_search(
    query="optimize SQL performance",
    top_k=10,
    filters={
        "modality": ["text"],
        "source_type": ["code", "document"]
    }
)

for result in results['results']:
    print(f"Score: {result['score']:.2f} - {result['canonicalText']}")
```

### JavaScript/TypeScript SDK

```typescript
import { HartonomousClient } from '@hartonomous/client';

const client = new HartonomousClient({
  baseUrl: 'https://api.hartonomous.ai',
  token: 'YOUR_TOKEN'
});

// Semantic search
const results = await client.query.semanticSearch({
  query: 'optimize SQL performance',
  topK: 10,
  filters: {
    modality: ['text'],
    sourceType: ['code', 'document']
  }
});

results.results.forEach(result => {
  console.log(`Score: ${result.score.toFixed(2)} - ${result.canonicalText}`);
});
```

---

## Related Documentation

- [Ingestion API](ingestion.md) - Data ingestion and atomization
- [Reasoning API](reasoning.md) - Chain-of-Thought and Tree-of-Thought
- [Provenance API](provenance.md) - Atom lineage and relationships
- [Architecture: Semantic-First](../architecture/semantic-first.md) - O(log N) pattern details
