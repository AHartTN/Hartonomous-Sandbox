# API Reference

## Overview

Hartonomous Web API provides REST endpoints for embedding generation, vector search, graph queries, and autonomous system management.

**Base URL:** `https://api.hartonomous.com`

**Authentication:** Bearer token (JWT)

**Content-Type:** `application/json`

## Authentication

All API requests require a valid JWT bearer token.

### Obtain Token

```http
POST /api/auth/token
Content-Type: application/json

{
  "username": "your-username",
  "password": "your-password"
}
```

**Response:**

```json
{
  "token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
  "expiresIn": 3600,
  "tokenType": "Bearer"
}
```

### Use Token

Include in Authorization header:

```http
Authorization: Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...
```

## Embeddings API

### Generate Embedding

Create vector embedding from text.

```http
POST /api/embedding/generate
Authorization: Bearer {token}
Content-Type: application/json

{
  "text": "The quick brown fox jumps over the lazy dog",
  "modelIdentifier": "all-MiniLM-L6-v2",
  "normalize": true
}
```

**Parameters:**

| Name | Type | Required | Description |
|------|------|----------|-------------|
| text | string | Yes | Input text to embed |
| modelIdentifier | string | No | Model ID (default: all-MiniLM-L6-v2) |
| normalize | boolean | No | L2 normalize vector (default: true) |

**Response:**

```json
{
  "embeddingId": 12345,
  "dimensions": 384,
  "vector": [0.123, -0.456, 0.789, ...],
  "modelIdentifier": "all-MiniLM-L6-v2",
  "timestamp": "2025-11-07T10:30:00Z",
  "metadata": {
    "textLength": 44,
    "tokenCount": 9
  }
}
```

**Status Codes:**

- `200 OK` - Success
- `400 Bad Request` - Invalid input
- `401 Unauthorized` - Missing/invalid token
- `429 Too Many Requests` - Rate limit exceeded
- `500 Internal Server Error` - Server error

### Batch Generate Embeddings

Generate embeddings for multiple texts in single request.

```http
POST /api/embedding/batch
Authorization: Bearer {token}
Content-Type: application/json

{
  "texts": [
    "First text to embed",
    "Second text to embed",
    "Third text to embed"
  ],
  "modelIdentifier": "all-MiniLM-L6-v2"
}
```

**Response:**

```json
{
  "embeddings": [
    {
      "embeddingId": 12345,
      "dimensions": 384,
      "vector": [...]
    },
    {
      "embeddingId": 12346,
      "dimensions": 384,
      "vector": [...]
    },
    {
      "embeddingId": 12347,
      "dimensions": 384,
      "vector": [...]
    }
  ],
  "totalCount": 3,
  "processingTime": "00:00:01.234"
}
```

### Get Embedding

Retrieve existing embedding by ID.

```http
GET /api/embedding/{id}
Authorization: Bearer {token}
```

**Response:**

```json
{
  "embeddingId": 12345,
  "dimensions": 384,
  "vector": [...],
  "sourceText": "Original text that was embedded",
  "modelIdentifier": "all-MiniLM-L6-v2",
  "createdDate": "2025-11-07T10:30:00Z"
}
```

## Vector Search API

### Semantic Search

Find similar vectors using spatial indexes.

```http
POST /api/search/vector
Authorization: Bearer {token}
Content-Type: application/json

{
  "queryVector": [0.123, -0.456, 0.789, ...],
  "topK": 10,
  "threshold": 0.7,
  "filters": {
    "modelIdentifier": "all-MiniLM-L6-v2",
    "createdAfter": "2025-11-01T00:00:00Z"
  }
}
```

**Parameters:**

| Name | Type | Required | Description |
|------|------|----------|-------------|
| queryVector | number[] | Yes | Query embedding vector |
| topK | integer | No | Number of results (default: 10, max: 100) |
| threshold | number | No | Minimum similarity (0-1, default: 0.0) |
| filters | object | No | Additional filters |

**Response:**

```json
{
  "results": [
    {
      "embeddingId": 67890,
      "similarity": 0.95,
      "distance": 0.05,
      "sourceText": "Similar text content",
      "metadata": {
        "modelIdentifier": "all-MiniLM-L6-v2",
        "createdDate": "2025-11-07T09:15:00Z"
      }
    },
    {
      "embeddingId": 67891,
      "similarity": 0.89,
      "distance": 0.11,
      "sourceText": "Another similar text",
      "metadata": {...}
    }
  ],
  "totalResults": 10,
  "searchDuration": "00:00:00.0089",
  "spatialCandidates": 127,
  "exactComparisons": 127
}
```

### Hybrid Search

Combined keyword and vector search.

