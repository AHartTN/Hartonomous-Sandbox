# API Reference

**RESTful API for Hartonomous Platform**

## Base URL

```
Local Development: https://localhost:5001/api
Production: https://api.hartonomous.ai/api
```

## Authentication

### Development Mode
```json
// appsettings.json
{
  "AzureAd": {
    "Enabled": false
  }
}
```
No authentication required for local development.

### Production Mode (Entra ID OAuth2)

**1. Obtain Access Token**:
```bash
curl -X POST https://login.microsoftonline.com/{tenant-id}/oauth2/v2.0/token \
  -H "Content-Type: application/x-www-form-urlencoded" \
  -d "client_id={client-id}" \
  -d "scope=api://{client-id}/user_impersonation" \
  -d "grant_type=authorization_code" \
  -d "code={authorization-code}" \
  -d "redirect_uri={redirect-uri}"
```

**2. Use Token in Requests**:
```bash
curl https://api.hartonomous.ai/api/v1/ingestion/file \
  -H "Authorization: Bearer {access-token}"
```

### Authorization Policies

| Policy | Required Roles | Endpoints |
|--------|---------------|-----------|
| **DataIngestion** | `DataIngestion.Write`, `Admin` | POST /ingestion/* |
| **DataRead** | `DataIngestion.Read`, `DataIngestion.Write`, `Admin` | GET /ingestion/*, /search/* |
| **Admin** | `Admin` | All administrative endpoints |
| **ApiDocumentationAccess** | `Admin`, `Developer`, `PremiumSubscriber` | /swagger |

## Rate Limiting

**Global Limits** (Fixed Window):
- **100 requests/minute** across all endpoints
- **20 requests/minute** for ingestion endpoints
- **50 requests/minute** for search endpoints

**Response Headers**:
```
X-RateLimit-Limit: 100
X-RateLimit-Remaining: 87
X-RateLimit-Reset: 2025-01-15T10:30:00Z
```

**429 Too Many Requests Response**:
```json
{
  "type": "https://httpstatuses.com/429",
  "title": "Too Many Requests",
  "status": 429,
  "detail": "Rate limit exceeded. Retry after 2025-01-15T10:30:00Z",
  "instance": "/api/v1/ingestion/file"
}
```

## Error Handling

All errors follow **RFC 7807 Problem Details** standard.

### Error Response Format

```json
{
  "type": "https://tools.ietf.org/html/rfc7807#section-3.1",
  "title": "Validation Error",
  "status": 400,
  "detail": "The 'file' field is required",
  "instance": "/api/v1/ingestion/file",
  "errors": {
    "file": ["The file field is required."]
  },
  "traceId": "00-abc123-def456-01"
}
```

### HTTP Status Codes

| Code | Meaning | Common Causes |
|------|---------|---------------|
| **200** | Success | Request completed |
| **201** | Created | Resource created |
| **400** | Bad Request | Invalid input, validation failure |
| **401** | Unauthorized | Missing or invalid token |
| **403** | Forbidden | Insufficient permissions |
| **404** | Not Found | Resource doesn't exist |
| **409** | Conflict | Duplicate resource |
| **422** | Unprocessable Entity | Business logic error |
| **429** | Too Many Requests | Rate limit exceeded |
| **500** | Internal Server Error | Server error |
| **503** | Service Unavailable | Database down |

## API Controllers

### 1. Ingestion API

**Base Path**: `/api/v1/ingestion`

#### POST /file
Upload and atomize a file.

**Request** (multipart/form-data):
```
POST /api/v1/ingestion/file
Content-Type: multipart/form-data

file: <binary>
tenantId: 1
metadata: {"source": "manual_upload"}
```

**cURL Example**:
```bash
curl -X POST https://localhost:5001/api/v1/ingestion/file \
  -F "file=@document.pdf" \
  -F "tenantId=1" \
  -F "metadata={\"source\":\"manual_upload\"}"
```

**Response** (200 OK):
```json
{
  "jobId": "a1b2c3d4-e5f6-7890-abcd-ef1234567890",
  "status": "completed",
  "totalAtoms": 45678,
  "uniqueAtoms": 23456,
  "durationMs": 3456,
  "atomizerType": "DocumentAtomizer",
  "detectedFormat": "PDF",
  "warnings": null
}
```

#### POST /url
Fetch URL and atomize content.

**Request** (application/json):
```json
POST /api/v1/ingestion/url

{
  "url": "https://example.com/article.html",
  "tenantId": 1,
  "metadata": {
    "source": "web_scrape",
    "category": "news"
  }
}
```

**Response** (200 OK):
```json
{
  "jobId": "b2c3d4e5-f6g7-8901-bcde-fg2345678901",
  "status": "completed",
  "totalAtoms": 12345,
  "uniqueAtoms": 8901,
  "durationMs": 2345,
  "atomizerType": "DocumentAtomizer",
  "contentType": "text/html",
  "finalUrl": "https://example.com/article.html"
}
```

#### POST /database
Connect to database and atomize schema + data.

**Request** (application/json):
```json
POST /api/v1/ingestion/database

{
  "connectionString": "Server=db.example.com;Database=Production;...",
  "tableNames": ["Customers", "Orders", "Products"],
  "sampleRowCount": 1000,
  "tenantId": 1
}
```

**Response** (202 Accepted):
```json
{
  "jobId": "c3d4e5f6-g7h8-9012-cdef-gh3456789012",
  "status": "processing",
  "estimatedCompletionTime": "2025-01-15T10:45:00Z"
}
```

#### POST /git
Clone Git repository and atomize commits/files.

**Request** (application/json):
```json
POST /api/v1/ingestion/git

{
  "cloneUrl": "https://github.com/user/repo.git",
  "branch": "main",
  "commitDepth": 100,
  "tenantId": 1,
  "credentials": {
    "username": "git-user",
    "password": "personal-access-token"
  }
}
```

**Response** (202 Accepted):
```json
{
  "jobId": "d4e5f6g7-h8i9-0123-defg-hi4567890123",
  "status": "processing",
  "estimatedCompletionTime": "2025-01-15T11:00:00Z"
}
```

#### POST /ollama
Ingest Ollama model from local instance.

**Request** (application/json):
```json
POST /api/v1/ingestion/ollama

{
  "modelIdentifier": "llama3.2:latest",
  "tenantId": 1,
  "source": {
    "name": "Llama 3.2 from Ollama",
    "metadata": "{\"ollamaEndpoint\":\"http://localhost:11434\"}"
  }
}
```

**Response** (202 Accepted):
```json
{
  "jobId": "e5f6g7h8-i9j0-1234-efgh-ij5678901234",
  "status": "processing",
  "estimatedTotalAtoms": 110000000,
  "estimatedDurationMinutes": 45
}
```

#### GET /jobs/{jobId}
Check ingestion job status.

**Request**:
```
GET /api/v1/ingestion/jobs/a1b2c3d4-e5f6-7890-abcd-ef1234567890
```

**Response** (200 OK):
```json
{
  "jobId": "a1b2c3d4-e5f6-7890-abcd-ef1234567890",
  "status": "completed",
  "progress": 100.0,
  "totalAtoms": 45678,
  "uniqueAtoms": 23456,
  "currentAtomOffset": 45678,
  "durationMs": 3456,
  "createdAt": "2025-01-15T10:00:00Z",
  "completedAt": "2025-01-15T10:00:03.456Z",
  "error": null
}
```

**Job Status Values**:
- `pending`: Queued, not yet started
- `processing`: Currently atomizing
- `completed`: Successfully finished
- `failed`: Error occurred
- `quotaExceeded`: Tenant quota limit reached

#### GET /atoms?hash={sha256}
Query atoms by content hash.

**Request**:
```
GET /api/v1/ingestion/atoms?hash=48656c6c6f20576f726c64...&tenantId=1
```

**Response** (200 OK):
```json
{
  "atoms": [
    {
      "atomId": 12345,
      "contentHash": "48656c6c6f20576f726c64...",
      "atomicValue": "SGVsbG8gV29ybGQ=",
      "canonicalText": "Hello World",
      "modality": "text",
      "subtype": "token",
      "referenceCount": 42,
      "createdAt": "2025-01-15T10:00:00Z"
    }
  ],
  "totalResults": 1
}
```

---

### 2. Search API

**Base Path**: `/api/search`

#### POST /semantic
Pure vector similarity search.

**Request** (application/json):
```json
POST /api/search/semantic

{
  "query": "machine learning optimization techniques",
  "topK": 10,
  "tenantId": 1,
  "minSimilarity": 0.7,
  "modality": "text"  // Optional filter
}
```

**Response** (200 OK):
```json
{
  "results": [
    {
      "atomId": 789,
      "canonicalText": "gradient descent optimization",
      "modality": "text",
      "subtype": "token",
      "distance": 0.12,
      "similarity": 0.88
    },
    {
      "atomId": 790,
      "canonicalText": "learning rate scheduling",
      "modality": "text",
      "subtype": "token",
      "distance": 0.18,
      "similarity": 0.82
    }
  ],
  "totalResults": 10,
  "queryTimeMs": 24,
  "indexUsed": "spatial"
}
```

#### POST /hybrid
BM25 keyword + vector fusion search.

**Request** (application/json):
```json
POST /api/search/hybrid

{
  "query": "neural network architecture",
  "topK": 20,
  "tenantId": 1,
  "alpha": 0.5,  // 0.0 = pure keyword, 1.0 = pure vector
  "bm25Settings": {
    "k1": 1.2,
    "b": 0.75
  }
}
```

**Response** (200 OK):
```json
{
  "results": [
    {
      "atomId": 1001,
      "canonicalText": "convolutional neural network layers",
      "modality": "text",
      "fusionScore": 0.92,
      "bm25Score": 8.5,
      "vectorScore": 0.88
    }
  ],
  "totalResults": 20,
  "queryTimeMs": 45
}
```

#### POST /fusion
Vector + keyword + spatial (XYZM) fusion.

**Request** (application/json):
```json
POST /api/search/fusion

{
  "query": "database indexing strategies",
  "topK": 15,
  "tenantId": 1,
  "spatialWeight": 0.3,
  "vectorWeight": 0.5,
  "keywordWeight": 0.2,
  "spatialRegion": {
    "minX": 0,
    "maxX": 100,
    "minY": 0,
    "maxY": 100
  }
}
```

#### POST /cross-modal
Search across modalities (text→image, image→audio, etc.).

**Request** (application/json):
```json
POST /api/search/cross-modal

{
  "sourceAtomId": 456,  // Text atom: "sunset over ocean"
  "targetModality": "image",
  "topK": 10,
  "tenantId": 1
}
```

**Response** (200 OK):
```json
{
  "results": [
    {
      "atomId": 9876,
      "modality": "image",
      "subtype": "image-caption",
      "canonicalText": "Beautiful sunset over calm ocean waters",
      "distance": 0.15,
      "sourceImageUrl": "/api/atoms/9876/image"
    }
  ],
  "totalResults": 10,
  "queryTimeMs": 67
}
```

---

### 3. Inference API

**Base Path**: `/api/inference`

#### POST /jobs
Queue async inference job.

**Request** (application/json):
```json
POST /api/inference/jobs

{
  "modelId": 42,
  "inputAtomIds": [123, 456, 789],
  "maxTokens": 100,
  "temperature": 0.7,
  "tenantId": 1
}
```

**Response** (202 Accepted):
```json
{
  "jobId": "f6g7h8i9-j0k1-2345-fghi-jk6789012345",
  "status": "queued",
  "estimatedCompletionTime": "2025-01-15T10:05:00Z"
}
```

#### GET /jobs/{id}
Get inference job status.

**Request**:
```
GET /api/inference/jobs/f6g7h8i9-j0k1-2345-fghi-jk6789012345
```

**Response** (200 OK):
```json
{
  "jobId": "f6g7h8i9-j0k1-2345-fghi-jk6789012345",
  "status": "completed",
  "inputAtomIds": [123, 456, 789],
  "outputAtomIds": [1001, 1002, 1003],
  "outputText": "This is the generated output...",
  "tokensGenerated": 87,
  "durationMs": 2345,
  "cacheHit": false
}
```

#### POST /run
Synchronous inference (blocking).

**Request** (application/json):
```json
POST /api/inference/run

{
  "modelId": 42,
  "prompt": "Explain quantum computing in simple terms",
  "maxTokens": 200,
  "temperature": 0.8,
  "tenantId": 1
}
```

**Response** (200 OK):
```json
{
  "output": "Quantum computing uses quantum mechanics principles...",
  "tokensGenerated": 156,
  "atomsUsed": 234,
  "durationMs": 1234,
  "modelVersion": "llama3.2:v2.1"
}
```

#### POST /ensemble
Multi-model voting/averaging.

**Request** (application/json):
```json
POST /api/inference/ensemble

{
  "modelIds": [42, 43, 44],
  "prompt": "Is this email spam?",
  "ensembleStrategy": "MajorityVoting",  // or "WeightedAverage", "Stacking"
  "tenantId": 1
}
```

**Response** (200 OK):
```json
{
  "consensusOutput": "Yes, this appears to be spam",
  "confidence": 0.87,
  "individualOutputs": [
    {"modelId": 42, "output": "Spam", "confidence": 0.92},
    {"modelId": 43, "output": "Spam", "confidence": 0.85},
    {"modelId": 44, "output": "Not Spam", "confidence": 0.65}
  ],
  "durationMs": 3456
}
```

---

### 4. Reasoning API

**Base Path**: `/api/reasoning`

#### POST /analyze
OODA Observe phase - detect anomalies.

**Request** (application/json):
```json
POST /api/reasoning/analyze

{
  "tenantId": 1,
  "analysisScope": "full",  // or "incremental"
  "lookbackHours": 24
}
```

**Response** (200 OK):
```json
{
  "analysisId": "a1a2a3a4-b5b6-c7c8-d9d0-e1e2e3e4e5e6",
  "anomaliesDetected": 5,
  "observations": {
    "slowQueries": 3,
    "missingIndices": 2,
    "conceptDrift": [12, 34],
    "deadAtoms": 15678
  },
  "timestamp": "2025-01-15T10:00:00Z"
}
```

#### POST /hypothesize
OODA Orient phase - generate hypotheses.

**Request** (application/json):
```json
POST /api/reasoning/hypothesize

{
  "analysisId": "a1a2a3a4-b5b6-c7c8-d9d0-e1e2e3e4e5e6",
  "tenantId": 1
}
```

**Response** (200 OK):
```json
{
  "hypothesisId": "b2b3b4b5-c6c7-d8d9-e0e1-f2f3f4f5f6f7",
  "hypothesesGenerated": 4,
  "hypotheses": [
    {
      "action": "CreateIndex",
      "target": "dbo.Atom.ContentHash",
      "expectedImprovement": "80% query speedup",
      "approvalScore": 0.95
    },
    {
      "action": "PruneDeadAtoms",
      "target": "atoms with ReferenceCount=0 AND LastAccessed > 90 days",
      "expectedImprovement": "12GB storage reclaimed",
      "approvalScore": 0.88
    }
  ]
}
```

#### POST /act
OODA Act phase - execute approved actions.

**Request** (application/json):
```json
POST /api/reasoning/act

{
  "tenantId": 1,
  "autoApproveThreshold": 0.8
}
```

**Response** (200 OK):
```json
{
  "actionsExecuted": 2,
  "actionsSkipped": 0,
  "results": {
    "CreateIndex_dbo_Atom_ContentHash": "Success - Index created in 45s",
    "PruneDeadAtoms": "Success - 15,678 atoms deleted, 12.3GB reclaimed"
  },
  "timestamp": "2025-01-15T10:02:00Z"
}
```

---

### 5. Provenance API

**Base Path**: `/api/provenance`

#### GET /lineage/{atomId}
Trace atom ancestry.

**Request**:
```
GET /api/provenance/lineage/12345?tenantId=1&maxDepth=10
```

**Response** (200 OK):
```json
{
  "atomId": 12345,
  "ancestors": [
    {
      "atomId": 12000,
      "canonicalText": "Parent document",
      "depth": 1,
      "relationshipType": "DERIVED_FROM"
    },
    {
      "atomId": 11500,
      "canonicalText": "Original source",
      "depth": 2,
      "relationshipType": "DERIVED_FROM"
    }
  ],
  "totalAncestors": 2,
  "maxDepthReached": 2
}
```

#### GET /sessions/{sessionId}/paths
Retrieve reasoning session graph.

**Request**:
```
GET /api/provenance/sessions/abc-123/paths?tenantId=1
```

**Response** (200 OK):
```json
{
  "sessionId": "abc-123",
  "reasoningPaths": [
    {
      "stepNumber": 1,
      "operation": "Analyze",
      "atomId": 789,
      "branch": "main"
    },
    {
      "stepNumber": 2,
      "operation": "Hypothesize",
      "atomId": 790,
      "branch": "main"
    }
  ],
  "totalSteps": 5
}
```

---

## Pagination

All list endpoints support pagination:

**Query Parameters**:
- `page`: Page number (1-based, default: 1)
- `pageSize`: Items per page (default: 20, max: 100)

**Example**:
```
GET /api/v1/ingestion/jobs?page=2&pageSize=50
```

**Response Headers**:
```
X-Pagination-CurrentPage: 2
X-Pagination-PageSize: 50
X-Pagination-TotalPages: 10
X-Pagination-TotalCount: 487
Link: </api/v1/ingestion/jobs?page=1&pageSize=50>; rel="first",
      </api/v1/ingestion/jobs?page=3&pageSize=50>; rel="next",
      </api/v1/ingestion/jobs?page=10&pageSize=50>; rel="last"
```

## Versioning

API versioning via URL path: `/api/v1/...`, `/api/v2/...`

**Current Version**: v1
**Deprecation Policy**: 12 months notice before removing version

## SDK Examples

### C# (.NET)
```csharp
using Hartonomous.Sdk;

var client = new HartonomousClient("https://api.hartonomous.ai", apiKey);

// Upload file
var result = await client.Ingestion.UploadFileAsync(
    filePath: "document.pdf",
    tenantId: 1
);

// Semantic search
var searchResults = await client.Search.SemanticAsync(
    query: "machine learning",
    topK: 10,
    tenantId: 1
);
```

### Python
```python
from hartonomous import HartonomousClient

client = HartonomousClient("https://api.hartonomous.ai", api_key=api_key)

# Upload file
result = client.ingestion.upload_file(
    file_path="document.pdf",
    tenant_id=1
)

# Semantic search
results = client.search.semantic(
    query="machine learning",
    top_k=10,
    tenant_id=1
)
```

### JavaScript/TypeScript
```typescript
import { HartonomousClient } from '@hartonomous/sdk';

const client = new HartonomousClient('https://api.hartonomous.ai', apiKey);

// Upload file
const result = await client.ingestion.uploadFile({
  filePath: 'document.pdf',
  tenantId: 1
});

// Semantic search
const results = await client.search.semantic({
  query: 'machine learning',
  topK: 10,
  tenantId: 1
});
```

## OpenAPI Specification

**Swagger UI**: https://localhost:5001/swagger

**Download OpenAPI JSON**: https://localhost:5001/swagger/v1/swagger.json

---

**Document Version**: 2.0
**Last Updated**: January 2025
**API Version**: v1