```http
POST /api/search/hybrid
Authorization: Bearer {token}
Content-Type: application/json

{
  "queryText": "machine learning embeddings",
  "topK": 10,
  "vectorWeight": 0.7,
  "keywordWeight": 0.3
}
```

**Response:**

```json
{
  "results": [
    {
      "embeddingId": 12345,
      "combinedScore": 0.92,
      "vectorScore": 0.95,
      "keywordScore": 0.85,
      "sourceText": "...",
      "highlights": ["machine <mark>learning</mark> <mark>embeddings</mark>"]
    }
  ],
  "totalResults": 10,
  "searchDuration": "00:00:00.0156"
}
```

## Graph API

### Query Graph Relationships

Execute graph pattern queries.

```http
POST /api/graph/query
Authorization: Bearer {token}
Content-Type: application/json

{
  "pattern": "MATCH (e:Embedding)-[:USED_IN]->(i:Inference) WHERE i.inferenceId = $id RETURN e",
  "parameters": {
    "id": 12345
  },
  "limit": 100
}
```

**Response:**

```json
{
  "nodes": [
    {
      "nodeId": 1,
      "labels": ["Embedding"],
      "properties": {
        "embeddingId": 67890,
        "dimensions": 384
      }
    }
  ],
  "relationships": [
    {
      "relationshipId": 1,
      "type": "USED_IN",
      "startNode": 1,
      "endNode": 2,
      "properties": {
        "timestamp": "2025-11-07T10:30:00Z"
      }
    }
  ],
  "totalNodes": 1,
  "totalRelationships": 1,
  "executionTime": "00:00:00.0234"
}
```

### Trace Provenance

Get complete lineage for inference result.

```http
GET /api/graph/provenance/{inferenceId}
Authorization: Bearer {token}
```

**Response:**

```json
{
  "inferenceId": 12345,
  "timestamp": "2025-11-07T10:30:00Z",
  "sourceEmbeddings": [
    {
      "embeddingId": 67890,
      "sourceText": "Original source text",
      "contribution": 0.45
    },
    {
      "embeddingId": 67891,
      "sourceText": "Another source",
      "contribution": 0.35
    }
  ],
  "modelWeights": [
    {
      "layerName": "transformer.encoder.layer.0",
      "weightCount": 147456,
      "version": "1.0.0"
    }
  ],
  "computationPath": [
    "TokenizationStep",
    "EmbeddingLookup",
    "AttentionComputation",
    "FinalProjection"
  ],
  "totalHops": 4
}
```

## Autonomous System API

### Get System Status

Check autonomous system health and metrics.

```http
GET /api/autonomy/status
Authorization: Bearer {token}
```

**Response:**

```json
{
  "oodaLoopStatus": "Running",
  "lastObservation": "2025-11-07T10:29:00Z",
  "lastAction": "2025-11-07T10:28:00Z",
  "activeHypotheses": 3,
  "successfulActions": 127,
  "failedActions": 5,
  "metrics": {
    "avgQueryLatency": 8.5,
    "queriesPerSecond": 145.3,
    "indexUtilization": 0.92
  }
}
```

### List Hypotheses

Get current autonomous improvement hypotheses.

```http
GET /api/autonomy/hypotheses
Authorization: Bearer {token}
```

**Response:**

```json
{
  "hypotheses": [
    {
      "hypothesisId": 1,
      "type": "IndexOptimization",
      "description": "Create spatial index on Embeddings.EmbeddingGeometry for faster k-NN search",
      "expectedImprovement": 0.75,
      "status": "Testing",
      "createdDate": "2025-11-07T09:00:00Z",
      "testResults": {
        "baselineLatency": 125.3,
        "optimizedLatency": 12.1,
        "improvement": 0.90
      }
    },
    {
      "hypothesisId": 2,
      "type": "CachePrecomputation",
      "description": "Pre-compute embeddings for frequently accessed documents",
      "expectedImprovement": 0.50,
      "status": "Pending",
      "createdDate": "2025-11-07T09:15:00Z"
    }
  ],
  "totalHypotheses": 2
}
```

### Approve Hypothesis

Manually approve autonomous action.

```http
POST /api/autonomy/hypotheses/{id}/approve
Authorization: Bearer {token}
```

**Response:**

```json
{
  "hypothesisId": 1,
  "status": "Approved",
  "deploymentScheduled": "2025-11-07T11:00:00Z"
}
```

## Analytics API

### Query Performance Metrics

Get system performance analytics.

```http
GET /api/analytics/performance?startDate=2025-11-01&endDate=2025-11-07
Authorization: Bearer {token}
```

**Response:**

```json
{
  "period": {
    "start": "2025-11-01T00:00:00Z",
    "end": "2025-11-07T23:59:59Z"
  },
  "metrics": {
    "totalQueries": 1250000,
    "avgLatency": 8.5,
    "p50Latency": 6.2,
    "p95Latency": 18.7,
    "p99Latency": 45.3,
    "errorRate": 0.002,
    "cacheHitRate": 0.85
  },
  "topQueries": [
    {
      "queryPattern": "SELECT TOP 10 ... WHERE VECTOR_DISTANCE(...)",
      "executionCount": 45000,
      "avgDuration": 7.2
    }
  ]
}
```

### Usage Statistics

Get API usage statistics for billing.

```http
GET /api/analytics/usage?month=2025-11
Authorization: Bearer {token}
```

**Response:**

```json
{
  "period": "2025-11",
  "embeddingsGenerated": 125000,
  "searchQueries": 450000,
  "graphQueries": 15000,
  "totalApiCalls": 590000,
  "costs": {
    "embeddings": 125.00,
    "searches": 45.00,
    "graphQueries": 15.00,
    "total": 185.00,
    "currency": "USD"
  }
}
```

## Error Responses

All errors return consistent format:

```json
{
  "error": {
    "code": "INVALID_VECTOR_DIMENSIONS",
    "message": "Vector dimensions (512) do not match model dimensions (384)",
    "details": {
      "expected": 384,
      "received": 512
    },
    "timestamp": "2025-11-07T10:30:00Z",
    "requestId": "req_abc123xyz"
  }
}
```

### Common Error Codes

| Code | HTTP Status | Description |
|------|-------------|-------------|
| UNAUTHORIZED | 401 | Missing or invalid authentication token |
| FORBIDDEN | 403 | Insufficient permissions |
| INVALID_VECTOR_DIMENSIONS | 400 | Vector dimension mismatch |
| MODEL_NOT_FOUND | 404 | Specified model does not exist |
| RATE_LIMIT_EXCEEDED | 429 | Too many requests |
| INFERENCE_TIMEOUT | 504 | Embedding generation timeout |
| INTERNAL_ERROR | 500 | Server error |

## Rate Limiting

API requests are rate limited per account:

- **Free Tier:** 100 requests/minute
- **Standard Tier:** 1000 requests/minute
- **Enterprise Tier:** 10000 requests/minute

Rate limit headers included in every response:

```http
X-RateLimit-Limit: 1000
X-RateLimit-Remaining: 995
X-RateLimit-Reset: 1699363200
```

## Webhooks

Configure webhooks to receive event notifications.

### Register Webhook

```http
POST /api/webhooks
Authorization: Bearer {token}
Content-Type: application/json

{
  "url": "https://your-app.com/webhooks/hartonomous",
  "events": ["embedding.created", "hypothesis.deployed"],
  "secret": "your-webhook-secret"
}
```

### Webhook Events

**embedding.created:**

```json
{
  "event": "embedding.created",
  "timestamp": "2025-11-07T10:30:00Z",
  "data": {
    "embeddingId": 12345,
    "dimensions": 384,
    "modelIdentifier": "all-MiniLM-L6-v2"
  }
}
```

**hypothesis.deployed:**

```json
{
  "event": "hypothesis.deployed",
  "timestamp": "2025-11-07T11:00:00Z",
  "data": {
    "hypothesisId": 1,
    "type": "IndexOptimization",
    "measuredImprovement": 0.92
  }
}
```

## SDK Examples

### C# / .NET

```csharp
using Hartonomous.Client;

var client = new HartonomousClient("your-api-key");

// Generate embedding
var embedding = await client.Embeddings.GenerateAsync(
    text: "Sample text to embed",
    modelIdentifier: "all-MiniLM-L6-v2"
);

// Vector search
var results = await client.Search.VectorSearchAsync(
    queryVector: embedding.Vector,
    topK: 10,
    threshold: 0.7
);
```

### Python

```python
from hartonomous import HartonomousClient

client = HartonomousClient(api_key="your-api-key")

# Generate embedding
embedding = client.embeddings.generate(
    text="Sample text to embed",
    model_identifier="all-MiniLM-L6-v2"
)

# Vector search
results = client.search.vector_search(
    query_vector=embedding.vector,
    top_k=10,
    threshold=0.7
)
```

### JavaScript / TypeScript

```typescript
import { HartonomousClient } from '@hartonomous/client';

const client = new HartonomousClient({ apiKey: 'your-api-key' });

// Generate embedding
const embedding = await client.embeddings.generate({
  text: 'Sample text to embed',
  modelIdentifier: 'all-MiniLM-L6-v2'
});

// Vector search
const results = await client.search.vectorSearch({
  queryVector: embedding.vector,
  topK: 10,
  threshold: 0.7
});
```

## Support

- API Status: https://status.hartonomous.com
- GitHub Issues: https://github.com/AHartTN/Hartonomous-Sandbox/issues
- Documentation: https://docs.hartonomous.com
- Email: api-support@hartonomous.dev
